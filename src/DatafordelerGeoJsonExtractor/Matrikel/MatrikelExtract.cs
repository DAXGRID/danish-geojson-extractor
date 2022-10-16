using DatafordelerGeoJsonExtractor.Dataforsyning;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DatafordelerGeoJsonExtractor.Matrikel;

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
        // If none is enabled we just return since there is nothing to process.
        if (!setting.Matrikel.Datasets.Where(x => x.Value).Any())
        {
            _logger.LogInformation(
                "No datasets enabled for Matrikel, so skips extraction.");
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

        foreach (var dataset in setting.Matrikel.Datasets.Where(x => x.Value))
        {
            _logger.LogInformation("Processing {Name}", dataset.Key);

            // Cleanup last extracted geojson file if it exists.
            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, dataset.Key, ".geojson"));

            await GeoJsonExtract
                .ExtractGeoJson(
                    workingDirectory: setting.OutDirPath,
                    outFileName: dataset.Key,
                    inputFileName: extractedFile,
                    layerNames: dataset.Key,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        // Delete output from zip, it is no longer needed.
        ExtractUtil.DeleteIfExists(extractedFile);
    }
}
