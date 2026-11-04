using System.Text.Json.Nodes;
using Dimensions.Definitions;
using Dimensions.Pipeline;

namespace Dimensions.Tests;

public class PlannerTests
{
    private static BuildPlan PlanExample()
    {
        var project = DimensionProject.Load(Path.Combine(TestDimension.RepoRoot, "examples", "Ashfall"));
        Assert.False(project.Diagnostics.HasErrors);
        return Planner.Plan(project);
    }

    private static JsonObject Op(BuildPlan plan, string op, string assetSuffix) =>
        plan.Operations.Single(o => o["op"]!.GetValue<string>() == op && o["asset"]!.GetValue<string>().EndsWith(assetSuffix, StringComparison.Ordinal));

    private static BuildPlan Plan(TestDimension t)
    {
        var project = t.Load();
        Assert.False(project.Diagnostics.HasErrors, string.Join("\n", project.Diagnostics.Items));
        return Planner.Plan(project);
    }

    [Fact]
    public void WeaponBecomesAChildBlueprintWithItsOwnStatsTable()
    {
        var plan = PlanExample();
        var weapon = Op(plan, "createBlueprint", "/Weapons/Emberline/Weapon_Emberline");
        Assert.Equal("/Game/Items/Weapons/Common/Weapon_LongGun_Base.Weapon_LongGun_Base_C", weapon["parent"]!.GetValue<string>());

        var props = weapon["properties"]!;
        Assert.Equal("Emberline", props["Label"]!["$text"]!["en"]!.GetValue<string>());
        Assert.Equal("Glutlinie", props["Label"]!["$text"]!["de"]!.GetValue<string>());
        Assert.Equal("/Game/Dimensions/Ashfall/Weapons/Emberline/T_Weapon_Emberline_Icon.T_Weapon_Emberline_Icon", props["Icon"]!["$asset"]!.GetValue<string>());
        Assert.Equal("/Game/Dimensions/Ashfall/Weapons/Emberline/DT_Weapon_Emberline_Stats.DT_Weapon_Emberline_Stats", props["Stats"]!["$rowHandle"]!["table"]!.GetValue<string>());

        var table = Op(plan, "createDataTable", "DT_Weapon_Emberline_Stats");
        Assert.Equal("/Script/GunfireRuntime.RangedWeaponData", table["rowStruct"]!.GetValue<string>());
        var row = table["rows"]!["Weapon_Emberline"]!;
        Assert.Equal(7, row["AmmoPerClip"]!.GetValue<int>());
        Assert.Equal(45, row["Range"]!.GetValue<int>()); // from the abstract base
        Assert.False(row["IsAutomatic"]!.GetValue<bool>());

        var slots = props["ModSlots"]!.AsArray();
        Assert.Equal("/Game/Dimensions/Ashfall/Mods/CinderBurst/Mod_CinderBurst.Mod_CinderBurst_C", slots[0]!["$struct"]!["DefaultMod"]!["$class"]!.GetValue<string>());
        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public void ModelImportsMeshMaterialsAndTextures()
    {
        var plan = PlanExample();
        var mesh = Op(plan, "importMesh", "SM_Weapon_Emberline");
        Assert.Equal("static", mesh["meshType"]!.GetValue<string>());
        Assert.True(Path.IsPathRooted(mesh["source"]!.GetValue<string>()));
        var mi = Op(plan, "createMaterialInstance", "MI_Weapon_Emberline_0");
        Assert.EndsWith("T_Weapon_Emberline_BaseColor", mi["textures"]!["BaseColor"]!.GetValue<string>());
        var component = Op(plan, "createBlueprint", "Weapon_Emberline")["components"]!["Mesh"]!;
        Assert.EndsWith("SM_Weapon_Emberline", component["$mesh"]!["$asset"]!.GetValue<string>());
        Assert.Single(component["$materials"]!.AsArray());
        Assert.Equal("world", Op(plan, "importTexture", "T_Weapon_Emberline_Normal")["usage"]!.GetValue<string>());
        Assert.Equal("ui", Op(plan, "importTexture", "T_Weapon_Emberline_Icon")["usage"]!.GetValue<string>());
    }

    [Fact]
    public void ItemsDroppingFromTheDimensionsTableGoStraightIntoIt()
    {
        var plan = PlanExample();
        var table = Op(plan, "createObject", "ST_AshfallLoot");
        var items = table["properties"]!["Entries"]!.AsArray()
            .Select(e => e!["properties"]!["ItemBP"]!["$softClass"]!.GetValue<string>()).ToList();
        Assert.Contains(items, i => i.EndsWith("Amulet_EmberHeart_C"));
        Assert.Contains(items, i => i.EndsWith("Weapon_Emberline_C"));
        Assert.Contains(items, i => i.EndsWith("MetaGem_Kindling_C"));
        Assert.Contains(items, i => i.EndsWith("Ring_AshenSeal_C"));
        Assert.Contains(items, i => i.EndsWith("Engram_Pyrelord_C"));
        Assert.Empty(plan.OperationsOf("appendToList"));
        Assert.Equal(["/Game/_Core/Stats/Stats_Scaling_Difficulty"], plan.Overrides); // only the difficulty row
    }

    [Fact]
    public void ArchetypeGetsTraitEngramAndStartingKit()
    {
        var plan = PlanExample();
        var arch = Op(plan, "createBlueprint", "Archetype_Pyrelord")["properties"]!;
        Assert.EndsWith("Archetype_Trait_Pyrelord_C", arch["Archetype"]!["$softClass"]!.GetValue<string>());
        Assert.EndsWith("Engram_Pyrelord_C", arch["Engram"]!["$softClass"]!.GetValue<string>());
        Assert.Equal(2, arch["StartingGear"]!.AsArray().Count);
        Assert.EndsWith("Trait_Smoulder_C", arch["StartingTraits"]![0]!["$struct"]!["Trait"]!["$softClass"]!.GetValue<string>());
        Assert.Equal(10, Op(plan, "createDataTable", "DT_Archetype_Trait_Pyrelord_Levels")["rows"]!.AsObject().Count);
    }

    [Fact]
    public void WorldZonesAndAdventure()
    {
        var plan = PlanExample();
        var zone = Op(plan, "createBlueprint", "Zone_Ashfall_Cinderfields");
        Assert.Equal("/Script/Remnant.ZoneDef", zone["parent"]!.GetValue<string>());
        Assert.EndsWith("ST_AshfallLoot.ST_AshfallLoot", zone["properties"]!["SpawnLists"]![0]!["$struct"]!["SpawnTable"]!["$asset"]!.GetValue<string>());
        var quest = Op(plan, "createBlueprint", "Quest_AdventureMode_AshfallAdventure")["properties"]!;
        Assert.Equal("Adventure", quest["QuestGameMode"]!["$enum"]!.GetValue<string>());
        Assert.Equal("Ashfall", quest["BiomeName"]!.GetValue<string>());
        // Maps under the dimension's own folder are made by hand in the project; they aren't "required game assets".
        Assert.DoesNotContain(plan.RequiredAssets, a => a.StartsWith("/Game/Dimensions/", StringComparison.Ordinal));
    }

    [Fact]
    public void ChunkLabelComesLast()
    {
        var plan = PlanExample();
        var last = plan.Operations[^1];
        Assert.Equal("createChunkLabel", last["op"]!.GetValue<string>());
        Assert.Equal(7101, last["chunkId"]!.GetValue<int>());
    }

    [Fact]
    public void PlanJsonRoundTrips()
    {
        var json = JsonNode.Parse(PlanExample().ToJsonString())!;
        Assert.Equal(1, json["format"]!.GetValue<int>());
        Assert.Equal("/Game/Dimensions/Ashfall", json["contentRoot"]!.GetValue<string>());
        Assert.Equal("iostore", json["pak"]!["format"]!.GetValue<string>());
    }

    [Fact]
    public void GameTablesAndRecipeListsBecomeOverrides()
    {
        using var t = new TestDimension().Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "Band", "template": "/Game/R/Ring_X.Ring_X_C", "acquisition": [
                { "type": "loot", "table": "/Game/Loot/ST_Game.ST_Game", "chance": 7 },
                { "type": "craft", "recipeList": "/Game/Crafting/RL_Wallace.RL_Wallace",
                  "materials": [ { "item": "/Game/Mat/Mat_A.Mat_A_C", "quantity": 3 } ], "scrap": 500 } ] },
              { "kind": "lootTable", "id": "Extra", "entries": [ { "item": "Band" } ],
                "injectInto": [ { "table": "/Game/Loot/ST_Game.ST_Game", "chance": 50 } ] }
            ] }
            """);
        var plan = Plan(t);
        Assert.Equal(["/Game/Crafting/RL_Wallace", "/Game/Loot/ST_Game"], plan.Overrides);
        Assert.Contains("/Game/Loot/ST_Game", plan.RequiredAssets);

        var loot = Op(plan, "appendToList", "/Game/Loot/ST_Game");
        Assert.Equal("Entries", loot["property"]!.GetValue<string>());
        var entries = loot["items"]!.AsArray();
        Assert.Equal(2, entries.Count); // the ring, and the injected table: one op per list
        Assert.Contains(entries, e => e!["$new"]!.GetValue<string>() == "/Script/GunfireRuntime.SpawnTableLink");

        var recipe = Op(plan, "appendToList", "/Game/Crafting/RL_Wallace")["items"]![0]!["properties"]!;
        Assert.EndsWith("Ring_Band_C", recipe["Item"]!["$softClass"]!.GetValue<string>());
        Assert.Equal("/Game/Mat/Mat_A.Mat_A_C", recipe["Ingredient1"]!["$softClass"]!.GetValue<string>());
        Assert.Equal(500, recipe["Ingredient2Quantity"]!.GetValue<int>());
        Assert.EndsWith("Material_Scraps_C", recipe["Ingredient2"]!["$softClass"]!.GetValue<string>());
    }

    [Fact]
    public void ReplaceRegistrationBuildsAtTheReplacedPath()
    {
        using var t = new TestDimension().Content("x.json", """
            { "kind": "archetype", "id": "Swap", "registration": { "mode": "replace",
              "replaces": "/Game/World_Base/Items/Archetypes/Medic/Archetype_Medic.Archetype_Medic_C" } }
            """);
        var plan = Plan(t);
        Assert.Contains("/Game/World_Base/Items/Archetypes/Medic/Archetype_Medic", plan.Overrides);
        Assert.Single(plan.Operations, o => o["asset"]?.GetValue<string>() == "/Game/World_Base/Items/Archetypes/Medic/Archetype_Medic");
    }

    [Fact]
    public void AppendRegistrationAddsToTheList()
    {
        using var t = new TestDimension().Content("x.json", """
            { "kind": "adventure", "id": "Trip", "template": "/Game/Q/Quest_Base.Quest_Base_C", "world": "/Game/W/X",
              "registration": { "mode": "append", "list": "/Game/Lists/DA_Adventures", "property": "Quests" } }
            """);
        var plan = Plan(t);
        var op = Op(plan, "appendToList", "/Game/Lists/DA_Adventures");
        Assert.EndsWith("Quest_AdventureMode_Trip_C", op["items"]![0]!["$class"]!.GetValue<string>());
    }

    [Fact]
    public void RawPropertiesWinAndRefsResolve()
    {
        using var t = new TestDimension().Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "Band", "template": "/Game/R/Ring_X.Ring_X_C", "name": "Band",
                "properties": { "Label": { "$text": "Override" }, "Partner": { "$ref": "Charm" }, "Table": { "$ref": "Drops" } } },
              { "kind": "amulet", "id": "Charm", "template": "/Game/R/Amulet_X.Amulet_X_C" },
              { "kind": "lootTable", "id": "Drops", "entries": [] }
            ] }
            """);
        var props = Op(Plan(t), "createBlueprint", "Ring_Band")["properties"]!;
        Assert.Equal("Override", props["Label"]!["$text"]!.GetValue<string>());
        Assert.EndsWith("Amulet_Charm.Amulet_Charm_C", props["Partner"]!["$class"]!.GetValue<string>());
        Assert.EndsWith("ST_Drops.ST_Drops", props["Table"]!["$asset"]!.GetValue<string>());
    }

    [Fact]
    public void SharedTexturesAreImportedOnce()
    {
        using var t = new TestDimension().File_("art/shared.png").Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "One", "template": "/Game/R/R.R_C", "icon": { "small": "art/shared.png" } },
              { "kind": "ring", "id": "Two", "template": "/Game/R/R.R_C", "icon": { "small": "art/shared.png" } }
            ] }
            """);
        var plan = Plan(t);
        Assert.Single(plan.OperationsOf("importTexture"));
        var two = Op(plan, "createBlueprint", "Ring_Two")["properties"]!;
        Assert.EndsWith("T_Ring_One_Icon", two["Icon"]!["$asset"]!.GetValue<string>());
    }

    [Fact]
    public void ArmorNamesIncludeTheSlot()
    {
        using var t = new TestDimension().Content("x.json", """
            { "kind": "armor", "id": "Ashen", "template": "/Game/A/Armor_Body_X.Armor_Body_X_C", "slot": "hands", "armor": 5, "resistances": { "fire": 3 } }
            """);
        var plan = Plan(t);
        var table = Op(plan, "createDataTable", "DT_Armor_Gloves_Ashen_Stats");
        Assert.Equal(3, table["rows"]!["Armor_Gloves_Ashen"]!["FireResistance"]!.GetValue<int>());
    }
}
