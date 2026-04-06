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
        logger.LogInformation("Received message: '{Message}' with Trace ID: {TraceId}", message, traceId);

        var actorId = new ActorId(traceId ?? Guid.NewGuid().ToString());
        var actor = proxyFactory.CreateActorProxy<IMessageHandlerActor>(actorId, "MessageHandlerActor");
        await actor.HandleMessageAsync(message);

        return Ok();
    }
}
