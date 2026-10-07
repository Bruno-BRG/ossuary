# Systems — Ossuary

Where each one lives (everything under `engine/Ossuary.Core/`):

## Dungeon (`Dungeon.cs`, `Gen/`, `GameMap.cs`, `Tile.cs`)

- 5 branches, ~54 levels in total (the `AllBranchDepths` test guarantees it).
- Styles: Rooms, Cave, Maze, Barracks, Warrens, Fort, **Ruins** (collapsed rooms), **Catacombs** (grid crypts); vaults and caches in `Gen/Vaults.cs` (`DungeonGen` +
  `LevelBuilder.Populate` for monsters/loot). Every level: 1 up stair + 1
  down, everything reachable (tests per style, 12 seeds each).
- Tiles: wall, floor, doors (closed/open/locked/secret), stairs,
  altar, fountain, rubble, traps (`TrapTable`: spike, hole, dart, teleport,
  alarm, web, fire). Interacting with a wall resolves door/rubble/altar/fountain.
- Glyph vocabulary (classic NetHack, 100% ASCII; canonical list in
  `Core/GlyphSet.cs`, covered by the `GlyphCoverage` test). Target vocabulary
  with CP437 (`·` `▲` `♣` `≈`…) and fg+bg per tile: `visual.md` §3.2, phase F2:
  `#` wall (tone distinguishes stone/brick/rock), `.` floor, `,` alt floor,
  `I` pillar, `+` closed/locked door (color distinguishes), `'` open door,
  `>`/`<` stairs down/up, `^` portal, `"` rubble, `_` altar, `{` fountain.
  A secret door draws `#` (wall) until found.
- Overworld: `~` water, `.` sand/snow/ash/road (color distinguishes), `"`
  grass, `t` forest, `h` hill, `M` mountain, `,` swamp, `#` terrain
  ruin; features in capitals: `+` town, `^` dungeon, `R` ruin,
  `C` cave, `m` mine, `K` fortress, `S` shrine, `=` bridge.
- UI: `@` player, `✚` aim, `✦` travel cursor, `▶` list cursor,
  `▼` encounter marker, `█`/`░` bars, `─│╭╮╰╯` frames.

## Turns and monsters (`Game.cs`)

- Player action → `EndPlayerTurn()`: turn++, hunger, status, monsters by
  energy (`Speed`, 12 to act), FOV, death.
- NetHack-style hunger: a `Nutrient` pool drains per turn; eating refills; at zero =
  warning → starving damage. (`EatFood`, `ProcessHunger`).
- A monster at distance 1 attacks (some explode); sight by `Def.Vision`;
  Ambush/Guard/Territorial AI patrols or chases; rubble can be crushed.

## Combat (`Combat/`)

- `Battles.PlayerMelee` / `Battles.MeleeAttack`: hit, damage, crit, death.
  A kill gives XP (`AddXp`), Combat skill, drops gold/inventory and sometimes a corpse.
- Status: sleep, stun, confusion, blindness (FOV=1), hallucination, poison,
  amulet of strangulation.

## Bodies and wounds (`Combat/Body.cs`, `Game.Wounds.cs`, `Loc.Body.cs`)

- **Plans.** `Bodies.PlanFor(def)` picks a body plan from the monster's glyph (humanoid, quadruped, insect, winged, dragon); moulds,
  floating eyes, wraiths, elementals, swarms and illusions get none and are never wounded. The hero is humanoid. Undead, skeletons,
  golems and treants are wounded but do not bleed (`Bodies.Bleeds`).
- **Where a hit lands.** After any player melee, ranged shot or ability blow, and after a monster's melee on the hero (dungeon, road
  fight, the Watch), `WoundFrom` turns the hit into a wound when it took 15% or more of the defender's max HP (a critical counts 15
  points more): grazed/bruised (15%), cut/battered (30%), torn/broken (50%), mangled/crushed (75%). One RNG draw picks the part by
  weight. Bites, claws and points are *edged* (bleed, "cut", "torn"); blows, kicks and blunt weapons are not ("battered", "broken");
  touches, drains and blasts never wound. Hitting a wounded part again worsens it by at least one step.
- **Effects.** Legs (wings for fliers) at broken or worse: `Bodies.Limp` 1 = limping, 2 = crawling. A limping monster moves at 3/4
  speed, a crawling one at 1/2 (`MoveSpeed`); a limping hero gives the monsters an extra move every 4th turn (every 2nd when crawling).
  Arms cost to-hit (`ArmPenalty`: 1/2/4 by severity; the hero's off arm counts half). The head at broken or worse stuns (2 turns for the
  hero, a lost turn for a monster). Each cut eye costs 2 squares of sight (FOV for the hero, `VisionRadius` and notice for monsters).
- **Bleeding.** Edged wounds of severity 2+ bleed 1 HP a turn for 3/5/8 turns (+2 on the trunk). The hero's cause of death is
  "blood loss"; a monster that bleeds out is the hero's kill. Regenerating monsters close a wound every 20 turns.
- **Mending (the hero).** A wound heals by itself after it stops bleeding: 80/250/900/2000 turns by severity. A wound that was torn or
  broken leaves a scar (`Player.Scars`, cosmetic for now). Healing spells and potions drop every wound one step and stop bleeding; full
  healing and a night at the inn close all of them; the temple's cure stops bleeding.
- **Shown.** One log line per new or worse wound ("Your left leg is broken.", "The jackal's hind left leg is torn."), pills BLEEDING /
  LIMPING / CRAWLING / WOUNDED in the sidebar, *Wounds* and *Scars* on the character sheet and in the morgue, and the look command lists a
  monster's wounds. Portuguese agrees the adjective with the part's gender (`Loc.BodyLine`).
- **Called shots.** `Shift+F` cycles `Player.Aim` through anywhere, head, arms, legs, eyes, wings and tail (pill AIM in the sidebar).
  Melee, shots, road fights and ability blows pay `Bodies.AimPenalty` to hit (−2 limbs, wings and tail, −3 head, −5 eyes); a hit then draws
  its part among the parts of that kind and always leaves at least a graze. A target without that kind of part takes the hit anywhere.
- **Losing parts.** An edged hit that mangles a monster's arm, leg, wing or tail on its own (or on a critical) severs it: it never mends,
  bleeds 10 turns and reads "severed". The hero is mangled but never severed. A mangled arm lets go: a monster is `Disarmed` (drops
  carried weapons, its weapon-type attacks do half damage), the hero drops a weapon held in the right hand. A wing broken on a flier
  grounds it (`Grounded`; a dragon also moves a step slower).
- **Wounded monsters.** A monster with a mind that is limping at half HP or less breaks and runs once (`Fled`, 10 turns of fear). A
  monster whose eyes are all cut is blind (`Bodies.Blind`): half its swings go at a random neighbouring square, hitting whatever stands
  there. Look shows blind, cannot fly and a useless weapon arm.
- **More wounds.** Monsters wound each other (rivals, allies, blind swings), and spells of force, frost, lightning and fire wound like a
  blunt blow. Snakes (`viper`, `giant python`) use the serpent plan: head, body, tail and eyes; a broken tail leaves them crawling.
- **Treatment.** A `bandage` (`a`; every hero starts with two, the temple sells them, a tailor cuts three from a cloth anywhere) stops all
  bleeding and marks every wound `Bound`, mending twice as fast until it is hurt again. Catching fire sears open cuts shut. Hours that
  pass outside the turn loop (road travel, fights and flight, gathering, events, the cells) mend as 30 turns each (`PassHours`).
- **Scars that count.** The first scar on the head or an eye costs 1 Cha; each scar (up to two) adds 1 to intimidation (the road toll
  bluff). The first time a townsperson talks with a scarred hero, the log notes their eyes on it.
- Saves move to format **13** (hit locations draw from the RNG, so old key logs would replay differently).
- Test: `bodies: hits land on parts, wounds hinder, bleed, heal and scar` (FeatureTests).
- Saves move to format **16** for the rest of the track; test `combat and bodies: called shots, severing, …` (FeatureTests).

## Items (`Items/`, `Game.Items.cs`)

