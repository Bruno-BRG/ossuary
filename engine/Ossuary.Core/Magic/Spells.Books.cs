namespace Ossuary.Core.Magic
{
    /// <summary>A spellbook: what it is called, what it costs, how deep it is found (Tier 1..5), and the spells it teaches.</summary>
    public sealed class BookDef
    {
        public string Name;
        public int Cost, Tier;
        public string[] Spells;
    }

    public static partial class Spells
    {
        static BookDef Bk(string name, int tier, params string[] spells) => new BookDef { Name = name, Tier = tier, Cost = tier * tier * 40 + 60, Spells = spells };

        /// <summary>Every spellbook in the game. A book may be read away from enemies to learn what is in it; the ones the old game had keep their names.</summary>
        public static readonly BookDef[] BookList =
        {
            // ---- first steps (the starting kits)
            Bk("a book of prayers", 1, "cure-wounds", "ward", "bless", "cleanse", "minor-mending", "sacred-flame", "shield-of-faith", "holy-light", "command"),
            Bk("a book of shadows", 2, "sleep", "blink", "drain-life", "raise-skeleton", "fear", "chill-touch"),
            Bk("a spellbook", 1, "magic-missile", "shocking-grasp", "ward", "frost-ray", "familiar", "spark"),

            // ---- Evocation
            Bk("a primer of embers", 1, "ember-dart", "burning-hands", "frostbite", "spark", "acid-splash", "stone-shard", "thunderclap"),
            Bk("a tome of evocation", 3, "magic-missile", "frost-ray", "ice-lance", "fireball", "steam-burst", "lightning-bolt", "wall-of-fire", "scorching-ray", "gust-of-wind", "forked-lightning", "poison-cloud", "lava-bolt"),
            Bk("a codex of storms", 4, "lightning-bolt", "fireball", "wall-of-fire", "chain-lightning", "meteor", "call-lightning", "ice-storm", "cone-of-cold"),
            Bk("the annals of ruin", 5, "prismatic-spray", "disintegrate", "sunburst", "earthquake", "cone-of-cold", "call-lightning", "meteor"),

            // ---- Conjuration
            Bk("a tome of conjuration", 2, "familiar", "summon-beast", "create-water", "create-oil", "blink", "teleport", "spectral-blade", "phase-step", "web", "fog-cloud"),
            Bk("a bestiary of the unseen", 3, "summon-swarm", "mirror-image", "swap-places", "frozen-ground", "banish"),
            Bk("the book of four winds", 4, "summon-fire-elemental", "summon-water-elemental", "summon-earth-elemental", "summon-air-elemental", "dimension-door", "banish"),

            // ---- Alteration
            Bk("a book of wards", 2, "ward", "haste", "slow", "clairvoyance", "stone-skin", "mage-armor", "arcane-shield", "blur", "arcane-focus"),
            Bk("a hedge-wizard's notes", 1, "light", "detect-traps", "knock", "identify", "detect-monsters", "levitate", "dig"),
            Bk("a manual of the body", 3, "bulls-strength", "cats-grace", "foxs-cunning", "owls-wisdom", "enlarge", "endure-elements"),
            Bk("a treatise on resistance", 2, "fire-ward", "frost-ward", "storm-ward", "endure-elements", "displacement"),

            // ---- Illusion
            Bk("a book of illusions", 2, "sleep", "confuse", "invisibility", "charm", "dazzle", "color-spray", "befuddle", "suggestion"),
            Bk("a folio of glamours", 4, "displacement", "hypnotic-pattern", "terrify", "phantasmal-killer", "nightmare", "mass-charm"),

            // ---- Necromancy
            Bk("a charnel primer", 1, "chill-touch", "bone-shard", "enfeeble", "wither", "life-tap", "grave-chill", "bone-hound", "vampiric-touch"),
            Bk("a grimoire of the dead", 4, "raise-skeleton", "ossify", "reshape-flesh", "drain-life", "marrow-bolt", "fear", "finger-of-death", "army-of-bones"),
            Bk("the rotted codex", 3, "plague-bolt", "rotting-burst", "hemorrhage", "bone-spear", "dread", "curse-of-weakness", "gravebind", "skull-barrage"),
            Bk("a book of grave-bargains", 3, "unholy-vigor", "dark-pact", "command-undead", "animate-ghoul", "bloodlust", "feign-death", "death-aura"),
            Bk("the black litany", 4, "contagion", "cloudkill", "siphon-soul", "death-wave", "banshee-wail", "raise-wight", "bone-golem"),
            Bk("the last rite", 5, "soul-reap", "power-word-kill", "summon-wraith", "ritual-of-blood", "finger-of-death", "army-of-bones"),

            // ---- Sacred
            Bk("a book of mercy", 4, "smite", "turn-undead", "greater-heal", "purify", "revive", "restoration", "mass-cure"),
            Bk("a psalter of light", 2, "searing-light", "divine-favor", "protection-from-evil", "heroism", "remedy", "second-wind", "blinding-light", "spiritual-weapon"),
            Bk("a breviary of the faithful", 3, "regeneration", "sanctuary", "prayer", "fortitude", "remove-curse", "hammer-of-wrath", "holy-lance", "consecrate", "hold-person"),
            Bk("a missal of wrath", 4, "holy-aura", "flame-strike", "radiant-nova", "exorcise", "sunbeam", "divine-might", "spirit-guardians"),
            Bk("the book of last things", 5, "judgement", "guardian-angel", "angelic-blessing", "divine-intervention", "restoration"),

            // ---- Nature
            Bk("a druid's handbook", 1, "thorn-dart", "insect-swarm", "barkskin", "hunters-mark", "herbal-poultice", "antidote", "shillelagh", "wild-growth"),
            Bk("a ranger's almanac", 2, "entangle", "spike-growth", "thorns", "rejuvenate", "goodberries", "eagle-eye", "camouflage", "cheetah-sprint", "flame-blade", "wild-leap"),
            Bk("the green psalter", 3, "speak-with-animals", "calm-beasts", "summon-hawk", "spirit-wolves", "spirit-boar", "giant-spider"),
            Bk("a book of weather", 3, "hoarfrost", "dust-devil", "lightning-lash", "fire-seeds", "poison-spray", "moonfire", "gale", "sleet-storm"),
            Bk("the verdant grimoire", 4, "venom-bolt", "spore-cloud", "choking-vines", "tremor", "commune", "stone-spikes", "rockslide", "briar-wall", "healing-rain", "tornado"),
            Bk("a codex of beast-shapes", 4, "bears-endurance", "wolf-form", "bear-form", "eagle-form", "summon-bear"),
            Bk("the wild hunt", 5, "lightning-storm", "starfall", "treant", "summon-bear", "tornado"),

            // ---- Shadow
            Bk("a cutpurse's primer", 1, "shadow-bolt", "throwing-knives", "poison-dart", "blinding-powder", "venom-blade", "hex", "skeleton-key"),
            Bk("a book of whispers", 2, "cloak-of-shadows", "shadow-step", "smoke-bomb", "mark-for-death", "caltrops", "cripple", "mind-spike", "venom-spit", "light-fingers", "disarm-traps", "shade-strike", "gloom"),
            Bk("a manual of the knife", 3, "shadow-blade", "vampiric-edge", "shadow-clone", "umbral-grasp", "veil-of-darkness", "knife-flurry", "fade", "dread-gaze", "umbral-shroud", "night-whip"),
            Bk("the assassin's testament", 4, "assassinate", "black-tentacles", "void-rift", "vanish", "coup-de-grace", "soul-dagger", "quicken-shadow"),
            Bk("the book of the long night", 5, "eclipse", "shadow-walk", "dominate", "vanish", "assassinate"),

            // ---- the second round
            Bk("a folio of flames", 3, "flame-arrow", "thunderstrike", "searing-orb", "ice-comet", "arc-flash"),
            Bk("the pyrelord's treatise", 5, "magma-wave", "static-field", "arcane-barrage", "rimeblast", "sun-lance"),
            Bk("a menagerie of the lesser planes", 3, "summon-imp", "arcane-hound", "storm-sprite", "pull-through"),
            Bk("the stone and the gate", 5, "stone-sentinel", "gargoyle-guard", "summon-earth-elemental", "stone-skin"),
            Bk("a ward-smith's ledger", 3, "lightfoot", "keen-edge", "aegis", "arcane-survey"),
            Bk("the giant's manual", 5, "iron-body", "titans-might", "aegis", "bulls-strength", "enlarge"),
            Bk("a mummer's folio", 3, "phantom-guard", "lullaby", "bewilder", "mind-fog"),
            Bk("the dreamless book", 5, "deep-slumber", "spectral-horror", "mind-fog", "sleep", "hypnotic-pattern"),
            Bk("the bonewright's notes", 3, "death-grip", "soul-bolt", "grave-hands", "miasma", "blight"),
            Bk("the lich's catechism", 5, "bone-cage", "lich-form", "wail-of-the-damned", "blight"),
            Bk("a lay-brother's psalms", 3, "aura-of-courage", "holy-weapon", "mercy-touch", "divine-shield", "cleansing-nova"),
            Bk("the canticle of dawn", 5, "sacred-ground", "benediction", "wrath-of-heaven", "mercy-touch"),
            Bk("a forester's lore", 3, "wasp-swarm", "quicksand", "hail-of-thorns", "sun-bolt"),
            Bk("the tide and the stone", 4, "stoneform", "regrowth", "tidal-surge", "frostwind"),
            Bk("a footpad's tricks", 3, "garrote", "sleeping-dust", "snare", "nightshade", "shadow-leap"),
            Bk("the night-blade's creed", 4, "vampiric-veil", "chain-of-shadows", "ambush", "assassinate", "vanish"),
        };
    }
}
