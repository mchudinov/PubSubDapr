using Dapr.Actors.Runtime;

namespace Sub;

public class MessageHandlerActor : Actor, IMessageHandlerActor
{
    private const string CompletedSecondsKey = "completedSeconds";
    private static readonly Random _random = Random.Shared;
    private readonly ILogger<MessageHandlerActor> _logger;

    public MessageHandlerActor(ActorHost host, ILogger<MessageHandlerActor> logger)
        : base(host)
    {
        _logger = logger;
    }

    public async Task HandleMessageAsync(string message)
    {
        var completed = await StateManager.GetOrAddStateAsync(CompletedSecondsKey, 0);
        var crashSecond = _random.Next(completed + 1, 4); // crash on one of the remaining seconds (not the last)

        _logger.LogInformation("Actor {ActorId} resuming message '{Message}' from second {From}/4",
            Id.GetId(), message, completed + 1);

        for (var second = completed + 1; second <= 4; second++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            if (second == crashSecond)
            {
                await StateManager.SetStateAsync(CompletedSecondsKey, second - 1);
                await StateManager.SaveStateAsync();

                _logger.LogError("Actor {ActorId} crashed at second {Second}/4 — disappearing!", Id.GetId(), second);
                throw new Exception("Ah! Oh! Crash!");
            }

            _logger.LogInformation("Actor {ActorId} processing second {Second}/4", Id.GetId(), second);
        }

        // Reset state so the actor slot is clean for reuse
        await StateManager.SetStateAsync(CompletedSecondsKey, 0);
        await StateManager.SaveStateAsync();

        _logger.LogInformation("Actor {ActorId} finished processing message: '{Message}'", Id.GetId(), message);
    }
}
