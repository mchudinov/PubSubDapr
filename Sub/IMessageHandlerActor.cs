using Dapr.Actors;

namespace Sub;

public interface IMessageHandlerActor : IActor
{
    Task HandleMessageAsync(string message);
}
