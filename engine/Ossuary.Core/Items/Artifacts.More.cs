using Ossuary.Core.Entities;

namespace Ossuary.Core.Items
{
    /// <summary>
    /// The unique items after the first thirteen: weapons, armour, rings and amulets, many of which let you cast a spell while you
    /// hold them. Each sits on a level of one branch with a chance (several may share a level); four are sets.
    /// </summary>
    public static partial class Artifacts
    {
        const string Dun = "The Dungeons", Min = "The Mines of Dwarfdeep", War = "The Warrens", Vau = "The Sunken Vaults", Spi = "The Ashen Spire", Ann = "The Annex";

        static ArtifactDef U(string id, string name, string baseName, string branch, int depth, int chance, int enchant, string lore, ItemMods mods, string[] grants = null, string set = null, bool corrupts = false)
            => new ArtifactDef { Id = id, Name = name, Base = baseName, Branch = branch, Depth = depth, Chance = chance, Enchant = enchant, Lore = lore, Mods = mods, Grants = grants, Set = set, Corrupts = corrupts };

        static ArtifactDef[] More() => new[]
        {
            // ---------------------------------------------------------- The Dungeons
            U("gaolers-keyring", "Gaoler's Keyring", "silver band", Dun, 3, 40, 0, "A ring of keys melted into one band. Every lock gave way to it but the one on the way out.",
                new ItemMods { Stealth = 1, Dex = 1 }, new[] { "knock", "detect-traps" }),
            U("ratcatchers-cudgel", "Ratcatcher's Cudgel", "club", Dun, 2, 50, 2, "Knotted oak, black with old blood. The rats never learned to fear it, and the ones who tried are in the walls.",
                new ItemMods { Dmg = 2, ExtraType = DamageType.Poison, ExtraSides = 3 }),
            U("mourners-blade", "Mourner's Blade", "stiletto", Dun, 5, 45, 2, "Made for one purpose, which was to be quiet. It has outlived the reason.",
                new ItemMods { Dex = 1, ExtraType = DamageType.Necrotic, ExtraSides = 4 }, new[] { "assassinate" }),
            U("prisoners-prayer", "Prisoner's Prayer", "amulet of faith", Dun, 4, 40, 0, "Scratched into a coin with a spoon by someone who did not expect to be heard. Someone heard.",
                new ItemMods { Hp = 10, Wis = 1 }, new[] { "sanctuary", "cure-wounds" }),
            U("heartwood-staff", "Heartwood Staff", "druid's crook", Dun, 6, 40, 2, "Grown, not cut, in a cellar where nothing should grow. It still leans toward a light that is not there.",
                new ItemMods { Wis = 2, Mp = 8, Stealth = 1 }, new[] { "thorns", "spike-growth" }, "verdant-court"),
            U("wardens-bulwark", "Warden's Bulwark", "tower shield", Dun, 7, 40, 2, "Dented by a thousand prisoners and not one of them got past.",
                new ItemMods { Con = 1, Ac = 1, ResFire = 15 }),
            U("nightglove", "Nightglove", "thieves' gloves", Dun, 4, 35, 2, "Cut from a single black glove, the other of which was never found. It likes to be alone with the dark.",
                new ItemMods { Dex = 1, Stealth = 2 }, new[] { "shadow-bolt", "hex" }, "midnight-cabal"),
            U("stairwalkers-cloak", "Stairwalker's Cloak", "cloak", Dun, 10, 40, 2, "Frayed at the hem from every stair it has ever climbed. It knows the way up better than you do.",
                new ItemMods { Dex = 1, Evasion = 2, Vigor = 6 }, new[] { "blink" }),

            // ---------------------------------------------------------- The Mines of Dwarfdeep
            U("first-vein-pick", "Pickaxe of the First Vein", "war pick", Min, 3, 45, 2, "The pick that struck the first seam, and has not been quiet since.",
                new ItemMods { Str = 1, Dmg = 2 }, new[] { "dig" }),
            U("delvers-lamp-helm", "Delver's Lamp-Helm", "dwarvish helm", Min, 4, 40, 2, "A miner's lamp hammered into a helm. The flame is not oil and has never needed trimming.",
                new ItemMods { ResFire = 20, Hp = 8 }, new[] { "light" }),
            U("stonebinders-gauntlets", "Stonebinder's Gauntlets", "gauntlets", Min, 5, 40, 2, "They close on a hand like a vault door. The stone listens.",
                new ItemMods { Str = 2, Con = 1 }, new[] { "stone-skin" }),
            U("deepmaw-plate", "Deepmaw Plate", "banded mail", Min, 6, 35, 3, "Beaten from the hide of something the dwarves dug into and could not dig out of.",
                new ItemMods { Con = 2, Hp = 12, ResCold = 20 }),
            U("ore-golems-heart", "Ore Golem's Heart", "jade ring", Min, 8, 40, 0, "Cold, heavy, and it beats once a minute. The golem was not finished with it.",
                new ItemMods { Hp = 15, Ac = 2 }, new[] { "tremor" }),
            U("barkhide-vest", "Barkhide Vest", "druid's vestments", Min, 2, 45, 2, "Bark, stitched to bark, over a heart nobody has found.",
                new ItemMods { Wis = 1, Hp = 8, ResPoison = 20 }, new[] { "rejuvenate" }, "verdant-court"),
            U("mantle-of-the-tempest", "Mantle of the Tempest", "cloak of the mage", Min, 7, 35, 2, "It crackles when you take it off. The dwarves called a storm in the deep, once, and this is what was left.",
                new ItemMods { Mp = 8, ResLightning = 30, Evasion = 1 }, new[] { "spark" }, "stormcaller"),

            // ---------------------------------------------------------- The Warrens
            U("rat-kings-whiskers", "Rat King's Whiskers", "bone ring", War, 3, 40, 0, "Twisted into a ring that quivers when anything moves behind you. A great many things do.",
                new ItemMods { Dex = 1, Stealth = 2, ResPoison = 30 }, new[] { "summon-swarm" }),
            U("scrap-kings-cleaver", "Scrap-King's Cleaver", "falchion", War, 4, 40, 2, "Sharpened on everything. It has opinions about what you should cut.",
                new ItemMods { Str = 1, Dmg = 3, ToHit = -1 }),
            U("plaguebearers-mask", "Plaguebearer's Mask", "skull cap of the dead", War, 5, 35, 3, "Wear it and the sickness walks beside you instead of in you. It will want a favour.",
                new ItemMods { ResPoison = 60, ResNecrotic = 30, Con = 1 }, new[] { "plague-bolt", "cloudkill" }, null, true),
            U("burrowers-boots", "Burrower's Boots", "boots of striding", War, 6, 35, 2, "Every step a little further than it should be. The tunnels are not where they were.",
                new ItemMods { Evasion = 2, Dex = 1 }, new[] { "phase-step" }),
            U("mantle-of-many-teeth", "Mantle of Many Teeth", "cloak", War, 2, 45, 2, "Sewn from a hundred small hides, every one of which bit someone.",
                new ItemMods { Con = 1, Hp = 6 }, new[] { "thorns" }),
            U("gloves-of-static", "Gloves of Static", "gloves of spellcasting", War, 7, 35, 2, "Rub them together and the hair stands up on everything alive within ten feet.",
                new ItemMods { SpellFocus = 10, ResLightning = 20 }, new[] { "lightning-lash" }, "stormcaller"),
            U("antlered-crown", "Antlered Crown", "circlet", War, 8, 35, 2, "Antlers grown through bronze. They are, by some measure, still growing.",
                new ItemMods { Wis = 1, Mp = 6, Stealth = 1 }, new[] { "barkskin", "entangle" }, "verdant-court"),

            // ---------------------------------------------------------- The Sunken Vaults
            U("tidecallers-trident", "Tidecaller's Trident", "trident", Vau, 3, 45, 3, "The tide came when it was raised, and went when it was set down. No one has set it down in a long time.",
                new ItemMods { ExtraType = DamageType.Cold, ExtraSides = 6, ResCold = 25 }, new[] { "frozen-ground" }),
            U("pearl-of-the-drowned", "Pearl of the Drowned", "amulet of resistance", Vau, 4, 40, 0, "Pried from the throat of someone who swallowed it on purpose.",
                new ItemMods { Wis = 1, Mp = 10, ResCold = 40 }, new[] { "create-water", "ice-lance" }),
            U("saltwhite-mail", "Saltwhite Mail", "scale mail", Vau, 6, 35, 3, "Crusted white, and it will not rust. Salt remembers every sea it has been.",
                new ItemMods { ResCold = 30, Hp = 10 }, new[] { "frost-ward" }),
            U("lantern-of-the-deep", "Lantern of the Deep", "gold band", Vau, 7, 40, 0, "A ring that glows a little less than the water it came out of.",
                new ItemMods { Stealth = 1, SpellFocus = 8 }, new[] { "detect-monsters", "light" }),
            U("midnight-hood", "Midnight Hood", "hood of shadows", Vau, 8, 35, 2, "It is always the hour just after the lamps go out.",
                new ItemMods { Stealth = 2, Evasion = 2 }, new[] { "fade" }, "midnight-cabal"),
            U("weepers-hands", "Weeper's Hands", "leather gloves", Vau, 9, 40, 2, "Gloves of someone who held on to something for a very long time.",
                new ItemMods { Dex = 1, Evasion = 1 }, new[] { "hold-person" }),
            U("sunken-sceptre", "Sunken Sceptre", "wizard's staff", Vau, 11, 45, 3, "A king's sceptre that was never allowed to be a king's staff. It has been waiting for a better hand.",
                new ItemMods { Int = 2, Mp = 12, SpellPower = 2 }, new[] { "chain-lightning", "steam-burst" }),
            U("lich-queens-crown", "Lich-Queen's Crown", "crown of thorns", Vau, 12, 60, 3, "The first of the three pieces, and the only one that is angry.",
                new ItemMods { Int = 2, SpellPower = 2, ResNecrotic = 30 }, new[] { "enfeeble", "wither" }, "lich-queen"),

            // ---------------------------------------------------------- The Ashen Spire
            U("cindercrown", "Cindercrown", "circlet", Spi, 4, 40, 2, "A circlet of ash that has been burning for three hundred years, with no fuel but you.",
                new ItemMods { Mp = 8, ResFire = 25, SpellPower = 1 }, new[] { "ember-dart", "fire-ward" }),
            U("stormcallers-staff", "Stormcaller's Staff", "wizard's staff", Spi, 5, 35, 3, "The Spire's tower-wizards called lightning to quench the fires. It has a grudge against both.",
                new ItemMods { Int = 2, Mp = 10, SpellPower = 2, ResLightning = 30 }, new[] { "lightning-bolt", "call-lightning" }, "stormcaller"),
            U("pyre-knights-sword", "Pyre-Knight's Sword", "bastard sword", Spi, 6, 40, 3, "A sword that rode into the Spire to put out a fire. It came out on fire.",
                new ItemMods { ExtraType = DamageType.Fire, ExtraSides = 6, ResFire = 30, Str = 1 }),
            U("ashmonks-beads", "Ashmonk's Beads", "amulet of the grave", Spi, 7, 40, 0, "Prayer beads of bone and cinder. Each one is a name.",
                new ItemMods { ResNecrotic = 40, Wis = 1, Mp = 6 }, new[] { "turn-undead", "searing-light" }),
            U("lich-queens-shroud", "Lich-Queen's Shroud", "necromancer's shroud", Spi, 10, 40, 3, "She wore it to her own funeral and then declined to be buried.",
                new ItemMods { Int = 1, Mp = 10, SpellPower = 1 }, new[] { "grave-chill", "death-aura" }, "lich-queen"),
            U("wraithwalkers", "Wraithwalkers", "boots of elvenkind", Spi, 9, 35, 2, "Boots of someone who is, strictly, no longer using them.",
                new ItemMods { Stealth = 2, Evasion = 2, ResNecrotic = 20 }, new[] { "shadow-step" }),
            U("spire-lords-staff", "Spire-Lord's Staff", "wizard's staff", Spi, 12, 45, 3, "The last staff in the Spire still lit. The fire in it is not a flame but a decision.",
                new ItemMods { Int = 3, SpellPower = 3, Mp = 14 }, new[] { "fireball", "meteor" }),
            U("lich-queens-phylactery", "Lich-Queen's Phylactery", "amulet of the grave", Spi, 13, 40, 0, "The last of the three. Whatever she put in it is not what you would expect, and it is not finished.",
                new ItemMods { Mp = 14, SpellPower = 1, ResNecrotic = 30 }, new[] { "siphon-soul" }, "lich-queen"),
            U("regents-tithe", "Regent's Tithe", "gold band", Spi, 14, 40, 0, "Gold, warm, a little heavy. The Regent paid his debts in fire.",
                new ItemMods { Hp = 12, SpellPower = 2, ResFire = 30 }, new[] { "wall-of-fire" }),
            U("final-ember", "Final Ember", "rune shield", Spi, 15, 45, 3, "The last coal of the Spire, held in an iron fist that learned not to let go.",
                new ItemMods { Mp = 10, ResFire = 40, Ac = 1 }, new[] { "flame-strike" }),

            // ---------------------------------------------------------- The Annex
            U("auditors-spectacles", "Auditor's Spectacles", "circlet", Ann, 1, 50, 2, "Wire and two lenses. They show you what everything cost, and who paid.",
                new ItemMods { Int = 2, Wis = 1 }, new[] { "identify", "detect-traps" }),
            U("ledger-of-debts", "Ledger of Debts", "bone ring", Ann, 2, 45, 0, "A ring stamped with a number that increases whenever you are not looking.",
                new ItemMods { SpellFocus = 15, Mp = 6 }, new[] { "mark-for-death", "curse-of-weakness" }),
            U("dusk-daggers", "Dusk Daggers", "stiletto", Ann, 2, 45, 3, "A pair that were never found together, and have been waiting to be.",
                new ItemMods { Dex = 1, ExtraType = DamageType.Necrotic, ExtraSides = 4, Stealth = 1 }, new[] { "throwing-knives", "knife-flurry" }, "midnight-cabal"),
        };
    }
}
