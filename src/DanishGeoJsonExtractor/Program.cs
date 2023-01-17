using DanishGeoJsonExtractor.Dawa;
using DanishGeoJsonExtractor.GeoDanmark;
using DanishGeoJsonExtractor.Matrikel;
using DanishGeoJsonExtractor.StedNavn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.Text.Json;

namespace DanishGeoJsonExtractor;

internal static class Program
{
    public static async Task Main()
    {
        using var cancellationToken = new CancellationTokenSource();

        try
        {
            using var serviceProvider = BuildServiceProvider();
            var start = serviceProvider.GetService<StartUp>() ??
                throw new InvalidOperationException(
                    $"Could find service '{nameof(StartUp)}'.");

            await start.StartAsync(cancellationToken.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            cancellationToken.Cancel();
            throw;
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        const string APP_SETTINGS_FILE_NAME = "appsettings.json";

        var loggingConfiguration = new ConfigurationBuilder()
            .AddJsonFile(APP_SETTINGS_FILE_NAME)
            .Build();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .ReadFrom.Configuration(loggingConfiguration)
            .Enrich.FromLogContext()
            .CreateLogger();

        var settingsJson = JsonDocument.Parse(File.ReadAllText("appsettings.json"))
            .RootElement.GetProperty("settings").ToString();

        var setting = JsonSerializer.Deserialize<Setting>(settingsJson) ??
            throw new ArgumentException("Could not deserialize appsettings into settings.");

        return new ServiceCollection()
            .AddLogging(logging =>
            {
                logging.AddSerilog(logger, true);
            })
            .AddSingleton<StartUp>()
            .AddSingleton<Setting>(setting)
            .AddSingleton<GeoDanmarkExtract>()
            .AddSingleton<MatrikelExtract>()
            .AddSingleton<DawaExtract>()
            .AddSingleton<StedNavnExtract>()
            .BuildServiceProvider();
    }
}
