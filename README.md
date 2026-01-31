# 🛠 Background Job & Task Processing Service

A lightweight **background job processing service** for handling long-running tasks
(such as imports and report generation) **without blocking HTTP requests**.

This project demonstrates **asynchronous job execution**, **persistent job state tracking**,
**retry policies with exponential backoff**, and **execution-level observability** —
in a small but **production-shaped** backend service.

---

## ✨ Features

- 🚀 Non-blocking job submission (HTTP requests return immediately)
- 🧵 Background job processing using `BackgroundService`
- 📦 Persistent job lifecycle tracking (Queued → Running → Succeeded / Failed)
- 🔁 Automatic retries with exponential backoff (Polly)
- 🧾 Per-attempt execution audit trail
- 📊 Structured logging with Serilog
- 🔗 RESTful API with Swagger UI
- ⚙️ SQLite + Entity Framework Core
- 🧩 Clean separation of API, queue, worker, and handlers

---

## 🧱 System Architecture

```mermaid
flowchart TB
    Client["Client<br/>(Browser / cURL / Frontend App)"]
    API["ASP.NET Core API<br/>BackgroundJobService"]
    Queue["In-Memory Queue<br/>(Channel<Guid>)"]
    Worker["JobProcessorWorker<br/>(BackgroundService)"]
    Handlers["Job Handlers<br/>(Import / Report)"]
    DB["SQLite Database<br/>(EF Core)"]
    Logs["JobExecutionLogs<br/>(Audit Trail)"]

    Client -->|POST /api/jobs| API
    Client -->|GET /api/jobs/{id}| API
    Client -->|GET /api/jobs/{id}/logs| API

    API -->|Enqueue jobId| Queue
    Queue -->|Dequeue jobId| Worker
    Worker -->|Execute job| Handlers
    Worker -->|Update job state| DB
    Handlers -->|Persist results| DB
    Worker -->|Write attempt logs| Logs


## 🤔 Why This Exists

Synchronous APIs are a poor fit for long-running work:

- Requests time out
- Thread pool pressure increases
- Failures are difficult to retry and diagnose

This service accepts a job request, returns a `jobId` immediately, and executes
the job asynchronously in a background worker, allowing clients to poll for status
or inspect execution history.

---

## 🔌 API Overview

### Create Job


POST /api/jobs


Request

{
  "type": "Import",
  "payload": {
    "fileName": "demo.csv",
    "rows": 1000
  },
  "maxAttempts": 3
}


Response

{
  "jobId": "uuid",
  "statusUrl": "/api/jobs/uuid"
}

Get Job Status
GET /api/jobs/{id}


Returns current job state, attempt count, timestamps, and last error (if any).

{
  "id": "uuid",
  "type": "Import",
  "status": "Failed",
  "attemptCount": 3,
  "maxAttempts": 3,
  "lastError": "Simulated import failure based on payload."
}

Get Execution Logs
GET /api/jobs/{id}/logs


Returns per-attempt execution history, including duration and error messages.

[
  {
    "attemptNumber": 1,
    "succeeded": false,
    "error": "Simulated import failure based on payload.",
    "durationMs": 3012
  },
  {
    "attemptNumber": 2,
    "succeeded": false,
    "error": "Simulated import failure based on payload.",
    "durationMs": 2987
  }
]

🧠 Design Notes

Job execution is decoupled from HTTP requests

Retry behavior is handled centrally using Polly

Job handlers are extensible via the IJobHandler abstraction

Execution logs provide auditability and debuggability

Queue implementation is intentionally simple and replaceable
(Redis / RabbitMQ can be swapped in later)

🛠 Tech Stack

ASP.NET Core (.NET 8)

Entity Framework Core

SQLite

Polly (retry policies)

Serilog (structured logging)

Swagger / OpenAPI

🚀 Local Development
Prerequisites

.NET 8 SDK

Run Locally
dotnet restore
dotnet run


Swagger UI:

http://localhost:5042/swagger

👤 Author

Zixi Zhao
Computer Science Graduate
Interested in Backend Systems, Reliability, and Scalable Architectures

📄 License

MIT