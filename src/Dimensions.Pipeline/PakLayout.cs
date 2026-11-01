using Dimensions.Definitions;

namespace Dimensions.Pipeline;

/// <summary>
/// Which cooked files go in the pak, and where they mount. Only the dimension's own folder and the game assets
/// it overrides are packed; everything else in the project (Rem2Proj's stand-ins for game assets) stays out, so
/// the game keeps loading its own copies.
/// </summary>
public static class PakLayout
{
    /// <summary>The game's project name: cooked content mounts at ../../../Remnant2/Content/.</summary>
    public const string GameProject = "Remnant2";

    public static readonly string[] PackageExtensions = [".uasset", ".uexp", ".ubulk", ".uptnl", ".umap"];

    /// <summary>
    /// Pairs of (cooked file on disk, path inside the pak). <paramref name="cookedContent"/> is
    /// Saved/Cooked/Windows/&lt;project&gt;/Content.
    /// </summary>
    public static IReadOnlyList<(string Source, string Mount)> Collect(string cookedContent, string contentRoot,
        IEnumerable<string> overridePackages, Action<string>? missing = null)
    {
        var files = new List<(string, string)>();
        var rootRel = ContentRelative(contentRoot);
        var rootDir = Path.Combine(cookedContent, rootRel);
        if (Directory.Exists(rootDir))
        {
            foreach (var f in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                files.Add((f, Mount(cookedContent, f)));
        }
        else
        {
            missing?.Invoke(rootDir);
        }

        foreach (var package in overridePackages)
        {
            var rel = ContentRelative(GamePath.PackageOf(package));
            var found = false;
            foreach (var ext in PackageExtensions)
            {
                var f = Path.Combine(cookedContent, rel + ext);
                if (!File.Exists(f)) continue;
                files.Add((f, Mount(cookedContent, f)));
                found = true;
            }
            if (!found) missing?.Invoke(Path.Combine(cookedContent, rel + ".uasset"));
        }
        return files;
    }

    /// <summary>"/Game/Dimensions/X" -> "Dimensions/X" (relative to Content).</summary>
    public static string ContentRelative(string gamePackage)
    {
        if (!gamePackage.StartsWith("/Game/", StringComparison.Ordinal))
            throw new ArgumentException($"{gamePackage} isn't under /Game/", nameof(gamePackage));
        return gamePackage["/Game/".Length..].Replace('/', Path.DirectorySeparatorChar);
    }

    private static string Mount(string cookedContent, string file) =>
        $"../../../{GameProject}/Content/{Path.GetRelativePath(cookedContent, file).Replace('\\', '/')}";

    /// <summary>UnrealPak's response file: one "source" "mount" line per file.</summary>
    public static string ResponseFile(IEnumerable<(string Source, string Mount)> files) =>
        string.Join(Environment.NewLine, files.Select(f => $"\"{f.Source}\" \"{f.Mount}\"")) + Environment.NewLine;
}
