using Microsoft.Extensions.Logging;

namespace BackgroundJobService.Jobs;

public class ImportJobHandler : IJobHandler
{
    private readonly ILogger<ImportJobHandler> _logger;

    public ImportJobHandler(ILogger<ImportJobHandler> logger)
    {
        _logger = logger;
    }

    public JobType Type => JobType.Import;

    public async Task ExecuteAsync(JobRecord job, CancellationToken ct)
    {
        _logger.LogInformation("Starting import job {JobId} with payload: {Payload}", job.Id, job.PayloadJson);

        // Simulate a long-running import
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        // Simulate failure if payload contains: "shouldFail": true
        if (job.PayloadJson.Contains("\"shouldFail\":true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Simulated import failure based on payload.");
        }

        _logger.LogInformation("Import job {JobId} completed successfully.", job.Id);
    }
}
