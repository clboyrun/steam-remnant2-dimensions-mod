using System.Text.Json.Nodes;
using Dimensions.Definitions;
using Dimensions.Pipeline;

namespace Dimensions.Tests;

public class CharacterAndQuestTests
{
    private static string Errors(DimensionProject p) =>
        string.Join("\n", p.Diagnostics.Items.Where(d => d.Severity == Severity.Error).Select(d => d.ToString()));

    private static BuildPlan Plan(TestDimension t)
    {
        var project = t.Load();
        Assert.False(project.Diagnostics.HasErrors, Errors(project));
        return Planner.Plan(project);
    }

    private static BuildPlan PlanExample()
    {
        var project = DimensionProject.Load(Path.Combine(TestDimension.RepoRoot, "examples", "Ashfall"));
        Assert.False(project.Diagnostics.HasErrors, Errors(project));
        return Planner.Plan(project);
    }

    private static JsonObject Op(BuildPlan plan, string op, string assetSuffix) =>
        plan.Operations.Single(o => o["op"]!.GetValue<string>() == op && o["asset"]!.GetValue<string>().EndsWith(assetSuffix, StringComparison.Ordinal));

    [Fact]
    public void DialogueNodesMustDoOneThingAndJumpsMustLand()
    {
        using var t = new TestDimension().Content("x.json", """
            { "kind": "dialogue", "id": "Talk", "nodes": [
              { "id": "A", "say": "Hi", "exit": true },
              { "say": "Hi", "then": [ { "say": "x" } ] },
              { },
              { "jump": "Nowhere" },
              { "questSwitch": [ { "when": "All", "then": [ { "jump": "A" }, { "id": "A", "say": "again" } ] } ] }
            ] }
            """);
        var errors = Errors(t.Load());
        Assert.Contains("does one thing; this one has say, exit", errors);
        Assert.Contains("'then' goes with 'trigger' or 'onItem'", errors);
        Assert.Contains("needs one of:", errors);
        Assert.Contains("jump to 'Nowhere'", errors);
        Assert.Contains("node id 'A' is used twice", errors); // ids inside questSwitch branches count too
    }

    [Fact]
    public void DialogueBecomesAnEventTreeWithLinkedJumps()
    {
        var plan = PlanExample();
        var tree = Op(plan, "createObject", "ET_CinderkeeperTalk");
        Assert.Equal("/Script/GunfireRuntime.EventTree", tree["class"]!.GetValue<string>());
        var trigger = tree["properties"]!["Nodes"]![0]!;
        Assert.Equal("/Script/GunfireRuntime.EventTreeNode_Trigger", trigger["$new"]!.GetValue<string>());
        Assert.Equal("Interact", trigger["properties"]!["TriggerName"]!["$name"]!.GetValue<string>());
        var greeting = trigger["properties"]!["Nodes"]![0]!;
        Assert.Equal("CinderkeeperTalk.Greeting", greeting["$key"]!.GetValue<string>());
        var jump = trigger["properties"]!["Nodes"]![2]!["properties"]!["Branches"]![0]!["properties"]!["Nodes"]![1]!;
        Assert.Equal("CinderkeeperTalk.Greeting", jump["properties"]!["JumpNode"]!["$node"]!.GetValue<string>());
        var onItem = tree["properties"]!["Nodes"]![1]!;
        Assert.EndsWith("Quest_Item_SmolderingEmber_C", onItem["properties"]!["ItemBP"]!["$softClass"]!.GetValue<string>());
    }

    [Fact]
    public void QuestSwitchBranchesOnStateAndObjectives()
    {
        var plan = PlanExample();
        var tree = Op(plan, "createObject", "ET_ClementineTalk");
        var json = tree.ToJsonString();
        Assert.Contains("EventTreeNode_QuestSwitch", json);
        Assert.Contains("\"ValidForState\":{\"$enum\":\"QuestInProgress\"}", json);
        Assert.Contains("\"ValidObjectives\":[{\"$name\":\"ClueEmberline\"}]", json);
    }

    [Fact]
    public void EnemyGetsAnimBlueprintStatsAndLoot()
    {
        var plan = PlanExample();
        var abp = Op(plan, "createBlueprint", "ABP_Character_AshHusk");
        var layer = abp["properties"]!["AnimationLayers"]!;
        Assert.EndsWith("AS_HuskMoves", layer["items"]![0]!["$struct"]!["AnimationSet"]!["$asset"]!.GetValue<string>());

        var enemy = Op(plan, "createBlueprint", "Enemies/AshHusk/Character_AshHusk");
        Assert.EndsWith("ABP_Character_AshHusk_C", enemy["components"]!["Mesh"]!["AnimClass"]!["$class"]!.GetValue<string>());
        Assert.EndsWith("ST_HuskDrops", enemy["properties"]!["LootTable"]!["$asset"]!.GetValue<string>());
        Assert.Equal("Character_AshHusk", enemy["properties"]!["Data"]!["$rowHandle"]!["row"]!.GetValue<string>());

        Assert.Equal(3, plan.OperationsOf("importAnimation").Count());
        var encounters = Op(plan, "createObject", "ST_AshfallEncounters");
        var spawn = encounters["properties"]!["Entries"]![0]!["properties"]!;
        Assert.EndsWith("Character_AshHusk_C", spawn["CharacterBP"]!["$softClass"]!.GetValue<string>());
        Assert.Equal(3, spawn["QuantityMax"]!.GetValue<int>());
    }

