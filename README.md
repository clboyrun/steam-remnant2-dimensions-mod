# Remnant 2 Dimensions

A Remnant 2 content mod (new worlds, weapons, mutators, rings, armor, archetypes and more), shipped as a normal
`.pak` with no UE4SS or script loader at runtime. Players need a pak signature bypass such as Narknon's
"Allow Asset Mods", like every Remnant 2 asset mod.

## What's here

| Path | What it is |
| --- | --- |
| `Rem2Proj/` | The UE 5.2 project (`Remnant2.uproject`, C++ headers, Config, stand-in Content) that content is built in. |
| `tools/pak-builder` (on the `pak-builder` branch for now) | The build path: generates Blueprints, cooks and packs the pak. Python. **All building goes through this.** |
| `tools/clickup-sync` | ClickUp sync tray tool. |
| `data/catalog.json` | Game facts (template paths, row structs, stat field names). Entries marked `verified: false` still need checking in FModel. |
| `docs/pak-builder-json.md` | The JSON format pak-builder reads to create Blueprints and a pak. |
| `docs/architecture.md` | How the game finds mod content, risks and unknowns, and the first in-game tests. |
| `docs/injectable-reusability.md` | Which injectables can interact with which dungeon, per new world (all untested). |
| `docs/aesop-fables-injectables.md` | Catalog of 18 Aesop's Fables as injectable quests: start in Grimm, then world-agnostic. Design options only. |
| `docs/world-content-cards.md` | Index of the per-world and per-boss ClickUp content cards. |

## Building

Use `tools/pak-builder`; see its README. It currently lives on the `pak-builder` branch and is not in `main` yet.

## History

An earlier JSON-to-pak toolchain (a C# `dimensions` CLI with schemas, validation and its own cook/pack stage) was
proposed in PR #1 and dropped on 2026-11-04 in favour of pak-builder as the single build path. Its code stays in
the history of that closed PR. Its game research and catalog were kept here.
