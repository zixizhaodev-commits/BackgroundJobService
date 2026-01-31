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
- 📦 Persistent job lifecycle tracking (`Queued → Running → Succeeded / Failed`)
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
