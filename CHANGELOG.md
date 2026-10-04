# Changelog

**English** · [Português (Brasil)](CHANGELOG.pt-BR.md)

Ossuary versions are a **single number** (Version 11, Version 12…), as in *Project Zomboid*. In the manifests (`package.json`, `Cargo.toml`,
`tauri.conf.json`) and in the installer name the version appears as `0.N.0`. The **save format** (`SaveData.Version`) uses the same number: change a rule, bump it, old saves stop loading.

## Version 13 — Sound

### Music
- **Eighteen tracks that follow the moment.** The engine now reports what is going on (`Frame.scene`: a boss in view, a hostile in view, inside a tavern or temple, a shop, night or day, or the dungeon branch) and the music changes with a fade. They share the five-note motif and the same voices: the title theme "Phosphor & Bone", the story intro, the road by day and by night, towns by day and by night, the tavern, the shop, the temple, one track for each dungeon branch (the Dungeons, the Mines, the Warrens, the Sunken Vaults, the Ashen Spire, the Annex), combat, the Gaoler and the Stone Warden.
- A dead hero or a won run hears nothing.

### Sound effects
- **Fifteen stingers** rendered by code from small instrument models (bells, piano, harp, cello, organ, timpani, wind and scrape noise): level up, quest accepted, updated and complete, rare item, danger, trap, rest, gate, death, victory, the three endings and a new cycle. Level up, quest, death, stairs, resting at an inn and surfacing with the Amulet play one.
- New cues for stairs, unlocked doors, picking things up and resting. Menu blips for moving, confirming and cancelling.
- Master, music and effects volumes now work.
- `npm run stingers` (in `desktop/`) writes the stingers as WAV files.

### Saves
- The save format is unchanged (still 12): Version 12 saves keep loading.

## Version 12 — A world that remembers

### People
- **Townsfolk have personalities and memory.** Every resident has a persona and remembers what you did to them; a ledger of deeds follows the hero across towns. They stand still while you talk, in a proper conversation box.
- **Quests with deadlines**, a journal on `F7`, and personal, Watch, Temple and Cult tracks.
- **Rumours** that point at real bosses and places.
- **Town events** and **road travellers**: a pilgrim, a peddler with a map fragment, refugees and a wounded delver, with choices the world remembers. The hired sword comments as you go, and a **rival party** races you down the Dungeons.

### Crime and the Watch
- Witnesses, a bounty per region, arrest, cells and murder. Essential NPCs are knocked out instead of killed.

### The main questline
- **"The Seal"**: documents, the Reader, truths, six endings and a new cycle.

### Fixes
- **Entering a dungeon from the overworld at level 1 left no way back up.** The stairs up were removed from level 1 of every branch; now only side branches (the Annex, left by its portal) lose them.

### Saves
- **Save format 12.** Version 11 saves no longer load.
## Version 11 — Magic you can see

### Spells and animations
- **322 spells in 8 schools** (up from 39 in 6). New schools: **Nature** (ranger) and **Shadow** (rogue). New spells are data recipes: damage, status effects (hold, poison, bleeding, knockback…), buffs, summons, surfaces, chains, scattered strikes, cones, executions.
- **Spell animations**: the engine records what happens and the front end plays it over the frame. Projectiles with trails, jagged lightning, chains, cones, bursts, waves, rain, pillars, meteors. Wands, arrows, molotovs, traps and boss attacks are animated too. `headless.ps1 fx <spell>` shows them in ASCII.
- **55 spellbooks** in five depth tiers; a spell list with **school tabs**; every spell, buff and effect translated to Portuguese.
- Rangers and Rogues now start with a book and spells.

### Items
- **Magic items imbued with a spell that fits their kind** (sword: attack; armour: ward; boots: movement…). The spell is lent to you while the item is in use; weapons fire it on their own when they hit and armour answers a blow with it.
- **43 unique items** across every branch (38 lend spells) and 4 new sets.
- Wands, scrolls and potions now **cast real spells** (38 / 34 / 18), with the same animations and no mana.
- Dozens of new weapons, armours, helms, gloves, boots, cloaks, shields, rings and amulets; 55 new affixes; loot respects depth.
- Rings and amulets now **count** (and amulets can be worn: `P`/`R`).

### Fixes
- The old amulets were *rings* in the catalogue; the amulet of life saving never worked. They are real amulets now.
- The game crashed if a monster died of poison during its own turn.

### For developers
- Save format moves to **version 11** (old saves do not load).
- New `ArsenalTests` and `fx.test.ts`; documentation in [`docs/spells-and-items.md`](docs/spells-and-items.md); [CONTRIBUTING.md](CONTRIBUTING.md) and a new README.

## Before version 11

Earlier history is in `git log` and in the finished-items log of [`docs/todo.md`](docs/todo.md): vertical towns, the living world (reputation, jobs, road events), bosses, factions, corruption and mutations, companions, achievements, the daily challenge, game modes, sound effects and animated water, among others.