- Kinds: Weapon, Armor, Shield, potion, Scroll, Wand, Food, Tool, Ring,
  Amulet, Gold, Rock, Corpse, Book, Gem, Ornament.
- Verbs: `g` pick up everything in the cell, `d` drop, `w`/`W` wield/wear,
  `T` take off armor, `P`/`R` ring on/off, `r` read, `z` zap, `e` eat,
  `a` apply tool (pick-axe→dig, lock pick→open).
- Modal choice (`PushChoice`) when there is more than one candidate.

## Potions and scrolls (`Game.Potions.cs`, `Game.Items.cs`)

- **Drinking** (`Shift+Q`, `Commands.DoQuaff`): healing (6d4), extra healing (6d8), full healing (heals everything, +2 max HP),
  poison, sleeping (3–6 turns helpless), confusion, hallucination, speed (`haste` buff 25), levitation (buff, ignores
  traps for 40 turns), acid, oil (spills oil in 3×3), see invisible, gain ability (+1 attribute), gain level.
- Potions and scrolls are **single use** (`Charges = 1`; a read scroll vanishes). Only wands have charges.
- Engine dice are **0-based** (`Rng.Dice(n)` = `0..n−1`); see [balance.md](balance.md).

## Surfaces and elemental status (`Surfaces.cs`, `Game.Surfaces.cs`)

- **Surface layer** (`GameMap.Surfaces`, sparse; only on `Floor/FloorAlt` ground): `Water` (`≈`), `Ice` (`≡`),
  `Fire` (`▲`/`^`, flickers by hash(x,y,turn)), `Oil` (`,`), `Grass`/brush (`"`). Colors/glyphs in `Theme.SurfaceStyle`
  (the background carries meaning). The ground under a player/monster is described by `l`.
- **Rules** (`Game.PutSurface`): fire does not catch on water (becomes steam and wets), melts ice into water, burns brush
  (≥5 turns) and oil (≥10); ice only freezes water; water puts out fire.
- **Tick** (`TickSurfaces`, every turn, current level only): fire exposed to whoever stands in it, spreads to flammable
  cardinal neighbors (brush 70%, oil 85%), melts neighboring ice and goes out; ice melts into water (40 turns);
  puddles created by magic dry up (60).
- **Conditions** (`Actor.BurnTurns/WetTurns`; BURNING/WET pills): burning = 1d3 fire for 4 turns (resistance
  counts; the immune and the wet do not catch fire; water puts it out); wet (in water) = cold and lightning ×1.5.
  **Ice**: stepping on it slips ~30% (lose a turn; monsters too). Monsters avoid stepping on fire.
- **Elements** (`Game.Magic.Effects.cs`, `Aftermath`): fire damage ignites the target; cold slows (and freezes water under the
  target, trapping it); lightning in water **conducts** through the connected puddle (up to 4 cells) and hits everyone, the caster included.
  Also applies to weapons' elemental dice. `MonsterResist`: the dead are immune to poison and half immune to cold; fire giant/
  tiamat immune to fire; ice troll/lich/wraith immune to cold; weaknesses of −50%.
- **Spells**: *Wall of Fire* (cross of flames, 8 turns), *Create Water* (5×5, 60 turns). Fireball burns brush/oil
  in its radius; Meteor ignites the whole ground. The fire trap now leaves flames and ignites the player.
- **Generation** (`LevelBuilder.PlaceSurfaces`): puddles in the Sunken Vaults and (1) in the Mines, brush in the Warrens, oil in the Spire,
  and a bit of oil/brush in the Dungeons. Saves move to version 7.

## Gods, piety and altars (`Entities/Gods.cs`, `Game.Gods.cs`)

- **Mourne, the Weeping Seam** (6th god; see *Corruption and mutations*), **rivals** (`GodDef.Rival`), altar lines *Sacrifice a corpse*, *Trial*
  (`Player.TrialGoal/TrialDone`, `ScoreDeed` counts the deeds that grant piety) and *Defile* (`DefileAltar`, dead altar stored in `_defiled`) — see `todo.md`.
- **5 original gods** (`Gods.All`): **Aurel**, the Last Lamp (light, mercy), **Khorr**, the Hammer Below
  (war), **Veyra**, Mother of Ashes (fire), **Nhal**, the Drowned King (death), **Sylk**, the Quiet One (shadow).
  Each has likes, dislikes, a prayer **boon** and two permanent bonuses (piety 50 and 100).
- **Altar**: walking into a `_` opens the menu (free, costs no turn; `Panel.Altar`). The altar's god comes from
  `Gods.AtAltar(map, x, y)` (a pure function of place, nothing stored). Lines: *Swear* (no god; after renouncing
  costs 150 gold × renunciations), *Pray*, *Offer gold* (up to 100 gold → +gold/20), *Offer an item* (value/40,
  artifact +25), *Renounce* (asks for confirmation), *Forsake X, swear to Y* (tribute 150 × (renunciations+1)).
  Cleric and Paladin start following Aurel with piety 30.
- **Piety** 0–200 (`Player.Piety`, `God`, `PrayerTimer`, `Renounced`): −1 every 250 turns. Gains/losses
  (`GodsOnKill/OnCast/OnAbility`):
  - Aurel: +2 undead killed, +1 Sacred spell; −4 killing the harmless (level 0), −3 necromancy.
  - Khorr: +1 per kill (+3 if the target is ≥2 levels above), −1 illusion, −2 Vanish.
  - Veyra: +2 kill by fire, +1 Fireball/Meteor, −1 kill by ice.
  - Nhal: +2 necrotic kill, +1 necromancy, +1 kill by an ally, −2 Sacred spell.
  - Sylk: +2 stealth kill (target sleeping/unaware/fleeing), +1 illusion, −2 War Cry.
- **Prayer** (`Pray`): in trouble (HP <⅓) heals everything for 20 piety (if piety ≥10); otherwise spends the
  god's boon (cost 30–60) and starts a 400-turn cooldown; too early = −20 piety and a shove (never kills).
  Boons: Aurel heals+mana+cleanses; Khorr +1 enchantment on the weapon; Veyra `flame` buff (+1d4 fire on hits,
  300 turns); Nhal 2 allied skeletons (200); Sylk invisibility 100 + full Vigor.
- **Permanent bonuses**: Aurel 50 faster HP regen, 100 necrotic resistance 30%; Khorr 50 +1 hit,
  100 +2 damage; Veyra 50/100 fire 30%/60% (100: hits burn); Nhal 50 necrotic 30%, 100 heals 2 HP per kill;
  Sylk 50 evasion +2, 100 monsters notice 1 cell less. The Character panel shows faith; saves v6.

## Magic items (`Items/Affixes.cs`, `Items/Item.cs`)

- **Armor slots**: body (`WornArmor`), shield, **helm, gloves, boots, cloak** (`ItemKind.Helm/Gloves/Boots/Cloak`;
  lists `Catalogue.Helms/Gloves/Boots/Cloaks`). `Player.Wear/TakeOff/WornPieces`; `W` wears into the
  right slot (returns the swapped piece to the pack), `T` asks which to take off when there are several. A shield and a
  two-handed weapon do not coexist. AC = sum of the pieces' `Item.TotalAc`.
- **Rarity** (`Item.Rarity`): Common, Magic (1 affix or +N), Rare (prefix + suffix + enchantment),
  Artifact. `ItemRoller.Roll` (in `RollLoot`): Rare chance 2+depth% (max 15), Magic 10+3×depth% (max 45);
  only affects weapon/armor/shield/helm/gloves/boots/cloak. Colors: blue / yellow / orange (`Theme.ItemRaw`).
- **Affixes** (`Affixes.All`, 20): weapon prefixes (keen, brutal, flaming, frozen, venomous, vampiric), armor
  (sturdy, fire/frost/storm-warded, venom-proof) and suffixes (of the fox/bear/owl/sage/mage, of life, of vigor,
  of shadows, of ruin). Everything becomes an `ItemMods` summed in `Player.Gear` and applied by
  `Player.RefreshGear()` (attributes enter as a difference, so training and equipment do not overwrite each other;
  it also feeds HP, Mp, Vigor, evasion and resistances). A weapon's elemental die and lifesteal run in
  `Game.MeleeProcs` (melee and abilities).
