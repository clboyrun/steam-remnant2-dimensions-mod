# Authoring reference

A dimension is a folder:

```
MyDimension/
  dimension.json        manifest
  content/**/*.json     definitions (any layout you like)
  art/, models/ ...     files the definitions point at
```

Paths to files are relative to the folder with `dimension.json`. Paths that start with `/Game/` or `/Script/` are
Unreal object paths (game assets, or assets you made by hand under `/Game/Dimensions/<id>/`). Anything else in a
reference field is the **id** of another definition in the dimension.

Add `"$schema"` to each file (see `examples/Ashfall`) for autocomplete; `dimensions schema <folder>` writes copies
of the schemas anywhere you like. Comments (`//`) and trailing commas are allowed.

## dimension.json

| Field | |
| --- | --- |
| `id` | PascalCase. Content goes in `/Game/Dimensions/<id>/`, the pak is `zz_Dimensions_<id>_P`. |
| `name`, `version`, `author`, `description` | `version` is `x.y.z`. |
| `defaultCulture` | Culture of plain-string text, default `en`. |
| `content` | Globs for definition files, default `["content/**/*.json"]`. |
| `pak.name`, `pak.chunkId`, `pak.format` | `format` is `iostore` (default, the game's format) or `legacy`. |

## Every definition

| Field | |
| --- | --- |
| `kind` | `weapon`, `weaponMod`, `mutator`, `ring`, `amulet`, `armor`, `trait`, `archetype`, `lootTable`, `world`, `adventure`, `npc`, `enemy`, `animationSet`, `dialogue`, `quest`, `questItem`, `itemCategory`, `blueprint` |
| `id` | Unique in the dimension, PascalCase. Asset names come from it (`Weapon_<id>`, `Ring_<id>`...). |
| `extends` | Id of another definition of the same kind. Objects merge key by key; arrays and values replace; `null` removes. |
| `abstract` | `true`: only a base for `extends`, nothing generated. |
| `template` | Game class/blueprint this is made from (most kinds have a default in the catalog). |
| `assetName` | Override the generated asset name. |
| `name`, `description`, `flavorText` | Text: a string, or `{ "en": "...", "de": "..." }`. |
| `icon` | `{ "small": "art/x.png", "large": ..., "hud": ... }`. PNG/TGA, imported as UI textures. |
| `acquisition` | List of ways to get it (below). |
| `properties` | Raw Unreal property overrides on the generated asset (below). They win over generated values. |
| `components` | Raw property overrides on the blueprint's components, by component name. |

### Acquisition

```json
"acquisition": [
  { "type": "loot", "table": "AshfallLoot", "chance": 40, "quantity": 1, "levelMin": 1, "levelMax": 20 },
  { "type": "loot", "table": "/Game/.../ST_SomeGameTable.ST_SomeGameTable" },
  { "type": "craft", "recipeList": "/Game/.../SomeRecipeList", "materials": [ { "item": "/Game/...Mat_C", "quantity": 3 } ], "scrap": 500 },
  { "type": "vendor", "recipeList": "/Game/.../VendorList", "scrap": 1500 },
  { "type": "startingGear", "archetype": "Pyrelord" }
]
```

A game table or list becomes an override in the pak (see [architecture](architecture.md#how-new-content-gets-found)).
`questReward` is accepted but not wired yet (rewards live in the quest graph).

## Kinds

**weapon**: `weaponType` (`longGun`, `handGun`, `melee`), `model`, `stats` (`damage`, `rateOfFire`, `magazine`,
`maxAmmo`, `ammoPerReload`, `range`, `falloffStart`, `falloffEnd`, `spread`, `pellets`, `automatic`, `burstCount`,
`burstRateOfFire`, `scope`, `scopeFov`; any other key goes into the row as written), or `statsRow` to reuse a game
row, `mod` (a fixed weaponMod), `mutatorSlot`.

**model** (weapons, armor): `mesh` (FBX, or glTF for static meshes), `type` (`skeletal` default, or `static`),
`skeleton` (game skeleton to bind to), `scale`, `component` (the template's mesh component, default `Mesh`),
`materials`: `[ { "parent": "<master material>", "slot": "...", "textures": { "<param>": "file or asset" }, "scalars": {}, "vectors": { "Tint": [1, 0.5, 0.2] } } ]`,
`importOptions` (FBX import settings passed through).

**weaponMod**: `template` (an existing mod to start from), `powerRequired`, `charges`, `toggleable`, `stats`.

**mutator**: `template`, `slot` (`ranged`/`melee`), `maxLevel`, `levels` (stats per level, padded with the last).

**ring**, **amulet**: `template`, `stats`.

**armor**: `template`, `slot` (`head`, `body`, `hands`, `legs`), `model`, `armor`, `weight`, `resistances`
(`bleed`, `fire`, `shock`, `blight`, `toxin`), `stats`.

**trait**: `maxLevel`, `cost`, `levels` (`{ "Stat": [one value per level] }`).

**archetype**: `name`, `description`, `briefDescription`, `flavorTags`, `difficulty`, `image`, `archetypeTrait`
(`levels`, `template`, `properties`), `engram` (`name`, `description`, `icon`, `acquisition`, `template`),
`startingGear` (items), `startingTraits` (`[{ "trait": "...", "level": 1 }]`), `uiTags`, `registration`.

**lootTable**: `tableType` (ESpawnTableType: `List`, `Table`, `TableGroup`, `IndividualChance`,
`IndividualChanceOneShot`), `rolls` (`min`, `max`), `entries` (`item` or nested `table`, `chance`, `quantity`,
`levelMin`, `levelMax`, `tags`; or `character`: an npc/enemy to spawn, for encounter tables), `injectInto` (`[{ "table": "<game spawn table>", "chance": 100 }]`).

**world**: `biomeName`, `loot`, `zones`: `[{ "id", "name", "template", "maps": ["/Game/Dimensions/<id>/Maps/..."], "tileSet", "loot", "encounters", "properties" }]`, `registration`.
Maps are made by hand in the editor. `campaign`: `{ "alternativeTo": "yaesha", "chance": 0.25, "quest": "<storyline quest>" }`
makes the world an **extra** roll for that base campaign slot (`losomn`, `yaesha`, `nerud`); the base world stays in
the pool. This is how the World of Grimm (Yaesha slot) and the Lovecraftian world (N'Erud slot) plug in.

**adventure**: `template` (a root quest), `world`, `container` (the persistent map), `levelMin`, `levelMax`,
`rarity` (`Common`...`Legendary`), `icon`, `registration`, `storyline` (quests it can roll: ids or
`{ "quest", "chance", "optional" }`). `mode`: `adventure` (an Adventure mode option, default) or `storyCampaign` (a
standalone story picked from the campaign menu, like One True King), with `once` (finish once per character) and
`difficulty` (extra difficulty steps).

**animationSet**: `skeleton`, `template` (a game AnimationSet to start from: your IDs replace or add to its own),
`animations`: `{ "<AnimationID>": { "files": ["animations/x.fbx"], "loop", "blendTime", "blendOut", "startTime", "tags" } }`.
Several files under one ID play at random. Files can also be game animation assets.

**enemy**: `template` (a game enemy, whose AI is kept), `model`, `animBlueprint` (`{ "template": "<game ABP>",
"layers": { "<layer NameID>": "<animationSet>" } }`), `stats` (its stats row), `faction`, `loot` (dropped on death).
Spawn it from a loot table entry with `character`, and use that table as a zone's `encounters`.

**npc**: `template` (a game NPC), `model`, `animBlueprint`, `stats`, `faction`, `dialogue`, `vendor`:
`{ "items": [ { "item", "scrap", "materials", "requires": [ { "item", "quantity", "show": "hidden" | "secret" } ] } ] }`.
`requires` gates stock: the entry is hidden (or shown as ???) until the player owns that item. Each vendor entry has
4 ingredient slots shared by materials, scrap and requires. `unlock`: `{ "after": "<quest>", "state": "Complete",
"objective" }` and `location`: `{ "hub": "ward13" | "map": "...", "position": [x, y, z], "rotation" }` say when and
where the NPC appears (see the open points in [architecture](architecture.md#risks-and-unknowns)).

**dialogue**: `nodes`, a list of steps. Each node does one thing:

| Node | |
| --- | --- |
| `{ "say": "...", "facial", "audio" }` | A line. |
| `{ "choices": [ { "label": "...", "hidden", "then": [...] } ] }` | Player options. |
| `{ "trigger": "Interact", "then": [...] }` | Runs when the named trigger fires (talking to the NPC). |
| `{ "onItem": "<item>", "then": [...] }` | Runs when the player hands over that item. |
| `{ "questSwitch": [ { "when": "QuestInProgress", "objectives": [...], "then": [...] } ] }` | Branches on the owning quest's state; `when` is `QuestNotStarted`, `QuestInProgress`, `QuestCompleteSuccess`, `QuestCompleteFailure` or `All`. |
| `{ "activateObjective": "Id" }`, `{ "completeObjective": "Id" }`, `{ "completeQuest": "Success" }` | Quest steps. |
| `{ "event": "Name", "context" }` | Sends an event to the quest or level blueprint (open a vendor, give a reward). |
| `{ "jump": "<node id>" }`, `{ "exit": true }` | Go to a node with that `id`, or end the conversation. |
| `{ "node": "/Script/...EventTreeNode_X", "properties": {} }` | Any other node class. |

Any node can have `id` (a `jump` target) and `once` (runs the first time only).

**quest**: `template` (a game quest to start from), `name`, `description`, `questType`, `levelMin`, `levelMax`,
`container` (its map or tile), `objectives` (`[{ "id", "name", "description", "hidden" }]`), `entities`
(`[{ "id", "name", "npc", "dialogue" }]`: who talks during the quest), `rewards`. Add it to an adventure's `storyline`.

**itemCategory**: `name`, `icon`, `group` (`playerQuest` default, `quest`, or `other`: which game category class
it's made from). A section of the inventory. Any item (weapon, mod, mutator, ring, amulet, armor, questItem) joins it
with `"category": "<itemCategory id>"` (or a game UItemCategory class). The Clementine example puts the campaign
completion tokens in a `CompletionTokens` category.

**questItem**: `template` (a game quest item), `name`, `description`, `icon`, `quest`, `category`. Hand it in with `onItem`, sell
it from a vendor, or drop it from a loot table.

See `examples/Ashfall/content/story` (an enemy, an NPC and a hand-in quest) and `content/hub/clementine.json` (a
Ward 13 NPC that unlocks after Root Earth, gives clues with `questSwitch`, and sells quest books gated on campaign
tokens).

**blueprint**: anything else. `template` (any class), `folder` (under the content root), `asset` (`blueprint`
child class, `copy` of an asset, or `dataAsset`/object of a class), `properties`, `components`.

### registration (archetype, world, adventure)

```json
"registration": { "mode": "discover" }
"registration": { "mode": "append", "list": "/Game/.../SomeList", "property": "Entries" }
"registration": { "mode": "replace", "replaces": "/Game/.../Quest_AdventureMode_Jungle.Quest_AdventureMode_Jungle_C" }
```

## Raw properties

`properties` and `components` take Unreal property names (`MaxStackCount`, `bToggleable`) and plain JSON values,
converted to the property's type. For values JSON can't express, use a tagged object:

| Tag | Value |
| --- | --- |
| `{ "$ref": "Id" }` | Another definition (its class, or its asset for loot tables) |
| `{ "$asset": "/Game/..." }` | Object reference |
| `{ "$class": "/Game/....X_C" }` | Class reference |
| `{ "$soft": "/Game/..." }`, `{ "$softClass": "..." }` | Soft references |
| `{ "$text": "..." }` | FText |
| `{ "$name": "..." }` | FName |
| `{ "$enum": "ValueName" }` | Enum value, as named in the header |
| `{ "$rowHandle": { "table": "...", "row": "..." } }` | DataTable row handle |
| `{ "$struct": { ... }, "$type": "StructName" }` | Struct (type needed inside empty arrays) |
| `{ "$new": "/Script/Module.Class", "properties": { ... } }` | New instanced subobject (spawn table entries, recipes) |

Property names come from the headers in Rem2Proj's `Source/` folder, or FModel.
