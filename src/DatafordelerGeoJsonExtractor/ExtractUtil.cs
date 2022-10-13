using System.Globalization;

namespace DatafordelerGeoJsonExtractor;

internal static class ExtractUtil
{
    public static (string name, DateTime created) NewestDirectory(
        string folderStartName,
        IEnumerable<(string name, DateTime created)> ftpFiles)
    {
        return ftpFiles
            .Where(x => x.name.StartsWith(folderStartName,
                                          true,
                                          CultureInfo.InvariantCulture))
            .OrderBy(x => x.created)
            .First();
    }

    public static void DeleteIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
