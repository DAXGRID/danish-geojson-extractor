using FluentFTP;
using FluentFTP.Logging;
using Microsoft.Extensions.Logging;

namespace DanishGeoJsonExtractor.Dataforsyning;

internal sealed class DataforsyningFtpClient : IDisposable
{
    private readonly AsyncFtpClient _client;

    public DataforsyningFtpClient(FtpSetting ftpSetting, ILogger logger)
    {
        _client = new AsyncFtpClient(
            ftpSetting.Host,
            ftpSetting.Username,
            ftpSetting.Password);

        // Should be converted to milliseconds.
        _client.Config.ConnectTimeout = ftpSetting.ConnectionTimeOutSeconds * 1000;
        if (ftpSetting.EnableLogging)
        {
            _client.Config.LogToConsole = false;
            _client.Logger = new FtpLogAdapter(logger);
        }

        _client.Config.RetryAttempts = ftpSetting.RetryAttempts;
        _client.Config.ReadTimeout = ftpSetting.ReadTimeoutSeconds * 1000;
    }

    public async Task<IEnumerable<(string name, DateTime created)>> FilesInPathAsync(
        string remotePath,
        CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
        {
            await _client.AutoConnect(cancellationToken).ConfigureAwait(false);
        }

        var listing = await _client
            .GetListing(remotePath, new(), cancellationToken)
            .ConfigureAwait(false);

        return listing
            .Where(x => x.Type == FtpObjectType.File)
            .Select(x => (x.Name, x.Created));
    }

    public async Task<IEnumerable<(string name, DateTime created)>> DirectoriesInPathAsync(
        string remotePath,
        CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
        {
            await _client.AutoConnect(cancellationToken).ConfigureAwait(false);
        }

        var listing = await _client
            .GetListing(remotePath, new(), cancellationToken)
            .ConfigureAwait(false);

        return listing
            .Where(x => x.Type == FtpObjectType.Directory)
            .Select(x => (x.Name, x.Modified));
    }

    public async Task DownloadFileAsync(
        string remotePath,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
        {
            await _client.AutoConnect(cancellationToken).ConfigureAwait(false);
        }

        await _client
            .DownloadFile(
                localPath,
                remotePath,
                FtpLocalExists.Overwrite,
                FtpVerify.Retry,
                token: cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
