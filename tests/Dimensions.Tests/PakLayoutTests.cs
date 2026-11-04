using System.Text.Json.Nodes;
using Dimensions.Pipeline;

namespace Dimensions.Tests;

public class PakLayoutTests
{
    [Fact]
    public void OnlyTheDimensionAndItsOverridesArePacked()
    {
        var cooked = Directory.CreateTempSubdirectory("cooked-").FullName;
        try
        {
            void Touch(string rel) { var p = Path.Combine(cooked, rel); Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllText(p, ""); }
            Touch("Dimensions/Ashfall/Weapons/W.uasset");
            Touch("Dimensions/Ashfall/Weapons/W.uexp");
            Touch("Dimensions/Other/X.uasset");
            Touch("Items/Weapons/Common/Weapon_LongGun_Base.uasset"); // a Rem2Proj stand-in: must stay out
            Touch("Loot/ST_Game.uasset");
            Touch("Loot/ST_Game.uexp");

            var missing = new List<string>();
            var files = PakLayout.Collect(cooked, "/Game/Dimensions/Ashfall", ["/Game/Loot/ST_Game", "/Game/Loot/ST_Gone"], missing.Add);
            var mounts = files.Select(f => f.Mount).ToList();
            Assert.Equal(
            [
                "../../../Remnant2/Content/Dimensions/Ashfall/Weapons/W.uasset",
                "../../../Remnant2/Content/Dimensions/Ashfall/Weapons/W.uexp",
                "../../../Remnant2/Content/Loot/ST_Game.uasset",
                "../../../Remnant2/Content/Loot/ST_Game.uexp",
            ], mounts);
            Assert.Single(missing);
            Assert.Contains("ST_Gone", missing[0]);
            Assert.Contains("\" \"../../../Remnant2/Content/Loot/ST_Game.uexp\"", PakLayout.ResponseFile(files));
        }
        finally
        {
            Directory.Delete(cooked, true);
        }
    }

    [Fact]
    public void SetupCopiesThePluginAndEnablesIt()
    {
        var dir = Directory.CreateTempSubdirectory("proj-").FullName;
        try
        {
            var uproject = Path.Combine(dir, "Remnant2.uproject");
            File.WriteAllText(uproject, """{ "FileVersion": 3, "Plugins": [ { "Name": "PythonScriptPlugin", "Enabled": true } ] }""");
            var plugin = Path.Combine(TestDimension.RepoRoot, "unreal", "Plugins", ProjectSetup.PluginName);
            var changes = ProjectSetup.Install(uproject, plugin);
            Assert.True(File.Exists(Path.Combine(dir, "Plugins", "DimensionsEditor", "DimensionsEditor.uplugin")));
            var plugins = JsonNode.Parse(File.ReadAllText(uproject))!["Plugins"]!.AsArray();
            Assert.Contains(plugins, p => p!["Name"]!.GetValue<string>() == "DimensionsEditor");
            Assert.Contains(plugins, p => p!["Name"]!.GetValue<string>() == "EditorScriptingUtilities");
            Assert.NotEmpty(changes);
            Assert.Empty(ProjectSetup.Install(uproject, plugin)); // second run changes nothing
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
