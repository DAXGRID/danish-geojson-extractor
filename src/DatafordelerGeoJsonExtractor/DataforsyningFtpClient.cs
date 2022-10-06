using FluentFTP;

namespace DatafordelerGeoJsonExtractor;

internal sealed class DataforsyningFtpClient
{
    private readonly FtpSetting _ftpSetting;

    public DataforsyningFtpClient(FtpSetting ftpSetting)
    {
        _ftpSetting = ftpSetting;
    }

    public async Task<IEnumerable<(string name, DateTime created)>> FilesInPathAsync(
        string remotePath,
        CancellationToken cancellationToken = default)
    {
        using var client = new AsyncFtpClient(
            remotePath,
            _ftpSetting.Username,
            _ftpSetting.Password);

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        return (await client
                .GetListing(remotePath, new(), cancellationToken)
                .ConfigureAwait(false))
            .Where(x => x.Type == FtpObjectType.File)
            .Select(x => (x.Name, x.Created));
    }

    public async Task DownloadFileAsync(
        string remotePath,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        using var client = new AsyncFtpClient(
            "/",
            _ftpSetting.Username,
            _ftpSetting.Password);

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        await client
            .DownloadFile(
                localPath,
                remotePath,
                FtpLocalExists.Skip,
                FtpVerify.Retry,
                token: cancellationToken)
            .ConfigureAwait(false);
    }
}
