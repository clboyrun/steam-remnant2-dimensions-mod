# Architecture

## The constraint

The mod is a plain `.pak` (plus `.utoc`/`.ucas`) in `Remnant2\Content\Paks\~mods`. Nothing runs code at runtime
besides the game itself, so every new thing has to exist as a **cooked game asset**, and the game has to **find**
it through the same references it uses for its own content.

The only runtime requirement is a pak signature bypass ("Allow Asset Mods" by Narknon), which every Remnant 2 asset
mod needs because the shipped paks are signed.

The Unreal project the build runs in is `Rem2Proj/` in this repository (`Rem2Proj/Remnant2.uproject`). It is ours to
edit and is not a separate clone or submodule: its headers are regenerated from the UE4SS UHT dump, and the `dimensions` CLI is pointed at it with `--project Rem2Proj\Remnant2.uproject` or
`DIMENSIONS_UPROJECT`.

## What the game looks like (research, 2026-11-01)

From `Rem2Proj/` in this repo (a UE 5.2 project with header stand-ins for the game's `Remnant` and
`GunfireRuntime` modules, started from the community Rem2Proj project) and the item database from
[lib.remnant2.analyzer](https://github.com/AndrewSav/lib.remnant2.analyzer):

- **Engine 5.2, IoStore** (`bUseIoStore=True`), Oodle compression, chunked paks. Mods cooked with UE 5.2 and packed as
  pak/IoStore load (the existing pak mods on Nexus were made this way, with a project like Rem2Proj).
- **Items** are blueprints derived from `AItem` (`AEquipment` → weapons, armor, trinkets, `AEquipmentMod` → mods and
  mutators). Display text, icons and a stats row (`FDataTableRowHandle Stats`) are properties on the class defaults.
- **Weapon stats** are `FRangedWeaponData` rows (Damage, RateOfFire, AmmoPerClip, MaxAmmo, Range, falloff, spread,
  pellets, burst, scope). Because the row handle names its own table, each new weapon gets its **own** data table and
  the game's shared stat tables are never touched.
- **Loot** is `USpawnTable` with instanced `USpawnTableItem` / `USpawnTableLink` entries.
- **Crafting and vendors** are `UCraftingRecipeList` assets of `UCraftingRecipe` (item + up to four ingredients).
- **Archetypes** are `URemnantArchetype` (label, image, archetype trait class, engram item, starting gear, starting
  traits). The archetype's own levels are a `URemnantArchetypeTrait` (a `UTrait` with a stat table).
- **Worlds** are `UZoneDef` (map templates, tile set, spawn lists, encounter groups, skyboxes).
- **Adventures** are root quests: `ARemnantQuest` with `QuestGameMode = Adventure`, a container map, level range,
  rarity and biome name. The quest graph that places zones, bosses and rewards lives in the container map.
- The asset manager **scans /Game** for `ZoneDef`, `Quest`, `RecipeList` and `RemnantArchetype` primary assets
  (`Rem2Proj/Config/DefaultGame.ini`).

## Pipeline

```
 your JSON + art + models
        │  dimensions validate     Dimensions.Definitions: load, extends, schema, references, files
        ▼
 definitions
        │  dimensions plan         Dimensions.Pipeline.Planner + data/catalog.json
        ▼
 plan.json  ── operations: importTexture, importMesh, createMaterialInstance, createDataTable,
        │                  createBlueprint, createObject, copyAsset, appendToList, createChunkLabel
        │  dimensions build
        ▼
 UnrealEditor-Cmd -run=pythonscript  unreal/Python/dimensions_apply.py (+ DimensionsEditor plugin)
        │   assets under /Game/Dimensions/<id>/, plus edited copies of game lists you add to
        ▼
 UnrealEditor-Cmd -run=cook -targetplatform=Windows -unversioned
        ▼
 PakLayout: keep only /Game/Dimensions/<id>/** and the overridden game assets
        ▼
 UnrealPak → legacy .pak → retoc to-zen → zz_Dimensions_<id>_P.pak/.utoc/.ucas → Paks\~mods
```

Each layer is usable on its own: the definitions library has no Unreal knowledge, the planner is pure (it reads
JSON and the catalog and writes JSON), and the editor script only knows the plan format. That's what keeps the
JSON side modular: adding a field usually means a schema change and a few lines in the planner, and game facts
(template paths, struct names, stat field names) live in `data/catalog.json`, not in code.

### Why a stand-in project works

`Rem2Proj/` has the game's C++ classes as headers, and some game blueprints as stand-ins. A new weapon is a child
blueprint of the stand-in `Weapon_LongGun_Base`; when cooked, the child refers to its parent by path, and in game
that path resolves to the **real** parent in the game's paks, because the stand-in itself is never packed
(`PakLayout` packs only the dimension's folder and the overrides). The child keeps everything it doesn't change
from the real parent.

Limit: components inherited from a parent blueprint can only be edited if the stand-in has the same components.
Where it doesn't, import the real parent from FModel (JsonAsAsset) or set the mesh on a component the template
exposes.

### How new content gets found

| Content | How the game reaches it | Generated |
| --- | --- | --- |
| Weapon, mod, mutator, ring, amulet, armor | A loot table, recipe list, vendor list, or starting gear refers to its class | Entries added to the table/list you name |
| Loot table | Linked from a game spawn table (`injectInto`) or from a zone's spawn list | `USpawnTableLink` entry, or `SpawnLists` |
| Archetype | Asset registry scan for `RemnantArchetype` (discover), or a list/slot (append/replace) | `registration` |
| World zones, adventure | Asset registry scan for `ZoneDef`/`Quest` (discover), or append/replace | `registration` |
| Enemy | A spawn table entry (`USpawnTableCharacter`) used as a zone's encounter group | `encounters` |
| NPC | Placed in a map, or spawned by a quest | A blueprint; placement is open (below) |
| Dialogue, objectives | A quest entity (`UQuestEntity.EventTree`) or an NPC's dialogue component | `addComponents` on the quest |
| Campaign alternative world | The campaign root quest's slot for that world (`campaign.alternativeTo`: `losomn`, `yaesha` or `nerud` only; Hell needs a root-quest override) | Note only until the catalog has `campaign.*` |

Adding to a game list means packing a **modified copy** of that game asset (an override). Compatibility with other
pak mods is **not a goal** (decided 2026-11-01): when overriding a game asset (the inventory widget, the campaign
root quest, a hub quest) is the simplest way to add something, do that. The plan lists every
override, and the planner merges all of a dimension's additions to one list into a single edit. Two separate mods
that override the same game list conflict; keep related content in one dimension.

## Risks and unknowns

1. **Discovery from a mod pak.** In a cooked game the asset registry comes from the game's own
   `AssetRegistry.bin`, so a new `RemnantArchetype`, `ZoneDef` or `Quest` in a mod pak may not be found by the scan.
   If it isn't, use `registration: append` (add to a list the game reads) or `replace` (take over an existing slot,
   for example replace one adventure option). This is the first thing to test.
2. **Unconfirmed catalog entries** (`verified: false`): the row structs for item, armor, mutator and trait stats,
   armor stat field names, the archetype/engram templates, and the adventure template. Check them in FModel and fix
   the catalog.
3. **Weapon mod slots**: slot NameIDs (`Mod`, `Gem`) are a guess; copy them from a real weapon.
4. **New maps** are built by hand in the editor under `/Game/Dimensions/<id>/Maps/` (tiles, container map, quest
   graph). The JSON references them; it can't generate level geometry.
5. **Localization**: only the default culture's text is written into assets so far; other cultures in the JSON
   are kept for a later string-table step.
6. **Campaign slots and story campaigns**: the mod adds four new worlds. Three fit the base campaign's rollable
   slots through `campaign.alternativeTo`, which accepts only `losomn`, `yaesha` and `nerud`: the Grimm world is an
   extra Yaesha roll, the Lovecraftian world an extra N'Erud roll and the Abandoned Arcade an extra Losomn roll, each
   by adding a sub-quest to the campaign root quest's slot. Hell (Root Earth) does **not** fit: Root Earth is a fixed
   quest, not a rolled slot, so Hell probably needs the campaign root quest overridden (unverified; no schema field
   or catalog slot exists for it yet). Which asset and property that is
   isn't known yet, so the plan writes a note. Export `Quest_Campaign` (and a DLC story campaign's setup) from FModel
   and fill in `campaign` in the catalog.
