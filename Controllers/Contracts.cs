using BackgroundJobService.Jobs;

namespace BackgroundJobService.Controllers;

public record SubmitJobRequest(
    JobType Type,
    object Payload,
    int? MaxAttempts
);

public record SubmitJobResponse(
    Guid JobId,
    string StatusUrl
);
