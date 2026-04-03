using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

var topic = serviceBus.AddServiceBusTopic("topic");
topic.AddServiceBusSubscription("subscription");

var daprPath = Path.Combine(Directory.GetCurrentDirectory(), ".dapr\\components");
var daprConfigPath = Path.Combine(daprPath, "global.yaml");

// Component files are written here at startup; not tracked in git.
var generatedPath = Path.Combine(Directory.GetCurrentDirectory(), ".dapr", "generated");
Directory.CreateDirectory(generatedPath);

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
            ResourcesPaths = [generatedPath],
            PlacementHostAddress = "",
            LogLevel = "Debug",
            Config = daprConfigPath,
            EnableApiLogging = true
        });
    })
    .WaitFor(serviceBus);

// The CommunityToolkit Dapr integration launches daprd through its own lifecycle hook,
// bypassing Aspire's env-var pipeline. Instead, generate pubsub.yaml on disk before
// daprd reads it. 'UseDevelopmentEmulator=true' is stripped because Dapr's Go Azure
// SDK doesn't recognise it and fails to parse the connection string.
builder.Eventing.Subscribe<BeforeResourceStartedEvent>(serviceBus.Resource, async (_, ct) =>
{
    var cs = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(ct);
    if (cs is null) return;

    var stripped = string.Join(";", cs.Split(';')
        .Where(p => !string.IsNullOrEmpty(p) &&
                    !p.StartsWith("UseDevelopmentEmulator", StringComparison.OrdinalIgnoreCase)));

    await File.WriteAllTextAsync(
        Path.Combine(generatedPath, "pubsub.yaml"),
        $"""
        apiVersion: dapr.io/v1alpha1
        kind: Component
        metadata:
          name: servicebus_pubsub
          namespace: default
        spec:
          type: pubsub.azure.servicebus
          version: v1
          metadata:
            - name: connectionString
              value: "{stripped}"
            - name: disableEntityManagement
              value: "true"
        """,
        ct);
});

builder.Build().Run();
