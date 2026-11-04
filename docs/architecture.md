# Architecture

## The constraint

The mod is a plain `.pak` (plus `.utoc`/`.ucas`) in `Remnant2\Content\Paks\~mods`. Nothing runs code at runtime
besides the game itself, so every new thing has to exist as a **cooked game asset**, and the game has to **find**
it through the same references it uses for its own content.

The only runtime requirement is a pak signature bypass ("Allow Asset Mods" by Narknon), which every Remnant 2 asset
mod needs because the shipped paks are signed.

The Unreal project the build runs in is `Rem2Proj/` in this repository (`Rem2Proj/Remnant2.uproject`). It is ours to
edit and is not a separate clone or submodule: its headers are regenerated from the UE4SS UHT dump. Building,
cooking and packing go through `tools/pak-builder` (Python) only; see below.

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

## Build path

All building goes through `tools/pak-builder` (Python; on the `pak-builder` branch until it is merged). It generates the stub and child Blueprints in `Rem2Proj/`,
cooks the project, and packs the cooked files into a pak with UnrealPak. See `tools/pak-builder/README.md`.

An earlier JSON-driven toolchain (a C# `dimensions` CLI with schemas, a planner and its own cook/pack stage) was
dropped in favour of this single path (decided by Astra, 2026-11-04). It is still in the history of the closed
PR #1. `data/catalog.json` is kept as a record of game facts (template paths, row structs, stat field names); the
`verified: false` entries are the ones still to be checked in FModel.

### Why a stand-in project works

`Rem2Proj/` has the game's C++ classes as headers, and some game blueprints as stand-ins. A new weapon is a child
blueprint of the stand-in `Weapon_LongGun_Base`; when cooked, the child refers to its parent by path, and in game
that path resolves to the **real** parent in the game's paks, because the stand-in itself is never packed
(only the new content and the overrides are packed). The child keeps everything it doesn't change
from the real parent.

Limit: components inherited from a parent blueprint can only be edited if the stand-in has the same components.
Where it doesn't, import the real parent from FModel (JsonAsAsset) or set the mesh on a component the template
exposes.

### How new content gets found

| Content | How the game reaches it | Generated |
| --- | --- | --- |
| Weapon, mod, mutator, ring, amulet, armor | A loot table, recipe list, vendor list, or starting gear refers to its class | Entries added to the table/list |
| Loot table | Linked from a game spawn table or from a zone's spawn list | `USpawnTableLink` entry, or `SpawnLists` |
| Archetype | Asset registry scan for `RemnantArchetype` (discover), or a list/slot (append/replace) | Registration entry |
| World zones, adventure | Asset registry scan for `ZoneDef`/`Quest` (discover), or append/replace | Registration entry |
| Enemy | A spawn table entry (`USpawnTableCharacter`) used as a zone's encounter group | Encounter groups on the zone |
| NPC | Placed in a map, or spawned by a quest | A blueprint; placement is open (below) |
| Dialogue, objectives | A quest entity (`UQuestEntity.EventTree`) or an NPC's dialogue component | Components added to the quest |
| Campaign alternative world | The campaign root quest's slot for that world (`losomn`, `yaesha` or `nerud` only; Hell needs a root-quest override) | Open until the catalog has `campaign.*` |

Adding to a game list means packing a **modified copy** of that game asset (an override). Compatibility with other
pak mods is **not a goal** (decided 2026-11-01): when overriding a game asset (the inventory widget, the campaign
root quest, a hub quest) is the simplest way to add something, do that. Two separate mods
that override the same game list conflict, so keep related additions to one list in a single override.

## Risks and unknowns

1. **Discovery from a mod pak.** In a cooked game the asset registry comes from the game's own
   `AssetRegistry.bin`, so a new `RemnantArchetype`, `ZoneDef` or `Quest` in a mod pak may not be found by the scan.
   If it isn't, use an append (add to a list the game reads) or a replace (take over an existing slot,
   for example replace one adventure option). This is the first thing to test.
2. **Unconfirmed catalog entries** (`verified: false`): the row structs for item, armor, mutator and trait stats,
   armor stat field names, the archetype/engram templates, and the adventure template. Check them in FModel and fix
   the catalog.
