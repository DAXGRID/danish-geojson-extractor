using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DanishGeoJsonExtractor.Datafordeleren;

internal sealed class DatafordelerFileDownload : IDisposable
{
    private const string _baseAddressApi = "https://api.datafordeler.dk";

    private readonly HttpClient _httpClient;
    private readonly Setting _setting;
    private bool _disposed;

    public DatafordelerFileDownload(Setting setting)
    {
        _httpClient = new();
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        _setting = setting;
    }

    public async Task<string> DownloadAsync(
        string register,
        string resourceName,
        string format,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var latestGenerationFile = await LatestGenerationFileResourceCurrentTotalDownloadAsync(
            register,
            resourceName,
            format,
            cancellationToken).ConfigureAwait(false);

        var uri = BuildResourcePathFileDownload(_baseAddressApi, latestGenerationFile.FileName, _setting.DatafordelerApiKey);

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var tempOutputFilePath = $"{Path.Combine(outputPath, latestGenerationFile.FileName)}.tmp";
        var outputFilePath = Path.Combine(outputPath, latestGenerationFile.FileName);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var fs = new FileStream(
            tempOutputFilePath,
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous
            });

        await stream.CopyToAsync(fs, cancellationToken: cancellationToken).ConfigureAwait(false);

        File.Move(tempOutputFilePath, outputFilePath, overwrite: true);

        return outputFilePath;
    }

    private async Task<DatafordelerFile> LatestGenerationFileResourceCurrentTotalDownloadAsync(
        string register,
        string resourceName,
        string format,
        CancellationToken cancellationToken = default)
    {
        var resources = await LatestGenerationFileResourcesCurrentTotalDownloadAsync(format, register, cancellationToken).ConfigureAwait(false);
        return resources
            .Where(x => x.EntityName.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
            // This is done because sometimes there can be multiple total downloads with a subset.
            // Don't ask me why, an exaple is:
            // Full:
            // DAR_V3_NavngivenVej_TotalDownload_json_Current_636.zip
            // Subsets:
            // DAR_V3_Adressepunkt_0766_TotalDownload_json_Current_636.zip
            // DAR_V3_Adressepunkt_0787_TotalDownload_json_Current_636.zip
            .Where(x => x.FileName.StartsWith($"{register}_V3_{resourceName}_TotalDownload_{format}_Current_", StringComparison.OrdinalIgnoreCase))
            .First();
    }

    public async Task<IEnumerable<DatafordelerFile>> LatestGenerationFileResourcesCurrentTotalDownloadAsync(
        string format,
        string register,
        CancellationToken cancellationToken = default)
    {
        var resources = await LatestGenerationFileResourcesAsync(format, register, cancellationToken).ConfigureAwait(false);

        // Console.WriteLine(JsonSerializer.Serialize(resources.DistinctBy(x => x.EntityName).Select(x => x.EntityName)));

        return resources
            .Where(x => x.TypeOfDownload == "TotalDownload")
            .Where(x => x.TypeOfData == "Current")
            .OrderByDescending(x => x.GenerationNumber);
    }

    private async Task<IEnumerable<DatafordelerFile>> LatestGenerationFileResourcesAsync(
        string format,
        string register,
        CancellationToken cancellationToken = default)
    {
        var resourcePath = new Uri($"{_baseAddressApi}/FileDownloads/GetAvailableFileDownloads?Register={register}&format={format}&apikey={_setting.DatafordelerApiKey}");

        var response = await _httpClient.GetAsync(resourcePath, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var resources = await response.Content.ReadFromJsonAsync<DatafordelerFileResponse>(cancellationToken).ConfigureAwait(false);

        if (resources is null)
        {
            throw new InvalidOperationException($"Received NULL when trying to get resouces from path: '{resourcePath}'.");
        }

        return resources
            .AvailableFileDownloads
            .Where(x => x.ContainedFileFormat == format)
            .Where(x => x.Version == "3");
    }

    private static Uri BuildResourcePathFileDownload(
        string baseUri,
        string fileName,
        string apiKey)
    {
        return new Uri($"{baseUri}/FileDownloads/GetFile?filename={fileName}&apikey={apiKey}");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

internal sealed record DatafordelerFileResponse
{
    [JsonPropertyName("availableFileDownloads")]
    public required IReadOnlyList<DatafordelerFile> AvailableFileDownloads { get; init; }
}

internal sealed record DatafordelerFile
{
    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("register")]
    public required string Register { get; init; }

    [JsonPropertyName("entityName")]
    public required string EntityName { get; init; }

    [JsonPropertyName("typeOfDownload")]
    public required string TypeOfDownload { get; init; }

    [JsonPropertyName("typeOfData")]
    public required string TypeOfData { get; init; }

    [JsonPropertyName("generationNumber")]
    public required int GenerationNumber { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("containedFileFormat")]
    public required string ContainedFileFormat { get; init; }

    [JsonPropertyName("pointInTime")]
    public required DateTime? PointInTime { get; init; }
}
