using DawaAddress;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DanishGeoJsonExtractor.Dawa;

internal sealed record GeoJsonFeature
{
    [JsonProperty("type")]
    public string Type { get; init; }

    [JsonProperty("properties")]
    public Dictionary<string, string?> Properties { get; init; }

    [JsonIgnore]
    public Geometry? Geometry { get; init; }

    [JsonProperty("geometry", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? GeometryField => GeoJsonJObject();

    public GeoJsonFeature(string type, Dictionary<string, string?> properties)
    {
        Type = type;
        Properties = properties;
    }

    [JsonConstructor]
    public GeoJsonFeature(
        string type,
        Dictionary<string, string?> properties,
        Geometry? geometry)
    {
        Type = type;
        Properties = properties;
        Geometry = geometry;
    }

    private JObject? GeoJsonJObject()
    {
        if (Geometry is not null)
        {
            var serializer = GeoJsonSerializer.Create();
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(jsonWriter, Geometry);
                return JObject.Parse(stringWriter.ToString());
            }
        }
        else
        {
            return null;
        }
    }
}

internal sealed class DawaExtract
{
    private readonly ILogger<DawaExtract> _logger;

    public DawaExtract(ILogger<DawaExtract> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(
        Setting setting,
        CancellationToken cancellationToken = default)
    {
        var datasets = ExtractUtil.GetEnabled(setting.Dawa.Datasets);
        // If none is enabled we just return since there is nothing to process.
        if (!datasets.Any())
        {
            _logger.LogInformation("No datasets enabled for Dawa, so skips extraction.");
            return;
        }

        const string accessAddressOutputName = "adgangsadresse";
        const string unitAddressOutputName = "enhedsadresse";
        const string roadOutputName = "vej";
        const string postCodeOutputName = "postnummer";
        const string namedRoadMunicipalDistrictOutputName = "navngivenvejkommunedel";

        using var httpClient = new HttpClient();
        var client = new DawaClient(httpClient);
        var tId = await client
            .GetLatestTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        if (datasets.Contains(accessAddressOutputName))
        {
            _logger.LogInformation("Starting processing {Name}", accessAddressOutputName);

            var accessAddressesEnumerable = client
                .GetAllAccessAddresses(tId.Id, cancellationToken);

            await MakeGeoJsonFile<DawaAccessAddress>(
                setting.OutDirPath,
                accessAddressOutputName,
                accessAddressesEnumerable,
                MapDawa.Map).ConfigureAwait(false);
        }

        if (datasets.Contains(unitAddressOutputName))
        {
            _logger.LogInformation("Starting processing {Name}", unitAddressOutputName);

            var unitAddressEnumerable = client
                .GetAllUnitAddresses(tId.Id, cancellationToken);

            await MakeGeoJsonFile<DawaUnitAddress>(
                setting.OutDirPath,
                unitAddressOutputName,
                unitAddressEnumerable,
                MapDawa.Map).ConfigureAwait(false);
        }

        if (datasets.Contains(roadOutputName))
        {
            _logger.LogInformation("Starting processing {Name}", roadOutputName);

            var roadEnumerable = client
                .GetAllRoadsAsync(tId.Id, cancellationToken);

            await MakeGeoJsonFile<DawaRoad>(
                setting.OutDirPath,
                roadOutputName,
                roadEnumerable,
                MapDawa.Map).ConfigureAwait(false);
        }

        if (datasets.Contains(postCodeOutputName))
        {
            _logger.LogInformation("Starting processing {Name}", postCodeOutputName);

            var postCodeEnumerable = client
                .GetAllPostCodesAsync(tId.Id, cancellationToken);

            await MakeGeoJsonFile<DawaPostCode>(
                setting.OutDirPath,
                postCodeOutputName,
                postCodeEnumerable,
                MapDawa.Map).ConfigureAwait(false);
        }

        if (datasets.Contains(namedRoadMunicipalDistrictOutputName))
        {
            _logger.LogInformation(
                "Starting processing {Name}",
                namedRoadMunicipalDistrictOutputName);

            var namedMunicipalDistrictEnumerable = client
                .GetAllNamedRoadMunicipalDistrictsAsync(tId.Id, cancellationToken);

            await MakeGeoJsonFile<NamedRoadMunicipalDistrict>(
                setting.OutDirPath,
                namedRoadMunicipalDistrictOutputName,
                namedMunicipalDistrictEnumerable,
                MapDawa.Map).ConfigureAwait(false);
        }
    }

    private static async Task MakeGeoJsonFile<T>(
        string outputDirPath,
        string outputName,
        IAsyncEnumerable<T> stream,
        Func<T, GeoJsonFeature> mapFunc)
    {
        var path = Path.Combine(outputDirPath, $"{outputName}.geojson");

        using var writer = new StreamWriter(path);

        await writer.WriteLineAsync('{').ConfigureAwait(false);
        await writer.WriteLineAsync("\"type\": \"FeatureCollection\",").ConfigureAwait(false);
        await writer.WriteLineAsync($"\"name\": \"{outputName}\",").ConfigureAwait(false);
        await writer.WriteLineAsync("\"crs\": { \"type\": \"name\", \"properties\": { \"name\": \"urn:ogc:def:crs:EPSG::25832\" } },").ConfigureAwait(false);
        await writer.WriteLineAsync("\"features\": [").ConfigureAwait(false);

        var serializeSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        var first = true;
        await foreach (var line in stream.ConfigureAwait(false))
        {
            if (first)
            {
                var json = JsonConvert.SerializeObject(mapFunc(line), serializeSettings);
                await writer.WriteLineAsync(json).ConfigureAwait(false);
                first = false;
            }
            else
            {
                var json = JsonConvert.SerializeObject(mapFunc(line), serializeSettings);
                await writer.WriteLineAsync(',').ConfigureAwait(false);
                await writer.WriteAsync(json).ConfigureAwait(false);
            }
        }

        await writer.WriteLineAsync("").ConfigureAwait(false);
        await writer.WriteLineAsync(']').ConfigureAwait(false);
        await writer.WriteLineAsync('}').ConfigureAwait(false);
    }
}
