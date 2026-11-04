using System.Text.Json.Nodes;

namespace Dimensions.Definitions;

public enum PakFormat { IoStore, Legacy }

/// <summary>dimension.json: the root of one content mod.</summary>
public sealed record DimensionManifest(
    string Id,
    string Name,
    string Version,
    string? Author,
    string DefaultCulture,
    IReadOnlyList<string> Content,
    string PakName,
    int ChunkId,
    PakFormat PakFormat)
{
    public const string FileName = "dimension.json";

    public static DimensionManifest FromJson(JsonObject json)
    {
        var id = json["id"]?.GetValue<string>() ?? "";
        var pak = json["pak"] as JsonObject;
        return new(
            id,
            json["name"]?.GetValue<string>() ?? id,
            json["version"]?.GetValue<string>() ?? "0.0.0",
            json["author"]?.GetValue<string>(),
            json["defaultCulture"]?.GetValue<string>() ?? "en",
            json["content"] is JsonArray content ? [.. content.Select(c => c!.GetValue<string>())] : ["content/**/*.json"],
            pak?["name"]?.GetValue<string>() ?? $"zz_Dimensions_{id}",
            pak?["chunkId"]?.GetValue<int>() ?? 7100,
            pak?["format"]?.GetValue<string>() == "legacy" ? PakFormat.Legacy : PakFormat.IoStore);
    }
}
