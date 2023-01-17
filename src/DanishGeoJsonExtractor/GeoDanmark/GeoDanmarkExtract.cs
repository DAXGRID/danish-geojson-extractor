using DanishGeoJsonExtractor.Dataforsyning;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DanishGeoJsonExtractor.GeoDanmark;

internal sealed class GeoDanmarkExtract
{
    private readonly ILogger<GeoDanmarkExtract> _logger;

    public GeoDanmarkExtract(ILogger<GeoDanmarkExtract> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(Setting setting, CancellationToken cancellationToken)
    {
        var datasets = ExtractUtil
            .GetEnabled(setting.GeoDanmark.Datasets)
            .ToList()
            .AsReadOnly();

        if (!datasets.Any())
        {
            _logger.LogInformation(
                "No datasets enabled for GeoDanmark, so skips extraction.");
            return;
        }

        using var ftpClient = new DataforsyningFtpClient(setting.FtpSetting);

        const string remoteRootPath = "/";
        const string folderStartName = "GeoDanmark60_GML";

        var ftpFiles = await ftpClient
            .DirectoriesInPathAsync(remoteRootPath, cancellationToken)
            .ConfigureAwait(false);

        var newestDirectory = ExtractUtil.NewestDirectory(folderStartName, ftpFiles);

        var downloads = datasets.Select(dataset =>
        {
            var remotePath = Path.Combine(
                remoteRootPath,
                newestDirectory.name,
                "Tema",
                $"{dataset}.zip");

            var localPath = Path.Combine(setting.OutDirPath, $"{dataset}.zip");

            return (remotePath: remotePath, localPath: localPath);
        });

        foreach (var download in downloads)
        {
            // We use multiple ftp clients because datafordeler might time it out.
            using var localFtpClient = new DataforsyningFtpClient(setting.FtpSetting);

            _logger.LogInformation("Starting download {FilePath}", download.remotePath);
            await localFtpClient
                .DownloadFileAsync(
                    download.remotePath,
                    download.localPath,
                    cancellationToken)
                .ConfigureAwait(false);

            var zipFileOutputPath = Path.Combine(
                setting.OutDirPath,
                Path.GetFileName(download.remotePath));

            ZipFile.ExtractToDirectory(zipFileOutputPath, setting.OutDirPath, true);
            File.Delete(zipFileOutputPath);

            var fileName = Path.GetFileNameWithoutExtension(download.remotePath);

            // Check if output files already exists, if they do delete them.
            ExtractUtil.DeleteIfExists(
                Path.Combine(Path.Combine(setting.OutDirPath, $"{fileName}.geojson")));

            _logger.LogInformation("Extracting geojson for {FileName}", fileName);
            await GeoJsonExtract
                .ExtractGeoJson(
                    workingDirectory: setting.OutDirPath,
                    outFileName: fileName,
                    inputFileName: $"{fileName}.gml",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Delete output from zip, it is no longer needed.
            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, $"{fileName}.gml"));

            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, $"{fileName}.xsd"));
        }
    }
}
