using Dimensions.Definitions;
using Dimensions.Pipeline;

namespace Dimensions.Cli;

public static class Program
{
    private const string Usage = """
        dimensions: build Remnant 2 content mods from JSON definitions.

          dimensions validate <dimension>            Check every definition (schema, references, files).
          dimensions plan <dimension> [-o plan.json] Show what the editor step will create and which game assets it needs.
          dimensions build <dimension> --project <Remnant2.uproject> [options]
              --engine <UE 5.2 folder>  Default: found through the Epic launcher.
              --out <folder>            Default: <dimension>/out
              --retoc <retoc.exe>       Needed for IoStore output. Default: from PATH.
              --install                 Copy the pak into the game's Paks/~mods folder.
              --game <game folder>      Where the game is, if Steam can't tell.
              --skip-apply, --skip-cook Rerun only the later steps.
          dimensions setup --project <Remnant2.uproject>
              Copies the DimensionsEditor plugin into the project and enables the plugins the editor step uses.
              Build the project once in Visual Studio afterwards (Development Editor, Win64).
          dimensions new <folder> --id <Id>          Start a new dimension from the template.
          dimensions schema <folder>                 Write the JSON schemas (for editor autocomplete).

        <dimension> is a folder with dimension.json, or the file itself.
        --project and --engine can also come from DIMENSIONS_UPROJECT and DIMENSIONS_ENGINE.
        """;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 1 : 0;
        }
        try
        {
            var options = Options.Parse(args[1..]);
            return args[0] switch
            {
                "validate" => Validate(options),
                "plan" => Plan(options),
                "build" => await Build(options),
                "new" => New(options),
                "setup" => Setup(options),
                "schema" => Schema(options),
                _ => Fail($"unknown command '{args[0]}'.\n\n{Usage}"),
            };
        }
        catch (BuildException e)
        {
            return Fail(e.Message);
        }
        catch (ArgumentException e)
        {
            return Fail(e.Message);
        }
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        return 1;
    }

    private static DimensionProject? Load(Options options)
    {
        var path = options.Positional.FirstOrDefault() ?? ".";
        var project = DimensionProject.Load(path);
        foreach (var d in project.Diagnostics.Items) (d.Severity == Severity.Error ? Console.Error : Console.Out).WriteLine(d);
        if (project.Diagnostics.HasErrors)
        {
            Console.Error.WriteLine($"{project.Diagnostics.Items.Count(d => d.Severity == Severity.Error)} error(s).");
            return null;
        }
        return project;
    }

    private static int Validate(Options options)
    {
        var project = Load(options);
        if (project is null) return 1;
        var kinds = project.Concrete.GroupBy(d => d.Kind).Select(g => $"{g.Count()} {g.Key}").ToList();
        Console.WriteLine($"{project.Manifest.Name} {project.Manifest.Version}: OK ({string.Join(", ", kinds)}).");
        return 0;
    }

    private static int Plan(Options options)
    {
        var project = Load(options);
        if (project is null) return 1;
        var plan = Planner.Plan(project, LoadCatalog(options));
        var output = options.Value("-o") ?? options.Value("--out");
        if (output is not null)
        {
            File.WriteAllText(output, plan.ToJsonString());
            Console.WriteLine($"Wrote {output}.");
        }
        else
        {
            Console.WriteLine(plan.ToJsonString());
        }
        PrintSummary(plan);
        return 0;
    }

    private static void PrintSummary(BuildPlan plan)
    {
        Console.WriteLine();
        foreach (var group in plan.Operations.GroupBy(o => o["op"]!.GetValue<string>()))
            Console.WriteLine($"  {group.Count(),4} {group.Key}");
        if (plan.RequiredAssets.Count > 0)
        {
            Console.WriteLine("\nThese game assets must be in the Unreal project before the editor step (Rem2Proj has some; import the rest with FModel + JsonAsAsset):");
            foreach (var a in plan.RequiredAssets) Console.WriteLine($"  {a}");
        }
        if (plan.Overrides.Count > 0)
        {
            Console.WriteLine("\nThese game assets are changed and go in the pak (they replace the game's copy):");
            foreach (var a in plan.Overrides) Console.WriteLine($"  {a}");
        }
        if (plan.Notes.Count > 0)
        {
            Console.WriteLine("\nNotes:");
            foreach (var n in plan.Notes) Console.WriteLine($"  - {n}");
        }
    }

    private static Catalog LoadCatalog(Options options) => Catalog.Load(options.Value("--catalog"));

    private static async Task<int> Build(Options options)
    {
        var project = Load(options);
        if (project is null) return 1;
        var plan = Planner.Plan(project, LoadCatalog(options));
        PrintSummary(plan);

        var uproject = options.Value("--project") ?? Environment.GetEnvironmentVariable("DIMENSIONS_UPROJECT")
            ?? throw new ArgumentException("pass --project <path to Remnant2.uproject> (or set DIMENSIONS_UPROJECT).");
        string? install = null;
        if (options.Flag("--install"))
        {
            var game = options.Value("--game");
            install = (game is null ? GameLocator.FindPaksFolder() : GameLocator.NormalizePaksFolder(game))
                ?? throw new ArgumentException("couldn't find the game's Paks folder; pass --game <Remnant 2 folder>.");
        }

        var build = new BuildOptions
        {
            UProject = uproject,
            Engine = options.Value("--engine") ?? Environment.GetEnvironmentVariable("DIMENSIONS_ENGINE"),
            OutDir = Path.GetFullPath(options.Value("--out") ?? Path.Combine(project.Root, "out")),
            EditorScripts = FindEditorScripts(),
            Retoc = options.Value("--retoc"),
            SkipApply = options.Flag("--skip-apply"),
            SkipCook = options.Flag("--skip-cook"),
            InstallTo = install,
        };
        var outputs = await new BuildPipeline(Console.WriteLine).RunAsync(project, plan, build);
        Console.WriteLine($"\nBuilt {string.Join(", ", outputs.Select(Path.GetFileName))} in {Path.GetDirectoryName(outputs[0])}.");
        if (install is null) Console.WriteLine("Copy them to Remnant2\\Content\\Paks\\~mods (or rerun with --install). The game also needs a pak signature bypass (\"Allow Asset Mods\").");
        return 0;
    }

    private static string FindEditorScripts()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "unreal", "Python");
                if (File.Exists(Path.Combine(candidate, "dimensions_apply.py"))) return candidate;
            }
        }
        throw new BuildException("Couldn't find unreal/Python/dimensions_apply.py next to the CLI or in a parent folder.");
    }

    private static int Setup(Options options)
    {
        var uproject = options.Value("--project") ?? Environment.GetEnvironmentVariable("DIMENSIONS_UPROJECT")
            ?? throw new ArgumentException("pass --project <path to Remnant2.uproject>.");
        if (!File.Exists(uproject)) throw new ArgumentException($"no project at {uproject}.");
        var plugin = Path.Combine(Path.GetDirectoryName(FindEditorScripts())!, "Plugins", ProjectSetup.PluginName);
        if (!Directory.Exists(plugin)) throw new BuildException($"plugin source not found at {plugin}.");
        var changes = ProjectSetup.Install(uproject, plugin);
        foreach (var c in changes) Console.WriteLine($"  {c}");
        Console.WriteLine(changes.Count == 0
            ? "The project is already set up."
            : "Done. Rebuild the project in Visual Studio (Development Editor, Win64) so the plugin compiles.");
        return 0;
    }

    private static int New(Options options)
    {
        var folder = options.Positional.FirstOrDefault() ?? throw new ArgumentException("give a folder: dimensions new <folder> --id <Id>");
        var id = options.Value("--id") ?? throw new ArgumentException("give an id: --id MyDimension (PascalCase)");
        if (!GamePath.IsId(id)) throw new ArgumentException($"'{id}' isn't a valid id (PascalCase letters, digits, _).");
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new ArgumentException($"{folder} isn't empty.");
        Scaffold.Write(folder, id);
        Console.WriteLine($"Created {folder}. Next: dimensions validate {folder}");
        return 0;
    }

    private static int Schema(Options options)
    {
        var folder = options.Positional.FirstOrDefault() ?? "schema";
        Scaffold.WriteSchemas(folder);
        Console.WriteLine($"Wrote the schemas to {folder}.");
        return 0;
    }
}

internal sealed class Options
{
    private readonly Dictionary<string, string?> _named = [];
    public List<string> Positional { get; } = [];

    private static readonly HashSet<string> Flags = ["--install", "--skip-apply", "--skip-cook"];

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (Flags.Contains(a)) o._named[a] = null;
            else if (a.StartsWith('-'))
            {
                if (i + 1 >= args.Length) throw new ArgumentException($"{a} needs a value.");
                o._named[a] = args[++i];
            }
            else o.Positional.Add(a);
        }
        return o;
    }

    public string? Value(string name) => _named.GetValueOrDefault(name);
    public bool Flag(string name) => _named.ContainsKey(name);
}
