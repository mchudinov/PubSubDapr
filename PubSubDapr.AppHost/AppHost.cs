var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

//serviceBus.ExposeConnectionString("AzureServiceBus:ConnectionString");

builder.AddProject<Projects.Pub>("pub")
    .WithDaprSidecar();

builder.Build().Run();
