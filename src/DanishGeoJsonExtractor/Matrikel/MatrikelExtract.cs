using System.Globalization;
using DanishGeoJsonExtractor.Datafordeleren;
using Microsoft.Extensions.Logging;

namespace DanishGeoJsonExtractor.Matrikel;

internal sealed class MatrikelExtract
{
    private readonly ILogger<MatrikelExtract> _logger;
    private readonly DatafordelerExtractGeoJson _datafordelerExtractGeoJson;
    private readonly DatafordelerFileDownload _datafordelerFileDownload;

    public MatrikelExtract(
        DatafordelerExtractGeoJson datafordelerExtractGeoJson,
        ILogger<MatrikelExtract> logger,
        DatafordelerFileDownload datafordelerFileDownload)
    {
        _logger = logger;
        _datafordelerExtractGeoJson = datafordelerExtractGeoJson;
        _datafordelerFileDownload = datafordelerFileDownload;
    }

    public async Task StartAsync(
        Setting setting,
        CancellationToken cancellationToken = default)
    {
        const string register = "MAT";
        const string format = "gpkg";

        var allDataSets = setting.Matrikel!.Datasets.Select(x => x.Key).ToHashSet().AsReadOnly();
        var enabledDataSets = setting.StedNavn!.Datasets
            .Where(x => x.Value)
            .Select(x => x.Key)
            .ToList()
            .AsReadOnly();

        if (enabledDataSets.Count == 0)
        {
            _logger.LogInformation(
                $"No datasets enabled for StedNavn, so skips extraction.");
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

        var missingDataSets = allAvailableDatasets.Except(allDataSets).ToArray();
        if (missingDataSets.Length != 0)
        {
            _logger.LogWarning("The following datasets are missing from the settings: {MissingDataSets}.", String.Join(",", missingDataSets));
        }

        var configuredDataSetsNotExistExternally = allDataSets.Except(allAvailableDatasets).ToArray();
        if (configuredDataSetsNotExistExternally.Length != 0)
        {
            _logger.LogWarning("The configured dataset do not exist externally: {}", String.Join(",", configuredDataSetsNotExistExternally));
        }

        var enabledConfiguredDataSetsNotExistExternally = enabledDataSets.Except(allAvailableDatasets).ToArray();
        if (configuredDataSetsNotExistExternally.Length != 0)
        {
            var enabledConfiguredDataSetsNotExistExternallyText = String.Join(",", configuredDataSetsNotExistExternally);
            _logger.LogError("The enabled configured dataset do not exist externally: {}", String.Join(",", configuredDataSetsNotExistExternally));
            throw new ArgumentException($"The enabled configured dataset do not exist externally: {enabledConfiguredDataSetsNotExistExternallyText}");
        }

        var tasks = new List<Task>();

        foreach (var dataset in enabledDataSets)
        {
            var executeTask = _datafordelerExtractGeoJson.ExecuteDatasetDownloadProcessing(register, dataset, format, cancellationToken);
            tasks.Add(executeTask);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
