using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.AspNetCore.Components.Rendering;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();
var topic = serviceBus.AddServiceBusTopic("topic");
topic.AddServiceBusSubscription("subscription");

var dapr = builder.AddDapr();

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
               Config = ".\\.dapr\\components",
               AppId = "my-app-id",
               DaprGrpcPort = 50001,
               DaprHttpPort = 3500,
               MetricsPort = 9090
           })
    .WithEnvironment("AzureServiceBus__ConnectionString", "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;")
    .WaitFor(serviceBus);

builder.Build().Run();
