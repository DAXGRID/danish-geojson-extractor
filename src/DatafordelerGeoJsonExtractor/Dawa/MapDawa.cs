using DawaAddress;
using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DatafordelerGeoJsonExtractor.Dawa;

internal static class MapDawa
{
    public static GeoJsonFeature Map(object x)
    {
        return new GeoJsonFeature("Feature", RetrieveProperties(x));
    }

    public static GeoJsonFeature Map(DawaAccessAddress dawaAccessAddress)
    {
        var properties = dawaAccessAddress
            .GetType()
            .GetProperties()
            .Where(x =>
                   x.Name != nameof(dawaAccessAddress.EastCoordinate) ||
                   x.Name != nameof(dawaAccessAddress.NorthCoordinate))
            .ToDictionary(
                x => x.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? throw new InvalidOperationException("Could not find name of property."),
                x => x.GetValue(x)?.ToString());

        return new GeoJsonFeature(
            "Feature",
            properties,
            new Point(
                dawaAccessAddress.NorthCoordinate,
                dawaAccessAddress.EastCoordinate));
    }

    private static Dictionary<string, string?> RetrieveProperties(object x)
    {
        return x
            .GetType()
            .GetProperties()
            .ToDictionary(
                x => x.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? throw new InvalidOperationException("Could not find name of property."),
                x => x.GetValue(x)?.ToString() ?? null);
    }
}
