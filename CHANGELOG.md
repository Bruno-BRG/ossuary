# Changelog

**English** · [Português (Brasil)](CHANGELOG.pt-BR.md)

Ossuary versions are a **single number** (Version 11, Version 12…), as in *Project Zomboid*. In the manifests (`package.json`, `Cargo.toml`,
`tauri.conf.json`) and in the installer name the version appears as `0.N.0`. The **save format** (`SaveData.Version`) uses the same number: change a rule, bump it, old saves stop loading.

## Version 22 — Spells that answer back

### Casters
- **Monsters cast real spells.** An orc shaman, a dark acolyte or a sorcerer that sees you, with one of its spells in reach, may spend its turn casting instead of moving. It picks from its own list of spells, the same recipes your spells are written in, and the spell is animated from its cell to yours. Every cast lands: no mana, no failure roll, and your resistances count.
- **The casters.** Dark acolytes (depths 4–14) cast bone shards, withering, soul bolts and rot bursts; sorcerers (depths 9–20) cast arc flashes, searing orbs, thunderstrikes and static fields. The orc shaman now casts ember darts, frostbite and stone shards.
- **The Sallow Magister.** A new boss on the seventh level of the Dungeons, with 100 HP. It casts searing orbs, thunderstrikes and arc flashes, and adds ice comets and static fields in its second phase.
- **Fair fights.** A caster waits two turns between spells. A creature casts on two in five of its eligible turns, a boss on one in two. A monster's spell rolls half the dice of your version of it, rounded up, plus a quarter of the caster's level.
- **Riders.** A spell can burn you, confuse, blind, stun or poison you, and you can shake one off. Other riders (slow, weakness, bleeding, fear, sleep, root) only do damage, because you have no status for them yet.

### Ice and lightning
- **Frozen creatures conduct.** Lightning that strikes a creature standing on ice runs along the connected ice within four cells, and everything standing on it is shocked, you included.
- **Wet creatures arc.** Lightning that strikes a wet creature standing outside the water jumps to the nearest other wet hostile within three cells, up to three times, each jump at half the damage. Water still conducts as before.

### Projectiles land when they arrive
- **The hit waits for the bolt.** The damage, the death of its target, the log and the map change when a bolt reaches its target, not when the animation starts. Until then the screen keeps what you saw before. Arrows, thrown molotovs and the bolts of abilities land on arrival too.
- Effects with no projectile (cones, beams, novas, chains, traps) still resolve at once.

### Sound and skipping
- **One cast sound per element.** Fourteen short sounds replace the single cast chime, and the sounds of what a spell does (hits, kills, hurts) wait for the bolt to land.
- **Escape skips an animation.** While an animation plays, Escape jumps to its last frame and plays the sounds it was holding. The key is not sent to the game, so it does not open the pause menu during an animation.

### Saves
- The save format moves to **22**: Version 21 saves do not load.

## Version 21 — Hired hands

### Jobs on the quest engine
- **The notice board is a quest board.** A job becomes a quest on the Guild track when you take it. The Journal shows its steps, progress and reward, and the Character panel shows one `Job:` line for each active job. Hunts and delves are reported at any notice board; a commission is delivered at any workshop that teaches the trade.
- **One offer per week.** A job you take is not offered again that week, whether it is active, done or failed. You can carry three jobs at once, commissions included.
- **The same rewards.** Hunts and delves pay their reward and give the giver +10 standing. A commission pays, and on delivery it gives the Guild +5 standing and 5 trade XP.
- **Hired Hand still counts.** Finished jobs count toward the *Hired Hand* achievement and the morgue's *Jobs done* line, as before.

### The Drowned's errands
- **Two errands after the vial.** Once the Drowned have their vial and you have Cult standing 10 or more, *What work?* offers two more errands, each one once:
  - **Clear the vaults:** destroy three human zombies in The Sunken Vaults, then report to the Drowned. Pays 110 gold; Cult +8, Temple −4.
  - **A blade of bone:** bring a bone blade (a smith can forge one from a blade and remains). The Drowned keep it. Pays 150 gold; Cult +8, Temple −4.

### Journal and panels
- **One list.** The Journal's separate block of contracts is gone. Guild jobs sit under *Guild* with the other quests, showing the step they are on and its hint.

### Saves
- The save format moves to **21**: Version 20 saves do not load.

## Version 20 — The world remembers

