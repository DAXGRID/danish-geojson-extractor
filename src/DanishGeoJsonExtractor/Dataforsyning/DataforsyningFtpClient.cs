using FluentFTP;
using FluentFTP.Exceptions;
using FluentFTP.Logging;
using Microsoft.Extensions.Logging;

namespace DanishGeoJsonExtractor.Dataforsyning;

internal sealed class DataforsyningFtpClient : IDisposable
{
    private readonly AsyncFtpClient _client;
    private readonly ILogger _logger;
    private bool _isDisposed;

    public DataforsyningFtpClient(FtpSetting ftpSetting, ILogger logger)
    {
        _logger = logger;

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

    public async Task RetryDownloadFileAsync(
        string remotePath,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        var retries = 0;
        var maxRetries = 10;
        var timeoutMs = 5000;

        while (true)
        {
            try
            {
                // First time we overwrite, otherwise we resume in case of a timeout.
                if (retries == 0)
                {
                    _logger.LogInformation("Downloading file {RemoteFilePath}.", remotePath);
                    await DownloadFileAsync(remotePath, localPath, FtpLocalExists.Overwrite, FtpVerify.Retry, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("Retrying downloading file {RemoteFilePath}.", remotePath);
                    await DownloadFileAsync(remotePath, localPath, FtpLocalExists.Resume, FtpVerify.Retry, cancellationToken).ConfigureAwait(false);
                }

                // If we get to this point it means that the download has finished.
                break;
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning("Timeout reading from the FTP server: {Exception}. Current number of retries {Retries}.", ex, retries);

                retries++;

                if (retries >= maxRetries)
                {
                    _logger.LogCritical("Reached the maximum amount of retries. {MaxRetries}.", maxRetries);
                    throw;
                }

                await Task.Delay(timeoutMs, cancellationToken).ConfigureAwait(false);
            }
            catch (FtpException ex)
            {
                if (ex.InnerException is TimeoutException)
                {
                    _logger.LogWarning("Timeout reading from the FTP server: {Exception}. Current number of retries {Retries}.", ex, retries);

                    retries++;

                    if (retries >= maxRetries)
                    {
                        _logger.LogCritical("Reached the maximum amount of retries. {MaxRetries}.", maxRetries);
                        throw;
                    }

                    await Task.Delay(timeoutMs, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    private async Task DownloadFileAsync(
        string remotePath,
        string localPath,
        FtpLocalExists ftpLocalExists,
        FtpVerify ftpVerify,
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
                ftpLocalExists,
                ftpVerify,
                token: cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _client.Dispose();
            _isDisposed = true;
        }
    }
}
