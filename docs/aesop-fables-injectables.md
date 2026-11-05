# Aesop's Fables as injectable quests

Design catalog. The unlock rule below is decided (Astra, 2026-11-05); the fables themselves are options, none chosen, and nothing here defines campaign structure or boss mechanics. All reuse cells are **untested** (see [injectable-reusability.md](injectable-reusability.md)). Nothing here has been run in Unreal or through pak-builder.

## Idea and unlock rule (Astra, 2026-11-05)

Fables start in the Grimm world. Each fable unlocks **globally and individually, after it has been played**: completing one fable makes that fable (and only that one) world-agnostic, so it can be injected into any dungeon where it fits. Completing Grimm does not unlock the others.

- "Played" means completed. Failing or abandoning a fable does **not** unlock it.
- Unlocking does not move the fable out of Grimm: it simply joins the global pool, so it stays available in Grimm as well as in every world where it fits.

## Realism check

What the FModel dump confirms (PR #20/#21):

- `Quest_Dungeon_Base_C` has one optional sub quest slot `Injectable` (`RemnantQuestSubQuest`, `QuestTags = [Injectable]`). A fable is a small quest tagged `Injectable`, so the slot is the right hook.
- The slot is on the dungeon quest, not the boss quest. A fable reaches a boss only through the dungeon that leads to it (inferred).
- Quests carry `QuestTags` and an optional `RequiredAward` (account award), both data fields. Those are the two levers for "Grimm first, then everywhere".

What is not known (blockers for the whole idea, not per fable):

1. What fills `Injectable`, and whether a tag match alone lets a dungeon of another world pick it.
2. Whether a pak-added quest is seen by `RegisteredQuests` (test card 86bcwayg0).
3. Whether `SetSpawnTableOverride` on the zone is needed for extra encounters.
4. Whether Corrupted applies to the fable's spawns.
5. Whether a pak can add account awards, and whether an award can be granted from a quest completion without Blueprint.

Verdict: realistic as a small-quest system, with the per-fable unlock being the riskiest part. The first four fables in the test order below are chosen to test the mechanism cheaply.

## Unlock lifecycle

Decided: one unlock per fable, global, granted only when that fable is completed. Two ways to model it:

| Option | How | Data-only? | Risk |
| --- | --- | --- | --- |
| A. Two quest copies per fable | Grimm copy tagged `[Injectable, Jungle]`. A second copy tagged `[Injectable]` with `RequiredAward` set to that fable's own award, granted when the Grimm copy is completed. | Mostly data; the award grant needs a completion hook (Blueprint). 18 fables means 18 awards | Doubles assets. Needs `RequiredAward` to filter selection (untested) |
| B. One quest, tag swap | One quest; a Blueprint adds the world-agnostic tag on completion and stores a per-fable save flag | No, Blueprint logic and a persistent flag per fable | Needs a save-flag read at selection time (doc row 26/33) |

Recommendation: start with A, because it keeps most of the work in data and the award is already account-level, which gives "global" for free. With A, the unlock state is one award per fable, so there is no shared counter to keep in sync. Because an unlocked fable stays available in Grimm, the Grimm copy keeps its `Jungle` tag and the world-agnostic copy is added alongside it, never swapped for it.

The award must be granted on completion only, not on start, fail or abandon. Whether the quest's `Complete` state can trigger that without Blueprint is part of unknown 5.

## Data-only vs Blueprint (legend)

- **Data**: encounter groups via `SpawnTags`, loot links via `USpawnTableLink`, quest tags, awards. Expressible as quest/spawn data.
- **BP**: needs Blueprint logic (state, timers, choices, NPC behavior, item checks). Ring and mutator logic is Blueprint per the dump, so anything touching those is BP too.

Rewards are suggestions in kind only (no numbers); they should reuse existing item types and the Quest Books system where a fable leaves a hint or lore entry.

## Catalog

Format mirrors the Grimm tale catalog (doc 4.1): objective, moral, reward, data vs BP, fit. Fit is a design judgment (natural / possible / poor); the status of every cell is `?`.

### F1. The Tortoise and the Hare

- **Objective**: Reach the dungeon's end before a fast rival NPC, who is faster on open ground.
- **Moral**: Slow and steady wins the race.
- **Reward**: Trinket-type reward for finishing without sprinting the whole way (suggested).
- **Mechanics**: Data: rival spawn. BP: race timer, sprint tracking, rival napping state.
- **Fits**: Grimm natural (forest paths). Arcade natural (racer). Lovecraft possible. Hell poor.

### F2. The Boy Who Cried Wolf

- **Objective**: A scared NPC calls "wolf" at a false alarm twice; the third time the threat is real and unwarned.
- **Moral**: Liars are not believed even when they tell the truth.
- **Reward**: Loot link on surviving the real ambush (suggested).
- **Mechanics**: Data: ambush encounter group. BP: fake alarm triggers that spawn nothing, then the real one.
- **Fits**: Grimm natural. Arcade possible. Lovecraft natural (false alarms and dread). Hell possible.

### F3. The Fox and the Grapes

- **Objective**: Reach an item placed just out of reach; leaving it earns a different consolation reward.
- **Moral**: It is easy to despise what you cannot get.
- **Reward**: A consolation item from a loot link, or the unreachable one if a route is found (suggested).
- **Mechanics**: Data: loot placement. BP: reachability gate and alternate payout.
- **Fits**: Grimm natural. Arcade natural (claw machine). Lovecraft possible. Hell natural (Tantalus-like).

### F4. The Goose That Laid the Golden Eggs

- **Objective**: Escort a goose. Taking the eggs yields steady small rewards; killing the goose gives one burst and ends the quest.
- **Moral**: Greed destroys the source of gain.
- **Reward**: Scrap-type rewards repeated over time, or one large burst (suggested; the choice is the point).
- **Mechanics**: Data: loot links. BP: escort NPC, choice tracking. Good first test of persistent choice flags.
- **Fits**: Grimm natural (Falada/goose-girl neighbor). Hell natural (Greed). Arcade possible. Lovecraft possible.

### F5. The Ant and the Grasshopper

- **Objective**: Gather supplies for an NPC before a hazard phase begins; an idle NPC later begs for a share.
- **Moral**: Prepare for the days of necessity.
- **Reward**: Supply cache reward scaled to what was collected (suggested).
- **Mechanics**: Data: collectable placement. BP: counter, phase trigger, begging NPC.
- **Fits**: Grimm natural. Arcade natural (collectables run). Lovecraft possible. Hell possible (Sloth).

### F6. The Lion and the Mouse

- **Objective**: Spare a small enemy that offered a bargain; later it frees the player from a trap.
- **Moral**: Little friends may prove great friends.
- **Reward**: The mouse as a one-use rescue, plus a keepsake (suggested).
- **Mechanics**: Data: trap and small NPC spawn. BP: spare/kill choice, delayed payoff, rescue logic.
- **Fits**: Grimm natural. Arcade possible. Lovecraft possible. Hell poor.

### F7. The Town Mouse and the Country Mouse

- **Objective**: Choose between a rich, dangerous area and a poor, safe one to retrieve the target item.
- **Moral**: Simple safety beats dangerous luxury.
- **Reward**: Risky route pays more; safe route pays less but without ambush (suggested).
- **Mechanics**: Data: two encounter-group sets. BP: route choice logic.
- **Fits**: Grimm natural. Arcade natural (two level routes). Lovecraft possible. Hell natural.

### F8. The Dog and Its Reflection

- **Objective**: Carry an item across water or glass; a reflected copy lures the player to drop it.
- **Moral**: Grasping at shadows loses the substance.
- **Reward**: The carried item, or nothing if dropped (suggested).
- **Mechanics**: Data: reflection decoy spawn. BP: carry state, drop trigger.
- **Fits**: Grimm natural. Arcade natural (pinball/claw). Lovecraft natural (mirror horror). Hell possible.

### F9. The Wolf in Sheep's Clothing

- **Objective**: Find the disguised predator among a flock of NPCs.
- **Moral**: Appearances can deceive.
- **Reward**: Loot link on a correct find (suggested).
- **Mechanics**: Data: flock spawn. BP: selection and reveal logic; hidden-wolf tell.
- **Fits**: Grimm natural (Red Riding Hood neighbor). Lovecraft natural. Hell natural. Arcade possible.

### F10. The Crow and the Pitcher

- **Objective**: Fill a basin with stones to raise an item or key into reach.
- **Moral**: Little by little does the trick.
- **Reward**: Access to a hidden cache (suggested).
- **Mechanics**: Data: stone pickups, cache. BP: fill counter and unlock.
- **Fits**: Natural almost anywhere: Grimm, Arcade (puzzle), Lovecraft, Hell. Good cheap test of cross-world reuse.

### F11. The Fox and the Crow

- **Objective**: Flatter or trick an enemy into dropping a prized item (or be tricked yourself).
- **Moral**: Do not trust flatterers.
- **Reward**: The dropped item (suggested).
- **Mechanics**: Data: carrier enemy. BP: dialogue/bait trigger, drop on trigger.
- **Fits**: Grimm natural. Arcade possible. Lovecraft possible. Hell natural (demon bargains).

### F12. The North Wind and the Sun

- **Objective**: Make a hostile NPC give up an item; force fails, kindness succeeds.
- **Moral**: Gentleness succeeds where force fails.
- **Reward**: The item plus a peaceful-resolution bonus (suggested).
- **Mechanics**: Data: NPC and item. BP: damage-dealt detection, dialogue branch.
- **Fits**: Grimm natural. Arcade poor. Lovecraft possible. Hell possible.

### F13. Belling the Cat

- **Objective**: Place a warning bell on a dangerous patrol enemy; the bearer takes the risk.
- **Moral**: Easy to propose, hard to do.
- **Reward**: The bell makes the patrol audible for the rest of the dungeon (suggested).
- **Mechanics**: Data: patrol enemy. BP: bell attach, audio cue.
- **Fits**: Grimm natural. Arcade natural (stealth/maze chase). Lovecraft natural. Hell possible.

### F14. The Dog in the Manger

- **Objective**: Fight a guardian that hoards a resource it cannot use, and release it.
- **Moral**: Do not begrudge others what you cannot use.
- **Reward**: Resource cache released on defeat (suggested).
- **Mechanics**: Data: guardian and cache. BP: little beyond a guardian fight.
- **Fits**: Natural almost anywhere. Hell natural (Envy/Greed).

### F15. The Frogs Who Desired a King

- **Objective**: Choose a ruler for a group of NPCs; a harmless log becomes a tyrannical stork.
- **Moral**: Be careful what you wish for.
- **Reward**: Depends on the outcome; fighting the stork gives the loot (suggested).
- **Mechanics**: Data: stork elite spawn. BP: choice and escalation.
- **Fits**: Grimm natural (the Frog King neighbor). Arcade possible. Lovecraft natural. Hell natural.

### F16. Androcles and the Lion

- **Objective**: Help an enemy beast; later it protects the player in an arena.
- **Moral**: Gratitude and kindness are repaid.
- **Reward**: The beast joins a later fight as an ally (suggested).
- **Mechanics**: Data: beast spawn. BP: ally logic, remembered kindness.
- **Fits**: Grimm natural. Arcade natural (gladiator-like). Lovecraft poor. Hell possible.

### F17. The Oak and the Reed

- **Objective**: Survive a hazard storm by bending: an object that resists breaks.
- **Moral**: Flexibility beats stiff pride.
- **Reward**: Hazard-immunity trinket effect for the rest of the dungeon (suggested).
- **Mechanics**: Data: hazard zone. BP: stance/dodge check.
- **Fits**: Grimm natural. Arcade possible. Lovecraft natural. Hell natural (Pride).

### F18. The Bundle of Sticks

- **Objective**: Defeat an enemy group that is strong together; split it up and each one is weak.
- **Moral**: United we stand, divided we fall.
- **Reward**: Loot link per separated group (suggested).
- **Mechanics**: Data: grouped encounter spawn with `SpawnTags`. BP: link/separate logic if a mechanic is wanted; otherwise data-only.
- **Fits**: Natural almost anywhere. Cheapest candidate for pure data.

## Summary matrix

Status of every cell: `?` (untested). The note is the fit judgment, not a test result.

| Fable | Grimm | Arcade | Lovecraft | Hell | Mostly |
| --- | --- | --- | --- | --- | --- |
| F1 Tortoise and Hare | ? natural | ? natural | ? possible | ? poor | BP |
| F2 Boy Who Cried Wolf | ? natural | ? possible | ? natural | ? possible | BP |
| F3 Fox and Grapes | ? natural | ? natural | ? possible | ? natural | BP |
| F4 Golden Eggs | ? natural | ? possible | ? possible | ? natural | BP |
| F5 Ant and Grasshopper | ? natural | ? natural | ? possible | ? possible | BP |
| F6 Lion and Mouse | ? natural | ? possible | ? possible | ? poor | BP |
| F7 Town and Country Mouse | ? natural | ? natural | ? possible | ? natural | BP |
| F8 Dog and Reflection | ? natural | ? natural | ? natural | ? possible | BP |
| F9 Wolf in Sheep's Clothing | ? natural | ? possible | ? natural | ? natural | BP |
| F10 Crow and Pitcher | ? natural | ? natural | ? natural | ? natural | BP (small) |
| F11 Fox and Crow | ? natural | ? possible | ? possible | ? natural | BP |
| F12 North Wind and Sun | ? natural | ? poor | ? possible | ? possible | BP |
| F13 Belling the Cat | ? natural | ? natural | ? natural | ? possible | BP (small) |
| F14 Dog in the Manger | ? natural | ? natural | ? natural | ? natural | Data |
| F15 Frogs and King | ? natural | ? possible | ? natural | ? natural | BP |
| F16 Androcles | ? natural | ? natural | ? poor | ? possible | BP |
| F17 Oak and Reed | ? natural | ? possible | ? natural | ? natural | BP |
| F18 Bundle of Sticks | ? natural | ? natural | ? natural | ? natural | Data |

Only F14 and F18 look close to data-only. Everything with a choice, escort, timer or remembered outcome needs Blueprint logic.

## Suggested test order

1. F18 (data-only): does a tag-matched `Injectable` quest spawn in a Grimm-world dungeon at all, and does a pak-added quest reach `RegisteredQuests`?
2. F14: same quest reused in a dungeon of another world. Answers reuse questions 1 to 3 in the reusability doc.
3. F10: smallest Blueprint state (a counter). Tests whether a pak Blueprint can run on an injected quest.
4. F4: persistent choice flag; also the first test of the per-fable unlock award (option A) or tag swap (option B): completing it in Grimm must unlock only that fable elsewhere, failing or abandoning must not unlock it, and it must stay available in Grimm.

## pak-builder note

Per [pak-builder-json.md](pak-builder-json.md), JSON only describes Blueprint inheritance and components, not quest data. A fable would be one mapping whose `parent_class` is a quest Blueprint class from the dump, with its components; tags, awards and spawn data are set in the editor or remain open work. No mapping is written here, because the real parent class and component list need the dump.

## Open questions for Astra

1. Are morals just flavor, or should a fable branch (kind vs greedy) change what is rewarded? (Branching needs Blueprint.)
2. Is any non-Aesop fable (for example Panchatantra) in scope later?

Answered 2026-11-05: unlock is per fable, global, on completion only (failing or abandoning does not count), and an unlocked fable stays in Grimm as part of the global pool.