### World and history
- **A real past, from the seed.** Three centuries: noble houses rise at their seats and about half of them fall; a line of kings succeeds itself by blood, by the sword or by the lords' choice; smiths, knights, priests, thieves, scholars and warlords are born, live and die — some of them are still alive in the year you arrive. Towns are founded, wars and plagues pass, towns burn, and the Pit opens.
- **The dead lie below.** Knights, warlords, kings and priests who died after the Pit opened are buried on real levels: a grave, their name cut into the stone, and what they were buried with — gear that remembers them.
- **Legends panel (F8).** Everything you learn about the past, in one place: events, people, places, and the heroes who came before you.
- **History feeds the game.** Tavern songs, rumours, the Scholar's *Ask about the old days*, books taken down from library and emporium shelves, relic biographies, engravings and tombs all read the chronicle and teach you its legends. Ruins are named after the towns that burned; keeps after the houses that fell.
- **The heroes before you.** The bones of earlier runs enter this world's history: the bard sings of them, and the panel lists them.

### Blood and traces
- **Blood by species.** The living bleed, insects and demons leave ichor, molds and oozes slime; the dead and the made leave nothing. Hits splatter, a heavy blow throws it further, and it darkens the floor where it falls.
- **It fades.** Blood dries and darkens over hundreds of turns; footprints, mud carried in from water, soot where fire burned out and the drag marks of a crawling creature all fade in their own time.
- **The trail both ways.** A bleeding hero is smelled: hunting things come for you even out of sight. And you can follow a wounded creature's trail with Shift+M, which points the way and follows it.

### Engravings, rooms and tombs
- **Old walls talk.** The chronicle is cut into the floor down there, along with warnings above a lair and a clue pointing at a hidden door. Walking over one reads it and teaches its legend.
- **Rooms remember.** The first time you step into a barracks, a temple, a larder or a treasury, one line says what it was; looking at a cell inside says the same.
- **Carve your own (Shift+O).** On bare floor you can cut your name, a warning, or the last thing you killed into the stone.

### Raids on towns
- **Some weeks a band comes.** Kobolds near the coast roads, orcs further in, the dead in the worst country, on one day of the week.
- **If you are there**, the raiders come over the wall by the gate and you can fight them in the streets. Kill them all and the town remembers: gold, and the Watch's gratitude.
- **If you are not**, the Watch holds or the town pays: a shop burns (its stock gone, its keeper dead, its floor black for good) and one to three of its people die. Leave town while raiders are in the streets and it counts as a loss.

### The Hollow Court
- A second portal branch, behind a portal deep in the Mines (level 5): three floors of a court the dwarves walled up, with the Hollow Queen on the last, her Thorn and her Gown, and two achievements.

### The end
- **The morgue tells your legend**: the bosses you slew, the relics you carried, the works you made, the raids you beat off, and the legends you learned.
- **The next world remembers**: your bones carry that legend into the history of the run after yours.

### Saves
- The save format moves to **20**: Version 19 saves do not load.

## Version 19 — Every trade

### Items and materials
- **Identify by use.** Potions, scrolls and wands start as what they look like ("murky amber potion", "twisted oak wand"), different every run. Drink, read or zap one, buy it, have it appraised or read identify, and you know that kind for good. F6 counts what you know.
- **Shop services.** Sages in emporiums and libraries recharge wands (the second time is a risk). Priests lift curses.
- **Cursed amulets.** The leech, restless sleep and the hungry dead: strong, with a price, and they will not come off until a priest lifts the curse.
- **More relics.** The Weeping Edge, Marrow Mail and the Crown of the Pit, all of them corrupting.
- **Relics with a history.** The world now has a past generated from the seed: three centuries of smiths, kings, knights and warlords, wars, plagues, towns founded and burned, and the year the Pit opened. Every unique has a biography read from it (who forged it, of what, for whom, who carried it, how it was lost), and libraries read the chronicles aloud.
- **Deeds written into relics.** Kill a boss with relics in hand or on and each one remembers it. The morgue prints every relic's story.
- **Previous owners.** Gear taken from a monster remembers it, and so does a dead hero's. Examine (Shift+I) or look (`l`) at a single thing on the floor to read it all.

