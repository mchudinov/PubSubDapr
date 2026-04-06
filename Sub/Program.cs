using Dapr.Actors;
using Dapr.Actors.Client;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Sub;

public class Program
{
    public static void Main(string[] args)
    {
        Serilog.Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Default", LogEventLevel.Debug)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateBootstrapLogger();

        SelfLog.Enable(Console.Error);

        try
        {
            Serilog.Log.Logger.Information("Subscriber is running");
            Serilog.Log.Logger.Debug($".NET Version: {Environment.Version}");
            Serilog.Log.Logger.Debug("► Environment variables");
            Environment.GetEnvironmentVariables().OutputEnvironmentVariables();

            var enviroment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{enviroment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            var settings = configuration.GetRequiredSection("Settings").Get<Settings>() ?? throw new InvalidOperationException("Settings configuration section is missing or invalid.");

            Serilog.Log.Logger.Information("► Final configuration");
            configuration.AllConfigurationKeys().LogStrings();

            var builder = WebApplication.CreateBuilder(args);

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);

            builder.AddServiceDefaults();
            builder.Services.AddActors(options =>
            {
                options.Actors.RegisterActor<MessageHandlerActor>();
            });
            builder.Services.AddSingleton<SubscriberWorker>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<SubscriberWorker>());

            var app = builder.Build();
            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseCloudEvents();
            app.MapSubscribeHandler();
            app.MapActorsHandlers();

            app.MapPost("/topic1", async (HttpRequest request, IActorProxyFactory proxyFactory) =>
                {
                    using var reader = new StreamReader(request.Body);
                    var message = await reader.ReadToEndAsync();
                    var actor = proxyFactory.CreateActorProxy<IMessageHandlerActor>(new ActorId(Guid.NewGuid().ToString()), "MessageHandlerActor");
                    await actor.HandleMessageAsync(message);
                    return Results.Ok();
                })
                .WithTopic("servicebus_pubsub", "topic1");

            app.MapGet("/", () => "Subscriber");

            app.Run();
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal($"Subscriber process terminated unexpectedly. Error: {ex.Message}");
        }
        finally
        {
            Serilog.Log.Information("Shut down complete.");
            Serilog.Log.CloseAndFlush();
        }
    }
}
