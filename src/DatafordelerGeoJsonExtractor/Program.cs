using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace DatafordelerGeoJsonExtractor;

internal static class Program
{
    public static async Task Main()
    {
        // TODO this is some BS, should parse arguments from stdin.
        var setting = new Setting(
            ftpSetting: new FtpSetting("", "", ""));

        using var serviceProvider = BuildServiceProvider(setting);

        using var cancellationToken = new CancellationTokenSource();

        await Task.CompletedTask.ConfigureAwait(false);

        await MatrikelExtract
            .StartAsync(setting.FtpSetting)
            .ConfigureAwait(false);
    }

    private static ServiceProvider BuildServiceProvider(Setting setting)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        return new ServiceCollection()
            .AddLogging(logging =>
            {
                logging.AddSerilog(logger, true);
            })
            .AddSingleton<Setting>(setting)
            .BuildServiceProvider();
    }
}
