using BackgroundJobService.Data;
using BackgroundJobService.Jobs;
using BackgroundJobService.Queue;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace BackgroundJobService.Services;

public class JobProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJobQueue _queue;
    private readonly ILogger<JobProcessorWorker> _logger;

    public JobProcessorWorker(IServiceScopeFactory scopeFactory, IJobQueue queue, ILogger<JobProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job processor worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try
            {
                jobId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessJobAsync(jobId, stoppingToken);
        }

        _logger.LogInformation("Job processor worker stopped.");
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handlers = scope.ServiceProvider.GetServices<IJobHandler>().ToList();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobProcessorWorker>>();

        var job = await db.Jobs.FirstOrDefaultAsync(x => x.Id == jobId, ct);
        if (job is null)
        {
            logger.LogWarning("Job {JobId} not found in database.", jobId);
            return;
        }

        var handler = handlers.FirstOrDefault(h => h.Type == job.Type);
        if (handler is null)
        {
            job.Status = JobStatus.Failed;
            job.LastError = $"No handler registered for job type {job.Type}.";
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogError("No handler registered for job type {JobType}. Job {JobId} failed.", job.Type, job.Id);
            return;
        }

        job.Status = JobStatus.Running;
        job.StartedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        // IMPORTANT:
        // MaxAttempts includes the first execution attempt.
        // Polly retryCount should be MaxAttempts - 1.
        var retryCount = Math.Max(0, job.MaxAttempts - 1);

        AsyncRetryPolicy retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))),
                onRetry: (ex, delay, attempt, _) =>
                {
                    logger.LogWarning(ex,
                        "Job {JobId} failed. Retry {Retry}/{MaxRetries} in {Delay}.",
                        job.Id, attempt, retryCount, delay);
                });

        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                // Guard: never exceed MaxAttempts
                if (job.AttemptCount >= job.MaxAttempts)
                {
                    throw new InvalidOperationException(
                        $"Attempt limit exceeded. AttemptCount={job.AttemptCount}, MaxAttempts={job.MaxAttempts}");
                }

                // Increment attempt
                job.AttemptCount += 1;
                await db.SaveChangesAsync(ct);

                // Create an execution log entry
                var execLog = new JobExecutionLog
                {
                    JobId = job.Id,
                    AttemptNumber = job.AttemptCount,
                    StartedAtUtc = DateTimeOffset.UtcNow
                };

                db.JobExecutionLogs.Add(execLog);
                await db.SaveChangesAsync(ct);

                try
                {
                    logger.LogInformation("Executing job {JobId}. Attempt {Attempt}/{MaxAttempts}.",
                        job.Id, job.AttemptCount, job.MaxAttempts);

                    await handler.ExecuteAsync(job, ct);

                    execLog.Succeeded = true;
                    execLog.FinishedAtUtc = DateTimeOffset.UtcNow;
                    execLog.DurationMs = (long)(execLog.FinishedAtUtc.Value - execLog.StartedAtUtc).TotalMilliseconds;
                    execLog.Error = null;

                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    execLog.Succeeded = false;
                    execLog.FinishedAtUtc = DateTimeOffset.UtcNow;
                    execLog.DurationMs = (long)(execLog.FinishedAtUtc.Value - execLog.StartedAtUtc).TotalMilliseconds;
                    execLog.Error = ex.Message;

                    await db.SaveChangesAsync(ct);

                    throw; // rethrow so Polly triggers retry
                }
            });

            job.Status = JobStatus.Succeeded;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.LastError = null;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Job {JobId} succeeded.", job.Id);
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            job.LastError = ex.Message;
            await db.SaveChangesAsync(ct);

            logger.LogError(ex, "Job {JobId} failed after {Attempts} attempts.", job.Id, job.AttemptCount);
        }
    }
}
