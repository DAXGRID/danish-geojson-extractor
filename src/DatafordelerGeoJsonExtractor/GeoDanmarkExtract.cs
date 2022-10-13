using DatafordelerGeoJsonExtractor.Dataforsyning;
using System.IO.Compression;

namespace DatafordelerGeoJsonExtractor;

internal static class GeoDanmarkExtract
{
    public static async Task StartAsync(
        Setting setting,
        CancellationToken cancellationToken)
    {
        var datasets = setting.GeoDanmark.Datasets.Where(x => x.Value);
        if (!datasets.Any())
        {
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
                $"{dataset.Key}.zip");

            var localPath = Path.Combine(setting.OutDirPath, $"{dataset.Key}.zip");

            return (remotePath: remotePath, localPath: localPath);
        });

        foreach (var download in downloads)
        {
            await ftpClient
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
                Path.Combine(Path.Combine(setting.OutDirPath, fileName, ".geojson")));

            await GeoJsonExtract
                .ExtractGeoJson(
                    workingDirectory: setting.OutDirPath,
                    outFileName: fileName,
                    inputFileName: $"{fileName}.gml",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Cleanup
            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, $"{fileName}.gml"));

            ExtractUtil.DeleteIfExists(
                Path.Combine(setting.OutDirPath, $"{fileName}.xsd"));
        }
    }
}
