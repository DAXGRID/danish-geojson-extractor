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
            .GetEnabled(setting.Matrikel!.Datasets)
            .ToList()
            .AsReadOnly();

        // If none is enabled we just return since there is nothing to process.
        if (datasets.Count == 0)
        {
            _logger.LogInformation("Skipping, no datasets enabled for Matrikel");
            return;
        }

        const string remoteRootPath = "/";
        const string folderStartName = "MAT_Kortdata_Gaeldende_DK_Complete_GPKG_";
        const string fileName = "GPKG.zip";
        const string outputFileName = "MATkortdataGaeldendeDKComplete.gpkg";

        using var ftpClient = new DataforsyningFtpClient(setting.FtpSetting, _logger);

        var ftpFiles = await ftpClient
            .DirectoriesInPathAsync(remoteRootPath, cancellationToken)
            .ConfigureAwait(false);

        var newestFolder = ExtractUtil.NewestDirectory(folderStartName, ftpFiles);
        if (newestFolder is null)
        {
            throw new FtpDirectoryNotFoundException(
                $"The directory {folderStartName} does not exist on the FTP server.");
        }

        var zipFileOutputPath = Path.Combine(setting.OutDirPath, fileName);

        _logger.LogInformation("Starting downloading matrikel data.");
        await ftpClient
            .RetryDownloadFileAsync(
                remotePath: Path.Combine(newestFolder.Value.name, fileName),
                localPath: zipFileOutputPath,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Extracting matrikel to output folder.");
        ZipFile.ExtractToDirectory(zipFileOutputPath, setting.OutDirPath, true);

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
        }

        // Delete output from zip, it is no longer needed.
        ExtractUtil.DeleteIfExists(extractedFile);
    }
}
