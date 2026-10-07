using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Monsters that hate each other. The dungeon's own factions (greenskins and deepfolk, the dead and the wild) fight on
    /// sight, which gives a clever player a third party to hide behind. Pure bookkeeping on names and flags; the fights use
    /// the same melee as everything else.
    /// </summary>
    public sealed partial class Game
    {
        public enum Faction { None, Greenskin, Deepfolk, Dead, Wild }

        static readonly HashSet<string> GreenskinNames = new HashSet<string> { "kobold", "orc", "orc shaman", "orc chieftain" };
        static readonly HashSet<string> DeepfolkNames = new HashSet<string> { "dwarf", "dwarf lord", "gnome", "gnome lord", "hobbit" };
        static readonly HashSet<string> WildNames = new HashSet<string>
            { "jackal", "giant rat", "newt", "centipede", "cave spider", "cave bat", "plague rat", "rat swarm", "gaol hound", "little lizard" };

        public static Faction FactionOf(Monster m)
        {
            if (m == null || m.Ally || m.Townsperson || m.Companion || m.BossId != null) return Faction.None;
            string n = m.Def.Name;
            if (m.Def.Undead || m.Def.Skeleton) return Faction.Dead;
            if (GreenskinNames.Contains(n)) return Faction.Greenskin;
            if (DeepfolkNames.Contains(n)) return Faction.Deepfolk;
            if (WildNames.Contains(n)) return Faction.Wild;
            return Faction.None;
        }

        public static bool AreRivals(Faction a, Faction b) =>
            (a == Faction.Greenskin && b == Faction.Deepfolk) || (a == Faction.Deepfolk && b == Faction.Greenskin)
            || (a == Faction.Dead && b == Faction.Wild) || (a == Faction.Wild && b == Faction.Dead);

        /// <summary>The nearest monster this one hates within <paramref name="radius"/>, or null.</summary>
        Monster NearestRival(Monster m, int radius)
        {
            var mine = FactionOf(m);
            if (mine == Faction.None) return null;
            Monster best = null; int bestD = int.MaxValue;
            foreach (var o in Monsters)
            {
                if (ReferenceEquals(o, m) || o.IsDead || !AreRivals(mine, FactionOf(o))) continue;
                int d = Pathfinder.Chebyshev(m.X, m.Y, o.X, o.Y);
                if (d <= radius && d < bestD) { best = o; bestD = d; }
            }
            return best;
        }

        /// <summary>
        /// Called on a monster's turn while the player is not next to it. Fights an adjacent rival, or closes on one that is
        /// near and nearer than the player. Returns true when the turn was spent.
        /// </summary>
        bool FightRival(Monster m, int distToPlayer)
        {
            var rival = NearestRival(m, 6);
            if (rival == null) return false;
            int d = Pathfinder.Chebyshev(m.X, m.Y, rival.X, rival.Y);
            if (d > 1)
            {
                if (distToPlayer <= d + 1) return false;      // the player is the nearer target
                StepToward(m, rival.X, rival.Y);
                return true;
            }
            var res = Battles.MeleeAttack(m, rival, Rng);
            if (Map.IsVisible(m.X, m.Y) || Map.IsVisible(rival.X, rival.Y))
                Say(res.Hit ? $"The {m.Name} fights the {rival.Name}: {res.Damage} damage." : $"The {m.Name} swings at the {rival.Name} and misses.", MessageKind.Info);
            if (Bodies.Wounding(res.Kind)) WoundFrom(rival, res, Bodies.EdgedAttack(res.Kind), false);
            if (res.Killed)
            {
                rival.HP = 0;
                foreach (var it in rival.Inventory) GroundItems.Add(Map.Number, rival.X, rival.Y, it);
                Monsters.Remove(rival);
                Map.Version++;
                if (Map.IsVisible(rival.X, rival.Y)) Say($"The {rival.Name} falls to the {m.Name}.", MessageKind.Info);
            }
            return true;
        }
    }
}
