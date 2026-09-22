using DanishGeoJsonExtractor.Dagi;
using DanishGeoJsonExtractor.Dar;
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
    private readonly DarExtract _darExtract;
    private readonly DagiExtract _dagiExtract;

    public StartUp(
        ILogger<StartUp> logger,
        Setting setting,
        GeoDanmarkExtract geoDanmarkExtract,
        MatrikelExtract matrikelExtract,
        DawaExtract dawaExtract,
        StedNavnExtract stedNavnExtract,
        DarExtract darExtract,
        DagiExtract dagiExtract)
    {
        _setting = setting;
        _logger = logger;
        _geoDanmarkExtract = geoDanmarkExtract;
        _matrikelExtract = matrikelExtract;
        _dawaExtract = dawaExtract;
        _stedNavnExtract = stedNavnExtract;
        _darExtract = darExtract;
        _dagiExtract = dagiExtract;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting GeoJSON extraction.");

        var tasks = new List<Task>();

        if (_setting.Matrikel is not null)
        {
            _logger.LogInformation(
                "Starting processing {Name}.", nameof(_setting.Matrikel));

            var matrikelExtractTask = _matrikelExtract.StartAsync(_setting, cancellationToken);

            tasks.Add(matrikelExtractTask);
        }

        if (_setting.GeoDanmark is not null)
        {
            _logger.LogInformation("Starting processing {Name}.", nameof(_setting.GeoDanmark));

            var geoDanmarkExtractTask = _geoDanmarkExtract.StartAsync(_setting, cancellationToken);

            tasks.Add(geoDanmarkExtractTask);
        }

        if (_setting.Dawa is not null)
        {
            _logger.LogInformation("Starting processing {Name}.", nameof(_setting.Dawa));

            var dawaExtractTask = _dawaExtract.StartAsync(_setting, cancellationToken);

            tasks.Add(dawaExtractTask);
        }

        if (_setting.StedNavn is not null)
        {
            _logger.LogInformation("Starting processing {Name}.", nameof(_setting.StedNavn));

            var stedNavnExtractTask = _stedNavnExtract
                .StartAsync(_setting, cancellationToken);

            tasks.Add(stedNavnExtractTask);
        }

        if (_setting.Dar is not null)
        {
            _logger.LogInformation("Starting processing {Name}.", nameof(_setting.Dar));

            var darExtractTask = _darExtract
                .StartAsync(_setting, cancellationToken);

            tasks.Add(darExtractTask);
        }

        if (_setting.Dagi is not null)
        {
            _logger.LogInformation("Starting processing {Name}.", nameof(_setting.Dagi));

            var dagiExtractTask = _dagiExtract
                .StartAsync(_setting, cancellationToken);

            tasks.Add(dagiExtractTask);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
