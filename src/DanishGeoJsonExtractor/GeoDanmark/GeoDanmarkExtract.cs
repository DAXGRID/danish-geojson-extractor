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
            .GetEnabled(setting.GeoDanmark!.Datasets)
            .ToList()
            .AsReadOnly();

        if (datasets.Count == 0)
        {
            _logger.LogInformation(
                "No datasets enabled for GeoDanmark, so skips extraction.");
            return;
        }

        const string remoteRootPath = "/";
        const string folderStartName = "GeoDanmark60_GML_HF";

        List<(string remotePath, string localPath)>? downloads = null;
        using (var ftpClient = new DataforsyningFtpClient(setting.FtpSetting, _logger))
        {
            var ftpFiles = await ftpClient
                .DirectoriesInPathAsync(remoteRootPath, cancellationToken)
                .ConfigureAwait(false);

            var newestDirectory = ExtractUtil.NewestDirectory(folderStartName, ftpFiles);
            if (newestDirectory is null)
            {
                throw new FtpDirectoryNotFoundException(
                    $"The directory {folderStartName} does not exist on the FTP server.");
            }

            downloads = datasets.Select(dataset =>
            {
                var remotePath = Path.Combine(
                    remoteRootPath,
                    newestDirectory.Value.name,
                    "Tema",
                    $"{dataset}.zip");

                var localPath = Path.Combine(setting.OutDirPath, $"{dataset}.zip");

                return (remotePath: remotePath, localPath: localPath);
            }).ToList();
        }

        foreach (var download in downloads)
        {
            // We use multiple ftp clients because datafordeler might time it out.
#pragma warning disable CA2000 // Seems like the static analysis thinks this is not being disposed.
            using var localFtpClient = new DataforsyningFtpClient(setting.FtpSetting, _logger);
#pragma warning restore CA2000

            _logger.LogInformation("Starting download {FilePath}.", download.remotePath);
            await localFtpClient
                .RetryDownloadFileAsync(
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

            _logger.LogInformation(
                "Extracting geojson for {FileName}.",
                fileName);

            var extractArguments = GeoJsonExtract.BuildArguments(fileName, $"{fileName}.gml", null);
            _logger.LogDebug(
                "Executing {ExecuteableName} with {Arguments}.",
                GeoJsonExtract.ExecuteableName, extractArguments);

            await GeoJsonExtract
                .ExtractGeoJson(
                    workingDirectory: setting.OutDirPath,
                    arguments: extractArguments,
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
