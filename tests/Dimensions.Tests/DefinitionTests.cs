using System.Text.Json.Nodes;
using Dimensions.Definitions;

namespace Dimensions.Tests;

public class DefinitionTests
{
    private static string Errors(DimensionProject p) =>
        string.Join("\n", p.Diagnostics.Items.Where(d => d.Severity == Severity.Error));

    [Fact]
    public void ExampleDimensionIsValid()
    {
        var project = DimensionProject.Load(Path.Combine(TestDimension.RepoRoot, "examples", "Ashfall"));
        Assert.False(project.Diagnostics.HasErrors, Errors(project));
        Assert.Equal("Ashfall", project.Manifest.Id);
        Assert.Equal(7101, project.Manifest.ChunkId);
        Assert.Contains(project.Definitions, d => d.Kind == "adventure");
    }

    [Fact]
    public void ManifestDefaults()
    {
        using var t = new TestDimension("Plain");
        var m = t.Load().Manifest;
        Assert.Equal("zz_Dimensions_Plain", m.PakName);
        Assert.Equal(PakFormat.IoStore, m.PakFormat);
        Assert.Equal("en", m.DefaultCulture);
        Assert.Equal(["content/**/*.json"], m.Content);
    }

    [Fact]
    public void SchemaErrorsPointAtTheField()
    {
        using var t = new TestDimension().Content("bad.json", """
            { "definitions": [
              { "kind": "ring", "id": "NoTemplate" },
              { "kind": "ring", "id": "lowercase", "template": "/Game/X.X_C" },
              { "kind": "spaceship", "id": "Nope" },
              { "kind": "weapon", "id": "Typo", "weaponType": "longGun", "dammage": 5 }
            ] }
            """);
        var errors = t.Load().Diagnostics.Items.Where(d => d.Severity == Severity.Error).ToList();
        Assert.Contains(errors, e => e.Pointer == "/definitions/0" && e.Message.Contains("template"));
        Assert.Contains(errors, e => e.Pointer == "/definitions/1/id");
        Assert.Contains(errors, e => e.Pointer == "/definitions/2" && e.Message.Contains("'kind' must be one of"));
        Assert.Contains(errors, e => e.Pointer!.StartsWith("/definitions/3") && e.Message.Contains("dammage"));
        // The "or use extends" branch of a required check isn't reported.
        Assert.DoesNotContain(errors, e => e.Message.Contains("\"extends\""));
    }

    [Fact]
    public void ValidAlternativesAreNotReported()
    {
        // "name" as a plain string and "loot" as an id are valid branches of anyOf; nothing should be reported.
        using var t = new TestDimension().Content("w.json", """
            { "definitions": [
              { "kind": "lootTable", "id": "Drops", "entries": [] },
              { "kind": "world", "id": "Place", "name": "Place", "loot": "Drops", "zones": [ { "id": "One", "name": "One" } ] }
            ] }
            """);
        var project = t.Load();
        Assert.False(project.Diagnostics.HasErrors, Errors(project));
    }

    [Fact]
    public void ExtendsMergesObjectsAndReplacesArrays()
    {
        using var t = new TestDimension().Content("guns.json", """
            { "definitions": [
              { "kind": "weapon", "id": "Base", "abstract": true, "weaponType": "longGun",
                "stats": { "damage": 10, "range": 30 }, "properties": { "Tags": ["a", "b"] } },
              { "kind": "weapon", "id": "Child", "extends": "Base", "stats": { "damage": 50 }, "properties": { "Tags": ["c"] } }
            ] }
            """);
        var project = t.Load();
        Assert.False(project.Diagnostics.HasErrors, Errors(project));
        var child = project.Find("Child")!;
        Assert.Equal(50, child.Json["stats"]!["damage"]!.GetValue<int>());
        Assert.Equal(30, child.Json["stats"]!["range"]!.GetValue<int>());
        Assert.Equal("longGun", child.GetString("weaponType"));
        Assert.Single(child.Json["properties"]!["Tags"]!.AsArray());
        Assert.False(child.IsAbstract);
        Assert.DoesNotContain(project.Concrete, d => d.Id == "Base");
    }

