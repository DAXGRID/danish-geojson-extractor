using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text.Json;

namespace DatafordelerGeoJsonExtractor;

internal static class Program
{
    public static async Task Main()
    {
        using var serviceProvider = BuildServiceProvider();

        var setting = serviceProvider.GetService<Setting>();

        if (setting is null)
        {
            throw new InvalidOperationException("Could not get setting.");
        }

        using var cancellationToken = new CancellationTokenSource();

        await MatrikelExtract
            .StartAsync(setting, cancellationToken.Token)
            .ConfigureAwait(false);

        await GeoDanmarkExtract
            .StartAsync(setting, cancellationToken.Token)
            .ConfigureAwait(false);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
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
            .AddSingleton<Setting>(setting)
            .BuildServiceProvider();
    }
}
