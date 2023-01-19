using DanishGeoJsonExtractor.Dawa;
using DanishGeoJsonExtractor.GeoDanmark;
using DanishGeoJsonExtractor.Matrikel;
using DanishGeoJsonExtractor.StedNavn;
using Microsoft.Extensions.Logging;

namespace DanishGeoJsonExtractor;

internal sealed class StartUp
{
    private readonly ILogger<StartUp> _logger;
    private readonly Setting _setting;
    private readonly GeoDanmarkExtract _geoDanmarkExtract;
    private readonly MatrikelExtract _matrikelExtract;
    private readonly DawaExtract _dawaExtract;
    private readonly StedNavnExtract _stedNavnExtract;

    public StartUp(
        ILogger<StartUp> logger,
        Setting setting,
        GeoDanmarkExtract geoDanmarkExtract,
        MatrikelExtract matrikelExtract,
        DawaExtract dawaExtract,
        StedNavnExtract stedNavnExtract)
    {
        _setting = setting;
        _logger = logger;
        _geoDanmarkExtract = geoDanmarkExtract;
        _matrikelExtract = matrikelExtract;
        _dawaExtract = dawaExtract;
        _stedNavnExtract = stedNavnExtract;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting GeoJSON extraction.");

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

        _logger.LogInformation(
            "Starting processing {Name}.", nameof(_setting.StedNavn));
        await _stedNavnExtract
            .StartAsync(_setting, cancellationToken)
            .ConfigureAwait(false);
    }
}
