using DatafordelerGeoJsonExtractor.GeoDanmark;
using DatafordelerGeoJsonExtractor.Matrikel;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.Text.Json;

namespace DatafordelerGeoJsonExtractor;

internal static class Program
{
    public static async Task Main()
    {
        using var serviceProvider = BuildServiceProvider();

        var start = serviceProvider.GetService<Start>() ??
            throw new InvalidOperationException($"Could find service '{nameof(Start)}'.");

        using var cancellationToken = new CancellationTokenSource();
        await start.StartAsync(cancellationToken.Token).ConfigureAwait(false);
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
            .AddSingleton<Start>()
            .AddSingleton<Setting>(setting)
            .AddSingleton<GeoDanmarkExtract>()
            .AddSingleton<MatrikelExtract>()
            .BuildServiceProvider();
    }
}
