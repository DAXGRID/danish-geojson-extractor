using DanishGeoJsonExtractor.Datafordeleren;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO.Compression;

namespace DanishGeoJsonExtractor.Dagi;

internal sealed class DagiExtract
{
    private readonly ILogger<DagiExtract> _logger;
    private readonly DatafordelerExtractGeoJson _datafordelerExtractGeoJson;
    private readonly DatafordelerFileDownload _datafordelerFileDownload;

    public DagiExtract(
        ILogger<DagiExtract> logger,
        DatafordelerExtractGeoJson datafordelerExtractGeoJson,
        DatafordelerFileDownload datafordelerFileDownload)
    {
        _logger = logger;
        _datafordelerExtractGeoJson = datafordelerExtractGeoJson;
        _datafordelerFileDownload = datafordelerFileDownload;
    }

    public async Task StartAsync(Setting setting, CancellationToken cancellationToken)
    {
        const string register = "DAGI";
        const string format = "gpkg";

        var allDataSets = setting.Dagi!.Datasets.Select(x => x.Key).ToHashSet().AsReadOnly();
        var enabledDataSets = setting.Dagi!.Datasets
            .Where(x => x.Value)
            .Select(x => x.Key)
            .ToList()
            .AsReadOnly();

        if (enabledDataSets.Count == 0)
        {
            _logger.LogInformation(
                $"No datasets enabled for GeoDanmark, so skips extraction.");
            return;
        }

        await _datafordelerExtractGeoJson.DownloadProcessExtractGeoJson(
            register, format, allDataSets, enabledDataSets, cancellationToken).ConfigureAwait(false);
    }
}
