using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator(emulator =>
    {
        emulator.WithHostPort(55555);
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var topic1 = serviceBus.AddServiceBusTopic("topic1");
topic1.AddServiceBusSubscription("subscription1");

// Add the Service Bus Emulator UI
builder.AddAsbEmulatorUi("asb-ui", serviceBus);

var daprConfigPath = Path.Combine(Directory.GetCurrentDirectory(), ".dapr", "components");
var daprGlobalConfigFile = Path.Combine(daprConfigPath, "global.yaml");

// Component files are written here at startup; not tracked in git.
var daprGeneratedPath = Path.Combine(Directory.GetCurrentDirectory(), ".dapr", "generated");
Directory.CreateDirectory(daprGeneratedPath);

//// The CommunityToolkit Dapr integration launches daprd through its own lifecycle hook,
//// bypassing Aspire's env-var pipeline. Instead, generate pubsub.yaml on disk before
//// daprd reads it. The full connection string including 'UseDevelopmentEmulator=true'
//// is required so Dapr's Go Azure SDK connects without TLS to the local emulator
//// (azure-sdk-for-go/azservicebus v1.7.0+ supports the flag; Dapr 1.14+ ships with it).
builder.Eventing.Subscribe<BeforeResourceStartedEvent>(serviceBus.Resource, async (_, ct) =>
{
   var connectionString = await serviceBus.Resource.ConnectionStringExpression.GetValueAsync(ct);
   if (connectionString is null) return;

   await File.WriteAllTextAsync(
       Path.Combine(daprGeneratedPath, "pubsub.yaml"),
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
             value: "{connectionString}"
           - name: consumerID
             value: "subscription1"
           - name: disableEntityManagement
             value: "true"
       """,
       ct);
});

// Use the Redis that 'dapr init' provisions (localhost:6379).
// That container is always running when Dapr is installed: no TLS, no password.
// actorStateStore=true elects it as the actor state store; scoped to sub-app-id only
// so pub-app-id never tries to load it.
File.WriteAllText(
    Path.Combine(daprGeneratedPath, "statestore.yaml"),
    """
    apiVersion: dapr.io/v1alpha1
    kind: Component
    metadata:
      name: statestore
      namespace: default
    spec:
      type: state.redis
      version: v1
      metadata:
        - name: redisHost
          value: "localhost:6379"
        - name: actorStateStore
          value: "true"
    scopes:
      - sub-app-id
    """);

builder.AddProject<Projects.Pub>("pub")
    .WithDaprSidecar(sidecar =>
    {
        sidecar.WithOptions(new DaprSidecarOptions
        {
            AppId = "pub-app-id",
            AppPort = 8082,
            DaprGrpcPort = 50001,
            DaprHttpPort = 3500,
            MetricsPort = 9090,
            ResourcesPaths = [daprConfigPath, daprGeneratedPath],
            LogLevel = "Info",
            Config = daprGlobalConfigFile,
            EnableApiLogging = true
        });
    })
    .WaitFor(serviceBus);

builder.AddProject<Projects.Sub>("sub")
    .WithDaprSidecar(sidecar =>
    {
        sidecar.WithOptions(new DaprSidecarOptions
        {
            AppId = "sub-app-id",
            AppPort = 8083,
            DaprGrpcPort = 50002,
            DaprHttpPort = 3501,
            MetricsPort = 9091,
            ResourcesPaths = [daprConfigPath, daprGeneratedPath],
            PlacementHostAddress = "localhost:50005",               // can be other ports here, value on Windows 6050
            LogLevel = "Info",
            Config = daprGlobalConfigFile,
            EnableApiLogging = true
        });
    })
    .WaitFor(serviceBus);

builder.Build().Run();
