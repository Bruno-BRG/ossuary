using System;

namespace Ossuary.Core.Entities
{
    /// <summary>
    /// A god of the Ossuary: dead, dying or merely tired. Likes and dislikes are what the faithful earn
    /// or lose piety for (rules in <see cref="Gods"/>); the boon is what a prayer at one of their altars
    /// grants; the tiers are standing blessings that come with piety 50 and 100.
    /// </summary>
    public sealed class GodDef
    {
        public string Id, Name, Title, Domain;
        public string Likes, Dislikes, Boon, Tier1, Tier2;
        public int BoonCost;
        public Alignment Align;
    }

    public static class Gods
    {
        public const int MaxPiety = 200;
        public const int Tier1At = 50, Tier2At = 100;
        public const int PrayerCooldown = 400;

        public static readonly GodDef[] All = {
            new GodDef {
                Id = "aurel", Name = "Aurel", Title = "the Last Lamp", Domain = "light and mercy", Align = Alignment.LawfulGood,
                Likes = "killing the undead; sacred magic", Dislikes = "necromancy; killing the harmless",
                Boon = "full healing, cleansing and a full measure of mana", BoonCost = 30,
                Tier1 = "wounds close faster", Tier2 = "necrotic resistance 30%",
            },
            new GodDef {
                Id = "khorr", Name = "Khorr", Title = "the Hammer Beneath", Domain = "war and stone", Align = Alignment.LawfulNeutral,
                Likes = "kills, above all of foes stronger than you", Dislikes = "illusions, vanishing and trickery",
                Boon = "a +1 enchantment on your weapon", BoonCost = 60,
                Tier1 = "+1 to hit in melee", Tier2 = "+2 melee damage",
            },
            new GodDef {
                Id = "veyra", Name = "Veyra", Title = "Mother of Ash", Domain = "fire and ruin", Align = Alignment.ChaoticNeutral,
                Likes = "killing with fire", Dislikes = "killing with cold",
                Boon = "a flame that rides your blows for 300 turns", BoonCost = 40,
                Tier1 = "fire resistance 30%", Tier2 = "fire resistance 60%; your blows burn",
            },
            new GodDef {
                Id = "nhal", Name = "Nhal", Title = "the Drowned King", Domain = "death and still water", Align = Alignment.NeutralEvil,
                Likes = "killing with death magic; raising the dead", Dislikes = "sacred magic",
                Boon = "two skeletons to serve you for 200 turns", BoonCost = 40,
                Tier1 = "necrotic resistance 30%", Tier2 = "every kill restores 2 HP",
            },
            new GodDef {
                Id = "sylk", Name = "Sylk", Title = "the Quiet One", Domain = "shadow and theft", Align = Alignment.ChaoticNeutral,
                Likes = "killing the sleeping and unaware; illusions", Dislikes = "roaring war cries",
                Boon = "invisibility for 100 turns and a full measure of Vigor", BoonCost = 30,
                Tier1 = "+2 evasion", Tier2 = "foes notice you from one cell less",
            },
        };

        public static GodDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>
        /// Which god an altar belongs to. Derived from where it is, not stored: the same altar is always
        /// the same god's, whatever the visit order.
        /// </summary>
        public static GodDef AtAltar(int mapNumber, int x, int y)
        {
            unchecked
            {
                uint h = (uint)(mapNumber * 73856093) ^ (uint)(x * 19349663) ^ (uint)(y * 83492791);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return All[(int)(h % (uint)All.Length)];
            }
        }
    }
}
