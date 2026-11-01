using System.Text.Json.Nodes;
using Dimensions.Definitions;

namespace Dimensions.Tests;

/// <summary>A throwaway dimension folder for one test.</summary>
internal sealed class TestDimension : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("dimensions-test-").FullName;

    public TestDimension(string id = "Testing", string? manifestExtra = null)
    {
        File.WriteAllText(Path.Combine(Root, "dimension.json"),
            $$"""{ "id": "{{id}}", "name": "{{id}}", "version": "1.0.0" {{manifestExtra}} }""");
    }

    public TestDimension Content(string relative, string json)
    {
        var path = Path.Combine(Root, "content", relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return this;
    }

    public TestDimension File_(string relative, string text = "x")
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return this;
    }

    public DimensionProject Load() => DimensionProject.Load(Root);

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); } catch (IOException) { }
    }

    public static string RepoRoot
    {
        get
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "Dimensions.slnx"))) return dir.FullName;
            throw new InvalidOperationException("repo root not found");
        }
    }

    public static JsonObject Obj(JsonNode? n) => Assert.IsType<JsonObject>(n);
}
