using System;
using System.Collections.Generic;

namespace Ossuary.Core.Magic
{
    /// <summary>
    /// The spells monsters cast (see Game.Casters.cs), as data. A pool holds recipe spells only: their damage is numbers, their shape and riders
    /// are data, so a monster can aim them at the hero. The hand-coded spells in Game.Magic.Effects.cs assume the hero is the caster.
    /// </summary>
    public static class MonsterSpells
    {
        /// <summary>Percent chance per eligible turn that a monster casts instead of moving.</summary>
        public const int MonsterChance = 40;
        /// <summary>Percent chance per eligible turn that a boss casts (bosses cast more often than the creatures they lead).</summary>
        public const int BossChance = 50;
        /// <summary>Turns a caster waits after a cast before it casts again.</summary>
        public const int Cooldown = 2;

        static readonly Dictionary<string, string[]> Creatures = new Dictionary<string, string[]>
        {
            ["orc shaman"] = new[] { "ember-dart", "frostbite", "stone-shard" },
            ["dark acolyte"] = new[] { "bone-shard", "wither", "soul-bolt", "rotting-burst" },
            ["sorcerer"] = new[] { "arc-flash", "searing-orb", "thunderstrike", "static-field" },
        };

        static readonly string[] MagisterFirst = { "searing-orb", "thunderstrike", "arc-flash" };
        static readonly string[] MagisterAngry = { "ice-comet", "static-field" };

        /// <summary>The pool of a creature, by its bestiary name; null when it casts nothing.</summary>
        public static string[] ForCreature(string name) => name != null && Creatures.TryGetValue(name, out var pool) ? pool : null;

        /// <summary>The pool of a boss, by id. In its second phase it adds the angry spells.</summary>
        public static string[] ForBoss(string bossId, bool angry)
        {
            if (bossId != "sallow-magister") return null;
            var list = new List<string>(MagisterFirst);
            if (angry) list.AddRange(MagisterAngry);
            return list.ToArray();
        }

        /// <summary>Every spell id a caster may cast (both phases of every boss included).</summary>
        public static IEnumerable<string> AllIds()
        {
            var seen = new HashSet<string>();
            foreach (var pool in Creatures.Values) foreach (string id in pool) if (seen.Add(id)) yield return id;
            foreach (string id in MagisterFirst) if (seen.Add(id)) yield return id;
            foreach (string id in MagisterAngry) if (seen.Add(id)) yield return id;
        }

        /// <summary>The riders a monster's spell can put on the hero. The others are damage only: the hero has no status for them.</summary>
        public static bool ReachesHero(Rider r) => r == Rider.None || r == Rider.Burn || r == Rider.Confuse || r == Rider.Blind || r == Rider.Stun || r == Rider.Poison;

        /// <summary>How far from the caster a spell reaches the hero: a nova by its radius, everything else by its range.</summary>
        public static int Reach(SpellDef sp) => sp.Shape == Shape.Nova ? sp.Radius : sp.Range;

        /// <summary>
        /// The dice a monster's cast rolls: half the hero's version, rounded up. The spell is the same; the caster is not the hero, and a
        /// creature that casts every other turn must not out-damage the hero's own fireball.
        /// </summary>
        public static int Dice(SpellDef sp) => (sp.Dice + 1) / 2;
    }
}
