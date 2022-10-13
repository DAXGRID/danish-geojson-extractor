using DatafordelerGeoJsonExtractor.GeoDanmark;
using DatafordelerGeoJsonExtractor.Matrikel;
using Microsoft.Extensions.Logging;

namespace DatafordelerGeoJsonExtractor;

internal sealed class Start
{
    private readonly Setting _setting;
    private readonly ILogger<Start> _logger;
    private readonly GeoDanmarkExtract _geoDanmarkExtract;
    private readonly MatrikelExtract _matrikelExtract;

    public Start(
        Setting setting,
        ILogger<Start> logger,
        GeoDanmarkExtract geoDanmarkExtract,
        MatrikelExtract matrikelExtract)
    {
        _setting = setting;
        _logger = logger;
        _geoDanmarkExtract = geoDanmarkExtract;
        _matrikelExtract = matrikelExtract;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting GeoJSON extraction.");

        _logger.LogInformation("Starting processing matrikel.");
        await _matrikelExtract
            .StartAsync(_setting, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Starting processing GeoDanmark.");
        await _geoDanmarkExtract
            .StartAsync(_setting, cancellationToken)
            .ConfigureAwait(false);
    }
}
