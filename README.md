# Pub-Sub powered by Dapr

## Project Overview

A Dapr-powered pub/sub demo using .NET 10, .NET Aspire for orchestration, and Azure Service Bus for messaging. The publisher runs as a container job, publishing messages via Dapr sidecar to Azure Service Bus.

## Crash

**On crash**: the actor saves *completedSeconds* to state, logs "crashed at second N/4 — disappearing!", and throws. From the controller's perspective, the actor is gone.

**New actor**: the controller catches the exception, logs "Spawning actor #2...", and calls the **same ActorId** again. Dapr reactivates the actor (new in-memory instance = "new actor") which reads *completedSeconds* from state and resumes from the next second.