- **Identification**: a magic item found shows as *magical long sword*; wielding/wearing reveals name, bonuses and
  (artifact) the lore; the identify scroll reveals everything in use.
- **Enchant**: *scroll of enchant weapon* (+1 on the weapon) and *scroll of enchant armour* (+1 on the least
  enchanted piece), cap +5. Shop value: `Item.TradeValue`.
- **Artifacts** (`Artifacts.All`, one per branch, fixed level): Veil of the First Cell (Dungeons 8), Dwarfdeep Cleaver
  (Mines 6), Rat King's Tooth (Warrens 7), Crown of the Drowned King (Vaults 10), Ashfall (Spire 13).
  `LevelBuilder.PlaceLoot` places them; they stay unidentified until used.
- Saves move to version 5.

## Materials (`Items/Materials.cs`, `Game.Materials.cs`)

- **Table** (`Materials.All`): copper, bronze, iron, steel, silver, cold iron, mithril, adamantine, obsidian, bone, wood. Each has a
  weight and value percent, to-hit/damage/AC deltas against iron, a durability (blows per wear step), and flags: brittle, metal, bane.
  Colours live in `Theme.MaterialColor`.
- **Which items take one** (`Materials.StuffOf`): blades, axes, spears (edge: metals, bone, obsidian); clubs (metals, bone, wood);
  staves and bows (wood, bone); mail, metal helms and gauntlets (metals, bone); shields (metals, wood, bone). Leather, cloaks, slings,
  whips, crossbows, elven and bone-named items have none. `Item.Material` stores the id; **null is the default** (iron, or wood for
  staves and bows), which is never named, so the starting kit and old names read as before.
- **Generation**: `RollLoot` and monster carries call `Materials.Assign` by depth (copper and bronze shallow; steel from 3, silver from
  4, cold iron from 5, obsidian from 6, mithril from 10, adamantine from 14). The pick is a hash of the world seed, the uid and the name,
  so it **draws no simulation RNG**. Artifacts keep their base.
- **Numbers**: the deltas go into `Item.Mods` (so combat, AC and the sheet use them with no extra code); `Item.Weight` feeds the
  burden; `Item.BaseCost` feeds `TradeValue`. Mithril mail is half the weight and +2 AC; adamantine +2 damage and +3 AC.
- **Banes** (`Materials.IsBane`, in `Battles.PlayerMelee`): silver against undead and were-creatures, cold iron against the fey (nymphs,
  sprites, treants...), obsidian against constructs (golems, gargoyles, sentinels): +1d6+2 and "The silver bites deep!".
- **Wear** (`Item.Wear`, `Item.Condition`): a weapon wears one per hit dealt, a worn piece one per hit taken (which piece follows the
  damage, no RNG). At durability it is *blunted* / *dented* (-1), at twice *chipped* / *battered* (-2), and sells for less. A brittle
  weapon already worn, or a cheap one (durability ≤ 200) chipped, can **shatter on a critical**. Artifacts do not wear.
- **Ore** (`Materials.Ores`): copper, iron, silver, mithril and adamantine ore. The Mines put 2–4 lumps on each level, richer deeper;
  every smithy stocks copper and iron.
- **Smithy and armoury** (the Hone service): *Repair my gear* (10 + 40 per wear step, per piece) and *Pour my weapon / armour in X*,
  one row per metal the pack has ore for (2 ore for a weapon, 3 for body armour; bronze from copper, steel and cold iron from iron).
  Pouring keeps enchantment and affixes and resets wear.
- Saves move to version 14.

## Character (`Entities/Player.cs`, `Entities/Roles.cs`, `Entities/Progression.cs`, `Game.Rpg.cs`)

- **Creation** (`Panel.Create`, `Session.CreateKey`): name (≤16 ASCII,
  `Heroes.CleanName`), race, class, confirmation. Every new expedition opens here
  (op `new` with `create`); `Game.NewHero(seed, name, race, role)` builds the hero.
  Name/race/role go in `SaveData` (they are not replay keys); old saves
  load as a human adventurer Wanderer.
- Races (`Entities/Races.cs`): Human, Dwarf, Elf, Halfling, Orc, Gnome, Ashen —
  attribute bias (over the same roll), HP/level, gold, alignment and
  starting skills and **active traits** (data in `RaceDef`): Human +1 free
  advance; Dwarf poison 50%; Elf Mp +25%, evasion +1; Halfling evasion +2; Orc
  regen ×2, poison 25%; Gnome Mp +15%; Ashen fire 50%, cold −25%.
  `Player.CharName` is the name; `Name` stays "you" in the log.
- **Derived attributes** (`Entities/Stats.cs`, `Player`, `Game.Rpg.cs`):
  - `DamageType` (Physical/Fire/Cold/Lightning/Poison/Necrotic/Holy) and
    `Player.ResistPct`/`ResistDamage` (today: fire/dart traps and poison).
  - **Mp**: `RoleDef.MpBase/MpPerLevel/MpStat` + attribute above 10 + Magic/10,
    × the race's `MpPct`. Rises and refills on level-up; `MP` bar in the sidebar.
  - **Regeneration** (`Game.Regenerate`, no RNG): HP 1 every 6–20 turns
    (level/Con, ÷ `RegenPct`); halted by hunger or poison. Mp 1 every 4–20 (Int/Wis/Magic).
  - **Skill ranks** (`SkillRanks`): Novice 0, Trained 25, Skilled 50, Expert 75,
    Master 100. Combat gives +rank to hit; Dodging +rank to evasion.
    **Per-class cap** (`RoleDef.SkillCaps`, e.g. Fighter Magic 40) applied in `GainSkill`.
  - Saves moved to version 3 (replay depends on the rules and item catalog); old saves stop loading (current version: 11).
- Roles: Adventurer (default, classic kit preserved), Fighter, Rogue,
  Cleric, Wizard, Ranger, Paladin, Necromancer — attribute bias over the same base roll (same seed,
  same base), starting skills, own kit/gold/rations and HP/level
  (3–6), 4 titles per level (e.g. Apprentice→Archmage).
- `Game(seed)` = adventurer (compatible); `Game(seed, roleId)` or
  `NewGameWithRole` for the rest. The title follows the level via
  `Roles.TitleFor`.
- **Level-up** (`Entities/Progression.cs`): the base (HP/XP/heal) applies immediately and each
  level queues 1 pick (`PendingAdvances`; humans start with 1). The
  **Advancement** panel (`Shift+C`, opens by itself on level-up) lists only the available
  perks (`Progression.Available`): filtered by class, minimum level, minimum skill,
  prerequisite perk, Mp (magic perks) and max rank. Arrows or letter, Enter takes.
  - Attributes (ranks up to 4–5): tough, mighty, agile, hale, learned, devout, focused.
  - General: lucky (evasion), iron-will (poison), gourmand (hunger ÷2), quick-learner (+15% XP),
    vigorous (+Vigor and regen).
  - Martial: weapon-master (+1 hit/damage per rank), shield-wall, aura (Paladin),
    light-feet (monsters notice from closer), keen-eye (missiles).
  - Magic: mind-expansion (+6 Mp), focus (−5% failure), spell-power (+2 damage), quick-recovery.
  - Perks that **teach abilities** (below).
- **Vigor** (`Player.Vigor/VigorMax`, `VG` bar when there are abilities): 8 + 2×level + (Con−10)/2
  (+8 from *vigorous*); 1 point every 5 turns (less with *vigorous*); refills on level-up.
- **Active abilities** (`Abilities.cs`, `Game.Abilities.cs`; `Shift+V`): Power Strike (×2),
  Backstab (×3 on a sleeping/fleeing/confused/unaware target, otherwise ×2), Holy Strike (+2d6 holy,
  undead ×2), Shield Bash (needs a shield; target loses ~2 turns), Cleave (all adjacent),
  Aimed Shot (+4 hit, ×2 damage, range 8), Second Wind (¼ of HP), Lay on Hands (½ of HP and
  cures poison), War Cry (living creatures within 6 cells flee), Vanish (invisible 12 turns). Without Vigor or
  with an invalid target: refuses **without spending a turn**. Each martial class starts with one
  (`RoleDef.StartPerks`: Fighter power-strike, Rogue backstab, Ranger aimed-shot, Paladin lay-on-hands).
