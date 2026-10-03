namespace Ossuary.Core.Magic
{
    /// <summary>A second round for every school (sixty more spells), written after the first catalogue.</summary>
    public static partial class Spells
    {
        static SpellDef[] More() { const School E = School.Evocation, C = School.Conjuration, A = School.Alteration, I = School.Illusion, N = School.Necromancy, H = School.Sacred, R = School.Nature, D = School.Shadow; return new[] {
            // ---- Evocation
            S("flame-arrow", "Flame Arrow", 1, E, 3, Mon, 8, "An arrow of fire that never needs a bow. May set the target alight.").Dmg(Fire, 2, 6, 3, 0, "A flame arrow burns").Ride(Rider.Burn, 40).Look(FxKind.Bolt, Elem.Fire, '\0'),
            S("thunderstrike", "Thunderstrike", 2, E, 5, Mon, 7, "A bolt from a clear sky. It may stun.").Dmg(Elec, 3, 6, 3, 0, "Thunder strikes").Ride(Rider.Stun, 40, 2).Look(FxKind.Pillar, Elem.Lightning),
            S("searing-orb", "Searing Orb", 3, E, 7, Area, 7, "A small sun that bursts on contact, 1 cell wide.", 1).Dmg(Fire, 4, 6, 3, 0, "A searing orb bursts on").Ride(Rider.Burn, 60).Look(FxKind.Ball, Elem.Fire, 'O'),
            S("ice-comet", "Ice Comet", 3, E, 7, Area, 8, "A comet of ice that slows what it does not kill.", 1).Dmg(Cold, 4, 6, 3, 0, "An ice comet shatters on").Ride(Rider.Slow, 70, 6).Look(FxKind.Meteor, Elem.Cold),
            S("arc-flash", "Arc Flash", 3, E, 6, Cn, 4, "A fan of lightning four cells long that may stun.", 4).Dmg(Elec, 3, 6, 3, 0, "An arc of lightning scorches").Ride(Rider.Stun, 30, 2).Look(FxKind.Cone, Elem.Lightning),
            S("magma-wave", "Magma Wave", 4, E, 10, Cn, 5, "A wave of molten rock, five cells long, that sets everything alight.", 5).Dmg(Fire, 5, 6, 3, 0, "Magma washes over").Ride(Rider.Burn, 80).Look(FxKind.Cone, Elem.Fire),
            S("static-field", "Static Field", 4, E, 9, Self, 0, "Lightning crawls over everything within 3 cells of you, and may stun it.", 3).Dmg(Elec, 3, 6, 3, 0, "Static lightning shocks").Ride(Rider.Stun, 40, 2).Look(FxKind.Nova, Elem.Lightning),
            S("arcane-barrage", "Arcane Barrage", 4, E, 9, Self, 0, "Four missiles of force, each finding a creature in view.").Dmg(Phys, 3, 6, 4, 0, "A missile of force strikes").Scatter(4, FxKind.Bolt).Look(FxKind.Custom, Elem.Arcane, '*'),
            S("rimeblast", "Rimeblast", 5, E, 14, Self, 0, "A blast of killing frost 5 cells around you that slows what lives.", 5).Dmg(Cold, 5, 6, 3, 0, "Rime blasts").Ride(Rider.Slow, 80, 8).Look(FxKind.Nova, Elem.Cold),
            S("sun-lance", "Sun Lance", 5, E, 15, Ln, 8, "A lance of white-hot light through everything in a line.").Dmg(Fire, 7, 6, 2, 0, "The sun lance pierces").Ride(Rider.Burn, 80).Look(FxKind.Beam, Elem.Holy),

            // ---- Conjuration
            S("summon-imp", "Summon Imp", 2, C, 5, Self, 0, "A small, spiteful imp serves you for a while.", 0, 1).Call("imp", 1, 80),
            S("arcane-hound", "Arcane Hound", 3, C, 7, Self, 0, "A hound of purple fire runs at your side.", 0, 1).Call("arcane hound", 1, 100),
            S("storm-sprite", "Storm Sprite", 3, C, 7, Self, 0, "A crackling sprite darts at your enemies.", 0, 1).Call("storm sprite", 1, 90),
            S("pull-through", "Pull Through", 3, C, 6, Mon, 8, "A hand through the world drags the target to your side.").Shove(-6).Look(FxKind.Beam, Elem.Mind),
            S("stone-sentinel", "Stone Sentinel", 4, C, 10, Self, 0, "A sentinel of carved stone stands guard over you.", 0, 1).Call("stone sentinel", 1, 120),
            S("gargoyle-guard", "Gargoyle Guard", 5, C, 14, Self, 0, "A winged gargoyle unfolds from nothing to guard you.", 0, 1).Call("gargoyle", 1, 150),

            // ---- Alteration
            S("lightfoot", "Lightfoot", 1, A, 2, Self, 0, "Your step goes quiet: harder to notice, a little harder to hit.").Aura("lightfoot", 60).Look(FxKind.Swirl, Elem.Wind),
            S("keen-edge", "Keen Edge", 2, A, 4, Self, 0, "Your weapon finds its mark: +3 to hit, +1 damage.").Aura("keen-edge", 50).Look(FxKind.Swirl, Elem.Wind),
            S("aegis", "Aegis", 3, A, 6, Self, 0, "A ward against everything: AC +3 and 15% resistance to fire, cold and lightning.").Aura("aegis", 50).Look(FxKind.Swirl, Elem.Arcane),
            S("arcane-survey", "Arcane Survey", 3, A, 6, Self, 0, "You see the shape of the land and every trap in it, 14 cells around.", 8).Spec("commune").Look(FxKind.Nova, Elem.Arcane),
            S("iron-body", "Iron Body", 4, A, 10, Self, 0, "You are, briefly, a statue that moves: AC +5, poison 40%, a little slower to dodge.").Aura("iron-body", 60).Look(FxKind.Swirl, Elem.Earth),
            S("titans-might", "Titan's Might", 5, A, 14, Self, 0, "Str +6, Con +4, +25 HP for a good while.").Aura("titan", 40).Look(FxKind.Swirl, Elem.Blood),

            // ---- Illusion
            S("phantom-guard", "Phantom Guard", 2, I, 4, Self, 0, "A duplicate of you flickers at your side and takes the blows meant for you.", 0, 1).Call("mirror image", 1, 30),
            S("lullaby", "Lullaby", 2, I, 4, Cn, 3, "A hum three cells long that may put the living to sleep.", 3).Ride(Rider.Sleep, 60, 12).Look(FxKind.Cone, Elem.Mind),
            S("bewilder", "Bewilder", 3, I, 6, Cn, 4, "A cone of nonsense: everything in it staggers.", 4).Ride(Rider.Confuse, 70, 8).Look(FxKind.Cone, Elem.Mind),
            S("mind-fog", "Mind Fog", 3, I, 7, Area, 6, "A fog on the mind: those in it fight poorly and take more from everything.", 2).Ride(Rider.Weaken, 100, 15).Look(FxKind.Cloud, Elem.Mind),
            S("deep-slumber", "Deep Slumber", 4, I, 9, Mon, 6, "A sleep that is very hard to shake.").Ride(Rider.Sleep, 95, 30).Look(FxKind.Mark, Elem.Mind, 'z'),
            S("spectral-horror", "Spectral Horror", 5, I, 15, Area, 6, "A horror made of the target's own dread: hurts, and sends everything within 3 cells running.", 3).Dmg(Phys, 5, 6, 3, 0, "A spectral horror claws").Ride(Rider.Fear, 90, 12).Look(FxKind.Cloud, Elem.Shadow),

            // ---- Necromancy
            S("death-grip", "Death Grip", 2, N, 4, Mon, 6, "A cold hand pulls the target in and wounds it.").Dmg(Necro, 2, 6, 3, 0, "A dead hand drags").Shove(-4).Look(FxKind.Beam, Elem.Necrotic),
            S("soul-bolt", "Soul Bolt", 2, N, 4, Mon, 7, "A bolt of stolen soul; a third of it comes back to you.").Dmg(Necro, 3, 6, 3, 0, "A soul bolt tears at").Drain(33).Look(FxKind.Bolt, Elem.Necrotic, 'o'),
            S("grave-hands", "Grave Hands", 2, N, 3, Area, 6, "Hands claw up out of the floor and hold what stands there.", 1).Ride(Rider.Root, 85, 5).Look(FxKind.Eruption, Elem.Necrotic, '^'),
            S("miasma", "Miasma", 3, N, 6, Area, 7, "A foul cloud that sickens and weakens.", 2).Dmg(Pois, 2, 6, 3, 0, "Miasma chokes").Ride(Rider.Weaken, 100, 12).Look(FxKind.Cloud, Elem.Poison),
            S("blight", "Blight", 3, N, 7, Mon, 6, "Rots the target from the inside: poison, and it takes more from everything.").Dmg(Pois, 4, 6, 3, 0, "Blight withers").Ride(Rider.Poison, 100, 8).Look(FxKind.Bolt, Elem.Poison, 'x'),
            S("bone-cage", "Bone Cage", 4, N, 9, Area, 7, "Ribs of bone close around everything within 2 cells.", 2).Dmg(Phys, 2, 6, 3, 0, "Bone ribs crush").Ride(Rider.Root, 95, 6).Look(FxKind.Eruption, Elem.Earth, '^'),
            S("lich-form", "Lich Form", 5, N, 14, Self, 0, "For a time you are most of the way to dead: strong against the grave, cold and poison, and a drinker of life. The Ossuary takes 3 points.").Aura("lich-form", 50).Taint(3).Look(FxKind.Swirl, Elem.Necrotic),
            S("wail-of-the-damned", "Wail of the Damned", 5, N, 15, Self, 0, "A scream from the pit hurts and sends everything within 6 cells running.", 6).Dmg(Necro, 5, 6, 3, 0, "The damned wail through").Ride(Rider.Fear, 80, 12).Taint(1).Look(FxKind.Nova, Elem.Shadow),

            // ---- Sacred
            S("aura-of-courage", "Aura of Courage", 2, H, 4, Self, 0, "Courage steadies you: +2 to hit and +6 HP.").Aura("courage", 60).Look(FxKind.Swirl, Elem.Holy),
            S("holy-weapon", "Holy Weapon", 2, H, 4, Self, 0, "Your weapon shines: +1d6 holy and +1 to hit.").Aura("holy-weapon", 40).Look(FxKind.Swirl, Elem.Holy),
            S("mercy-touch", "Mercy Touch", 3, H, 5, Self, 0, "A laying on of hands: 3d6 plus Wisdom.").Heal(3, 6, 0).Look(FxKind.Rise, Elem.Holy, '+'),
            S("divine-shield", "Divine Shield", 3, H, 7, Self, 0, "A shield of light: AC +6 for a short while.").Aura("divine-shield", 15).Look(FxKind.Swirl, Elem.Holy),
            S("cleansing-nova", "Cleansing Nova", 3, H, 7, Self, 0, "A ring of white fire 2 cells around you that blinds as it burns.", 2).Dmg(Holy, 3, 6, 3, 0, "White fire cleanses").Ride(Rider.Blind, 70, 5).Look(FxKind.Nova, Elem.Holy),
            S("sacred-ground", "Sacred Ground", 4, H, 9, Area, 7, "A patch of floor where the light stands up and burns.", 3).Dmg(Holy, 2, 6, 3, 0, "The ground blazes under").Ride(Rider.Slow, 70, 6).Look(FxKind.Eruption, Elem.Holy, '+'),
            S("benediction", "Benediction", 4, H, 10, Self, 0, "A full blessing: AC +2, +2 to hit, +2 damage, necrotic 30%, and a mending.").Aura("benediction", 50).Heal(2, 8, 0).Look(FxKind.Pillar, Elem.Holy),
            S("wrath-of-heaven", "Wrath of Heaven", 5, H, 15, Self, 0, "Five pillars of fire on five creatures in view.").Dmg(Holy, 5, 6, 4, 0, "A pillar of light falls on").Scatter(5, FxKind.Pillar).Look(FxKind.Custom, Elem.Holy),

            // ---- Nature
            S("wasp-swarm", "Wasp Swarm", 2, R, 4, Area, 6, "Angry wasps in a small patch: stings and poison.", 2).Dmg(Pois, 2, 4, 3, 0, "Wasps sting").Ride(Rider.Poison, 70, 5).Look(FxKind.Cloud, Elem.Nature),
            S("quicksand", "Quicksand", 2, R, 4, Area, 6, "The floor goes soft and wet; whatever stands on it sinks and sticks.", 1).Surf(SurfaceKind.Water, 30).Ride(Rider.Root, 70, 4).Look(FxKind.Eruption, Elem.Earth, '~'),
            S("hail-of-thorns", "Hail of Thorns", 3, R, 6, Self, 0, "Three volleys of thorns on creatures in view.").Dmg(Phys, 2, 6, 4, 0, "Thorns rake").Scatter(3, FxKind.Bolt).Look(FxKind.Custom, Elem.Nature, '\0'),
            S("sun-bolt", "Sun Bolt", 3, R, 6, Mon, 8, "A ray of concentrated sunlight that sets the target alight.").Dmg(Fire, 4, 6, 3, 0, "Sunlight burns").Ride(Rider.Burn, 70).Look(FxKind.Beam, Elem.Holy),
            S("stoneform", "Stoneform", 3, R, 6, Self, 0, "Your skin takes the grain of stone: AC +5, fire 20%, a little clumsy.").Aura("stoneform", 60).Look(FxKind.Swirl, Elem.Earth),
            S("regrowth", "Regrowth", 3, R, 6, Self, 0, "Green life closes your wounds: 3d8 plus Wisdom.").Heal(3, 8, 0).Look(FxKind.Rise, Elem.Nature, '+'),
            S("tidal-surge", "Tidal Surge", 4, R, 9, Cn, 5, "A wall of water five cells long: batters, slows and throws creatures back.", 5).Dmg(Phys, 3, 6, 3, 0, "The tide slams into").Ride(Rider.Slow, 70, 6).Shove(3).Look(FxKind.Wave, Elem.Water),
            S("frostwind", "Frostwind", 4, R, 9, Cn, 5, "A wind that cuts like ice, five cells long.", 5).Dmg(Cold, 4, 6, 3, 0, "Frostwind flays").Shove(2).Ride(Rider.Slow, 60, 6).Look(FxKind.Wave, Elem.Cold),

            // ---- Shadow
            S("garrote", "Garrote", 2, D, 4, Mon, 1, "A wire from nowhere: hurts and opens a wound.").Dmg(Phys, 3, 6, 3, 0, "A wire bites").Ride(Rider.Bleed, 100, 8).Look(FxKind.Slash, Elem.Blood),
            S("sleeping-dust", "Sleeping Dust", 2, D, 4, Cn, 3, "A pinch of dust three cells long that may put the living to sleep.", 3).Ride(Rider.Sleep, 70, 10).Look(FxKind.Cone, Elem.Earth),
            S("snare", "Snare", 2, D, 4, Area, 5, "A hidden snare: hurts and holds whatever steps in it.", 1).Dmg(Phys, 2, 6, 4, 0, "A snare bites").Ride(Rider.Root, 90, 4).Look(FxKind.Eruption, Elem.Earth, '^'),
            S("nightshade", "Nightshade", 3, D, 6, Mon, 6, "A drop of nightshade at a distance: poison that does not let go.").Dmg(Pois, 3, 6, 3, 0, "Nightshade burns").Ride(Rider.Poison, 100, 10).Look(FxKind.Bolt, Elem.Poison, '\0'),
            S("shadow-leap", "Shadow Leap", 3, D, 5, Cell, 8, "Out of one shadow and into another, eight cells away.").Spec("blink").Look(FxKind.Teleport, Elem.Shadow),
            S("ambush", "Ambush", 3, D, 6, Self, 0, "Patience pays: +4 to hit, +3 damage and well hidden, for a short while.").Aura("ambush", 15).Look(FxKind.Swirl, Elem.Shadow),
            S("vampiric-veil", "Vampiric Veil", 4, D, 9, Self, 0, "A veil that drinks: 20% life steal, evasion +3, harder to notice.").Aura("vampiric-veil", 30).Look(FxKind.Swirl, Elem.Blood),
            S("chain-of-shadows", "Chain of Shadows", 4, D, 9, Mon, 6, "Shadow leaps from one creature to the next and holds each for a moment.").Dmg(Necro, 3, 6, 3, 0, "Shadow clamps on").Chain(2).Ride(Rider.Root, 60, 3).Look(FxKind.Custom, Elem.Shadow),
        }; }
    }
}
