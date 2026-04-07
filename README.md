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

## Azure Infrastructure

Publisher and Subscriber as Azure Container Apps with Dapr sidecars
Azure Service Bus (Standard tier minimum, topics require Standard+) with topic topic1 and subscription subscription1
Azure Cache for Redis for Dapr actor state store (used by MessageHandlerActor in Subscriber)
Container Apps Environment with Dapr enabled and the two Dapr components registered
Files to Create
infra/
  main.bicep            # Entry point: params, modules wiring
  modules/
    servicebus.bicep    # Service Bus namespace, topic, subscription
    redis.bicep         # Azure Cache for Redis
    containerapps.bicep # CAE, Dapr components, publisher app, subscriber app
Resource Details
Azure Service Bus (servicebus.bicep)
SKU: Standard (topics require Standard or Premium)
Namespace name: param serviceBusNamespaceName
Topic: topic1, infinite max retention
Subscription: subscription1 on topic1, maxDeliveryCount: 10
Output: connection string (primary) via listKeys
Azure Cache for Redis (redis.bicep)
SKU: Basic C0 (dev/demo)
Name: param redisCacheName
Output: hostname + primary access key via listKeys
Container Apps Environment + Apps (containerapps.bicep)
Container Apps Environment

Dapr enabled (built-in — no extra flag needed, Dapr is available per-app)
Log Analytics workspace attached
Dapr Component — pubsub (registered on the environment)

Component name: servicebus_pubsub
Type: pubsub.azure.servicebus
Version: v1
Metadata:
connectionString: Service Bus primary connection string
consumerID: subscription1
disableEntityManagement: true
Scopes: pub-app-id, sub-app-id
Dapr Component — statestore (registered on the environment)

Component name: statestore
Type: state.redis
Version: v1
Metadata:
redisHost: <redis-hostname>:6380
redisPassword: Redis primary key
enableTLS: true
actorStateStore: true
Scopes: sub-app-id only
Publisher Container App

Name: publisher
Dapr app ID: pub-app-id
Dapr app port: 8082
Dapr enabled: true
Container image: param publisherImage (e.g. mcr.microsoft.com/dotnet/samples:aspnetapp as placeholder)
Min replicas: 1, Max replicas: 3
Ingress: external, port 8082
Subscriber Container App

Name: subscriber
Dapr app ID: sub-app-id
Dapr app port: 8083
Dapr enabled: true
Container image: param subscriberImage
Min replicas: 1, Max replicas: 3
Ingress: external, port 8083