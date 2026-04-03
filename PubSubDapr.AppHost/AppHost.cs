using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

var topic = serviceBus.AddServiceBusTopic("topic");
topic.AddServiceBusSubscription("subscription");

var daprPath = Path.Combine(Directory.GetCurrentDirectory(), ".dapr\\components");
var daprConfigPath = Path.Combine(daprPath, "global.yaml");

builder.AddProject<Projects.Pub>("pub")
    .WithEnvironment("AzureServiceBus__ConnectionString", serviceBus.Resource.ConnectionStringExpression)
    .WithDaprSidecar(sidecar =>
    {
        sidecar.WithOptions(new DaprSidecarOptions
            {
                AppId = "pub-app-id",
                AppPort = 8082,
                DaprGrpcPort = 50001,
                DaprHttpPort = 3500,
                MetricsPort = 9090,
                ResourcesPaths = [daprPath],
                LogLevel = "Debug",
                Config = daprConfigPath,
                EnableApiLogging = true
            });
        // daprd is a separate process with its own environment.
        // Inject the emulator connection string so pubsub.yaml can read it via `env:`.
        sidecar.Resource.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
        {
            //var connStr = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            //context.EnvironmentVariables["AzureServiceBus__ConnectionString"] = connStr ?? string.Empty;
            var connStr = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
            if (connStr is not null)
            {
                var stripped = string.Join(";", connStr.Split(';')
                    .Where(p => !string.IsNullOrEmpty(p) &&
                                !p.StartsWith("UseDevelopmentEmulator", StringComparison.OrdinalIgnoreCase)));
                context.EnvironmentVariables["AzureServiceBus__ConnectionString"] = stripped;
            }
        }));
    })
    .WaitFor(serviceBus);

builder.Build().Run();
