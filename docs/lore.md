# Lore — Ossuary

Worldbuilding bible. Tone: **dark fantasy**. The Ossuary is a **physical
place**. The protagonist is a **drifter**. Everything here must match the
code: 5 branches, 9 regions, the Amulet of Yendor at the bottom of The Dungeons.

## Premise (one sentence)

The world threw its dead, its kings and its gods into a single hole — the
Ossuary — and now a nobody descends to steal what is left.

## What the Ossuary is

Not a metaphor. It is the continent's central catacomb: centuries of crypts,
mines, sewers, war dungeons and towers stacked on top of each other
until they became geology. Nobody planned it. Each generation dug a new floor
above or below the previous one and lost the map.

That is why the Ossuary has 5 "branches" that do not connect cleanly —
they are different ages of the same pit:

1. **The Dungeons (10 levels)** — the top layer. Prisons, barracks
   (`Barracks`), mazes (`Maze`) and forts (`Fort`). It was the basement of the
   old kingdom. Easiest entrance, exit still possible (`AscendPossible`).
   It is where the drifter is born: the current `EntryText` says only *"You enter the
   dungeons beneath the earth."*
2. **The Mines of Dwarfdeep (8)** — *"The air grows cold and the walls turn
   to living stone."* An abandoned dwarven mine. The dwarves (`dwarf`, `dwarf lord`)
   dug too deep, found black water and left. Tools
   (`pick-axe`) and veins are still there.
3. **The Warrens (9)** — *"Something has been living here a long time."*
   Den of kobolds, giant rats, spiders, centipedes. Low on the
   food chain. Nobody built it: it was gnawed.
4. **The Sunken Vaults (12)** — *"Black water drips from a ceiling you
   cannot see."* Sunken vaults, temples and archives. The water rose, the
   dead did not (`zombie`, `skeleton`, `gnome mummy`). It is where hunger and
   poison kill most.
5. **The Ashen Spire (15)** — *"The stone here is warm, and it should not
   be."* An inverted tower that descends instead of rising. Final war, final
   magic. At the bottom: `tiamat`, `lich`, `fire giant`, `dread knight`. The
   deepest and newest place — dug by those who were no longer people.

Golden rule of the lore: **descending is going back in time, in reverse**. The
deeper, the more recent and the worse what happened.

## Timeline (short, usable)

1. **Age of the Cities** — the 9 overworld regions were client kingdoms.
   Roads (`Road`) linked towns (`Town`, glyph `+`). Trade in
   weapons, potions, scrolls, wands — today's 8 shops are the fossil
   of that.
2. **Age of Dwarfdeep** — dwarves open the Mines. They bring `axe`, `war hammer`,
   `chain/plate mail` to the surface. They get rich, close the gates.
3. **The Sinking** — black water takes the Vaults. Archives, temples and
   banks sink with everything inside. The saying is born: "the Ossuary does not give back".
4. **The Spire War** — someone (the `guardian of the deep` still remembers
   who) raises the Ashen Spire to burn the dead. It burned the living.
   Ash covers `Emberdown` and `Ashen Marches`.
5. **The Present** — kingdoms became dangerous regions (`Danger 15–100`),
   towns became walled villages with dwarf guards and hobbit innkeepers,
   roads became trails with encounters. The Amulet stayed down there and
   nobody of importance volunteers to fetch it.

## Overworld: the 9 regions

Generated on a 4×3 grid (`DefineRegions`). Fixed names in the code, dangers
rising from west to east. The lore ties each one together:

| Region | Danger/Depth | Lore reading |
|---|---|---|
| The Verdant Reach | low | The last land still pretending everything is fine. Fields, the current `Ashford`. |
| Ashen Marches | low-medium | Ash from the Spire. Sand, ruin (`⌂`), hot wind. |
| The Sunken Vale | medium | Edge of the Vaults. Swamp (`Whisperfen` leaks in here), water rises. |
| Gallowmoor | medium | Where Ossuary looters were hanged. The gallows became `wayshrine` (`⛩`). |
| The Iron Hills | medium | Surface dwarven colonies. `Mine` (`⇛`) and `Keep` (`♜`) everywhere. |
| Whisperfen | medium-high | A whispering swamp. `gas spore`, `mold`, `floating eye`. Nobody drinks the water. |
| The Craglands | high | Broken stone, `Cave` (`▼`). Kobolds and orcs rule. |
| Emberdown | high | Hot ash, `fire giant` in local mythology. Villages pay tolls to an `ogre lord`. |
| The Hollow Wastes | maximum | End of the map. `tiamat` is religion here. `Depth 30`, `Danger 100`. |

