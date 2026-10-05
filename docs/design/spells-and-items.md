# Spells, items and animations

This document describes the catalog of spells, books, magic items and unique items, and the **animation**
system that makes every spell show up in the world (the fireball *is* a fireball that flies and explodes).
The tables below were generated from the code; the source of truth is the files cited in each section.

## In numbers

| | |
|---|---|
| Spells | **322** in **8 schools** (were 39 in 6) |
| Books | **55** (depth tiers 1–5) |
| Wands / scrolls / potions that cast spells | **38 / 34 / 18** |
| Random magic items **imbued with a spell that fits the item type** | weapons, armor, helms, gloves, boots, cloaks, shields, rings and amulets |
| New weapons / armors / helms / gloves / boots / cloaks / shields | 37 / 19 / 14 / 11 / 9 / 10 / 9 |
| New rings / amulets (now **wearable**) | 33 / 22 |
| New affixes | 54 (30 prefixes, 24 suffixes) |
| New unique items | **43**, 4 new sets, 38 of them lend spells |
| New summonable creatures | 26 |
| Animation effects | 23 pieces (`FxLib`) |

## Animations

The engine resolves the spell **immediately** and only *records* how it should look; the frontend plays the recording over the
finished frame, without asking the engine for a turn (same idea as the animated water).

```
Game.CastSpell ─▶ PlaySpellFx / Fx(...)   records FxTimeline (steps of map cells)
Session.Draw   ─▶ BuildFx                 map → screen (camera, square tiles, theme), Frame.Fx
protocol.ts    ─▶ Frame.fx / fxMs         one array per step: [cell, glyph, fg, bg] × N  (bg −1 = keep)
main.ts        ─▶ createFxPlayer          plays ~45 ms per step; a new key cuts it; prefers-reduced-motion turns it off
```

- **Pure data** (`Fx.cs`): no Rng and no game state; recording never changes what happens (saves are replays).
  Effects use `Shapes.Hash(x, y, salt)` to vary the drawing. `Game.FxEnabled` (turned on by `Session` outside replays)
  avoids cost in tests and *soak*.
- **Shared geometry** (`Shapes`): the explosion covers exactly the Chebyshev square the damage hits, the cone is the
  same set of cells as the damage cone. What you see is what was hit.
- **Colors** come from per-element ramps (`Elem`: Arcane, Fire, Cold, Lightning, Necrotic, Holy, Poison, Nature, Shadow, Earth,
  Blood, Water, Mind, Wind) and go through `Theme.Remap`, so the amber/phosphor themes work too.
- **Visibility**: only cells in view are drawn (`FxTimeline.Show`); under an open panel the animation is discarded.
- **Vocabulary** (`FxLib`; returns the step on which it ends): `Bolt` (projectile with trail, directional glyph with `'\0'`),
  `Beam`, `Zap` (jagged ray), `Chain`, `Drain`, `Wave`, `Cone`, `Burst`, `Nova`, `Implode`, `Cloud`, `Rain`, `Eruption`,
  `Shatter`, `Flash`, `Slash`, `Rise`, `Swirl`, `Pillar`, `Meteor`, `Mark`, `Teleport`, `Summon`.
- **Each spell declares its look**: `.Look(FxKind.Ball, Elem.Fire, 'o')`. `FxKind.Custom` = the code records it itself
  (chains and rains of strikes). Summons record `Summon` on every occupied cell.
- **Beyond spells**: wands, scrolls and potions (they are spells), bow shots, molotovs, fire/alarm/teleport traps,
  monster explosions and the boss attacks (Stone Warden's slam, Drowned King's flood and lightning, Annex Warden's drain,
  Ashen Regent's blast, Gaoler's chain).
- **See it without a window**: `headless.ps1 fx fireball` prints every step as ASCII; `fx list` lists the animations; `fx all` counts the steps of all of them.
- **Known limits**: the spell is already resolved when the animation plays (a dead enemy disappears before the projectile arrives); the drawing
  covers the cell's glyph (monsters and the hero are hidden for an instant). A new key cuts a running animation.

## How a spell is defined (`Magic/Spells*.cs`)

A spell is a line of data with a recipe (`SpellDef`); most need no new code.

```csharp
S("fireball", "Fireball", 3, E, 7, Area, 8, "Bursts where you aim…", 2).Look(FxKind.Ball, Elem.Fire, 'o')          // own code (legacy)
S("cone-of-cold", "Cone of Cold", 4, E, 10, Cn, 5, "A cone of killing frost…", 5)
    .Dmg(Cold, 4, 6).Ride(Rider.Slow, 70, 8).Look(FxKind.Cone, Elem.Cold)                                       // recipe
```

| Piece | What it does |
|---|---|
| `Dmg(type, dice, sides, div, fixed, verb)` | damage `(dice + level/div)d sides + Magic rank`; physical, fire, cold, lightning, poison, necrotic or holy |
| `Ride(rider, %, turns)` | effect on those hit: **Burn, Slow, Fear, Sleep, Confuse, Blind, Stun, Root, Poison, Bleed, Weaken, Charm** (mind and body ones can be resisted) |
| `Heal(d, s, fixed)` | healing (plus Wisdom and level) |
| `Aura(buff, turns)` | buff in `SpellBuffs` (stats, AC, resistances, extra die, lifesteal, heal per turn, retaliation) |
| `Call(creature, n, turns)` | summons temporary allies |
| `Surf(surface, turns)` | water, ice, fire, oil, grass |
| `Shove(n)` | pushes (n > 0) or pulls (n < 0); hitting a wall hurts |
| `Drain(%)` | heals part of the damage dealt |
| `Chain(jumps)` / `Scatter(hits, animation)` | chain of bolts / hits on random targets in view |
| `Taint(n)` | costs corruption |
| `Spec("name")` | code effect: blink, swap places, banish, light, find traps, pick locks, identify, dig, execute, assassinate, coup de grace, steal, disarm, remove curse, restoration, mass heal, command undead, speak with animals… |

**Shapes** (derived from the target): `Single`, `Ball` (radius around a cell), `Line` (pierces), `Cone` (new target: aim a direction),
`Nova` (around you), `Chain`, `Scatter`. **New riders** on monsters (`HeldTurns` loses its turn, `DotTurns`/`DotDmg` damage over
time, `VulnTurns` takes +25%) live in `Monster` and are applied in `Game.Magic.Recipes.cs`.

