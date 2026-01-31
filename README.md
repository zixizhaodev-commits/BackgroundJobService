# Background Job & Task Processing Service (ASP.NET Core)

A lightweight background job processing service that demonstrates how to:

- Offload long-running work (imports / report generation) from HTTP requests
- Track job lifecycle and execution attempts in persistent storage
- Increase reliability with retry policies (exponential backoff)
- Improve observability with structured logs and per-attempt audit records

> This project is intentionally small but **production-shaped**: clear boundaries, persistence, retries, and debuggable execution history.

---

## System Architecture

```mermaid
flowchart TB
    client["Client (Browser / cURL / Frontend App)"]
    api["ASP.NET Core API (BackgroundJobService)"]
    queue["InMemory Queue (Channel)"]
    worker["JobProcessorWorker (BackgroundService)"]
    handlers["Job Handlers (Import / Report)"]
    db["SQLite Database (EF Core)"]
    logs["JobExecutionLogs (Audit Trail)"]

    client -->|"POST /api/jobs"| api
    client -->|"GET /api/jobs/{id}"| api
    client -->|"GET /api/jobs/{id}/logs"| api

    api -->|"Enqueue jobId"| queue
    queue -->|"Dequeue jobId"| worker
    worker -->|"Execute job"| handlers
    worker -->|"Update job state"| db
    handlers -->|"Persist results"| db
    worker -->|"Write attempt logs"| logs
