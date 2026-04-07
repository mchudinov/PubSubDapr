using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Messaging.PublishSubscribe;

namespace Sub;

public class SubscriberWorker(
    DaprPublishSubscribeClient pubSubClient,
    IActorProxyFactory proxyFactory,
    ILogger<SubscriberWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new DaprSubscriptionOptions(new MessageHandlingPolicy(TimeSpan.FromSeconds(60), TopicResponseAction.Retry));

        await using var subscription = await pubSubClient.SubscribeAsync("servicebus_pubsub", "topic1", options, HandleMessageAsync, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<TopicResponseAction> HandleMessageAsync(TopicMessage message, CancellationToken cancellationToken)
    {
        var text = System.Text.Encoding.UTF8.GetString(message.Data.Span);
        message.Extensions.TryGetValue("traceid", out var traceId);
        logger.LogInformation("Received message: '{Message}' with Trace ID: {TraceId}", text, traceId);

        try
        {
            var actorId = new ActorId(Guid.NewGuid().ToString());
            var actor = proxyFactory.CreateActorProxy<IMessageHandlerActor>(actorId, "MessageHandlerActor");
            await actor.HandleMessageAsync(text);
            return TopicResponseAction.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Actor failed to process message: '{Message}'", text);
            return TopicResponseAction.Retry;
        }
    }
}
