# To do — Ossuary

What is left to build, by topic. Every session reads this file before starting and updates it when done (see `AGENTS.md`).
How a system works lives in [`design/systems.md`](design/systems.md); what shipped in each version lives in the [changelog](../CHANGELOG.md).

## How to use

- **One line per item**, always in this form: `- [ ] **Name**: what it is, and why when it is not obvious.`
- **States**: `[ ]` to do, `[~]` started (write `has …; left …`). A finished item leaves its topic and goes to *Done* as
  `- YYYY-MM-DD · **Name**: what shipped (`File.cs`).`
- **Order** inside a topic is priority, top first. A new idea goes in its topic, in one line.
- **Rules for every item**: simulation only in the Core (`Game.<Subject>.cs`, screens as `TextBuilder`); generated content is a pure
  function of the seed (or `hash(x, y)` for looks), never extra simulation RNG; a rule change bumps the save format and gets a headless
  test; every new string gets its Portuguese in `Loc`; a system the player cannot see in the log, a look or the morgue is not worth it.
- Design references: Dwarf Fortress, Caves of Qud, DCSS, Brogue, Cogmind, Sil, NetHack, Angband.

## At a glance

| Topic | Where it stands | Next |
|---|---|---|
| [Combat and bodies](#combat-and-bodies) | wounds, called shots, severed parts, bandages, martial techniques | limbs as trophies, burns |
| [Magic](#magic) | 322 spells, 55 books, animations, casting monsters and a boss that casts, ice × lightning, hits on arrival, element sounds | the Magister's balance, from real play |
| [Items and materials](#items-and-materials) | materials, wear, relics with biographies and deeds, identify by use, services, examine, ammunition | done; new ideas go in the topic |
| [Crafting and trades](#crafting-and-trades) | 16 trades, ~105 recipes, forge, loom and still, fletching, named masterworks, crafters, archers | done; new ideas go in the topic |
| [Economy and market](#economy-and-market) | glut, caravans you meet on the road, haggling, traders' memory, counter, tolls, town smiths' work | done; new ideas go in the topic |
| [Towns and people](#towns-and-people) | personas, dialogue, quests, guild jobs, crime, rumours, essential people and their apprentices | done; new ideas go in the topic |
| [World and history](#world-and-history) | overworld, seven branches, the chronicle with houses and kings, legends, blood and traces, engravings, rooms, tombs, raids | done; new ideas go in the topic |
| [Creatures](#creatures) | factions, bosses, branch natives | named survivors, ecology |
| [The hero](#the-hero) | corruption, modes, achievements, bones | thoughts and mood |
| [Tutorials](#tutorials) | nothing yet | first-time hints |
| [Interface and controls](#interface-and-controls) | auto-explore, travel, rebinding, travel marks | done; new ideas go in the topic |
| [Audio](#audio) | 18 tracks, 15 stingers, effects | the missing boss tracks |
| [Technical and quality](#technical-and-quality) | headless suites, soak, balance bot | a language gate worth reading |

**Next up**: tutorials, then named survivors and mood, then creatures' ecology.

---

## Combat and bodies

- [ ] **Severed parts as things**: a cut-off limb lies on the floor as an item (a trophy, butchery, a quest token); blood stays in *World*.
- [ ] **Burns as wounds**: fire and acid get their own words (scorched, charred) and scars, instead of counting as blunt blows.
- [ ] **Techniques for the other martial roles**: a rogue's and a ranger's manual, and techniques bought from the Guild's masters.

## Magic

Plan and rationale in [`roadmap/magic.md`](roadmap/magic.md).

- [ ] **Sallow Magister balance**: the dive bot meets it on its path and dies to it about a quarter of the time (`headless balance 12 1500 dive`); tune it from real play, not from the bot alone.

## Items and materials

Nothing open: every item is in *Done*.

## Crafting and trades

Nothing open: every item is in *Done*.

## Economy and market

Nothing open: every item is in *Done*.

## Towns and people

Plan and rationale in [`game/living-world.md`](game/living-world.md).

Nothing open: every item is in *Done*.

## World and history

Nothing open: every item is in *Done*.

## Creatures

- [ ] **Named survivors**: a monster that survives the hero gets a name, remembers, levels up and earns titles ("Grak, Biter of Heroes").
- [ ] **They come back**: as rumours, road ambushes, rivals deeper down; killing one is a deed with its own epilogue line.
- [ ] **Ecology**: lairs, sleep, hunting and eating corpses, factions fighting without the hero.
- [ ] **Detailed look**: build, scars, missing parts, gear and its material, mood and age ("a lean kobold with a notched ear, gripping a chipped bronze spear").

## The hero

- [ ] **Thoughts** (`Game.Mood.cs`): "saw a companion die", "slept in a fine bed", "ate raw meat", each with a weight and a duration.
- [ ] **Mood effects**: small changes to XP, prices and lines; a breakdown at the bottom; companions with their own thoughts.
- [ ] **Pacifist challenge** and more achievements (per class, per god, per branch).

## Tutorials

The game is big: each system should explain itself once, the first time, and never again.

- [ ] **First-time hints** (`Game.Tutorial.cs`): a short guided popup the first time the hero crafts, casts, enters a dungeon, reaches a town,
  shops, gathers, levels up, is wounded, finds an altar or takes a job; it shows the keys step by step and closes when the step is done.
- [ ] **Seen once per install**: shown hints are kept in the settings, so a new hero is not taught twice.
- [ ] **Skip them**: *Tutorials on/off* in F2, asked at the first launch ("Have you played before?"), plus *reset tutorials*.
- [ ] **UI as data**: the hint is a `TextBuilder` panel from the Core, English with Portuguese in `Loc`.
- [ ] **Tests**: each hint fires once, never when off, and changes neither the turn nor the replay.

## Interface and controls

Nothing open: every item is in *Done*.

## Audio

- [ ] **Missing tracks**: the Watch, the rival party, the endings, and boss music for the Rat King and the Drowned King.
- [ ] **Depth detuning**: the dungeon music sours as you go down.
- [ ] **Unused stingers**: wire the trap, ending and new-cycle stingers.

## Technical and quality

- [ ] **Balance bot with every spell**: it knows ~40 of the 322 (see [`design/balance.md`](design/balance.md)).
- [ ] **A language gate worth reading**: the `loc` soak counts fragments of composed lines and stops at depth 1; the `Tables` test is today's gate.
- [ ] **Regression test**: dungeon level 1 always has stairs up.
- [ ] **Every item ships with a test** and a `dump panels` check, and `Loc` keeps up with every new text (ongoing).

---

## Done

One line per feature, newest first inside each topic. Details: the [changelog](../CHANGELOG.md) and [`design/systems.md`](design/systems.md).

**Combat and bodies**
- 2026-10-06 · **Martial techniques**: the Fighter's manual of strikes teaches Hamstring, Disarming Blow, Skull Crack and Lunge by level, paid with Vigor in the abilities panel, animated (`Abilities.cs`, `Game.Abilities.cs`).
- 2026-10-06 · **More wounds**: monsters wound each other, force/frost/lightning/fire spells wound, snakes with a serpent body (`Game.Wounds.cs`, `Combat/Body.cs`, `Bestiary.cs`).
- 2026-10-06 · **Called shots and severed parts**: Shift+F aims at a part; edges sever monster limbs; a mangled arm drops its weapon; wings ground fliers (`Game.Wounds.cs`).
- 2026-10-06 · **Wounded monsters**: a crippled monster flees once, a blind one swings at random squares (`Game.Wounds.cs`).
- 2026-10-06 · **Scars that count**: a face scar costs 1 Cha, scars add intimidation, townsfolk notice them (`Game.Wounds.cs`, `Game.Dialogue.cs`).
- 2026-10-06 · **Treatment and bleeding**: bandages (start kit, temple, tailor), fire cauterises, road time mends (`Game.Wounds.cs`); blood on the floor moved to *World*.
- 2026-10-05 · **Deep piercing wounds**: daggers, spears, tridents, stings and missiles wound a step deeper on a hard blow (`AttackResult.Deep`).
- 2026-10-05 · **Bodies and wounds**: body plans, hit location, four severities, limping, aim, stun, sight, bleeding, mending, scars (`Combat/Body.cs`, `Game.Wounds.cs`).
- 2026-10-02 · **Stealth and noise**: notice radius from stealth, light feet and noise (`Game.Stealth.cs`).
- 2026-10-02 · **Found traps and disarming**: search and passive perception reveal traps, `Shift+A` disarms (`Game.Traps.cs`).

**Magic**
- 2026-10-08 · **Casting monsters**: orc shamans, dark acolytes and sorcerers cast real recipe spells at the hero in sight, animated from their own cell; the Sallow Magister (The Dungeons, depth 7) casts in both phases (`Game.Casters.cs`, `Magic/MonsterSpells.cs`, `Game.Bosses.cs`).
- 2026-10-08 · **Ice and arcing lightning**: lightning runs along connected ice and arcs from one wet creature to the next (`Game.Surfaces.cs`).
- 2026-10-08 · **Hits on arrival**: a bolt's damage, death, log and map change when it lands; the previous frame holds until then (`Fx.cs`, `Session.cs`, `fx.ts`, `main.ts`).
- 2026-10-08 · **Element sounds and skip**: one cast cue per element, outcome sounds held for the hit; Escape skips a running animation (`Game.Sound.cs`, `audio.ts`, `main.ts`).
- 2026-10-03 · **322 spells, 55 books, animations**: eight schools, recipes as data, every spell animated (`Game.Magic.Recipes.cs`, `Fx.cs`).
- 2026-10-02 · **Corruption and surface spells**: Steam Burst, Create Oil, Ossify, Reshape Flesh, Marrow Bolt, Purify, Ice Lance.
- 2026-10-02 · **Mourne, rival gods, sacrifice and trials** (`Game.Gods.cs`).

**Economy and market**
- 2026-10-07 · **Caravans on the road**: the week's caravan met on the road; escort it and the town's goods arrive and it pays, rob it and the town goes short and the Watch wants you (`Game.Caravans.cs`).
- 2026-10-07 · **A haggle verb**: `o` at a counter offers 90, 75 or 60 percent; Cha, mood, Guild and memory answer; a refusal sours the trader for the day (`Game.Haggle.cs`).
- 2026-10-07 · **Merchants' memory**: traders read the ledger for being flooded, cheated with cursed or ruined goods, or haggled well; it shows in their greeting and mood (`Game.Haggle.cs`).
- 2026-10-07 · **Town smiths' work**: every smithy and armoury sells a piece its smith made and signed, smithies sell ammunition by the bundle, ammunition trades by the stack (`Town.cs`, `Game.Market.cs`).
- 2026-10-06 · **Town markets**: supply and demand by goods class with a glut that recovers over days, town tastes, weekly caravans and raided roads, market-day traders on the stalls, a weekly order board, a rented counter that sells while you are away, haggling by Cha, mood, Guild and a master's name, gate tolls, rent and Guild dues, price history in the journal (`Game.Market.cs`, `Loc.Market.cs`).

**Items and materials**
- 2026-10-07 · **Identify by use**: potions, scrolls and wands look like something (by seed) until drunk, read, zapped, bought, appraised or identified; the discoveries panel counts what is known (`Items/Appearances.cs`, `Game.Identify.cs`).
- 2026-10-07 · **Shop services**: sages recharge wands (each recharge after the first may blow it apart), priests lift curses, three cursed amulets that will not come off, three more corrupting relics (`Game.Services.cs`, `Artifacts.More.cs`).
- 2026-10-07 · **Relic biographies**: every unique has a story from the chronicle: forged by whom, of what, for whom, who carried it, how it was lost; libraries read the chronicles aloud (`World/History.cs`).
- 2026-10-07 · **Deeds written into relics**: a boss or unique killed with relics in hand or on writes a line into each; the morgue prints every relic's story (`Game.Ammo.cs`, `Morgue.cs`).
- 2026-10-07 · **Item descriptions**: Shift+I and `l` on a single floor item show numbers, material, maker, quality, wear, worth, previous owners and deeds (`Game.Ammo.cs`).
- 2026-10-07 · **Ammunition**: arrows, bolts and sling stones loosed by the matching launcher, spent one a shot and picked up again, metal heads that bane, stacks that merge (`Items/Ammo.cs`, `Game.Ammo.cs`).
- 2026-10-05 · **Materials**: eleven materials by depth and branch, banes, wear and shattering, repair and recasting from ore (`Items/Materials.cs`, `Game.Materials.cs`).
- 2026-10-03 · **Items with a spell inside**: gear imbued by kind, +48 bases, +22 affixes (`Magic/SpellFit.cs`).
- 2026-10-03 · **43 uniques and 4 sets** across every branch, many lending a spell.
- 2026-10-02 · **Artifacts, sets and corrupting relics** (`Items/Affixes.cs`, `Game.Corruption.cs`).
- 2026-10-02 · **Vaults and caches**: locked vaults with a brass key, trapped hidden caches (`Gen/Vaults.cs`).

**Crafting and trades**
- 2026-10-07 · **Loom and still as tiles**: weaving and cloaks at a loom (general stores), ale and mead at a still (taverns, alchemists); every smithy also has a forge on the customer's side (`Tile.cs`, `Town.cs`).
- 2026-10-07 · **Crafters elsewhere**: each week the smith, armourer, tailor and alchemist set out new signed work, and on market days the rival party sells arrows and what it brought up (`Game.Makers.cs`).
- 2026-10-07 · **Monster archers**: kobolds, gnomes and orcs with slings, bows and crossbows shoot from range with real ammunition that lands at your feet and drops with them (`Game.Ammo.cs`).
- 2026-10-07 · **A quiver slot**: Shift+Y picks which stack to loose first (`Game.Ammo.cs`).
- 2026-10-07 · **Named works in the world**: a sold named work is bought by a townsperson after a few days, they carry it, and the taverns talk about it and its maker (`Game.Works.cs`).
- 2026-10-07 · **Arrows as ammunition**: fletching anywhere, metal-headed arrows and bolts at the forge, sling stones from rocks; rangers and rogues start with a launcher (`Items/Trades.cs`, `Game.Ammo.cs`).
- 2026-10-07 · **Named masterworks**: a master's masterwork sometimes gets a name and the hero as its maker, becomes a relic worth far more, and enters the morgue (`Game.Crafting.cs`, `Items/Masterworks.cs`).
- 2026-10-05 · **Crafting for everyone**: 16 trades, ~100 recipes, forge and workshops, quality by rank, recipe book, gathering, music, masters, commissions (`Items/Trades.cs`, `Game.Crafting.cs`, `Game.Gathering.cs`, `Game.Workshops.cs`).
- 2026-10-02 · **Light crafting**: molotov, bone blade, bone armour, extra healing (`Items/Crafted.cs`).

**Towns and people**
- 2026-10-08 · **Crime figures and new game plus**: the fine is the figure the Watch holds here (the region's own bounty, or half of a touching region's, rounded up), and settling it forgives the neighbours' bounties too; the cells are one day per hundred gold (one to thirty), a bribe is three halves of it under 500, the escape doubles it, and a bounty stops at 5000; the Stamp keeps gold, gear, House standing, truths, ledger and companions, and clears the bounties, the Amulet and an open seal errand, which the Elder does not offer again (`Game.Crime.cs`, `Game.Theft.cs`, `Game.Main.cs`, `Dialogues.cs`, `living-world.md`, `main-quest.md`).
- 2026-10-08 · **Companions**: two companions may walk with the hero; `F12` orders them to hold or follow; an archer (the fourth role) loses arrows from its own bow at foes two to six cells off; a companion who falls drops their pack where they died (`Game.Companions.cs`, `Game.Ammo.cs`, `Game.Magic.Effects.cs`).
- 2026-10-08 · **Theft and burning**: `F9` steals from a counter you stand beside and `F10` burns it with a molotov; witnessed, each is a bounty by its value and the Watch's memory, and a burnt shop stays burnt; a lock pick from the starting kit opens the cell when the Watch takes the hero in (`Game.Theft.cs`, `Dialogues.cs` *Arrest*, `KeyBindings.cs`, `Commands.cs`).
- 2026-10-08 · **Main quest polish**: the ceremony is spoken by the house's holder, by name (Priest, the Drowned's voice, Captain, Elder, Scholar), or at the altar when that post is empty; the Archivist's Shade (The Sunken Vaults, level 10) and the Guardian of the Deep (The Ashen Spire, level 12) are bosses that stay until they fall; the morgue names the ending (`Game.Main.cs`, `Morgue.cs`, `Game.Bosses.cs`).
- 2026-10-08 · **New game plus legend**: in a new cycle the townspeople speak of the stamp and the descent in talk and at the counters; a killing in the town before the stamp is not forgiven (`Game.Town.cs`, `LegendLine`).
- 2026-10-08 · **Cult errands**: the Drowned offers four errands after the vial: clear the vaults of the dead, silence three acolytes in The Dungeons, light the shrine at the sixth level of the vaults, or bring a blade of bone; each is offered once and pays 100 to 150 gold with Cult +8 and Temple −4 (`QuestBook.cs`, `Dialogues.cs`, `guild-jobs.md` D11).
- 2026-10-08 · **World ledger**: the ledger is read where the hero can see it: a town's counters ask more after killings and less after good deeds (`TownMemoryPct`), a healer refuses a hand that has killed twice there (`HealingRefused`), a dead shopkeeper is replaced by an heir who says so (`InstallShopHeirs`, `HeirWords`), the Guild's cellar holds three dead that stay down once cleared (`CellarDead`, `CellarCleared`, a `cleared` deed), news of a killing reaches the three nearest towns three days later (`NewsHere`, `NearbyTowns`), and the epilogue names the six heaviest deeds in order (`MatteringDeeds`) (`Game.Ledger.cs`, `Game.Succession.cs`, `Game.Town.cs`, `Game.Rumours.cs`, `Town.cs`, `Game.Main.cs`).
- 2026-10-08 · **Dungeon folk**: a wounded delver who pays for a potion of healing, a looter who runs and drops a sack, the remains of a party with a note, and captives who are freed with `D` and walk home as an escort quest that the town's gate ends (`Game.Folk.cs`, `Game.Quests.cs`, `Game.Companions.cs`, `Commands.cs`, `Game.Repeat.cs`).
- 2026-10-08 · **Essential people**: every essential person (Elder, Reader, Captain, High Priest, the Cult's beggar) has an apprentice lodged with them; three blows at one who lies out cold remove them, and the apprentice takes the post, worse disposed and owing one favour on the quest engine; with no apprentice left the Guild posts a Messenger notice; raids spare essential keepers (`Game.Succession.cs`, `Game.Crime.cs`, `Town.cs`, `Dialogues.cs`, `QuestBook.cs`, `Persona.cs`, `Game.Raids.cs`).
- 2026-10-08 · **Conversations**: a face in the box (hat from the trade, eyes from the temperament, mouth from the mood), mood and temper tags, and PgUp/PgDn read the talk back (`Portrait.cs`, `Game.Dialogue.cs`, `Ui.cs`, `Session.cs`).
- 2026-10-08 · **Guild jobs on the quest engine**: notice-board jobs, workshop commissions and the Journal are quests on the Guild track; an offer is one-shot per week, and a board never offers a delve to a depth already reached (`Game.Jobs.cs`, `Game.Workshops.cs`, `Ui.cs`).
- 2026-10-08 · **Cult errands**: after the vial, the Drowned offer clearing the Sunken Vaults of human zombies and bringing them a blade of bone, both on the quest engine (`QuestBook.cs`, `Dialogues.cs`).
- 2026-10-03 · **Rival party and the main questline**: four documents, the Reader, six endings, a new cycle (`Game.Rival.cs`, `Game.Main.cs`).
- 2026-10-03 · **Crime and the Watch**: witnesses, bounty by region, arrest, the cells, clearing your name (`Game.Crime.cs`).
- 2026-10-03 · **Town events**: market day, festival, funeral, robbery, fever (`Game.TownEvents.cs`).
- 2026-10-03 · **Rumours with teeth**: true, vague or wrong by the teller, real places marked on the map (`Rumours.cs`).
- 2026-10-03 · **Personal, Watch and Temple errands** (`PersonalQuests.cs`).
- 2026-10-03 · **Dialogue**: a conversation box, authored talks with choices (`Dialogue.cs`, `Dialogues.cs`).
- 2026-10-03 · **Personas, memory and the ledger** (`Persona.cs`, `Game.Ledger.cs`).
- 2026-10-02 · **Reputation, contracts, road events and routine** (`Game.Reputation.cs`, `Game.Jobs.cs`, `Game.Events.cs`).
- 2026-10-02 · **Permanent companions** from the tavern (`Game.Companions.cs`).

**World and history**
- 2026-10-07 · **Second portal branch**: the Hollow Court hangs off a portal on Mines 5, with the Hollow Queen, her two relics and two achievements (`Dungeon.cs`, `Game.Bosses.cs`).
- 2026-10-07 · **Legends at the end**: the morgue tells the hero's legend (bosses, works, relics, raids) and the bones carry it into the next world's history (`Game.Legends.cs`, `Morgue.cs`, `Bones.cs`).
- 2026-10-07 · **Raids on towns**: a band comes for a town on one day of a week; the hero fights it in the streets, or the town loses a shop to fire and some of its people, and the burnt shop stays burnt (`Game.Raids.cs`).
- 2026-10-07 · **Room flavour**: what a place was, told once on entering and readable when looking at a cell (`Game.Places.cs`).
- 2026-10-07 · **Engravings**: old walls tell the chronicle, warn of the lair below and point at hidden doors; the hero can carve one with Shift+O (`Game.Places.cs`).
- 2026-10-07 · **Traces and tracking**: drag marks and blood trails; a bleeding hero is smelled from further off, and the hero follows wounded prey with Shift+M (`Game.Stains.cs`).
- 2026-10-07 · **Blood and fluids**: blood by species, ichor, slime, mud, soot, footprints and drag marks on the cell's background, fading with time (`Surfaces.cs`, `Game.Stains.cs`, `Ui.cs`).
- 2026-10-07 · **Legends panel**: F8 browses the events, people, places and heroes-before-you the hero has learned, taught by libraries, scholars, bards, shelves, rumours, relics, engravings and tombs (`Game.Legends.cs`, `Ui.cs`).
- 2026-10-07 · **History feeds the game**: rumours, the Scholar, books on shelves, tavern songs, place names, relic biographies, engravings and tombs all read the chronicle (`Game.Legends.cs`, `World/History.cs`).
- 2026-10-07 · **Historical figures**: smiths, kings, knights, priests, thieves, scholars and warlords with births and deaths, some still alive, some buried below with their gear and their names cut in the stone (`World/History.cs`).
- 2026-10-07 · **History pass**: three centuries from the seed with houses rising and falling and the kings' successions by blood, by the sword or by the lords' choice, towns founded and razed, wars, plagues and the year the Pit opened (`World/History.cs`).
- 2026-10-03 · **Dungeon level 1 always has a way out** (`Dungeon.Ensure`).
- 2026-10-02 · **The Annex**: a portal branch with a warden and a mantle.
- 2026-10-02 · **Every branch reachable** from its overworld region (`OverworldGen.BranchForRegion`).
- 2026-10-02 · **Ruins and catacombs** level styles.

**Creatures**
- 2026-10-02 · **Bosses with mechanics**, one per branch (`Game.Bosses.cs`).
- 2026-10-02 · **Branch natives with habits** (`Game.Traits.cs`).
- 2026-10-02 · **Monster factions** that fight each other (`Game.Factions.cs`).

**The hero**
- 2026-10-02 · **Mutations and corruption** (`Entities/Mutations.cs`, `Game.Corruption.cs`).
- 2026-10-02 · **Trained mode**: XP bought into skills (`Game.Training.cs`).
- 2026-10-02 · **Modes and challenges**: Classic, Hardcore, Dive, Naked, the daily seed (`Difficulty.cs`, `Daily.cs`).
- 2026-10-02 · **Achievements, bones, run history and the morgue** (`Achievements.cs`, `Bones.cs`, `Morgue.cs`).

**Interface and controls**
- 2026-10-08 · **Marked travel points**: `'` marks the cell underfoot for free, `Shift+'` walks to the nearest other mark and on to the next; a mark warms its cell on the map and is kept per level (`Game.Marks.cs`, `Commands.cs`, `Ui.cs`).
- 2026-10-04 · **Stairs keys say Shift** on every keyboard, ABNT2 included.
- 2026-10-02 · **Auto-explore, travel and rest**: `t`, `` ` ``, `~`, `Shift+S`, and held keys that keep walking (`Game.Explore.cs`, `Game.Repeat.cs`).
- 2026-10-02 · **Animated water and square tiles** (`anim.ts`).

**Audio**
- 2026-10-04 · **Danger warning and a typed intro**: the *Danger Spotted* stinger, letter clicks.
- 2026-10-04 · **Stingers and the whole soundtrack**: 15 stingers, 18 tracks that follow the moment (`stingers.ts`, `audio.ts`).
- 2026-10-03 · **Synthesised music**, Web Audio only.
- 2026-10-02 · **PC-speaker effects** named by the Core (`Game.Sound.cs`).

**Language and docs**
- 2026-10-05 · **One tracker**: the depth roadmap folded into this file, by topic.
- 2026-10-04 · **One language at a time**: names with gender, composed sentences, every panel, audit tools and the `language` suite.
- 2026-10-03 · **Docs in English**, Portuguese for the root files.
