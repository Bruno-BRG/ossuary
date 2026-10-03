# RPG — research, catalog and roadmap

Design notes for expanding Ossuary's RPG side: character, magic,
abilities, items and progression. This document is the **map**; each implemented
part becomes a section in [systems.md](systems.md) and its status lives in the
roadmap table (§7).

## 1. Current state (baseline)

- 6 NetHack attributes (3..18, Str with an 18/xx fraction), HP, Energy (turns).
- 5 roles (`Roles.cs`): Adventurer, Fighter, Rogue, Cleric, Wizard — attribute
  bias, kit, HP/level, starting skills, 4 titles.
- 6 skills 0..100 that rise with use (Combat, Dodging, Stealth, Magic,
  Survival, Search). Magic currently **does nothing** beyond a number.
- Level-up: automatic HP/XP + 1 pick (`Progression`: 7 options, all
  flat increments).
- Items: weapons/armor/rings/amulets/wands/scrolls/potions/books.
  `a spellbook` and `a book of prayers` are only decoration. No mana, no spells.
- No **name**, no **race**. Saving is replay (seed + keys): any
  creation choice must enter `SaveData`.

## 2. Research — what each game teaches

### Roguelikes

| Game | Usable idea | Fits Ossuary? |
|---|---|---|
| **DCSS** | Species grant *aptitudes* (XP cost per skill) and HP/MP per level; background only sets the start (nothing is forbidden). Schools of magic: a spell has up to 3 schools and fails by skill + armor. Gods with piety. | **Yes**, core. Per-race aptitude, spell failure, gods |
| **NetHack** | Roles with ranks, alignment, prayer, skills by *use* capped per class, identification by use, books with level and failure chance, Pw regenerates by Wis/XL. | Already the base; adopt per-class skill cap and book failure |
| **Brogue** | No class: power comes from items (enchant scrolls into a single item). No XP. | Inspire the *enchant* system and items with identity |
| **Caves of Qud** | Mutations (70+) instead of class, controlled randomness, skill trees per domain, physical vs mental mutations. | Part 5: rare "mutations/blessings" as perks |
| **Tales of Maj'Eyal** | Talents with cooldown, distinct per-class resources (mana, stamina, vim, hate), mixed trees. | Per-class resources: Mana, Vigor, Blood/Piety |
| **Cogmind/Sil/Angband** | Sil: skills bought with XP (no levels), serious stealth. Angband: magic by level books, failure by Int/armor, elemental resistances. | Resistances, XP spent on skills (option) |

### Tabletop

| System | Usable idea |
|---|---|
| **D&D 5e** | Race + class + background; proficiency bonus by level; saving throws; spell slots; concentration; short/long rest; advantage/disadvantage. |
| **Pathfinder 2e** | ABC (ancestry, background, class); heritage within a race; class feats every even level; proficiency in 4 ranks (Trained→Legendary); *focus spells* that recharge quickly. |
| **GURPS/Savage Worlds** | Advantages/disadvantages (scored perks and flaws); skills by cost. |
| **Call of Cthulhu/Dark Souls tabletop** | Sanity/corruption as a resource — fits the lore (necromancy). |

### Magic video games

| Game | Idea |
|---|---|
| **Baldur's Gate 3** | Slots per level, *upcast*, concentration, bonus action, interacting elemental surfaces (fire+oil, water+lightning), advantage from terrain/height. |
| **Skyrim** | Skills by use and *perks* every level; 5 schools; mana + cost; enchanting tied to soul gems; scrolls; shouts with cooldown. |
| **Diablo/PoE** | Item affixes (prefix/suffix), rarity, support gems, sockets. |
| **Dark Souls** | Attributes that unlock spells/weapons, catalysts, pyromancy vs miracles. |
| **Divinity: OS2** | Surfaces, status by combination, scrolls as part of the system. |

## 3. Design decisions (what we adopted)

1. **Two identity axes**: Race (body, aptitudes, traits) × Class
   (role, kit, ability tree). Background is left out for now.
2. **Power by use + choice**: skills rise by use (already exists, NetHack/Skyrim
   style); each level, **1 perk** from a short list (replaces
   the flat increments).
3. **Single mana** (`Mp`/`MpMax`) for arcane magic; Cleric and Paladin
   spend the same pool (labeled "Faith") — no Vancian, no recharge by
   resting: per-turn regen tied to Int/Wis/Magic, plus potions.
4. **Spells come from books** (learning costs turns, may fail by
   Int/level, NetHack style), organized by **school**.
5. **Spell failure** by heavy armor and skill (DCSS/NetHack style);
   a plate-armored mage is a choice, not a bug.
6. **Resistances and damage types** (physical, fire, ice, lightning, poison,
   necrotic, holy) — the basis for races, items and enemies to talk to each other.
