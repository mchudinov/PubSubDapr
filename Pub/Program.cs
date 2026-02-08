using Serilog;
using Serilog.Debugging;
using Serilog.Events;

namespace Pub;

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
            Serilog.Log.Logger.Information("Overlege worker is running");
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
            builder.Services.AddDaprClient();
            builder.Services.AddHostedService<PublisherWorker>();

            var app = builder.Build();
            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.Run();
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal($"Overlege worker process terminated unexpectedly. Error: {ex.Message}");
        }
        finally
        {
            Serilog.Log.Information("Shut down complete.");
            Serilog.Log.CloseAndFlush();
        }
    }
}
