using System.Diagnostics;
using Bogus;
using ServiceDefaults;

namespace Pub;

public class PublisherWorker(
    IConfiguration configuration, 
    ILogger<PublisherWorker> logger, 
    Dapr.Client.DaprClient daprClient) : BaseHostedWorker(configuration, logger)
{
    protected override string WorkerName { get => "Publisher"; }
    public static long ExecutionTime { get; private set; }

    private readonly Dapr.Client.DaprClient _daprClient = daprClient;

    protected override async Task DoWork(object state)
    {
        _logger.LogInformation("Publisher worker is working.");

        try
        {
            // Generate a manual W3C traceparent (format: 00-traceid-spanid-flags)
            // Example: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
            var traceId = ActivityTraceId.CreateRandom().ToHexString();
            var spanId = ActivitySpanId.CreateRandom().ToHexString();
            var traceParent = $"00-{traceId}-{spanId}-01";
            var metadata = new Dictionary<string, string>
            {
                { "cloudevent.traceid", traceId },
                { "cloudevent.traceparent", traceParent }
            };

            var faker = new Faker();
            var ingVerb = faker.Hacker.IngVerb();
            var message = $"{char.ToUpper(ingVerb[0])}{ingVerb[1..]} {faker.Hacker.Noun()}";

            await _daprClient.PublishEventAsync("servicebus_pubsub", "topic1", message, metadata);

            _logger.LogInformation("Message '{Message}' published with Trace ID: {TraceId}.", message, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to publish message: {ex.Message}");
        }
    }
}
