# Depth

Our own list of ideas for giving Ossuary the *texture* Dwarf Fortress has: bodies that get hurt in specific places, things made of
specific stuff, a world with a past and creatures with lives of their own. The main backlog stays in [`todo.md`](../todo.md); this
file is the plan for the depth track. Mark items the same way (`[x]` with a date, `[~]` partial) and record new ideas in the right section.

Ground rules for every item here:

- **Simulation in the Core only.** New mechanics get a `Game.<Subject>.cs`; screens come out as `TextBuilder`; nothing in TypeScript or Rust.
- **Seeded and replayable.** Generated history, descriptions and biographies are pure functions of the seed (or `hash(x, y)` for
  cosmetic variation), never extra simulation RNG draws that would break replays.
- **Every rule change bumps the save format** (`SaveData.Version`) and gets a headless test; every new string gets its PT line in `Loc.cs`.
- **Depth must read in the log.** A system the player cannot see in a message, a look description or the morgue is not worth its cost.

Priority: **P1** changes how every fight or item feels; **P2** adds texture on top of existing systems; **P3** is long-term world simulation.

---

## 1. Bodies and wounds (P1)

DF's signature: a hit lands *somewhere*. Today combat is HP plus status effects.

- [x] **Body plans** (2026-10-05). `Combat/Body.cs`: humanoid, quadruped, insect, winged and dragon, 6–9 weighted parts each, picked from
  the monster's glyph so the bestiary needs no new data; bodiless things (moulds, eyes, wraiths, elementals, swarms, illusions) get none.
  Left: a serpent plan (no snake in the bestiary yet).
- [x] **Hit location** (2026-10-05). Every player melee, shot and ability blow, and every monster melee on the hero, picks a part when the
  hit takes 15%+ of max HP; the log adds a line ("Your left leg is broken."). Left: called shots; monster-on-monster and spell wounds.
- [x] **Wounds as layers on top of HP** (2026-10-05, `Game.Wounds.cs`). Four severities (grazed/cut/torn/mangled for edges,
  bruised/battered/broken/crushed for blows). Legs/wings = limping or crawling, arms = to-hit, head = stun, eyes = sight.
  Left: weapon drop from a mangled arm, severed parts, fliers grounded by their wings.
- [~] **Bleeding** (2026-10-05). Edged wounds bleed 1 HP a turn for a few turns; bleeding out is a kill / a "blood loss" death.
  Left: blood on the floor (section 6), bandages and cauterising.
- [~] **Treatment** (2026-10-05). Healing magic and potions drop wounds a step, full healing and the inn close them, the temple stops
  bleeding, and wounds mend alone over 80–2000 turns. Left: a bandage tool, overworld travel time counting toward mending.
- [~] **Scars** (2026-10-05). A healed torn/broken wound leaves a scar on the sheet and in the morgue. Left: the tiny effect (−1 Cha,
  +1 intimidation) and NPCs who notice them.
- [~] **Monsters too** (2026-10-05). Monsters limp, crawl, lose aim and sight, reel from head blows and bleed out; look lists their wounds.
  Left: a crippled monster choosing to flee, a blinded one swinging wild, targeting a part.
- [x] Tests (2026-10-05): `bodies: hits land on parts, wounds hinder, bleed, heal and scar`.

## 2. Materials (P1)

A copper sword is not a steel sword.

- [x] **Material table** (2026-10-05, `Items/Materials.cs`): bone, wood, copper, bronze, iron, steel, silver, cold iron, mithril, adamantine,
  obsidian, with weight and value percent, to-hit/damage/AC deltas, durability and a colour in `Theme.MaterialColor`.
- [x] **Items carry a material** (2026-10-05). Loot and monster gear get one by depth and branch (a seed hash, no RNG drawn: the Mines lean
  to good metal, the Warrens to bone, wood and copper, the Vaults to silver, the Spire to obsidian); the default (iron, wood for staves and
  bows) is not named, the rest leads the name ("steel long sword", PT "espada longa de aço").
- [x] **Material versus creature** (2026-10-05). Silver vs undead and were-creatures, cold iron vs fey, obsidian vs constructs (+1d6+2);
  bone and obsidian are brittle.
- [x] **Wear and breakage** (2026-10-05, `Game.Materials.cs`). Blows dealt or taken wear gear (blunted/dented, then chipped/battered,
  -1 each); repair at the smithy or armoury; brittle or cheap worn weapons can shatter on a critical.
- [x] **Forge uses ore** (2026-10-05). Ore lies in the Mines, breaks loose when digging and is mined on hills; the miner smelts it into bars
  at a forge, the blacksmith and armourer make new gear from bars in their metal, and the smith pours your weapon or body armour anew (2/3 ore).
- [x] **Material decides wound type** (2026-10-05, with 1). Edged weapons cut, blunt ones bruise and break, and piercing ones (daggers,
  spears, tridents, piercing monster attacks, missiles) leave deep wounds on a hard blow (`AttackResult.Deep`, a step worse as on a critical).
