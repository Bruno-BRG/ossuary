using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    /// <summary>A wand, scroll or potion that is a spell in a container: which spell it casts, at what caster level at least, what it costs and how deep it is found.</summary>
    public sealed class MagicItemDef
    {
        public string Name, Spell;
        public int Power, Cost, Tier;
    }

    /// <summary>
    /// Wands, scrolls and potions backed by the spell engine, so they get every effect and animation a spell has.
    /// They never fizzle and cost no mana; a wand has charges, a scroll and a potion are used up.
    /// </summary>
    public static class ItemSpells
    {
        static MagicItemDef I(string name, string spell, int power, int cost, int tier) => new MagicItemDef { Name = name, Spell = spell, Power = power, Cost = cost, Tier = tier };

        // The older wands keep their names (and their place in the catalogue); these are theirs plus the new ones.
        public static readonly MagicItemDef[] Wands =
        {
            I("wand of light", "light", 5, 100, 2), I("wand of striking", "stone-shard", 6, 200, 2), I("wand of digging", "dig", 6, 200, 2),
            I("wand of cold", "frost-ray", 7, 200, 2), I("wand of fire", "scorching-ray", 7, 200, 2), I("wand of lightning", "lightning-bolt", 7, 200, 2),
            I("wand of teleportation", "teleport", 8, 200, 2),
            I("wand of sparks", "spark", 4, 100, 1), I("wand of magic missiles", "magic-missile", 6, 150, 1), I("wand of acid", "acid-splash", 5, 150, 1),
            I("wand of sleep", "sleep", 6, 200, 1), I("wand of confusion", "confuse", 6, 200, 1), I("wand of webs", "web", 6, 200, 1),
            I("wand of entangling", "entangle", 6, 200, 1), I("wand of smiting", "smite", 6, 200, 1), I("wand of searing light", "searing-light", 6, 200, 1),
            I("wand of shadows", "shadow-bolt", 5, 150, 1),
            I("wand of slowness", "slow", 6, 220, 2), I("wand of fear", "fear", 7, 250, 2), I("wand of draining", "drain-life", 7, 300, 2),
            I("wand of venom", "venom-bolt", 8, 300, 2), I("wand of holding", "hold-person", 7, 300, 2), I("wand of bones", "bone-spear", 7, 300, 2),
            I("wand of gales", "gale", 7, 250, 2), I("wand of thunder", "thunderclap", 6, 250, 2), I("wand of rot", "rotting-burst", 7, 300, 2),
            I("wand of mending", "greater-heal", 8, 300, 2), I("wand of blinking", "blink", 8, 300, 2),
            I("wand of fireballs", "fireball", 8, 400, 3), I("wand of frost", "cone-of-cold", 8, 400, 3), I("wand of charming", "charm", 8, 350, 3),
            I("wand of lava", "lava-bolt", 8, 350, 3), I("wand of fire seeds", "fire-seeds", 7, 300, 3), I("wand of banishment", "banish", 9, 400, 3),
            I("wand of the blizzard", "ice-storm", 9, 450, 4), I("wand of the storm", "chain-lightning", 9, 450, 4),
            I("wand of ruin", "disintegrate", 12, 800, 5), I("wand of meteors", "meteor", 12, 800, 5),
        };

        public static readonly MagicItemDef[] Scrolls =
        {
            I("scroll of light", "light", 5, 40, 1), I("scroll of knocking", "knock", 5, 50, 1), I("scroll of trapfinding", "detect-traps", 6, 60, 1),
            I("scroll of sensing", "detect-monsters", 6, 80, 1), I("scroll of protection", "mage-armor", 6, 80, 1), I("scroll of resistance", "endure-elements", 6, 100, 1),
            I("scroll of clarity", "cleanse", 6, 100, 1), I("scroll of levitation", "levitate", 6, 100, 2), I("scroll of entangling", "entangle", 6, 100, 1),
            I("scroll of remove curse", "remove-curse", 6, 150, 2), I("scroll of frost", "frost-ray", 7, 120, 2), I("scroll of lightning", "lightning-bolt", 8, 140, 2),
            I("scroll of smiting", "smite", 7, 120, 2), I("scroll of fear", "terrify", 7, 140, 2), I("scroll of gales", "gale", 8, 140, 2),
            I("scroll of fireball", "fireball", 8, 180, 3), I("scroll of slumber", "hypnotic-pattern", 8, 180, 3), I("scroll of warding", "stone-skin", 8, 180, 3),
            I("scroll of healing", "greater-heal", 8, 160, 2), I("scroll of haste", "haste", 8, 180, 3), I("scroll of invisibility", "invisibility", 8, 180, 3),
            I("scroll of blinking", "blink", 8, 150, 2), I("scroll of bone servants", "raise-skeleton", 8, 180, 3), I("scroll of beasts", "summon-beast", 8, 160, 3),
            I("scroll of sanctuary", "sanctuary", 8, 200, 3), I("scroll of darkness", "veil-of-darkness", 8, 160, 3),
            I("scroll of banishment", "banish", 9, 240, 3), I("scroll of ice", "ice-storm", 9, 240, 4), I("scroll of the tempest", "call-lightning", 9, 260, 4),
            I("scroll of mending", "restoration", 9, 240, 4), I("scroll of elementals", "summon-fire-elemental", 10, 300, 4),
            I("scroll of revival", "revive", 10, 400, 4), I("scroll of meteors", "meteor", 12, 500, 5), I("scroll of mass charm", "mass-charm", 12, 500, 5),
        };

        public static readonly MagicItemDef[] Potions =
        {
            I("potion of mending", "restoration", 8, 180, 2), I("potion of might", "bulls-strength", 6, 150, 1), I("potion of giants", "enlarge", 8, 250, 3),
            I("potion of resistance", "endure-elements", 6, 150, 1), I("potion of fire protection", "fire-ward", 6, 150, 1), I("potion of frost protection", "frost-ward", 6, 150, 1),
            I("potion of storm protection", "storm-ward", 6, 150, 1), I("potion of regeneration", "regeneration", 6, 200, 2), I("potion of stone skin", "stone-skin", 8, 220, 3),
            I("potion of shadows", "invisibility", 8, 220, 2), I("potion of clarity", "cleanse", 6, 120, 1), I("potion of vigor", "second-wind", 6, 100, 1),
            I("potion of heroism", "heroism", 6, 180, 2), I("potion of sanctuary", "sanctuary", 8, 240, 3), I("potion of grace", "cats-grace", 6, 180, 2),
            I("potion of wisdom", "owls-wisdom", 6, 180, 2), I("potion of cunning", "foxs-cunning", 6, 180, 2), I("potion of the bear", "bears-endurance", 6, 180, 2),
        };

        static Dictionary<string, MagicItemDef> _by;

        public static bool TryGet(string name, out MagicItemDef def)
        {
            if (_by == null)
            {
                var d = new Dictionary<string, MagicItemDef>();
                foreach (var list in new[] { Wands, Scrolls, Potions }) foreach (var m in list) d[m.Name] = m;
                _by = d;
            }
            def = null;
            return name != null && _by.TryGetValue(name, out def);
        }
    }
}