7. **Hub NPCs**: a pak can't safely add an actor to Ward 13's map (that would replace the whole map). The way the
   game makes NPCs appear there after story events (a hub quest, spawn points, a sequence) has to be found in FModel
   and recorded under `npc.hub` in the catalog. Until then, `unlock` and `location` are written as notes. Vendor
   gates (`requires`) use the recipe's own hide-if-missing ingredient flags; whether a gate ingredient is consumed on
   purchase needs an in-game test, and the campaign tokens need something that hands them out when a campaign ends.
8. **Inventory sections**: an item's section comes from `AItem.Category` (a `UItemCategory` class with a label and
   icon), and inventory panels list items through `FItemQueryFilter.RequiredCategories`. A new category is plain
   data. If the inventory groups by category on its own, the new section just appears; if its tabs are fixed in the
   inventory widget, that widget blueprint is overridden (`ui.inventoryWidget` in the catalog).
9. **Difficulty**: a difficulty is an index into `DifficultyScalingTable` (rows of `FDifficultyScalingData`: enemy
   health, enemy damage, spawn quantity, currency, experience). Adding a row is data; the table's asset path, the
   difficulty menu's list, and how loot and enemy level follow the index still need finding in FModel.
10. **Dialogue wiring**: node classes and fields come from the headers, but the trigger name the game fires on
   interact (`Interact` in the examples) and the voice audio struct still need confirming.
