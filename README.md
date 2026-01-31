## System Architecture

```mermaid
flowchart TB
    client["Client (Browser / cURL / Frontend App)"]
    api["ASP.NET Core API - BackgroundJobService"]
    queue["InMemory Queue (Channel<Guid>)"]
    worker["JobProcessorWorker (BackgroundService)"]
    handlers["Job Handlers: ImportJobHandler / ReportJobHandler"]
    db["SQLite Database (EF Core)"]
    logs["JobExecutionLogs (Audit Trail)"]

    client -->|POST /api/jobs| api
    client -->|GET /api/jobs/{id}| api
    client -->|GET /api/jobs/{id}/logs| api

    api -->|Enqueue(jobId)| queue
    queue -->|Dequeue(jobId)| worker
    worker -->|Execute job| handlers
    worker -->|Update job state| db
    handlers -->|Persist results| db
    worker -->|Write attempt logs| logs


# Background Job & Task Processing Service (ASP.NET Core)

A lightweight background job processing service that demonstrates how to:
- Offload long-running work (imports / report generation) from HTTP requests
- Track job status and attempts in persistent storage
- Increase reliability with retry policies (exponential backoff)
- Improve observability with structured logs and per-attempt audit records

> This project is intentionally small but “production-shaped”: clear boundaries, persistence, retries, and debuggable execution history.

---

## Why this exists

Synchronous APIs are a bad fit for long-running work:
- Requests time out
- Thread pool pressure increases
- Failures are harder to diagnose and retry

This service accepts a job request, returns a `jobId` immediately, and executes the job asynchronously in a background worker.

---

## Key Features

- **Non-blocking APIs**: job submission returns immediately (HTTP 201 + jobId).
- **Persistent job state**: SQLite + EF Core store job record (Queued/Running/Succeeded/Failed).
- **Retries with exponential backoff**: Polly retry policy improves reliability.
- **Audit trail per attempt**: each execution attempt is stored in `JobExecutionLogs`.
- **Structured logging**: Serilog outputs useful logs for debugging.

---

## Architecture (High Level)

**Controllers**
- `POST /api/jobs` → create + enqueue job
- `GET /api/jobs/{id}` → query job status
- `GET /api/jobs/{jobId}/logs` → query attempt history (audit trail)

**Queue**
- In-memory queue based on Channels (`IJobQueue`).
- Easy to swap to Redis/RabbitMQ later for multi-instance scaling.

**Worker**
- `JobProcessorWorker` dequeues jobs and runs the correct handler.
- Updates job status + persists attempt logs.
- Uses Polly for retries with backoff.

**Handlers**
- `IJobHandler` defines job handlers.
- Example handlers simulate Import / Report jobs.

---

## Tech Stack

- .NET 8 + ASP.NET Core
- EF Core + SQLite
- Serilog (Console + File)
- Polly (Retry Policies)
- Swagger/OpenAPI

---

## Getting Started

### Prerequisites
- .NET 8 SDK

### Restore
```bash
dotnet restore
