namespace BackgroundJobService.Queue;

public interface IJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken ct);
    ValueTask<Guid> DequeueAsync(CancellationToken ct);
}
