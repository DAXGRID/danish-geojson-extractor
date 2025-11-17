namespace DanishGeoJsonExtractor;

internal sealed class FtpDirectoryNotFoundException : Exception
{
    public FtpDirectoryNotFoundException() { }
    public FtpDirectoryNotFoundException(string message) { }
    public FtpDirectoryNotFoundException(string message, Exception innerException) { }
}
