var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();

builder.AddProject<Projects.Pub>("pub");

builder.Build().Run();
