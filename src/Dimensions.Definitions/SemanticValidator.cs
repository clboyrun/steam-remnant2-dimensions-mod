using System.Text.Json.Nodes;
using static Dimensions.Definitions.DefinitionKinds;

namespace Dimensions.Definitions;

/// <summary>Checks the schema can't: ids, references between definitions, files on disk, cultures, ranges.</summary>
internal static class SemanticValidator
{
    public static IEnumerable<Diagnostic> Validate(DimensionProject project, SchemaValidator validator)
    {
        var bag = new DiagnosticBag();
        var culture = project.Manifest.DefaultCulture;

        foreach (var group in project.Definitions.GroupBy(d => d.Id).Where(g => g.Count() > 1))
        {
            foreach (var d in group.Skip(1))
                bag.Error($"id '{d.Id}' is already used by the {group.First().Kind} in {group.First().File}", d.File, d.Pointer + "/id");
        }

        foreach (var d in project.Definitions)
        {
            // A definition that extends another is checked again once merged, so required fields from the base count.
            if (d.Extends is not null && !d.IsAbstract)
            {
                var merged = (JsonObject)d.Json.DeepClone();
                merged.Remove("extends");
                bag.AddRange(validator.ValidateDefinition(merged, d.File, d.Pointer)
                    .Select(e => e with { Message = e.Message + " (after applying 'extends')" }));
            }
            if (d.IsAbstract) continue;

            var check = new Checker(project, d, bag, culture);
            check.Walk(d.Json, d.Pointer);
            check.KindRules();
        }
        return bag.Items;
    }

    private sealed class Checker(DimensionProject project, Definition d, DiagnosticBag bag, string culture)
    {
        private static readonly HashSet<string> TextFields =
            ["name", "description", "flavorText", "briefDescription"];

        private static readonly HashSet<string> FileFields = ["small", "large", "hud", "image", "mesh"];

        public void Walk(JsonNode? node, string pointer, string? key = null, bool inProperties = false)
        {
            switch (node)
            {
                case JsonObject obj:
                    if (inProperties && obj["$ref"] is JsonValue r && r.TryGetValue<string>(out var refId))
                    {
                        Reference(refId, pointer + "/$ref", null);
                    }
                    if (!inProperties && key is not null && TextFields.Contains(key)) Text(obj, pointer);
                    foreach (var (k, v) in obj)
                    {
                        var props = inProperties || k is "properties" or "components";
                        Walk(v, $"{pointer}/{Escape(k)}", k, props);
                    }
                    break;
                case JsonArray arr:
                    for (var i = 0; i < arr.Count; i++) Walk(arr[i], $"{pointer}/{i}", key, inProperties);
                    break;
                case JsonValue v when !inProperties && key is not null && v.TryGetValue<string>(out var s):
                    if (FileFields.Contains(key) || IsTextureParameter(pointer)) File(s, pointer);
                    if (s.StartsWith("/Game/Dimensions/", StringComparison.Ordinal) &&
                        !s.StartsWith($"/Game/Dimensions/{project.Manifest.Id}/", StringComparison.Ordinal))
                    {
                        bag.Warning($"'{s}' is in another dimension's folder; it won't be in this pak", d.File, pointer);
                    }
                    break;
            }
        }

        private static bool IsTextureParameter(string pointer) => pointer.Contains("/textures/", StringComparison.Ordinal);

        private void File(string value, string pointer)
        {
            if (GamePath.IsGamePath(value)) return;
            if (!System.IO.File.Exists(project.ResolveFile(value)))
                bag.Error($"file '{value}' not found (paths are relative to the folder with dimension.json)", d.File, pointer);
        }

        private void Text(JsonObject text, string pointer)
        {
            if (text[culture] is null)
                bag.Error($"text has no '{culture}' entry (the dimension's default culture)", d.File, pointer);
        }

        /// <summary>Checks an id reference. Game paths aren't checked here (the editor step reports missing ones).</summary>
        public void Reference(string? value, string pointer, IReadOnlySet<string>? allowed, string? what = null)
        {
            if (value is null || GamePath.IsGamePath(value)) return;
            var target = project.Find(value);
            if (target is null)
            {
                bag.Error($"'{value}' isn't defined in this dimension (or isn't a game path)", d.File, pointer);
            }
            else if (target.IsAbstract)
            {
                bag.Error($"'{value}' is abstract, so it can't be referenced", d.File, pointer);
            }
            else if (allowed is not null && !allowed.Contains(target.Kind))
            {
                bag.Error($"'{value}' is a {target.Kind}; expected {what ?? string.Join(" or ", allowed)}", d.File, pointer);
            }
        }

        private void Ref(JsonNode? node, string pointer, IReadOnlySet<string>? allowed, string? what = null) =>
            Reference(node is JsonValue v && v.TryGetValue<string>(out var s) ? s : null, pointer, allowed, what);

        private static readonly HashSet<string> LootTables = [LootTable];
        private static readonly HashSet<string> Traits = [Trait];
        private static readonly HashSet<string> Worlds = [World];
        private static readonly HashSet<string> Archetypes = [Archetype];
        private static readonly HashSet<string> Quests = [Adventure, Blueprint];

