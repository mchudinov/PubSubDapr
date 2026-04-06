using System.Diagnostics;
using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Mvc;

namespace Sub;

[ApiController]
public class TopicController(IActorProxyFactory proxyFactory, ILogger<TopicController> logger) : ControllerBase
{
    [Topic("servicebus_pubsub", "topic1")]
    [HttpPost("topic1")]
    public async Task<IActionResult> HandleMessage([FromBody] string message)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        logger.LogInformation("Received message: {Message} with Trace ID: {TraceId}", message, traceId);

        // Use a stable ActorId per message so each retry reactivates the same actor
        // with its persisted state (completed seconds), appearing as a fresh "new actor"
        // to the caller while Dapr resumes from saved progress.
        var actorId = new ActorId(traceId ?? Guid.NewGuid().ToString());
        var actorNumber = 1;

        while (true)
        {
            logger.LogInformation("Spawning actor #{ActorNumber} with ID {ActorId}", actorNumber, actorId.GetId());
            var actor = proxyFactory.CreateActorProxy<IMessageHandlerActor>(actorId, "MessageHandlerActor");
            try
            {
                await actor.HandleMessageAsync(message);
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Actor #{ActorNumber} disappeared: {Error}. Spawning a new actor...", actorNumber, ex.Message);
                actorNumber++;
            }
        }

        return Ok();
    }
}
