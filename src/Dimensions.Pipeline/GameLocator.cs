using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Dimensions.Pipeline;

/// <summary>Finds Remnant 2 (through Steam's library list) and an installed Unreal Engine 5.2.</summary>
public static partial class GameLocator
{
    [GeneratedRegex("\"path\"\\s+\"([^\"]+)\"")]
    private static partial Regex LibraryPathRegex();

    /// <summary>The game's Paks folder: &lt;library&gt;/steamapps/common/Remnant2/Remnant2/Content/Paks.</summary>
    public static string? FindPaksFolder()
    {
        foreach (var library in SteamLibraries())
        {
            var paks = Path.Combine(library, "steamapps", "common", "Remnant2", "Remnant2", "Content", "Paks");
            if (Directory.Exists(paks)) return paks;
        }
        return null;
    }

    /// <summary>Accepts the game folder, its inner Remnant2 folder, Content, or Paks, and returns Paks.</summary>
    public static string? NormalizePaksFolder(string folder)
    {
        string[] candidates =
        [
            folder,
            Path.Combine(folder, "Paks"),
            Path.Combine(folder, "Content", "Paks"),
            Path.Combine(folder, "Remnant2", "Content", "Paks"),
        ];
        return candidates.FirstOrDefault(c => Directory.Exists(c) && Directory.EnumerateFiles(c, "*.pak").Any());
    }

    /// <summary>
    /// The UE 5.2 install folder: from the Epic launcher's install list, the registry, or the default location.
    /// </summary>
    public static string? FindEngine(string version = "5.2")
    {
        var candidates = new List<string>();
        var launcherList = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "UnrealEngineLauncher", "LauncherInstalled.dat");
        if (File.Exists(launcherList))
        {
            try
            {
                var json = JsonNode.Parse(File.ReadAllText(launcherList));
                foreach (var entry in json?["InstallationList"]?.AsArray() ?? [])
                {
                    if (entry?["AppName"]?.GetValue<string>() == $"UE_{version}" && entry["InstallLocation"]?.GetValue<string>() is { } location)
                        candidates.Add(location);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // A damaged launcher file just means we look elsewhere.
            }
        }
        if (OperatingSystem.IsWindows() &&
            Registry.GetValue($@"HKEY_LOCAL_MACHINE\SOFTWARE\EpicGames\Unreal Engine\{version}", "InstalledDirectory", null) is string installed)
        {
            candidates.Add(installed);
        }
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", $"UE_{version}"));
        return candidates.FirstOrDefault(c => File.Exists(EditorCmd(c)));
    }

    public static string EditorCmd(string engine) => Path.Combine(engine, "Engine", "Binaries", "Win64", "UnrealEditor-Cmd.exe");
    public static string UnrealPak(string engine) => Path.Combine(engine, "Engine", "Binaries", "Win64", "UnrealPak.exe");

    private static IEnumerable<string> SteamLibraries()
    {
        var roots = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is string userPath)
                roots.Add(userPath.Replace('/', '\\'));
            if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) is string machinePath)
                roots.Add(machinePath);
        }
        roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots.Where(Directory.Exists))
        {
            if (seen.Add(root)) yield return root;
            var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            foreach (Match m in LibraryPathRegex().Matches(File.ReadAllText(vdf)))
            {
                var library = m.Groups[1].Value.Replace(@"\\", @"\");
                if (seen.Add(library)) yield return library;
            }
        }
    }
}
