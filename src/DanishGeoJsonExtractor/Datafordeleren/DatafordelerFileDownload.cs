using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
        string entity,
        string format,
        CancellationToken cancellationToken = default)
    {
        var resources = await LatestGenerationFileResourcesCurrentTotalDownloadAsync(format, register, entity, cancellationToken).ConfigureAwait(false);
        try
        {
            return resources
                .Where(x => x.EntityName.Equals(entity, StringComparison.OrdinalIgnoreCase))
                // This is done because sometimes there can be multiple total downloads with a subset.
                // Don't ask me why, an exaple is:
                // Full:
                // DAR_V3_NavngivenVej_TotalDownload_json_Current_636.zip
                // Subsets:
                // DAR_V3_Adressepunkt_0766_TotalDownload_json_Current_636.zip
                // DAR_V3_Adressepunkt_0787_TotalDownload_json_Current_636.zip
                .Where(x => x.FileName.StartsWith($"{register}_V3_{entity}_TotalDownload_{format}_Current_", StringComparison.OrdinalIgnoreCase))
                .First();
        }
        catch (System.InvalidOperationException)
        {
            throw new InvalidOperationException($"Could not find resource in register: '{register}' with resource name: '{entity}' in the format: '{format}'.");
        }
    }

    public async Task<IEnumerable<DatafordelerFile>> LatestGenerationFileResourcesCurrentTotalDownloadAsync(
        string format,
        string register,
        string? entity = null,
        CancellationToken cancellationToken = default)
    {
        var resources = await LatestGenerationFileResourcesAsync(format, register, entity, cancellationToken).ConfigureAwait(false);
        return resources
            .Where(x => x.TypeOfDownload == "TotalDownload")
            .Where(x => x.TypeOfData == "Current")
            .Where(x => Regex.IsMatch(x.FileName, $"{register}_V3_[A-Za-z]*_TotalDownload_[A-Za-z]*_Current_[1-9]*.zip"))
            .OrderByDescending(x => x.GenerationNumber);
    }

    private async Task<IEnumerable<DatafordelerFile>> LatestGenerationFileResourcesAsync(
        string format,
        string register,
        string? entity = null,
        CancellationToken cancellationToken = default)
    {
        async Task<DatafordelerFileResponse> GetAvailableFileDownloadsAsync(int pageNumber)
        {
            var resourcePath = $"{_baseAddressApi}/FileDownloads/GetAvailableFileDownloads?Register={register}&format={format}&apikey={_setting.DatafordelerApiKey}&Version=3&Register={register}&PageNumber={pageNumber}";

            if (entity is not null)
            {
                resourcePath = $"{resourcePath}&Entity={entity}";
            }

            var response = await _httpClient.GetAsync(new Uri(resourcePath), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response is null)
            {
                throw new InvalidOperationException($"Received NULL when trying to get resouces from path: '{resourcePath}'.");
            }

            var datafordelerFileResponse = await response.Content.ReadFromJsonAsync<DatafordelerFileResponse>(cancellationToken).ConfigureAwait(false);
            return datafordelerFileResponse!;
        }

        var initialPageResponse = await GetAvailableFileDownloadsAsync(1).ConfigureAwait(false);
        var totalResources = new List<DatafordelerFile>(initialPageResponse.PaginationMetadata.TotalCount);
        totalResources.AddRange(initialPageResponse.AvailableFileDownloads);

        if (initialPageResponse.PaginationMetadata.TotalPages > 1)
        {
            for (var pageCount = 2; pageCount <= initialPageResponse.PaginationMetadata.TotalPages; pageCount++)
            {
                var pageResponse = await GetAvailableFileDownloadsAsync(pageCount).ConfigureAwait(false);
                totalResources.AddRange(pageResponse.AvailableFileDownloads);
            }
        }

        if (totalResources.Count != initialPageResponse.PaginationMetadata.TotalCount)
        {
            throw new InvalidDataException($"The initial page count does not match the count of objects retrieved from available file downloads. Expected: {initialPageResponse.PaginationMetadata.TotalCount}, retrieved {totalResources.Count}");
        }

        return totalResources
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

    [JsonPropertyName("paginationMetadata")]
    public required PaginationMetadata PaginationMetadata { get; init; }
}

internal sealed record PaginationMetadata
{
    [JsonPropertyName("currentPage")]
    public required int CurrentPage { get; init; }

    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }

    [JsonPropertyName("totalPages")]
    public required int TotalPages { get; init; }
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
