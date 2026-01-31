using BackgroundJobService.Jobs;
using Microsoft.EntityFrameworkCore;

namespace BackgroundJobService.Data;

public class AppDbContext : DbContext
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
    public DbSet<JobExecutionLog> JobExecutionLogs => Set<JobExecutionLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
