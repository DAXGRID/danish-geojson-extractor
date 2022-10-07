using DatafordelerGeoJsonExtractor.Dataforsyning;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace DatafordelerGeoJsonExtractor;

internal static class MatrikelExtract
{
    public static async Task StartAsync(
        FtpSetting ftpSetting,
        CancellationToken cancellationToken = default)
    {
        const string outputFolder = "/home/notation/datafordeler/";
        const string remoteRootPath = "/";
        const string folderStartName = "MatrikelGeometriGaeldendeDKComplete_gpkg";
        const string fileName = "MatrikelGeometriGaeldendeDKComplete.zip";

        var ftpClient = new DataforsyningFtpClient(ftpSetting);

        var ftpFiles = await ftpClient
            .DirectoriesInPathAsync(remoteRootPath, cancellationToken)
            .ConfigureAwait(false);

        var newestFolder = ftpFiles
            .Where(x => x.name.StartsWith(folderStartName, true, CultureInfo.InvariantCulture))
            .OrderBy(x => x.created)
            .First();

        var zipFileOutputPath = Path.Combine(outputFolder, fileName);

        await ftpClient
            .DownloadFileAsync(
                remotePath: Path.Combine(newestFolder.name, fileName),
                localPath: zipFileOutputPath,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ZipFile.ExtractToDirectory(zipFileOutputPath, outputFolder);
        File.Delete(zipFileOutputPath);

        var extractedFile = Path.Combine(outputFolder, "MatrikelGeometriGaeldendeDKComplete.gpkg");
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ogr2ogr",
                Arguments = $"-f GeoJSONSeq matrikel.geojson {extractedFile} jordstykke",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = outputFolder
            }
        };

        proc.Start();
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        File.Delete(extractedFile);
    }
}
