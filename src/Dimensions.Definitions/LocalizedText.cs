using System.Text.Json.Nodes;

namespace Dimensions.Definitions;

/// <summary>Text shown in game: one string per culture. A plain JSON string is the default culture's.</summary>
public sealed record LocalizedText(IReadOnlyDictionary<string, string> Cultures)
{
    public static LocalizedText? From(JsonNode? node, string defaultCulture) => node switch
    {
        JsonValue v when v.TryGetValue<string>(out var s) => new(new Dictionary<string, string> { [defaultCulture] = s }),
        JsonObject o => new(o.Where(p => p.Value is JsonValue).ToDictionary(p => p.Key, p => p.Value!.GetValue<string>())),
        _ => null,
    };

    public string? Get(string culture) => Cultures.TryGetValue(culture, out var s) ? s : null;
}
