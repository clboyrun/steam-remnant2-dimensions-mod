using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Dimensions.Definitions;

/// <summary>Checks files against the JSON schemas in /schema (embedded in this assembly).</summary>
public sealed class SchemaValidator
{
    private const string BaseUri = "https://github.com/clboyrun/steam-remnant2-dimensions-mod/schema/";

    private readonly EvaluationOptions _options = new() { OutputFormat = OutputFormat.Hierarchical };
    private readonly ConcurrentDictionary<string, JsonSchema> _byRef = new();
    private readonly Dictionary<string, HashSet<string>> _fieldsByKind = [];

    public SchemaValidator()
    {
        var assembly = typeof(SchemaValidator).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("schema/", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            // The files carry no $id so editors resolve their references next to the file; give each one here.
            var json = JsonNode.Parse(stream)!.AsObject();
            json["$id"] = BaseUri + name["schema/".Length..];
            _options.SchemaRegistry.Register(JsonSchema.FromText(json.ToJsonString()));
            if (name == "schema/definition.schema.json") ReadKindFields(json);
        }
    }

    /// <summary>Top-level field names each kind allows (its own plus the shared base ones).</summary>
    private void ReadKindFields(JsonObject definitionSchema)
    {
        var defs = definitionSchema["$defs"]!.AsObject();
        var baseFields = defs["base"]!["properties"]!.AsObject().Select(p => p.Key).ToList();
        foreach (var kind in DefinitionKinds.All)
        {
            var own = defs[kind]?["properties"]?.AsObject().Select(p => p.Key) ?? [];
            _fieldsByKind[kind] = [.. baseFields, .. own];
        }
    }

    public static IEnumerable<string> SchemaNames() =>
        typeof(SchemaValidator).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("schema/", StringComparison.Ordinal))
            .Select(n => n["schema/".Length..]);

    public static string ReadSchema(string name)
    {
        using var stream = typeof(SchemaValidator).Assembly.GetManifestResourceStream("schema/" + name)
            ?? throw new FileNotFoundException(name);
        return new StreamReader(stream).ReadToEnd();
    }

    public IEnumerable<Diagnostic> ValidateManifest(JsonNode json, string file) =>
        Validate(json, "dimension.schema.json", file, "");

    /// <summary>Validates one definition against its kind's schema (clearer errors than the oneOf over all kinds).</summary>
    public IEnumerable<Diagnostic> ValidateDefinition(JsonObject json, string file, string pointer)
    {
        var kind = json["kind"] is JsonValue k && k.TryGetValue<string>(out var s) ? s : null;
        if (kind is null || !DefinitionKinds.All.Contains(kind))
        {
            return [new(Severity.Error,
                $"'kind' must be one of: {string.Join(", ", DefinitionKinds.All.Order())}", file, pointer)];
        }
        return Validate(json, $"definition.schema.json#/$defs/{kind}", file, pointer, _fieldsByKind[kind]);
    }

    private IEnumerable<Diagnostic> Validate(JsonNode json, string reference, string file, string pointer,
        IReadOnlySet<string>? topLevelFields = null)
    {
        var schema = _byRef.GetOrAdd(reference, r =>
            JsonSchema.FromText($$"""{ "$schema": "https://json-schema.org/draft/2020-12/schema", "$id": "{{BaseUri}}wrapper/{{Uri.EscapeDataString(r)}}", "$ref": "{{BaseUri}}{{r}}" }"""));
        var result = schema.Evaluate(json, _options);
        if (result.IsValid) yield break;

        var seen = new HashSet<string>();
        var errors = new List<Diagnostic>();
        Collect(result, pointer, file, seen, errors, topLevelFields);
        foreach (var e in errors) yield return e;
        if (seen.Count == 0) yield return new(Severity.Error, "does not match the schema", file, pointer);
    }

    /// <summary>
    /// Walks only the failing parts of the result: a branch of an anyOf that failed while another branch passed
    /// isn't an error, so valid nodes are never descended into.
    /// </summary>
    private static void Collect(EvaluationResults node, string pointer, string file, HashSet<string> seen,
        List<Diagnostic> errors, IReadOnlySet<string>? topLevelFields)
    {
        if (node.IsValid) return;
        foreach (var (keyword, rawMessage) in node.Errors ?? new Dictionary<string, string>())
        {
            var message = rawMessage;
            var path = node.InstanceLocation.ToString();
            if (rawMessage.Contains("false schema", StringComparison.Ordinal))
            {
                // additionalProperties/unevaluatedProperties: name the field. A known top-level field only shows up
                // here because another check on the same definition failed, so it's left to that error.
                var field = path[(path.LastIndexOf('/') + 1)..].Replace("~1", "/").Replace("~0", "~");
                var topLevel = path.Count(c => c == '/') == 1;
                if (topLevel && topLevelFields?.Contains(field) == true) continue;
                message = $"unknown field '{field}'";
            }
            if (keyword is "anyOf" or "oneOf" or "allOf" or "$ref") continue;
            // "extends" is the other way to satisfy a required field; saying it's missing only confuses.
            if (keyword == "required" && message.Contains("\"extends\"", StringComparison.Ordinal)) continue;
            var location = pointer + node.InstanceLocation;
            if (seen.Add(location + message)) errors.Add(new(Severity.Error, message, file, location));
        }
        foreach (var child in node.Details) Collect(child, pointer, file, seen, errors, topLevelFields);
    }
}
