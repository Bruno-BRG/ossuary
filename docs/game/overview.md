# Overview — Ossuary

The current runtime is a Tauri 2 desktop app: a Rust shell, a bitmap Canvas terminal and an independent
C# simulation. See [`desktop.md`](../tech/desktop.md). The game is at **Version 11**.

A terminal roguelike: a glyph grid, turns, permadeath, and generation that is reproducible from a seed.

## Inspirations (project vision)

- **Dungeons: Rogue + NetHack**: procedural generation, stairs, traps, hunger, identification,
  one-key verbs, permadeath.
- **World: Fallout 1/2 + Caves of Qud**: an overworld of named regions, hub towns with shops and NPCs,
  travel that costs time, encounters, factions and services; history that emerges from simulation, not from a script.
- **Look: Dwarf Fortress**: dense, colourful ASCII, panels full of information, a raw terminal feel with a CRT on top.
  The detailed art direction ("Phosphor & Bone": 80s, dark UI, coloured glyphs) is in [`visual.md`](../tech/visual.md).

## Pillars

1. **Everything is a turn.** Walking, picking up, reading, travelling on the overworld: each action advances the
   clock and gives the monsters their move. Fast monsters act more often (an energy meter, NetHack style).
2. **Everything is a seed.** `Game(seed)` generates the dungeon, the overworld and the towns. The seed is the save:
   a save is the seed plus the keys pressed, replayed deterministically.
3. **Everything is testable without a GUI.** `Ossuary.Core` holds only simulation and UI-as-data;
   the headless suite (`headless.ps1 test`) covers generation, combat, shops, UI, spells and items.
4. **Magic is something you see.** Every spell, wand, scroll and trap that does something draws it: a fireball flies and bursts,
   lightning forks, meteors fall (see [`spells-and-items.md`](../design/spells-and-items.md)).

## Game loop

1. You start at the mouth of the Ossuary with a dagger or your class kit, rations and a little gold.
2. Explore (FOV 10), fight or evade, pick up loot, descend (`>`) to the bottom of the branch.
3. Climb back (`<` to the surface) to the overworld: roads, regions, day and night,
   random encounters, dungeon entrances and towns.
4. Vertical towns: wall, square, fountain, smith, alchemist, mages, tavern, inn, temple, guild, library,
   barracks… with towers, lofts, crypts and cellars (stairs `<`/`>`), townsfolk who talk, paid services. Buy and sell with gold.
5. Dying is `GameOver`; any key starts over. Carrying the Amulet to the surface ends the quest in victory (see [`alpha.md`](../roadmap/alpha.md)).
