using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

var topic = serviceBus.AddServiceBusTopic("topic");
topic.AddServiceBusSubscription("subscription");

var daprPath = Path.Combine(Directory.GetCurrentDirectory(), ".dapr\\components");
var daprConfigPath = Path.Combine(daprPath, "global.yaml");

//var dapr = builder.AddDapr();

builder.AddProject<Projects.Pub>("pub")
           //.WithDaprSidecar(new DaprSidecarOptions
           //{
           //    AppId = "publisher",
           //    Config = ".\\.dapr\\components",
           //    DaprGrpcPort = 59004,
           //    DaprHttpPort = 59005,
           //    LogLevel = "Debug"
           //})
           .WithDaprSidecar(new DaprSidecarOptions
           {
               //Config = ".\\.dapr\\config.yaml", // Path to your Dapr config file (if any)
               //ComponentsPath = ".\\.dapr\\components", // Path to your components directory
               //Config = ".\\.dapr\\components",
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
    .WithEnvironment("AzureServiceBus__ConnectionString", "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;")
    .WaitFor(serviceBus);

builder.Build().Run();
