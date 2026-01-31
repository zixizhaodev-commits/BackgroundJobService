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

## Why This Exists

Synchronous APIs are a poor fit for long-running work:

- Requests time out
- Thread pool pressure increases
- Failures are difficult to retry and diagnose

This service accepts a job request, returns a `jobId` immediately, and executes the job asynchronously in a background worker.

---

## Key Features

- **Non-blocking APIs**  
  Job submission returns immediately (`HTTP 201 Created`).

- **Persistent job state**  
  Job lifecycle stored using SQLite + EF Core:  
  `Queued → Running → Succeeded / Failed`

- **Retries with exponential backoff**  
  Polly retry policies automatically retry transient failures.

- **Execution audit trail**  
  Every execution attempt is recorded in `JobExecutionLogs`.

- **Structured logging**  
  Serilog provides structured logs for observability and debugging.

---

## API Overview

### Submit a Job

POST /api/jobs


**Example request**
```json
{
  "type": "Import",
  "payload": {
    "fileName": "demo.csv",
    "rows": 1000
  },
  "maxAttempts": 3
}


Example response

{
  "jobId": "uuid",
  "statusUrl": "/api/jobs/uuid"
}

Get Job Status
GET /api/jobs/{id}


Returns the current job state, attempt count, timestamps, and last error (if any).

Example response

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


Returns per-attempt execution history including duration and error messages.

Example response

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

Architecture Breakdown
Controllers

Handle HTTP requests

Persist job records

Enqueue job IDs

Expose job status and execution logs

Queue

In-memory queue based on Channel

Easily replaceable with Redis or RabbitMQ for distributed setups

Worker

JobProcessorWorker runs as a background service

Dequeues jobs

Applies retry policies

Updates job state and execution logs

Job Handlers

IJobHandler abstraction

Each job type implements its own handler

Example handlers simulate import and report generation

Tech Stack

.NET 8 + ASP.NET Core

EF Core + SQLite

Serilog (Console + File)

Polly (Retry policies)

Swagger / OpenAPI

Getting Started
Prerequisites

.NET 8 SDK

Run Locally
dotnet restore
dotnet run


Swagger UI:

http://localhost:5042/swagger