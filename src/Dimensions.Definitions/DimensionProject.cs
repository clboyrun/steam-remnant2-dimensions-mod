using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Dimensions.Definitions;

/// <summary>A loaded dimension: the manifest, every definition (with 'extends' applied) and what went wrong.</summary>
public sealed class DimensionProject
{
    private readonly Dictionary<string, Definition> _byId;

    private DimensionProject(string root, DimensionManifest manifest, List<Definition> definitions, DiagnosticBag diagnostics)
    {
        Root = root;
        Manifest = manifest;
        Definitions = definitions;
        Diagnostics = diagnostics;
        _byId = definitions.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
    }

    public string Root { get; }
    public DimensionManifest Manifest { get; }
    public IReadOnlyList<Definition> Definitions { get; }
    public DiagnosticBag Diagnostics { get; }

    /// <summary>Definitions that produce content (not abstract bases).</summary>
    public IEnumerable<Definition> Concrete => Definitions.Where(d => !d.IsAbstract);

    public Definition? Find(string id) => _byId.GetValueOrDefault(id);

    /// <summary>A file path from a definition, resolved against the dimension root.</summary>
    public string ResolveFile(string relative) => Path.GetFullPath(Path.Combine(Root, relative));

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Loads dimension.json (or the folder holding it) and every content file it lists, then validates.</summary>
    public static DimensionProject Load(string path, SchemaValidator? validator = null)
    {
        validator ??= new SchemaValidator();
        var diagnostics = new DiagnosticBag();
        var manifestFile = Directory.Exists(path) ? Path.Combine(path, DimensionManifest.FileName) : path;
        manifestFile = Path.GetFullPath(manifestFile);
        var root = Path.GetDirectoryName(manifestFile)!;

        var manifestJson = ReadJson(manifestFile, diagnostics) as JsonObject;
        if (manifestJson is null)
        {
            if (!diagnostics.HasErrors) diagnostics.Error("the manifest must be a JSON object", manifestFile);
            return new(root, DimensionManifest.FromJson([]), [], diagnostics);
        }
        diagnostics.AddRange(validator.ValidateManifest(manifestJson, Relative(root, manifestFile)));
        var manifest = DimensionManifest.FromJson(manifestJson);

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(manifest.Content);
        matcher.AddExclude(DimensionManifest.FileName);
        var files = matcher.GetResultsInFullPath(root).Order(StringComparer.Ordinal).ToList();
        if (files.Count == 0) diagnostics.Warning($"no content files match {string.Join(", ", manifest.Content)}", Relative(root, manifestFile));

        var definitions = new List<Definition>();
        foreach (var file in files)
        {
            var rel = Relative(root, file);
            var json = ReadJson(file, diagnostics);
            if (json is null) continue;
            foreach (var (obj, pointer) in Split(json, rel, diagnostics))
            {
                var schemaErrors = validator.ValidateDefinition(obj, rel, pointer).ToList();
                diagnostics.AddRange(schemaErrors);
                var kind = obj["kind"] is JsonValue k && k.TryGetValue<string>(out var ks) ? ks : null;
                var id = obj["id"] is JsonValue i && i.TryGetValue<string>(out var ids) ? ids : null;
                if (kind is null || id is null) continue;
                definitions.Add(new Definition(kind, id, obj, rel, pointer));
            }
        }

        ResolveExtends(definitions, diagnostics);
        var project = new DimensionProject(root, manifest, definitions, diagnostics);
        diagnostics.AddRange(SemanticValidator.Validate(project, validator));
        return project;
    }

    private static IEnumerable<(JsonObject, string)> Split(JsonNode json, string file, DiagnosticBag diagnostics)
    {
        if (json is JsonObject { } single && single["definitions"] is JsonArray many)
        {
            for (var i = 0; i < many.Count; i++)
            {
                if (many[i] is JsonObject o) yield return (o, $"/definitions/{i}");
                else diagnostics.Error("each entry in 'definitions' must be an object", file, $"/definitions/{i}");
            }
        }
        else if (json is JsonObject o)
        {
            yield return (o, "");
        }
        else
        {
            diagnostics.Error("a content file must hold an object", file);
        }
    }

    private static void ResolveExtends(List<Definition> definitions, DiagnosticBag diagnostics)
    {
        var byId = definitions.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
        var done = new HashSet<Definition>();

        JsonObject Resolve(Definition d, List<string> chain)
        {
            if (done.Contains(d)) return d.Json;
            var parentId = d.Extends;
            if (parentId is not null)
            {
                if (chain.Contains(parentId))
                {
                    diagnostics.Error($"'extends' loops: {string.Join(" -> ", chain.Append(parentId))}", d.File, d.Pointer + "/extends");
                }
                else if (!byId.TryGetValue(parentId, out var parent))
                {
                    diagnostics.Error($"'extends' names '{parentId}', which isn't defined", d.File, d.Pointer + "/extends");
                }
                else if (parent.Kind != d.Kind)
                {
                    diagnostics.Error($"'extends' names a {parent.Kind}, but this is a {d.Kind}", d.File, d.Pointer + "/extends");
                }
                else
                {
                    var parentJson = (JsonObject)Resolve(parent, [.. chain, parentId]).DeepClone();
                    parentJson.Remove("abstract"); // a concrete child of an abstract base is concrete
                    parentJson.Remove("assetName");
                    d.Json = JsonMerge.Merge(parentJson, d.Raw);
                }
            }
            done.Add(d);
            return d.Json;
        }

        foreach (var d in definitions) Resolve(d, [d.Id]);
    }

    private static JsonNode? ReadJson(string file, DiagnosticBag diagnostics)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(file), documentOptions: JsonOptions);
        }
        catch (JsonException e)
        {
            diagnostics.Error($"not valid JSON: {e.Message}", file);
        }
        catch (IOException e)
        {
            diagnostics.Error($"can't read: {e.Message}", file);
        }
        return null;
    }

    private static string Relative(string root, string file) => Path.GetRelativePath(root, file).Replace('\\', '/');
}
