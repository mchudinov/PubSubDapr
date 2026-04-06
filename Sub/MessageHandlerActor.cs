using Dapr.Actors.Runtime;

namespace Sub;

public class MessageHandlerActor : Actor, IMessageHandlerActor
{
    private readonly ILogger<MessageHandlerActor> _logger;

    public MessageHandlerActor(ActorHost host, ILogger<MessageHandlerActor> logger)
        : base(host)
    {
        _logger = logger;
    }

    public Task HandleMessageAsync(string message)
    {
        _logger.LogInformation(
            "Actor {ActorId} handled message: {Message}",
            Id.GetId(),
            message);
        return Task.CompletedTask;
    }
}
