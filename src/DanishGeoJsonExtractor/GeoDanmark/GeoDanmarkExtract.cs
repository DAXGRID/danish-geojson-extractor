using DanishGeoJsonExtractor.Datafordeleren;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO.Compression;

namespace DanishGeoJsonExtractor.GeoDanmark;

internal sealed class GeoDanmarkExtract
{
    private readonly ILogger<GeoDanmarkExtract> _logger;
    private readonly DatafordelerExtractGeoJson _datafordelerExtractGeoJson;
    private readonly DatafordelerFileDownload _datafordelerFileDownload;

    public GeoDanmarkExtract(
        ILogger<GeoDanmarkExtract> logger,
        DatafordelerExtractGeoJson datafordelerExtractGeoJson,
        DatafordelerFileDownload datafordelerFileDownload)
    {
        _logger = logger;
        _datafordelerExtractGeoJson = datafordelerExtractGeoJson;
        _datafordelerFileDownload = datafordelerFileDownload;
    }

    public async Task StartAsync(Setting setting, CancellationToken cancellationToken)
    {
        const string register = "GEODKV";
        const string format = "gpkg";

        var allDataSets = setting.GeoDanmark!.Datasets.Select(x => x.Key).ToHashSet().AsReadOnly();
        var enabledDataSets = setting.GeoDanmark!.Datasets
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

        var allAvailableDatasets = (
            await _datafordelerFileDownload
            .LatestGenerationFileResourcesCurrentTotalDownloadAsync(format, register, cancellationToken)
            .ConfigureAwait(false))
            .DistinctBy(x => x.EntityName)
            .Select(x => x.EntityName.ToLower(CultureInfo.InvariantCulture))
            .ToHashSet()
            .AsReadOnly();

        await _datafordelerExtractGeoJson.DownloadProcessExtractGeoJson(
            register, format, allDataSets, enabledDataSets, cancellationToken).ConfigureAwait(false);
    }
}
