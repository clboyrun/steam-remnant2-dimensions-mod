using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dimensions.Pipeline;

/// <summary>Prepares the Rem2Proj-based Unreal project for the editor step.</summary>
public static class ProjectSetup
{
    public const string PluginName = "DimensionsEditor";
    private static readonly string[] RequiredPlugins = [PluginName, "PythonScriptPlugin", "EditorScriptingUtilities"];

    /// <summary>
    /// Copies the DimensionsEditor plugin into the project's Plugins folder and enables it, plus the Python and
    /// editor scripting plugins the script uses. Returns what it changed.
    /// </summary>
    public static IReadOnlyList<string> Install(string uproject, string pluginSource)
    {
        var changes = new List<string>();
        var projectDir = Path.GetDirectoryName(Path.GetFullPath(uproject))!;
        var target = Path.Combine(projectDir, "Plugins", PluginName);
        CopyDirectory(pluginSource, target, changes);

        var json = JsonNode.Parse(File.ReadAllText(uproject))!.AsObject();
        if (json["Plugins"] is not JsonArray plugins) json["Plugins"] = plugins = [];
        foreach (var name in RequiredPlugins)
        {
            var entry = plugins.OfType<JsonObject>().FirstOrDefault(p => p["Name"]?.GetValue<string>() == name);
            if (entry is null)
            {
                plugins.Add(new JsonObject { ["Name"] = name, ["Enabled"] = true });
                changes.Add($"enabled {name} in {Path.GetFileName(uproject)}");
            }
            else if (entry["Enabled"]?.GetValue<bool>() != true)
            {
                entry["Enabled"] = true;
                changes.Add($"enabled {name} in {Path.GetFileName(uproject)}");
            }
        }
        if (changes.Any(c => c.Contains(Path.GetFileName(uproject), StringComparison.Ordinal)))
        {
            // Unreal writes .uproject with tabs; keep the diff small.
            var text = json.ToJsonString(new JsonSerializerOptions { WriteIndented = true, IndentCharacter = '\t', IndentSize = 1 });
            File.WriteAllText(uproject, text);
        }
        return changes;
    }

    private static void CopyDirectory(string source, string target, List<string> changes)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var dest = Path.Combine(target, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (File.Exists(dest) && File.ReadAllBytes(dest).AsSpan().SequenceEqual(File.ReadAllBytes(file))) continue;
            File.Copy(file, dest, overwrite: true);
            changes.Add($"copied {rel}");
        }
    }
}
