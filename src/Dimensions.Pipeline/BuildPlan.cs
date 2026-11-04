using System.Text.Json;
using System.Text.Json.Nodes;
using Dimensions.Definitions;

namespace Dimensions.Pipeline;

/// <summary>
/// Everything the Unreal editor step does for one dimension, in order. Written as plan.json and read by
/// unreal/Python/dimensions/apply.py, so it holds plain JSON: each operation is an object with an "op" field.
/// </summary>
public sealed class BuildPlan
{
    public const int FormatVersion = 1;

    public required DimensionManifest Manifest { get; init; }
    public required string ContentRoot { get; init; }
    public List<JsonObject> Operations { get; } = [];

    /// <summary>Game assets this plan changes in place. They're cooked into the pak and override the game's.</summary>
    public SortedSet<string> Overrides { get; } = new(StringComparer.Ordinal);

    /// <summary>Game assets that must be in the Unreal project before the plan runs (templates, lists to edit).</summary>
    public SortedSet<string> RequiredAssets { get; } = new(StringComparer.Ordinal);

    /// <summary>Things the generator couldn't do automatically, for the person to finish by hand.</summary>
    public List<string> Notes { get; } = [];

    public IEnumerable<JsonObject> OperationsOf(string op) => Operations.Where(o => o["op"]?.GetValue<string>() == op);

    public JsonObject ToJson() => new()
    {
        ["format"] = FormatVersion,
        ["dimension"] = new JsonObject
        {
            ["id"] = Manifest.Id,
            ["name"] = Manifest.Name,
            ["version"] = Manifest.Version,
            ["defaultCulture"] = Manifest.DefaultCulture,
        },
        ["contentRoot"] = ContentRoot,
        ["pak"] = new JsonObject
        {
            ["name"] = Manifest.PakName,
            ["chunkId"] = Manifest.ChunkId,
            ["format"] = Manifest.PakFormat == PakFormat.IoStore ? "iostore" : "legacy",
        },
        ["overrides"] = new JsonArray([.. Overrides.Select(o => (JsonNode)o)]),
        ["requiredAssets"] = new JsonArray([.. RequiredAssets.Select(o => (JsonNode)o)]),
        ["notes"] = new JsonArray([.. Notes.Select(n => (JsonNode)n)]),
        ["operations"] = new JsonArray([.. Operations.Select(o => o.DeepClone())]),
    };

    public string ToJsonString() => ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}
