using DanishGeoJsonExtractor.Dataforsyning;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DanishGeoJsonExtractor.Matrikel;

internal sealed class MatrikelExtract
{
    private readonly ILogger<MatrikelExtract> _logger;

    public MatrikelExtract(ILogger<MatrikelExtract> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(
        Setting setting,
        CancellationToken cancellationToken = default)
    {
        var datasets = ExtractUtil
            .GetEnabled(setting.Matrikel.Datasets)
            .ToList()
            .AsReadOnly();

        // If none is enabled we just return since there is nothing to process.
        if (!datasets.Any())
        {
            _logger.LogInformation("Skipping, no datasets enabled for Matrikel");
            return;
        }

        const string remoteRootPath = "/";
        const string folderStartName = "MatrikelGeometriGaeldendeDKComplete_gpkg";
        const string fileName = "MatrikelGeometriGaeldendeDKComplete.zip";
        const string outputFileName = "MatrikelGeometriGaeldendeDKComplete.gpkg";

        using var ftpClient = new DataforsyningFtpClient(setting.FtpSetting);

        var ftpFiles = await ftpClient
            .DirectoriesInPathAsync(remoteRootPath, cancellationToken)
            .ConfigureAwait(false);

        var newestFolder = ExtractUtil.NewestDirectory(folderStartName, ftpFiles);

        var zipFileOutputPath = Path.Combine(setting.OutDirPath, fileName);

        _logger.LogInformation("Starting downloading matrikel data.");
        await ftpClient
            .DownloadFileAsync(
                remotePath: Path.Combine(newestFolder.name, fileName),
                localPath: zipFileOutputPath,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Extracting matrikel to output folder.");
        ZipFile.ExtractToDirectory(zipFileOutputPath, setting.OutDirPath);

        ExtractUtil.DeleteIfExists(zipFileOutputPath);

        var extractedFile = Path.Combine(
            setting.OutDirPath,
            outputFileName);

        foreach (var dataset in datasets)
        {
            _logger.LogInformation("Processing {Name}", dataset);

            // Cleanup last extracted geojson file if it exists.
            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, dataset, ".geojson"));

            await GeoJsonExtract
                .ExtractGeoJson(
                    workingDirectory: setting.OutDirPath,
                    outFileName: dataset,
                    inputFileName: extractedFile,
                    layerNames: dataset,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        // Delete output from zip, it is no longer needed.
        ExtractUtil.DeleteIfExists(extractedFile);
    }
}
