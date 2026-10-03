using Ossuary.Core.Entities;

namespace Ossuary.Core.Magic
{
    /// <summary>The wizard's four schools: Evocation, Conjuration, Alteration, Illusion.</summary>
    public static partial class Spells
    {
        const SpellTarget Self = SpellTarget.Self, Mon = SpellTarget.Monster, Cell = SpellTarget.Cell, Area = SpellTarget.Area, Ln = SpellTarget.Line, Cn = SpellTarget.Cone;
        const DamageType Phys = DamageType.Physical, Fire = DamageType.Fire, Cold = DamageType.Cold, Elec = DamageType.Lightning, Pois = DamageType.Poison, Necro = DamageType.Necrotic, Holy = DamageType.Holy;

        static SpellDef[] Evocation() { const School E = School.Evocation; return new[] {
            S("magic-missile", "Magic Missile", 1, E, 2, Mon, 8, "Unerring bolts of force. Dice grow with level.").Look(FxKind.Bolt, Elem.Arcane, '*'),
            S("shocking-grasp", "Shocking Grasp", 1, E, 2, Mon, 1, "Lightning through your touch. Adjacent only; hits hard.").Look(FxKind.Zap, Elem.Lightning),
            S("frost-ray", "Frost Ray", 2, E, 4, Mon, 8, "A beam of cold. Heavy damage, needs a clear line.").Look(FxKind.Beam, Elem.Cold),
            S("fireball", "Fireball", 3, E, 7, Area, 8, "Bursts where you aim, burning everything within 2 cells.", 2).Look(FxKind.Ball, Elem.Fire, 'o'),
            S("lightning-bolt", "Lightning Bolt", 3, E, 6, Ln, 8, "A bolt that pierces every creature along its line.").Look(FxKind.Zap, Elem.Lightning),
            S("chain-lightning", "Chain Lightning", 4, E, 10, Mon, 8, "Strikes a target, then leaps to up to three more nearby.").Look(FxKind.Custom, Elem.Lightning),
            S("wall-of-fire", "Wall of Fire", 4, E, 9, Area, 7, "A burning cross on the floor. Spreads through brush and oil; creatures in it catch fire.", 1).Look(FxKind.Eruption, Elem.Fire, '^'),
            S("meteor", "Meteor", 5, E, 15, Area, 8, "A burning rock from nowhere. Devastates 3 cells around.", 3).Look(FxKind.Meteor, Elem.Fire),
            S("ice-lance", "Ice Lance", 2, E, 4, Mon, 8, "A spear of ice. Chills the target and freezes any water it stands in.").Look(FxKind.Bolt, Elem.Cold, '\0'),
            S("steam-burst", "Steam Burst", 3, E, 6, Area, 7, "Boils water into scalding steam: heavy damage to anything wet within 2 cells, and the water is gone. Dry ground only hisses.", 2).Look(FxKind.Cloud, Elem.Water),

            S("ember-dart", "Ember Dart", 1, E, 2, Mon, 8, "A mote of flame. Sets the target alight, now and then.").Dmg(Fire, 2, 4, 3, 0, "An ember sears").Ride(Rider.Burn, 35).Look(FxKind.Bolt, Elem.Fire, '*'),
            S("frostbite", "Frostbite", 1, E, 2, Mon, 6, "A bite of cold that slows the target.").Dmg(Cold, 2, 4, 3, 0, "Frost bites").Ride(Rider.Slow, 50).Look(FxKind.Bolt, Elem.Cold, '*'),
            S("spark", "Spark", 1, E, 2, Mon, 7, "A snap of lightning. Quick and cheap.").Dmg(Elec, 2, 4, 3, 0, "A spark leaps into").Look(FxKind.Zap, Elem.Lightning),
            S("burning-hands", "Burning Hands", 1, E, 3, Cn, 3, "A fan of flame from your fingertips, three cells long.", 3).Dmg(Fire, 2, 6, 3, 0, "Flame washes over").Look(FxKind.Cone, Elem.Fire),
            S("acid-splash", "Acid Splash", 1, E, 3, Mon, 7, "A glob of acid. Eats armour: the target takes more from everything for a while.").Dmg(Pois, 2, 6, 3, 0, "Acid splashes").Ride(Rider.Weaken, 60, 8).Look(FxKind.Bolt, Elem.Poison, 'o'),
            S("thunderclap", "Thunderclap", 2, E, 4, Self, 0, "A deafening clap: everything within 2 cells is hurt and may be stunned.", 2).Dmg(Phys, 2, 6, 3, 0, "Thunder batters").Ride(Rider.Stun, 40, 2).Look(FxKind.Nova, Elem.Wind),
            S("scorching-ray", "Scorching Ray", 2, E, 5, Mon, 8, "A needle of white fire. Sets the target alight.").Dmg(Fire, 3, 6, 3, 0, "A scorching ray burns").Ride(Rider.Burn, 70).Look(FxKind.Beam, Elem.Fire),
            S("stone-shard", "Stone Shard", 2, E, 4, Mon, 7, "A jagged stone flung hard.").Dmg(Phys, 3, 4, 3, 0, "A stone shard hits").Look(FxKind.Bolt, Elem.Earth, '#'),
            S("gust-of-wind", "Gust of Wind", 2, E, 3, Cn, 4, "A blast of air that batters and shoves things back two cells.", 4).Dmg(Phys, 1, 6, 4, 0, "A gust slams into").Shove(2).Look(FxKind.Wave, Elem.Wind),
            S("forked-lightning", "Forked Lightning", 2, E, 5, Mon, 8, "Lightning that forks to one more creature nearby.").Dmg(Elec, 2, 6, 4, 0, "Forked lightning strikes").Chain(1).Look(FxKind.Custom, Elem.Lightning),
            S("poison-cloud", "Poison Cloud", 3, E, 6, Area, 7, "A foul green cloud. Everything in it is poisoned for a few turns.", 2).Dmg(Pois, 1, 6, 4, 0, "The poison cloud chokes").Ride(Rider.Poison, 85, 6).Look(FxKind.Cloud, Elem.Poison),
            S("lava-bolt", "Lava Bolt", 3, E, 7, Mon, 7, "A glob of molten rock. Burns, and leaves flames on the floor where it lands.").Dmg(Fire, 4, 6, 3, 0, "Molten rock splashes").Ride(Rider.Burn, 50).Surf(SurfaceKind.Fire, 5).Look(FxKind.Bolt, Elem.Fire, 'O'),
            S("cone-of-cold", "Cone of Cold", 4, E, 10, Cn, 5, "A cone of killing frost, five cells long. Chills what survives.", 5).Dmg(Cold, 4, 6, 3, 0, "Killing frost sweeps over").Ride(Rider.Slow, 70, 8).Look(FxKind.Cone, Elem.Cold),
            S("ice-storm", "Ice Storm", 4, E, 10, Area, 8, "Hail the size of fists, in a wide patch.", 3).Dmg(Cold, 3, 6, 3, 0, "Hail hammers").Ride(Rider.Slow, 60, 6).Look(FxKind.Rain, Elem.Cold, '*'),
            S("call-lightning", "Call Lightning", 4, E, 9, Self, 0, "Three bolts fall from nowhere on creatures in view.").Dmg(Elec, 3, 6, 4, 0, "Lightning falls on").Scatter(3, FxKind.Pillar).Look(FxKind.Custom, Elem.Lightning),
            S("prismatic-spray", "Prismatic Spray", 4, E, 10, Cn, 4, "A fan of coloured light: hurts and dazes.", 4).Dmg(Phys, 3, 6, 3, 0, "Coloured light flays").Ride(Rider.Confuse, 60, 8).Look(FxKind.Cone, Elem.Mind),
            S("disintegrate", "Disintegrate", 5, E, 16, Mon, 7, "A green ray that unmakes what it touches.").Dmg(Phys, 6, 6, 2, 0, "The green ray unmakes").Look(FxKind.Beam, Elem.Poison),
            S("sunburst", "Sunburst", 5, E, 16, Area, 8, "A sun falls: it scours everything within 3 cells and blinds what it does not kill.", 3).Dmg(Holy, 5, 6, 3, 0, "Sunlight scours").Ride(Rider.Blind, 80, 6).Look(FxKind.Meteor, Elem.Holy),
            S("earthquake", "Earthquake", 5, E, 15, Self, 0, "The ground heaves for 4 cells around you. Hurts and stuns everything standing on it.", 4).Dmg(Phys, 5, 6, 3, 0, "The ground heaves under").Ride(Rider.Stun, 50, 2).Look(FxKind.Eruption, Elem.Earth, '^'),
        }; }

