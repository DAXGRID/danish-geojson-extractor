using DanishGeoJsonExtractor.Datafordeleren;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO.Compression;

namespace DanishGeoJsonExtractor.Dar;

internal sealed class DarExtract
{
    private readonly ILogger<DarExtract> _logger;
    private readonly DatafordelerExtractGeoJson _datafordelerExtractGeoJson;
    private readonly DatafordelerFileDownload _datafordelerFileDownload;

    public DarExtract(
        ILogger<DarExtract> logger,
        DatafordelerExtractGeoJson datafordelerExtractGeoJson,
        DatafordelerFileDownload datafordelerFileDownload)
    {
        _logger = logger;
        _datafordelerExtractGeoJson = datafordelerExtractGeoJson;
        _datafordelerFileDownload = datafordelerFileDownload;
    }

    public async Task StartAsync(Setting setting, CancellationToken cancellationToken)
    {
        const string register = "DAR";
        const string format = "csv";

        var allDataSets = setting.Dar!.Datasets.Select(x => x.Key).ToHashSet().AsReadOnly();
        var enabledDataSets = setting.Dar!.Datasets
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
