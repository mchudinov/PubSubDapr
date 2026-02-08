using PubSubDapr.ServiceDefaults;

namespace Pub;

public class PublisherWorker : BaseHostedWorker
{
    protected override string WorkerName { get => "Publisher"; }
    public static long ExecutionTime { get; private set; }

    private readonly Dapr.Client.DaprClient _daprClient;

    public PublisherWorker(IConfiguration configuration, ILogger<PublisherWorker> logger, Dapr.Client.DaprClient daprClient)
            : base(configuration, logger)
    {
        _daprClient = daprClient;
    }

    protected override async Task DoWork(object state)
    {
        _logger.LogInformation("Publisher worker is working.");

        try
        {
            await _daprClient.PublishEventAsync("servicebus", "topic", "Hello world!");
            _logger.LogInformation("Message 'Hello world' published to service bus.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to publish message: {ex.Message}");
        }
    }
}