Each region has 1 dungeon entrance (`DungeonName`: *the Sunless Vaults*,
*the Weeping Warren*… — procedural names are local nicknames, not the branch's
"official" name) and towns with names like `Ashford`, `Grimhold`
(`TownName`: Ash/Gloom/Hollow/Ember + brook/ford/haven…). Lore: towns have
no history of their own because **they are all the same town rebuilt** —
people who fled one region and founded another with the same mold: wall,
square, fountain, smithy, tavern, temple, guards. With no spare terrain,
the towns grew up (towers, attics) and down (basements, crypts): whoever
founded a village beside the Ossuary learned to dig.

## Factions and peoples (who actually appears in the bestiary)

No faction with faction AI (the code only has `Hunt/Walk/Ambush/Guard`). The lore
does not promise a system that does not exist. These are cultures, not teams:

- **Those Who Stayed (dwarves and gnomes).** `dwarf`, `dwarf lord`, `gnome`,
  `gnome lord` — `Lawful`. They are not monsters: they are the Ossuary's last
  employees. Town guards use a dwarf body, shopkeepers use a hobbit body
  (`Town.cs`: `GuardShell`/`KeeperShell`). Lore: the town hires
  the short ones because the tall ones died down there.
- **The Starving (kobolds, orcs, jackals).** `ChaoticEvil`. Tribes of the
  Warrens and Craglands. `orc shaman` and `orc chieftain` are real leadership;
  a `kobold` with a `club` is a child with a stick. They do not hate you: they are hungry.
- **The Mold (molds, gas spore, lizard, spider).** `Neutral`, `mindless`.
  The Ossuary's biology. `brown/yellow/gray mold` explodes because that is how
  spores travel. Not evil: damp.
- **The Drowned (zombie, skeleton, mummy, wraith, lich, dread knight).**
  `Undead`. Each one is a layer: zombie = peasant drowned in the Vaults,
  skeleton = soldier of the Dungeons, `gnome mummy` = embalmed priest,
  `wandering wraith` = one who saw the Spire, `lich` + `dread knight` = those who
  ordered it built. They did not return by a generic curse: **nobody buried them
  properly because the cemetery became a dungeon.**
- **The Great (troll, ogre, giant, tiamat).** Hunger with size. `troll`
  regenerates because the Ossuary will not even let it die in peace. `tiamat`
  (level 30, 300 HP) is not a boss with lines: it is the bottom of the pit breathing.
- **The Surface Folk (hobbits, wood nymph, werenothing, stalker).**
  Hobbits run shops because they are the only ones who can still count.
  `wood nymph` and `stalker` are the wild trying to retake the mouth of the hole.

## The Amulet of Yendor

Current quest: an `amulet of Yendor` (`Tier 4`, `QuestItem`) waits on level
10 of The Dungeons; leaving the dungeon with it (`CheckVictory`) wins the run:
*"You emerge into the open air, the Amulet blazing against your chest."*

Minimal lore, no bloated prophecy:

- It is not a king's jewel. It is the **seal of the sunken archive** — whoever held it
  could open any door, vault and tomb of the old kingdom.
- That is why every dead thing in the Ossuary "recognizes" it and every living one wants it: shops
  would pay 5000 gold (`Cost`), but none has that gold (`ShopEconomy`).
- Yendor is not a god. He is the **archivist who stamped his own tomb** and
  descended with the seal so no one else would climb out. The drifter will prove he
  was wrong.

The closing line is already in the code — *"The Ossuary remembers."* It is the default
epitaph. Keep it.

## The protagonist: the drifter

No heroic origin. Rules:

- Starts with a dagger, leather, 3 rations, lock pick, 30 gold. That **is** the
  backstory: ex-city thief, ex-deserter soldier, ex-peasant — whatever,
  because they sold everything but this.
- Not chosen. They were the only one who accepted descending for 30 gold and a
  promise of 5000 that nobody can pay.
- F5 saves the run (the seed plus the keys). Lore: the drifter tells the same story every
  time they die, only the number changes. Permadeath (`GameOver`, any
  key restarts) is canon: **the Ossuary spits out another identical drifter.**
- They feel hunger (`Nutrient`), read a scroll without knowing (`r`), zap a
  wand (`z`), eat a corpse (`corpse`) when needed. It is not courage: it is
  necessity with a lantern.

## Beliefs, altars and landmarks

- **Altars and fountains** (`TileKind.Altar/Fountain`): each floor has the god
  of the age in which it was dug. Nobody knows the name, everyone drinks and prays
  anyway. The game effect rules; the lore does not explain.
