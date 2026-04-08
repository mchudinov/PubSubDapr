using Dapr.Actors.Runtime;

namespace Sub;

public class MessageHandlerActor : Actor, IMessageHandlerActor
{
    private const string CompletedStepsKey = "completedSteps";
    private static readonly Random _random = Random.Shared;
    private readonly ILogger<MessageHandlerActor> _logger;

    public MessageHandlerActor(ActorHost host, ILogger<MessageHandlerActor> logger)
        : base(host)
    {
        _logger = logger;
    }

    public async Task HandleMessageAsync(string message)
    {
        var completed = await StateManager.GetOrAddStateAsync(CompletedStepsKey, 0);
        var willCrash = _random.NextDouble() < 0.5;
        var totalSteps = 5;
        int[] crashCandidates = [2, 3, 4];
        var eligible = crashCandidates.Where(s => s > completed).ToArray();
        var crashStep = (willCrash && eligible.Length > 0) ? eligible[_random.Next(eligible.Length)] : -1;

        _logger.LogInformation("Actor {ActorId} resuming message '{Message}' from step {From}/{TotalSteps}",
            Id.GetId(), message, completed + 1, totalSteps);

        for (var step = completed + 1; step <= totalSteps; step++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            if (step == crashStep)
            {
                await StateManager.SetStateAsync(CompletedStepsKey, step - 1);
                await StateManager.SaveStateAsync();

                _logger.LogError("Actor {ActorId} crashed at step {Step}/{TotalSteps} — crashed!", Id.GetId(), step, totalSteps);
                throw new Exception("Exception: Ah! Oh! Crash!");
            }

            _logger.LogInformation("Actor {ActorId} processing step {Step}/{TotalSteps}", Id.GetId(), step, totalSteps);
        }

        // Reset state so the actor slot is clean for reuse
        await StateManager.SetStateAsync(CompletedStepsKey, 0);
        await StateManager.SaveStateAsync();

        _logger.LogInformation("Actor {ActorId} finished processing message: '{Message}'", Id.GetId(), message);
    }
}
