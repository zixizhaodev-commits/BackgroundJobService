using System.Text.Json.Serialization;
using BackgroundJobService.Data;
using BackgroundJobService.Jobs;
using BackgroundJobService.Queue;
using BackgroundJobService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Serilog configuration
// --------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// --------------------
// Controllers + Swagger
// --------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // IMPORTANT:
        // Serialize enums as strings instead of integers
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------
// EF Core (SQLite)
// --------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    options.UseSqlite(cs);
});

// --------------------
// Queue + Worker
// --------------------
builder.Services.AddSingleton<IJobQueue, InMemoryJobQueue>();
builder.Services.AddHostedService<JobProcessorWorker>();

// --------------------
// Job Handlers
// --------------------
builder.Services.AddScoped<IJobHandler, ImportJobHandler>();
builder.Services.AddScoped<IJobHandler, ReportJobHandler>();

var app = builder.Build();

// --------------------------------
// Auto-migrate on startup (demo use)
// --------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --------------------
// Middleware
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
