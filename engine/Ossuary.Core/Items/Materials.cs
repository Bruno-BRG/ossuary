using System;
using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    /// <summary>What a piece of gear is made of, as numbers. Iron is the baseline every catalogue entry was balanced on.</summary>
    public sealed class MaterialDef
    {
        public string Id, Name;
        /// <summary>Percent of the catalogue weight and value.</summary>
        public int Weight = 100, Value = 100;
        /// <summary>Added to a weapon's to-hit and damage, and to a worn piece's AC.</summary>
        public int ToHit, Dmg, Ac;
        /// <summary>Blows (dealt by a weapon, taken by armour) before each step of wear: blunted, then chipped.</summary>
        public int Durability = 300;
        /// <summary>Snaps instead of bending: a worn brittle weapon can shatter on a critical.</summary>
        public bool Brittle;
        /// <summary>What it is deadly to: "undead" (also werebeasts), "fey" or "construct".</summary>
        public string Bane;
        /// <summary>A metal the smith can pour from ore (see <see cref="Materials.Ores"/>).</summary>
        public bool Metal;
    }

    /// <summary>How a catalogue item can be made: which materials it accepts and which one it is when nothing else is said.</summary>
    public enum Stuff { None, Edge, Blunt, Haft, Mail, Shield }

    /// <summary>
    /// The material table and the rules that tie it to items. An item stores only a material id; a null id is the
    /// default of its kind (iron for blades and mail, wood for staves and bows), so the starting kit and the old
    /// catalogue read exactly as before. The default is never written in the name.
    /// </summary>
    public static class Materials
    {
        public static readonly MaterialDef[] All =
        {
            new MaterialDef { Id = "copper",     Name = "copper",     Weight = 105, Value = 50,   Dmg = -1, Ac = -1, Durability = 120, Metal = true },
            new MaterialDef { Id = "bronze",     Name = "bronze",     Weight = 105, Value = 75,   Durability = 200, Metal = true },
            new MaterialDef { Id = "iron",       Name = "iron",       Durability = 300, Metal = true },
            new MaterialDef { Id = "steel",      Name = "steel",      Value = 220, ToHit = 1, Dmg = 1, Ac = 1, Durability = 500, Metal = true },
            new MaterialDef { Id = "silver",     Name = "silver",     Weight = 110, Value = 400, Ac = -1, Durability = 180, Bane = "undead", Metal = true },
            new MaterialDef { Id = "cold-iron",  Name = "cold iron",  Weight = 105, Value = 300, Durability = 320, Bane = "fey", Metal = true },
            new MaterialDef { Id = "mithril",    Name = "mithril",    Weight = 50,  Value = 1000, ToHit = 1, Dmg = 1, Ac = 2, Durability = 900, Metal = true },
            new MaterialDef { Id = "adamantine", Name = "adamantine", Weight = 120, Value = 1500, ToHit = 1, Dmg = 2, Ac = 3, Durability = 2000, Metal = true },
            new MaterialDef { Id = "obsidian",   Name = "obsidian",   Weight = 80,  Value = 150,  ToHit = 1, Dmg = 2, Durability = 80, Brittle = true, Bane = "construct" },
            new MaterialDef { Id = "bone",       Name = "bone",       Weight = 70,  Value = 45,   Dmg = -1, Ac = -1, Durability = 100, Brittle = true },
            new MaterialDef { Id = "wood",       Name = "wood",       Weight = 60,  Value = 40,   Dmg = -1, Ac = -1, Durability = 160 },
        };

        public static MaterialDef Find(string id)
        {
            if (id == null) return null;
            foreach (var m in All) if (m.Id == id) return m;
            return null;
        }

        /// <summary>The family a catalogue item belongs to, read from its kind, class and name.</summary>
        public static Stuff StuffOf(ItemDef d)
        {
            string n = d.Name ?? "";
            if (n.Contains("elven") || n.Contains("...") || n.Contains("bone")) return Stuff.None;
            switch (d.Kind)
            {
                case ItemKind.Weapon:
                    switch (d.Class)
                    {
                        case ItemClass.Blade: case ItemClass.Axe: return Stuff.Edge;
                        case ItemClass.Club: return Stuff.Blunt;
                        case ItemClass.Polearm: return n.Contains("staff") ? Stuff.Haft : Stuff.Edge;
                        case ItemClass.Bow: return n.Contains("crossbow") ? Stuff.None : Stuff.Haft;
                        default: return Stuff.None;
                    }
                case ItemKind.Armor: return n.Contains("mail") ? Stuff.Mail : Stuff.None;
                case ItemKind.Helm: return n.Contains("leather") ? Stuff.None : Stuff.Mail;
                case ItemKind.Gloves: return n.Contains("gauntlet") ? Stuff.Mail : Stuff.None;
                case ItemKind.Shield: return Stuff.Shield;
                case ItemKind.Ammo: return n.Contains("stone") ? Stuff.None : Stuff.Edge;
                default: return Stuff.None;
            }
        }

        /// <summary>The material an item of this family is when nothing else is said.</summary>
        public static MaterialDef DefaultFor(Stuff s) => s == Stuff.None ? null : s == Stuff.Haft ? Find("wood") : Find("iron");

        /// <summary>Whether this family can be made of this material.</summary>
        public static bool Takes(Stuff s, MaterialDef m)
        {
            if (m == null || s == Stuff.None) return false;
            switch (s)
            {
                case Stuff.Edge: return m.Metal || m.Id == "bone" || m.Id == "obsidian";
                case Stuff.Blunt: return m.Metal || m.Id == "bone" || m.Id == "wood";
                case Stuff.Haft: return m.Id == "wood" || m.Id == "bone";
                case Stuff.Mail: return m.Metal || m.Id == "bone";
                case Stuff.Shield: return m.Metal || m.Id == "wood" || m.Id == "bone";
                default: return false;
            }
        }

        public static bool Takes(ItemDef d, MaterialDef m) => Takes(StuffOf(d), m);

        /// <summary>How common each material is at a depth, before the family filter; a branch leans the odds toward what it holds.</summary>
        static int Weight(MaterialDef m, int depth, string branch)
        {
            int w = BaseWeight(m, depth);
            switch (branch)
            {
                // Dwarves worked good metal; the Warrens make do with bone, wood and copper; the drowned kings hoarded silver;
                // the Spire is glass and black stone.
                case "The Mines of Dwarfdeep": if (m.Id == "steel" || m.Id == "mithril" || m.Id == "adamantine" || m.Id == "silver") w *= 2; else if (m.Id == "bone") w /= 2; break;
                case "The Warrens": if (m.Id == "bone") w *= 3; else if (m.Id == "wood" || m.Id == "copper") w *= 2; break;
                case "The Sunken Vaults": if (m.Id == "silver") w *= 3; else if (m.Id == "bronze") w *= 2; break;
                case "The Ashen Spire": if (m.Id == "obsidian") w *= 4; else if (m.Id == "cold-iron") w *= 2; break;
            }
            return w;
        }

        static int BaseWeight(MaterialDef m, int depth)
        {
            switch (m.Id)
            {
                case "copper": return depth <= 3 ? 25 : depth <= 6 ? 10 : 3;
                case "bronze": return depth <= 6 ? 25 : 8;
                case "iron": return 60;
                case "steel": return depth >= 3 ? 8 + depth * 2 : 2;
                case "silver": return depth >= 4 ? 6 : 0;
                case "cold-iron": return depth >= 5 ? 5 : 0;
                case "mithril": return depth >= 10 ? depth - 8 : 0;
                case "adamantine": return depth >= 14 ? depth - 12 : 0;
                case "obsidian": return depth >= 6 ? 4 : 0;
                case "bone": return 6;
                case "wood": return 30;
                default: return 0;
            }
        }

        /// <summary>
        /// Gives a fresh piece of gear its material, by depth. The pick is a hash of the world seed, the item's uid and
        /// its name, so it draws nothing from the simulation RNG: every map, monster and roll stays where it was.
        /// </summary>
        public static void Assign(Item item, ulong seed, int depth, string branch = null)
        {
            if (item == null || item.Rarity == Rarity.Artifact || item.Material != null) return;
            var stuff = StuffOf(item.Def);
            if (stuff == Stuff.None) return;
            var pool = new List<MaterialDef>(); var weights = new List<int>(); int total = 0;
            foreach (var m in All)
            {
                if (!Takes(stuff, m)) continue;
                int w = Weight(m, depth, branch);
                if (w <= 0) continue;
                pool.Add(m); weights.Add(w); total += w;
            }
            if (total <= 0) return;
            ulong h = Mix(seed ^ (ulong)item.Uid * 0x9E3779B97F4A7C15UL ^ Fnv(item.Def.Name) ^ (ulong)depth * 0xBF58476D1CE4E5B9UL);
            int roll = (int)(h % (ulong)total);
            for (int i = 0; i < pool.Count; i++)
            {
                if (roll < weights[i]) { Set(item, pool[i]); return; }
                roll -= weights[i];
            }
        }

        /// <summary>Makes the item of this material (the default is stored as null). Wear starts over.</summary>
        public static void Set(Item item, MaterialDef m)
        {
            var def = DefaultFor(StuffOf(item.Def));
            item.Material = m == null || (def != null && def.Id == m.Id) ? null : m.Id;
            item.Wear = 0;
            item.Value = item.TradeValue;
        }

        static ulong Fnv(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s ?? "") { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        // ---------------------------------------------------------------- material against creature

        static readonly string[] Fey = { "nymph", "sprite", "pixie", "dryad", "fairy", "faerie", "sylph", "satyr", "fey", "treant" };
        static readonly string[] Constructs = { "golem", "gargoyle", "sentinel", "automaton", "construct", "statue", "animated" };

        /// <summary>True when this material is a bane to that creature: silver to the dead and to werebeasts, cold iron to the fey, obsidian to things made.</summary>
        public static bool IsBane(MaterialDef m, Entities.Monster target)
        {
            if (m == null || m.Bane == null || target == null) return false;
            string n = target.Def.Name ?? "";
            switch (m.Bane)
            {
                case "undead": return target.Def.Undead || n.StartsWith("were");
                case "fey": foreach (var f in Fey) if (n.Contains(f)) return true; return false;
                case "construct": foreach (var c in Constructs) if (n.Contains(c)) return true; return false;
                default: return false;
            }
        }

        // ---------------------------------------------------------------- ore

        /// <summary>Raw metal the Mines give up and the smith pours into gear: one def per metal, named "metal ore".</summary>
        public static readonly ItemDef[] Ores =
        {
            Ore("copper ore", 8), Ore("iron ore", 12), Ore("silver ore", 60), Ore("mithril ore", 220), Ore("adamantine ore", 320),
        };

        static ItemDef Ore(string name, int cost) =>
            new ItemDef { Name = name, Glyph = '*', Kind = ItemKind.Rock, Cost = cost, Weight = 10, Tier = 1, Flags = ItemFlags.Uncursed };

        public static bool IsOre(Item it) => it != null && it.Def.Kind == ItemKind.Rock && (it.Def.Name ?? "").EndsWith(" ore");

        /// <summary>The ore a metal is poured from: bronze from copper, steel and cold iron from iron.</summary>
        public static string OreFor(MaterialDef m)
        {
            if (m == null || !m.Metal) return null;
            switch (m.Id)
            {
                case "copper": case "bronze": return "copper ore";
                case "iron": case "steel": case "cold-iron": return "iron ore";
                default: return m.Name + " ore";
            }
        }

        /// <summary>Which ore a vein holds at a depth of the Mines (deeper veins are richer); roll is 0..99.</summary>
        public static ItemDef OreAt(int depth, int roll)
        {
            if (depth >= 7 && roll < 6) return Ores[4];
            if (depth >= 5 && roll < 18) return Ores[3];
            if (depth >= 3 && roll < 35) return Ores[2];
            return roll % 2 == 0 ? Ores[0] : Ores[1];
        }
    }
}