    [Fact]
    public void ExtendsLoopsAndKindMismatchesAreErrors()
    {
        using var t = new TestDimension().Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "A", "extends": "B", "template": "/Game/R.R_C" },
              { "kind": "ring", "id": "B", "extends": "A", "template": "/Game/R.R_C" },
              { "kind": "amulet", "id": "C", "extends": "A", "template": "/Game/R.R_C" },
              { "kind": "amulet", "id": "D", "extends": "Missing", "template": "/Game/R.R_C" }
            ] }
            """);
        var errors = Errors(t.Load());
        Assert.Contains("loops", errors);
        Assert.Contains("names a ring, but this is a amulet", errors);
        Assert.Contains("'Missing', which isn't defined", errors);
    }

    [Fact]
    public void ExtendsSuppliesRequiredFields()
    {
        using var t = new TestDimension().Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "RingBase", "abstract": true, "template": "/Game/R.R_C" },
              { "kind": "ring", "id": "Real", "extends": "RingBase" },
              { "kind": "ring", "id": "Bad", "extends": "Other" },
              { "kind": "ring", "id": "Other", "abstract": true }
            ] }
            """);
        var errors = t.Load().Diagnostics.Items.Where(d => d.Severity == Severity.Error).ToList();
        Assert.DoesNotContain(errors, e => e.Pointer == "/definitions/1");
        Assert.Contains(errors, e => e.Pointer == "/definitions/2" && e.Message.Contains("after applying 'extends'"));
    }

    [Fact]
    public void ReferencesFilesCulturesAndRangesAreChecked()
    {
        using var t = new TestDimension().File_("art/ok.png").Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "Ring1", "template": "/Game/R.R_C", "icon": { "small": "art/ok.png", "large": "art/missing.png" },
                "name": { "de": "Ring" } },
              { "kind": "lootTable", "id": "Drops", "entries": [
                { "item": "Ring1" }, { "item": "Ghost" }, { "item": "Drops" }, { "item": "Ring1", "levelMin": 9, "levelMax": 2 } ] },
              { "kind": "ring", "id": "Ring1", "template": "/Game/R.R_C" },
              { "kind": "archetype", "id": "Arch", "startingTraits": [ { "trait": "Ring1" } ] }
            ] }
            """);
        var errors = Errors(t.Load());
        Assert.Contains("file 'art/missing.png' not found", errors);
        Assert.DoesNotContain("art/ok.png", errors);
        Assert.Contains("no 'en' entry", errors);
        Assert.Contains("'Ghost' isn't defined", errors);
        Assert.Contains("'Drops' is a lootTable; expected an item", errors);
        Assert.Contains("levelMin 9 is above levelMax 2", errors);
        Assert.Contains("id 'Ring1' is already used", errors);
        Assert.Contains("'Ring1' is a ring; expected trait", errors);
    }

    [Fact]
    public void RawPropertyRefsAreChecked()
    {
        using var t = new TestDimension().Content("x.json", """
            { "kind": "blueprint", "id": "Thing", "template": "/Script/Engine.Actor", "properties": { "Other": { "$ref": "Nobody" } } }
            """);
        Assert.Contains("'Nobody' isn't defined", Errors(t.Load()));
    }

    [Fact]
    public void BadJsonIsReported()
    {
        using var t = new TestDimension().Content("broken.json", "{ \"kind\": ");
        Assert.Contains("not valid JSON", Errors(t.Load()));
    }

    [Fact]
    public void CommentsAndTrailingCommasAreAllowed()
    {
        using var t = new TestDimension().Content("x.json", """
            // a ring
            { "kind": "ring", "id": "Commented", "template": "/Game/R.R_C", }
            """);
        var project = t.Load();
        Assert.False(project.Diagnostics.HasErrors, Errors(project));
    }

    [Fact]
    public void MergeRemovesKeysSetToNull()
    {
        var merged = JsonMerge.Merge(
            JsonNode.Parse("""{ "a": 1, "b": { "c": 2, "d": 3 } }""")!.AsObject(),
            JsonNode.Parse("""{ "a": null, "b": { "d": 4 } }""")!.AsObject());
        Assert.Equal("""{"b":{"c":2,"d":4}}""", merged.ToJsonString());
    }

    [Theory]
    [InlineData("/Game/A/B.B_C", "/Game/A/B")]
    [InlineData("/Game/A/B", "/Game/A/B")]
    [InlineData("/Script/Remnant.ZoneDef", "/Script/Remnant.ZoneDef")]
    public void PackageOf(string path, string expected) => Assert.Equal(expected, GamePath.PackageOf(path));

    [Fact]
    public void ObjectAndClassPaths()
    {
        Assert.Equal("/Game/A/B.B", GamePath.ObjectPath("/Game/A/B"));
        Assert.Equal("/Game/A/B.B_C", GamePath.ClassPath("/Game/A/B"));
        Assert.Equal("/Game/A/B.B_C", GamePath.ClassPath("/Game/A/B.B"));
    }
}
