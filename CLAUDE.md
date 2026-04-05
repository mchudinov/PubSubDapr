# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Dapr-powered pub/sub demo using .NET 10, .NET Aspire for orchestration, and Azure Service Bus for messaging. The publisher periodically publishes messages via Dapr sidecar to Azure Service Bus; the subscriber receives them via Dapr's push delivery.

## Commands

```bash
# Build the solution
dotnet build

# Run with Aspire (starts all services, Dapr sidecars, and Azure Service Bus emulator)
dotnet run --project PubSubDapr.AppHost
```

No test or lint commands are configured in this project.

## Architecture

Four projects in `PubSubDapr.slnx`:

- **`Pub/`** — ASP.NET Core Worker Service. `PublisherWorker` inherits from `BaseHostedWorker` and calls `DaprClient.PublishEventAsync("servicebus_pubsub", "topic1", ...)` on a timer (default 5s, configurable via `Publisher:WorkerPeriodSeconds`). Kestrel on port 8082.

- **`Sub/`** — ASP.NET Core Web Service. `SubscriberWorker` inherits from `BaseHostedWorker` (timer is a heartbeat only). The actual message intake is a minimal-API endpoint `POST /topic1` registered via `.WithTopic("servicebus_pubsub", "topic1")`. When Dapr pushes a message, the endpoint reads the raw body and calls `worker.HandleMessageAsync(message)`. Kestrel on port 8083.

- **`PubSubDapr.AppHost/`** — .NET Aspire orchestrator. Key responsibilities:
  - Provisions Azure Service Bus emulator (persistent container, host port 55555) with topic `topic1` / subscription `subscription1`.
  - At startup, generates `.dapr/generated/pubsub.yaml` on disk (git-ignored) containing the live connection string. This is necessary because the CommunityToolkit Dapr integration launches `daprd` directly, bypassing Aspire's env-var injection. `UseDevelopmentEmulator=true` is kept in the connection string — required by the Go AMQP SDK to skip TLS.
  - Configures two sidecars with distinct ports: `pub-app-id` (gRPC 50001, HTTP 3500, metrics 9090) and `sub-app-id` (gRPC 50002, HTTP 3501, metrics 9091).
  - Exposes a Service Bus Emulator UI via `AddAsbEmulatorUi`.

- **`PubSubDapr.ServiceDefaults/`** — Shared library. `BaseHostedService` is the timer-based abstract base for all workers. `Extensions.cs` registers OpenTelemetry, health checks (`/healthz`, `/livez`), service discovery, and HTTP resilience.

## Key Configuration

**Dapr components** — static files in `PubSubDapr.AppHost/.dapr/components/`:

- `global.yaml` — OTLP tracing (`localhost:18889`), metrics, and access control policies for both app IDs.

**Generated at runtime** in `PubSubDapr.AppHost/.dapr/generated/` (git-ignored):

- `pubsub.yaml` — Azure Service Bus component (`pubsub.azure.servicebus`), connection string embedded, `consumerID: "subscription1"`, `disableEntityManagement: "true"`.

**If you change topic/subscription names**, update all three: `AppHost.cs` (provisioning + `consumerID`), `PublisherWorker.cs` (publish call), `Sub/Program.cs` (`.WithTopic(...)`). Then delete the stale `.dapr/generated/pubsub.yaml` before restarting so AppHost regenerates it.

## Data Flow

```text
AppHost (Aspire orchestrator)
  ├─ Pub Worker ──► Dapr sidecar (pub-app-id) ──► Azure Service Bus (topic1)
  │                 gRPC :50001, HTTP :3500
  │
  └─ Sub Worker ◄── Dapr sidecar (sub-app-id) ◄── Azure Service Bus (topic1/subscription1)
                    gRPC :50002, HTTP :3501
                    POST /topic1 → HandleMessageAsync()
```

## Dapr Pub/Sub Subscription Pattern

Dapr push delivery requires three things in the subscriber:

1. `app.UseCloudEvents()` — unwraps CloudEvents envelope, making raw data available as the request body.
2. `app.MapSubscribeHandler()` — responds to `GET /dapr/subscribe` so Dapr discovers registered topics.
3. `.WithTopic("componentName", "topicName")` on the `MapPost` route — registers the endpoint with Dapr. The `[Topic]` attribute does **not** work on minimal API lambdas; always use `.WithTopic(...)`.

The endpoint reads `HttpRequest.Body` directly (not `[FromBody]`) to avoid content-type validation failures when raw messages arrive without a `Content-Type` header.

## Rules

1. Always use Context7 when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.
2. Always use excalidraw MCP when I need to draw a diagram.
