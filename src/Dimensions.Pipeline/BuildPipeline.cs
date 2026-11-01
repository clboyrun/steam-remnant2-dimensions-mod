using Dimensions.Definitions;

namespace Dimensions.Pipeline;

public sealed record BuildOptions
{
    /// <summary>The Rem2Proj-based Unreal project (Remnant2.uproject).</summary>
    public required string UProject { get; init; }
    /// <summary>UE 5.2 install folder (the one holding Engine/). Found automatically when null.</summary>
    public string? Engine { get; init; }
    /// <summary>Where plan.json, logs and the finished pak go.</summary>
    public required string OutDir { get; init; }
    /// <summary>The editor script folder (unreal/Python in this repo).</summary>
    public required string EditorScripts { get; init; }
    /// <summary>retoc.exe, for IoStore output. Looked up on PATH when null.</summary>
    public string? Retoc { get; init; }
    public bool SkipApply { get; init; }
    public bool SkipCook { get; init; }
    /// <summary>Game Paks folder to copy the result into (~mods). Null leaves it in OutDir.</summary>
    public string? InstallTo { get; init; }
}

/// <summary>
/// The whole build: plan, generate assets in the editor, cook, pack, and optionally install. Every step logs the
/// exact command, so a failing step can be rerun by hand.
/// </summary>
public sealed class BuildPipeline(Action<string> log)
{
    public async Task<IReadOnlyList<string>> RunAsync(DimensionProject project, BuildPlan plan, BuildOptions options,
        CancellationToken cancel = default)
    {
        var engine = options.Engine ?? GameLocator.FindEngine()
            ?? throw new BuildException("Couldn't find Unreal Engine 5.2. Pass --engine <folder holding Engine\\>.");
        var uproject = Path.GetFullPath(options.UProject);
        if (!File.Exists(uproject)) throw new BuildException($"No Unreal project at {uproject}.");
        var projectDir = Path.GetDirectoryName(uproject)!;
        var projectName = Path.GetFileNameWithoutExtension(uproject);
        if (!projectName.Equals(PakLayout.GameProject, StringComparison.OrdinalIgnoreCase))
            log($"warning: the project is '{projectName}', but the game's is '{PakLayout.GameProject}'. Cooked paths only line up if they match.");

        Directory.CreateDirectory(options.OutDir);
        var planFile = Path.Combine(options.OutDir, "plan.json");
        await File.WriteAllTextAsync(planFile, plan.ToJsonString(), cancel);
        log($"Wrote {planFile} ({plan.Operations.Count} operations).");

        var editor = GameLocator.EditorCmd(engine);
        if (!options.SkipApply)
        {
            var script = Path.Combine(Path.GetFullPath(options.EditorScripts), "dimensions_apply.py");
            if (!File.Exists(script)) throw new BuildException($"Editor script not found at {script}.");
            var resultFile = Path.Combine(options.OutDir, "apply-result.json");
            File.Delete(resultFile);
            var code = await ProcessRunner.RunAsync(editor,
                [uproject, "-run=pythonscript", $"-script={script}", "-unattended", "-nop4", "-nosplash", "-stdout", "-FullStdOutLogOutput"],
                log,
                new Dictionary<string, string> { ["DIMENSIONS_PLAN"] = planFile, ["DIMENSIONS_RESULT"] = resultFile },
                cancel);
            if (code != 0 || !File.Exists(resultFile))
                throw new BuildException($"The editor step failed (exit code {code}). The log above has [Dimensions] lines; {resultFile} has details if it was written.");
            var result = await File.ReadAllTextAsync(resultFile, cancel);
            if (result.Contains("\"ok\": false", StringComparison.Ordinal))
                throw new BuildException($"The editor step reported errors; see {resultFile}.");
        }

        if (!options.SkipCook)
        {
            var code = await ProcessRunner.RunAsync(editor,
                [uproject, "-run=cook", "-targetplatform=Windows", "-unversioned", "-iterate", "-unattended", "-nop4", "-stdout", "-FullStdOutLogOutput"],
                log, cancel: cancel);
            if (code != 0) throw new BuildException($"Cooking failed (exit code {code}).");
        }

        var cookedContent = Path.Combine(projectDir, "Saved", "Cooked", "Windows", projectName, "Content");
        var missing = new List<string>();
        var files = PakLayout.Collect(cookedContent, plan.ContentRoot, plan.Overrides, missing.Add);
        foreach (var m in missing) log($"warning: expected cooked file not found: {m}");
        if (files.Count == 0) throw new BuildException($"Nothing cooked under {cookedContent} for {plan.ContentRoot}.");

        var outputs = await PackAsync(engine, plan, options, files, cancel);
        if (options.InstallTo is not null) Install(outputs, options.InstallTo);
        return outputs;
    }

