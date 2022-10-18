using DanishGeoJsonExtractor.Dawa;
using DanishGeoJsonExtractor.GeoDanmark;
using DanishGeoJsonExtractor.Matrikel;
using Microsoft.Extensions.Logging;

namespace DanishGeoJsonExtractor;

internal sealed class StartUp
{
    private readonly ILogger<StartUp> _logger;
    private readonly Setting _setting;
    private readonly GeoDanmarkExtract _geoDanmarkExtract;
    private readonly MatrikelExtract _matrikelExtract;
    private readonly DawaExtract _dawaExtract;

    public StartUp(
        ILogger<StartUp> logger,
        Setting setting,
        GeoDanmarkExtract geoDanmarkExtract,
        MatrikelExtract matrikelExtract,
        DawaExtract dawaExtract)
    {
        _setting = setting;
        _logger = logger;
        _geoDanmarkExtract = geoDanmarkExtract;
        _matrikelExtract = matrikelExtract;
        _dawaExtract = dawaExtract;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting GeoJSON extraction.");

        try
        {
            _logger.LogInformation(
                "Starting processing {Name}.", nameof(_setting.Matrikel));
            await _matrikelExtract
                .StartAsync(_setting, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Starting processing {Name}.", nameof(_setting.GeoDanmark));
            await _geoDanmarkExtract
                .StartAsync(_setting, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Starting processing {Name}.", nameof(_setting.Dawa));
            await _dawaExtract
                .StartAsync(_setting, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);
            throw;
        }
    }
}
