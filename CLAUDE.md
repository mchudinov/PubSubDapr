# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Dapr-powered pub/sub demo using .NET 10, .NET Aspire for orchestration, and Azure Service Bus for messaging. The publisher periodically publishes messages via Dapr sidecar to Azure Service Bus; the subscriber receives them via Dapr's **pull (streaming) delivery** using `DaprPublishSubscribeClient`.

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

- **`Sub/`** — ASP.NET Core Web Service. `SubscriberWorker` is a `BackgroundService` that opens a gRPC streaming subscription to the Dapr sidecar via `DaprPublishSubscribeClient.SubscribeAsync()`. On each message it delegates to a `MessageHandlerActor` (Dapr Virtual Actor) that processes the message over 4 simulated seconds, saving progress to Redis state after each second, and may randomly crash mid-processing to demonstrate actor resiliency (retry + resume from saved state). Kestrel on port 8083.

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

**If you change topic/subscription names**, update: `AppHost.cs` (provisioning + `consumerID`), `PublisherWorker.cs` (publish call), `SubscriberWorker.cs` (`SubscribeAsync` call). Then delete the stale `.dapr/generated/pubsub.yaml` before restarting so AppHost regenerates it.

## Data Flow

```text
AppHost (Aspire orchestrator)
  ├─ Pub Worker ──► Dapr sidecar (pub-app-id) ──► Azure Service Bus (topic1)
  │                 gRPC :50001, HTTP :3500
  │
  └─ Sub Worker ◄══► Dapr sidecar (sub-app-id) ──► Azure Service Bus (topic1/subscription1, pull)
                    gRPC :50002, HTTP :3501
                    Sub Worker opens duplex stream; sidecar pulls from ASB and streams messages back
                    SubscriberWorker → MessageHandlerActor → Redis state
```

## Dapr Pub/Sub Subscription Pattern

The subscriber uses **pull (streaming) delivery** via the `Dapr.Messaging` NuGet package:

- `builder.Services.AddDaprPubSubClient()` — registers `DaprPublishSubscribeClient` (namespace: `Dapr.Messaging.PublishSubscribe.Extensions`).
- `DaprPublishSubscribeClient.SubscribeAsync("servicebus_pubsub", "topic1", options, handler, ct)` — opens a gRPC duplex stream to the Dapr sidecar. Returns an `IAsyncDisposable`; subscription is active immediately after `await` (no `StartAsync` needed).
- `DaprSubscriptionOptions` wraps a `MessageHandlingPolicy(timeout, defaultAction)` — SDK-side only, not configurable via Dapr YAML. Controls how long the SDK waits for the handler before sending a default `TopicResponseAction`.
- Handler returns `TopicResponseAction.Success`, `Retry`, or `Drop` to ACK/NAK the message at the sidecar.
- Trace ID from the publisher is available in `TopicMessage.Extensions["traceid"]` (Dapr strips the `cloudevent.` prefix from CloudEvent extension attributes).

## Dapr Actor Pattern

`MessageHandlerActor` implements `IMessageHandlerActor` and extends `Actor` (from `Dapr.Actors.Runtime`):

- Each incoming message gets a fresh actor with a random `ActorId` (`Guid.NewGuid()`).
- Simulates 4-second processing with a 50% chance of crashing on any intermediate second.
- On crash: saves completed seconds to Redis state (`StateManager.SetStateAsync` + `SaveStateAsync`), then throws.
- On retry (via Dapr actor resiliency): resumes from saved `completedSeconds`, not from 0.
- On success: resets state to 0 so the actor slot is clean for reuse.

Actor resiliency is configured in `.dapr/components/resiliency.yaml` with `actorRetry` (constant, 3 retries).

## Rules

1. Always use Context7 when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.
2. Always use drawio MCP when I need to draw any diagram.