- **Martial techniques** (`AbilityDef.Level` > 0): the Fighter starts with *a manual of strikes*; reading it (`r`) teaches a Fighter,
  Paladin or Adventurer every technique their level allows, and rereading later teaches the rest. Hamstring (3 Vigor, level 1: a leg wound
  one step worse), Disarming Blow (5, level 3: the arm, two steps worse), Skull Crack (6, level 5: the head, two steps worse, stuns), Lunge
  (4, level 7: steps in from two squares away and strikes ×2). They live in the abilities panel (`Shift+V`) and animate like spells.
- The Character panel shows perks with rank; saves moved to version 4.

## Magic (`Magic/Spells*.cs`, `Game.Magic*.cs`)

Full catalog, recipes, animations and items in [`spells-and-items.md`](spells-and-items.md). Summary:

- `SpellDef` is data (id, level 1–5, school, Mp cost, target, range, radius, summons) **and recipe** (damage, riders, buff, heal,
  summon, surface, push, drain, chain, random hits, corruption, special) **and look** (`FxKind`, `Elem`, glyph).
  Older spells have their own method in `Game.Magic.Effects.cs` (`ApplySpell`); the rest are resolved by `Game.Magic.Recipes.cs`.
  **322 spells in 8 schools**: Evocation (39), Conjuration (26), Alteration (31), Illusion (20), Necromancy (50), Sacred (51), Nature (58), Shadow (47).
- Targets: `Self`, `Monster` (hostile), `Cell` (empty cell), `Area` (visible cell), `Line` (from you, pierces, stopped by walls),
  `Cone` (from you, widens with distance). Derived shapes: Single, Ball, Line, Cone, Nova (around you), Chain, Scatter.
  The undead ignore necrotic damage and poison; holy doubles against them.
- **Buffs** (`Player.Buffs`, id → turns; `SpellBuffs.cs`): add into `Gear`, adjust AC, regenerate, retaliate and show as pills on the status bar.
- **Monster states** from spells: fear, slow, sleep, confusion (and blindness), **held/stunned** (`HeldTurns`), **damage over time**
  (poison, bleeding: `DotTurns`), **vulnerable** (`VulnTurns`, +25% spell damage).
- **Allies** (`Monster.Ally`, `SummonTurns`; `AllyTurn`): attack the nearest visible hostile, otherwise follow you; walking into an ally swaps places;
  hostiles adjacent to an ally (and far from you) attack the ally. Summons vanish on expiry (`charmed` turns hostile again) and on level change.
  26 summon-only creatures (`Ally(...)` in the bestiary, branch `~summon`: they never appear on their own). Ally kills grant XP to the player.
- `Player.Spells` holds learned spells; unique items and **imbued magic items** (`Item.Imbue`, `SpellFit`) **lend** others while in use (`Game.CastableSpells`, `Game.Knows`).
  Roles with Mp start with spells and a book: Wizard magic-missile/ward, Cleric/Paladin cure-wounds, Necromancer drain-life/raise-skeleton,
  **Ranger** thorn-dart/barkskin (*a druid's handbook*), **Rogue** throwing-knives/hex (*a cutpurse's primer*).
- **Casting**: `Shift+Z` opens the panel with **school tabs** (`Panel.Spells`; `←`/`→`, arrows, letter a–z, `Enter`, `Esc`). A targeted spell opens the aim
  (`TargetingMode.Cast`) already on the nearest visible monster. No mana, invalid target or no line: refuses **without spending a turn**.
- **Failure** (`Spells.FailPct`): 20 + 10×level − 3×(Int/Wis−10) − Magic/4 − focus + 2×armor AC + 3×shield AC (0–95%). On failure: spends half the Mp, 1 turn
  (and a flash of shadow). Items (`CastFromItem`) never fail and spend no Mp.
- **Learning** (`r` on a book → `StudyBook`): one attempt per unknown spell, 2×level turns each; chance `Spells.LearnPct`; failure = dizziness
  (confusion), never damage. Refused with an enemy in view; classes without Mp cannot learn. **55 books** (`Spells.BookList`), across 5 depth tiers.