        static SpellDef[] Conjuration() { const School C = School.Conjuration; return new[] {
            S("familiar", "Familiar", 1, C, 3, Self, 0, "Calls a small beast to fight for you for a time.", 0, 1),
            S("summon-beast", "Summon Beast", 2, C, 5, Self, 0, "Calls a stronger beast as your level grows.", 0, 1),
            S("create-water", "Create Water", 2, C, 4, Area, 7, "Floods the floor around a spot. The wet burn less and conduct lightning; frost turns it to ice.", 2).Look(FxKind.Rain, Elem.Water, '\''),
            S("create-oil", "Create Oil", 1, C, 3, Area, 6, "Slicks the floor around a spot with oil. It burns long and slides the unwary. Mind your torches.", 1).Look(FxKind.Cloud, Elem.Shadow),
            S("blink", "Blink", 3, C, 5, Cell, 6, "Step through space to a spot you can see.").Look(FxKind.Teleport, Elem.Arcane),
            S("teleport", "Teleport", 4, C, 9, Self, 0, "Throws you to a random place on this level."),

            S("spectral-blade", "Spectral Blade", 1, C, 3, Self, 0, "A sword of cold light fights beside you for a while.").Call("spectral blade", 1, 40),
            S("phase-step", "Phase Step", 2, C, 3, Cell, 4, "A short hop through the walls of the world.").Spec("blink").Look(FxKind.Teleport, Elem.Arcane),
            S("web", "Web", 2, C, 4, Area, 7, "Sticky strands in a small patch. Whatever they touch is stuck for a few turns.", 1).Ride(Rider.Root, 85, 5).Look(FxKind.Cloud, Elem.Wind),
            S("fog-cloud", "Fog Cloud", 2, C, 4, Self, 0, "A thick fog around you: foes within 2 cells grope blindly, and you are harder to hit.", 2).Ride(Rider.Blind, 70, 5).Aura("blur", 15).Look(FxKind.Cloud, Elem.Wind),
            S("summon-swarm", "Summon Swarm", 2, C, 5, Self, 0, "A cloud of bats answers your call.", 0, 3).Call("summoned bat", 3, 40),
            S("mirror-image", "Mirror Image", 3, C, 6, Self, 0, "Two duplicates of you flicker beside you, drawing blows meant for you.", 0, 2).Call("mirror image", 2, 30),
            S("swap-places", "Swap Places", 3, C, 6, Mon, 8, "You and the target trade places in a blink.").Spec("swap").Look(FxKind.Teleport, Elem.Mind),
            S("frozen-ground", "Frozen Ground", 3, C, 5, Area, 7, "Ice spreads across the floor, and what stands on it slows.", 2).Surf(SurfaceKind.Ice, 40).Ride(Rider.Slow, 50, 6).Look(FxKind.Cloud, Elem.Cold),
            S("banish", "Banish", 4, C, 10, Mon, 6, "Throws the target away to some other part of the level.").Spec("banish").Look(FxKind.Implode, Elem.Shadow),
            S("dimension-door", "Dimension Door", 4, C, 8, Cell, 12, "A long step through the walls of the world.").Spec("blink").Look(FxKind.Teleport, Elem.Arcane),
            S("summon-fire-elemental", "Summon Fire Elemental", 4, C, 10, Self, 0, "A fire elemental claws up out of nothing to serve you.", 0, 1).Call("fire elemental", 1, 100).Look(FxKind.None, Elem.Fire),
            S("summon-water-elemental", "Summon Water Elemental", 4, C, 10, Self, 0, "A water elemental rises, dripping, to serve you.", 0, 1).Call("water elemental", 1, 100),
            S("summon-earth-elemental", "Summon Earth Elemental", 4, C, 10, Self, 0, "An earth elemental pulls itself from the floor to serve you.", 0, 1).Call("earth elemental", 1, 100),
            S("summon-air-elemental", "Summon Air Elemental", 4, C, 10, Self, 0, "A whirling air elemental answers you.", 0, 1).Call("air elemental", 1, 100),
        }; }

