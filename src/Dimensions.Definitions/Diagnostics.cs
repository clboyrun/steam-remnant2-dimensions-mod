namespace Dimensions.Definitions;

public enum Severity { Info, Warning, Error }

/// <summary>A problem found while loading, validating or planning. File and JSON pointer say where.</summary>
public sealed record Diagnostic(Severity Severity, string Message, string? File = null, string? Pointer = null)
{
    public override string ToString()
    {
        var where = File is null ? "" : Pointer is null or "" ? $"{File}: " : $"{File} {Pointer}: ";
        return $"{Severity.ToString().ToLowerInvariant()}: {where}{Message}";
    }
}

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _items = [];

    public IReadOnlyList<Diagnostic> Items => _items;
    public bool HasErrors => _items.Any(d => d.Severity == Severity.Error);

    public void Error(string message, string? file = null, string? pointer = null) =>
        _items.Add(new(Severity.Error, message, file, pointer));

    public void Warning(string message, string? file = null, string? pointer = null) =>
        _items.Add(new(Severity.Warning, message, file, pointer));

    public void Info(string message, string? file = null, string? pointer = null) =>
        _items.Add(new(Severity.Info, message, file, pointer));

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _items.AddRange(diagnostics);
}
