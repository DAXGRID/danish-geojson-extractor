using FluentFTP;

namespace DatafordelerGeoJsonExtractor.Dataforsyning;

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
            _ftpSetting.Host,
            _ftpSetting.Username,
            _ftpSetting.Password);

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        var listing = await client
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
        using var client = new AsyncFtpClient(
            _ftpSetting.Host,
            _ftpSetting.Username,
            _ftpSetting.Password);

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        var listing = await client
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
        using var client = new AsyncFtpClient(
            _ftpSetting.Host,
            _ftpSetting.Username,
            _ftpSetting.Password);

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        await client
            .DownloadFile(
                localPath,
                remotePath,
                FtpLocalExists.Overwrite,
                FtpVerify.Retry,
                token: cancellationToken)
            .ConfigureAwait(false);
    }
}