### Crafting and trades
- **Loom and still.** Cloth and cloaks are woven at a loom in the general store; ale and mead are brewed at a still in the tavern or the alchemist's. Every smithy now has a forge you can reach.
- **Crafters elsewhere.** Each week the smith, the armourer, the tailor and the alchemist set out new signed work, and on market days the rival party sells arrows and what it brought up from below.
- **Monster archers.** Kobolds, gnomes and orcs with slings, bows and crossbows shoot from range with real ammunition, which lands at your feet.
- **Choose your ammunition** (Shift+Y): silver for the dead, plain for the rest.
- **Your named works in the world.** Sell one and a townsperson buys it a few days later and carries it; the taverns talk about it and about you.

### Economy and market
- **Caravans on the road.** The caravan you meet is the nearest town's. Guard it and its goods arrive and you get paid; rob it and the town goes short and the Watch wants you.
- **Haggling** (`o` at a counter): offer 90, 75 or 60 percent. Cha, the trader's mood and the Guild decide; a refusal sours the trader for the day.
- **Traders remember.** Flood one with goods, sell them something cursed or ruined, or haggle well, and they greet you accordingly and price accordingly.

### Saves
- The save format moves to **19**: Version 18 saves do not load.

## Version 18 — Forge to market

### Crafting, items and the market
- **Ammunition.** Bows loose arrows, crossbows bolts, slings stones. Each shot spends one; it lands where you aimed and you pick it up again, unless it snapped. A silver or cold iron head is a bane like a blade of that metal. With no launcher, or nothing to loose, you hurl a stone.
- **Fletching.** Bowyers make arrows from a log and a thread anywhere, metal-headed arrows and bolts at the forge, and sling stones from rocks. A journeyman's headed arrows come out +1.
- **Rangers and rogues start ready to shoot**: a short bow and 30 arrows, a sling and 15 stones.
- **Named masterworks.** A master's masterwork sometimes earns a name ("Ashtooth, +3 masterwork steel long sword") and becomes a relic of its maker: worth far more, and listed in the morgue.
- **Examine an item** (Shift+I): what it is, its numbers, material, maker and quality, engraving, wear and worth.
- **The town smith's own work.** Smithies and armouries sell a piece their smith made and signed, and smithies sell ammunition by the bundle.
- Ammunition, goods and food merge into one stack when picked up or made; ammunition trades as weapons and sells by the stack. Goods now show in the inventory.
- A troubled beggar or scholar no longer gets a favour their own conversation hid.

### Saves
- The save format moves to **18**: Version 17 saves do not load.

## Version 17 — Coin and caravan

