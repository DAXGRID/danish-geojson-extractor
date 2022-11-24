using System.Globalization;

namespace DanishGeoJsonExtractor;

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

    public static (string name, DateTime created) NewestFile(
        string fileName,
        IEnumerable<(string name, DateTime created)> ftpFiles)
    {
        return ftpFiles
            .Where(x => x.name.StartsWith(fileName,
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

    public static IEnumerable<string> GetEnabled(Dictionary<string, bool> x) =>
        x.Where(x => x.Value).Select(x => x.Key);
}
