using System.ComponentModel.DataAnnotations;

namespace BackgroundJobService.Jobs;

public class JobRecord
{
    [Key]
    public Guid Id { get; set; }

    public JobType Type { get; set; }

    [MaxLength(2048)]
    public string PayloadJson { get; set; } = "{}";

    public JobStatus Status { get; set; } = JobStatus.Queued;

    public int AttemptCount { get; set; } = 0;

    public int MaxAttempts { get; set; } = 3;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }
}
