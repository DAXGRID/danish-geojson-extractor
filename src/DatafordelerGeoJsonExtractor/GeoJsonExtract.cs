using FluentFTP;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DatafordelerGeoJsonExtractor;

internal sealed class GeoJsonExtract
{
    private readonly ILogger<GeoJsonExtract> _logger;
    private readonly Setting _setting;

    public GeoJsonExtract(ILogger<GeoJsonExtract> logger, Setting setting)
    {
        _logger = logger;
        _setting = setting;
    }

    public async Task StartAsync(
        string remoteDir,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var ftpClient = new DataforsyningFtpClient(_setting.FtpSetting);

        var ftpFiles = await ftpClient
            .FilesInPathAsync(remoteDir, cancellationToken)
            .ConfigureAwait(false);

        var newestFile = ftpFiles
            .Where(x => x.name.StartsWith(fileName, true, CultureInfo.InvariantCulture))
            .OrderBy(x => x.created)
            .First();

        _logger.LogInformation(
            "Found newest file {FileName}, created at {CreatedDate}.",
            newestFile.name,
            newestFile.created);

        await ftpClient
            .DownloadFileAsync(
                Path.Combine(remoteDir, newestFile.name),
                "/tmp",
                cancellationToken)
            .ConfigureAwait(false);
    }
}
