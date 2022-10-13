using DatafordelerGeoJsonExtractor.GeoDanmark;
using DatafordelerGeoJsonExtractor.Matrikel;
using Microsoft.Extensions.Logging;

namespace DatafordelerGeoJsonExtractor;

internal sealed class StartUp
{
    private readonly Setting _setting;
    private readonly ILogger<StartUp> _logger;
    private readonly GeoDanmarkExtract _geoDanmarkExtract;
    private readonly MatrikelExtract _matrikelExtract;

    public StartUp(
        Setting setting,
        ILogger<StartUp> logger,
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
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);
            throw;
        }
    }
}
