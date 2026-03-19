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
    .WithDaprSidecar(new DaprSidecarOptions
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
    })    
    .WaitFor(serviceBus);

builder.Build().Run();
