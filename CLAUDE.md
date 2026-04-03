# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Dapr-powered pub/sub demo using .NET 10, .NET Aspire for orchestration, and Azure Service Bus for messaging. The publisher runs as a container job, publishing messages via Dapr sidecar to Azure Service Bus.

## Commands

```bash
# Build the solution
dotnet build

# Run with Aspire (starts all services, Dapr sidecars, and Azure Service Bus emulator)
dotnet run --project PubSubDapr.AppHost

# Build the Pub container image
docker build -t pub-app -f Pub/Dockerfile .
```

No test or lint commands are configured in this project.

## Architecture

Three projects in `PubSubDapr.slnx`:

- **`Pub/`** — ASP.NET Core Worker Service that publishes messages. `PublisherWorker` inherits from `BaseHostedWorker` and calls `DaprClient.PublishEventAsync()` on a timer (default 5s, configurable via `Publisher:WorkerPeriodSeconds`). Targets pubsub component `servicebus_pubsub`, topic `topic`.

- **`PubSubDapr.AppHost/`** — .NET Aspire orchestrator. `AppHost.cs` wires up the Azure Service Bus emulator, configures the Dapr sidecar for `pub-app-id` (gRPC 50001, HTTP 3500), and points it at `.dapr/components/` for component definitions. The Service Bus connection string flows from Aspire into the Dapr component via `AzureServiceBus__ConnectionString`.

- **`PubSubDapr.ServiceDefaults/`** — Shared library. `BaseHostedWorker` is the timer-based base class for all workers. `Extensions.cs` registers OpenTelemetry, health checks (`/healthz`, `/livez`), service discovery, and HTTP resilience.

## Key Configuration

**Dapr components** live in `PubSubDapr.AppHost/.dapr/components/`:

- `pubsub.yaml` — Azure Service Bus component (`pubsub.azure.servicebus`), reads connection string from env `AzureServiceBus__ConnectionString`
- `global.yaml` — Tracing (OTLP to `localhost:4317`), metrics, and access control policy for `pub-app-id`

**Publisher settings** (`Pub/appsettings.json`): Kestrel on port 8082, Serilog with Console + OpenTelemetry sinks.

## Data Flow

```text
AppHost (Aspire orchestrator)
  └─ Pub Worker ──► Dapr sidecar ──► Azure Service Bus
                    (app-id: pub-app-id, port 3500)   (component: servicebus_pubsub)
```

## Rules

Always use Context7 when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.
