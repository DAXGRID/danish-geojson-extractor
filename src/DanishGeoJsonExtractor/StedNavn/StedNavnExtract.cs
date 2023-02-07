using DanishGeoJsonExtractor.Dataforsyning;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DanishGeoJsonExtractor.StedNavn;

internal sealed class StedNavnExtract
{
    private readonly ILogger<StedNavnExtract> _logger;

    public StedNavnExtract(ILogger<StedNavnExtract> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(Setting setting, CancellationToken cancellationToken)
    {
        var datasets = ExtractUtil.GetEnabled(setting.StedNavn!.Datasets);
        if (!datasets.Any())
        {
            _logger.LogInformation(
                $"No datasets enabled for StedNavn, so skips extraction.");
            return;
        }

        using var ftpClient = new DataforsyningFtpClient(setting.FtpSetting);

        const string remoteRootPath = "/";
        const string fileNamePrefix = "DKstednavneBearbejdedeNohist_GML321";

        var ftpFiles = await ftpClient
            .FilesInPathAsync(remoteRootPath, cancellationToken)
            .ConfigureAwait(true);

        var newestFile = ExtractUtil.NewestFile(fileNamePrefix, ftpFiles);

        var remotePath = Path.Combine(
            remoteRootPath,
            newestFile.name);

        var localPath = Path.Combine(setting.OutDirPath, newestFile.name);

        // We use multiple ftp clients because datafordeler might time it out.
        using var localFtpClient = new DataforsyningFtpClient(setting.FtpSetting);

        _logger.LogInformation("Starting download {FilePath}", remotePath);
        await localFtpClient
            .DownloadFileAsync(
                remotePath,
                localPath,
                cancellationToken)
            .ConfigureAwait(false);

        var zipFileOutputPath = Path.Combine(
            setting.OutDirPath,
            Path.GetFileName(remotePath));

        ZipFile.ExtractToDirectory(zipFileOutputPath, setting.OutDirPath, true);

        // Cleanup zip file and metadata file
        ExtractUtil.DeleteIfExists(zipFileOutputPath);
        ExtractUtil.DeleteIfExists(
            Path.Combine(
                setting.OutDirPath,
                $"{Path.GetFileNameWithoutExtension(remotePath)}_Metadata.json"));

        foreach (var dataset in setting.StedNavn.Datasets)
        {
            var fileToProcessName = dataset.Key;
            var pathFileToProcess = Path.Combine(setting.OutDirPath, fileToProcessName);

            if (dataset.Value)
            {
                // Check if output files already exists, if they do delete them.
                ExtractUtil.DeleteIfExists(
                    Path.Combine(setting.OutDirPath, $"{fileToProcessName}.geojson"));

                _logger.LogInformation(
                    "Extracting geojson for {FileNameNoExtension}.",
                    fileToProcessName);

                var extractArguments = GeoJsonExtract.BuildArguments(
                    fileToProcessName,
                    $"{fileToProcessName}.gml", null);

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
            }

            // Delete output from zip, it is no longer needed.
            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, $"{fileToProcessName}.gml"));

            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, $"{fileToProcessName}.xsd"));
        }
    }
}
