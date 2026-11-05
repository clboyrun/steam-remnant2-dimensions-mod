# Injectable reusability matrix

Which injectables can interact with which dungeon, per new world. This is a scaffold: only the Injectable slot itself is confirmed from the FModel dump (PR #20). Every cell is **untested** until the dungeon-injection investigation (ClickUp "Investigate dungeon injection so our instance tables tie to a dungeon spawn", parent #23) has run. Nothing here defines campaign structure or boss mechanics.

## What is confirmed

- `Quest_Dungeon_Base_C` has an optional sub quest component `Injectable` (`RemnantQuestSubQuest`, `QuestTags = [Injectable]`, Optional), next to `TraitBook` `[Trait]`, `Zone_AIDirector` (`IncludeZoneGroups`), `Start`/`End` tiles, a `Zone` and a waypoint link.
- The zone quest `Quest_AdventureMode_1Zone` picks parts by `QuestTags` (`MainDungeon` `[City, Dungeon, Miniboss]`, `SideD` `[City, Dungeon, Side]`, `TraitBook`, `Simulacrum` and others).
- Boss quests use `Quest_Boss_Base` (`BossFightComplete`, `ActivateEndGameCinematic`, `ActivateBacktoWard13`). The Injectable slot is on the dungeon quest, **not** on the boss quest, so injectables reach a boss only through the dungeon that leads to it (inferred from the dump).
- Corrupted: any dungeon can roll Corrupted (Survivor 0%, Veteran 5%, Nightmare 5%, Apocalypse 10%, Vex and Torment 10%).

## Not known

- The graph that fills `Injectable`, and whether a tag match is enough for reuse across dungeons.
- Whether `SetSpawnTableOverride` on the zone is needed.
- Whether a pak-added quest is seen by `RegisteredQuests` (test card 86bcwayg0).

## Injectable kinds

| Kind | Source | Status |
| --- | --- | --- |
| Extra encounter group | `SpawnTags` fill zone encounter groups | untested |
| Loot link | `USpawnTableLink` into a game spawn table | untested |
| Miniboss | `MainDungeon` has `Miniboss` tag | untested |
| Trait book | `TraitBook` `[Trait]` slot (separate from Injectable) | untested |
| Simulacrum | zone slot `[Simulacrum]`, `ChanceToSpawn 20` | untested |
| Corrupted roll | applies to any dungeon | rule known, per-world test open |
| Fable quest | small quest tagged `Injectable`; starts in Grimm, world-agnostic after completion (see [aesop-fables-injectables.md](aesop-fables-injectables.md)) | untested |

## Matrix per world

Legend: `?` = untested, `n/a` = no such dungeon defined yet. "Inj" means any kind above.

| World | Dungeon (candidate) | Status | Inj reuse |
| --- | --- | --- | --- |
| Arcade | Maze chase | named by Astra | ? |
| Arcade | Side-scroller brawl (melee only) | named by Astra | ? |
| Arcade | Collectables run | named by Astra | ? |
| Arcade | shmup, light-gun, racer, platformer, brick breaker, falling blocks, rhythm, pinball, claw machine | suggested, not decided | ? |
| Grimm | 26 tale dungeons | options, none chosen (G4) | ? |
| Lovecraft | none defined | n/a | n/a |
| Hell | none defined (sins are bosses) | n/a | n/a |

Boss rows: the eight Lovecraft bosses, seven sins, the demon and the arcade final boss each have an "injectable compatibility" card; all inherit the rule that they are reached through a dungeon, with the dungeon undefined.

Fable rows: the 18 Aesop fables each have a per-world fit and mostly-data-or-Blueprint note in [aesop-fables-injectables.md](aesop-fables-injectables.md). Those cells are all `?` too, and the fable test order there (F18, F14, F10, F4) answers the reuse questions below.

## Reuse questions to answer by test

1. Can one injectable quest be tagged `[Injectable]` and be picked by dungeons of different worlds?
2. Does reuse depend on extra tags (world biome tag) on the injectable?
3. Can an Arcade machine dungeon use the same injectable as a Grimm tale dungeon?
4. Does Corrupted apply to the injectable's spawns as well as the dungeon's?
5. Can `RequiredAward` (or a save flag) gate an injectable so a fable unlocks for other worlds only after its Grimm run?
