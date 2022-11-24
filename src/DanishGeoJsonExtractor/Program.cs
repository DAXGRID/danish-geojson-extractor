using DanishGeoJsonExtractor.Dawa;
using DanishGeoJsonExtractor.GeoDanmark;
using DanishGeoJsonExtractor.Matrikel;
using DanishGeoJsonExtractor.StedNavn;
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
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console()
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
