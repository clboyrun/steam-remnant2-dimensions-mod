using System.Text.Json.Nodes;

namespace Dimensions.Definitions;

/// <summary>
/// One content definition (a weapon, a world...). <see cref="Json"/> is the definition after 'extends' is applied;
/// the generator reads it directly, so any field the schema allows is available without a code change.
/// </summary>
public sealed class Definition(string kind, string id, JsonObject json, string file, string pointer)
{
    public string Kind { get; } = kind;
    public string Id { get; } = id;
    public JsonObject Json { get; internal set; } = json;
    /// <summary>The definition as written, before 'extends'.</summary>
    public JsonObject Raw { get; } = (JsonObject)json.DeepClone();
    public string File { get; } = file;
    public string Pointer { get; } = pointer;

    public bool IsAbstract => Json["abstract"]?.GetValue<bool>() == true;
    public string? Extends => Raw["extends"]?.GetValue<string>();
    public string? Template => Json["template"]?.GetValue<string>();

    public string? GetString(string name) => Json[name] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    public override string ToString() => $"{Kind} {Id}";
}
