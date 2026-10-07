using System;

namespace Ossuary.Core.Items
{
    /// <summary>
    /// Ammunition as data: what each launcher looses, and how hard. A bow looses arrows, a crossbow bolts, a sling stones.
    /// Arrows and bolts take a metal head (see <see cref="Materials"/>), so a silver arrow sears the dead like a silver blade.
    /// The rules that spend and recover them are in Game.Ammo.cs.
    /// </summary>
    public static class Ammo
    {
        static ItemDef D(string name, int cost, int dice, int sides) =>
            new ItemDef { Name = name, Glyph = ')', Kind = ItemKind.Ammo, Class = ItemClass.Bow, Cost = cost, Weight = 1, Damage = dice, Sides = sides, Tier = 1, Flags = ItemFlags.Uncursed };

        public static readonly ItemDef Arrow = D("arrow", 2, 1, 6);
        public static readonly ItemDef Bolt = D("crossbow bolt", 3, 1, 8);
        public static readonly ItemDef Stone = D("sling stone", 1, 1, 4);

        public static readonly ItemDef[] All = { Arrow, Bolt, Stone };

        /// <summary>Whether the thing looses ammunition: bows, crossbows and slings.</summary>
        public static bool IsLauncher(Item it) => it != null && it.Def.Kind == ItemKind.Weapon && AmmoFor(it.Def) != null;

        /// <summary>The ammunition a launcher takes, by its name, or null for anything else.</summary>
        public static string AmmoFor(ItemDef launcher)
        {
            string n = launcher.Name ?? "";
            if (n.Contains("crossbow")) return Bolt.Name;
            if (n.Contains("sling")) return Stone.Name;
            if (launcher.Class == ItemClass.Bow && n.Contains("bow")) return Arrow.Name;
            return null;
        }

        /// <summary>The draw of the launcher itself: a heavier pull adds to every shot.</summary>
        public static int Pull(ItemDef launcher)
        {
            string n = launcher.Name ?? "";
            if (n.Contains("crossbow")) return 3;
            if (n.Contains("elven")) return 3;
            if (n.Contains("sling")) return 1;
            return 2;
        }

        /// <summary>Chance in a hundred that a loosed piece is lost: arrows snap, bolts less so, stones never.</summary>
        public static int BreakChance(ItemDef ammo, bool hit)
        {
            if (ammo.Name == Stone.Name) return hit ? 10 : 0;
            int c = ammo.Name == Bolt.Name ? 20 : 30;
            return hit ? c : c / 3;
        }

        public static ItemDef Find(string name) { foreach (var d in All) if (d.Name == name) return d; return default; }
    }
}

