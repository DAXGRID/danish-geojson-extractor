using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Compression;
using System.Net.Sockets;

namespace DanishGeoJsonExtractor.Datafordeleren;

internal sealed class DatafordelerExtractGeoJson
{
    private readonly DatafordelerFileDownload _datafordelerFileDownload;
    private readonly Setting _setting;
    private readonly ILogger<DatafordelerExtractGeoJson> _logger;

    public DatafordelerExtractGeoJson(DatafordelerFileDownload datafordelerFileDownload, Setting setting, ILogger<DatafordelerExtractGeoJson> logger)
    {
        _datafordelerFileDownload = datafordelerFileDownload;
        _setting = setting;
        _logger = logger;
    }

    public async Task DownloadProcessExtractGeoJson(
        string register, string format, ReadOnlySet<string> allDataSets, ReadOnlyCollection<string> enabledDataSets, CancellationToken cancellationToken)
    {
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
            var executeTask = ExecuteDatasetDownloadProcessing(register, dataset, format, cancellationToken);
            tasks.Add(executeTask);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteDatasetDownloadProcessing(string register, string dataset, string downloadFileFormat, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {Name}.", dataset);

        string? zipFileOutputPath = null;
        var downloadRetryCount = 0;
        while (zipFileOutputPath is null)
        {
            try
            {
                zipFileOutputPath = await _datafordelerFileDownload.DownloadAsync(register, dataset, downloadFileFormat, _setting.OutDirPath, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Downloading {Name} to {OutputFileName}.", dataset, zipFileOutputPath);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                downloadRetryCount++;
                if (downloadRetryCount == 10)
                {
                    _logger.LogError("Reached maximum of {Retries}, throwing: {Exception}", downloadRetryCount, ex);
                    throw;
                }

                _logger.LogWarning("Socket connection was reset by the server, retrying {RetryCount}.", downloadRetryCount);
            }
        }

        _logger.LogInformation("Extracting {Name} to output folder.", zipFileOutputPath);
        await ZipFile.ExtractToDirectoryAsync(zipFileOutputPath, _setting.OutDirPath, true, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleting {Name}, no longer needed.", zipFileOutputPath);
        if (File.Exists(zipFileOutputPath))
        {
            File.Delete(zipFileOutputPath);
        }

        var extractedFile = Path.Combine(
            _setting.OutDirPath,
            $"{Path.GetFileNameWithoutExtension(zipFileOutputPath)}.{downloadFileFormat}");

        _logger.LogInformation("Started processing {Name}", dataset);

        // Cleanup last extracted geojson file if it exists.
        var outputGeoJsonFileName = Path.Combine(_setting.OutDirPath, dataset, ".geojson");
        if (File.Exists(outputGeoJsonFileName))
        {
            File.Delete(outputGeoJsonFileName);
        }

        var extractArguments = GeoJsonExtract.BuildArguments(dataset, extractedFile, dataset);
        _logger.LogDebug(
                    "Executing {ExecuteableName} with {Arguments}.",
                    GeoJsonExtract.ExecuteableName,
                    extractArguments);

        await GeoJsonExtract
            .ExtractGeoJson(
                workingDirectory: _setting.OutDirPath,
                arguments: extractArguments,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (File.Exists(extractedFile))
        {
            File.Delete(extractedFile);
        }

        _logger.LogInformation("Finished processing {Name}", dataset);
    }
}