### Economy and market
- **Supply and demand.** Each town has its own tastes for weapons, armour, potions, books, wands, jewellery, food, raw goods and tools. Sell ten swords to one smith and what the town pays for blades drops; it recovers over a few days.
- **Caravans and raided roads.** Every week a caravan comes in (one class of goods is cheap and the town's surplus is bought up) or the road is raided (that class is dear). Dangerous regions are raided more. The news greets you at the gate.
- **The market square.** Stalls open a menu. On market days traders from elsewhere lay out their wares and the stalls pay more.
- **The order board.** Each week a town posts two things it wants, paying well (often something you can craft), and one thing offered cheap.
- **Your own counter.** Rent a stall counter for a week, set out up to eight things, ask a cheap, fair or dear price, and come back for the coin: it sells while you are away.
- **Haggling.** Charisma, the trader's mood (shown at the counter), Guild standing and paid Guild dues move prices. Your signed work sells for a quarter more once you are a master.
- **Money sinks.** Gate tolls in towns and cities (friends of the Watch walk in free), stall rent and Guild dues.
- **Price history.** The journal (F7) lists the latest prices you saw, town by town.
- Selling raw goods and food is priced by the stack, and a shop now pays you out of its own purse.

### Saves
- The save format moves to **17**: Version 16 saves do not load.

## Version 16 — Blood and bone

### Combat and bodies
- **Called shots.** Shift+F picks where you aim: head, arms, legs, eyes, wings or tail. Smaller targets are harder to hit, and an aimed hit always leaves a mark.
- **Severed parts.** A heavy edge can take off a monster's arm, leg, wing or tail. A mangled arm drops its weapon and hits half as hard; a broken wing brings a flier down.
- **Wounded monsters act like it.** A crippled creature at half health turns and runs; one with both eyes cut swings at random, sometimes into its friends.
- **More wounds.** Monsters wound each other, and force, frost, lightning and fire spells break bodies too. Vipers and giant pythons arrive with a serpent's body.
- **Bandages.** Every hero starts with two; the temple sells them and a tailor cuts three from a cloth. They stop bleeding and double the pace of mending. Fire sears cuts shut, and hours on the road count toward healing.
- **Scars that count.** A scar on the face costs a point of Charisma, scars make road bandits think twice, and townsfolk notice them.
- **Martial techniques.** The Fighter carries a manual of strikes: Hamstring, Disarming Blow, Skull Crack and Lunge, learned by level and paid with Vigor.

### Saves
- The save format moves to **16**: Version 15 saves do not load.

## Version 15 — Iron and craft

### Materials
- **Gear is made of something.** Copper, bronze, iron, steel, silver, cold iron, mithril, adamantine, obsidian, bone and wood, each with its own weight, price, edge and armour. The name says it ("steel long sword"); plain iron is not named.
- **Deeper is finer**, and each branch leans its own way: good metal in the Mines, bone and copper in the Warrens, silver in the Sunken Vaults, black glass in the Ashen Spire.
- **Material against creature.** Silver sears the dead and werebeasts, cold iron the fey, obsidian golems and gargoyles: "The silver bites deep!".
- **Gear wears.** Weapons blunt and chip with use, armour dents and batters under blows, each step a point lost. Brittle or cheap blades can shatter on a critical.
- **The smith repairs**, and pours your weapon or armour anew in a metal you bring as ore. Ore lies in the Mines, breaks loose when you dig, and the smithy sells copper and iron.
- **Piercing wounds go deep**: daggers, spears, tridents, arrows and stings leave a worse wound on a hard blow.

### Crafting for everyone
- **Sixteen trades, open to any hero**: blacksmith, armourer, bowyer, leatherworker, tailor, jeweller, alchemist, scribe, carpenter, toolmaker, luthier, cook, brewer, miner, musician, forager. Each grows by the work, from Novice to Grandmaster.
- **About a hundred recipes**: swords and plate in the metal of the bars you smelt, bows, leather, cloaks, rings that take a spell from their stone, potions, scrolls, pick-axes and fishing rods, lutes, harps and tambourines, bread, stew and ale.
- **Where you work matters**: a fire anywhere, a town workshop, or the smithy's forge. **Quality follows the hand**: crude, plain, fine or a masterwork that carries the maker's name.
- **Shift+B** makes what you can, **Shift+J** shows the whole recipe book, **Shift+G** gathers: butcher a carcass, or spend two hours on the road taking wood, flax, barley, herbs, honey, ore or fish from the land.
- **Music**: play an instrument for coin in town, or to lull creatures to sleep below.
- **Masters and commissions**: each workshop teaches its trades up to journeyman and posts a commission a week. Shops sell raw goods and buy your work by the stack.
- **You never have to go down**: a hero can live by a trade from the first day.

### Saves
- The save format moves to **15**: Version 14 saves do not load.

## Version 14 — Flesh and bone

### Bodies and wounds
- **A hit lands somewhere.** When a blow takes a real share of a creature's life (15% or more), it lands on a body part and leaves a wound on top of the damage. The log says where: "Your left leg is broken.", "The jackal's hind left leg is torn."
- **Four severities, two kinds.** Bites, claws, blades and arrows graze, cut, tear and mangle; fists, clubs and kicks bruise, batter, break and crush. A critical cuts deeper, and hitting a hurt part again makes it worse.
- **Wounds change the fight.** Broken legs make anything limp or crawl (wings, for fliers), hurt arms spoil the aim, a broken head stuns, a cut eye shortens sight and how far a monster notices you. A limping hero gives the monsters extra moves.
- **Cuts bleed.** A few points a turn for a few turns. A monster can bleed to death (it counts as your kill), and so can you.
- **Every creature has a body**: humanoids, four-legged beasts, insects and spiders, bats and birds, dragons. Moulds, floating eyes, wraiths, elementals and swarms have nothing to break; skeletons, golems and the dead break but do not bleed.
- **Mending.** Wounds heal on their own with time; a torn or broken one leaves a scar. Healing spells and potions mend every wound a step, full healing and a night at the inn close them all, and the temple stops the bleeding.
- **You can see it**: BLEEDING, LIMPING, CRAWLING and WOUNDED in the sidebar, *Wounds* and *Scars* on the character sheet and in the morgue, and looking at a monster lists its wounds. Everything reads in Portuguese with the right gender ("Sua perna esquerda está quebrada").

### Docs
- The `docs/` folder is organised by topic (`game/`, `design/`, `tech/`, `audio/`, `roadmap/`) with an index in `docs/README.md`.
- A new depth roadmap, the *Depth track* in [`docs/todo.md`](docs/todo.md), collects the Dwarf Fortress-inspired ideas still to come: materials, generated history, relic biographies, fluids and tracking, mood, engravings, named enemies, sieges and legends.

### Saves
- The save format moves to **13**: where a blow lands is drawn from the dice, so Version 13 saves do not load.

## Version 13.3 — Every word, and a warning you hear

A patch on Version 13 (`0.13.3` in the manifests). No rule changed, so Version 12 and 13 saves keep loading.

### Portuguese, the second pass
- **Composed sentences translate whole.** Rumours, places and names inside a sentence no longer leave an English half ("Dizem que uma mina abandonada nas Colinas de Ferro..."), and "You arrive at The Dungeons : level 1." reads "Você chega às Masmorras, nível 1.".
- **Names with gender and number**: dungeon entrances, landmarks, houses, branch kings and tiles ("Você cava através dos escombros"); items agree in gender and number ("botas robustas do urso") and enchantments in front of the name compose correctly.
- **The rest of the game**: relics, sets and their lore, reputation reasons, rival parties, hero titles, the morgue file, gods and the altar menu, the six bosses, the endings and new cycle, companions, main-quest documents, tooltips, and the 111 damage verbs ("Uma só palavra desfaz o chacal por 5 de dano.").
- **The other direction**: the frontend's error strings, screen-reader labels and boot line follow the chosen language; English screens carry no Portuguese.

### Sound
- **A warning you cannot miss**: the *Danger Spotted* stinger plays when a hostile first comes into view and when the road refuses to let you pass, without stacking if you keep bumping into it.
- **The intro types out loud**, letter by letter.

### Controls
- The stairs keys are symbols with no key of their own: messages, the Commands panel and the Controls panel now say "> is Shift + ." on US and ABNT2 keyboards.

### For contributors
- `loc msgs` scans every string literal with interpolation stand-ins; `loc pairs` prints lines identical in both languages; new tests `English frames carry no Portuguese`, `i18n.test.ts` and a broader `LocTests`.

## Version 13.2 — One language at a time

A patch on Version 13 (`0.13.2` in the manifests). No rule changed, so Version 12 and 13 saves keep loading.

### Portuguese, finished where it was half-done
- **Combat reads properly in both languages.** The English log said "You hit the the jackal", "The jackal hits the you" and "you misses the kobold"; now it says "You hit the jackal for 3 damage.", "The jackal hits you for 3 damage.", "The jackal misses you." and "You evade the jackal's attack." Named townsfolk no longer get a "the" in front. Portuguese has the same lines with gender and contractions ("do chacal", "a aranha das cavernas").
- **"It is weak" no longer leaks into Portuguese.** Looking at a monster, and the road-encounter prompt, now say "Parece fraco / um pouco perigoso / perigoso / muito perigoso".
- **Monster and item names are translated.** All the bestiary and every catalogue item, with the usual parts composed ("blessed +1 keen dagger of the fox" becomes "adaga afiada da raposa +1 abençoada"), plus "x3" stacks.
- **The screens finish the job**: the top bar ("NÍVEL 3", "Dia 1"), the hint bar, the status line, the HP/EN/VG/MP labels, the character sheet (skills, ranks, alignment, perks, traits), abilities and perks with their descriptions, the advancement list, the whole Controls panel, the class and race descriptions in character creation, and the tile names.
- **About 300 more messages**: spells, potions, gods, traps, road events, bosses, items, training and advancement.

### For contributors
- `headless.ps1 loc [seeds] [turns]` plays in Portuguese and lists every string that reached the screen untranslated; `loc frames` prints the panels in Portuguese; `loc msgs` checks every `Say`/`Tell` in the Core; `loc names` lists the names to translate.
- A `language` test suite fails when a monster, item, affix, perk, ability or control label has no Portuguese, and pins the main combat and HUD lines in both languages.

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
- New `ArsenalTests` and `fx.test.ts`; documentation in [`docs/spells-and-items.md`](docs/design/spells-and-items.md); [CONTRIBUTING.md](CONTRIBUTING.md) and a new README.

## Before version 11

Earlier history is in `git log` and in the finished-items log of [`docs/todo.md`](docs/todo.md): vertical towns, the living world (reputation, jobs, road events), bosses, factions, corruption and mutations, companions, achievements, the daily challenge, game modes, sound effects and animated water, among others.
