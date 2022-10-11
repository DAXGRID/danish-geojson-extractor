using DatafordelerGeoJsonExtractor.Dataforsyning;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace DatafordelerGeoJsonExtractor;

internal static class MatrikelExtract
{
    public static async Task StartAsync(
        Setting setting,
        CancellationToken cancellationToken = default)
    {
        // If none is enabled we just return since there is nothing to process.
        if (setting.Matrikel.Datasets.Where(x => x.Value).Any())
        {
            return;
        }

        const string remoteRootPath = "/";
        const string folderStartName = "MatrikelGeometriGaeldendeDKComplete_gpkg";
        const string fileName = "MatrikelGeometriGaeldendeDKComplete.zip";
        const string outputFileName = "MatrikelGeometriGaeldendeDKComplete.gpkg";

        var ftpClient = new DataforsyningFtpClient(setting.FtpSetting);

        var ftpFiles = await ftpClient
            .DirectoriesInPathAsync(remoteRootPath, cancellationToken)
            .ConfigureAwait(false);

        var newestFolder = ftpFiles
            .Where(x => x.name.StartsWith(folderStartName, true, CultureInfo.InvariantCulture))
            .OrderBy(x => x.created)
            .First();

        var zipFileOutputPath = Path.Combine(setting.OutDirPath, fileName);

        await ftpClient
            .DownloadFileAsync(
                remotePath: Path.Combine(newestFolder.name, fileName),
                localPath: zipFileOutputPath,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ZipFile.ExtractToDirectory(zipFileOutputPath, setting.OutDirPath);
        File.Delete(zipFileOutputPath);

        var extractedFile = Path.Combine(
            setting.OutDirPath,
            outputFileName);

        foreach (var dataset in setting.Matrikel.Datasets.Where(x => x.Value))
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ogr2ogr",
                    Arguments = $"-f GeoJSONSeq {dataset.Key}.geojson {extractedFile} {dataset.Key}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = setting.OutDirPath
                }
            };

            proc.Start();
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Delete(extractedFile);
    }
}