- [x] **Crafting for everyone** (2026-10-05, save format 15; see `docs/design/systems.md`). The player can live a whole life without entering the dungeon: any class or
  race learns any trade by practice or from a town master, with ranks (apprentice → journeyman → master) that unlock materials
  (iron → steel → mithril/adamantine), quality and fewer failures. Trades: blacksmith (weapons), armourer (armour, shields),
  bowyer/fletcher (bows, arrows), leatherworker, tailor/weaver (cloth, cloaks, robes), jeweller (rings, amulets, settings), alchemist
  (potions, oils, poisons), scribe (scrolls, books), carpenter/woodworker, toolmaker (picks, lock picks, lanterns, traps), luthier and
  instrument maker (lute, tambourine, flute, drum: a bard can make their own), cook and brewer, miner and smelter (ore to bars).
  Gathering (mining, logging, skinning, herbs), workshops in town (anvil, loom, still, bench), selling what you make, and commissions
  from townsfolk. Why: freedom to play as a craftsman, and a use for every material. Ties: masterwork (4), the smith, the economy (10).
  Done: 16 trades with five ranks, ~100 recipes as data, goods, meals, instruments and the fishing rod, forge and workshop stations,
  quality and failure by rank, the recipe book (`Shift+J`), gathering (`Shift+G`: butchering, foraging by terrain, mining veins),
  music for coin or sleep, masters who teach to journeyman, weekly commissions, raw goods in the shops and stack prices.
  Left: arrows as real ammunition, a loom and a still as their own tiles, crafted relics named after the maker (with 4), crafters in the
  rival parties, and prices that follow what the hero sells (with 10).

## 3. Generated world history (P1, large)

DF simulates centuries before you start. A small version, run once at world creation.

- [ ] **History pass** (`World/History.cs`): 100–300 simulated years from the seed: the houses rise and fall, wars, plagues, the
  branch kings take their thrones, heroes live and die, towns are founded and razed. Output: an event list with dates, actors and places.
- [ ] **Historical figures.** Named people with birth, deeds and death; some are still alive (old NPCs), some are buried in dungeons.
- [ ] **Feed existing systems**: rumours, the Scholar's lore, books on shelves, relic lore (see 4), ruin and landmark names, tavern songs.
- [ ] **Legends panel** (F-key or in the Journal): browse events, figures and places the hero has learned about.
- [ ] Constraint: pure function of the seed; it must not change the map or anything the replay depends on unless the save version bumps.

## 4. Artifacts with biographies (P2)

- [ ] **Biography per relic**: who forged it, of what material, for whom, who carried it, how it was lost (drawn from history, 3).
- [ ] **Deeds get written in.** A boss or named monster killed with a relic adds a line; the morgue prints the relic's full story.
- [ ] **Named masterwork.** Rarely, a crafted item gets a name and the hero's name as its maker.

## 5. Generated descriptions (P2)

- [ ] **Look descriptions with detail** for monsters and people: build, scars, missing parts, gear and its material, mood, age.
  "A lean kobold with a notched ear, gripping a chipped bronze spear." Cosmetic variety from `hash`, real facts from the simulation.
- [ ] **Item descriptions**: material, craftsmanship, wear, engravings, previous owners (with 4).
- [ ] **Room and level flavour**: what the place was (barracks, shrine, larder) inferred from its contents.

## 6. Fluids and traces on the ground (P2)

- [ ] **Blood by species colour**, ichor, slime, vomit, mud, soot, footprints. Extend `Surfaces.cs`; the background of the cell carries it.
- [ ] **Traces tell a story**: drag marks to a body, a trail of blood from a wounded monster that fled.
- [ ] **Tracking**: monsters with a nose follow a bleeding hero; the hero (Survival skill) can follow a wounded quarry.
- [ ] Decay over time so levels do not fill up.

## 7. Thoughts and mood (P2)

- [ ] **Thought list** for the hero (`Game.Mood.cs`): "saw a companion die", "slept in a fine bed", "ate raw meat", "drank in good company",
  "was in darkness too long". Each has a weight and a duration.
- [ ] **Mood effects**: small changes to XP gain, prices, NPC lines and stress; a breakdown at the bottom (a forced rest, a refusal to go deeper).
- [ ] Ties with corruption and companions (companions get their own short thought list).

## 8. Engravings and murals (P2)

- [ ] **Engraved walls** in old dungeon rooms showing events from history (3); read with look (`l`).
- [ ] Some engravings are clues for the main quest; some are warnings about the level's boss.
- [ ] The hero can engrave a wall (skill check); bones levels may show what the dead hero carved.

## 9. Creatures with lives of their own (P2)

- [ ] **Named survivors.** A monster that survives a fight with the hero gets a name, remembers the hero, levels up and earns titles
  ("Grak, Biter of Heroes"); recorded in the ledger.
- [ ] **They come back**: as rumours, as a road ambush, as a rival on deeper levels; killing one is a deed with its own epilogue line.
- [ ] **Monster ecology**: lairs, sleeping cycles, hunting and eating corpses, fights between factions without the hero (extends `Game.Factions.cs`).

## 10. Sieges, caravans and a moving world (P3)

- [ ] **Caravans** that travel between towns on the overworld (already in `todo.md` *Living world 7*); they can be robbed, escorted or lost.
- [ ] **Raids on towns** when a monster faction grows strong; a town left undefended loses shops and people, remembered by the ledger.
- [ ] **Prices and stock** that follow supply (a raided mine town lacks iron).

## 11. Legends at the end of a run (P3)

- [ ] **Legends view** in the morgue and a panel after death: the hero's deeds, the relics they carried, the named enemies, how the world
  changed, alongside the generated history (3).
- [ ] Past heroes from `history.json` appear in the next world's history as figures ("the Drifter who fell on level 7").

---

## Suggested order

1. Materials (2) and body/wounds (1): biggest change in how fights and items feel, both reuse existing systems and are easy to test headless.
2. Fluids (6) and descriptions (5), which mostly *show* what 1 and 2 simulate.
3. Named survivors (9) and mood (7).
4. History (3), then artifacts (4), engravings (8) and legends (11) that read from it.
5. Sieges and caravans (10) with the living-world track.
