# To do — Ossuary

Living tracker of what is left in the game. Single source of progress: **every session
reads this file before starting and updates it when done** (see `AGENTS.md`).

## How to use

- States: `[ ]` pending · `[~]` in progress · `[x]` done (with date and where it lives) · `[-]` dropped (with reason).
- When finishing an item: mark `[x]`, note the date and the file/system (`Game.<Subject>.cs`),
  and document the system in `systems.md` / `controls.md` as usual.
- A new idea goes in the right category, in one line, with the why.
- Priority is the order within a category (top to bottom), unless noted otherwise.
- Design references: Caves of Qud (QD), DCSS, Brogue, Cogmind, Sil, NetHack, Angband.

---

## Future ideas (nothing pending from the original backlog)

Everything that was listed has been implemented (see *Done*). What is left are extensions noted along the way:

- [ ] **Music**: the menu already has the volume; nothing composed/synthesized yet (today only effects).
- [ ] **Player-marked travel points** (today `~` goes to remembered altars/fountains and `` ` `` to the stairs).
- [ ] **Companions**: orders (stay/follow), shared inventory, more than one, archers.
- [ ] **Pacifist challenge** and more achievements (per class, per god, per branch).
- [ ] **Magic**: ice × lightning interaction; PT translation of altar texts and combat messages (spell names and descriptions are already in PT).
- [ ] **Martial techniques** for the Fighter (no Mp today): a "book" of strikes using Vigor in the same panel, with the same animations.
- [ ] **Animations**: monsters casting spells (sorcerers, new bosses) and damage arriving *with* the projectile (today the spell is already resolved when the animation plays);
  per-element sound; a key to skip the whole animation.
- [ ] **Balance bot** that uses the 322 spells (today it knows the ~40 old ones): see docs/balance.md.
- [ ] **Second optional portal in another branch**; more corrupting relics.
- [ ] **Items**: identify wands/potions/scrolls by use (today they are born identified by name), recharge wands at the shop, new cursed amulets.

## Living world (NPCs, quest tracks, other people)

Plan and rationale in `living-world.md`. Phases are ordered; each ships with tests and a docs update.
Today (2026-10-03): townsfolk have a role and a line, the Guild board has two job kinds, rumours are flat flavour, nobody else is out in the world.

- [x] **1. Persona + NpcMemory** (2026-10-03): `Persona.cs` (8 traits, wants, essential flag for Elder/Scholar/Captain/Priest), `NpcMemory` (disposition + flags) on every townsperson, generated from a private `Rng` forked off the town seed (layout and other people unchanged); trait lines in small talk (`TownText.TraitLines`, EN+PT); a blow to a citizen is remembered (line changes, Watch rep -3 once per person) and recorded in the first `WorldLedger` (`Game.Ledger.cs`: killed uniques/bosses, contracts, strikes). Test `PersonaAndLedger`. Fear/secret/ties/successor fields still to come with phases 4 and 8.
- [x] **Conversation box (Qud style)** (2026-10-03): every talk opens a dialogue box instead of a log line; the person is frozen while it is open (`TownsfolkTurn` skips whoever you talk to); ordinary people get a short chat with choices. Test `Conversations`. Left: portraits/mood tags in the box, scrollable history of the conversation.
- [x] **2. Dialogue** (2026-10-03): `Dialogue.cs` (nodes, gated/priced choices, effects) + `Game.Dialogue.cs` (runs on the service panel like road events, so no protocol/frontend change; a *Talk* row on counters, direct for the Bard) + `Dialogues.cs` (Elder, Captain, Priest, Innkeeper, Bard, EN+PT; story flags in `Game.Flags`). A struck person only gives the cold line. Test `Conversations`.
- [~] **3. Quest engine + journal** (2026-10-03): `Quests.cs`/`QuestBook.cs`/`Game.Quests.cs` (objectives Kill, Reach, Flag, Item, Talk, Wait; rewards; deadlines), `Panel.Journal` on F7 with hints and days left, first quests `main.seal` (prologue) and `watch.bandits` (14-day deadline, from the Captain). Test `QuestEngine`. Left: moving `Game.Contracts.cs` fully onto the engine (today Guild jobs are listed in the Journal but run on the old code).
- [x] **4. Personal / Watch / Temple tracks** (2026-10-03): `PersonalQuests.cs` turns a troubled person's want into an errand (avenge = kill three kobolds, debt = pay forty gold, cure = bring a healing potion, kin = find them in town and tell the asker); finishing it sets `Helped`, +40 disposition, a ledger deed and Guild standing, and the person's line changes; the kin says someone is looking for them. Watch (`watch.bandits`) and Temple (`temple.rest`, four skeletons) jobs come from the Captain and the Priest. Test `PersonalErrands`. Escort and find-an-item-in-the-dungeon errands wait for phase 7.
- [x] **5. Rumours with teeth** (2026-10-03): `Rumours.cs` picks a real fact (a branch boss, or a named place on the overworld), the teller's temperament decides true / vague / wrong (drunks 55% wrong, scholars 5%), a true place is marked on the map, a boss opens a Region quest that ends when it falls; every third answer at the Guild, tavern or inn is a fact, the rest is gossip; learned rumours are listed in the Journal. Test `RumoursWithTeeth`. Altars and relics inside dungeon levels are not rumoured yet (levels are generated lazily).
- [x] **6. Town events** (2026-10-03): `Game.TownEvents.cs`, a deterministic two-day schedule per town (seed + town + day): market day (-10% services), festival (-20%), funeral (temple healing shut), robbery (a 6-day Watch job from the Captain), fever (inn rest and meals shut); announced on arrival, in small talk, and in the Journal. Test `TownEvents`. Left: fire and raid events, burnt buildings that stay burnt.
- [~] **7. Other people on the map** (2026-10-03): four new road events (pilgrim, peddler with a map fragment, refugees, a wounded delver) with choices the world remembers (ledger, reputation, a flag); the hired sword comments every other level (`Game.Travellers.cs`). Tests `RoadTravellers`. Left: NPC delvers and captives inside dungeon levels, caravans that move on the overworld.
- [x] **8. Rival party and Main track** (2026-10-03): `Game.Rival.cs` (a named party that descends one level every four days from day 3, reported in taverns, a 150g race to Dungeons level 7 that can be lost by the clock) and `Game.Main.cs` + `Dialogues.cs` (four documents placed with their own seeded Rng in the Dungeons 5, Mines 6, Vaults 8, Spire 12; the Reader explains them and unlocks the truths; walking out with the Amulet after reading the Seal opens the ending choice: Drifter's Pay, The Pit Shut, The Archive Opens, The Warden, The Auction, The Stamp; the Stamp starts a new cycle instead of ending the run; the Holds and the League charge more once the Vote and the Order are known). `main.seal` quest follows it in the Journal. Tests `MainQuestline`, `RivalRace`. Save version bumped to 12. Left: house authority at the ceremony (Priest/Captain/Cult speaker as actors), the Archivist's Shade and the Spire guardian as encounters, Elder successors, ending ids in the morgue and daily leaderboard, new-cycle carry-over beyond truths and the ledger.
- [~] **9. Cult track + polish** (2026-10-03): `cult.vial` (the beggar who is not a beggar, opens at Cult standing 10; costs Temple and Watch standing and corruption); `soak` still runs clean with dialogue, quests and crime in the game. Left: more Cult quests, reward balance with the balance bot, Guild jobs fully on the quest engine.
- [x] **Crime, hostility and the Watch** (design in `living-world.md`, *Crime and the Watch*): hostility lasts while seen plus a short grace; witnessed crimes build a per-region bounty that persists; guards arrest on sight (pay / jail / resist / flee / persuade); jail passes days (timed quests can fail) and can be escaped with the lock pick; name cleared by gold, favour or time served. Today townsfolk are untouchable. **Done 2026-10-03**: `Game.Crime.cs` — `k` in town strikes the person in front (other attacks stay refused); witnesses by line of sight (range 9); assault 15+2×damage (max 150), murder 1000 (guard/priest 1500), +100 for laying hands on an essential; the victim and every guard turn hostile (cowards run) for 40/60 turns, extended while they see the hero, then calm; the bounty persists per region and neighbours hold half; a guard who sees the hero arrests (pay / cells / bribe 1.5× / resist); cells pass 1+bounty/100 days (max 30), clear the name and let timed quests run out; the Captain clears names for gold or a Guild/Temple favour. Sentences, grief lines for a murdered town, journal "Wanted". Test `CrimeAndTheWatch`. Left: theft and property crimes (no steal/burn verbs exist yet), lock-pick escape from the cells.
- [~] **Essential NPCs** (`Persona.Essential`, `Persona.Successor`): knocked out instead of killed, recover after days; successor, two sources of knowledge and no gate without bypass (see `main-quest.md`). **Partial 2026-10-03**: essentials (Elder, Scholar, Captain, Priest) are knocked out for 3 days instead of dying, and their counter is closed meanwhile; `Persona.Successor` and the extreme-act removal are still to do with the Main track (phase 8).
- [ ] **Main questline "The Seal"** (design in `main-quest.md`): short path unchanged (take the Amulet, leave, win); long path = Prologue, Act I ledger, Act II three truths (Mines, Vaults, Spire), Act III choice with six endings (Drifter's Pay, The Pit Shut, The Archive Opens, The Warden, The Auction, The Stamp). Needs phases 2, 3 and 8 above plus `Quests/Main.cs`.
- Decided 2026-10-03: quests fail by time only when their def sets a `Deadline`; attacked NPCs turn hostile and the Watch answers; essential NPCs are near-immortal; dialogue is authored in C#; hostility lasts while seen, bounty persists, name cleared by gold, favour or jail time; essential-NPC fallbacks designed (see `living-world.md`, *Decisions*).
- Decided 2026-10-03 (cont.): sentences scale with the harm done (damage, death, value stolen or destroyed); bounty reaches only neighbouring regions; new game plus is in; the main-quest truths change the world; every meaningful action must leave a visible, remembered mark.
- [~] **WorldLedger** (`Game.Ledger.cs`, `living-world.md` pillar 9): append-only record of deeds (killed, spared, stole, saved, failed, freed, burnt…) read by NPC memory, prices, town state, bounty, rumours, epilogue and morgue; one visible consequence per deed type, with a test each. Pure function of seed + keys (replay saves). **Partial 2026-10-03**: ledger + consumers so far: NPC lines, grief lines for murdered towns, quests, bounty, journal; prices, town state, rumours about the hero and epilogue still to wire.
- [x] **Neighbour-region lookup + per-region bounty store** (`Overworld.RegionAt` exists, neighbours do not). **Done 2026-10-03**: `Game.NeighbourRegions` + `Game.Bounties` (`BountyHere` = own or half of a neighbour's).
- [~] **New game plus** after The Stamp: carry-over (legend, truths, name) and reset rules. **Partial 2026-10-03**: `Game.StartNewCycle` keeps level, items, truths, the ledger and a `legend` flag, drops the Amulet, clears bounties and the Seal quest, and sends the hero to the overworld; the NPCs do not yet react to the legend.
- [ ] Still open: exact fine formulas and `k`; how far neighbouring bounties reach in strength; new-game-plus carry-over details.

## Technical and quality

- [ ] Headless test coverage for each new item (project standard) and `dump panels` for layout. *(Ongoing: every item above came with a test; `dump panels` shows Runs, road event and morgue.)*
- [ ] Keep `Loc.cs` (EN→PT) up to date with every new text; see `languages.md`.

---

## Done

- [x] **Synthesised sound and music** (2026-10-03). `desktop/src/audio.ts`: effects (now with stairs, doors and pickups), menu blips and looping music built only from Web Audio oscillators and noise
  (the theme "Phosphor & Bone" plus dungeon, road and town tracks), through one reverb; volumes follow the Audio settings. Engine cues `stairs`, `door`, `pickup`. Tests in `audio.test.ts` and `SoundCues`.
  Left: boss/death/victory music, detuning with depth, a recorded score.

- [x] **Dungeon level 1 always has a way out** (2026-10-03). `Dungeon.Ensure` stripped the stairs up from depth 1 of *every* branch, so entering a dungeon from the overworld
  at depth 1 left the hero with no exit. Now only side branches (the Annex, left by its portal) lose them. Left: a regression test (`Dungeon.Ensure` depth 1 has `StairsUp`).

_(move here, with a date, whatever is completed)_

- [x] **Documentation converted to English** (2026-10-03). Every doc under `docs/` and `AGENTS.md` is now English, with English file names
  (`overview`, `architecture`, `systems`, `controls`, `languages`, `todo`, `build-and-test`, `spells-and-items`). Root README, CONTRIBUTING, CHANGELOG and INSTALL link to the new names;
  the `*.pt-BR.md` variants stay as Portuguese translations of the root files.

- [x] **Magic items imbued with spells + more variety** (2026-10-03). Details in [`spells-and-items.md`](spells-and-items.md#magic-items-with-spells-magicspellfitcs).
  - `SpellFit` filters spells by item type (sword: attack and control; armor: protection; boots: movement; helm: senses; cloak: stealth; ring/amulet: what you cast on yourself);
    rare (55%) or magic (18%) equipment comes imbued (`Item.Imbue`, name "long sword of Frostbite"); *blank* rings and amulets only exist imbued. The spell is lent
    while the item is in use; weapons fire by themselves on a hit (14%) and worn pieces answer a blow (10%), for free and without spending a turn.
  - **+60 spells** (322 in total: 10 in Evocation, 6 in each of Conjuration, Alteration and Illusion, 8 in each of the other four), 13 buffs and 5 new creatures, **+16 books** (55), PT translation.
  - **+48 base items** (11 weapons, 5 armors, 4 helms, 4 gloves, 3 boots, 3 cloaks, 3 shields, 9 rings, 6 amulets), 4 blank amulets and **+22 affixes**.
  - Fixed: the old amulets were *rings* in the catalog (the real amulet, including life saving, never worked); they are now `ItemKind.Amulet`.

- [x] **Spells, books, magic items, uniques and animations** (2026-10-03). Details in [`spells-and-items.md`](spells-and-items.md). Saves move to **version 11**.
  - **Spell animations** (`Fx.cs`, `Game.Fx.cs`, `desktop/src/fx.ts`): the engine records an `FxTimeline` (23 pieces: projectile with trail, ray, jagged lightning, chain,
    cone, explosion, nova, rain, eruption, pillar, meteor, particle swarm, teleport…) that goes in `Frame.fx` and is played by the frontend over the frame, without asking for a turn.
    Geometry shared with damage (`Shapes`), per-element colors going through the theme, no Rng. Wands, scrolls, potions, bow shots, molotovs,
    traps, explosions and boss attacks animate too. `headless.ps1 fx <spell>` shows the result in ASCII.
  - **262 spells in 8 schools** (were 39 in 6): 220 new ones as **recipes** (`SpellDef` with damage, riders, buffs, summons, surfaces, push, drain, chain, random hits,
    specials) in `Game.Magic.Recipes.cs`. New schools **Nature** (Ranger) and **Shadow** (Rogue); Necromancer, Cleric/Paladin and Wizard gain ~40 each.
    New monster states (held, damage over time, vulnerable), 47 buffs (`SpellBuffs`), 21 summonable creatures, **cone** targeting.
  - **39 books** across 5 depth tiers (loot and bookshop respect them), spell panel with **school tabs**, PT translation of every spell, buff and rider.
  - **Items**: 26 weapons, 14 armors, 30 helm/glove/boot/cloak pieces, 6 shields, 24 rings and 12 amulets new (`ItemEffects`: numbers per base); rings and amulets now **work**
    (and amulets can be worn); 33 new affixes; 38 wands, 34 scrolls and 18 potions that cast real spells; depth-based loot (`PickDeep`).
  - **43 new unique items** in every branch, with a per-level chance, 38 that **lend spells** while in use, and 4 new sets.
  - Fixed: `RunMonsters` crashed if a monster died inside its own effects turn. `ArsenalTests` (14) and `fx.test.ts`.

- [x] **Animated water and square tiles** (2026-10-02). *Water*: the Core marks visible water cells (`TextBuilder.Shimmer` → `Frame.Anim`, empty under panels) and the frontend
  (`desktop/src/anim.ts`) alternates `≈ ~ ≈ -` and the brightness every ~380 ms between frames, without asking the engine for a turn and respecting `prefers-reduced-motion`.
  *Square tiles*: **Tiles** option in the menu (`DisplaySettings.Square`, `square` field on all three sides of the protocol, saved in localStorage): each map/overworld cell uses two
  columns (scenery repeats the glyph, things stay on the left); camera and cursors count in cells. Tests `WaterAnimates`, `SquareTiles`, `anim.test.ts`.

- [x] **PC-speaker style sound effects** (2026-10-02). The Core only **names** what happened (`Game.Sound.cs`: a cue per `MessageKind` in `Say`, plus `levelup` and `magic`; `DrainCues` delivers
  at most 3, strongest first); `Frame.Sounds` carries the names and `desktop/src/audio.ts` plays short square waves (WebAudio), with the *Master × Effects* volume from the menu.
  Replays and the title stay silent. Tests `SoundCues` (C#) and `audio.test.ts` (vitest). Music is still unwritten.

- [x] **XP spent on skills (Trained mode)** (2026-10-02). A new `Difficulty` value: in it use no longer teaches (`Player.GainSkill` ignores), every experience gained also becomes `Player.TrainXp`
  and `Shift+N` opens the skill list (`Game.Training.cs`): +5 points per `20 + current value` XP, respecting the class cap. The sheet shows "XP to spend". Test `TrainedMode`.

- [x] **The Annex: optional challenge branch** (2026-10-02). A **portal** (`^`, `TileKind.Portal`) on *Dungeons 4* leads (`>`) to the Annex: 3 floors (Catacombs → Maze → Fort) with monsters and loot
  from six floors deeper (`LevelBuilder` uses effective depth `depth+6`). You arrive on top of the exit portal (`<` or `>` returns to the same spot). At the bottom: the boss **Annex Warden**
  (collects debts in blood + corruption; phase 2 raises skeletons) and the artifact **Tithe-Collector's Mantle**. `Branch.Parent/ParentDepth`, `Dungeon.SideBranchAt`, `Game.UsePortal`.
  Achievements *Past the Portal* and *Debts Paid*. Test `AnnexFlow`.

- [x] **Every branch is now reachable** (2026-10-02). Before, every overworld dungeon entrance led to *The Dungeons*; Mines, Warrens, Vaults and Spire only existed
  in tests. Now `OverworldGen.BranchForRegion` ties each region to a branch (Ashen Marches/Emberdown → Spire, Sunken Vale → Vaults, Iron Hills/Craglands → Mines,
  Whisperfen → Warrens, the rest → Dungeons, where the Amulet is) and the entrance's name says which (`...Spire/Vaults/Delve/Warren`). `EnterDungeonFromOverworld` enters the right
  branch (lower starting depth for the side ones). It gives meaning to bosses, artifacts, native monsters and per-branch contracts. Test `EntrancesReachBranches`.

- [x] **Vaults, trapped caches and two new level styles** (2026-10-02). `Gen/Vaults.cs` carves a 4×3 chamber in unused rock (never changes what the stairs reach):
  **locked vault** (locked door, gold + 3 rich loot items; the **brass key** — `Crafted.BrassKey` — is held by a monster on the level, opens any locked door once) and
  **hidden cache** (secret door, floor trapped ~45%, loot at the back). `LevelBuilder.PlaceVaults`. **Ruins** (collapsed rooms: breaches, rubble, columns) and
  **Catacombs** (grid of passages with crypts and tombs) styles used in the branches (`Dungeon.BuildBranches`). Tests `VaultsAndKeys`, `LevelRuins`, `LevelCatacombs`.

- [x] **Living world: reputation, contracts, road events and routine** (2026-10-02). Saves move to **version 10**.
  - **Reputation** (`Game.Reputation.cs`): four houses — the Watch, the Temple, the Guild and the Cult of the Drowned — from −100 to 100 (*revered/trusted/known/distrusted/hated*).
    Effects: shop prices (Guild, 80–120%), temple fees (Temple), inn (Watch: ≥25 cheaper; ≤−25 refuses), and the Cult sells *grave-water* (mutation potion)
    to anyone with ≥25. Sources: offerings/healing/purge (Temple), services (Guild/Watch +10), burying/looting corpses, sacrifices (Cult), swearing to Nhal, defeating bandits.
  - **Contracts** (`Game.Contracts.cs`): the Guild board shows 3 offers per town and week (function of town name + `World.Day/7`): *Hunt N monsters in a branch* or *Reach depth N*.
    Up to 3 active; progress counts by itself (`ContractKill`, `ContractDepth`); turning in at the board pays gold and +10 reputation. Achievement *Hired Hand*.
  - **Road events** (`Game.Events.cs`): 7% per free step — camp, caravan, ruin, shrine, toll, body — with choices and prices, reusing the service panel.
  - **Routine and reaction**: at night residents go home and stay there (`GoHomeAtNight`, no Rng); guards, priests and merchants speak differently depending on reputation,
    priests notice corruption and everyone notices mutations (`TownText.Reaction`). The Character sheet shows reputation and active services.
  - Tests `ReputationAndJobs`, `RoadEvents`, `TownRoutine`.

- [x] **Monster factions** (2026-10-02). `Game.Factions.cs`: *Greenskin* (kobold, orc…) ↔ *Deepfolk* (dwarf, gnome, hobbit) and *Dead* (undead) ↔ *Wild* (animals)
  fight on sight (`FightRival`, up to 6 cells, only when the hero is not the nearest target). Allies, citizens, companions and bosses take no side.
  Test `FactionWar`.

- [x] **Bosses with mechanics** (2026-10-02). `Game.Bosses.cs`: one boss at the bottom of each branch (`Bosses.All`; generated on first visit with a private Rng, far from the stairs,
  with an entrance line). **Gaoler** (Dungeons 10: a chain that pulls the hero; below 50% calls hounds), **Stone Warden** (Mines 8: a ground slam that stuns;
  faster), **Rat King** (Warrens 9: a rat cut, up to 6; phase 2 with plague rats), **Drowned King** (Vaults 12: floods the floor and shocks anyone in the
  water), **Ashen Regent** (Spire 15: a fire blast around; phase 2 calls wisps). Phase 2 below 50% health, own line; on death pays gold + 2 potions;
  achievements *Boss Slayer* and *Kingslayer*. Test `BossFights`.

- [x] **Per-branch bestiary with own habits** (2026-10-02). `MonsterDef.Branch/Trait` and `Game.Traits.cs`: 9 native monsters — *gaol hound* (Dungeons, hunts in
  a pack: +4 speed with a companion), *cave bat* (Mines, erratic flight), *ore golem* (Mines, a stunning slam), *plague rat* (Warrens, a bite that
  rots/poisons), *rat swarm* (Warrens, multiplies up to 8), *drowned dead* and *tide wraith* (Vaults, heal in water), *ember wisp* (Spire, explodes into fire)
  and *ash wraith* (Spire, ignites). `SpawnTable(depth, rng, branch)` filters by branch. Test `BranchMonsters`.
  **Old bug fix:** `TrapTable`/`GroundItems` are static per map number and leaked between games of the same seed (the 1st bot played differently from the later ones);
  `LevelBuilder.Populate` now clears them when generating the level.

- [x] **Mourne, rivalries, sacrifice and god trials** (2026-10-02). Sixth god: **Mourne, the Weeping Seam** (flesh and change: likes mutations,
  hates purging; boon = an always-good mutation; piety 50 → more good ones, 100 → poison 30%). Each god has a **rival** (`GodDef.Rival`: Aurel↔Nhal,
  Khorr↔Sylk, Veyra↔Mourne): switching to the rival costs double and, at the rival's altar, *Defile the altar* gives +8 piety (40% curse
  and +5 corruption) and leaves the altar dead. New altar lines: **Sacrifice a corpse** (value by level; Nhal ×2, Aurel punishes) and **Trial**
  (5 deeds the god likes → a one-time boon: attribute, +6 HP, mutation). Test `GodsExpanded`.

- [x] **Seven new spells (corruption and surfaces)** (2026-10-02). *Ice Lance*, **Steam Burst** (boils the water: more damage on the wet and the puddle vanishes),
  **Create Oil** (ignites with fire), **Ossify** (AC +4, +2 corruption), **Reshape Flesh** (one mutation per 10 corruption), **Marrow Bolt** (necrotic, +2 corruption) and
  **Purify** (−15 corruption). They entered the evocation, conjuration, dead and mercy books. Total: 39 spells. Test `NewSpells`.
  Future ideas: a new school, ice × lightning.

- [x] **More artifacts, sets and corruption relics** (2026-10-02). `Artifacts.All` went from 5 to 13 (one per level in each branch). **Sets**
  (`ArtifactSets`): *The Drowned Court* (Crown, Tidecaller's Gauntlets, Brinewalkers) and *The Ashen Regalia* (Ashfall, Mantle of Ash, Cinder Plate); 2 pieces =
  1st bonus, 3 = 2nd (summed in `Player.Gear`). **Relics** (`Corrupts`): *Hollow Ribs*, *Gravedigger's Spade*, *Gnawed Cowl*: strong, but each
  one worn gives +1 corruption every 25 turns (`Game.WornRelics`). Shown on the Character sheet. Test `SetsAndRelics`.

- [x] **Light crafting** (2026-10-02). `Shift+B` (`Game.Crafting.cs`, `Items/Crafted.cs`): recipes as data — **molotov** (oil potion + candle),
  **bone blade** (blade + remains; comes *vampiric*), **bone-studded armour** (light armor + 2 remains, +1) and **extra healing** (2 healings).
  The molotov is applied with `a` and thrown (new `TargetingMode.Throw`, range 7): fire on the target and the 4 neighbors. Defs outside the loot tables.
  Test `CraftingFlow`.

- [x] **Challenges: Dive and Naked** (2026-10-02). New `Difficulty` values chosen at creation: **Dive** (starts on level 5, level 4, 2 potions, `ApplyChallenge`)
  and **Naked** (no weapon/armor/shield, +1 advance). Points ×2 (`Difficulties.ScoreFactor`). Test `Challenges`. Future idea: Pacifist.
- [x] **Travel to altar/fountain** (2026-10-02). `Shift+Backquote` (`~`): `Game.FeatureStep`/`IsFeatureSpot` (fountain or floor beside a remembered altar), reusing `AutoStep`.
  Test `FeatureTravel`. Still no player-marked points.

- [x] **Permanent companions** (2026-10-02). `Game.Companions.cs`: the tavern hires a mercenary (*sellsword / shield-bearer / cutthroat*,
  `100 + 40×level` gold); an ally with no timer that crosses every stair, levels up with the hero (keeping the health fraction) and,
  if it falls, is gone (`ReapCompanions`). Shown on the Character sheet. Test `Companions`. Pending: inventory/orders (stay/follow), more than one
  companion, archers.

- [x] **Mutations and the Ossuary's Corruption** (2026-10-02). `Entities/Mutations.cs` (16 mutations: good, mixed and bad; numbers in `ItemMods` summed
  in `Player.Gear` + sight/hunger/noise) and `Game.Corruption.cs`: `Player.Corruption` 0–100, **one mutation every 20 points**. Sources: **Shift+E**
  at a dungeon fountain (clean, bitter or tainted), the Amulet in the pack (+1/40 turns), level 4+ necromancy (+1/cast up to 40) and the new
  *potion of mutation*. The temple sells *Purge the Ossuary from me* (−30, removes the worst, newest mutation). Shown on the Character sheet, in the
  morgue and in the *Mutant* achievement. Test `CorruptionMutations`. Pending: relics that give a mutation in exchange for power (Items).

- [x] **Stealth and noise** (2026-10-02). `Game.Stealth.cs`: the radius at which a monster notices you is `Vision − Stealth/25 − light-feet×2 − Sylk + noise`.
  Action noise: fight +3, spell/wand/fire/door +2, heavy armor when walking +1/+2 (chain/splint, plate), standing still or searching −2.
  Stealth rises by passing unnoticed. No Rng. Saves moved to **version 9** (old saves stop loading). Test `StealthNoise`.

- [x] **Found traps and disarming** (2026-10-02). `Game.Traps.cs` + `TrapTable.Reveal`: before, a "found" trap was not marked.
  Now `s` and passive perception (`SenseTraps`: Search/2 + 25 for the Rogue, by position hash, no Rng) reveal them; found ones show as `^`,
  auto-explore avoids them and `Shift+A` disarms them (35 + Dex×2 + Search/2, +30 Rogue; failure may set it off). Test `TrapsFlow`.

- [x] **Local achievements** (2026-10-02). `Core/Achievements.cs`: 18 achievements as pure functions of state (`Game.CheckAchievements`,
  every turn and on victory); the announcement goes straight to the log without touching `Game.Said` (does not alter auto-walk/replay). The host stores
  `achievements.json` (id → date) and the menu has the *Achievements* panel. Tests `AchievementsEarned` and `AchievementsFlow`.
  Future idea: more achievements (per class, per god, per branch).

- [x] **Daily seed with a local leaderboard** (2026-10-02). `Core/Daily.cs` (FNV-1a seed of the UTC date + hero fixed by the seed), *Daily challenge*
  button on the title (`op:new, daily:true`; a new field on all three sides of the protocol), `Session.NewDaily`. The run records
  `RunRecord.Daily`; in *Past runs* the `D` key shows the daily leaderboard (best score first). Test `DailyFlow`.

- [x] **Difficulty modes + save-and-quit** (2026-10-02). `Core/Difficulty.cs`: Normal, **Classic** (no hunger) and **Hardcore**
  (a single save, written on exit through the menu and erased on resume; no quicksave). Chosen on the creation screen (◄► on the
  confirmation step), goes in `SaveData.Difficulty` and in the run record (points ×1.5 / ×0.67). Test `DifficultyFlow`.

- [x] **Graveyard / Bones** (2026-10-02). `Core/Bones.cs`: whoever dies from dungeon level 2 on leaves `Bones` (name, class,
  cause, equipment) in `bones.json` (one per level, max 100). In future runs, when that level is generated for the first time,
  60% chance of a *shade of <name>* (Unique, undead, scales with the hero's level, guards the tomb with the equipment).
  Own Rng: the seed and the world Rng do not change. The save carries the graveyard from the start of the run (`SaveData.Bones`) for the replay.
  Killing the shade erases the bones. Tests `BonesShades` and `BonesFlow`.

- [x] **Run history** (2026-10-02). Menu (`Esc`/`F2`) → *Past runs* (`Panel.Runs`, `Ui.DrawRunsPanel`, `Session.RunsKey`):
  lists finished runs, newest first, with cause, class, depth and points. Data read from `history.json`.

- [x] **Morgue file** (2026-10-02). On death, victory or abandoning: `Core/Morgue.cs` (`RunRecord`, `Summarize`, `Text`) and
  `Game.Death.cs` (`DeathCause`, `HurtBy`); the host writes `morgue/<date>-<name>.txt` and appends to `history.json`
  (`SaveStore.WriteRun`, max 200). The death screen shows the cause. Basis of the run history and the Graveyard.

- [x] **Auto-explore** (`t`), **travel to the stairs** (`` ` ``) and **rest until healed** (`Shift+S`) (2026-10-02).
  `Game.Explore.cs` (BFS over seen cells, `ExploreGoal`, `StairsStep`) and `Commands.DoAutoWalk`: loops of
  ordinary turns, so the log replay stays exact. They stop on seeing a hostile, taking damage, any message, items
  or stairs underfoot; refusals spend no turn. Tests in `Tests/FeatureTests.cs` and `DesktopTests`.
  Pending: travel to altar/fountain/marked point (stayed under Controls and QoL).

- [x] **Holding a direction key repeats the move** (2026-10-02). `Game.Repeat.cs` (`CanKeepWalking`,
  `HostileInView`), `Session.KeyRepeat`, `repeat` flag in the protocol/`main.ts`. Stops by itself on a hostile in view,
  item/stairs/altar/fountain/door underfoot, damage, a new message or a wall. Test `HeldKeyWalking`.