3. **Weapon mod slots**: slot NameIDs (`Mod`, `Gem`) are a guess; copy them from a real weapon.
4. **New maps** are built by hand in the editor under `/Game/Dimensions/<id>/Maps/` (tiles, container map, quest
   graph). Nothing generates level geometry.
5. **Localization**: only the default culture's text goes into assets; other cultures need a string-table step.
6. **Campaign slots and story campaigns**: the mod adds four new worlds. Three fit the base campaign's rollable
   slots through the campaign root quest's alternative-slot setting, which accepts only `losomn`, `yaesha` and `nerud`: the Grimm world is an
   extra Yaesha roll, the Lovecraftian world an extra N'Erud roll and the Abandoned Arcade an extra Losomn roll, each
   by adding a sub-quest to the campaign root quest's slot. Hell (Root Earth) does **not** fit: Root Earth is a fixed
   quest, not a rolled slot, so Hell probably needs the campaign root quest overridden (unverified; no catalog slot
   exists for it yet). Which asset and property that is isn't known yet. Export `Quest_Campaign` (and a DLC story campaign's setup) from FModel
   and fill in `campaign` in the catalog.
7. **Hub NPCs**: a pak can't safely add an actor to Ward 13's map (that would replace the whole map). The way the
   game makes NPCs appear there after story events (a hub quest, spawn points, a sequence) has to be found in FModel
   and recorded under `npc.hub` in the catalog. Vendor
   gates use the recipe's own hide-if-missing ingredient flags; whether a gate ingredient is consumed on
   purchase needs an in-game test, and the campaign tokens need something that hands them out when a campaign ends.
8. **Inventory sections**: an item's section comes from `AItem.Category` (a `UItemCategory` class with a label and
   icon), and inventory panels list items through `FItemQueryFilter.RequiredCategories`. A new category is plain
   data. If the inventory groups by category on its own, the new section just appears; if its tabs are fixed in the
   inventory widget, that widget blueprint is overridden (`ui.inventoryWidget` in the catalog).
9. **Difficulty**: a difficulty is an index into `DifficultyScalingTable` (rows of `FDifficultyScalingData`: enemy
   health, enemy damage, spawn quantity, currency, experience). Adding a row is data; the table's asset path, the
   difficulty menu's list, and how loot and enemy level follow the index still need finding in FModel.
10. **Dialogue wiring**: node classes and fields come from the headers, but the trigger name the game fires on
   interact and the voice audio struct still need confirming.
11. **retoc and UE 5.2**: retoc notes missing dependency data before 5.3 "should not be an issue for modding". If
   IoStore output misbehaves, test a plain legacy pak.

## First test in game

Do these in order with the smallest content that answers each question, and record the answers in
`data/catalog.json` (set `verified: true`).

1. **Pipeline smoke test**: one ring whose template is a real ring, a new icon and name, acquired by `startingGear`
   of a game archetype or added to a vendor list. Proves cooking, packing, the stand-in parent trick and an override.
2. **Weapon**: one new weapon with a real skeletal mesh (FBX) on a weapon template's mesh component.
3. **Loot**: a loot table linked from a game spawn table; check the drop.
4. **Discovery**: an archetype found by the asset-registry scan alone. If it doesn't appear at character creation, retry
   with a replace, and record which approach works for archetypes, zones and adventures.
5. **World**: one zone with a simple map, then the adventure quest and its container map.
6. **Dungeon injection (investigate)**: before we write our own instance tables for the four new worlds (Grimm,
   Lovecraft, Abandoned Arcade, Hell), find out how the game injects a dungeon into a zone and ties spawns to it.
   Questions to answer from FModel and the UE4SS dump:
   - Which asset picks a dungeon for a zone slot, and does it read `QuestTags` or a `USpawnTable` of dungeons?
   - Where do per-dungeon instance tables (encounter groups, loot, minibosses) attach, and what links a table to
     the dungeon that spawned (`URemnantQuestZone.OnZoneLoaded` and `SetSpawnTableOverride` are the suspects).
   - Whether a loot-table link and a zone's encounter groups are enough to attach our tables, or whether a
     dungeon-level override is needed.
   - Whether a pak-added dungeon quest is seen by the quest manager's `RegisteredQuests` (see item 6).
   Until this is answered, spawn-table links for new dungeons are unverified. Tracked in ClickUp as
   "#23 How dungeons are generated per zone".
