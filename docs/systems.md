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
  See [`spells-and-items.md`](spells-and-items.md#animations) and [`renderer.md`](renderer.md).
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
- `Morgue.Summarize/Text` are pure functions of the finished game. `Session.RecordRun` (once per run, never on
  replay) writes `morgue/*.txt` and appends to `history.json` in the data directory (`OSSUARY_DATA` in tests).

## Graveyard (`Bones.cs`)

- Dying in a dungeon, level ≥ 2 (`Game.LeaveBones`), writes `Bones` via `SaveStore.WriteBones`. `Game.Graveyard` is fixed
  at the start of the run (and goes in the save), so the replay finds the same shades. `RaiseBones` runs only on the level's
  first generation, with a private Rng (`seed ^ hash(branch, depth)`): 60% chance, far from the entrance. The shade is a
  rescaled `wandering wraith` (`BonesKey` marks which one); destroying it enters `Game.LaidToRest` and the host removes the file.

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

## Optional branch: The Annex

- `Branch.Parent`/`ParentDepth` mark a side branch; `LevelBuilder.Populate` puts the portal on *Dungeons 4*. `Game.UsePortal` enters/leaves (stores `_portalX/_portalY`).
  Annex level 1 is generated with the exit portal under the arrival point. Monsters/loot use `depth+6`.

## Bosses (`Game.Bosses.cs`)

- `BossDef` (data) + `Game.BossTurn` (habits, by `Monster.BossClock` and `BossPhase`). `RaiseBosses` places the boss on the level's first generation
  (like `RaiseBones`, with a private Rng); `BossFalls` pays the reward; `BossesSlain` feeds the achievements.

## Monsters per branch (`Game.Traits.cs`)

- `MonsterDef.Branch` limits spawning (`Bestiary.SpawnTable(depth, rng, branch)`); `MonsterDef.Trait` hooks a habit at a fixed point of the turn:
  `TraitBeforeAct` (erratic, swarm, pack), `TraitAfterHit` (plague, slam, ashen), `TraitTick` (drowned) and `TraitOnDeath` (wisp).

## Sets and relics (`Items/Affixes.cs`, `Game.Corruption.cs`)

- `ArtifactDef.Set` ties a piece to an `ArtifactSetDef` (`Two`/`Three` bonuses, summed by `ArtifactSets.Bonus` in `Player.Gear`).
  `ArtifactDef.Corrupts` marks relics: `AmuletCorrupts` adds `WornRelics()` every 25 turns. One artifact per (branch, depth).

## Crafting (`Game.Crafting.cs`)

- `Recipe` = (Id, Needs, Gather, Make). `CraftChoices` lists what the pack allows, as preview items (`Uid = −1 − index`);
  `CommitChoice` with `CraftPrompt` calls `Craft`, which spends the ingredients and costs a turn. The **molotov** (`Crafted.Molotov`, a tool) uses
  `TargetingMode.Throw` → `ThrowAt`: fire (`PutSurface`) on the target and the four walkable neighbors, `SetAlight` + fire damage on the monster.

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
  [`renderer.md`](renderer.md) for why it is not slope shadowcasting.
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
`>` descends and `<` ascends (`Game.TownStairs`); the stairs alternate between two corners
(A on even z, B on odd z) so the one you step on is never the one that continues.
The header shows `▲2`/`▼1`; the map title becomes the building's name.

- **Generation** (`TownGen`): wall with 4 gates, cross of streets, square with
  fountain, 13×11 lots on both sides of the main street. Size by
  population: `hamlet` (8 lots) / `village` / `town` / `city` (12 lots).
  There is always a tavern, general store, smithy and temple; the rest is drawn.
  Layouts in "door-relative" coordinates (u = width, v = depth), so
  the same mold serves lots to the north and south.
- **Buildings**: Smithy (weapons; **sharpen** a weapon up to +3), Armory (armor;
  **reinforce** armor up to +3), Alchemist (potions, lab in the basement),
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
- Shop: price = cost × (100 + shop_gold/60)%; selling = half. The shop's gold
  limits both sides. Stock comes identified. (`ShopEconomy` tests it.)
- Tests: `TownVerticality` (stair pairs, reach on all floors,
  nobody on top of a wall or another person), `TownServices`, `TownIsStable`,
  `DesktopTests.TownFlow`. `headless.ps1 dump town` shows the town, each floor
  of the buildings and the services panel.

## UI (`GameHud.cs`, `Ui.cs`, `UiState.cs`, `TextBuilder.cs`)

- Frame: map + sidebar (Health/Energy, Depth/Turn, equipment, AC/gold),
  status line (`Dlvl HP … XP region`) and log. Minimap (`m`).
- Panels: Inventory, Character, Help, History, Discoveries, Choice, Travel,
  Shop, Death, Win. Aim (`x`/`l`/`v`, `f` fire, `X` swap): the cursor eats
  the movement keys (`NudgeTarget`, Enter confirms, Esc cancels).
