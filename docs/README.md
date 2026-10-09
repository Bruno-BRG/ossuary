# Ossuary — docs

The game's documentation, in English. The operational source of truth (commands, rules, workflow) is
[`AGENTS.md`](../AGENTS.md) at the repo root. Here lives the *knowledge*: what the game is, how each system works and where it
lives in the code. Real captures are in [`shots/`](shots/).

## Tracking

- [`todo.md`](todo.md) — **backlog and progress tracking** (what is left, what is done), including the depth track: Dwarf Fortress-inspired
  ideas (wounds, materials, crafting, history, named enemies), tutorials and the economy
- [`roadmap/alpha.md`](roadmap/alpha.md) — state of the playable alpha (historical snapshot)
- [`roadmap/magic.md`](roadmap/magic.md) — design and plan of Version 22, *Spells that answer back* (casting monsters, ice and arcing lightning, hits on arrival, element sounds)
- [`../CHANGELOG.md`](../CHANGELOG.md) — what changed in each version; [`../CONTRIBUTING.md`](../CONTRIBUTING.md) — how to contribute

## The game (`game/`)

- [`overview.md`](game/overview.md) — what Ossuary is, pillars, game loop
- [`controls.md`](game/controls.md) — full keyboard (vi-keys + verbs)
- [`lore.md`](game/lore.md) — worldbuilding bible (tone, the Ossuary, factions, Yendor)
- [`main-quest.md`](game/main-quest.md) — the main questline "The Seal": short and long path, truths, endings, essential NPCs
- [`living-world.md`](game/living-world.md) — personas, dialogue, quest tracks, rumours, travellers and town events
- [`guild-jobs.md`](game/guild-jobs.md) — spec: notice-board jobs and workshop commissions on the quest engine, and the two Cult errands

## Systems design (`design/`)

- [`systems.md`](design/systems.md) — dungeon, overworld, towns, combat, items, FOV… and where each lives in the code
- [`rpg.md`](design/rpg.md) — research, catalogue and roadmap of the RPG system (races, classes, magic, items)
- [`spells-and-items.md`](design/spells-and-items.md) — 322 spells, 55 books, magic and unique items, and the spell animations
- [`balance.md`](design/balance.md) — balance bot, baseline and tuning knobs

## Engineering (`tech/`)

- [`architecture.md`](tech/architecture.md) — layers, classes, dependency rules
- [`desktop.md`](tech/desktop.md) — Tauri runtime, protocol and distribution
- [`renderer.md`](tech/renderer.md) — bitmap font, Canvas, CRT, themes, FOV, how to verify
- [`visual.md`](tech/visual.md) — "Phosphor & Bone" art direction: palette, font, glyphs and layout
- [`languages.md`](tech/languages.md) — English/Portuguese, how to translate and the opening story
- [`build-and-test.md`](tech/build-and-test.md) — pipeline: scripts, CLI, builds, shots

## Audio (`audio/`)

- [`music-prompts-phosphor.md`](audio/music-prompts-phosphor.md) — soundtrack prompts, machine + bone (the approved identity)
- [`music-prompts-natural.md`](audio/music-prompts-natural.md) — soundtrack prompts, acoustic chamber version
