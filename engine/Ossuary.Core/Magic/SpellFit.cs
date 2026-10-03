using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.Magic
{
    /// <summary>
    /// Which spells make sense on which kind of item. A random magic item is imbued with a spell from the pool that fits it: swords carry
    /// attacks and control, armour carries wards, boots carry movement, helms carry senses and the mind, gloves carry touch spells, cloaks
    /// carry stealth, rings and amulets carry anything you cast on yourself. The categories are read off the spell's own data.
    /// </summary>
    public static class SpellFit
    {
        static readonly HashSet<string> MoveSpecials = new HashSet<string> { "blink", "swap", "shadow-walk", "vanish" };
        static readonly HashSet<string> DetectSpecials = new HashSet<string> { "light", "detect-traps", "commune", "identify", "knock", "disarm" };
        static readonly HashSet<string> StealthBuffs = new HashSet<string> { "shadow-cloak", "fade", "blur", "displacement", "camouflage", "umbral-shroud", "invisibility" };
        static readonly HashSet<string> MoveBuffs = new HashSet<string> { "haste", "levitating", "eagle-form", "wolf-form" };
        static readonly HashSet<string> MindBuffs = new HashSet<string> { "foxs-cunning", "owls-wisdom", "arcane-focus", "sense", "dark-pact" };

        // The first spells have code of their own and no recipe, so their roles are listed.
        static readonly HashSet<string> OldAttacks = new HashSet<string> { "magic-missile", "shocking-grasp", "frost-ray", "fireball", "lightning-bolt", "chain-lightning", "meteor", "ice-lance", "steam-burst", "drain-life", "marrow-bolt", "finger-of-death", "smite" };
        static readonly HashSet<string> OldControl = new HashSet<string> { "slow", "sleep", "confuse", "charm", "fear" };
        static readonly HashSet<string> OldWards = new HashSet<string> { "ward", "stone-skin", "bless", "cure-wounds", "greater-heal", "cleanse", "revive", "ossify", "purify" };

        public static bool Damaging(SpellDef s) => s.Dice > 0 || s.Hits > 0 || s.Hops > 0 || OldAttacks.Contains(s.Id);
        public static bool Aimed(SpellDef s) => s.Target == SpellTarget.Monster || s.Target == SpellTarget.Area || s.Target == SpellTarget.Line || s.Target == SpellTarget.Cone;
        public static bool SelfCast(SpellDef s) => s.Target == SpellTarget.Self;
        public static bool Control(SpellDef s) => s.Rider != Rider.None || OldControl.Contains(s.Id);
        public static bool IsMove(SpellDef s) => s.Id == "haste" || s.Id == "blink" || (s.Special != null && MoveSpecials.Contains(s.Special)) || (s.Buff != null && MoveBuffs.Contains(s.Buff)) || s.Id == "teleport" || s.Id == "dimension-door";
        public static bool IsDetect(SpellDef s) => (s.Special != null && DetectSpecials.Contains(s.Special)) || s.Id == "clairvoyance" || s.Buff == "sense";
        public static bool IsStealth(SpellDef s) => s.Buff != null && StealthBuffs.Contains(s.Buff) || s.Id == "invisibility" || s.Id == "vanish" || s.Id == "veil-of-darkness";
        public static bool IsMind(SpellDef s) => (s.School == School.Illusion && Control(s)) || (s.Buff != null && MindBuffs.Contains(s.Buff));
        public static bool IsWard(SpellDef s) => SelfCast(s) && (s.Buff != null || OldWards.Contains(s.Id) || s.HealDice > 0 || s.Special == "restoration" || s.Special == "full-heal" || s.Special == "mass-cure" || s.Special == "remedy")
                                                   && !IsMove(s) && !IsStealth(s) && !IsMind(s) && !IsDetect(s) && !WeaponBuff(s);
        public static bool WeaponBuff(SpellDef s) => s.Buff == "flame-blade" || s.Buff == "venom-blade" || s.Buff == "shadow-blade" || s.Buff == "vampiric-edge" || s.Buff == "shillelagh" || s.Buff == "favor" || s.Buff == "unholy-vigor" || s.Buff == "bloodlust";
        public static bool Summon(SpellDef s) => s.Summons > 0;
        public static bool Barrage(SpellDef s) => s.Hits > 0;
        public static bool Nova(SpellDef s) => s.Shape == Shape.Nova;

        /// <summary>Can this spell be imbued into this kind of item?</summary>
        public static bool Fits(SpellDef s, ItemKind kind, string baseName = null)
        {
            switch (kind)
            {
                case ItemKind.Weapon:
                    return (Damaging(s) && Aimed(s)) || Barrage(s) || (Aimed(s) && Control(s) && !Damaging(s) && s.Target != SpellTarget.Area) || WeaponBuff(s);
                case ItemKind.Armor:
                case ItemKind.Shield:
                    return IsWard(s) || (Nova(s) && Damaging(s)) || Summon(s);
                case ItemKind.Helm:
                    return IsDetect(s) || IsMind(s);
                case ItemKind.Gloves:
                    return (Aimed(s) && (Damaging(s) || Control(s)) && (s.Range <= 3 || s.Target == SpellTarget.Cone)) || s.Special == "pickpocket" || s.Special == "knock" || s.Special == "disarm";
                case ItemKind.Boots:
                    return IsMove(s);
                case ItemKind.Cloak:
                    return IsStealth(s) || (IsMind(s) && SelfCast(s));
                case ItemKind.Ring:
                    return SelfCast(s) && (IsWard(s) || IsDetect(s) || IsStealth(s) || IsMove(s) || WeaponBuff(s)) ;
                case ItemKind.Amulet:
                    return SelfCast(s) && (IsWard(s) || Summon(s) || IsDetect(s) || ((Nova(s) || Barrage(s)) && Damaging(s)) || IsMind(s));
            }
            return false;
        }

        public static List<SpellDef> Pool(ItemKind kind, int maxLevel)
        {
            var l = new List<SpellDef>();
            foreach (var s in Spells.All) if (s.Level <= maxLevel && Fits(s, kind)) l.Add(s);
            return l;
        }

        /// <summary>A spell for an item found at this depth: any that fits, up to level 1 + depth/3 (deeper pulls the greater ones).</summary>
        public static SpellDef Pick(Rng rng, ItemKind kind, int depth)
        {
            int max = System.Math.Max(1, System.Math.Min(5, 1 + depth / 3));
            var pool = Pool(kind, max);
            if (pool.Count == 0)
            {
                // Nothing that low fits this kind of item (boots start at level 2): take the lowest that does.
                var all = Pool(kind, 5);
                int lowest = 6; foreach (var sp in all) lowest = System.Math.Min(lowest, sp.Level);
                pool = all.FindAll(sp => sp.Level == lowest);
            }
            return pool.Count == 0 ? null : pool[rng.Range(0, pool.Count)];
        }
    }
}
