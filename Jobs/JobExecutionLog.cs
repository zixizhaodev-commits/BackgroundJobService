using System.ComponentModel.DataAnnotations;

namespace BackgroundJobService.Jobs;

public class JobExecutionLog
{
    [Key]
    public long Id { get; set; }

    public Guid JobId { get; set; }

    public int AttemptNumber { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FinishedAtUtc { get; set; }

    public bool Succeeded { get; set; }

    [MaxLength(2048)]
    public string? Error { get; set; }

    public long DurationMs { get; set; }
}
