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
        // The current Activity is automatically populated if using OpenTelemetry
        var traceId = Activity.Current?.TraceId.ToString();

        logger.LogInformation("Received message: {Message} with Trace ID: {TraceId}", message, traceId);

        var actor = proxyFactory.CreateActorProxy<IMessageHandlerActor>(new ActorId(Guid.NewGuid().ToString()), "MessageHandlerActor");        
        await actor.HandleMessageAsync(message);

        return Ok();
    }
}