        public void KindRules()
        {
            var json = d.Json;
            var p = d.Pointer;
            Acquisition(json["acquisition"] as JsonArray, p + "/acquisition");
            Range(json, p);

            switch (d.Kind)
            {
                case Weapon:
                    Ref(json["mod"], p + "/mod", new HashSet<string> { WeaponMod }, "a weaponMod");
                    if (json["stats"] is null && json["statsRow"] is null)
                        bag.Warning("no 'stats' or 'statsRow': the weapon keeps its template's stats", d.File, p);
                    break;
                case Mutator:
                    MaxLevelArray(json["levels"] as JsonArray, json["maxLevel"]?.GetValue<int>() ?? 10, p + "/levels");
                    break;
                case Trait:
                    var max = json["maxLevel"]?.GetValue<int>() ?? 10;
                    if (json["levels"] is JsonObject levels)
                        foreach (var (stat, values) in levels)
                            if (values is JsonArray a && a.Count > max)
                                bag.Error($"{a.Count} values for '{stat}' but maxLevel is {max}", d.File, $"{p}/levels/{Escape(stat)}");
                    break;
                case Archetype:
                    Each(json["startingGear"], p + "/startingGear", (n, ptr) => Ref(n, ptr, Items, "an item"));
                    Each(json["startingTraits"], p + "/startingTraits", (n, ptr) => Ref(n?["trait"], ptr + "/trait", Traits));
                    Acquisition(json["engram"]?["acquisition"] as JsonArray, p + "/engram/acquisition");
                    break;
                case LootTable:
                    Each(json["entries"], p + "/entries", (n, ptr) =>
                    {
                        Ref(n?["item"], ptr + "/item", Items, "an item");
                        Ref(n?["table"], ptr + "/table", LootTables);
                        Range(n as JsonObject, ptr);
                    });
                    break;
                case World:
                    Ref(json["loot"], p + "/loot", LootTables);
                    var zoneIds = new HashSet<string>();
                    Each(json["zones"], p + "/zones", (n, ptr) =>
                    {
                        var zid = n?["id"]?.GetValue<string>();
                        if (zid is not null && !zoneIds.Add(zid)) bag.Error($"zone id '{zid}' is used twice", d.File, ptr + "/id");
                        Ref(n?["loot"], ptr + "/loot", LootTables);
                        Each(n?["encounters"], ptr + "/encounters", (e, eptr) => Ref(e, eptr, LootTables));
                    });
                    break;
                case Adventure:
                    Ref(json["world"], p + "/world", Worlds);
                    Each(json["rewards"], p + "/rewards", (n, ptr) => Ref(n, ptr, Items, "an item"));
                    Each(json["storyline"], p + "/storyline", (n, ptr) => Ref(n, ptr, Quests, "an adventure or blueprint"));
                    break;
            }

            if (json["registration"] is JsonObject reg)
            {
                var mode = reg["mode"]?.GetValue<string>() ?? "discover";
                if (mode == "append" && (reg["list"] is null || reg["property"] is null))
                    bag.Error("registration mode 'append' needs 'list' and 'property'", d.File, p + "/registration");
                if (mode == "replace" && reg["replaces"] is null)
                    bag.Error("registration mode 'replace' needs 'replaces'", d.File, p + "/registration");
            }
        }

        private void Acquisition(JsonArray? list, string pointer)
        {
            if (list is null) return;
            for (var i = 0; i < list.Count; i++)
            {
                var a = list[i] as JsonObject;
                var ptr = $"{pointer}/{i}";
                switch (a?["type"]?.GetValue<string>())
                {
                    case "loot": Ref(a["table"], ptr + "/table", LootTables); Range(a, ptr); break;
                    case "craft" or "vendor":
                        Each(a["materials"], ptr + "/materials", (n, mptr) => Ref(n?["item"], mptr + "/item", Items, "an item"));
                        if (a["materials"] is JsonArray m && m.Count + (a["scrap"] is null ? 0 : 1) > 4)
                            bag.Error("a recipe takes at most 4 ingredients, scrap included", d.File, ptr);
                        break;
                    case "questReward": Ref(a["quest"], ptr + "/quest", Quests, "an adventure"); break;
                    case "startingGear": Ref(a["archetype"], ptr + "/archetype", Archetypes); break;
                }
            }
        }

        private void Range(JsonObject? json, string pointer)
        {
            var min = json?["levelMin"]?.GetValue<int>();
            var max = json?["levelMax"]?.GetValue<int>();
            if (min is not null && max is not null && min > max)
                bag.Error($"levelMin {min} is above levelMax {max}", d.File, pointer);
        }

        private void MaxLevelArray(JsonArray? levels, int max, string pointer)
        {
            if (levels is not null && levels.Count > max)
                bag.Error($"{levels.Count} levels but maxLevel is {max}", d.File, pointer);
        }

        private static void Each(JsonNode? node, string pointer, Action<JsonNode?, string> action)
        {
            if (node is not JsonArray arr) return;
            for (var i = 0; i < arr.Count; i++) action(arr[i], $"{pointer}/{i}");
        }
    }

    private static string Escape(string key) => key.Replace("~", "~0").Replace("/", "~1");
}
