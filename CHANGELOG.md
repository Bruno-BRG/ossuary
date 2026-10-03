# Changelog

**English** · [Português (Brasil)](CHANGELOG.pt-BR.md)

Ossuary versions are a **single number** (Version 11, Version 12…), as in *Project Zomboid*. In the manifests (`package.json`, `Cargo.toml`,
`tauri.conf.json`) and in the installer name the version appears as `0.N.0`. The **save format** (`SaveData.Version`) uses the same number: change a rule, bump it, old saves stop loading.

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
- New `ArsenalTests` and `fx.test.ts`; documentation in [`docs/magia-e-itens.md`](docs/magia-e-itens.md) (Portuguese); [CONTRIBUTING.md](CONTRIBUTING.md) and a new README.

## Before version 11

Earlier history is in `git log` and in the finished-items log of [`docs/a-fazer.md`](docs/a-fazer.md): vertical towns, the living world (reputation, jobs, road events), bosses, factions, corruption and mutations, companions, achievements, the daily challenge, game modes, sound effects and animated water, among others.
