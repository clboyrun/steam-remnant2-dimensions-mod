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
| Campaign alternative world | The campaign root quest's slot for that world (three tag-picked story slots `World1` to `World3`; Hell needs a root-quest override) | Shape confirmed in the dump (`campaign.*` in the catalog); the slot-to-world mapping is open |

Adding to a game list means packing a **modified copy** of that game asset (an override). Compatibility with other
pak mods is **not a goal** (decided 2026-11-01): when overriding a game asset (the inventory widget, the campaign
root quest, a hub quest) is the simplest way to add something, do that. Two separate mods
that override the same game list conflict, so keep related additions to one list in a single override.

## Confirmed by the FModel dump (2026-11-04)

Astra dumped the game's properties with FModel to `workspace/extracted/Remnant2/Content/` on her machine (about
17,000 JSON files; not in the repo). The files below were read for this section. Everything here is read from
those files; anything the files do not show is listed under "Not shown by the dump". Package paths are the file path
below `Content/` with `/Game/` in front (`Content/_Core/Stats/Stats_Armor.json` is `/Game/_Core/Stats/Stats_Armor`).

**Tables and structs** (`Content/_Core/Stats/`). These are `DataTable` assets whose row struct is a *user-defined*
struct under `/Game/_Core/Stats/Base/`. In the cooked JSON every row property is named `<Name>_<id>_<GUID>`, for
example `Damage_2_201A70444998A31015340E9DF30CF2AF`; the game's own tools match on the part before the id.

| Table | Rows | Struct | Fields |
| --- | --- | --- | --- |
| `Stats_RangedWeapon` | 72 (`Repeater`, `SMG`, `Special_*`, `Boss_*`, ...) | `StatDef_RangedWeapon` | Value, Damage, ImpactScalar, Range, PrimaryFalloff, SecondaryFalloff, SprayCount, Spread, MaxAmmo, AmmoPerClip, AmmoPerReload, RateOfFire, IsAutomatic, BurstCount, BurstRateOfFire, HasScope, ScopeFOV, MinCritRange, MaxCritRange, CritChance, ProcChance, WeakSpotDamageMod, CrouchSpreadScalar |
| `Stats_MeleeWeapon` | 48 | `StatDef_MeleeWeapon` | Value, Damage, ImpactScalar, BleedCritChance, AttackSpeed, StaminaCost, CritChance, ProcChance, WeakSpotDamageMod |
| `Stats_Armor` | 123 | `StatDef_Armor` | Value, Armor, Encumbrance, BleedResistance, BlightResistance, FireResistance, ShockResistance, AcidResistance |
| `Stats_Items` | 37 | `StatDef_Item` | Value, SellValueScalar (a price table) |
| `Stats_Scaling_Difficulty` | 4 (`"1"` to `"4"`) | `StatDef_Scaling` | 23 scalars (enemy, elite, brute, aberration, miniboss and world boss health and damage, spawn quantity and weight, healing, currency, experience, friendly fire, player status damage, blight build-up, consumable, ammo and gem drop chance) |

Corrections to earlier assumptions: armor has `Encumbrance` (not `Weight`) and `AcidResistance` (not
`ToxinResistance`); the ranged weapon struct is `StatDef_RangedWeapon`, not `GunfireRuntime.RangedWeaponData`; and
`StatDef_Item` holds only prices, so it is not the struct for ring, amulet, mutator or trait effects. Difficulty is
exactly four rows, so a fifth difficulty is a new row `"5"`.

**Item types and categories** (`Content/_Core/ItemTypes/`). Both are Blueprint classes. `ItemCategory_*` derive
from `/Script/GunfireRuntime.ItemCategory` and set `Label` and `Icon` (categories: All, Ammo, Armor, Consumables,
Engrams, Equipment, Hidden, Items, Skin, Trinket, Weapons). `ItemType_*` derive from `/Script/GunfireRuntime.ItemType`
and set `ItemStats` and `CharacterStats` (both `StatInfoSet` assets in `/Game/_Core/Stats/Inspect`), `Label`, `Icon`
and `SortPriority`. There are item types for Ring, Amulet, Helmet/Body/Gloves/Legs, LongGun/HandGun/Melee/SpecialGun,
Gem_* and MetaGem_* (mutators), PrismStone_Base, Quest and the vendor types.