    private async Task<IReadOnlyList<string>> PackAsync(string engine, BuildPlan plan, BuildOptions options,
        IReadOnlyList<(string Source, string Mount)> files, CancellationToken cancel)
    {
        var pakName = plan.Manifest.PakName + "_P";
        var staging = Path.Combine(options.OutDir, "pak");
        Directory.CreateDirectory(staging);
        foreach (var old in Directory.EnumerateFiles(staging, pakName + ".*")) File.Delete(old);

        var unrealPak = GameLocator.UnrealPak(engine);
        var response = Path.Combine(options.OutDir, "pak-files.txt");
        await File.WriteAllTextAsync(response, PakLayout.ResponseFile(files), cancel);
        log($"Packing {files.Count} files ({response}).");

        if (plan.Manifest.PakFormat == PakFormat.Legacy)
        {
            var pak = Path.Combine(staging, pakName + ".pak");
            await Run(unrealPak, [pak, $"-create={response}", "-compressed", "-compressionformats=Oodle", "-platform=Windows"], cancel);
            return [pak];
        }

        // IoStore: pack the cooked files as a legacy pak, convert it to .utoc/.ucas with retoc, and ship a small
        // .pak next to them (the game mounts IoStore containers through a .pak of the same name).
        var legacy = Path.Combine(options.OutDir, pakName + ".legacy.pak");
        await Run(unrealPak, [legacy, $"-create={response}", "-platform=Windows"], cancel);
        var retoc = options.Retoc ?? FindOnPath("retoc.exe") ?? FindOnPath("retoc")
            ?? throw new BuildException("IoStore output needs retoc (https://github.com/trumank/retoc). Put retoc.exe on PATH or pass --retoc, or set pak.format to \"legacy\".");
        var utoc = Path.Combine(staging, pakName + ".utoc");
        await Run(retoc, ["to-zen", legacy, utoc, "--version", "UE5_2"], cancel);

        var marker = Path.Combine(options.OutDir, "dimension.txt");
        await File.WriteAllTextAsync(marker, $"{plan.Manifest.Name} {plan.Manifest.Version} ({plan.Manifest.Id})\n", cancel);
        var markerResponse = Path.Combine(options.OutDir, "pak-marker.txt");
        await File.WriteAllTextAsync(markerResponse,
            PakLayout.ResponseFile([(marker, $"../../../{PakLayout.GameProject}/Content/Dimensions/{plan.Manifest.Id}/dimension.txt")]), cancel);
        var companion = Path.Combine(staging, pakName + ".pak");
        await Run(unrealPak, [companion, $"-create={markerResponse}", "-platform=Windows"], cancel);

        return [.. Directory.EnumerateFiles(staging, pakName + ".*").Order()];
    }

    private async Task Run(string exe, string[] args, CancellationToken cancel)
    {
        var code = await ProcessRunner.RunAsync(exe, args, log, cancel: cancel);
        if (code != 0) throw new BuildException($"{Path.GetFileName(exe)} failed (exit code {code}).");
    }

    private void Install(IEnumerable<string> outputs, string paksFolder)
    {
        var mods = Path.Combine(paksFolder, "~mods");
        Directory.CreateDirectory(mods);
        foreach (var file in outputs)
        {
            var dest = Path.Combine(mods, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
            log($"Installed {dest}");
        }
    }

    private static string? FindOnPath(string exe) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir, exe))
            .FirstOrDefault(File.Exists);
}

public sealed class BuildException(string message) : Exception(message);
