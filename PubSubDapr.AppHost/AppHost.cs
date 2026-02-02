var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("bus")
    .RunAsEmulator();

builder.AddProject<Projects.Pub>("pub")
    .WithDaprSidecar();

builder.Build().Run();