- **Wayshrines** (`⛩`), **ruins** (`⌂`), **bridges** (`=`): landmarks of
  those who tried to map the Ossuary and gave up halfway. Generic names
  (`FeatureName`: *"an ancient ruin"*, *"a ruined keep"*) are deliberate:
  whoever named them died before finishing.
- **Day/night** (`AdvanceTime`): the surface still has sun. Down there,
  no. Traveling at night (`IsNight +6` encounter chance) is asking to be
  remembered by the Ossuary.

## How the lore shows up in the game (without breaking anything)

1. `Branch.EntryText` — 1 sentence per branch, already exists. Do not lengthen: it is the
   only guaranteed text the player reads.
2. Procedural names (`TownName`, `DungeonName`, `FeatureName`) — keep
   generators short and Anglo-Saxon. Do not put lore in a random string.
3. `Say()` — all player feedback goes through it. Lore enters as flavor
   in an existing message, never as a new mandatory panel.
4. Death and victory — `GameOver` and `Won` are the only "endings". The line
   *"The Ossuary remembers."* is the motto. Repeat, do not vary.
5. Future (when there is UI): `History` and `Discoveries` (`UiState`) are the
   natural place for a codex. New lore goes there first, not in a popup.

## Countries and powers (the political map)

The code's 9 regions are geography. Politics is 6 powers + 3 no-man's
lands. None rules the Ossuary — all of them pay people to descend.

1. **The League of the Reach (The Verdant Reach).** The last handful of villages
   that still harvest wheat. Government: a council of hobbit shopkeepers (the same
   `KeeperShell` as the 8 shops). Wealth: food and rope. Doctrine: "the hole
   is everyone's problem, so let a drifter die, not one of our children".
   They are the ones who give the starting 30 gold.
2. **The Iron Holds (The Iron Hills).** Dwarves and gnomes who sealed
   Dwarfdeep from the inside and today rent axes and guards (`dwarf` as
   `GuardShell`). They say they closed the mine out of honor; they closed it because the
   black water rose and the council voted 4 to 3 to drown the night shift.
3. **The Khanate of the Rift (The Craglands).** Orcs, kobolds, jackals. Not a
   horde: a confederation of hunger. `orc chieftain` charges road tolls,
   `orc shaman` marks those who may pass with ash. They hate the League less than
   they hate the Hollow — the Hollow eats even orcs.
4. **The Pact of the Ember (Emberdown).** Villages that traded tax for
   protection: they pay food to an `ogre lord` and call him king. Forges lit
   day and night to pay the tithe in `plate mail`. Ash is currency here.
5. **The Church of the Rope (Gallowmoor).** Priest-hangmen. They hanged
   Ossuary looters on the road; when the hanged came back as
   `zombie`, they declared that the rope "keeps the soul in the right body". Today
   they sell knots, shrouds and maps. The `wayshrine` (`⛩`) is their altar.
6. **The Hollow Throne (The Hollow Wastes).** Not a country: a queue. Pilgrims,
   lesser liches, deserter `dread knight`s and people who heard `tiamat`
   breathe and found it beautiful. They have no capital, they have a direction: down.

No-man's lands (no throne, with history):

- **Ashen Marches** — a field of ash between League and Pact. Veil nomads
  (ex-soldiers of the Spire) guide by water. Rule: never camp in the wind.
- **The Sunken Vale** — canals over the Vaults. Boatmen charge per
  stroke and for silence. They say you can hear the archive stamping.
- **Whisperfen** — an oracle swamp. Nobody governs because nobody comes back
  the same. `floating eye` is a messenger; killing one is declaring war on the swamp.

## Unique characters (playable canon)

Rule: each uses a body that **already exists** in the `Bestiary`. Becoming "unique"
costs only a name + 1 line + 1 guaranteed drop. No new system.

**Below (dungeon):**

1. **Yendor, the Archivist (`lich`, The Dungeons:10).** Neither king nor god:
   he was the stamper of the sunken archive. He descended with the seal (`amulet of
   Yendor`) so no king would climb out with him. Line on seeing the amulet on the
   floor: *"You too came to stamp your own grave?"* Drop: the amulet
   itself. He does not guard the amulet — he dropped it and stayed watching.
2. **Marrow, the Guard of the Deep (`guardian of the deep`, Dungeons:9–10).**
   The last soldier of the old kingdom still keeping her shift. `Guard`, does not
   chase far from her post. Line: *"Post held. Pass with the seal or
   go back with the others."* She lets pass whoever has the amulet — the final
   tutorial nobody wrote.
3. **Durek Cold-Hammer (`dwarf lord`, Mines:8).** The foreman who sealed
   Dwarfdeep. He stayed `Lawful` even after death: still checks helmets.
   Drop: a named `war hammer`. Line: *"The night shift stayed inside.
   I stayed with them."*