- **Animations** (`Fx.cs`, `Game.Fx.cs`, `desktop/src/fx.ts`): every effect records an `FxTimeline` that goes in the frame (`Frame.fx`) and is played by the frontend.
  See [`spells-and-items.md`](spells-and-items.md#animations) and [`renderer.md`](../tech/renderer.md).
- Spell panel keys pass **without** the shortcut map (letters become selection, not movement) and enter the replay log normally.

## Auto-walk (`Game.Explore.cs`, `Game.Repeat.cs`, `Commands.DoAutoWalk`)

- **Explore** (`t`): BFS over already-seen cells to the nearest goal: an unvisited item pile or a
  walkable cell with a never-seen neighbor (`ExploreGoal`). Served cells go in `_exploreDone`, which guarantees
  termination. **Stairs** (`` ` ``): `StairsStep`. **Rest** (`Shift+S`): `Wait` until HP/Mp are full.
- Each one is a loop of normal turns (limit 600; rest 3000) that stops on a hostile in view, damage, any new
  `Say` (`Game.Said`), an item/stairs underfoot or a level change. No RNG of its own: the replay reproduces it.
- **Held key** (`Game.CanKeepWalking`, `Session.KeyRepeat`): see `architecture.md`.

## End of run: cause, morgue and history (`Morgue.cs`, `Game.Death.cs`, `SaveStore`)

- `Game.HurtBy(cause)` marks who last hurt the player (monsters, traps, poison, fire, hunger…);
  `CheckDeath` records `DeathCause`. `quit` marks `Abandoned`.
- `Morgue.Summarize/Text` are pure functions of the finished game. The text carries a *Legend* section (`Game.LegendOf`: bosses slain,
  kills, named works, relics carried, raids beaten off, legends learned, and the world's raids) and a *Relics* section with each relic's
  story and deeds. `Session.RecordRun` (once per run, never on replay) writes `morgue/*.txt` and appends to `history.json` in the
  data directory (`OSSUARY_DATA` in tests).

## Graveyard (`Bones.cs`)

- Dying in a dungeon, level ≥ 2 (`Game.LeaveBones`), writes `Bones` via `SaveStore.WriteBones`; it carries the hero's `Legend`
  (what they did, in a few lines) as well as their gear. `Game.Graveyard` is fixed at the start of the run (and goes in the save), so
  the replay finds the same shades — and the same heroes in the next world's history. `RaiseBones` runs only on the level's first
  generation, with a private Rng (`seed ^ hash(branch, depth)`): 60% chance, far from the entrance. The shade is a rescaled
  `wandering wraith` (`BonesKey` marks which one); destroying it enters `Game.LaidToRest` and the host removes the file.

## Modes (`Difficulty.cs`)

- `Game.Difficulty` is fixed by the host before the first key (`Session.Start`) and saved in `SaveData.Difficulty`.
  **Classic**: `ProcessHunger` does nothing. **Hardcore**: `Session.Save(forQuit)` only writes on exit (Main menu/Quit),
  `QuickSave` and *Save game* refuse, and `Load` deletes the file after replaying the run.

## Reputation, contracts, events and routine (`Game.Reputation.cs`, `Game.Contracts.cs`, `Game.Events.cs`)

- `Houses` (watch, temple, guild, cult), `Player.Rep`, `AddRep`, `Haggle(price, house)` (applied in `ShopPrice`, `RestPrice`, `HealPrice`, `CurePrice`, purge, caravan).
- Contracts: `ContractOffers` (deterministic per town and week), `AcceptContract`, `TurnInContract`; `offer:N`/`turnin:N` lines in the Guild service panel.
- Events: `MaybeRaiseEvent` (after `CheckOverworldEncounter`) → `OpenEvent`; `CurrentEvent` makes `ServiceRows/ServiceAction` answer for the event.
- `GoHomeAtNight` in `TownsfolkTurn`; `TownText.Reaction` for reactive lines.

## Monster factions (`Game.Factions.cs`)

- `FactionOf(m)` by name/flags; `AreRivals`: Greenskin↔Deepfolk, Dead↔Wild. In `MonsterTurn`, with the hero more than 1 cell away, `FightRival` attacks the
  adjacent rival or approaches one within ≤6 cells if it is closer than the hero. Kills between monsters grant no XP.

## Optional branches: The Annex and the Hollow Court

- `Branch.Parent`/`ParentDepth` mark a side branch; `LevelBuilder.Populate` puts the portal on *Dungeons 4* (the Annex) and on *Mines 5*
  (the Hollow Court). `Game.UsePortal` enters/leaves (stores `_portalX/_portalY`), and sets `AnnexVisited`/`CourtVisited`. Level 1 of a
  side branch is generated with the exit portal under the arrival point. Monsters/loot use `depth+6` (Annex) or `depth+5` (Court).
- The Hollow Court: three floors of fort and catacomb, the **Hollow Queen** (`hollow-queen`, a crowned wraith that blows cold kisses
  and calls her courtiers) on level 3, the Queen's Thorn and the Gown of the Last Dance, and two achievements.

## Bosses (`Game.Bosses.cs`)

- `BossDef` (data) + `Game.BossTurn` (habits, by `Monster.BossClock` and `BossPhase`). `RaiseBosses` places the boss on the level's first generation
  (like `RaiseBones`, with a private Rng); `BossFalls` pays the reward; `BossesSlain` feeds the achievements.

## Monsters per branch (`Game.Traits.cs`)

- `MonsterDef.Branch` limits spawning (`Bestiary.SpawnTable(depth, rng, branch)`); `MonsterDef.Trait` hooks a habit at a fixed point of the turn:
  `TraitBeforeAct` (erratic, swarm, pack), `TraitAfterHit` (plague, slam, ashen), `TraitTick` (drowned) and `TraitOnDeath` (wisp).

## Sets and relics (`Items/Affixes.cs`, `Game.Corruption.cs`)

- `ArtifactDef.Set` ties a piece to an `ArtifactSetDef` (`Two`/`Three` bonuses, summed by `ArtifactSets.Bonus` in `Player.Gear`).
  `ArtifactDef.Corrupts` marks relics: `AmuletCorrupts` adds `WornRelics()` every 25 turns. One artifact per (branch, depth).

## Crafting for everyone (`Items/Trades.cs`, `Game.Crafting.cs`, `Game.Gathering.cs`, `Game.Workshops.cs`)

Any hero can live as a craftsman and never enter the dungeon: gather or buy raw goods, make things, sell them, take commissions.

- **Trades** (`Trades.All`, 16): blacksmith, armourer, bowyer, leatherworker, tailor, jeweller, alchemist, scribe, carpenter, toolmaker,
  luthier, cook, brewer, miner, musician, forager. `Player.TradeXp` by id; ranks at 0/20/60/150/300 xp: Novice, Apprentice, Journeyman,
  Master, Grandmaster (`Trades.Rank`). Any class or race; xp comes from the work (`Game.GainTrade`, which announces a new rank).
- **Goods** (`ItemKind.Material`): a bar per metal, log, plank, raw hide, leather, flax, thread, cloth, healing herb, swiftroot, nightshade,
  glass flask, parchment, ink, tallow, barley, honey, raw meat, raw fish. **Meals** (food): roast meat, grilled fish, hearty stew, bread,
  honey cake, ale, mead. **Instruments** (tools): flute, drum, tambourine, lute, horn, fiddle, harp. New tool: fishing rod.
- **Recipes** (`Trades.Recipes`, ~100, data): trade, rank, station, product, needs (names or tokens `#remains`, `#blade`, `#light-armour`,
  `#gem`) and bars. **Stations**: anywhere, a town workshop (`GameMode.TownMap`), beside a forge tile (smithies), a loom (general stores) or a still (taverns and alchemists).
  A recipe with bars makes the piece **in the bar's metal**, and the metal asks for rank too (`Trades.MetalRank`: steel and silver 1,
  cold iron 2, mithril and adamantine 3). Smelting (miner): copper/iron/silver/mithril/adamantine ore into bars; bronze from copper, steel and
  cold iron from iron.
- **Craft** (`Shift+B`): `CraftChoices` lists every plan the pack, place and ranks allow (one row per bar metal), as previews
  (`Uid = −1 − index`, built without RNG); `Craft` spends the parts, rolls failure (none for rank-0 recipes, 10% per missing rank above the
  recipe's), quality for gear (crude −1, plain, fine +1, masterwork +2 with the masterwork edge on weapons and the maker's name), a spell for
  a jeweller's band or pendant (`ItemRoller`), and a bonus unit for a master's batches. A turn in the dungeon or town, an hour on the road.
  `Shift+J` lists the whole book (`RecipeBook`), with a dot on what is still out of reach.
- **Named masterworks**: when a master (20%) or grandmaster (33%, `NamedChance`) rolls a masterwork, it becomes a relic: +3, a proper
  name (`Masterworks.Coin`, a hash of seed and uid, so naming draws no RNG) in front of what it is (`Item.Title`: "Ashtooth, +3 masterwork
  steel long sword"), twice the trade value plus 300, a line in the log and a *Named works* line in the morgue (`Player.Works`). Every crafted
  thing remembers its maker (`Item.Maker`).
- **Ammunition** (`Items/Ammo.cs`, `Game.Ammo.cs`, `ItemKind.Ammo`): arrows (1d6) for bows, crossbow bolts (1d8) for crossbows, sling
  stones (1d6) for slings. `f` and Aimed Shot use the launcher in hand, or the first one in the pack that has something to loose
  (`Quiver`); damage is the ammunition's dice plus the launcher's pull (`Ammo.Pull`: sling 1, bow 2, elven bow and crossbow 3), both
  enchantments and the head's metal, and a silver or cold iron head is a bane like a blade of that metal. Each shot spends one piece
  (`Spend`): it lands at the target and stacks there, unless it snapped (arrows 30% on a hit, bolts 20%, stones 10%; a third of that on a
  miss). No launcher or nothing to loose: a hurled stone, 1d4+1. Bowyer recipes: 8 arrows from a log and a thread anywhere, 12 metal-headed
  arrows or 10 bolts per bar at the forge (+1 from a journeyman), 6 sling stones from a rock. Rangers start with a short bow and 30 arrows,
  rogues with a sling and 15 stones; smithies sell bundles; loot has quivers. Ammunition, goods, rocks and food merge into one stack when
  picked up or made (`Game.Pack`).
- **Examine** (`Shift+I`, `DescribeItem`): what the thing is, its numbers, material, maker and quality, engraving, wear and worth.
- **Gathering** (`Shift+G`, `Game.Gather`): on a carcass, butcher it for raw meat and a hide (cook and leatherworker xp). On the road, two
  hours by terrain: forest (logs with an axe, herbs, honey), grass and road (flax, barley, herbs, swiftroot), hills and mountains (ore with a
  pick-axe), swamp (nightshade), water and shore (fish with a rod), ruins (copper ore); then the usual chance of an encounter. Digging rock
  (`DigAt` → `MineVein`) breaks ore loose: 30% in the Mines, 6% elsewhere, +5% per miner rank.
- **Music** (`a` on an instrument, `PlayInstrument`): in town the street pays once a day (rank, Cha, instrument); in the dungeon it may lull
  visible creatures within 6 to sleep (never the mindless or undead); on the road it is practice.
- **Workshops** (`TradesTaughtAt`): smithy (blacksmith, miner, toolmaker), armoury (armourer, leatherworker), alchemist, general store
  (carpenter, tailor, bowyer), tavern (cook, brewer, musician, luthier), inn (cook), emporium (jeweller, scribe), library (scribe), guild
  (forager). A master teaches the next rank up to journeyman (`LearnPrice`: 60, 240). Each workshop has one **commission** a week
  (`CommissionOffer`, a function of town, week and building; a `Contract` of kind `make`), delivered at that workshop for gold, Guild
  standing and trade xp.
- **Shops** stock raw goods without drawing RNG (`Town.Staples`); goods and food sell by the stack (`ShopPrice`).
- The **molotov** (`Crafted.Molotov`, alchemist rank 0, anywhere) uses `TargetingMode.Throw` → `ThrowAt`: fire on the target and the four
  walkable neighbours, `SetAlight` + fire damage on the monster.
- Saves move to version 15; ammunition and named masterworks to 18; stations, identify by use and the rest of Version 19 to 19.

## Identify by use (`Items/Appearances.cs`, `Game.Identify.cs`)

- Potions, scrolls and wands start as a look: "murky amber potion", "twisted oak wand", "scroll labelled ZELGO MER". Looks are assigned per
  run from a hash of the seed and the item name, probing past taken looks, so no two kinds share one (`Appearances.For`, seed in
  `Appearances.Seed`, set by `Game`). `Item.Name` shows the look while `!Identified`.
- `KnownKinds` holds the names learned this run. `Learn` runs on drinking, reading, zapping (`Quaff`, `UseScroll`, `UseWand`,
  `CastFromItem`), appraisal, scroll or spell of identify; `Recognise` (from `Pack`) knows a picked or bought thing of a learned kind,
  and an identified thing entering the pack teaches its kind. Logic reads `Def.Name`, never the shown name. F6 counts kinds known.

## Item services (`Game.Services.cs`)

- **Recharge** (emporium, library): a wand with spent charges, `40 + 15 × spent + 40 × times recharged`, haggled by the Guild. The first
  recharge is safe; each after risks 30% more (`RechargeRisk`) of the wand splitting for 2–8 damage, and each after the first costs a
  charge of capacity.
- **Lift a curse** (temple): `80 + 10 × level`; frees every cursed thing worn.
- **Cursed amulets**: strangulation, the leech (+2 damage, 10% life steal, a point of HP every 20 turns), restless sleep (+10 MP, +1 spell
  power, may close your eyes when no foe is in view), the hungry dead (+2 Str, +1 Con, double hunger). A cursed amulet will not come off
  (`AmuletStuck`) until a priest lifts it.
- **More relics**: The Weeping Edge (Vaults 6), Marrow Mail (Mines 8), Crown of the Pit (Dungeons 10), all corrupting.

## The chronicle (`World/History.cs`)

- **`History.Of(seed)`** (its own Rng, never the game's) builds three centuries: a present year of 300–600, **houses** rising at a seat
  (about half of them fallen since), a **line of kings** from the first house to now, each succeeding **by blood, by the sword or by the
  lords' choice**, 30 figures (smith, knight, priest, thief, scholar, warlord, kings) with birth, death, epithet, house and home town,
  towns founded, four wars, three plagues, two towns razed and the year the **Pit opened**.
- **Still living**: any figure whose span reaches the present is `Alive` (no death year). **Buried below**: dead knights, warlords,
  kings and priests whose death follows the Pit are given a branch and a depth (`TombBranch/TombDepth`).
- Every line is built in English and Portuguese together and registered with `TownText.L`. `Describe` renders a figure or an event;
  `Entry` walks the chronicle in order; `TombsOn` lists the dead of a level.
- **Relic biographies** (`History.Biography`, keys out): forged of its metal by a smith in a year of their life, made for a king or
  knight alive then, carried by up to two later figures until their deaths, lost in the next war, plague or razing, or carried down
  into the Pit. Shown when an identified unique is examined and in the morgue.
- **Deeds** (`RelicsRemember`): a boss or unique killed writes a line into every relic in hand or worn (`Item.Deeds`); **owners**
  (`Item.Owners`): a monster's gear remembers who it was taken from, a shade's gear the hero who died, a sold named work who bought it.
  `Item.IsRelic`: a unique or a named masterwork. The morgue has a *Relics* section with each story.

## History in play and the Legends panel (`Game.Legends.cs`, `Ui.cs`)

- **`Game.Legends`**: keys learned ("e:i" events, "f:i" figures, "h:name" heroes). Taught by the library's *Read the chronicles* row,
  the Scholar's *Ask about the old days* (`ScholarLine`), a book taken down from a library or emporium shelf (bump it: `TryReadShelf`),
  tavern songs (`SongLine`), rumours (`HistoryRumour`, every third answer at a tavern), relic biographies, engravings and tombs.
- **Heroes before you**: the run's `Graveyard` (bones of earlier runs) becomes delvers of this world's history (`PastHeroes`), sung
  about and listed in the panel.
- **Place names** (`NamePlaces`, at world generation): ruins are named after the towns the chronicle burned, keeps after the houses.
- **Legends panel** (**F8**, `Panel.Legends`): events, people, places (with how much is known of each) and heroes before you, wrapped to
  the panel and scrolled with up/down.
- **The hero's legend** (`LegendOf`): bosses slain, notable kills, named works, relics carried, raids beaten off. The morgue prints it
  under *Legend*, and `LeaveBones` carries it into the next world's history (`Bones.Legend`).

## Blood, fluids and tracks (`Surfaces.cs`, `Game.Stains.cs`, `Ui.cs`)

- A second sparse layer under the surfaces: `GameMap.Stains` holds blood, ichor, slime, mud, soot, footprints and drag marks with the
  turn and the source. `BloodOf` gives the fluid by species: the dead and the made leave nothing, insects and demons ichor, molds and
  oozes slime, the rest blood. `Splatter` on every hit (and a second spatter beside it on a heavy one, placed by hash, never the Rng).
- `TickStains` each turn: the bleeding leave a trail, a limping creature drags, the hero's boots carry blood and mud on as footprints,
  and fire that burns out leaves soot. Stains fade with age and are removed after their life (blood 500 turns, mud 250, footprints 120).
  Drawn as a tint under the floor, darker the fresher; `StainLine` reads one ("fresh blood, left by a kobold").
- **The trail both ways**: `SmellsBlood` — a bleeding hero is noticed within 10 squares even out of sight ("something has caught the
  scent of your blood"). `FollowTrail` (**Shift+M**) steps toward the freshest trail something else left and says where it leads.

## Engravings, rooms and tombs (`Game.Places.cs`, `Gen/DungeonGen.cs`)

- **`DressLevel`**, once per level (its own Rng): old engravings that tell the chronicle (teaching their legend), a warning above a
  boss's lair ("the X waits below"), a clue pointing at a hidden door, and the **tombs** of the chronicle's dead of that level: a grave
  tile, a name cut into it (`Here lies ...`), and what they were buried with (a knight's sword, a warlord's axe, a king's helm, a
  priest's amulet), each remembering its owner (`Item.Owners`).
- **Reading underfoot** (`ReadUnderfoot`): stepping onto an engraving or a tomb says what is written and teaches its legend.
- **Room flavour**: `DungeonGen` keeps its rooms on the map (`GameMap.Rooms`); the first time the hero walks into a special room, one
  line says what it was (a barracks, a temple, a larder, a treasury…), and looking at a cell inside it says the same (`RoomAt`).
- **Carving** (**Shift+O**, `BeginCarve`/`Carve`): on bare floor the hero picks a line (their name was here, turn back, beware, or the
  last kill in the ledger) and cuts it in; it is read like any other engraving, and the ledger records the deed.

## Raids on towns (`Game.Raids.cs`)

- **When**: a pure function of seed, town and week (`RaidIn`) — one week in six or so, more in dangerous country; the band is kobolds
  near the coast roads, orcs further in, the dead in the worst country. The raid has one day of its week.
- **If the hero is there that day** (`RaidsOnArrival`, on entering): raiders come over the wall by the gate (`RaidNow`), hostile in the
  streets (bump them to fight). Killing the last one saves the town (`RaiderKilled`: gold, Watch +10, Guild +3, a line in
  `RaidChronicle`). Walking out on them costs Watch standing and counts as a loss.
- **If the hero is away** (`RaidWhileAway`): the Watch holds, or the town pays — `RaidLosses` burns one shop (its stock gone, its
  keeper dead, its floor black and broken for good: `Building.Burned`) and kills one to three of its people, never the essential ones.
  What came of it is remembered in `RaidChronicle` and shows in the morgue's legend.

## Stations, crafters and archers (`Tile.cs`, `Town.cs`, `Game.Makers.cs`, `Game.Ammo.cs`)

- **Loom** (`╬`, general stores) for cloth, cloaks and the cloak of elvenkind; **still** (`¤`, taverns and alchemists) for ale and mead.
  `TownGen.Workbench` puts a station on the customer's side of the counter, clear of doors and stairs, and every smithy now has a forge
  there too. `NearTile` checks the eight cells around the hero.
- **Crafters elsewhere** (`LocalCraftsmen`, on stepping up to a counter): each new week the house's maker (`Shop.Maker`) sets out one new
  signed piece (smith a weapon, armourer armour, general store a cloak, cloth, candles, boots or arrows, alchemist a potion), keeping at
  most four on the shelf. On market days the rival party takes a stall (`RivalStall`) with arrows they made and a piece brought up from
  the level they reached.
- **Monster archers** (`MonsterShoots`): kobolds, gnomes and orcs may carry a sling, bow or crossbow with 6–12 pieces; at 2–7 squares in
  line of sight they shoot (40% adjacent-ish, 75% further) instead of closing in. The piece lands at the hero's feet unless it snapped.
- **Quiver** (`Shift+Y`, `ChooseQuiver`): `Player.QuiverUid` makes a stack the first one loosed.

## Works in the world (`Game.Works.cs`)

- A sold named masterwork is recorded (`SoldWork`); after `WorkShelfDays` (3) on the shelf a townsperson buys it on the hero's next
  arrival (`WorksFindBuyers`), carries it (looking at them shows it) and remembers who sold it. Every third answer at a tavern may be
  about the hero's works: who carries one, where one is for sale, or the piece the hero forged.

## Caravans, haggling and memory (`Game.Caravans.cs`, `Game.Haggle.cs`)

- **The road's caravan** is the week's caravan of the nearest town (`CaravanTown`). *Ride with them as a guard*: six hours, 40 + 5 × level
  gold, Guild +3, a 40% bandit fight, and that town's news this week becomes *arrived*. *Rob the caravan*: 60–140 gold and rations, Guild
  −6, Watch −5, a 150 bounty, and the town's news becomes *raided*. Stored in `CaravanFate` by town and week; `CaravanThisWeek` reads it
  first.
- **Haggle** (`o` at a counter): offers of 90, 75 or 60% at 70, 45 or 20% odds, plus 3 × Cha bonus, 2 × mood and Guild/10, clamped 5–95.
  Taken: bought at the offer, a *haggled* deed. Refused: a *soured* deed and no more haggling with that trader today.
- **Traders' memory** (ledger deeds by shop name): *flooded* (a sale leaves the class glut at 6 or more, once a day), *cheated* (a cursed
  or ruined thing sold), *haggled*, *soured*. `MemoryMood` moves the trader's mood (soured −8 today, cheated −4 each for 30 days, flooded
  −2 each for a week, haggled +1 per two deals) and `TraderGreeting` says what they remember when the hero steps up.


## Companions (`Game.Companions.cs`)

- The *Hire a sellsword* service (tavern): `HireCompanion` creates a `Monster` with `Companion=true`, `Ally=true`, `SummonTurns=0`; `RescaleCompanion`
  derives HP/AC/damage from the hero's level. `PlaceCompanions` (in `DescendTo`) puts all of them beside the hero on every dungeon level; `ReapCompanions`
  removes from the list whoever was destroyed. `RescaleCompanions` runs in `AnnounceLevelUp`. They use the common `AllyTurn`.

## Corruption and mutations (`Mutations.cs`, `Game.Corruption.cs`)

- `AddCorruption` raises `Player.Corruption` (cap 100); every multiple of 20 crossed calls `GainMutation` (`MutationTable.Pick`: 50% good,
  20% mixed, 30% bad, no repeats). Numeric effects enter `Player.Gear` (and AC in `ArmorClass`); `Sight`, `Hunger` and `Noise` are
  read by `UpdateFov`, `ProcessHunger` and `NoticeRadius`. Sources: the dungeon fountain (`Shift+E`), the Amulet, necromancy, potion of mutation.
  Purge at the temple: `PurgeCorruption`.

## Stealth and noise (`Game.Stealth.cs`)

- `NoticeRadius(m) = max(1, Vision − StealthReduction + NoticeShift)`. `StealthReduction` = Stealth/25 + 2×*light-feet* + Sylk tier 2.
  `NoticeShift` comes from the turn's action (`MakeNoise`: fight 3, spell/zap/fire/door/kick 2, heavy armor when walking; `BeQuiet`: waiting/searching −2).
  `Commands.Execute` zeroes the noise on every verb, so a refused action leaves no noise. Stealth trains in `LearnFromHiding`.

## Found traps (`Game.Traps.cs`)

- Traps are born hidden (`TrapTable`). `Reveal` marks them (drawn as `^` in `theme.Warn`). They are revealed by searching (`s`),
  by passive perception when passing (`SenseTraps`, deterministic by position hash) or stay hidden. `Shift+A` → `DisarmTrap`.

## Achievements (`Achievements.cs`)

- A fixed list of `AchievementDef` (id, name, description, test over `Game`). `Game.Earned` is recomputed by the simulation,
  so the replay earns the same ones. `AlreadyUnlocked` (from the host) only decides whether the log announces. `Session.FlushAchievements`
  writes `achievements.json` (never on replay).

## FOV / pathfinding (`Fov.cs`, `Pathfinder.cs`)

- Shadow FOV (radius 10, +2 with ring of warning), symmetric, tested.
  Symmetric ray casting + corner reveal — see
  [`renderer.md`](../tech/renderer.md) for why it is not slope shadowcasting.
- `FindPath` (A*) + `FlowField`; a door becomes a passage, a sealed pocket = no route.

## Overworld (`World/`, `Game.Overworld.cs`)

- 96×60, named regions with danger/depth, connecting roads,
  ≥3 towns, 1 dungeon per region, day/night (`AdvanceTime`).
- Walking reveals (`Discover`); `O` + Enter = long travel (costs hours, may
  meet a monster or reach a town/dungeon).
- Encounter (a monster blocks the road): **Enter / Space / `K` / `F` attack,
  `R` or `<` flee**. Unshifted `K` is "walk north" in the vi keys, so
  `Session.KeyCore` remaps by the typed character (the code was already
  rewritten by key bindings: `KeyK` arrives as `ArrowUp`). Walking and traveling
  (`O`) are refused with a hint while the monster is there. Killing gives XP,
  announces level-up and drops the loot straight into the pack. Tests:
  `DesktopTests.RoadEncounter`.

## Towns, floors and people (`Town.cs`, `TownText.cs`, `Game.Town.cs`)

The town is **vertical**: a `Town` holds one `GameMap` per floor (`Floors`,
`z = 0` street, positive = upper floors, negative = basements). Every building has the
same footprint on every floor, so the stairs put you at the same (x, y).
`>` descends and `<` ascends (`Game.TownStairs`) - both are `Shift` plus `.` or `,`, on any layout
(see the *Keyboards* section of [`controls.md`](../game/controls.md)); the stairs alternate between two corners
(A on even z, B on odd z) so the one you step on is never the one that continues.
The header shows `▲2`/`▼1`; the map title becomes the building's name.

- **Generation** (`TownGen`): wall with 4 gates, cross of streets, square with
  fountain, 13×11 lots on both sides of the main street. Size by
  population: `hamlet` (8 lots) / `village` / `town` / `city` (12 lots).
  There is always a tavern, general store, smithy and temple; the rest is drawn.
  Layouts in "door-relative" coordinates (u = width, v = depth), so
  the same mold serves lots to the north and south.
- **Buildings**: Smithy (weapons; **sharpen** a weapon up to +3; **repair** and **pour in a metal** from ore, see Materials), Armory (armor;
  **reinforce** armor up to +3, repair and pour as well), Alchemist (potions, lab in the basement),
  Mage Tower/Emporium (wands and scrolls, **appraise** items, 3 floors),
  General store, Tavern (beer, meal, news, basement), Inn (**sleep**
  = heals everything and advances to morning, 2 floors of rooms), Temple (**heal**,
  **remove ailments**, offering that grants piety; gallery above, crypt below),
  Guild (notice board, "ask about the Ossuary" tells the story and the
  Amulet's depth), Library (books, appraise), Barracks (guards, cells
  in the basement), Watchtower, houses, market stalls, graveyards.
- **People** (`Monster.Townsperson`): residents, children, guards, bard,
  beggar, drunk, adventurers, scholars, prisoners, dogs and cats. Lines and
  rumors in `TownText` (EN with PT beside it via `L(en, pt)`). They walk by turn
  hash (`TownsfolkTurn`), never by the game's `Rng`, and never attack.
- **Interaction**: bumping into a person talks; bumping into a counter, notice
  board or altar calls whoever works there. Those who only sell open the shop directly;
  those who do more open the **services** panel (`Panel.Service`, letters a..z
  choose). Inside the walls `f z Z V k` are refused and `Attack` on a resident
  too (the Watch would hang you).
- **Determinism**: the town is a function of the world seed and the place
  (`Rng(seed ^ hash(name@x,y))`), is stored in `_towns` and is the same on every
  visit; entering and leaving does not consume the simulation `Rng` (`TownIsStable`).
- Shop: price = stack value × (100 + min(30, shop_gold/60))% × `BuyPct` × `Haggle`; selling = half the stack value ×
  `SellPct` (+25% for the hero's signed work once a trade is at master). The shop's gold limits both sides. Stock comes
  identified. (`ShopEconomy` and the `Markets` feature test cover it.) See *Economy and market* below.
- Tests: `TownVerticality` (stair pairs, reach on all floors,
  nobody on top of a wall or another person), `TownServices`, `TownIsStable`,
  `DesktopTests.TownFlow`. `headless.ps1 dump town` shows the town, each floor
  of the buildings and the services panel.

## Economy and market (`Game.Market.cs`, `Loc.Market.cs`)

- **Goods classes** (`GoodsClass`): weapons, armour, potions, books and scrolls, wands, jewellery, food, raw goods, tools.
  Ammunition trades as weapons. Goods, food, rocks and ammunition price by the stack (`StackValue`); a bundle of ammunition
  gluts the market by 1 + qty/10.
- **Town smiths' work** (`TownGen.LocalWork`): every smithy and armoury sells one piece its smith made and signed
  (`Item.Maker`, a given name) in bronze, iron or steel, 40% of them fine (+1). Smithies also sell arrows, bolts and sling stones
  by the bundle, and general stores sling stones.
- **The hero's name**: signed work sells for a quarter more once the hero is a master (`MastersWork`); a named masterwork is
  worth twice the piece plus 300 (`Item.TradeValue`).
- **Town taste** (`TownTaste`): -15..+15% per town and class, a hash of seed, town and class.
- **Glut** (`TownMarket.Glut`): each sale into a town adds units to its class (1, or 1 + qty/5 for a stack, cap 12); what the
  town pays falls `GlutStep` (6%) per unit and recovers `GlutRecovery` (2) units a day. Stored per town: it is what the hero did.
- **Caravans** (`CaravanThisWeek`): per town and week, a raided road (chance 12% + 2 × region depth, cap 45%: that class
  costs +40% and sells +25%) or an arrived caravan (that class −15% / −10%, and it buys up the town's glut of it).
  Announced on arrival.
- **Haggling**: `BuyPct`/`SellPct` add Cha − 10 (±10), the trader's mood (`TraderMood`, ±8 by shop and day, shown in the shop
  panel), Guild standing (`Haggle`, and rep/5 % on selling) and paid Guild dues (+10% for a week).
- **Market square**: stalls carry `Service.Market` and open a menu. On market days visiting traders add three wares to each
  stall (seeded by town, stall and two-day bucket; packed up when it ends) and stalls pay +15%.
- **Order board** (`OrdersThisWeek`): two things wanted (60% a recipe product) paying 150%, one thing offered at 60%; per town
  and week, each taken once (`OrdersDone`). Delivering gives Guild +2.
- **Your counter**: rent a stall counter for a week (12/20/30/45 by size), set out up to 8 things, ask cheap/fair/dear
  (55/30/10% a day to sell at 80/100/140% of value, +15% chance on market days). Days are settled lazily (`SettleStall`) on
  arrival and at the stall; takings wait to be collected.
- **Money sinks**: gate toll (town 2, city 5; free for Watch ≥ 25), stall rent, Guild dues (20 + 3 × level). `TollsPaid`, `RentPaid`.
- **Price history**: `PriceHistory` keeps the last price seen per town and class (on stepping up to a counter, on selling, on a
  counter sale); the journal (F7) shows the latest five under *Markets*.

## Living world: personas, dialogue, quests, ledger (`Persona.cs`, `Dialogue.cs`, `Dialogues.cs`, `Quests.cs`, `QuestBook.cs`, `Game.Ledger.cs`)

Plan in `living-world.md` and `main-quest.md`.
- **Persona / NpcMemory**: every townsperson has traits, a want and an `Essential` flag (Elder, Scholar, Captain, Priest), generated from a private `Rng` forked off the town seed, so layout and other people never move. Memory holds a disposition and flags (`met`, `struck`, `helped`). Small talk alternates job lines and trait lines (`TownText.TraitLines`).
- **WorldLedger** (`Game.Ledger.cs`): append-only `Deed` list (killed uniques/bosses, strikes, contracts, quests, failures, favours) written from the game's own choke points; read by lines, prices and later bounty/epilogue. Pure function of seed and keys, no save of its own.
- **Conversation box**: bumping any person (pets excepted) opens a dedicated dialogue box (name and role on top, several wrapped lines, lettered choices below, `DrawDialoguePanel`); the person stands still while it is open and no turn passes. Anyone without a written conversation gets a short chat (*what have you heard*, *how are you*), so every talk offers choices.
- **Dialogue**: nodes with gated, priced choices and effects, written in C# (`Dialogues.cs`, EN with PT beside it). It rides on the service panel (a *Talk* row on counters; direct for the Bard), so there is no protocol change. A person you struck only gives the cold line.
- **Quest engine**: `QuestDef` (track, steps, reward, optional `Deadline` in days) in `QuestBook`; `Game.Quests.cs` counts kills, depth, flags, items, talk and waits, advances steps, pays out and fails by the clock. `Game.Flags` holds story flags. `F7` opens the **Journal** (quests by track, hints and days left, today's town event, the bounty, learned rumours; Guild jobs listed under Guild).
- **Personal errands** (`PersonalQuests.cs`): a troubled person's want (revenge, debt, cure, lost kin) becomes a quest on the spot; finishing it sets `Helped`, +40 disposition, a ledger deed and Guild standing.
- **Rumours** (`Rumours.cs`): a real fact (a branch boss or a named place), told true, vague or wrong by the teller's temperament; true places are marked on the map, bosses open a Region quest.
- **Crime and the Watch** (`Game.Crime.cs`): `k` strikes the person in front; witnesses by line of sight; bounty by harm (assault 15+2×damage, murder 1000, guard or priest 1500) kept per region, neighbours hold half; the victim and guards turn hostile while they see the hero, then calm; a guard who sees a wanted hero arrests (pay, cells, bribe, resist); cells pass days; the Captain clears names for gold or a favour; essentials are knocked out, never killed; a murdered town grieves in its small talk.
- **Town events** (`Game.TownEvents.cs`): a deterministic two-day schedule (market, festival, funeral, robbery, fever) with prices, closed doors, lines and a Watch job.
- **Travellers and the rival** (`Game.Travellers.cs`, `Game.Rival.cs`): pilgrim, peddler, refugee and delver road events; a hired sword who comments on new depths; a named rival party that descends on its own clock and can be raced.
- **Main questline** (`Game.Main.cs`): four documents placed with their own seeded Rng, the Reader, the truths (which make the Holds and the League charge more), and six endings chosen when the hero walks out with the Amulet after reading the Seal; the Stamp starts a new cycle. The short path (take the Amulet and leave) is unchanged.

## UI (`GameHud.cs`, `Ui.cs`, `UiState.cs`, `TextBuilder.cs`)

- Frame: map + sidebar (Health/Energy, Depth/Turn, equipment, AC/gold),
  status line (`Dlvl HP … XP region`) and log. Minimap (`m`).
- Panels: Inventory, Character, Help, History, Discoveries, Choice, Travel,
  Shop, Death, Win. Aim (`x`/`l`/`v`, `f` fire, `X` swap): the cursor eats
  the movement keys (`NudgeTarget`, Enter confirms, Esc cancels).
