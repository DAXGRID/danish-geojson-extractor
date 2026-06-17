using Microsoft.Extensions.Logging;
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

    public async Task ExecuteDatasetDownloadProcessing(string register, string dataset, string downloadFileFormat, CancellationToken cancellationToken)
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
            ExtractUtil.DeleteIfExists(zipFileOutputPath);

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