**Campaign root quest** (`Content/Campaign_Main/Quest_Campaign_Main.json`). `Quest_Campaign_Main_C` derives from
`/Game/_Core/Blueprints/Quests/Quest_RootQuest.Quest_RootQuest_C`. Its defaults: `QuestGameMode = EQuestMode::Campaign`,
`QuestNameID = MainCampaign`, `WorldTags = ["Nerud", "Fae", "Jungle"]`, plus `RemnantQuestSubQuest` components:

| Component | Chooses its quest by | Level remapping |
| --- | --- | --- |
| `World1`, `World2`, `World3` | `QuestTags = ["Story"]` (no fixed quest) | World1 up to 5; World2 and World3 6 to 9 |
| `RootEarth` | a fixed `QuestBP`: `Quest_RootEarth_Zone1` | 12 to 14 |
| `Labyrinth` | a fixed `QuestBP`: `Quest_Story_Labyrinth`, `LevelMin 5` | none |

`RemnantQuestZoneLink` components join the Labyrinth to each world (`Labyrinth_ToWorld1` to `World1_ToLabyrinth`, and
so on, plus Root Earth). So the three rolled worlds are picked by tag, which fits the plan to add the Grimm, Lovecraft
and Arcade worlds as story quests with matching tags. Root Earth is a fixed `QuestBP`, which confirms that Hell
cannot be an extra roll and needs the root quest overridden. Functions on the quest include `AssignWorldTag`,
`ActivateFirstValidWorld`, `IsWorldValid` and `OnRootEarth`.

**Zone definition** (`Content/Campaign_Main/ZoneDefs/ZoneDef_RootEarth_City.json`). A Blueprint class derived from
`/Script/Remnant.ZoneDef` whose defaults hold `TileSet` (a `TileSet` asset), `EncounterGroups` and `SkyBoxes`. The
`Swarm` and `SwarmSpecial` groups have `SpawnTable = null` and are filled by `SpawnTags`; the fields are in
`data/catalog.json` under `kinds.world.dumpFindings`. Only this one zone def was read.

**Root Earth layout.** `Campaign_Main` also holds `Quests/Quest_RootEarth_Zone1`, `Zone2` and `TowerBoss`,
`SpawnTables/` and `ZoneDefs/` (City, Seabed, Towerboss). Quest files can be very large (`Quest_Earth_City.json` is
about 800 MB, `Quest_Earth_TowerBoss.json` about 300 MB), so they are not practical to stage or open whole.

**Quests, adventures and dungeons** (`Content/_Core/Blueprints/Quests/`). `Quest_Base_C` derives from
`/Script/Remnant` `RemnantQuest` and is the parent of the zone, dungeon, boss, miniboss and event quests.
`Quest_RootQuest_C` (a `RemnantQuest` child) has `Type = Root`, `QuestGameMode = None`, `Container =
/Game/Maps/Quest_Container` and `QuestTags = [Hidden]`.

- **Adventure.** `Quest_AdventureMode_Base_C` derives from `Quest_RootQuest_C` and has one `RemnantQuestSubQuest`
  component, `Quest`, whose completion calls `OnSubQuestComplete`. The zone quest `Quest_AdventureMode_1Zone_C`
  (parent `Quest_Base_C`) has `QuestScope = Campaign` and `QuestTags = [AdventureMode, InsertBiomeHere]`, so a
  biome-specific child replaces the placeholder tag.
- **Parts are picked by tag.** The zone quest's sub quest components carry only `QuestTags`: `Boss` [City, Boss],
  `Event` [City, Event], `MainDungeon` [City, Dungeon, Miniboss], `SideD` [City, Dungeon, Side], `OverworldPOI`
  [City, OverworldPOI], `TraitBook` [City, Trait], and two `Simulacrum` slots [Simulacrum] with `ChanceToSpawn 20`.
  It also has tiles (`Start`, `End`, `Dungeon1_Entrance`, `Dungeon1_Exit`, `Dungeon`) and zone links (`Trans`,
  `Waypoint` using `Interactive_DragonCrystal`).
- **Dungeon injection point.** `Quest_Dungeon_Base_C` has an optional sub quest `Injectable` (`QuestTags =
  [Injectable]`) and a `TraitBook` sub quest (`[Trait]`), plus an AI director, `Start`/`End` tiles, a `Zone` and a
  waypoint link. This answers the first two first-test-6 questions: dungeon content is chosen by `QuestTags`, and
  the `Injectable` slot is where extra content attaches. Still open: the graph that does the choosing, and whether
  a quest added by a mod pak is seen (the asset-registry question in Risk 1).