11. **retoc and UE 5.2**: retoc notes missing dependency data before 5.3 "should not be an issue for modding". If
   IoStore output misbehaves, set `"pak": { "format": "legacy" }` and test a plain pak.

## First test in game

Do these in order with the smallest content that answers each question, and record the answers in
`data/catalog.json` (set `verified: true`).

1. **Pipeline smoke test**: one ring whose template is a real ring, a new icon and name, acquired by `startingGear`
   of a game archetype or added to a vendor list. Proves cooking, packing, the stand-in parent trick and an override.
2. **Weapon**: Emberline from the example with a real skeletal mesh (FBX) on a weapon template's mesh component.
3. **Loot**: a loot table injected into a game spawn table; check the drop.
4. **Discovery**: an archetype with `registration: discover`. If it doesn't appear at character creation, retry with
   `replace`, and set the default for archetypes, zones and adventures accordingly.
5. **World**: one zone with a simple map, then the adventure quest and its container map.
6. **Dungeon injection (investigate)**: before we write our own instance tables for the four new worlds (Grimm,
   Lovecraft, Abandoned Arcade, Hell), find out how the game injects a dungeon into a zone and ties spawns to it.
   Questions to answer from FModel and the UE4SS dump:
   - Which asset picks a dungeon for a zone slot, and does it read `QuestTags` or a `USpawnTable` of dungeons?
   - Where do per-dungeon instance tables (encounter groups, loot, minibosses) attach, and what links a table to
     the dungeon that spawned (`URemnantQuestZone.OnZoneLoaded` and `SetSpawnTableOverride` are the suspects).
   - Whether `lootTable.injectInto` and a zone's `encounters` are enough to attach our tables, or whether a
     dungeon-level override is needed.
   - Whether a pak-added dungeon quest is seen by the quest manager's `RegisteredQuests` (see item 6).
   Until this is answered, `injectInto` entries for new dungeons are unverified. Tracked in ClickUp as
   "#23 How dungeons are generated per zone".
