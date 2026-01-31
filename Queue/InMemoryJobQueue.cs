using System.Threading.Channels;

namespace BackgroundJobService.Queue;

public class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel;

    public InMemoryJobQueue()
    {
        _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct)
        => _channel.Writer.WriteAsync(jobId, ct);

    public ValueTask<Guid> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct);
}
