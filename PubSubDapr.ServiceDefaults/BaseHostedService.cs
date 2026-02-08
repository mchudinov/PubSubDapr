using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace PubSubDapr.ServiceDefaults;

public abstract class BaseHostedWorker : IHostedService, IDisposable
{
    protected readonly ILogger _logger;
    private Timer? _timer;
    protected int _workerPeriodSeconds = 5;

    protected abstract string WorkerName { get; }

    public BaseHostedWorker(IConfiguration configuration, ILogger logger)
    {
        _logger = logger ?? NullLogger.Instance;
        _workerPeriodSeconds = int.TryParse(configuration[$"{WorkerName}:WorkerPeriodSeconds"], out int result) ? result : _workerPeriodSeconds;
    }

    protected abstract Task DoWork(object state);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {WorkerName} worker", WorkerName);
        _timer = new Timer(async state => await DoWork(state), null, TimeSpan.Zero, TimeSpan.FromSeconds(_workerPeriodSeconds));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {WorkerName} worker", WorkerName);
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
