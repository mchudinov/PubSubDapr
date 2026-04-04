using PubSubDapr.ServiceDefaults;

namespace Sub;

public class SubscriberWorker(
    IConfiguration configuration, 
    ILogger<SubscriberWorker> logger, 
    Dapr.Client.DaprClient daprClient) : BaseHostedWorker(configuration, logger)
{
    protected override string WorkerName { get => "Subscriber"; }
    public static long ExecutionTime { get; private set; }

    private readonly Dapr.Client.DaprClient _daprClient = daprClient;

    protected override async Task DoWork(object state)
    {
        _logger.LogInformation("Subscriber worker is working.");

        try
        {
            _logger.LogInformation("Message 'Hello world' subscribed to service bus.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
        }
    }
}
