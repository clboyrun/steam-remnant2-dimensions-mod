using System.Diagnostics;

namespace Dimensions.Pipeline;

public static class ProcessRunner
{
    /// <summary>Runs a tool, streaming its output to <paramref name="log"/>. Returns the exit code.</summary>
    public static async Task<int> RunAsync(string exe, IEnumerable<string> args, Action<string> log,
        IReadOnlyDictionary<string, string>? environment = null, CancellationToken cancel = default)
    {
        var info = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) info.ArgumentList.Add(a);
        foreach (var (k, v) in environment ?? new Dictionary<string, string>()) info.Environment[k] = v;

        log($"> {Path.GetFileName(exe)} {string.Join(' ', info.ArgumentList.Select(Quote))}");
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"couldn't start {exe}");
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) log(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) log(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancel);
        return process.ExitCode;
    }

    private static string Quote(string a) => a.Contains(' ') ? $"\"{a}\"" : a;
}
