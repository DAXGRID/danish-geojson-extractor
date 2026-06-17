using System.Text.Json.Serialization;

namespace DanishGeoJsonExtractor;

internal sealed record StedNavnSetting
{
    [JsonPropertyName("datasets")]
    public Dictionary<string, bool> Datasets { get; init; }

    [JsonConstructor]
    public StedNavnSetting(Dictionary<string, bool> datasets)
    {
        Datasets = datasets;
    }
}

internal sealed record MatrikelSetting
{
    [JsonPropertyName("datasets")]
    public Dictionary<string, bool> Datasets { get; init; }

    [JsonConstructor]
    public MatrikelSetting(Dictionary<string, bool> datasets)
    {
        Datasets = datasets;
    }
}

internal sealed record GeoDanmarkSetting
{
    [JsonPropertyName("datasets")]
    public Dictionary<string, bool> Datasets { get; init; }

    [JsonConstructor]
    public GeoDanmarkSetting(Dictionary<string, bool> datasets)
    {
        Datasets = datasets;
    }
}

internal sealed record DawaSetting
{
    [JsonPropertyName("datasets")]
    public Dictionary<string, bool> Datasets { get; init; }

    [JsonConstructor]
    public DawaSetting(Dictionary<string, bool> datasets)
    {
        Datasets = datasets;
    }
}

internal sealed record Setting
{
    [JsonPropertyName("datafordelerApiKey")]
    public string DatafordelerApiKey { get; init; }

    [JsonPropertyName("outDirPath")]
    public string OutDirPath { get; init; }

    [JsonPropertyName("matrikel")]
    public MatrikelSetting? Matrikel { get; init; }

    [JsonPropertyName("geoDanmark")]
    public GeoDanmarkSetting? GeoDanmark { get; init; }

    [JsonPropertyName("dawa")]
    public DawaSetting? Dawa { get; init; }

    [JsonPropertyName("stedNavn")]
    public StedNavnSetting? StedNavn { get; init; }

    [JsonConstructor]
    public Setting(
        string datafordelerApiKey,
        string outDirPath,
        MatrikelSetting? matrikel,
        GeoDanmarkSetting? geoDanmark,
        DawaSetting? dawa,
        StedNavnSetting? stedNavn)
    {
        DatafordelerApiKey = datafordelerApiKey;
        OutDirPath = outDirPath;
        Matrikel = matrikel;
        GeoDanmark = geoDanmark;
        Dawa = dawa;
        StedNavn = stedNavn;
    }
}
