using PubSubDapr.ServiceDefaults;

namespace Sub;

public class SubscriberWorker(
    IConfiguration configuration,
    ILogger<SubscriberWorker> logger) : BaseHostedWorker(configuration, logger)
{
    protected override string WorkerName { get => "Subscriber"; }

    protected override Task DoWork(object state)
    {
        _logger.LogInformation("Subscriber is ready for messages.");
        return Task.CompletedTask;
    }

    public Task HandleMessageAsync(string message)
    {
        _logger.LogInformation("Received message from topic: {Message}", message);
        return Task.CompletedTask;
    }
}