        static SpellDef[] Alteration() { const School A = School.Alteration; return new[] {
            S("ward", "Ward", 1, A, 2, Self, 0, "A shimmering shield: AC +3 for a while.").Look(FxKind.Swirl, Elem.Arcane),
            S("haste", "Haste", 3, A, 6, Self, 0, "You act twice as often as everything else, briefly.").Look(FxKind.Swirl, Elem.Lightning),
            S("slow", "Slow", 3, A, 5, Mon, 6, "Halves a creature's speed for a time. Strong ones resist.").Look(FxKind.Mark, Elem.Water, '~'),
            S("clairvoyance", "Clairvoyance", 3, A, 7, Self, 0, "The whole level unfolds in your mind.", 7).Look(FxKind.Nova, Elem.Mind),
            S("stone-skin", "Stone Skin", 4, A, 9, Self, 0, "Your skin hardens: AC +6 for a long while.").Look(FxKind.Swirl, Elem.Earth),

            S("light", "Light", 1, A, 2, Self, 0, "A burst of light shows the ground about you.", 5).Spec("light").Look(FxKind.Nova, Elem.Holy),
            S("detect-traps", "Find Traps", 1, A, 2, Self, 0, "Shows every trap within 10 cells.", 5).Spec("detect-traps").Look(FxKind.Nova, Elem.Earth),
            S("knock", "Knock", 1, A, 2, Self, 0, "Unlocks and opens every locked door within 3 cells.").Spec("knock").Look(FxKind.Swirl, Elem.Arcane),
            S("mage-armor", "Mage Armor", 1, A, 3, Self, 0, "Plates of unseen force: AC +4 for a long while.").Aura("mage-armor", 100).Look(FxKind.Swirl, Elem.Arcane),
            S("arcane-shield", "Shield", 1, A, 2, Self, 0, "A disc of force in front of you: AC +6, but only for a few turns.").Aura("arcane-shield", 12).Look(FxKind.Swirl, Elem.Arcane),
            S("detect-monsters", "Sense Life", 2, A, 3, Self, 0, "For a while you feel every living thing within 12 cells, through walls.", 8).Aura("sense", 40).Look(FxKind.Nova, Elem.Mind),
            S("identify", "Identify", 2, A, 4, Self, 0, "Everything you carry and wear shows what it is.").Spec("identify").Look(FxKind.Swirl, Elem.Mind),
            S("bulls-strength", "Bull's Strength", 2, A, 4, Self, 0, "Strength +3 for a long while.").Aura("bulls-strength", 80).Look(FxKind.Swirl, Elem.Blood),
            S("cats-grace", "Cat's Grace", 2, A, 4, Self, 0, "Dexterity +3 and a little evasion, for a long while.").Aura("cats-grace", 80).Look(FxKind.Swirl, Elem.Nature),
            S("foxs-cunning", "Fox's Cunning", 2, A, 4, Self, 0, "Intelligence +3 for a long while. Your mana pool grows with it.").Aura("foxs-cunning", 80).Look(FxKind.Swirl, Elem.Mind),
            S("owls-wisdom", "Owl's Wisdom", 2, A, 4, Self, 0, "Wisdom +3 for a long while. Your mana pool grows with it, if you cast by Wisdom.").Aura("owls-wisdom", 80).Look(FxKind.Swirl, Elem.Holy),
            S("endure-elements", "Endure Elements", 2, A, 4, Self, 0, "Fire and cold 30%, lightning 20%, for a long while.").Aura("endure", 80).Look(FxKind.Swirl, Elem.Water),
            S("fire-ward", "Fire Ward", 2, A, 3, Self, 0, "Fire resistance 60% for a while.").Aura("fire-ward", 50).Look(FxKind.Swirl, Elem.Fire),
            S("frost-ward", "Frost Ward", 2, A, 3, Self, 0, "Cold resistance 60% for a while.").Aura("frost-ward", 50).Look(FxKind.Swirl, Elem.Cold),
            S("storm-ward", "Storm Ward", 2, A, 3, Self, 0, "Lightning resistance 60% for a while.").Aura("storm-ward", 50).Look(FxKind.Swirl, Elem.Lightning),
            S("blur", "Blur", 2, A, 4, Self, 0, "Your outline smears: evasion +4 for a while.").Aura("blur", 40).Look(FxKind.Swirl, Elem.Wind),
            S("arcane-focus", "Arcane Focus", 3, A, 5, Self, 0, "Your spells hit harder (+3) and fizzle less (-10%) for a while.").Aura("arcane-focus", 50).Look(FxKind.Swirl, Elem.Arcane),
            S("levitate", "Levitate", 3, A, 5, Self, 0, "You float off the floor: traps cannot reach you.").Aura("levitating", 40).Look(FxKind.Rise, Elem.Wind, '\''),
            S("dig", "Dig", 3, A, 6, Ln, 6, "Bores through up to six cells of diggable rock in a line.").Spec("dig").Look(FxKind.Beam, Elem.Earth),
            S("enlarge", "Enlarge", 4, A, 9, Self, 0, "You grow to half again your size: Str +4, Con +3, +15 HP.").Aura("enlarge", 50).Look(FxKind.Swirl, Elem.Blood),
        }; }

