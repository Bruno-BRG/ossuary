# Survivors, and the end of Version 22

Design and plan for **Version 24 — The ones that live**. It closes the two open ends of the
roadmap at once: the *Creatures* items the todo names next (named survivors, their return, the
detailed look) and the leftovers of Version 22 (*Spells that answer back*), with the six
remaining first-time hints riding along.

Requested by the maintainer on 2026-10-29: named survivors plus closing Version 22. Once
shipped, the systems are described in [`design/systems.md`](../design/systems.md) and this file
becomes history.

## The items

| Todo item | Decision |
|---|---|
| Named survivors | A monster that meets the hero and lives earns a name, a title, a memory and slow growth. |
| They come back | It stays on its level (levels now keep their monsters), is spoken of in towns, hunts the hero on the road, and its death is a deed with an epilogue line. |
| Detailed look | The look command composes build, age, mood, gear, material, wounds and missing parts, from a stable hash. |
| V22: rider statuses | Slow, Weaken, Bleed, Fear, Sleep and Root reach the hero, so every rider in the catalogue means something. |
| V22: animation limits | An effect no longer hides the cell it covers, and only Escape skips a running animation. |
| V22: the Magister's music | The Sallow Magister gets its own boss track instead of the generic one. |
| Tutorials | The six remaining first-time hints: crafting, casting, towns, gathering, altars, jobs. |

Ecology (lairs, sleep, corpse-eating, off-screen factions) stays open in *Creatures*: it is
world simulation, not identity, and belongs to Version 25 on top of the roster built here.

## 1. Identity

### What a monster knows about itself

`Monster` gains:

- `SurvivorId` — null until it is promoted; then a stable string (`"s" + Uid`).
- `Titles` — up to three epithets, in the order earned.
- `Encountered` — it has traded a blow with the hero (set where the hero damages it and where
  it damages the hero).
- `Memory` — a thin record (encounters, times it fled, times it was wounded), not the
  townsfolk `Persona`.

`Actor` gains the timed statuses (see §5) and `Monster` loses its own `SlowTurns`/`FearTurns`
duplicates.

### The level roster

Today a level is cached (`Dungeon._levels`) but its monsters are not: `DescendTo` clears
`Monsters` and `Dungeon.Ensure` returns `spawns = null` on a revisit, so **a level you come
back to is empty** — bosses included, despite the changelog. Version 24 fixes that, because a
survivor that is not there cannot return.

- `Dungeon` keeps `Dictionary<int, List<Monster>> _roster`, keyed by `GameMap.Number` (the same
  key `TrapTable` and `GroundItems` use).
- `Game.StashLevelRoster()` is called before every level change (`DescendTo`, `UsePortal`,
  `LeaveToOverworld`): it copies every monster that belongs to the level — everyone except the
  hero's companions and summons (`Companion`, `Ally`) — and removes the dead.
- `DescendTo` restores the roster on a revisit instead of leaving the level empty. The
  first-visit hooks (`RaiseBones`, `PlaceFolk`, `RaiseBosses`, `DressLevel`) keep their
  `spawns != null` guard, so nothing is duplicated; `PlaceCompanions` still runs every time.
- Wounded monsters keep their wounds between visits. That is the point: you can leave a
  crippled thing alive and meet it again.

Determinism: the roster is rebuilt by the same replay that rebuilt everything else, so nothing
new is saved. The save-format bump comes from the rule change itself (§8).

### Promotion

On leaving a level, `PromoteSurvivors()` walks the living hostiles that were encountered and
keeps **at most two per departure**, chosen deterministically (highest `Level`, then highest
HP, then lowest `Uid`). A monster is promoted once:

1. It gets a name from `Loc.Names` chosen by `Uid` (a hash, never an RNG draw).
2. It gets a title from what happened: *Biter of Heroes* if it wounded the hero; *the Sly* if it
   fled; *the Unbroken* if it was wounded and lived; *the Patient* otherwise.
3. It is added to `Game.Survivors` (see below) with its branch, depth and level.

A promoted monster that later meets the hero again earns another title (up to three) and gains
one level when the hero leaves it alive a second time, capped at base level + 5. Growth is
deterministic and bounded, so the balance bot sees a slow curve, not a spiral.

`Game.Survivors` is a `List<Survivor>` on the run (id, name, titles, species, level, branch,
depth, encounters, slain). It exists for the things the monster itself cannot narrate once it is
dead: rumours, the morgue, the epilogue. It is rebuilt by replay and needs no save field.

## 2. They come back

