using DanishGeoJsonExtractor.Datafordeleren;
using DanishGeoJsonExtractor.Dataforsyning;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Sockets;

namespace DanishGeoJsonExtractor.Matrikel;

internal sealed class MatrikelExtract
{
    private readonly ILogger<MatrikelExtract> _logger;
    private readonly DatafordelerFileDownload _datafordelerFileDownload;

    public MatrikelExtract(DatafordelerFileDownload datafordelerFileDownload, ILogger<MatrikelExtract> logger)
    {
        _logger = logger;
        _datafordelerFileDownload = datafordelerFileDownload;
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
            var executeTask = ExecuteDatasetDownloadProcessing(setting, dataset, cancellationToken);
            tasks.Add(executeTask);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteDatasetDownloadProcessing(Setting setting, string dataset, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {Name}.", dataset);

        string? zipFileOutputPath = null;
        var downloadRetryCount = 0;
        while (zipFileOutputPath is null)
        {
            try
            {
                zipFileOutputPath = await _datafordelerFileDownload.DownloadAsync("MAT", dataset, "gpkg", setting.OutDirPath, cancellationToken).ConfigureAwait(false);
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
        await ZipFile.ExtractToDirectoryAsync(zipFileOutputPath, setting.OutDirPath, true, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleting {Name}, no longer needed.", zipFileOutputPath);
        ExtractUtil.DeleteIfExists(zipFileOutputPath);

        var extractedFile = Path.Combine(
            setting.OutDirPath,
            $"{Path.GetFileNameWithoutExtension(zipFileOutputPath)}.gpkg");

        _logger.LogInformation("Started processing {Name}", dataset);

        // Cleanup last extracted geojson file if it exists.
        ExtractUtil.DeleteIfExists(
            Path.Combine(setting.OutDirPath, dataset, ".geojson"));

        var extractArguments = GeoJsonExtract.BuildArguments(dataset, extractedFile, dataset);
        _logger.LogDebug(
                    "Executing {ExecuteableName} with {Arguments}.",
                    GeoJsonExtract.ExecuteableName,
                    extractArguments);

        await GeoJsonExtract
            .ExtractGeoJson(
                workingDirectory: setting.OutDirPath,
                arguments: extractArguments,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ExtractUtil.DeleteIfExists(extractedFile);

        _logger.LogInformation("Finished processing {Name}", dataset);
    }
}
