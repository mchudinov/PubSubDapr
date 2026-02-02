using PubSubDapr.ServiceDefaults;

namespace Pub;

public class PublisherWorker : BaseHostedWorker
{
    protected override string WorkerName { get => "Publisher"; }
    public static long ExecutionTime { get; private set; }

    public PublisherWorker(IConfiguration configuration, ILogger<PublisherWorker> logger)
            : base(configuration, logger)
    {

    }

    protected override void DoWork(object state)
    {
        _logger.LogInformation("Publisher worker is working.");
    }
}