        static SpellDef[] Illusion() { const School I = School.Illusion; return new[] {
            S("sleep", "Sleep", 2, I, 4, Mon, 6, "Puts a living foe to sleep. Strong ones resist; damage wakes it.").Look(FxKind.Mark, Elem.Mind, 'z'),
            S("confuse", "Confuse", 2, I, 4, Mon, 6, "The target staggers about at random.").Look(FxKind.Mark, Elem.Mind, '?'),
            S("invisibility", "Invisibility", 2, I, 5, Self, 0, "Foes lose track of you and strike at you worse.").Look(FxKind.Swirl, Elem.Shadow),
            S("charm", "Charm Monster", 4, I, 10, Mon, 6, "A living foe fights for you for a while. Hard on strong ones.").Look(FxKind.Mark, Elem.Mind, '♥'),

            S("dazzle", "Dazzle", 1, I, 2, Mon, 7, "A flash that leaves the target blundering blind.").Ride(Rider.Blind, 80, 6).Look(FxKind.Mark, Elem.Holy, '*'),
            S("color-spray", "Color Spray", 1, I, 3, Cn, 3, "A fan of dizzying colours, three cells long.", 3).Ride(Rider.Confuse, 65, 8).Look(FxKind.Cone, Elem.Mind),
            S("befuddle", "Befuddle", 2, I, 3, Area, 6, "A whorl of nonsense around a spot: creatures in it stagger.", 1).Ride(Rider.Confuse, 70, 8).Look(FxKind.Cloud, Elem.Mind),
            S("suggestion", "Suggestion", 2, I, 5, Mon, 6, "A whispered idea: the target serves you for a short while.").Ride(Rider.Charm, 40, 15).Look(FxKind.Mark, Elem.Mind, '♥'),
            S("displacement", "Displacement", 2, I, 4, Self, 0, "Your image is a hand's breadth off from where you stand.").Aura("displacement", 40).Look(FxKind.Swirl, Elem.Mind),
            S("hypnotic-pattern", "Hypnotic Pattern", 3, I, 7, Area, 6, "A weaving of light that puts creatures within 2 cells to sleep.", 2).Ride(Rider.Sleep, 70, 14).Look(FxKind.Cloud, Elem.Mind),
            S("terrify", "Terrify", 3, I, 6, Self, 0, "Every creature within 4 cells sees its worst fear.", 4).Ride(Rider.Fear, 70, 10).Look(FxKind.Nova, Elem.Shadow),
            S("phantasmal-killer", "Phantasmal Killer", 4, I, 10, Mon, 6, "The target's own fear takes shape and strikes.").Dmg(Phys, 4, 6, 3, 0, "A phantom claws").Ride(Rider.Fear, 60, 10).Look(FxKind.Mark, Elem.Mind, '☻'),
            S("nightmare", "Nightmare", 4, I, 9, Mon, 6, "A waking nightmare: hurts, and the target flees in terror.").Dmg(Phys, 3, 6, 3, 0, "A nightmare rends").Ride(Rider.Fear, 100, 12).Look(FxKind.Mark, Elem.Shadow, '!'),
            S("mass-charm", "Mass Charm", 5, I, 16, Area, 6, "A wave of affection for you: every creature within 3 cells may turn on its friends.", 3).Ride(Rider.Charm, 50, 30).Look(FxKind.Cloud, Elem.Mind),
        }; }
    }
}
