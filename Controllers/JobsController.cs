using BackgroundJobService.Data;
using BackgroundJobService.Jobs;
using BackgroundJobService.Queue;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackgroundJobService.Controllers;

[ApiController]
[Route("api/jobs")]
[Produces("application/json")]
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
    [ProducesResponseType(typeof(SubmitJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitJobResponse>> Submit([FromBody] SubmitJobRequest request, CancellationToken ct)
    {
        if (request.MaxAttempts is <= 0)
        {
            // Optional: keep it simple; you can also default to 3 instead of returning 400.
            // Here we default to 3 to be demo-friendly.
        }

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

        var statusUrl = Url.ActionLink(nameof(GetById), values: new { id = job.Id }) ?? $"api/jobs/{job.Id}";
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, new SubmitJobResponse(job.Id, statusUrl));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobRecord>> GetById(Guid id, CancellationToken ct)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (job is null) return NotFound();
        return Ok(job);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<JobRecord>), StatusCodes.Status200OK)]
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