4. **The Drowned Accountant (`gnome mummy`, Vaults:10–12).** Treasurer of the
   sunken bank, mummified with her ledger. `mindless`, but hugs whoever
   carries gold. Drop: `gem` + a mental map of the vault (flavor, not a new item).
5. **The Ash Lord (`dread knight`, Spire:12–14).** The general who ordered the
   Spire lit to "burn the dead". He burned the living and was promoted.
   `Predator`, mounted on a horse that no longer exists. Line:
   *"The order was ash. I only obeyed to the end."*
6. **The White Worm (`tiamat`, Spire:15 / Wastes).** Does not speak. It is the bottom of the
   pit breathing (`regen`, `flys`). The Cult of the Hollow worships it; the orcs
   measure it in hunger ("eats three villages a year"). Meeting it is the end
   point of the local religion.
7. **Vex the Squint (`orc shaman`, Warrens/Craglands:5–8).** A shaman who marks
   travelers with ash so the Khanate lets them pass. In the game: the first
   `shaman` that `Say()`s instead of only attacking — *"Ash on the brow or tooth on
   the ground."* Drop: `club` + 1 pass (flavor for an overworld encounter).
8. **Mother Rust (`ogre lord`, Emberdown encounters).** Collects the Pact's toll.
   Does not kill whoever pays; eats the mule of whoever does not. Line:
   *"The king eats first. I am the king."*

**Above (surface and towns):**

9. **Little Sorrel (`hobbit` shopkeeper, any town).** The network of 8
   shops is hers: `the Gilded Supply`, `the Rusty Depot` — procedural
   names (`ShopNameFor`) are branches. She never leaves the counter
   (`Altar` as a counter). Line: *"Everything has a price. Yendor's seal too,
   but you have nowhere to spend it."*
10. **Captain Bell (`dwarf` guard, gates).** Chief of the `town guard`.
    `Dormant` until provoked; when he wakes, he rings (a bell, not a new item).
    Knows the name of every drifter because he buried the previous version.
    Line: *"New seed? New grave. Good descent."*
11. **Sister Gallows (`gnome lord`, Gallowmoor).** Priestess of the Rope who
    blesses knots and sells shrouds as armor (`ring mail` lore). Line:
    *"If you come back, bring the teeth. The Ossuary already has the bones."*
12. **The Whisperer (`wood nymph` / `floating eye`, Whisperfen).** An oracle
    who only answers with what the wind brought: names of the dead from your
    previous run (hook for a per-seed epitaph). Never lies, never helps
    twice. Line: *"Ask the swamp. It remembers you."*

## Full history in 7 ages (for a future codex)

1. **Foundation (before the count).** First pit, first crypt. Each
   village buries its own. The Ossuary is still plural: ossuaries.
2. **The Archive (Age of Yendor).** The old kingdom centralizes seals, vaults and
   tombs. Yendor stamps everything. `The Dungeons` are built as the palace
   basement: prison, barracks, a maze to throw off thieves.
3. **The Iron (Age of Dwarfdeep).** Dwarves open the Mines, get rich,
   close the gates. The surface learns `axe`, `pick-axe`, chain mail.
   First real roads (`Road`).
4. **The Sinking.** Black water — sewage? a god? a mine leak? —
   takes archives and temples. `The Sunken Vaults` is born. The bank sinks with
   the Accountant inside. Saying: "the Ossuary does not give back".
5. **The Hunger (Age of the Warrens).** With the archive dead and the mine closed,
   those who stayed ate those who passed. Kobolds, rats, spiders take the service
   tunnels. `The Warrens` are gnawed, not built.
6. **The Spire.** The Ash Lord raises the inverted tower to incinerate the
   dead all at once. Warm stone (`"warm, and it should not be"`), ash
   over two regions, `fire giant` awake. The White Worm descends drawn by
   the heat and stays.
7. **The League (present).** What remains are villages, tolls, ropes and shopkeepers. The
   hobbit council funds drifters at 30 gold a head because a real
   hero costs more than the amulet is worth. You are the 1000th attempt.
   F5 proves it: same seed, same pit.

## Open hooks (updated)

- [x] Who lit the Spire? → The Ash Lord. Only the in-game line is missing.
- [x] Why did Dwarfdeep seal? → Durek sealed it with the night shift inside.
- [x] Was Yendor a person? → Yes: the archivist. A tomb on level 10 settles it.
- [ ] Black water: god, sewage or mine? (leave ambiguous on purpose.)
- [ ] Procedural epitaph per death using the seed (The Whisperer recites it).
- [ ] 1 line per unique (`Say` on first sighting) — cheap, strong.
