using Microsoft.Extensions.Logging;

namespace BackgroundJobService.Jobs;

public class ReportJobHandler : IJobHandler
{
    private readonly ILogger<ReportJobHandler> _logger;

    public ReportJobHandler(ILogger<ReportJobHandler> logger)
    {
        _logger = logger;
    }

    public JobType Type => JobType.Report;

    public async Task ExecuteAsync(JobRecord job, CancellationToken ct)
    {
        _logger.LogInformation("Starting report job {JobId} with payload: {Payload}", job.Id, job.PayloadJson);

        // Simulate report generation
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        _logger.LogInformation("Report job {JobId} completed successfully.", job.Id);
    }
}