    [Fact]
    public void NpcVendorStockCanBeGatedOnOwnedItems()
    {
        var plan = PlanExample();
        var list = Op(plan, "createObject", "RL_Character_ClementineInterface");
        var recipes = list["properties"]!["Recipes"]!.AsArray();
        Assert.Equal(3, recipes.Count);
        var first = recipes[0]!["properties"]!;
        Assert.EndsWith("Quest_Item_QuestBookLosomn_C", first["Item"]!["$softClass"]!.GetValue<string>());
        Assert.EndsWith("Quest_Item_CampaignSealLosomn_C", first["Ingredient2"]!["$softClass"]!.GetValue<string>());
        Assert.True(first["bIngredient2HideIfMissing"]!.GetValue<bool>());
        Assert.True(recipes[2]!["properties"]!["bIngredient2SecretIfMissing"]!.GetValue<bool>());

        var npc = Op(plan, "createBlueprint", "Character_ClementineInterface");
        Assert.EndsWith("ET_ClementineTalk", npc["components"]!["Dialog"]!["EventTree"]!["$asset"]!.GetValue<string>());
        Assert.Contains(plan.Notes, n => n.Contains("ClementineInterface: appears after") && n.Contains("Quest_Story_RootEarth"));
        Assert.Contains(plan.Notes, n => n.Contains("hub 'ward13'"));
    }

    [Fact]
    public void VendorEntriesHaveFourIngredientSlots()
    {
        using var t = new TestDimension().Content("x.json", """
            { "definitions": [
              { "kind": "ring", "id": "R", "template": "/Game/R.R_C" },
              { "kind": "npc", "id": "Seller", "template": "/Game/N.N_C", "location": { "position": [0, 0, 0] },
                "vendor": { "items": [ { "item": "R", "scrap": 10,
                  "materials": [ { "item": "R", "quantity": 1 }, { "item": "R", "quantity": 1 } ],
                  "requires": [ { "item": "R" }, { "item": "Ghost" } ] } ] } }
            ] }
            """);
        var errors = Errors(t.Load());
        Assert.Contains("materials, scrap and requires use 5", errors);
        Assert.Contains("'Ghost' isn't defined", errors);
        Assert.Contains("'location' needs 'map' or 'hub'", errors);
    }

    [Fact]
    public void QuestAddsObjectiveAndEntityComponents()
    {
        var plan = PlanExample();
        var quest = Op(plan, "createBlueprint", "Quest_Injectable_Cinderkeeper");
        var add = quest["addComponents"]!.AsArray();
        Assert.Equal("FindEmber", add[0]!["name"]!.GetValue<string>());
        Assert.Equal("/Script/GunfireRuntime.QuestObjective", add[0]!["class"]!.GetValue<string>());
        Assert.Equal("/Script/GunfireRuntime.QuestEntity", add[1]!["class"]!.GetValue<string>());
        Assert.EndsWith("ET_CinderkeeperTalk", add[1]!["properties"]!["EventTree"]!["$soft"]!.GetValue<string>());
    }

    [Fact]
    public void CampaignAlternativesAndStoryCampaignsLeaveNotesUntilTheCatalogKnowsTheSlots()
    {
        using var t = new TestDimension().Content("x.json", """
            { "definitions": [
              { "kind": "quest", "id": "GrimmStory", "template": "/Game/Q.Q_C" },
              { "kind": "world", "id": "Grimm", "zones": [ { "id": "Forest" } ],
                "campaign": { "alternativeTo": "yaesha", "chance": 0.25, "quest": "GrimmStory" } },
              { "kind": "adventure", "id": "GrimmCampaign", "template": "/Game/Q.Q_C", "world": "Grimm",
                "mode": "storyCampaign", "once": true, "difficulty": 1, "storyline": [ { "quest": "GrimmStory", "chance": 1 } ] }
            ] }
            """);
        var plan = Plan(t);
        Assert.Contains(plan.Notes, n => n.Contains("world Grimm: adding it as an extra 'yaesha' roll"));
        Assert.Contains(plan.Notes, n => n.Contains("adventure GrimmCampaign: 'once'"));
        Assert.Contains(plan.Notes, n => n.Contains("story campaigns are added to the campaign menu"));
        var adventure = Op(plan, "createBlueprint", "Quest_AdventureMode_GrimmCampaign");
        Assert.Contains("/Script/Remnant.RemnantQuestSubQuest", adventure.ToJsonString());
    }

    [Fact]
    public void CompletionTokensGetTheirOwnInventoryCategory()
    {
        var plan = PlanExample();
        var category = Op(plan, "createBlueprint", "ItemCategory_CompletionTokens");
        Assert.Equal("/Script/Remnant.ItemCategory_PlayerQuest", category["parent"]!.GetValue<string>());
        Assert.Equal("Completion Tokens", category["properties"]!["Label"]!["$text"]!["en"]!.GetValue<string>());
        var seal = Op(plan, "createBlueprint", "Quest_Item_CampaignSealYaesha");
        Assert.EndsWith("ItemCategory_CompletionTokens_C", seal["properties"]!["Category"]!["$class"]!.GetValue<string>());
        Assert.Contains(plan.Notes, n => n.StartsWith("itemCategory CompletionTokens"));

        using var t = new TestDimension().Content("x.json", """
            { "definitions": [ { "kind": "ring", "id": "R", "template": "/Game/R.R_C", "category": "R" } ] }
            """);
        Assert.Contains("'R' is a ring; expected an itemCategory", Errors(t.Load()));
    }
}