- **Their level.** The roster in §1. A survivor waits where you left it, wounded and angrier.
- **Rumours.** `HearRumour` gains a `SurvivorRumour` band: when a living survivor exists, one
  answer in nine names it, its title and the level it holds ("they say Grak, Biter of Heroes,
  holds the fourth floor of the Dungeons"). No new fact system: the survivor is the fact.
- **The road.** `Game.Events` gains one road event: a survivor whose level is within three
  depths of the hero's current branch tracks the party and ambushes it. Fleeing again leaves it
  alive, with a new title.
- **The deeper rival** is the same mechanic under a different trigger: on descending, a survivor
  from a shallower level of the same branch may have followed (one deterministic chance per
  descent, using the survivor id). It is re-placed near the stairs, not duplicated: its old
  level's roster loses it.
- **The deed.** `KillMonster` records a survivor as weight 3 (`RecordDeed`), which puts it in
  `MatteringDeeds` and the epilogue. `Game.Legends` adds it to the hero's legend; the morgue
  lists the survivors met, their titles and whether each was slain.

## 3. The detailed look

`Game.Targeting` composes a new `DescribeLook(Monster)` used by look and inspect:

- **Build and age** from a stable hash of `Uid` ("a lean", "a scarred", "an ancient").
- **Mood** from the monster's current state (asleep, fleeing, wounded, holding a lair).
- **Gear and material** — the wielded item and its material, named only when seen.
- **Wounds and missing parts** — the existing `DescribeWounds`.
- **Title** — a survivor's name and titles lead the sentence.

The hash is never the simulation RNG (`AGENTS.md`: looks are `hash(x, y)`), and it uses `Uid`
rather than the current cell, so the description does not change as the creature walks.

## 4. Grammar

`Actor.TakesArticle` exempts townsfolk today; a named survivor must be exempt too, or the game
prints "the Grak, Biter of Heroes". The rule becomes: a monster with a `SurvivorId` takes no
article. The name and every title are born in English and translated in `Loc.Names`/`Loc`.

## 5. Version 22 closure — the riders reach the hero

`MonsterSpells.ReachesHero` currently allows Burn, Confuse, Blind, Stun and Poison. The other
six become real hero statuses on `Actor` (shared with monsters, replacing the monster-only
duplicates):

| Rider | Field | Effect on the hero |
|---|---|---|
| Slow | `SlowTurns` | The world gets a move the hero does not: the monsters run again every other turn. |
| Weaken | `WeakenTurns` | The hero's blows and shots do 2 less damage (never below 1). |
| Bleed | `BleedTurns`, `BleedDmg` | HP loss each turn, blood on the floor; bandaging stops it. |
| Fear | `FearTurns` | The hero cannot attack or cast while afraid; movement is free. |
| Sleep | `SleepTurns` | The hero loses turns; a blow wakes them. |
| Root | `RootTurns` | The hero cannot walk or travel; stepping through a portal is allowed. |

Each one is decremented in `DecrementStatus` (or the existing monster tick), says its line, and
is cleared by the same things that clear its monster twin. Resistances, the `RiderPct` roll and
the "shake it off" roll stay as they are.

## 6. Version 22 closure — animation

- The effect glyph is drawn **under** the actors instead of over them, so a bolt crossing a cell
  no longer hides the creature standing on it.
- Only Escape skips a running animation; any other key is ignored until it ends. The key is not
  sent to the game, so it cannot open a panel mid-animation either.

## 7. Version 22 closure — the Magister's music

`audio.ts` gains a `boss-sallow-magister` track (synthesised, same identity as the other boss
music) instead of falling back to `boss`. The Audio todo gains the missing boss tracks it names.

## 8. Saves

The save format moves **23 → 24**: levels now keep their monsters, and the new hero statuses are
rules. `SaveStore.Version` and the load gate both move; Version 23 saves stop loading, as every
rule change does.

## 9. Tutorials

The six remaining hints (crafting, casting, towns, gathering, altars, jobs) follow the existing
`Hint` shape: one row in `Tutorials.All`, one Portuguese pair in `Loc.Tutorial`, a `ShowHint` at
the trigger and a `HintDone` at the closing verb. The altar hint must not be raised while its
panel is open (the card is marked seen at once), so it fires on the first altar action.

## 10. Tests and docs

- `FeatureTests`: promotion, roster across a revisit, a boss still present on return, titles,
  the deed, the ambush, the look.
- `CoreTests`/`ArsenalTests`: each of the six riders on the hero; the animation skip is a
  front-end test.
- `TutorialTests`: one case per new hint.
- `soak` and `balance` re-run: the roster makes revisits harder, which is intended and must not
  read as a regression.
- Docs: `design/systems.md` (monsters, levels, statuses), `game/controls.md` (no new keys),
  `design/spells-and-items.md` (known limits removed), `todo.md` (the done lines),
  `CHANGELOG.md` and `CHANGELOG.pt-BR.md`.