**Items.**
- **Rings** (`World_Base/Items/Trinkets/Rings/<Name>/`). `Ring_BlessedRing_C` derives from `Trinket_Base_C`
  (`/Game/World_Base/Items/_Core/Trinkets/Trinket_Base`). Its properties are `StatsComp`, `PersistenceComp`,
  `FilterTags` (`Defense`, `Relic`), `Label`, `Description`, `FlavorText`, `Icon`, `LargeIcon`, `HUDIcon`,
  `InspectActor`. The effect is Blueprint logic (`OnEquipped`, `OnUnequipped`, `OnEvent`, `ModifyInspectInfo`) and
  tuning numbers are Blueprint variables (`Duration`, `BulwarkStacks`). There is no ring stats table.
- **Mutators** (`World_Base/Items/Gems/Ranged/MetaGems/<Name>/`). `MetaGem_Bandit_C` derives from `MetaGem_Ranged_C`,
  which derives from `Gem_Meta_Base_C`. Levels come from `CurveData` (`MetaGemValueCurve`) and a `Tiers` array
  (`Level`, `Label`, `Icon`, `RimColor`). Per-mutator tuning is Blueprint variables (`ReloadSpeedBonus`,
  `MaxLevelDurationMax`, `MaxLevelStatBonus`, `MinValue`, `MaxValue`) and the behavior is in a sibling
  `Action_MetaGem_<Name>` asset. Only the ranged chain was read; melee mutators were not.
- **Archetypes** (`World_Base/Items/Archetypes/<Name>/`). `Archetype_Alchemist` has `EngramItem`, `IconColor`,
  `RemnantArchetype` (a class reference to `Archetype_Alchemist_UI_C`), `Type = TraitType_Archetype`, `StatTable`,
  `MaxLevel 10`, `Cost`, `Label`, `Description`, `FlavorText`, `Icon`. `StatTable` is `Archetype_Alchemist_Unlocks`,
  a `DataTable` of `/Script/GunfireRuntime.StatModTableEntry` (29 rows; fields `Level`, `StatName`, `StatBonus`,
  `ModifierBP`, `ModifierLevel`, `ModifierCapacity`, `OverrideExistingLevel`, `LevelScalingOwner`,
  `RequiredContextSlot`, `AllowOverrideRequiredContextSlot`, `Description`, `Hidden`). The engram derives from
  `Item_Engram_Base_C` (`/Game/Items/Common/Item_Engram_Base`). Trait types are classes in `/Game/_Core/TraitTypes`
  (Archetype, Character, Gear, Perk, Skill); trait Blueprints are under `World_Base/Items/Traits/`.
- **Item filters.** `/Game/UI/UI_Inventory/DataTable_ItemFilters` (row struct `ItemFilterRowData`, 23 rows such as
  `Defense`, `Relic`, `FireRate`) defines the search chips; an item's `FilterTags` use those row names.

**Inventory and menu UI** (`Content/UI/`). `Widget_InGameMenu` is the main menu with eight tab buttons: CHARACTER,
INVENTORY, ARCHETYPE, TRAITS, FRAGMENTS, MAP, VENDOR and SYSTEM, each naming a panel. `Widget_InventoryList` defaults
are `Sort = EItemQuerySortMethod::Sort`, `IgnoreTags = [HideInInventory]`, `DisplaySearch = true` and 21
placeholders, and it builds its list in `Build Inventory List` / `Update Inventory List` with `GetQueryFilter`.
`Widget_InventoryTab` has an `ItemCategory` property (default `ItemCategory_All_C`) and decides its visibility in
`IsValidCategory` / `HasValidItems`. So a new inventory section means a tab whose `ItemCategory` is the new class. The
widget that holds the row of tab instances was not in the files read. The main menu has no difficulty list.

### Not shown by the dump

- The JSON is the property view. Function graphs are listed by name only, so which World slot maps to which tag
  (`AssignWorldTag`), how the game picks a quest for an `Injectable` slot, and the logic of any ring or mutator are
  **not** answered.
