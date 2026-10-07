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
| [Magic](#magic) | 322 spells, 55 books, animations | monsters that cast, ice × lightning |
| [Items and materials](#items-and-materials) | materials, wear, repair, relics, sets, examine, ammunition | identify by use, relic biographies |
| [Crafting and trades](#crafting-and-trades) | 16 trades, ~105 recipes, gathering, fletching, named masterworks | loom and still, monster archers |
| [Economy and market](#economy-and-market) | glut, caravans, market square, orders, your counter, tolls, price history, town smiths' work | caravans you can meet on the road |
| [Towns and people](#towns-and-people) | personas, dialogue, quests, crime, rumours | Guild jobs on the quest engine, theft |
| [World and history](#world-and-history) | overworld, six branches, the Annex | generated history, blood and traces |
| [Creatures](#creatures) | factions, bosses, branch natives | named survivors, ecology |
| [The hero](#the-hero) | corruption, modes, achievements, bones | thoughts and mood |
| [Tutorials](#tutorials) | nothing yet | first-time hints |
| [Interface and controls](#interface-and-controls) | auto-explore, travel, rebinding | marked travel points |
| [Audio](#audio) | 18 tracks, 15 stingers, effects | the missing boss tracks |
| [Technical and quality](#technical-and-quality) | headless suites, soak, balance bot | a language gate worth reading |

**Next up**: tutorials, then blood and traces with richer look descriptions, then named survivors and mood, then the
generated history that relics, engravings and legends read from.

---

## Combat and bodies

- [ ] **Severed parts as things**: a cut-off limb lies on the floor as an item (a trophy, butchery, a quest token); blood stays in *World*.
- [ ] **Burns as wounds**: fire and acid get their own words (scorched, charred) and scars, instead of counting as blunt blows.
- [ ] **Techniques for the other martial roles**: a rogue's and a ranger's manual, and techniques bought from the Guild's masters.

## Magic

- [ ] **Monsters that cast**: sorcerers and new bosses using real spells, with their animations.
- [ ] **Ice × lightning**: frozen targets conduct, wet ones arc.
- [ ] **Damage with the projectile**: the hit lands when the bolt arrives, not before the animation.
- [ ] **Per-element sound** and a key to skip a whole animation.

## Items and materials

- [ ] **Identify by use**: wands, potions and scrolls start unknown and are learned by trying them.
- [ ] **Shop services**: recharge wands; new cursed amulets; more corrupting relics.
- [ ] **Relic biographies**: who forged it, of what, for whom, who carried it, how it was lost (reads *World and history*).
- [ ] **Deeds written into relics**: a boss killed with a relic adds a line; the morgue prints its whole story.
- [~] **Item descriptions**: has Shift+I with numbers, material, maker, quality, engraving, wear and worth; left previous owners and the look (`l`) at items on the floor.

## Crafting and trades

- [ ] **Loom and still as tiles**: tailoring and brewing at their own stations, like the forge.
- [~] **Crafters elsewhere**: has the town smith's signed piece in every smithy and armoury; left rival parties and other townsfolk making and selling over time.
- [ ] **Monster archers with real ammunition**: kobold and goblin archers loose arrows that lie on the floor afterwards, and drop their quivers.
- [ ] **A quiver slot**: choose which stack to loose (silver for the dead, plain for the rest) instead of the best one by default.
- [ ] **Named works in the world**: the hero's named masterworks show up in rumours and in the hands of whoever bought them.

## Economy and market

- [ ] **Caravans on the road**: the week's caravan as travellers you can meet, escort or rob, so the hero decides a raided road.
- [ ] **A haggle verb**: offer a price at the counter, the trader's mood answers, a failed haggle sours them for the day.
- [ ] **Merchants' memory**: a trader you cheated or flooded remembers it in their dialogue (reads the ledger).

## Towns and people

Plan and rationale in [`game/living-world.md`](game/living-world.md).

- [~] **Quest engine**: has objectives, rewards, deadlines and the F7 journal; left Guild jobs running on the engine instead of the old code.
- [~] **Other people on the road**: has pilgrims, peddlers, refugees and delvers; left delvers and captives inside dungeon levels.
- [~] **World ledger**: has deeds read by lines, grief, quests, bounty and journal; left prices, town state, rumours about the hero, the epilogue.
- [~] **Essential people**: has knocked out instead of killed; left successors and removal by extreme acts.
- [~] **Cult track**: has the vial errand; left more Cult quests and reward balance.
- [~] **New game plus**: has carry-over of level, items, truths and ledger; left people who react to the hero's legend.
- [ ] **Main quest polish**: house speakers at the ceremony, the Archivist's Shade and the Spire guardian as encounters, ending ids in the morgue.
- [ ] **Theft and property crimes**: steal and burn verbs, and escaping the cells with a lock pick.
- [ ] **Town fires and raids**: events that burn buildings, which stay burnt.
- [ ] **Conversations**: portraits and mood tags in the box, a scrollable history of what was said.
- [ ] **Companions**: orders (stay, follow), shared inventory, more than one, archers.
- [ ] **Open questions**: exact fine formulas, how far a bounty reaches, new-game-plus details.

## World and history

- [ ] **History pass** (`World/History.cs`): 100–300 years from the seed, houses rising and falling, wars, plagues, kings, towns founded and razed.
- [ ] **Historical figures**: named people with births, deeds and deaths; some still alive, some buried below.
- [ ] **History feeds the game**: rumours, the Scholar, books on shelves, relic lore, place names, tavern songs.
- [ ] **Legends panel**: browse the events, figures and places the hero has learned about.
- [ ] **Blood and fluids**: blood by species, ichor, slime, mud, soot and footprints on the cell's background (`Surfaces.cs`), fading with time.
- [ ] **Traces and tracking**: drag marks and blood trails; monsters follow a bleeding hero, the hero follows wounded prey.
- [ ] **Engravings**: old walls showing history, some quest clues or boss warnings; the hero can carve one.
- [ ] **Room flavour**: what a place was (barracks, shrine, larder), read from what is in it.
- [ ] **Raids on towns**: a strong monster faction attacks; an undefended town loses shops and people.
- [ ] **Legends at the end**: the morgue tells the hero's deeds, relics and enemies; past heroes enter the next world's history.
- [ ] **Second portal branch** in another dungeon.

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

- [ ] **Marked travel points**: the hero sets their own destinations (today `~` goes to altars and fountains, `` ` `` to the stairs).

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
- 2026-10-03 · **322 spells, 55 books, animations**: eight schools, recipes as data, every spell animated (`Game.Magic.Recipes.cs`, `Fx.cs`).
- 2026-10-02 · **Corruption and surface spells**: Steam Burst, Create Oil, Ossify, Reshape Flesh, Marrow Bolt, Purify, Ice Lance.
- 2026-10-02 · **Mourne, rival gods, sacrifice and trials** (`Game.Gods.cs`).

**Economy and market**
- 2026-10-07 · **Town smiths' work**: every smithy and armoury sells a piece its smith made and signed, smithies sell ammunition by the bundle, ammunition trades by the stack (`Town.cs`, `Game.Market.cs`).
- 2026-10-06 · **Town markets**: supply and demand by goods class with a glut that recovers over days, town tastes, weekly caravans and raided roads, market-day traders on the stalls, a weekly order board, a rented counter that sells while you are away, haggling by Cha, mood, Guild and a master's name, gate tolls, rent and Guild dues, price history in the journal (`Game.Market.cs`, `Loc.Market.cs`).

**Items and materials**
- 2026-10-07 · **Ammunition**: arrows, bolts and sling stones loosed by the matching launcher, spent one a shot and picked up again, metal heads that bane, stacks that merge (`Items/Ammo.cs`, `Game.Ammo.cs`).
- 2026-10-05 · **Materials**: eleven materials by depth and branch, banes, wear and shattering, repair and recasting from ore (`Items/Materials.cs`, `Game.Materials.cs`).
- 2026-10-03 · **Items with a spell inside**: gear imbued by kind, +48 bases, +22 affixes (`Magic/SpellFit.cs`).
- 2026-10-03 · **43 uniques and 4 sets** across every branch, many lending a spell.
- 2026-10-02 · **Artifacts, sets and corrupting relics** (`Items/Affixes.cs`, `Game.Corruption.cs`).
- 2026-10-02 · **Vaults and caches**: locked vaults with a brass key, trapped hidden caches (`Gen/Vaults.cs`).

**Crafting and trades**
- 2026-10-07 · **Arrows as ammunition**: fletching anywhere, metal-headed arrows and bolts at the forge, sling stones from rocks; rangers and rogues start with a launcher (`Items/Trades.cs`, `Game.Ammo.cs`).
- 2026-10-07 · **Named masterworks**: a master's masterwork sometimes gets a name and the hero as its maker, becomes a relic worth far more, and enters the morgue (`Game.Crafting.cs`, `Items/Masterworks.cs`).
- 2026-10-05 · **Crafting for everyone**: 16 trades, ~100 recipes, forge and workshops, quality by rank, recipe book, gathering, music, masters, commissions (`Items/Trades.cs`, `Game.Crafting.cs`, `Game.Gathering.cs`, `Game.Workshops.cs`).
- 2026-10-02 · **Light crafting**: molotov, bone blade, bone armour, extra healing (`Items/Crafted.cs`).

**Towns and people**
- 2026-10-03 · **Rival party and the main questline**: four documents, the Reader, six endings, a new cycle (`Game.Rival.cs`, `Game.Main.cs`).
- 2026-10-03 · **Crime and the Watch**: witnesses, bounty by region, arrest, the cells, clearing your name (`Game.Crime.cs`).
- 2026-10-03 · **Town events**: market day, festival, funeral, robbery, fever (`Game.TownEvents.cs`).
- 2026-10-03 · **Rumours with teeth**: true, vague or wrong by the teller, real places marked on the map (`Rumours.cs`).
- 2026-10-03 · **Personal, Watch and Temple errands** (`PersonalQuests.cs`).
- 2026-10-03 · **Dialogue**: a conversation box, authored talks with choices (`Dialogue.cs`, `Dialogues.cs`).
- 2026-10-03 · **Personas, memory and the ledger** (`Persona.cs`, `Game.Ledger.cs`).
- 2026-10-02 · **Reputation, contracts, road events and routine** (`Game.Reputation.cs`, `Game.Contracts.cs`, `Game.Events.cs`).
- 2026-10-02 · **Permanent companions** from the tavern (`Game.Companions.cs`).

**World and history**
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
