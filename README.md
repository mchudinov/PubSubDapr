# Pub-Sub powered by Dapr

## Project Overview

A Dapr-powered pub/sub demo using .NET 10, .NET Aspire for orchestration, and Azure Service Bus for messaging. The publisher runs as a container job, publishing messages via Dapr sidecar to Azure Service Bus.

## Crash

**On crash**: the actor saves *completedSeconds* to state, logs "crashed at second N/4 — disappearing!", and throws. From the controller's perspective, the actor is gone.

**New actor**: the controller catches the exception, logs "Spawning actor #2...", and calls the **same ActorId** again. Dapr reactivates the actor (new in-memory instance = "new actor") which reads *completedSeconds* from state and resumes from the next second.

## Pull delivery

In **SubscriberWorker**, once **ExecuteAsync** runs:

It sets up the streaming subscription with Dapr sidecar
*Task.Delay(Timeout.Infinite, stoppingToken)* keeps it alive indefinitely.
When the app shuts down, *stoppingToken* is cancelled → *Task.Delay* throws *OperationCanceledException* → *ExecuteAsync* exits → subscription is disposed via *await using*.

**HandleMessageAsync** is called by the Dapr .NET SDK (*PublishSubscribeReceiver*) internally, whenever a message arrives from the Dapr sidecar over the gRPC stream.

The call chain is:

```code
Azure Service Bus
  → Dapr sidecar (sub-app-id, gRPC :50002)
    → PublishSubscribeReceiver (background loop inside Dapr.Messaging)
      → HandleMessageAsync(message, cancellationToken)
```

Internally, **PublishSubscribeReceiver** runs two concurrent loops after **SubscribeAsync** is called:

1. Fetch loop — reads incoming **SubscribeTopicEventsResponseAlpha1** frames from the gRPC duplex stream and writes them to an internal **Channel<TopicMessage>**
2. Process loop — reads from that channel and calls your **HandleMessageAsync** for each message

**Handler's return value** drives what the receiver sends back to the sidecar on the same duplex stream:

- TopicResponseAction.Success → sidecar ACKs the message to Azure Service Bus (message deleted)
- TopicResponseAction.Retry → sidecar NAKs it (message redelivered after the broker's lock timeout)
- TopicResponseAction.Drop → sidecar drops it without retry (dead-letter if configured)

**Concurrency**: messages are processed one at a time per subscription — the process loop awaits your handler before picking up the next message from the channel.
