# Status — Ossuary, Version 11

A Tauri 2 + Rust desktop app, a TypeScript/Canvas bitmap terminal and a self-contained C#/.NET 10 engine. The definitive sources are in
`engine/`, `desktop/` and `assets/`; the pipeline and the tests use that structure.

## Implemented

- Dungeon generation: 5 branches plus the optional Annex, about 57 levels and 8 styles.
- Combat, FOV, pathfinding, monsters, hunger, status effects, items and equipment.
- The overworld, regions, roads, clock, travel, encounters and vertical towns (floors, cellars, services, people).
- Shops, economy, buy/sell selection, roles and progression.
- The quest, the final Amulet, permadeath and victory.
- HUD, inventory, character, history, discoveries, help, targeting and options.
- A title with a block logo, an optional seed, 4 themes and an adjustable CRT.
- The unscii-16 8×16 font, integer scaling in physical pixels and persisted preferences.
- Windows x64 packaging with the engine inside the NSIS installer.
- **Magic** (Version 11): 322 spells in 8 schools, 55 spellbooks, animated spells, magic items imbued with fitting spells, 43 uniques and 4 sets, wands/scrolls/potions that cast
  real spells, and a spell list with school tabs. See [`spells-and-items.md`](../design/spells-and-items.md).

## Validation

The headless suite (simulation, victory, features, the spells/items/animations suite and the desktop protocol) passes, and the
front-end tests (terminal, audio, water, animations) pass. The packaged-IPC test only runs on Windows. The protocol is exercised with the
self-contained executable in an empty folder. Soak: 50 seeds × 500 turns. Screenshots are in `docs/shots/`.

Run: `desktop.ps1 dev`. Ship: `desktop.ps1 build`. See [build-and-test.md](../tech/build-and-test.md).

## State after the follow-up rounds

The whole backlog of [`todo.md`](../todo.md) was implemented (held-key walking, auto-explore, morgue/history/graveyard, modes and challenges, journal, achievements, traps, stealth,
corruption and mutations, companions, crafting, artifacts and sets, spells, gods, branch monsters and bosses, factions, reputation/jobs/events, vaults and new level styles, the Annex,
Trained mode, sound, animated water and square tiles), followed by the Version 11 magic round. Save format: **version 11**.
Ideas that remain for the future are listed in [`todo.md`](../todo.md).

## Gameplay evolution

The full RPG system (races, classes, magic, perks, items, gods, surfaces; see rpg.md and balance.md) is in place. `F5` shows the starting seed.
Further content is tracked in [`todo.md`](../todo.md); none of it is a runtime requirement.
