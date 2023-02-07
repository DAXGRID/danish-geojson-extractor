using System.Diagnostics;

namespace DanishGeoJsonExtractor;

public class GeoJsonExtractFailedException : Exception
{
    public GeoJsonExtractFailedException() { }
    public GeoJsonExtractFailedException(string message) { }
    public GeoJsonExtractFailedException(string message, Exception innerException) { }
}

public class GeoJsonExtractProcessCouldNotBeStartedException : Exception
{
    public GeoJsonExtractProcessCouldNotBeStartedException() { }
    public GeoJsonExtractProcessCouldNotBeStartedException(string message) { }
    public GeoJsonExtractProcessCouldNotBeStartedException(string message, Exception innerException) { }
}

internal static class GeoJsonExtract
{
    public const string ExecuteableName = "ogr2ogr";

    public static string BuildArguments(
        string outFileName,
        string inputFileName,
        string? layerNames = null)
    {
        return $"-f GeoJSON \"{outFileName}.geojson\" \"{inputFileName}\" {layerNames}";
    }

    public static async Task ExtractGeoJson(
        string workingDirectory,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ExecuteableName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            }
        };

        if (!proc.Start())
        {
            throw new GeoJsonExtractProcessCouldNotBeStartedException(
                $"The geojson extract process with arguments {arguments} could not be started.");
        }

        await proc
            .WaitForExitAsync(cancellationToken)
            .ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            var errorMessage = await proc.StandardError
                .ReadToEndAsync()
                .ConfigureAwait(false);

            throw new GeoJsonExtractProcessCouldNotBeStartedException($"{errorMessage}");
        }
    }
}
