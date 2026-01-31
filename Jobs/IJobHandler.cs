namespace BackgroundJobService.Jobs;

public interface IJobHandler
{
    JobType Type { get; }
    Task ExecuteAsync(JobRecord job, CancellationToken ct);
}
