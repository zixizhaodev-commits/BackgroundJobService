using BackgroundJobService.Data;
using BackgroundJobService.Jobs;
using BackgroundJobService.Queue;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackgroundJobService.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJobQueue _queue;
    private readonly ILogger<JobsController> _logger;

    public JobsController(AppDbContext db, IJobQueue queue, ILogger<JobsController> logger)
    {
        _db = db;
        _queue = queue;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SubmitJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitJobResponse>> Submit([FromBody] SubmitJobRequest request, CancellationToken ct)
    {
        var job = new JobRecord
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            PayloadJson = JsonSerializer.Serialize(request.Payload),
            Status = JobStatus.Queued,
            MaxAttempts = request.MaxAttempts is > 0 ? request.MaxAttempts.Value : 3,
            AttemptCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(job.Id, ct);

        _logger.LogInformation("Job {JobId} submitted. Type={JobType}", job.Id, job.Type);

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, new SubmitJobResponse(
            job.Id,
            Url.ActionLink(nameof(GetById), values: new { id = job.Id }) ?? $"api/jobs/{job.Id}"
        ));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobRecord>> GetById(Guid id, CancellationToken ct)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (job is null) return NotFound();
        return Ok(job);
    }

    [HttpGet]
    public async Task<ActionResult<List<JobRecord>>> List([FromQuery] int take = 20, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);

        var jobs = await _db.Jobs
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        return Ok(jobs);
    }
}