**Buffs** (`SpellBuffs.cs`, 47 data lines): they are temporary `ItemMods` summed into `Player.Gear` (so attributes, HP, Mp, resistances,
stealth, spell power, lifesteal and a weapon's extra die work as on items and return to normal when they end), plus heal per turn
(`Regen`) and retaliation (`Retaliate`: thorns, holy aura, death aura). *Sanctuary* makes monsters hesitate 70% of the time; *Sense Life*
shows creatures through walls.

**Panel** (`Shift+Z`): school tabs (`←`/`→`), per-school count, sorted by level; letters a–z choose within the tab;
`PageUp`/`PageDown`/`Home`/`End` scroll; `◆` marks spells lent by items. Description in up to two lines, target, damage and effect.

**Failure and focus**: `FailPct` now also subtracts `ItemMods.SpellFocus`; `SpellPower` adds damage to every spell.

## Spell catalog

Each school belongs to a character type (anyone can read any book, but each one's first book comes in the pack):
**Wizard** (Evocation, Conjuration, Alteration, Illusion), **Necromancer** (Necromancy), **Cleric and Paladin** (Sacred),
**Ranger** (Nature, starting book *a druid's handbook*) and **Rogue** (Shadow, starting book *a cutpurse's primer*).

### Evocation — 39 spells (Wizard)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Acid Splash** (`acid-splash`) | 3 | creature | Bolt Poison | A glob of acid. Eats armour: the target takes more from everything for a while. |
| 1 | **Burning Hands** (`burning-hands`) | 3 | cone | Cone Fire | A fan of flame from your fingertips, three cells long. |
| 1 | **Ember Dart** (`ember-dart`) | 2 | creature | Bolt Fire | A mote of flame. Sets the target alight, now and then. |
| 1 | **Flame Arrow** (`flame-arrow`) | 3 | creature | Bolt Fire | An arrow of fire that never needs a bow. May set the target alight. |
| 1 | **Frostbite** (`frostbite`) | 2 | creature | Bolt Cold | A bite of cold that slows the target. |
| 1 | **Magic Missile** (`magic-missile`) | 2 | creature | Bolt Arcane | Unerring bolts of force. Dice grow with level. |
| 1 | **Shocking Grasp** (`shocking-grasp`) | 2 | creature | Zap Lightning | Lightning through your touch. Adjacent only; hits hard. |
| 1 | **Spark** (`spark`) | 2 | creature | Zap Lightning | A snap of lightning. Quick and cheap. |
| 2 | **Forked Lightning** (`forked-lightning`) | 5 | creature | Custom Lightning | Lightning that forks to one more creature nearby. |
| 2 | **Frost Ray** (`frost-ray`) | 4 | creature | Beam Cold | A beam of cold. Heavy damage, needs a clear line. |
| 2 | **Gust of Wind** (`gust-of-wind`) | 3 | cone | Wave Wind | A blast of air that batters and shoves things back two cells. |
| 2 | **Ice Lance** (`ice-lance`) | 4 | creature | Bolt Cold | A spear of ice. Chills the target and freezes any water it stands in. |
| 2 | **Scorching Ray** (`scorching-ray`) | 5 | creature | Beam Fire | A needle of white fire. Sets the target alight. |
| 2 | **Stone Shard** (`stone-shard`) | 4 | creature | Bolt Earth | A jagged stone flung hard. |
| 2 | **Thunderclap** (`thunderclap`) | 4 | self | Nova Wind | A deafening clap: everything within 2 cells is hurt and may be stunned. |
| 2 | **Thunderstrike** (`thunderstrike`) | 5 | creature | Pillar Lightning | A bolt from a clear sky. It may stun. |
| 3 | **Arc Flash** (`arc-flash`) | 6 | cone | Cone Lightning | A fan of lightning four cells long that may stun. |
| 3 | **Fireball** (`fireball`) | 7 | area | Ball Fire | Bursts where you aim, burning everything within 2 cells. |
| 3 | **Ice Comet** (`ice-comet`) | 7 | area | Meteor Cold | A comet of ice that slows what it does not kill. |
| 3 | **Lava Bolt** (`lava-bolt`) | 7 | creature | Bolt Fire | A glob of molten rock. Burns, and leaves flames on the floor where it lands. |
| 3 | **Lightning Bolt** (`lightning-bolt`) | 6 | line | Zap Lightning | A bolt that pierces every creature along its line. |
| 3 | **Poison Cloud** (`poison-cloud`) | 6 | area | Cloud Poison | A foul green cloud. Everything in it is poisoned for a few turns. |
| 3 | **Searing Orb** (`searing-orb`) | 7 | area | Ball Fire | A small sun that bursts on contact, 1 cell wide. |
| 3 | **Steam Burst** (`steam-burst`) | 6 | area | Cloud Water | Boils water into scalding steam: heavy damage to anything wet within 2 cells, and the water is gone. Dry ground only hisses. |
| 4 | **Arcane Barrage** (`arcane-barrage`) | 9 | self | Custom Arcane | Four missiles of force, each finding a creature in view. |
| 4 | **Call Lightning** (`call-lightning`) | 9 | self | Custom Lightning | Three bolts fall from nowhere on creatures in view. |
| 4 | **Chain Lightning** (`chain-lightning`) | 10 | creature | Custom Lightning | Strikes a target, then leaps to up to three more nearby. |
| 4 | **Cone of Cold** (`cone-of-cold`) | 10 | cone | Cone Cold | A cone of killing frost, five cells long. Chills what survives. |
| 4 | **Ice Storm** (`ice-storm`) | 10 | area | Rain Cold | Hail the size of fists, in a wide patch. |
| 4 | **Magma Wave** (`magma-wave`) | 10 | cone | Cone Fire | A wave of molten rock, five cells long, that sets everything alight. |
| 4 | **Prismatic Spray** (`prismatic-spray`) | 10 | cone | Cone Mind | A fan of coloured light: hurts and dazes. |
| 4 | **Static Field** (`static-field`) | 9 | self | Nova Lightning | Lightning crawls over everything within 3 cells of you, and may stun it. |
| 4 | **Wall of Fire** (`wall-of-fire`) | 9 | area | Eruption Fire | A burning cross on the floor. Spreads through brush and oil; creatures in it catch fire. |
| 5 | **Disintegrate** (`disintegrate`) | 16 | creature | Beam Poison | A green ray that unmakes what it touches. |
| 5 | **Earthquake** (`earthquake`) | 15 | self | Eruption Earth | The ground heaves for 4 cells around you. Hurts and stuns everything standing on it. |
| 5 | **Meteor** (`meteor`) | 15 | area | Meteor Fire | A burning rock from nowhere. Devastates 3 cells around. |
| 5 | **Rimeblast** (`rimeblast`) | 14 | self | Nova Cold | A blast of killing frost 5 cells around you that slows what lives. |
| 5 | **Sun Lance** (`sun-lance`) | 15 | line | Beam Holy | A lance of white-hot light through everything in a line. |
| 5 | **Sunburst** (`sunburst`) | 16 | area | Meteor Holy | A sun falls: it scours everything within 3 cells and blinds what it does not kill. |

### Conjuration — 26 spells (Wizard)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Create Oil** (`create-oil`) | 3 | area | Cloud Shadow | Slicks the floor around a spot with oil. It burns long and slides the unwary. Mind your torches. |
| 1 | **Familiar** (`familiar`) | 3 | self | — | Calls a small beast to fight for you for a time. |
| 1 | **Spectral Blade** (`spectral-blade`) | 3 | self | — | A sword of cold light fights beside you for a while. |
| 2 | **Create Water** (`create-water`) | 4 | area | Rain Water | Floods the floor around a spot. The wet burn less and conduct lightning; frost turns it to ice. |
| 2 | **Fog Cloud** (`fog-cloud`) | 4 | self | Cloud Wind | A thick fog around you: foes within 2 cells grope blindly, and you are harder to hit. |
| 2 | **Phase Step** (`phase-step`) | 3 | cell | Teleport Arcane | A short hop through the walls of the world. |
| 2 | **Summon Beast** (`summon-beast`) | 5 | self | — | Calls a stronger beast as your level grows. |
| 2 | **Summon Imp** (`summon-imp`) | 5 | self | — | A small, spiteful imp serves you for a while. |
| 2 | **Summon Swarm** (`summon-swarm`) | 5 | self | — | A cloud of bats answers your call. |
| 2 | **Web** (`web`) | 4 | area | Cloud Wind | Sticky strands in a small patch. Whatever they touch is stuck for a few turns. |
| 3 | **Arcane Hound** (`arcane-hound`) | 7 | self | — | A hound of purple fire runs at your side. |
| 3 | **Blink** (`blink`) | 5 | cell | Teleport Arcane | Step through space to a spot you can see. |
| 3 | **Frozen Ground** (`frozen-ground`) | 5 | area | Cloud Cold | Ice spreads across the floor, and what stands on it slows. |
| 3 | **Mirror Image** (`mirror-image`) | 6 | self | — | Two duplicates of you flicker beside you, drawing blows meant for you. |
| 3 | **Pull Through** (`pull-through`) | 6 | creature | Beam Mind | A hand through the world drags the target to your side. |
| 3 | **Storm Sprite** (`storm-sprite`) | 7 | self | — | A crackling sprite darts at your enemies. |
| 3 | **Swap Places** (`swap-places`) | 6 | creature | Teleport Mind | You and the target trade places in a blink. |
| 4 | **Banish** (`banish`) | 10 | creature | Implode Shadow | Throws the target away to some other part of the level. |
| 4 | **Dimension Door** (`dimension-door`) | 8 | cell | Teleport Arcane | A long step through the walls of the world. |
| 4 | **Stone Sentinel** (`stone-sentinel`) | 10 | self | — | A sentinel of carved stone stands guard over you. |
| 4 | **Summon Air Elemental** (`summon-air-elemental`) | 10 | self | — | A whirling air elemental answers you. |
| 4 | **Summon Earth Elemental** (`summon-earth-elemental`) | 10 | self | — | An earth elemental pulls itself from the floor to serve you. |
| 4 | **Summon Fire Elemental** (`summon-fire-elemental`) | 10 | self | None Fire | A fire elemental claws up out of nothing to serve you. |
| 4 | **Summon Water Elemental** (`summon-water-elemental`) | 10 | self | — | A water elemental rises, dripping, to serve you. |
| 4 | **Teleport** (`teleport`) | 9 | self | — | Throws you to a random place on this level. |
| 5 | **Gargoyle Guard** (`gargoyle-guard`) | 14 | self | — | A winged gargoyle unfolds from nothing to guard you. |

### Alteration — 31 spells (Wizard)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Find Traps** (`detect-traps`) | 2 | self | Nova Earth | Shows every trap within 10 cells. |
| 1 | **Knock** (`knock`) | 2 | self | Swirl Arcane | Unlocks and opens every locked door within 3 cells. |
| 1 | **Light** (`light`) | 2 | self | Nova Holy | A burst of light shows the ground about you. |
| 1 | **Lightfoot** (`lightfoot`) | 2 | self | Swirl Wind | Your step goes quiet: harder to notice, a little harder to hit. |
| 1 | **Mage Armor** (`mage-armor`) | 3 | self | Swirl Arcane | Plates of unseen force: AC +4 for a long while. |
| 1 | **Shield** (`arcane-shield`) | 2 | self | Swirl Arcane | A disc of force in front of you: AC +6, but only for a few turns. |
| 1 | **Ward** (`ward`) | 2 | self | Swirl Arcane | A shimmering shield: AC +3 for a while. |
| 2 | **Blur** (`blur`) | 4 | self | Swirl Wind | Your outline smears: evasion +4 for a while. |
| 2 | **Bull's Strength** (`bulls-strength`) | 4 | self | Swirl Blood | Strength +3 for a long while. |
| 2 | **Cat's Grace** (`cats-grace`) | 4 | self | Swirl Nature | Dexterity +3 and a little evasion, for a long while. |
| 2 | **Endure Elements** (`endure-elements`) | 4 | self | Swirl Water | Fire and cold 30%, lightning 20%, for a long while. |
| 2 | **Fire Ward** (`fire-ward`) | 3 | self | Swirl Fire | Fire resistance 60% for a while. |
| 2 | **Fox's Cunning** (`foxs-cunning`) | 4 | self | Swirl Mind | Intelligence +3 for a long while. Your mana pool grows with it. |
| 2 | **Frost Ward** (`frost-ward`) | 3 | self | Swirl Cold | Cold resistance 60% for a while. |
| 2 | **Identify** (`identify`) | 4 | self | Swirl Mind | Everything you carry and wear shows what it is. |
| 2 | **Keen Edge** (`keen-edge`) | 4 | self | Swirl Wind | Your weapon finds its mark: +3 to hit, +1 damage. |
| 2 | **Owl's Wisdom** (`owls-wisdom`) | 4 | self | Swirl Holy | Wisdom +3 for a long while. Your mana pool grows with it, if you cast by Wisdom. |
| 2 | **Sense Life** (`detect-monsters`) | 3 | self | Nova Mind | For a while you feel every living thing within 12 cells, through walls. |
| 2 | **Storm Ward** (`storm-ward`) | 3 | self | Swirl Lightning | Lightning resistance 60% for a while. |
| 3 | **Aegis** (`aegis`) | 6 | self | Swirl Arcane | A ward against everything: AC +3 and 15% resistance to fire, cold and lightning. |
| 3 | **Arcane Focus** (`arcane-focus`) | 5 | self | Swirl Arcane | Your spells hit harder (+3) and fizzle less (-10%) for a while. |
| 3 | **Arcane Survey** (`arcane-survey`) | 6 | self | Nova Arcane | You see the shape of the land and every trap in it, 14 cells around. |
| 3 | **Clairvoyance** (`clairvoyance`) | 7 | self | Nova Mind | The whole level unfolds in your mind. |
| 3 | **Dig** (`dig`) | 6 | line | Beam Earth | Bores through up to six cells of diggable rock in a line. |
| 3 | **Haste** (`haste`) | 6 | self | Swirl Lightning | You act twice as often as everything else, briefly. |
| 3 | **Levitate** (`levitate`) | 5 | self | Rise Wind | You float off the floor: traps cannot reach you. |
| 3 | **Slow** (`slow`) | 5 | creature | Mark Water | Halves a creature's speed for a time. Strong ones resist. |
| 4 | **Enlarge** (`enlarge`) | 9 | self | Swirl Blood | You grow to half again your size: Str +4, Con +3, +15 HP. |
| 4 | **Iron Body** (`iron-body`) | 10 | self | Swirl Earth | You are, briefly, a statue that moves: AC +5, poison 40%, a little slower to dodge. |
| 4 | **Stone Skin** (`stone-skin`) | 9 | self | Swirl Earth | Your skin hardens: AC +6 for a long while. |
| 5 | **Titan's Might** (`titans-might`) | 14 | self | Swirl Blood | Str +6, Con +4, +25 HP for a good while. |

### Illusion — 20 spells (Wizard)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Color Spray** (`color-spray`) | 3 | cone | Cone Mind | A fan of dizzying colours, three cells long. |
| 1 | **Dazzle** (`dazzle`) | 2 | creature | Mark Holy | A flash that leaves the target blundering blind. |
| 2 | **Befuddle** (`befuddle`) | 3 | area | Cloud Mind | A whorl of nonsense around a spot: creatures in it stagger. |
| 2 | **Confuse** (`confuse`) | 4 | creature | Mark Mind | The target staggers about at random. |
| 2 | **Displacement** (`displacement`) | 4 | self | Swirl Mind | Your image is a hand's breadth off from where you stand. |
| 2 | **Invisibility** (`invisibility`) | 5 | self | Swirl Shadow | Foes lose track of you and strike at you worse. |
| 2 | **Lullaby** (`lullaby`) | 4 | cone | Cone Mind | A hum three cells long that may put the living to sleep. |
| 2 | **Phantom Guard** (`phantom-guard`) | 4 | self | — | A duplicate of you flickers at your side and takes the blows meant for you. |
| 2 | **Sleep** (`sleep`) | 4 | creature | Mark Mind | Puts a living foe to sleep. Strong ones resist; damage wakes it. |
| 2 | **Suggestion** (`suggestion`) | 5 | creature | Mark Mind | A whispered idea: the target serves you for a short while. |
| 3 | **Bewilder** (`bewilder`) | 6 | cone | Cone Mind | A cone of nonsense: everything in it staggers. |
| 3 | **Hypnotic Pattern** (`hypnotic-pattern`) | 7 | area | Cloud Mind | A weaving of light that puts creatures within 2 cells to sleep. |
| 3 | **Mind Fog** (`mind-fog`) | 7 | area | Cloud Mind | A fog on the mind: those in it fight poorly and take more from everything. |
| 3 | **Terrify** (`terrify`) | 6 | self | Nova Shadow | Every creature within 4 cells sees its worst fear. |
| 4 | **Charm Monster** (`charm`) | 10 | creature | Mark Mind | A living foe fights for you for a while. Hard on strong ones. |
| 4 | **Deep Slumber** (`deep-slumber`) | 9 | creature | Mark Mind | A sleep that is very hard to shake. |
| 4 | **Nightmare** (`nightmare`) | 9 | creature | Mark Shadow | A waking nightmare: hurts, and the target flees in terror. |
| 4 | **Phantasmal Killer** (`phantasmal-killer`) | 10 | creature | Mark Mind | The target's own fear takes shape and strikes. |
| 5 | **Mass Charm** (`mass-charm`) | 16 | area | Cloud Mind | A wave of affection for you: every creature within 3 cells may turn on its friends. |
| 5 | **Spectral Horror** (`spectral-horror`) | 15 | area | Cloud Shadow | A horror made of the target's own dread: hurts, and sends everything within 3 cells running. |

### Necromancy — 50 spells (Necromancer)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Bone Shard** (`bone-shard`) | 3 | creature | Bolt Earth | A splinter of bone flung with a flick. |
| 1 | **Chill Touch** (`chill-touch`) | 2 | creature | Slash Necrotic | A grave-cold hand. Hurts, and the target hits worse for a while. |
| 2 | **Bone Hound** (`bone-hound`) | 5 | self | — | A hound of knit bones runs at your heel. |
| 2 | **Curse of Weakness** (`curse-of-weakness`) | 4 | area | Cloud Shadow | A muttered curse on everything around a spot: they take more from every blow. |
| 2 | **Death Grip** (`death-grip`) | 4 | creature | Beam Necrotic | A cold hand pulls the target in and wounds it. |
| 2 | **Enfeeble** (`enfeeble`) | 4 | creature | Beam Necrotic | A grey ray saps the target: it takes more from every blow for a good while. |
| 2 | **Grave Chill** (`grave-chill`) | 4 | self | Swirl Necrotic | Graveyard cold wraps you: necrotic 50%, cold 30%. |
| 2 | **Grave Hands** (`grave-hands`) | 3 | area | Eruption Necrotic | Hands claw up out of the floor and hold what stands there. |
| 2 | **Life Tap** (`life-tap`) | 1 | self | Implode Blood | Burns 8 of your HP into 8 mana. |
| 2 | **Soul Bolt** (`soul-bolt`) | 4 | creature | Bolt Necrotic | A bolt of stolen soul; a third of it comes back to you. |
| 2 | **Vampiric Touch** (`vampiric-touch`) | 4 | creature | Drain Blood | A touch that takes it all back: you heal what you deal. |
| 2 | **Wither** (`wither`) | 4 | creature | Bolt Necrotic | Rots a limb. Damages and slows. |
| 3 | **Animate Ghoul** (`animate-ghoul`) | 7 | self | — | A ghoul lurches up to serve you. It is always hungry. |
| 3 | **Blight** (`blight`) | 7 | creature | Bolt Poison | Rots the target from the inside: poison, and it takes more from everything. |
| 3 | **Bloodlust** (`bloodlust`) | 6 | self | Swirl Blood | Red heat behind the eyes: +2 to hit, +3 damage, a little life steal. |
| 3 | **Bone Spear** (`bone-spear`) | 7 | line | Beam Earth | A lance of bone that pierces every creature in a line. |
| 3 | **Command Undead** (`command-undead`) | 8 | creature | Mark Necrotic | An undead creature bends its knee to you for a while. |
| 3 | **Dark Pact** (`dark-pact`) | 4 | self | Swirl Blood | Spell power +4 for a good while. The Ossuary takes 6 points of corruption for it. |
| 3 | **Death Aura** (`death-aura`) | 7 | self | Swirl Necrotic | A shroud of cold death: those who strike you take 1d6 necrotic. |
| 3 | **Drain Life** (`drain-life`) | 5 | creature | Drain Necrotic | Steals life: damages the target, heals you by half. Not the dead. |
| 3 | **Dread** (`dread`) | 7 | self | Nova Shadow | A wave of dread: every creature within 5 cells may flee. |
| 3 | **Feign Death** (`feign-death`) | 6 | self | Cloud Necrotic | You go still and cold. Everything nearby loses track of you. |
| 3 | **Gravebind** (`gravebind`) | 6 | area | Eruption Necrotic | Dead hands grab at the floor-bound: creatures within 2 cells are held. |
| 3 | **Hemorrhage** (`hemorrhage`) | 6 | creature | Bolt Blood | Opens the target's veins from across the room: it bleeds for a while. |
| 3 | **Miasma** (`miasma`) | 6 | area | Cloud Poison | A foul cloud that sickens and weakens. |
| 3 | **Ossify** (`ossify`) | 5 | self | Swirl Earth | Bone creeps over your skin: AC +4 for a while. The Ossuary takes a little of you for it. |
| 3 | **Plague Bolt** (`plague-bolt`) | 6 | creature | Bolt Poison | A greasy green bolt. The target sickens for a while. |
| 3 | **Raise Skeleton** (`raise-skeleton`) | 6 | self | — | A skeleton claws out of the floor to serve you. |
| 3 | **Reshape Flesh** (`reshape-flesh`) | 8 | self | Implode Blood | Asks the Ossuary for a gift. You get a mutation, and it gets a share of you. |
| 3 | **Rotting Burst** (`rotting-burst`) | 7 | area | Ball Necrotic | A bubble of rot bursts around a spot. |
| 3 | **Skull Barrage** (`skull-barrage`) | 7 | self | Custom Necrotic | Three screaming skulls fly at creatures in view. |
| 3 | **Unholy Vigor** (`unholy-vigor`) | 6 | self | Swirl Necrotic | Your blows carry grave-cold (+1d4 necrotic) and feed you (15% life steal). |
| 4 | **Banshee Wail** (`banshee-wail`) | 9 | self | Nova Shadow | A scream that hurts and sends the living running. 4 cells. |
| 4 | **Bone Cage** (`bone-cage`) | 9 | area | Eruption Earth | Ribs of bone close around everything within 2 cells. |
| 4 | **Bone Golem** (`bone-golem`) | 11 | self | — | A golem of a hundred skeletons, wired together, answers you. |
| 4 | **Cloudkill** (`cloudkill`) | 9 | area | Cloud Poison | A killing green fog three cells across. |
| 4 | **Contagion** (`contagion`) | 9 | area | Cloud Poison | A sickness that sticks. Everything near the spot is poisoned for long. |
| 4 | **Death Wave** (`death-wave`) | 10 | self | Nova Necrotic | A ring of death sweeps 3 cells out from you. |
| 4 | **Fear** (`fear`) | 8 | creature | Mark Shadow | The target flees in terror. Mindless things do not fear. |
| 4 | **Marrow Bolt** (`marrow-bolt`) | 7 | creature | Bolt Necrotic | A spike of grave-cold marrow. Hits hard; the Ossuary takes a little of you each time. Not the dead. |
| 4 | **Raise Wight** (`raise-wight`) | 10 | self | — | A wight in rotted mail steps out of the dark and kneels. |
| 4 | **Siphon Soul** (`siphon-soul`) | 10 | creature | Drain Necrotic | Pulls a piece of soul out of the target and into you. |
| 5 | **Army of Bones** (`army-of-bones`) | 14 | self | — | Three skeletons rise to guard you. |
| 5 | **Finger of Death** (`finger-of-death`) | 15 | creature | Beam Necrotic | Unmakes the living with a point of the hand. |
| 5 | **Lich Form** (`lich-form`) | 14 | self | Swirl Necrotic | For a time you are most of the way to dead: strong against the grave, cold and poison, and a drinker of life. The Ossuary takes 3 points. |
| 5 | **Power Word Kill** (`power-word-kill`) | 18 | creature | Mark Necrotic | One word. Whatever is badly hurt dies; whatever is not takes a great blow. |
| 5 | **Ritual of Blood** (`ritual-of-blood`) | 8 | self | Implode Blood | Cut yourself for a quarter of your life; take your mana back to the brim. |
| 5 | **Soul Reap** (`soul-reap`) | 15 | self | Nova Necrotic | Everything living within 5 cells gives up a piece of its soul; you keep a quarter of it. |
| 5 | **Summon Wraith** (`summon-wraith`) | 14 | self | — | A wraith, bound to you for a time, comes through the wall. |
| 5 | **Wail of the Damned** (`wail-of-the-damned`) | 15 | self | Nova Shadow | A scream from the pit hurts and sends everything within 6 cells running. |

### Sacred — 51 spells (Cleric and Paladin)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Bless** (`bless`) | 3 | self | Swirl Holy | A steadier hand: +2 to hit for a long while. |
| 1 | **Command** (`command`) | 2 | creature | Mark Holy | A single word, in a voice not yours: 'flee'. |
| 1 | **Cure Wounds** (`cure-wounds`) | 3 | self | Rise Holy | Mends your flesh: 2d6 plus Wisdom and level. |
| 1 | **Holy Light** (`holy-light`) | 2 | self | Nova Holy | A soft light shows the ground about you. |
| 1 | **Minor Mending** (`minor-mending`) | 2 | self | Rise Nature | A small mercy: 1d8 plus a little Wisdom. |
| 1 | **Sacred Flame** (`sacred-flame`) | 2 | creature | Pillar Holy | Light falls on the target like a hand. The undead take double. |
| 1 | **Shield of Faith** (`shield-of-faith`) | 3 | self | Swirl Holy | Faith rises like a wall: AC +4 for a while. |
| 2 | **Aura of Courage** (`aura-of-courage`) | 4 | self | Swirl Holy | Courage steadies you: +2 to hit and +6 HP. |
| 2 | **Blinding Light** (`blinding-light`) | 4 | cone | Cone Holy | A cone of white light that leaves its victims blind. |
| 2 | **Cleanse** (`cleanse`) | 3 | self | Rise Water | Burns away poison, confusion, blindness and visions. |
| 2 | **Divine Favor** (`divine-favor`) | 4 | self | Swirl Holy | +3 to hit, +2 damage and a die of holy fire on your blows, for a while. |
| 2 | **Heroism** (`heroism`) | 4 | self | Swirl Blood | Courage in the chest: +3 to hit, Str +1. |
| 2 | **Holy Weapon** (`holy-weapon`) | 4 | self | Swirl Holy | Your weapon shines: +1d6 holy and +1 to hit. |
| 2 | **Protection from Evil** (`protection-from-evil`) | 4 | self | Swirl Holy | A ring of white fire: AC +3 and necrotic resistance 40%. |
| 2 | **Remedy** (`remedy`) | 3 | self | Rise Nature | Draws poison out of the blood. |
| 2 | **Searing Light** (`searing-light`) | 4 | creature | Beam Holy | A needle of white light. The undead take double. |
| 2 | **Second Wind** (`second-wind`) | 3 | self | Rise Wind | Your breath and your legs come back: Vigor to the brim. |
| 2 | **Smite** (`smite`) | 4 | creature | Pillar Holy | Radiant wrath. The undead take double. |
| 2 | **Spiritual Weapon** (`spiritual-weapon`) | 4 | self | — | A hammer of light fights beside you for a time. |
| 2 | **Turn Undead** (`turn-undead`) | 5 | self | Nova Holy | Every undead in sight burns and flees. |
| 3 | **Cleansing Nova** (`cleansing-nova`) | 7 | self | Nova Holy | A ring of white fire 2 cells around you that blinds as it burns. |
| 3 | **Consecrate** (`consecrate`) | 7 | area | Eruption Holy | Hallows a patch of floor: the light burns whoever stands in it. |
| 3 | **Divine Shield** (`divine-shield`) | 7 | self | Swirl Holy | A shield of light: AC +6 for a short while. |
| 3 | **Fortitude** (`fortitude`) | 6 | self | Swirl Blood | Con +3 and +15 HP for a long while. |
| 3 | **Greater Heal** (`greater-heal`) | 8 | self | Rise Nature | A great mending: 4d8 plus Wisdom and level. |
| 3 | **Hammer of Wrath** (`hammer-of-wrath`) | 6 | creature | Bolt Holy | A hammer of light. May stun. |
| 3 | **Hold Person** (`hold-person`) | 6 | creature | Mark Holy | A living target stands rigid for a few turns. |
| 3 | **Holy Lance** (`holy-lance`) | 7 | line | Beam Holy | A lance of light that pierces everything in a line. |
| 3 | **Mercy Touch** (`mercy-touch`) | 5 | self | Rise Holy | A laying on of hands: 3d6 plus Wisdom. |
| 3 | **Prayer** (`prayer`) | 7 | self | Swirl Holy | +2 to hit, +2 damage, AC +2, evasion +1. |
| 3 | **Regeneration** (`regeneration`) | 6 | self | Rise Nature | Wounds close by themselves: 2 HP a turn for a while. |
| 3 | **Remove Curse** (`remove-curse`) | 6 | self | Rise Holy | Lifts the curse off everything you carry. |
| 3 | **Sanctuary** (`sanctuary`) | 7 | self | Nova Holy | A hush: few will lift a hand against you. |
| 4 | **Benediction** (`benediction`) | 10 | self | Pillar Holy | A full blessing: AC +2, +2 to hit, +2 damage, necrotic 30%, and a mending. |
| 4 | **Divine Might** (`divine-might`) | 9 | self | Swirl Holy | Str +4 and +2 damage for a while. |
| 4 | **Exorcise** (`exorcise`) | 10 | self | Nova Holy | Burns and routs everything within 5 cells. The undead take double. |
| 4 | **Flame Strike** (`flame-strike`) | 10 | area | Pillar Fire | A column of fire from the sky, 3 cells wide. |
| 4 | **Holy Aura** (`holy-aura`) | 9 | self | Swirl Holy | A blazing halo: AC +3, necrotic resistance 40%, and those who strike you burn. |
| 4 | **Mass Cure** (`mass-cure`) | 11 | self | Rise Holy | Healing light on you and every ally within 5 cells. |
| 4 | **Purify** (`purify`) | 10 | self | Rise Holy | Burns 15 points of the Ossuary's corruption out of you. Mutations stay. |
| 4 | **Radiant Nova** (`radiant-nova`) | 10 | self | Nova Holy | A ring of light bursts 3 cells out from you. |
| 4 | **Restoration** (`restoration`) | 10 | self | Rise Holy | A deep mending: 3d8 plus Wisdom, and every ill state on you ends. |
| 4 | **Sacred Ground** (`sacred-ground`) | 9 | area | Eruption Holy | A patch of floor where the light stands up and burns. |
| 4 | **Spirit Guardians** (`spirit-guardians`) | 10 | self | Nova Holy | Pale spirits whirl about you, hurting and slowing everything within 2 cells. |
| 4 | **Sunbeam** (`sunbeam`) | 9 | line | Beam Holy | A lance of sunlight along a line; what it does not kill, it blinds. |
| 5 | **Angelic Blessing** (`angelic-blessing`) | 14 | self | Pillar Holy | For a long while you are a little more than you are. |
| 5 | **Divine Intervention** (`divine-intervention`) | 16 | self | Pillar Holy | Heaven takes a hand: full health, every ill ended, and a hush around you. |
| 5 | **Guardian Angel** (`guardian-angel`) | 15 | self | — | A winged guardian descends to fight for you. |
| 5 | **Judgement** (`judgement`) | 16 | area | Pillar Holy | A pillar of white fire falls: everything within 3 cells is judged. |
| 5 | **Revive** (`revive`) | 15 | self | Pillar Holy | Wards your soul: the next death within 300 turns is undone. |
| 5 | **Wrath of Heaven** (`wrath-of-heaven`) | 15 | self | Custom Holy | Five pillars of fire on five creatures in view. |

### Nature — 58 spells (Ranger)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Antidote** (`antidote`) | 2 | self | Rise Nature | A bitter root that ends poison. |
| 1 | **Barkskin** (`barkskin`) | 3 | self | Swirl Nature | Your skin roughens to bark: AC +4 for a long while. |
| 1 | **Herbal Poultice** (`herbal-poultice`) | 2 | self | Rise Nature | Crushed leaves on a wound: 1d8. |
| 1 | **Hunter's Mark** (`hunters-mark`) | 2 | creature | Mark Blood | Marks a quarry: it takes more from every blow for a long while. |
| 1 | **Insect Swarm** (`insect-swarm`) | 3 | area | Cloud Nature | A cloud of biting flies. Stings, and sets creatures swatting at the air. |
| 1 | **Shillelagh** (`shillelagh`) | 2 | self | Swirl Nature | Your weapon thrums like oak: +2 to hit, +3 damage. |
| 1 | **Thorn Dart** (`thorn-dart`) | 2 | creature | Bolt Nature | A poisoned thorn flung hard. |
| 1 | **Wild Growth** (`wild-growth`) | 2 | area | Eruption Nature | Grass springs up around a spot. Dry brush burns well. |
| 2 | **Calm Beasts** (`calm-beasts`) | 3 | self | Nova Nature | Every beast within 4 cells lies down and sleeps. |
| 2 | **Camouflage** (`camouflage`) | 4 | self | Swirl Earth | You take the colours of the stone: much harder to notice. |
| 2 | **Cheetah Sprint** (`cheetah-sprint`) | 4 | self | Swirl Wind | A short burst of speed. |
| 2 | **Dust Devil** (`dust-devil`) | 4 | area | Cloud Earth | A little whirlwind that batters, blinds and shoves. |
| 2 | **Eagle Eye** (`eagle-eye`) | 3 | self | Swirl Wind | +3 to hit for a while. |
| 2 | **Entangle** (`entangle`) | 4 | area | Eruption Nature | Roots and vines clutch at everything within 2 cells. |
| 2 | **Fire Seeds** (`fire-seeds`) | 5 | area | Ball Fire | Seeds that burst into flame where they land. |
| 2 | **Flame Blade** (`flame-blade`) | 4 | self | Swirl Fire | A skin of fire on your weapon: +1d6 fire. |
| 2 | **Goodberries** (`goodberries`) | 3 | self | Rise Blood | A handful of berries: a little healing and a little food. |
| 2 | **Hoarfrost** (`hoarfrost`) | 4 | area | Rain Cold | A rime of killing frost on a patch of floor. |
| 2 | **Lightning Lash** (`lightning-lash`) | 4 | creature | Zap Lightning | A whip of lightning from the sky. |
| 2 | **Moonfire** (`moonfire`) | 4 | creature | Pillar Water | Cold white fire from above. The undead take double. |
| 2 | **Poison Spray** (`poison-spray`) | 4 | cone | Cone Poison | A gout of venom three cells long. |
| 2 | **Quicksand** (`quicksand`) | 4 | area | Eruption Earth | The floor goes soft and wet; whatever stands on it sinks and sticks. |
| 2 | **Rejuvenate** (`rejuvenate`) | 4 | self | Rise Nature | Mends 2d6 now, and a little each turn after. |
| 2 | **Speak with Animals** (`speak-with-animals`) | 4 | creature | Mark Nature | A beast listens, and follows you for a while. |
| 2 | **Spike Growth** (`spike-growth`) | 4 | area | Eruption Nature | Thorny growth tears at whatever crosses it and slows it. |
| 2 | **Summon Hawk** (`summon-hawk`) | 4 | self | — | A spirit hawk stoops at your enemies. |
| 2 | **Thorns** (`thorns`) | 4 | self | Swirl Nature | Thorns push through your skin: those who hit you take 1d6. |
| 2 | **Wasp Swarm** (`wasp-swarm`) | 4 | area | Cloud Nature | Angry wasps in a small patch: stings and poison. |
| 2 | **Wild Leap** (`wild-leap`) | 3 | cell | Teleport Nature | A bound that takes you five cells. |
| 3 | **Bear's Endurance** (`bears-endurance`) | 5 | self | Swirl Earth | Con +4 and +10 HP for a long while. |
| 3 | **Choking Vines** (`choking-vines`) | 7 | creature | Eruption Nature | A single creature caught fast and squeezed. |
| 3 | **Commune with Nature** (`commune`) | 5 | self | Nova Nature | The land tells you what is near: the map within 14 cells, and every trap in it. |
| 3 | **Gale** (`gale`) | 6 | cone | Wave Wind | A howling wind that batters and throws creatures back three cells. |
| 3 | **Hail of Thorns** (`hail-of-thorns`) | 6 | self | Custom Nature | Three volleys of thorns on creatures in view. |
| 3 | **Regrowth** (`regrowth`) | 6 | self | Rise Nature | Green life closes your wounds: 3d8 plus Wisdom. |
| 3 | **Sleet Storm** (`sleet-storm`) | 6 | area | Rain Water | Sleet in a wide patch: it hurts, slows, and ices the floor. |
| 3 | **Spirit Boar** (`spirit-boar`) | 7 | self | — | A spirit boar charges your enemies. |
| 3 | **Spirit Spider** (`giant-spider`) | 7 | self | — | A spirit spider, all legs and venom, serves you. |
| 3 | **Spirit Wolves** (`spirit-wolves`) | 8 | self | — | Two wolves of pale light hunt for you. |
| 3 | **Spore Cloud** (`spore-cloud`) | 6 | area | Cloud Nature | A cloud of spores: stinging, and the mind goes with it. |
| 3 | **Stone Spikes** (`stone-spikes`) | 6 | cone | Cone Earth | Spikes of stone thrust out in a cone four cells long. |
| 3 | **Stoneform** (`stoneform`) | 6 | self | Swirl Earth | Your skin takes the grain of stone: AC +5, fire 20%, a little clumsy. |
| 3 | **Sun Bolt** (`sun-bolt`) | 6 | creature | Beam Holy | A ray of concentrated sunlight that sets the target alight. |
| 3 | **Tremor** (`tremor`) | 7 | self | Eruption Earth | The ground shudders 3 cells around you: hurts, and may stun. |
| 3 | **Venom Bolt** (`venom-bolt`) | 6 | creature | Bolt Poison | A bolt of concentrated venom. The poison lingers. |
| 4 | **Bear Form** (`bear-form`) | 10 | self | Swirl Earth | You take a bear's shape: Str +5, Con +4, +20 HP, AC +3. |
| 4 | **Briar Wall** (`briar-wall`) | 9 | area | Eruption Nature | A wall of iron-hard thorns springs up and holds what it catches. |
| 4 | **Eagle Form** (`eagle-form`) | 9 | self | Swirl Wind | You take an eagle's shape: Dex +4, evasion +4. |
| 4 | **Frostwind** (`frostwind`) | 9 | cone | Wave Cold | A wind that cuts like ice, five cells long. |
| 4 | **Healing Rain** (`healing-rain`) | 10 | self | Rain Water | A warm rain that heals you and every ally within 5 cells. |
| 4 | **Rockslide** (`rockslide`) | 9 | area | Rain Earth | A rain of boulders from above. |
| 4 | **Summon Bear** (`summon-bear`) | 10 | self | — | A spirit bear lumbers up beside you. |
| 4 | **Tidal Surge** (`tidal-surge`) | 9 | cone | Wave Water | A wall of water five cells long: batters, slows and throws creatures back. |
| 4 | **Tornado** (`tornado`) | 9 | area | Cloud Wind | A howling funnel: crushes, shoves and blinds. |
| 4 | **Wolf Form** (`wolf-form`) | 9 | self | Swirl Shadow | You take a wolf's shape: Str +3, Dex +3, AC +2, evasion +2. |
| 5 | **Lightning Storm** (`lightning-storm`) | 15 | self | Custom Lightning | Five bolts on five creatures in view. |
| 5 | **Starfall** (`starfall`) | 16 | self | Custom Arcane | Four stars fall on four creatures in view. |
| 5 | **Treant** (`treant`) | 15 | self | — | An old tree walks. |

### Shadow — 47 spells (Rogue)

| Lv | Spell | Mp | Target | Animation | What it does |
|---|---|---|---|---|---|
| 1 | **Blinding Powder** (`blinding-powder`) | 2 | cone | Cone Earth | A fistful of pepper and ash in the face of everything in front of you. |
| 1 | **Hex** (`hex`) | 2 | creature | Mark Shadow | A muttered hex: the target's feet drag. |
| 1 | **Poison Dart** (`poison-dart`) | 2 | creature | Bolt Poison | A tiny dart, a lot of poison. |
| 1 | **Shadow Bolt** (`shadow-bolt`) | 2 | creature | Bolt Shadow | A bolt of cold dark. Not the dead. |
| 1 | **Skeleton Key** (`skeleton-key`) | 2 | self | Swirl Wind | Unlocks every locked door within 5 cells. |
| 1 | **Throwing Knives** (`throwing-knives`) | 2 | creature | Bolt Wind | A flick of the wrist: three knives you did not have a moment ago. |
| 1 | **Venom Blade** (`venom-blade`) | 3 | self | Swirl Poison | Your weapon beads with venom: +1d6 poison. |
| 2 | **Caltrops** (`caltrops`) | 3 | area | Eruption Earth | A scatter of iron spikes: hurts and slows. |
| 2 | **Cloak of Shadows** (`cloak-of-shadows`) | 4 | self | Swirl Shadow | Shadow gathers about you: much harder to notice, evasion +2. |
| 2 | **Cripple** (`cripple`) | 4 | creature | Slash Blood | A cut to the hamstring: it hobbles after you. |
| 2 | **Disarm Traps** (`disarm-traps`) | 3 | self | Nova Earth | Disarms every trap within 3 cells. |
| 2 | **Garrote** (`garrote`) | 4 | creature | Slash Blood | A wire from nowhere: hurts and opens a wound. |
| 2 | **Gloom** (`gloom`) | 3 | area | Cloud Shadow | A patch of unnatural dark: creatures in it fight blind and take more. |
| 2 | **Light Fingers** (`light-fingers`) | 3 | creature | Slash Wind | Lifts a few coins off whatever you touch. |
| 2 | **Mark for Death** (`mark-for-death`) | 3 | creature | Mark Blood | A quarry marked: it takes more from every blow for a long while. |
| 2 | **Mind Spike** (`mind-spike`) | 4 | creature | Bolt Mind | A needle of thought driven into the target's head. |
| 2 | **Shade Strike** (`shade-strike`) | 4 | cone | Cone Shadow | A cone of living shadow that bites and frightens. |
| 2 | **Shadow Step** (`shadow-step`) | 4 | cell | Teleport Shadow | From one shadow to another, six cells away. |
| 2 | **Sleeping Dust** (`sleeping-dust`) | 4 | cone | Cone Earth | A pinch of dust three cells long that may put the living to sleep. |
| 2 | **Smoke Bomb** (`smoke-bomb`) | 4 | self | Cloud Shadow | A burst of black smoke: foes within 2 cells go blind, and you slip out of sight. |
| 2 | **Snare** (`snare`) | 4 | area | Eruption Earth | A hidden snare: hurts and holds whatever steps in it. |
| 2 | **Venom Spit** (`venom-spit`) | 4 | cone | Cone Poison | A spray of venom from between your teeth. |
| 3 | **Ambush** (`ambush`) | 6 | self | Swirl Shadow | Patience pays: +4 to hit, +3 damage and well hidden, for a short while. |
| 3 | **Dread Gaze** (`dread-gaze`) | 6 | creature | Mark Shadow | A look that shows the target its own death. |
| 3 | **Fade** (`fade`) | 6 | self | Swirl Shadow | You are hard to look at and harder to hit: evasion +5, hard to notice. |
| 3 | **Knife Flurry** (`knife-flurry`) | 7 | self | Custom Wind | Three knives each find a creature in view. |
| 3 | **Night Whip** (`night-whip`) | 6 | line | Beam Shadow | A whip of darkness that lashes everything along a line. |
| 3 | **Nightshade** (`nightshade`) | 6 | creature | Bolt Poison | A drop of nightshade at a distance: poison that does not let go. |
| 3 | **Shadow Blade** (`shadow-blade`) | 5 | self | Swirl Shadow | Your weapon drinks the light: +1d6 necrotic, +2 to hit. |
| 3 | **Shadow Clone** (`shadow-clone`) | 6 | self | — | Your shadow steps off the floor and fights. |
| 3 | **Shadow Leap** (`shadow-leap`) | 5 | cell | Teleport Shadow | Out of one shadow and into another, eight cells away. |
| 3 | **Umbral Grasp** (`umbral-grasp`) | 6 | creature | Eruption Shadow | Hands of shadow rise and hold the target. |
| 3 | **Umbral Shroud** (`umbral-shroud`) | 6 | self | Swirl Shadow | A shroud of dark folds around you: AC +2, necrotic 30%, cold 20%. |
| 3 | **Vampiric Edge** (`vampiric-edge`) | 5 | self | Swirl Blood | Your weapon thirsts: a quarter of what you deal comes back. |
| 3 | **Veil of Darkness** (`veil-of-darkness`) | 6 | self | Cloud Shadow | The dark thickens: everything nearby loses track of you. |
| 4 | **Assassinate** (`assassinate`) | 8 | creature | Slash Blood | A killing blow: double damage on anything asleep or unaware. |
| 4 | **Black Tentacles** (`black-tentacles`) | 9 | area | Eruption Shadow | Tentacles of dark crush and hold everything within 2 cells. |
| 4 | **Chain of Shadows** (`chain-of-shadows`) | 9 | creature | Custom Shadow | Shadow leaps from one creature to the next and holds each for a moment. |
| 4 | **Coup de Grace** (`coup-de-grace`) | 8 | creature | Slash Blood | Finishes anything badly hurt; a sound blow on the rest. |
| 4 | **Quicken Shadow** (`quicken-shadow`) | 9 | self | Swirl Shadow | A short burst of speed. |
| 4 | **Soul Dagger** (`soul-dagger`) | 9 | creature | Bolt Blood | A dagger of stolen life; half of what it takes comes back to you. |
| 4 | **Vampiric Veil** (`vampiric-veil`) | 9 | self | Swirl Blood | A veil that drinks: 20% life steal, evasion +3, harder to notice. |
| 4 | **Vanish** (`vanish`) | 9 | self | Cloud Shadow | Smoke, a sleight, and you are not here: invisible, and everyone has lost you. |
| 4 | **Void Rift** (`void-rift`) | 10 | area | Implode Shadow | A small hole in the world, pulling everything near it in. |
| 5 | **Dominate** (`dominate`) | 16 | creature | Mark Mind | A living creature becomes yours for a long while. |
| 5 | **Eclipse** (`eclipse`) | 14 | self | Nova Shadow | The light dies for 5 cells: everything in it is hurt and blind. |
| 5 | **Shadow Walk** (`shadow-walk`) | 12 | self | Custom Shadow | Step into the dark and out elsewhere on the level, unseen. |

## Books

Spells come from books; `r` on a book (away from enemies) tries to learn every spell you do not yet know. Books found on the floor respect
depth (`LevelBuilder.PickBook`: max tier = 1 + depth/3); the bookshop sells tiers 1–2. Price = tier² × 40 + 60.

| Book | Depth tier | Price | Teaches |
|---|---|---|---|
| *a book of prayers* | 1 | 100 | Cure Wounds, Ward, Bless, Cleanse, Minor Mending, Sacred Flame, Shield of Faith, Holy Light, Command |
| *a charnel primer* | 1 | 100 | Chill Touch, Bone Shard, Enfeeble, Wither, Life Tap, Grave Chill, Bone Hound, Vampiric Touch |
| *a cutpurse's primer* | 1 | 100 | Shadow Bolt, Throwing Knives, Poison Dart, Blinding Powder, Venom Blade, Hex, Skeleton Key |
| *a druid's handbook* | 1 | 100 | Thorn Dart, Insect Swarm, Barkskin, Hunter's Mark, Herbal Poultice, Antidote, Shillelagh, Wild Growth |
| *a hedge-wizard's notes* | 1 | 100 | Light, Find Traps, Knock, Identify, Sense Life, Levitate, Dig |
| *a primer of embers* | 1 | 100 | Ember Dart, Burning Hands, Frostbite, Spark, Acid Splash, Stone Shard, Thunderclap |
| *a spellbook* | 1 | 100 | Magic Missile, Shocking Grasp, Ward, Frost Ray, Familiar, Spark |
| *a book of illusions* | 2 | 220 | Sleep, Confuse, Invisibility, Charm Monster, Dazzle, Color Spray, Befuddle, Suggestion |
| *a book of shadows* | 2 | 220 | Sleep, Blink, Drain Life, Raise Skeleton, Fear, Chill Touch |
| *a book of wards* | 2 | 220 | Ward, Haste, Slow, Clairvoyance, Stone Skin, Mage Armor, Shield, Blur, Arcane Focus |
| *a book of whispers* | 2 | 220 | Cloak of Shadows, Shadow Step, Smoke Bomb, Mark for Death, Caltrops, Cripple, Mind Spike, Venom Spit, Light Fingers, Disarm Traps, Shade Strike, Gloom |
| *a psalter of light* | 2 | 220 | Searing Light, Divine Favor, Protection from Evil, Heroism, Remedy, Second Wind, Blinding Light, Spiritual Weapon |
| *a ranger's almanac* | 2 | 220 | Entangle, Spike Growth, Thorns, Rejuvenate, Goodberries, Eagle Eye, Camouflage, Cheetah Sprint, Flame Blade, Wild Leap |
| *a tome of conjuration* | 2 | 220 | Familiar, Summon Beast, Create Water, Create Oil, Blink, Teleport, Spectral Blade, Phase Step, Web, Fog Cloud |
| *a treatise on resistance* | 2 | 220 | Fire Ward, Frost Ward, Storm Ward, Endure Elements, Displacement |
| *a bestiary of the unseen* | 3 | 420 | Summon Swarm, Mirror Image, Swap Places, Frozen Ground, Banish |
| *a book of grave-bargains* | 3 | 420 | Unholy Bear's Endurance, Dark Pact, Command Undead, Animate Ghoul, Bloodlust, Feign Death, Death Aura |
| *a book of weather* | 3 | 420 | Hoarfrost, Dust Devil, Lightning Lash, Fire Seeds, Poison Spray, Moonfire, Gale, Sleet Storm |
| *a breviary of the faithful* | 3 | 420 | Regeneration, Sanctuary, Prayer, Fortitude, Remove Curse, Hammer of Wrath, Holy Lance, Consecrate, Hold Person |
| *a folio of flames* | 3 | 420 | Flame Arrow, Thunderstrike, Searing Orb, Ice Comet, Arc Flash |
| *a footpad's tricks* | 3 | 420 | Garrote, Sleeping Dust, Snare, Nightshade, Shadow Leap |
| *a forester's lore* | 3 | 420 | Wasp Swarm, Quicksand, Hail of Thorns, Sun Bolt |
| *a lay-brother's psalms* | 3 | 420 | Aura of Courage, Holy Weapon, Mercy Touch, Divine Shield, Cleansing Nova |
| *a manual of the body* | 3 | 420 | Bull's Strength, Cat's Grace, Fox's Cunning, Owl's Wisdom, Enlarge, Endure Elements |
| *a manual of the knife* | 3 | 420 | Shadow Blade, Vampiric Edge, Shadow Clone, Umbral Grasp, Veil of Darkness, Knife Flurry, Fade, Dread Gaze, Umbral Shroud, Night Whip |
| *a menagerie of the lesser planes* | 3 | 420 | Summon Imp, Arcane Hound, Storm Sprite, Pull Through |
| *a mummer's folio* | 3 | 420 | Phantom Guard, Lullaby, Bewilder, Mind Fog |
| *a tome of evocation* | 3 | 420 | Magic Missile, Frost Ray, Ice Lance, Fireball, Steam Burst, Lightning Bolt, Wall of Fire, Scorching Ray, Gust of Wind, Forked Lightning, Poison Cloud, Lava Bolt |
| *a ward-smith's ledger* | 3 | 420 | Lightfoot, Keen Edge, Aegis, Arcane Survey |
| *the bonewright's notes* | 3 | 420 | Death Grip, Soul Bolt, Grave Hands, Miasma, Blight |
| *the green psalter* | 3 | 420 | Speak with Animals, Calm Beasts, Summon Hawk, Spirit Wolves, Spirit Boar, Spirit Spider |
| *the rotted codex* | 3 | 420 | Plague Bolt, Rotting Burst, Hemorrhage, Bone Spear, Dread, Curse of Weakness, Gravebind, Skull Barrage |
| *a book of mercy* | 4 | 700 | Smite, Turn Undead, Greater Heal, Purify, Revive, Restoration, Mass Cure |
| *a codex of beast-shapes* | 4 | 700 | Bear's Endurance, Wolf Form, Bear Form, Eagle Form, Summon Bear |
| *a codex of storms* | 4 | 700 | Lightning Bolt, Fireball, Wall of Fire, Chain Lightning, Meteor, Call Lightning, Ice Storm, Cone of Cold |
| *a folio of glamours* | 4 | 700 | Displacement, Hypnotic Pattern, Terrify, Phantasmal Killer, Nightmare, Mass Charm |
| *a grimoire of the dead* | 4 | 700 | Raise Skeleton, Ossify, Reshape Flesh, Drain Life, Marrow Bolt, Fear, Finger of Death, Army of Bones |
| *a missal of wrath* | 4 | 700 | Holy Aura, Flame Strike, Radiant Nova, Exorcise, Sun Bolt, Divine Might, Spirit Guardians |
| *the assassin's testament* | 4 | 700 | Assassinate, Black Tentacles, Void Rift, Vanish, Coup de Grace, Soul Dagger, Quicken Shadow |
| *the black litany* | 4 | 700 | Contagion, Cloudkill, Siphon Soul, Death Wave, Banshee Wail, Raise Wight, Bone Golem |
| *the book of four winds* | 4 | 700 | Summon Fire Elemental, Summon Water Elemental, Summon Earth Elemental, Summon Air Elemental, Dimension Door, Banish |
| *the night-blade's creed* | 4 | 700 | Vampiric Veil, Chain of Shadows, Ambush, Assassinate, Vanish |
| *the tide and the stone* | 4 | 700 | Stoneform, Regrowth, Tidal Surge, Frostwind |
| *the verdant grimoire* | 4 | 700 | Venom Bolt, Spore Cloud, Choking Vines, Tremor, Commune with Nature, Stone Spikes, Rockslide, Briar Wall, Healing Rain, Tornado |
| *the annals of ruin* | 5 | 1060 | Prismatic Spray, Disintegrate, Sunburst, Earthquake, Cone of Cold, Call Lightning, Meteor |
| *the book of last things* | 5 | 1060 | Judgement, Guardian Angel, Angelic Blessing, Divine Intervention, Restoration |
| *the book of the long night* | 5 | 1060 | Eclipse, Shadow Walk, Dominate, Vanish, Assassinate |
| *the canticle of dawn* | 5 | 1060 | Sacred Ground, Benediction, Wrath of Heaven, Mercy Touch |
| *the dreamless book* | 5 | 1060 | Deep Slumber, Spectral Horror, Mind Fog, Sleep, Hypnotic Pattern |
| *the giant's manual* | 5 | 1060 | Iron Body, Titan's Might, Aegis, Bull's Strength, Enlarge |
| *the last rite* | 5 | 1060 | Soul Reap, Power Word Kill, Summon Wraith, Ritual of Blood, Finger of Death, Army of Bones |
| *the lich's catechism* | 5 | 1060 | Bone Cage, Lich Form, Wail of the Damned, Blight |
| *the pyrelord's treatise* | 5 | 1060 | Magma Wave, Static Field, Arcane Barrage, Rimeblast, Sun Lance |
| *the stone and the gate* | 5 | 1060 | Stone Sentinel, Gargoyle Guard, Summon Earth Elemental, Stone Skin |
| *the wild hunt* | 5 | 1060 | Lightning Storm, Starfall, Treant, Summon Bear, Tornado |

## Items

### New bases (`Items/Catalogue.More.cs`, effects in `Items/ItemEffects.cs`)

- **Weapons** (37): club, hand axe, stiletto, main gauche, javelin, ashwood staff, kris, rapier, falchion, bastard sword, morning star, flanged mace, war pick, glaive, pike, halberd, druid's crook, greatsword, great axe, maul, dwarven waraxe, katana, wizard's staff, bone staff, staff of the faithful, elven blade, spiked club, cutlass, estoc, runed dagger, witch's wand, sacrificial knife, thornwood staff, lucerne hammer, bardiche, claymore, crystal staff.
- **Armors** (19): padded armour, studded leather, brigandine, mage's robe, druid's vestments, priest's vestments, banded mail, necromancer's shroud, shadowsilk tunic, half plate, full plate, archmage's robe, mithril shirt, dragonhide armour, quilted gambeson, shaman's furs, storm-cloth robe, battle-priest's mail, lamellar.
- **Helms** (14): coif of mail, circlet, wizard's hat, hood of shadows, horned helm, visored helm, skull cap of the dead, winged helm, laurel of the sage, crown of thorns, bone crown, plague doctor's mask, iron halo, cat's-eye circlet.
- **Gloves** (11): gloves of dexterity, gloves of spellcasting, thieves' gloves, mage's mitts, gauntlets of the faithful, gauntlets of ogre power, bracers of defence, gloves of the healer, gauntlets of flame, gauntlets of storms, witch's gloves.
- **Boots** (9): sandals of the wind, boots of striding, boots of the mage, boots of elvenkind, boots of the north, boots of fire walking, boots of the wind-walker, boots of deep stone, ghoul-leather boots.
- **Cloaks** (10): wolf pelt, cloak of protection, cloak of the mage, cloak of the bat, cloak of fortitude, cloak of resistance, cloak of shadows, feathered cloak, cloak of the storm, mantle of the grave.
- **Shields** (9): kite shield, tower shield, bone shield, rune shield, mirror shield, aegis of the faithful, duelist's buckler, shield of the sun, rampart.
- **Rings** (33): silver band, gold band, bone ring, jade ring, ring of fire resistance, ring of frost resistance, ring of storm resistance, ring of poison resistance, ring of the grave, ring of accuracy, ring of evasion, ring of stealth, ring of might, ring of flames, ring of frost, ring of sparks, ring of the mage, ring of focus, ring of vitality, ring of intellect, ring of insight, ring of spell power, ring of vampirism, ring of the archmage, ring of warding, ring of the fox, ring of resistance, ring of mana, ring of the sage, ring of life, ring of the grave-knight, ring of the assassin, ring of the berserker.
- **Amulets** (22): charm, pendant, locket, talisman, amulet of stealth, amulet of health, amulet of warding, amulet of vigor, amulet of the wolf, amulet of the hunter, amulet of faith, amulet of the grave, amulet of resistance, amulet of the sage, amulet of the magi, amulet of spell power, amulet of the phoenix, amulet of the glacier, amulet of the tempest, amulet of the oracle, amulet of the berserker, amulet of the shadow.

`ItemEffects` gives numbers to a base just for existing (archmage's robe: +12 Mp, +2 spell power, +10% focus). Affixes, enchantment and
artifacts add on top. Loot respects **tier** (`LevelBuilder.PickDeep`: max tier = 2 + depth/3), so the big things appear deep.

**Rings and amulets now count.** The old amulets were, in the catalog, *rings* with an amulet's name (they took a finger slot and the real amulet slot was never used); they are now `ItemKind.Amulet`. `Player.AccessoryMods` sums rings and amulet into `Gear` (and into hit, damage, extra die and AC). Amulets can be
worn (`P`, or `Enter` in the inventory) and removed with `R`; the *amulet of life saving* undoes a death and is consumed; the *amulet of ESP* shows creatures through walls.

### New affixes (`Items/Affixes.cs`)

New prefixes (30): shocking, radiant, rotting, thundering, searing, holy, draining, masterwork, razor-edged, arcane, mage-woven, shadowed, grave-warded, mithril-lined, rune-etched, troll-hide, dragon-warded, fortified, wintry, stormforged, bloodthirsty, sanctified, serrated, balanced, stormproof, ghostly, sage's, hallowed, vital, bladeturning.

New suffixes (24): of the wolf, of the lion, of the sphinx, of stealth, of brilliance, of the archmage, of fire, of frost, of storms, of the grave, of vitality, of precision, of might, of the hunter, of slaying, of the phoenix, of the glacier, of the tempest, of the oracle, of agility, of fortitude, of the sentinel, of reaping, of the storm.

### Magic items with spells (`Magic/SpellFit.cs`)

Magic equipment found on the floor can come **imbued with a spell that fits what it is**. The roll picks from a pool filtered by type
(and by depth: spell level ≤ 1 + depth/3; if nothing that low fits the type, it takes the lowest that fits):

| Item | Spell pool | Automatic effect |
|---|---|---|
| Weapon | aimed attacks and control (ball, cone, line, bolt, random strikes) and weapon buffs | 14% to **fire by itself** at the target on every hit that lands |
| Armor, shield | protections, heals, auras, summons and new area-around-you spells | 10% to **answer by itself** when you are hit |
| Helm | senses and mind (light, find traps, sense life, illusions) | only lends the spell |
| Gloves | touch and short cones, pick locks, steal, disarm | only lends the spell |
| Boots | movement (blink, step, leap, haste, levitate, forms) | only lends the spell |
| Cloak | stealth and illusions on you | 10% to answer |
| Ring | protections, senses, stealth, movement, weapon buffs | 10% to answer |
| Amulet | protections, summons, novas and rains of strikes, senses, mind | 10% to answer |

- **Rare**: 55% to come imbued; **magic**: 18%. *Blank* rings (`silver band`, `gold band`, `bone ring`, `jade ring`) and amulets (`charm`, `pendant`, `locket`, `talisman`)
  **always** appear imbued. The name becomes "long sword of Frostbite" once identified (before: "magical long sword" or "enchanted silver band").
- While you hold or wear the item, the spell goes into your list (`◆`) and you cast it with mana, like any lent spell. Wearing reveals the spell.
- The automatic firing is free (no mana, no failure, costs no turn) and uses caster level `max(your level, 5 + depth/2)`. Worth more in trade (+150 per spell level).
- To fit a new spell to items, it only needs recipe data; `SpellFit.Fits` reads the target, riders, buffs and specials.

### Wands, scrolls and potions that are spells (`Items/ItemSpells.cs`)

Each one casts a **real spell** (with the same effect and animation), without mana and without failure, at a minimum caster-level *power*
(`CastFromItem`). A wand spends a charge; scrolls and potions are consumed. Aiming and cancelling spend **nothing**. The old ones (light, striking, cold, fire, lightning,
digging, teleportation) keep their name and now use aiming and the animations. The table's tier governs the depth at which they appear.

**Wands (38)**

| Item | Casts | Power | Price | Tier |
|---|---|---|---|---|
| wand of acid | Acid Splash (`acid-splash`) | 5 | 150 | 1 |
| wand of confusion | Confuse (`confuse`) | 6 | 200 | 1 |
| wand of entangling | Entangle (`entangle`) | 6 | 200 | 1 |
| wand of magic missiles | Magic Missile (`magic-missile`) | 6 | 150 | 1 |
| wand of searing light | Searing Light (`searing-light`) | 6 | 200 | 1 |
| wand of shadows | Shadow Bolt (`shadow-bolt`) | 5 | 150 | 1 |
| wand of sleep | Sleep (`sleep`) | 6 | 200 | 1 |
| wand of smiting | Smite (`smite`) | 6 | 200 | 1 |
| wand of sparks | Spark (`spark`) | 4 | 100 | 1 |
| wand of webs | Web (`web`) | 6 | 200 | 1 |
| wand of blinking | Blink (`blink`) | 8 | 300 | 2 |
| wand of bones | Bone Spear (`bone-spear`) | 7 | 300 | 2 |
| wand of cold | Frost Ray (`frost-ray`) | 7 | 200 | 2 |
| wand of digging | Dig (`dig`) | 6 | 200 | 2 |
| wand of draining | Drain Life (`drain-life`) | 7 | 300 | 2 |
| wand of fear | Fear (`fear`) | 7 | 250 | 2 |
| wand of fire | Scorching Ray (`scorching-ray`) | 7 | 200 | 2 |
| wand of gales | Gale (`gale`) | 7 | 250 | 2 |
| wand of holding | Hold Person (`hold-person`) | 7 | 300 | 2 |
| wand of light | Light (`light`) | 5 | 100 | 2 |
| wand of lightning | Lightning Bolt (`lightning-bolt`) | 7 | 200 | 2 |
| wand of mending | Greater Heal (`greater-heal`) | 8 | 300 | 2 |
| wand of rot | Rotting Burst (`rotting-burst`) | 7 | 300 | 2 |
| wand of slowness | Slow (`slow`) | 6 | 220 | 2 |
| wand of striking | Stone Shard (`stone-shard`) | 6 | 200 | 2 |
| wand of teleportation | Teleport (`teleport`) | 8 | 200 | 2 |
| wand of thunder | Thunderclap (`thunderclap`) | 6 | 250 | 2 |
| wand of venom | Venom Bolt (`venom-bolt`) | 8 | 300 | 2 |
| wand of banishment | Banish (`banish`) | 9 | 400 | 3 |
| wand of charming | Charm Monster (`charm`) | 8 | 350 | 3 |
| wand of fire seeds | Fire Seeds (`fire-seeds`) | 7 | 300 | 3 |
| wand of fireballs | Fireball (`fireball`) | 8 | 400 | 3 |
| wand of frost | Cone of Cold (`cone-of-cold`) | 8 | 400 | 3 |
| wand of lava | Lava Bolt (`lava-bolt`) | 8 | 350 | 3 |
| wand of the blizzard | Ice Storm (`ice-storm`) | 9 | 450 | 4 |
| wand of the storm | Chain Lightning (`chain-lightning`) | 9 | 450 | 4 |
| wand of meteors | Meteor (`meteor`) | 12 | 800 | 5 |
| wand of ruin | Disintegrate (`disintegrate`) | 12 | 800 | 5 |

**Scrolls (34)**

| Item | Casts | Power | Price | Tier |
|---|---|---|---|---|
| scroll of clarity | Cleanse (`cleanse`) | 6 | 100 | 1 |
| scroll of entangling | Entangle (`entangle`) | 6 | 100 | 1 |
| scroll of knocking | Knock (`knock`) | 5 | 50 | 1 |
| scroll of light | Light (`light`) | 5 | 40 | 1 |
| scroll of protection | Mage Armor (`mage-armor`) | 6 | 80 | 1 |
| scroll of resistance | Endure Elements (`endure-elements`) | 6 | 100 | 1 |
| scroll of sensing | Sense Life (`detect-monsters`) | 6 | 80 | 1 |
| scroll of trapfinding | Find Traps (`detect-traps`) | 6 | 60 | 1 |
| scroll of blinking | Blink (`blink`) | 8 | 150 | 2 |
| scroll of fear | Terrify (`terrify`) | 7 | 140 | 2 |
| scroll of frost | Frost Ray (`frost-ray`) | 7 | 120 | 2 |
| scroll of gales | Gale (`gale`) | 8 | 140 | 2 |
| scroll of healing | Greater Heal (`greater-heal`) | 8 | 160 | 2 |
| scroll of levitation | Levitate (`levitate`) | 6 | 100 | 2 |
| scroll of lightning | Lightning Bolt (`lightning-bolt`) | 8 | 140 | 2 |
| scroll of remove curse | Remove Curse (`remove-curse`) | 6 | 150 | 2 |
| scroll of smiting | Smite (`smite`) | 7 | 120 | 2 |
| scroll of banishment | Banish (`banish`) | 9 | 240 | 3 |
| scroll of beasts | Summon Beast (`summon-beast`) | 8 | 160 | 3 |
| scroll of bone servants | Raise Skeleton (`raise-skeleton`) | 8 | 180 | 3 |
| scroll of darkness | Veil of Darkness (`veil-of-darkness`) | 8 | 160 | 3 |
| scroll of fireball | Fireball (`fireball`) | 8 | 180 | 3 |
| scroll of haste | Haste (`haste`) | 8 | 180 | 3 |
| scroll of invisibility | Invisibility (`invisibility`) | 8 | 180 | 3 |
| scroll of sanctuary | Sanctuary (`sanctuary`) | 8 | 200 | 3 |
| scroll of slumber | Hypnotic Pattern (`hypnotic-pattern`) | 8 | 180 | 3 |
| scroll of warding | Stone Skin (`stone-skin`) | 8 | 180 | 3 |
| scroll of elementals | Summon Fire Elemental (`summon-fire-elemental`) | 10 | 300 | 4 |
| scroll of ice | Ice Storm (`ice-storm`) | 9 | 240 | 4 |
| scroll of mending | Restoration (`restoration`) | 9 | 240 | 4 |
| scroll of revival | Revive (`revive`) | 10 | 400 | 4 |
| scroll of the tempest | Call Lightning (`call-lightning`) | 9 | 260 | 4 |
| scroll of mass charm | Mass Charm (`mass-charm`) | 12 | 500 | 5 |
| scroll of meteors | Meteor (`meteor`) | 12 | 500 | 5 |

**Potions (18)**

| Item | Casts | Power | Price | Tier |
|---|---|---|---|---|
| potion of clarity | Cleanse (`cleanse`) | 6 | 120 | 1 |
| potion of fire protection | Fire Ward (`fire-ward`) | 6 | 150 | 1 |
| potion of frost protection | Frost Ward (`frost-ward`) | 6 | 150 | 1 |
| potion of might | Bull's Strength (`bulls-strength`) | 6 | 150 | 1 |
| potion of resistance | Endure Elements (`endure-elements`) | 6 | 150 | 1 |
| potion of storm protection | Storm Ward (`storm-ward`) | 6 | 150 | 1 |
| potion of vigor | Second Wind (`second-wind`) | 6 | 100 | 1 |
| potion of cunning | Fox's Cunning (`foxs-cunning`) | 6 | 180 | 2 |
| potion of grace | Cat's Grace (`cats-grace`) | 6 | 180 | 2 |
| potion of heroism | Heroism (`heroism`) | 6 | 180 | 2 |
| potion of mending | Restoration (`restoration`) | 8 | 180 | 2 |
| potion of regeneration | Regeneration (`regeneration`) | 6 | 200 | 2 |
| potion of shadows | Invisibility (`invisibility`) | 8 | 220 | 2 |
| potion of the bear | Bear's Endurance (`bears-endurance`) | 6 | 180 | 2 |
| potion of wisdom | Owl's Wisdom (`owls-wisdom`) | 6 | 180 | 2 |
| potion of giants | Enlarge (`enlarge`) | 8 | 250 | 3 |
| potion of sanctuary | Sanctuary (`sanctuary`) | 8 | 240 | 3 |
| potion of stone skin | Stone Skin (`stone-skin`) | 8 | 220 | 3 |

### Unique items (`Items/Artifacts.More.cs`)

Each unique lives on one level of a branch, with a **chance** (several can share a level; each rolls its own; the 13 old ones are still always there).
Many **lend spells** while in use (`ArtifactDef.Grants`, shown with `◆` in the spell list). Set pieces add bonuses at 2 and 3 pieces
(*Lich-Queen's Panoply*, *Stormcaller's Regalia*, *Verdant Court*, *Midnight Cabal*); they count weapons, armor, rings and amulet.

| Unique item | Base | Where (level) | Chance | Powers | Spells lent | Set |
|---|---|---|---|---|---|---|
| **Gaoler's Keyring** | silver band | Dungeons 3 | 40% | +1 stealth, +1 Dex | Knock, Find Traps | — |
| **Ratcatcher's Cudgel** | club | Dungeons 2 | 50% | +2 damage, +1d3 poison | — | — |
| **Mourner's Blade** | stiletto | Dungeons 5 | 45% | +1 Dex, +1d4 necrotic | Assassinate | — |
| **Prisoner's Prayer** | amulet of faith | Dungeons 4 | 40% | +10 HP, +1 Wis | Sanctuary, Cure Wounds | — |
| **Heartwood Staff** | druid's crook | Dungeons 6 | 40% | +2 Wis, +8 Mp, +1 stealth | Thorns, Spike Growth | Verdant Court |
| **Warden's Bulwark** | tower shield | Dungeons 7 | 40% | +1 Con, +1 AC, fire 15% | — | — |
| **Nightglove** | thieves' gloves | Dungeons 4 | 35% | +1 Dex, +2 stealth | Shadow Bolt, Hex | Midnight Cabal |
| **Stairwalker's Cloak** | cloak | Dungeons 10 | 40% | +1 Dex, +2 evasion, +6 Vigor | Blink | — |
| **Pickaxe of the First Vein** | war pick | Mines of Dwarfdeep 3 | 45% | +1 Str, +2 damage | Dig | — |
| **Delver's Lamp-Helm** | dwarvish helm | Mines of Dwarfdeep 4 | 40% | fire 20%, +8 HP | Light | — |
| **Stonebinder's Gauntlets** | gauntlets | Mines of Dwarfdeep 5 | 40% | +2 Str, +1 Con | Stone Skin | — |
| **Deepmaw Plate** | banded mail | Mines of Dwarfdeep 6 | 35% | +2 Con, +12 HP, cold 20% | — | — |
| **Ore Golem's Heart** | jade ring | Mines of Dwarfdeep 8 | 40% | +15 HP, +2 AC | Tremor | — |
| **Barkhide Vest** | druid's vestments | Mines of Dwarfdeep 2 | 45% | +1 Wis, +8 HP, poison 20% | Rejuvenate | Verdant Court |
| **Mantle of the Tempest** | cloak of the mage | Mines of Dwarfdeep 7 | 35% | +8 Mp, lightning 30%, +1 evasion | Spark | Stormcaller's Regalia |
| **Rat King's Whiskers** | bone ring | Warrens 3 | 40% | +1 Dex, +2 stealth, poison 30% | Summon Swarm | — |
| **Scrap-King's Cleaver** | falchion | Warrens 4 | 40% | +1 Str, +3 damage, -1 to hit | — | — |
| **Plaguebearer's Mask** | skull cap of the dead | Warrens 5 | 35% | poison 60%, necrotic 30%, +1 Con (relic: corrupts) | Plague Bolt, Cloudkill | — |
| **Burrower's Boots** | boots of striding | Warrens 6 | 35% | +2 evasion, +1 Dex | Phase Step | — |
| **Mantle of Many Teeth** | cloak | Warrens 2 | 45% | +1 Con, +6 HP | Thorns | — |
| **Gloves of Static** | gloves of spellcasting | Warrens 7 | 35% | +10 focus, lightning 20% | Lightning Lash | Stormcaller's Regalia |
| **Antlered Crown** | circlet | Warrens 8 | 35% | +1 Wis, +6 Mp, +1 stealth | Barkskin, Entangle | Verdant Court |
| **Tidecaller's Trident** | trident | Sunken Vaults 3 | 45% | cold 25%, +1d6 cold | Frozen Ground | — |
| **Pearl of the Drowned** | amulet of resistance | Sunken Vaults 4 | 40% | +1 Wis, +10 Mp, cold 40% | Create Water, Ice Lance | — |
| **Saltwhite Mail** | scale mail | Sunken Vaults 6 | 35% | cold 30%, +10 HP | Frost Ward | — |
| **Lantern of the Deep** | gold band | Sunken Vaults 7 | 40% | +1 stealth, +8 focus | Sense Life, Light | — |
| **Midnight Hood** | hood of shadows | Sunken Vaults 8 | 35% | +2 stealth, +2 evasion | Fade | Midnight Cabal |
| **Weeper's Hands** | leather gloves | Sunken Vaults 9 | 40% | +1 Dex, +1 evasion | Hold Person | — |
| **Sunken Sceptre** | wizard's staff | Sunken Vaults 11 | 45% | +2 Int, +12 Mp, +2 spell power | Chain Lightning, Steam Burst | — |
| **Lich-Queen's Crown** | crown of thorns | Sunken Vaults 12 | 60% | +2 Int, +2 spell power, necrotic 30% | Enfeeble, Wither | Lich-Queen's Panoply |
| **Cindercrown** | circlet | Ashen Spire 4 | 40% | +8 Mp, fire 25%, +1 spell power | Ember Dart, Fire Ward | — |
| **Stormcaller's Staff** | wizard's staff | Ashen Spire 5 | 35% | +2 Int, +10 Mp, +2 spell power, lightning 30% | Lightning Bolt, Call Lightning | Stormcaller's Regalia |
| **Pyre-Knight's Sword** | bastard sword | Ashen Spire 6 | 40% | fire 30%, +1 Str, +1d6 fire | — | — |
| **Ashmonk's Beads** | amulet of the grave | Ashen Spire 7 | 40% | necrotic 40%, +1 Wis, +6 Mp | Turn Undead, Searing Light | — |
| **Lich-Queen's Shroud** | necromancer's shroud | Ashen Spire 10 | 40% | +1 Int, +10 Mp, +1 spell power | Grave Chill, Death Aura | Lich-Queen's Panoply |
| **Wraithwalkers** | boots of elvenkind | Ashen Spire 9 | 35% | +2 stealth, +2 evasion, necrotic 20% | Shadow Step | — |
| **Spire-Lord's Staff** | wizard's staff | Ashen Spire 12 | 45% | +3 Int, +3 spell power, +14 Mp | Fireball, Meteor | — |
| **Lich-Queen's Phylactery** | amulet of the grave | Ashen Spire 13 | 40% | +14 Mp, +1 spell power, necrotic 30% | Siphon Soul | Lich-Queen's Panoply |
| **Regent's Tithe** | gold band | Ashen Spire 14 | 40% | +12 HP, +2 spell power, fire 30% | Wall of Fire | — |
| **Final Ember** | rune shield | Ashen Spire 15 | 45% | +10 Mp, fire 40%, +1 AC | Flame Strike | — |
| **Auditor's Spectacles** | circlet | Annex 1 | 50% | +2 Int, +1 Wis | Identify, Find Traps | — |
| **Ledger of Debts** | bone ring | Annex 2 | 45% | +15 focus, +6 Mp | Mark for Death, Curse of Weakness | — |
| **Dusk Daggers** | stiletto | Annex 2 | 45% | +1 Dex, +1 stealth, +1d4 necrotic | Throwing Knives, Knife Flurry | Midnight Cabal |

## How to add

1. **Spell**: a line in `Magic/Spells.*.cs` (recipe + `.Look`), the translation in `Loc.Spells.cs`, and the id in some book (`Spells.Books.cs`).
   A test sweeps the catalog: every spell is in a book, has an animation, has a translation, casts without error and changes something.
2. **Buff**: a line in `SpellBuffs.cs` (+ translation of the label and message).
3. **Summoned creature**: `Ally(...)` at the end of `Entities/Bestiary.cs` (the `~summon` branch keeps it from appearing on its own).
4. **Wand/scroll/potion**: a line in `ItemSpells.cs`.
5. **Item**: base in `Catalogue.More.cs`, numbers in `ItemEffects.cs`; **unique**: a line in `Artifacts.More.cs`.
6. **New animation**: a piece in `FxLib` and, if it belongs to a single spell, `FxKind.Custom` and record in `Game.Fx(...)`. See the result with `headless fx <id>`.

## Tests (`ArsenalTests.cs`, `desktop/src/fx.test.ts`)

Catalog wiring (books, buffs, summons, translation), **every spell casts, changes the world and animates** with glyphs from the set, animation geometry,
animation reaching the frame (including square tiles and under a panel), riders, buffs, retaliation, specials, school panel, item tables,
affixes, depth-based loot, uniques (found, sets, lent spells), wands/scrolls/potions (consumed, cancelling spends nothing, minimum power).
In the frontend, `applyFx` and the animation player (injectable time).
