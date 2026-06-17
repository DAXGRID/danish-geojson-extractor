using DanishGeoJsonExtractor.Datafordeleren;
using DanishGeoJsonExtractor.Dataforsyning;
using Microsoft.Extensions.Logging;

namespace DanishGeoJsonExtractor.Matrikel;

internal sealed class MatrikelExtract
{
    private readonly ILogger<MatrikelExtract> _logger;
    private readonly DatafordelerExtractGeoJson _datafordelerExtractGeoJson;

    public MatrikelExtract(DatafordelerExtractGeoJson datafordelerExtractGeoJson, ILogger<MatrikelExtract> logger)
    {
        _logger = logger;
        _datafordelerExtractGeoJson = datafordelerExtractGeoJson;
    }

    public async Task StartAsync(
        Setting setting,
        CancellationToken cancellationToken = default)
    {
        var datasets = ExtractUtil
            .GetEnabled(setting.Matrikel!.Datasets)
            .ToList()
            .AsReadOnly();

        // If none is enabled we just return since there is nothing to process.
        if (datasets.Count == 0)
        {
            _logger.LogInformation("Skipping, no datasets enabled for Matrikel");
            return;
        }

        var tasks = new List<Task>();

        foreach (var dataset in datasets)
        {
            var executeTask = _datafordelerExtractGeoJson.ExecuteDatasetDownloadProcessing(dataset, "gpkg", cancellationToken);
            tasks.Add(executeTask);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