7. **Determinism**: all new RNG comes from `Game.Rng`; drawing uses hash(x,y).
   Everything new needs a headless test and must enter the replay.
8. **Pure Core**: mechanics in `Game.<Subject>.cs`, screens as `TextBuilder`.

## 4. Catalog — character

### Races (trait + aptitudes; base HP/MP per race)

All use the **same 3d6 roll** from the seed; only the bias changes (current default).

| Race | Attributes | Trait | Tie to the lore |
|---|---|---|---|
| **Human** | no bias | Versatile (part 2: +1 extra perk at level 1) | The default drifter |
| **Dwarf** | Con+2, Str+1, Cha-1, Dex-1 | Resists poison; sees gold/veins; short infravision | Mines of Dwarfdeep |
| **Elf** | Dex+2, Int+1, Con-2 | Mana +25%; resists sleep; bows +1 | Age of the Cities |
| **Halfling** | Dex+2, Cha+1, Str-2 | Stealth +10; luck (crits against you reduced); eats less | Hobbit innkeepers |
| **Orc** | Str+2, Con+1, Int-1, Cha-2 | Regenerates HP (slowly); fury <25% HP | Barracks of the Dungeons |
| **Gnome** | Int+2, Con+1, Str-2 | Mana +15%; gadgets: wands spend fewer charges | Gnome mummies of the Vaults |
| **Ashen** (human touched by the Spire) | Wis+1, Con+1, Cha-2 | Resists fire; partly healed by necrotic; heat | Ashen Spire, Emberdown |
| **Ossuarian** (undead, unlockable) | Con+2, Cha-3 | Immune to poison/hunger (!); heals only by magic/damage dealt | Premise: the pit gives back |

Implementation order: Human, Dwarf, Elf, Halfling, Orc, Gnome, Ashen.
Ossuarian later (unlocked by victory).

### Classes (role + resource + signature)

| Class | Role | Resource | Signature |
|---|---|---|---|
| **Fighter** *(exists)* | Front line | Vigor | Power strike, second wind |
| **Rogue** *(exists)* | Sneak damage | — | Backstab x3, disarm traps |
| **Cleric** *(exists)* | Support/anti-undead | Faith (Mp) | Heal, turn undead, blessing |
| **Wizard** *(exists)* | Ranged damage | Mana | Spells from books, arcane schools |
| **Ranger** | Bow, survival | Vigor | Aimed shot, traps, tracking |
| **Paladin** | Holy tank | Faith | Aura, holy smite, healing by laying on hands |
| **Necromancer** | Summoner | Mana + Corruption | Raise the dead, drain, bones |
| **Barbarian** | Brute damage | Fury | Rage, fear immunity, high HP |
| **Monk** | Unarmed | Vigor | Martial arts, dodge, no armor |
| **Warlock/Pactbound** | Damage at a cost | HP/Corruption | Pact: power for a price |

Order: Ranger, Paladin, Necromancer first (they fit the lore and use the
magic system), then Barbarian, Monk, Pactbound.

### Derived attributes (to implement)

- **Max Mp** = race/class base + Int/Wis×k + level×k. **Regen** per turn.
- **Hit/Damage/Crit** by Combat + Str/Dex (partly today).
- **Saves** (Fort/Refl/Will) from Con/Dex/Wis: poison, traps, control.
- **Load** (`CarryingCapacity` already exists) affects Evasion.

### Progression

- XP: current curve `×1.35+10`. Keep. First-encounter XP bonus per
  species (discovery, Qud/Skyrim style) — optional.
- **Skills**: keep 0..100 by use; add **ranks** (Novice,
  Trained, Expert, Master, Legendary) at 0/25/50/75/100 with a bonus at each
  rank; **per-class cap** (DCSS/NetHack).
- **Perks**: 1 per level (+1 extra at levels 5/10), from a pool filtered
  by class/race/skill prerequisite. Examples:
  - Fighter: *Second Wind, Power Strike, Shield Wall, Steel Fury*
  - Rogue: *Backstab, Light Steps, Quick Hands, Thief's Eye*
  - Wizard: *Vast Mind (+Mp), Focus (-failure), Quick Casting*
  - Cleric: *Unshaken Faith, Greater Heal, Aura of Protection*
  - General: *Hardy, Lucky, Open Eyes, Grounded*

## 5. Catalog — magic

### Schools (6, with a skill for each)

`Evocation` (damage), `Conjuration` (summon/portals), `Alteration` (buff/utility),
`Illusion` (stealth/confusion), `Necromancy` (drain/dead), `Holy`
(heal/protection). *Per-school skill is phase 4; at first only `Magic`.*

