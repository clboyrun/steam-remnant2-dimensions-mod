# Remnant 2 Dimensions

Build new Remnant 2 content (a world, an Adventure option, weapons, weapon mods, mutators, rings, amulets, armor,
traits, archetypes) from JSON files, and ship it as a normal `.pak` mod. No UE4SS or script loader is needed at
runtime.

You write definitions like this:

```json
{
  "kind": "weapon",
  "id": "Emberline",
  "weaponType": "longGun",
  "name": "Emberline",
  "icon": { "small": "art/emberline_icon.png" },
  "model": { "mesh": "models/emberline.fbx", "materials": [ { "parent": "/Game/...", "textures": { "BaseColor": "art/emberline_d.png" } } ] },
  "stats": { "damage": 62, "rateOfFire": 2.1, "magazine": 7, "maxAmmo": 56 },
  "mod": "CinderBurst",
  "acquisition": [ { "type": "loot", "table": "AshfallLoot", "chance": 40 } ]
}
```

and `dimensions build` turns them into game assets in a UE 5.2 project, cooks them, and packs them into
`zz_Dimensions_<id>_P.pak/.utoc/.ucas` for `Remnant2\Content\Paks\~mods`.

## What's here

| Path | What it is |
| --- | --- |
| `schema/` | JSON schemas for `dimension.json` and every content kind. Point your editor at them for autocomplete. |
| `src/Dimensions.Definitions` | Library: loads a dimension, applies `extends`, checks schema, references, files and cultures. |
| `src/Dimensions.Pipeline` | Library: turns definitions into a build plan, runs the editor, cooks, packs, installs. |
| `src/Dimensions.Cli` | The `dimensions` command. |
| `data/catalog.json` | What the generator knows about the game's classes (templates, row structs, stat field names). Editable. |
| `unreal/Python` | The UE 5.2 editor script that applies a build plan to the project. |
| `unreal/Plugins/DimensionsEditor` | A tiny editor plugin (one asset factory) the script uses for spawn tables and other plain objects. |
| `examples/Ashfall` | A complete example dimension with placeholder art. |
| `gh-pages` branch | [Project status page](https://clboyrun.github.io/steam-remnant2-dimensions-mod/), refreshed hourly from ClickUp ([how it works](https://github.com/clboyrun/steam-remnant2-dimensions-mod/blob/gh-pages/README.md)). |
| `docs/` | [Architecture and what's proven vs. not](docs/architecture.md), [authoring reference](docs/authoring.md). |

## Quick start

You need: Windows, **Unreal Engine 5.2**, Visual Studio with C++ (to build the project once), the .NET 10 SDK,
[Rem2Proj](https://github.com/narknon/Rem2Proj) (a UE 5.2 project that mirrors Remnant 2's classes), and for the
game's IoStore format [retoc](https://github.com/trumank/retoc) on your PATH. Players need a pak signature bypass
such as Narknon's "Allow Asset Mods", like every Remnant 2 asset mod.

```powershell
# 1. Once: put the editor plugin into the project, then build Remnant2.uproject in Visual Studio (Development Editor).
dotnet run --project src/Dimensions.Cli -- setup --project C:\Mods\Rem2Proj\Remnant2.uproject

# 2. Check your dimension and see what it will do (no Unreal needed).
dotnet run --project src/Dimensions.Cli -- validate examples/Ashfall
dotnet run --project src/Dimensions.Cli -- plan examples/Ashfall -o plan.json

# 3. Generate, cook, pack, and copy into the game.
dotnet run --project src/Dimensions.Cli -- build examples/Ashfall --project C:\Mods\Rem2Proj\Remnant2.uproject --install
```

`plan` lists the game assets that must be in the Unreal project before the editor step (the templates you derive
from and any game lists you add to). Rem2Proj has some of them; export the rest from the game with FModel (using
your `.usmap`) and import them at the same path with JsonAsAsset. The editor step stops and lists anything missing.

Start your own with `dimensions new MyDimension --id MyDimension`.

## Status

The JSON side (schemas, validation, planning) is built and tested. The Unreal side (editor script, cooking,
packing) is written against UE 5.2's APIs but hasn't been run against the game yet, and several game facts in
`data/catalog.json` are marked `verified: false`. [docs/architecture.md](docs/architecture.md#first-test-in-game)
lists the first in-game tests that settle them.