- Not found or not read: the difficulty menu list, the widget holding the inventory tab row, melee mutators,
  amulets, individual traits and armor Blueprints, biome-specific adventure quests, the Ward 13 NPC spawn hook, and
  any zone def other than `ZoneDef_RootEarth_City`.
- Folders left out of the copy (art, audio, maps, `Characters`, `Quest_Earth_City`) were not read.

## Risks and unknowns

1. **Discovery from a mod pak.** In a cooked game the asset registry comes from the game's own
   `AssetRegistry.bin`, so a new `RemnantArchetype`, `ZoneDef` or `Quest` in a mod pak may not be found by the scan.
   If it isn't, use an append (add to a list the game reads) or a replace (take over an existing slot,
   for example replace one adventure option). This is the first thing to test.
2. **Unconfirmed catalog entries** (`verified: false`): weapon, armor and difficulty tables are now confirmed (see the
   dump section). Rings, mutators and archetypes are also read (see the dump section): ring and mutator effects are Blueprint
   logic, not table rows, and archetype levels are `StatModTableEntry` rows. Still open: amulets, melee mutators,
   individual traits, and biome-specific adventure quests. Check them in FModel and fix the catalog.
3. **Weapon mod slots**: slot NameIDs (`Mod`, `Gem`) are a guess; copy them from a real weapon.
4. **New maps** are built by hand in the editor under `/Game/Dimensions/<id>/Maps/` (tiles, container map, quest
   graph). Nothing generates level geometry.
5. **Localization**: only the default culture's text goes into assets; other cultures need a string-table step.
6. **Campaign slots and story campaigns**: the mod adds four new worlds. Three fit the base campaign's rollable
   slots (the root quest's `World1` to `World3` sub quests, picked by tag): the Grimm world is an
   extra Yaesha roll, the Lovecraftian world an extra N'Erud roll and the Abandoned Arcade an extra Losomn roll, each
   by adding a sub-quest to the campaign root quest's slot. Hell (Root Earth) does **not** fit: Root Earth is a fixed
   quest, not a rolled slot, so Hell needs the campaign root quest overridden (the fixed `QuestBP` is confirmed in the
   dump; the override itself is untested). The dump confirms this shape: the campaign root is `Quest_Campaign_Main` with three tag-picked `World1` to
   `World3` slots and a fixed Root Earth sub quest. The `losomn`/`yaesha`/`nerud` slot names above are not in the file;
   it uses `WorldTags` `Nerud`, `Fae`, `Jungle`. Still unknown: which slot goes with which tag, and a DLC story
   campaign's setup. `campaign` in the catalog is filled in from the dump.
7. **Hub NPCs**: a pak can't safely add an actor to Ward 13's map (that would replace the whole map). The way the
   game makes NPCs appear there after story events (a hub quest, spawn points, a sequence) has to be found in FModel
   and recorded under `npc.hub` in the catalog. Vendor
   gates use the recipe's own hide-if-missing ingredient flags; whether a gate ingredient is consumed on
   purchase needs an in-game test, and the campaign tokens need something that hands them out when a campaign ends.
8. **Inventory sections**: an item's section comes from `AItem.Category` (a `UItemCategory` class with a label and
   icon), and inventory panels list items through `FItemQueryFilter.RequiredCategories`. A new category is plain
   data. If the inventory groups by category on its own, the new section just appears; if its tabs are fixed in the
   inventory widget, that widget blueprint is overridden (`ui.inventoryWidget` in the catalog). The dump shows
   tabs are `Widget_InventoryTab` instances with an `ItemCategory` property, so a new section needs a tab; the
   widget that holds the tab row is still to find.
9. **Difficulty**: a difficulty is an index into `DifficultyScalingTable` (rows of `FDifficultyScalingData`: enemy
   health, enemy damage, spawn quantity, currency, experience). Adding a row is data; the table is
   `/Game/_Core/Stats/Stats_Scaling_Difficulty` with rows `"1"` to `"4"` (confirmed in the dump). The difficulty
   menu's list, and how loot and enemy level follow the index, still need finding in FModel.
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
   Partly answered by the dump: dungeons pick parts by `QuestTags`, with an optional `Injectable` sub quest slot (see
   the dump section). The graph that does the choosing, and the registry question, are still open. Until then,
   spawn-table links for new dungeons are unverified. Tracked in ClickUp as
   "#23 How dungeons are generated per zone".
