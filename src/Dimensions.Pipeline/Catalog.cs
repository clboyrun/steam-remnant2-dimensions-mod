using System.Text.Json.Nodes;

namespace Dimensions.Pipeline;

/// <summary>
/// What the generator knows about the game: default templates, row structs, stat field names, prefixes.
/// Loaded from data/catalog.json (embedded) so game facts can be corrected without touching code.
/// </summary>
public sealed class Catalog(JsonObject json)
{
    public JsonObject Json { get; } = json;

    public static Catalog Load(string? path = null)
    {
        if (path is not null) return new((JsonObject)JsonNode.Parse(File.ReadAllText(path))!);
        using var stream = typeof(Catalog).Assembly.GetManifestResourceStream("data/catalog.json")!;
        return new((JsonObject)JsonNode.Parse(stream)!);
    }

    public string ContentRoot => Json["contentRoot"]?.GetValue<string>() ?? "/Game/Dimensions";

    public JsonObject Kind(string kind) => Json["kinds"]?[kind] as JsonObject ?? [];

    public string Folder(string kind) => Kind(kind)["folder"]?.GetValue<string>() ?? kind;
    public string Prefix(string kind) => Kind(kind)["prefix"]?.GetValue<string>() ?? "";
    public bool Verified(string kind) => Kind(kind)["verified"]?.GetValue<bool>() ?? false;

    /// <summary>A string at a path like "templates/longGun" under the kind's entry.</summary>
    public string? Get(string kind, string path)
    {
        JsonNode? node = Kind(kind);
        foreach (var part in path.Split('/')) node = node?[part];
        return node is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
    }

    public IReadOnlyDictionary<string, string> Map(string kind, string name) =>
        Kind(kind)[name] is JsonObject o
            ? o.Where(p => p.Value is JsonValue).ToDictionary(p => p.Key, p => p.Value!.GetValue<string>())
            : new Dictionary<string, string>();

    public string RecipeClass => Json["recipe"]?["class"]?.GetValue<string>() ?? "/Script/GunfireRuntime.CraftingRecipe";
    public string RecipeListProperty => Json["recipe"]?["listProperty"]?.GetValue<string>() ?? "Recipes";
    public string ScrapItem => Json["recipe"]?["scrapItem"]?.GetValue<string>()
        ?? "/Game/World_Base/Items/Materials/Scraps/Material_Scraps.Material_Scraps_C";
}
