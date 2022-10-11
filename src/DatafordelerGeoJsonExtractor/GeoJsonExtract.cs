using System.Diagnostics;

namespace DatafordelerGeoJsonExtractor;

internal static class GeoJsonExtract
{
    public static async Task ExtractGeoJson(
        string workingDirectory,
        string outFileName,
        string inputFileName,
        string layerNames,
        CancellationToken cancellationToken = default)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ogr2ogr",
                Arguments = $"-f GeoJSONSeq {outFileName}.geojson {inputFileName} {layerNames}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };

        proc.Start();
        await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }
}
