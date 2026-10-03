# Balance — method and measurements

The RPG system (races, classes, magic, perks, items, gods, surfaces) was balanced with a **deterministic
bot** instead of gut-feel numbers. This document says how to run it, what it measures, what was
adjusted and what it **cannot** tell you.

## How to run

```powershell
.\headless.ps1 balance 10 2500            # 10 seeds per combination, 2500 turns, "cautious" policy
.\headless.ps1 balance 10 2500 dive       # "dive" policy: descends without caring about level
.\headless.ps1 balance 4 3000 role=wizard race=elf trace   # one combination, logging every 50 turns
```

560 runs (8 classes × 7 races × 10 seeds) take about a minute. The output shows survival,
average depth, level, deaths and most common death, per class, per race and in the class × race matrix.

## The bot (`engine/Ossuary.Headless/Balance.cs`)

The same for everyone, on purpose: it studies the books it has, spends perks from a per-class list, equips the
best armor and weapon it finds, reads *enchant*, drinks gain potions, heals when HP drops below 40%, summons and
buffs before fighting, uses the class's best spell or ability (Cleave with 2+ neighbors, Fireball on
groups, Aimed Shot at range), rests before moving on (HP 75%, Mp 80%), eats, picks up items and descends.
It walks the real map (BFS, opens doors with the door verb). It does **not** pray at altars, buy anything,
use unknown wands or scrolls, or switch branches: it measures class, race, level and abilities,
not the player's cleverness.

`BalanceBand` (in the tests) is a guard rail, not a goal: no class may survive less than 50% or fall
below 60% of the best one's depth.

## Baseline (10 seeds, 2500 turns, "cautious")

| Class | Surv. | Depth | Level | Monster kills |
|---|---|---|---|---|
| fighter | 100% | 7.7 | 7.8 | 62.6 |
| ranger | 100% | 7.6 | 7.8 | 62.4 |
| cleric | 100% | 7.5 | 7.6 | 60.4 |
| paladin | 100% | 7.4 | 7.4 | 60.4 |
| adventurer | 100% | 7.0 | 7.1 | 56.3 |
| rogue | 100% | 7.0 | 7.2 | 56.4 |
| wizard | 100% | 6.9 | 7.0 | 52.5 |
| necromancer | 100% | 6.5 | 6.6 | 46.5 |

Overall average: depth 7.2, level 7.3. Classes stay within −10% / +7% of the average; races within
±5% (human/orc 7.4, dwarf 7.0). Under the "dive" policy the average reaches 9.1 of 10 with 99% survival.

## What was adjusted (and why)

- **The bot came first.** The first runs "died" 15–40% of the time from bot defects (stuck on doors,
  ignoring slugs, thinking it was locked in combat). Only after fixing those do the numbers mean anything.
- **Casters died and were slow**: Wizard 3→4 HP/level, base Mp 6→8; Necromancer 3→4 HP/level, base Mp 5→9,
  starts with *Drain Life* and *Raise Skeleton* (before, only *Sleep*, with no attack); *Drain Life* 6→5 Mp and
  *Raise Skeleton* 7→6. Faster Mp regeneration (`14 − stat − Magic/15`, before `16 − stat − Magic/20`).
- **Adventurer**: 4→5 HP/level and starts with *Power Strike* (was "average at everything" with no ability).
- **Fighter**: 6→5 HP/level (dominated with 98–100% and the greatest depth).
- **HP regeneration** slower (6–20 → 10–30 turns per point): resting costs real food.
- **Monster density**: `area/90 + depth/2` → `area/70 + depth×2/3` per level.
- **Potions and scrolls** were nearly useless: potions could not be drunk (now `Shift+Q`, 14 effects) and
  both potions and scrolls had 4–12 "charges" (one *enchant* gave +12). They are now single-use.

## What this does NOT measure

- **Absolute difficulty.** A bot that always rests and heals at 40% almost never dies in the Dungeons (10 levels).
  Real pressure comes from **hunger** (about 2400 turns of rations, plus what you find) and the deep branches.
  Still missing: a bot that enters the other branches and a combat review (monsters with status, ranged,
  poison/bleeding). That is the next balance round, and only real play can settle the tone.
- **Altars, shops, wands and artifacts.** Outside the bot's policy.
- **Dice are 0-based.** `Rng.Dice(n)` returns `0..n−1`, so `NdS` yields on average `N×(S−1)/2`, not
  `N×(S+1)/2`. It is the engine's convention from the start (hit and AC already assume it); the numbers in this document
  and in the spells were calibrated with it.

## Tuning knobs (where to tweak)

| What | Where |
|---|---|
| HP/level, base and per-level Mp, skill caps, perks and starting spells | `Entities/Roles.cs` |
| Attributes, HP/level, resistances and race traits | `Entities/Races.cs` |
| Cost, range and dice of each spell | `Magic/Spells.cs`, `Game.Magic.Effects.cs` |
| Ability cost and effect; Vigor | `Entities/Abilities.cs`, `Game.Abilities.cs`, `Player.RecomputeMaxVigor` |
| HP, Mp and Vigor regeneration | `Player.HpRegenInterval/MpRegenInterval/VigorRegenInterval` |
| Chance of magic items and affixes | `Items/Affixes.cs` (`ItemRoller`) |
| Monsters per level and spread | `LevelBuilder.SpawnMonsters`, `Entities/Bestiary.cs` |
| Piety, boons and god bonuses | `Entities/Gods.cs`, `Game.Gods.cs` |
