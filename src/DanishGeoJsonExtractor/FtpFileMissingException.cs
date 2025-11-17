namespace DanishGeoJsonExtractor;

internal sealed class FtpFileMissingException : Exception
{
    public FtpFileMissingException() { }
    public FtpFileMissingException(string message) { }
    public FtpFileMissingException(string message, Exception innerException) { }
}
