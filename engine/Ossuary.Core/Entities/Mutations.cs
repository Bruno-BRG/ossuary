using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.Entities
{
    /// <summary>How a mutation tends to read: a gift, a trade-off, or a curse.</summary>
    public enum MutationKind { Boon, Mixed, Bane }

    /// <summary>
    /// A permanent change the Ossuary works on the body. Most are plain numbers (<see cref="Mods"/>, summed into the
    /// hero's gear like any item); a few reach further: sight radius, hunger and how loud the hero is.
    /// </summary>
    public sealed class MutationDef
    {
        public string Id, Name, Blurb;
        public MutationKind Kind;
        public ItemMods Mods;
        /// <summary>Extra squares of sight, extra nutrient burnt per turn, extra noise when moving.</summary>
        public int Sight, Hunger, Noise;
    }

    public static class MutationTable
    {
        public static readonly MutationDef[] All =
        {
            M("bone-plating", "Bone Plating", "Plates of bone grow under your skin. AC +2.", MutationKind.Boon, new ItemMods { Ac = 2 }),
            M("many-eyes", "Many Eyes", "A second and third pair of eyes open. Sight +2.", MutationKind.Boon, default, sight: 2),
            M("marrow-heart", "Marrow Heart", "Your heart beats thick and slow. HP +8.", MutationKind.Boon, new ItemMods { Hp = 8 }),
            M("grave-whisper", "Grave Whisper", "The dead murmur to you. Mp +6, necrotic 20%.", MutationKind.Boon, new ItemMods { Mp = 6, ResNecrotic = 20 }),
            M("ashen-skin", "Ashen Skin", "Your skin turns grey and dry. Fire 25%.", MutationKind.Boon, new ItemMods { ResFire = 25 }),
            M("hollow-step", "Hollow Step", "Your footfalls are light as dust. Evasion +2.", MutationKind.Boon, new ItemMods { Evasion = 2 }),
            M("knuckle-spurs", "Knuckle Spurs", "Spurs push out of your knuckles. To hit +1, damage +1.", MutationKind.Boon, new ItemMods { ToHit = 1, Dmg = 1 }),
            M("iron-gut", "Iron Gut", "Nothing you swallow can poison you for long. Poison 30%.", MutationKind.Boon, new ItemMods { ResPoison = 30 }),
            M("third-rib", "Third Rib", "A spare rib shields you but cramps you. AC +3, Dex -1.", MutationKind.Mixed, new ItemMods { Ac = 3, Dex = -1 }),
            M("ember-marrow", "Ember Marrow", "Your bones burn. Damage +2, and you are always hungry.", MutationKind.Mixed, new ItemMods { Dmg = 2 }, hunger: 1),
            M("brittle-bones", "Brittle Bones", "Your bones are chalk. AC -2.", MutationKind.Bane, new ItemMods { Ac = -2 }),
            M("ravenous", "Ravenous", "Something in you is always starving. You burn food twice as fast.", MutationKind.Bane, default, hunger: 1),
            M("palsied-hands", "Palsied Hands", "Your hands will not keep still. To hit -2.", MutationKind.Bane, new ItemMods { ToHit = -2 }),
            M("echoing-steps", "Echoing Steps", "Your steps ring in the dark. Monsters notice you from one square further.", MutationKind.Bane, default, noise: 1),
            M("pallid-skin", "Pallid Skin", "Cold bites deep. Cold -30%.", MutationKind.Bane, new ItemMods { ResCold = -30 }),
            M("thin-blood", "Thin Blood", "Your blood runs thin. HP -6.", MutationKind.Bane, new ItemMods { Hp = -6 }),
        };

        static MutationDef M(string id, string name, string blurb, MutationKind kind, ItemMods mods, int sight = 0, int hunger = 0, int noise = 0) =>
            new MutationDef { Id = id, Name = name, Blurb = blurb, Kind = kind, Mods = mods, Sight = sight, Hunger = hunger, Noise = noise };

        public static MutationDef Find(string id)
        {
            foreach (var m in All) if (m.Id == id) return m;
            return null;
        }

        /// <summary>A mutation that is always a gift, or null when the hero has them all.</summary>
        public static MutationDef PickBoon(Rng rng, ICollection<string> owned)
        {
            var pool = new List<MutationDef>();
            foreach (var m in All) if (m.Kind == MutationKind.Boon && !owned.Contains(m.Id)) pool.Add(m);
            return pool.Count == 0 ? null : pool[rng.Range(0, pool.Count)];
        }

        /// <summary>
        /// Picks a mutation the hero does not have: half boons, a fifth trade-offs, the rest banes. Falls back to
        /// whatever kind is left. Returns null when the hero has them all.
        /// </summary>
        public static MutationDef Pick(Rng rng, ICollection<string> owned, int luck = 0)
        {
            int roll = rng.Range(0, 100) - luck;
            MutationKind want = roll < 50 ? MutationKind.Boon : roll < 70 ? MutationKind.Mixed : MutationKind.Bane;
            var pool = new List<MutationDef>();
            foreach (var m in All) if (m.Kind == want && !owned.Contains(m.Id)) pool.Add(m);
            if (pool.Count == 0) foreach (var m in All) if (!owned.Contains(m.Id)) pool.Add(m);
            return pool.Count == 0 ? null : pool[rng.Range(0, pool.Count)];
        }
    }
}