### Initial list (30 spells, circles 1–5)

| Circle | Spells |
|---|---|
| 1 | Arcane Bolt (damage, aim) · Light · Detect Magic · Cure Wounds · Protect (AC) · Chill Touch |
| 2 | Lesser Fireball · Frost Ray · Invisibility · Open/Lock · Sleep · Turn Undead |
| 3 | Lightning · Short Teleport · Mass Heal · Raise Skeleton · Reveal Map · Slow |
| 4 | Wall of Fire (terrain) · Stoneskin · Drain Life · Fear · Dispel |
| 5 | Meteor · Portal · Death · Resurrection (1x) · Rain of Bones |

Associated mechanics: Mp cost, failure %, range/area reusing
`Game.Targeting`, buff **duration/concentration** (one active),
**surfaces** (fire burns grass/oil, ice freezes water, lightning in a puddle)
as a late phase, BG3/Divinity style.

### Sources of magic

- Books (permanent learning, level/failure), scrolls (single use, no
  mana, already exist), wands (charges), staves (recharging charges),
  altars/gods (prayers), potions.

## 6. Catalog — items and enchantments

- **Rarity**: Common, Uncommon (+1..+2), Rare (affix), Epic/Artifact (fixed
  name + lore), Cursed (`Cursed` already exists).
- **Affixes** (Diablo/PoE): prefixes (`Sharp +1`, `Flaming` +1d4 fire,
  `Venomous`, `Vampiric`, `Frigid`) and suffixes (`of the Fox` +Dex, `of the
  Bear` +Str, `of the Owl` +Int/Mp, `of the Mage` +Mp regen, `of Ruin` +crit).
- **Enchanting** (Brogue/Skyrim): *Enchant* scroll +1 on an item; **soul
  gem**/bone-soul recharges a staff or enchants a weapon (Ossuary theme).
- **Identification** by use (already partial) + scroll of identify.
- **Sets/artifacts**: *Crown of the Drowned King*, *Axe of Dwarfdeep*,
  *Ashes of the Spire*, tied to the branches.
- New **slots**: helm, boots, gloves, cloak, amulet, 2 rings — today there are
  Armor/Shield/2 rings/amulet; splitting slots is part 6.
- **Resistances** on items and enemies (listed in §3.6).

## 7. Implementation roadmap

Each part closes with: new headless tests, `fastcheck`, `docs/systems.md`
updated and the full suite. One part at a time.

| # | Part | Deliverable | Status |
|---|---|---|---|
| 1 | **Identity** | Name, race (7), class (+Ranger, Paladin, Necromancer), creation screen, `SaveData` with character, title/Character panel | **done** (active racial traits wait for part 2) |
| 2 | **Derived attributes** | Mp/regen, active racial traits, skill ranks and cap, base resistances | **done** (saves wait until there is something to save against) |
| 3 | **Magic core** | Mp in the HUD, `Z` cast verb, books, learning, failure, 6 starting spells, reused aiming | **done** |
| 4 | **Spell catalog** | 32 spells in 6 schools, timed buffs, summons/allies | **done** |
| 5 | **Perks and class abilities** | Replaces `Progression`; active abilities paid in Vigor | **done** (Fury/cooldowns wait for the Barbarian/Monk/Pactbound classes) |
| 6 | **Items** | Rarity, affixes, enchant, new slots, artifacts, soul gems | **done** (soul gems wait for after the staff system) |
| 7 | **Gods and piety** | Altars, prayers, favor (cleric/paladin) | **done** (temples with a priest wait for later) |
| 8 | **Surfaces and status** | Fire/ice/lightning on terrain, active resistances | **done** (bleeding/poison as conditions wait for the combat review) |
| 9 | **Balance** | Soak per class×race, damage/Mp curves, final docs | **done** ([balance.md](balance.md)) |

The catalog has since grown well past this roadmap: 322 spells in 8 schools, 55 books and
a large pool of magic and unique items. See [spells-and-items.md](spells-and-items.md).

## 8. Sources

- [DCSS — species, backgrounds, schools](https://crawl.develz.org/wordpress/0-31-the-alchemy-of-forms) · [manual](https://mitjafelicijan.com/assets/notes/dcss_manual.pdf)
- [Caves of Qud (RogueBasin)](https://www.roguebasin.com/index.php/Caves_of_Qud) · [Tales of Maj'Eyal](https://en.wikipedia.org/wiki/Tales_of_Maj%27Eyal)
- [D&D 5e vs Pathfinder 2e](https://www.dnddiceroller.com/blog/pathfinder-2e-vs-dnd-5e/)
- NetHack, Brogue, Angband, Sil, Baldur's Gate 3, Skyrim, Divinity: OS2, Diablo: domain knowledge.
