using BackgroundJobService.Data;
using BackgroundJobService.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackgroundJobService.Controllers;

[ApiController]
[Route("api/jobs/{jobId:guid}/logs")]
[Produces("application/json")]
public class JobLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobLogsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<JobExecutionLog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<JobExecutionLog>>> GetLogs(Guid jobId, CancellationToken ct)
    {
        var logs = await _db.JobExecutionLogs
            .Where(x => x.JobId == jobId)
            .OrderBy(x => x.AttemptNumber)
            .ToListAsync(ct);

        return Ok(logs);
    }
}
