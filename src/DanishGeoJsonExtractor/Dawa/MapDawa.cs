using DawaAddress;
using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DanishGeoJsonExtractor.Dawa;

internal static class MapDawa
{
    public static GeoJsonFeature Map(object x)
    {
        return new GeoJsonFeature("Feature", RetrieveProperties(x));
    }

    public static GeoJsonFeature Map(DawaAccessAddress accessAddress)
    {
        var properties = RetrieveProperties(accessAddress);
        properties.Remove("etrs89koordinat_øst");
        properties.Remove("etrs89koordinat_nord");

        return new GeoJsonFeature(
            "Feature",
            properties,
            new Point(
                accessAddress.NorthCoordinate,
                accessAddress.EastCoordinate));
    }

    private static Dictionary<string, string?> RetrieveProperties(object obj)
    {
        return obj
            .GetType()
            .GetProperties()
            .ToDictionary(
                x => x.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? throw new InvalidOperationException("Could not find name of property."),
                x => x.GetValue(obj)?.ToString());
    }
}
