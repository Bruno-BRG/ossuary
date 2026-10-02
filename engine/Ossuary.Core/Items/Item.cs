using System;
using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    public enum ItemKind { Weapon, Armor, Shield, Ring, Amulet, Wand, Scroll, Potion, Food, Gold, Gem, Tool, Corpse, Container, Book, Ornament, Statuette, Rock, Helm, Gloves, Boots, Cloak }

    public static class ItemKinds
    {
        /// <summary>Anything that is wielded or worn (and so may roll affixes).</summary>
        public static bool IsGear(this ItemKind k) =>
            k == ItemKind.Weapon || k == ItemKind.Armor || k == ItemKind.Shield || k == ItemKind.Helm || k == ItemKind.Gloves || k == ItemKind.Boots || k == ItemKind.Cloak;

        /// <summary>Worn armour of any slot, shield included.</summary>
        public static bool IsWearable(this ItemKind k) => k.IsGear() && k != ItemKind.Weapon;
    }

    public enum ItemClass { None, Light, Heavy, Blade, Polearm, Axe, Club, Whip, Bow, Throw, Magic }

    [Flags]
    public enum ItemFlags
    {
        None = 0,
        TwoHanded = 1 << 0,
        Fragile = 1 << 1,
        Blessed = 1 << 2,
        Cursed = 1 << 3,
        Uncursed = 1 << 4,
        Rare = 1 << 5,
        Special = 1 << 6,
        Reusable = 1 << 7,
        Valuable = 1 << 8,
        RandomAppearing = 1 << 9,
        Charging = 1 << 10,
        QuestItem = 1 << 11,
    }

    public struct ItemDef
    {
        public string Name;
        public char Glyph;
        public ItemKind Kind;
        public ItemClass Class;
        public int Cost;        // base value in gold
        public int Weight;
        public int AC;          // for armor
        public int ToHit;
        public int Damage;      // dice count
        public int Sides;       // dice sides
        public int DmgBonus;    // flat modifier
        public int Speed;       // attack speed
        public int Nutrition;
        public ItemFlags Flags;
        public int Tier;        // 0 common .. 4 artifact-ish
        public string SlotHint;
    }

    /// <summary>A concrete item in the world or in a bag. Copies of a def diverge only via charges/chargesUsed.</summary>
    public sealed class Item
    {
        public ItemDef Def;
        public int Charges = -1;
        public int ChargesUsed;
        public int Quantity = 1;
        public int Value;        // rolled at creation
        public long Uid;
        public bool Identified;
        public string ArtifactName;
        public string ArtifactId;
        public Rarity Rarity;
        /// <summary>Enchantment: +to hit and damage on weapons, +AC on armour.</summary>
        public int Enchant;
        public string Prefix, Suffix;
        public string Engraving;

        public Item(ItemDef def, Rng rng, long uid = 0)
        {
            Def = def;
            Uid = uid;
            Value = def.Cost;
            // Scrolls and potions are single-use; only wands carry charges.
            if (def.Kind == ItemKind.Scroll || def.Kind == ItemKind.Potion) Charges = 1;
            else if (def.Kind == ItemKind.Wand) Charges = def.Flags.HasFlag(ItemFlags.Reusable) ? 8 : rng.Range(4, 12);
            if (def.Kind == ItemKind.Gold) Value = 0;
        }

        /// <summary>The affixes, enchantment and artifact bonuses as one block of numbers.</summary>
        public ItemMods Mods
        {
            get
            {
                var m = new ItemMods();
                var pre = Affixes.Find(Prefix); if (pre != null) m.Add(pre.Mods);
                var suf = Affixes.Find(Suffix); if (suf != null) m.Add(suf.Mods);
                var art = Artifacts.Find(ArtifactId); if (art != null) m.Add(art.Mods);
                if (Def.Kind == ItemKind.Weapon) { m.ToHit += Enchant; m.Dmg += Enchant; }
                else if (Def.Kind.IsWearable()) m.Ac += Enchant;
                return m;
            }
        }

        /// <summary>Armour class this piece gives, base plus enchantment and affixes.</summary>
        public int TotalAc => Def.Kind.IsWearable() ? Def.AC + Mods.Ac : 0;

        /// <summary>What a shop thinks it is worth: base cost, grown by enchantment, affixes and rarity.</summary>
        public int TradeValue
        {
            get
            {
                if (!Def.Kind.IsGear()) return Def.Cost;
                int affixes = (Prefix != null ? 1 : 0) + (Suffix != null ? 1 : 0);
                int v = Def.Cost * (4 + Math.Max(0, Enchant) * 3 + affixes * 5) / 4;
                if (Rarity == Rarity.Artifact) v = Def.Cost * 12 + 1000;
                return Math.Max(1, v);
            }
        }

        public string Name
        {
            get
            {
                string n = ArtifactName ?? Def.Name;
                bool gear = Def.Kind.IsGear();
                if (gear && ArtifactName == null)
                {
                    if (Rarity != Rarity.Common && !Identified) n = "magical " + Def.Name;
                    else
                    {
                        string pre = Affixes.Find(Prefix)?.Name, suf = Affixes.Find(Suffix)?.Name;
                        n = (Enchant != 0 ? (Enchant > 0 ? "+" : "") + Enchant + " " : "")
                            + (pre != null ? pre + " " : "") + Def.Name + (suf != null ? " " + suf : "");
                    }
                }
                if (!Identified && (ArtifactName != null || (Def.Flags & ItemFlags.Cursed) != 0)) return n;
                if ((Def.Flags & ItemFlags.Blessed) != 0) return "blessed " + n;
                if ((Def.Flags & ItemFlags.Cursed) != 0) return "cursed " + n;
                return n;
            }
        }

        public string LongName
        {
            get
            {
                string s = Name;
                if (Def.Kind == ItemKind.Weapon)
                {
                    s += $" (weapon in hand)";
                    if (Def.Damage > 0) s += $" {Def.Damage}d{Def.Sides}";
                    if (Def.DmgBonus != 0) s += Def.DmgBonus > 0 ? $"+{Def.DmgBonus}" : Def.DmgBonus.ToString();
                }
                else if (Def.Kind.IsWearable()) s += $" (being worn, AC {TotalAc})";
                else if (Def.Kind == ItemKind.Ring) s += " (on right hand)";
                else if (Def.Kind == ItemKind.Amulet) s += " (being worn)";
                else if (Def.Kind == ItemKind.Wand) s += $" (charges {Math.Max(0, Charges - ChargesUsed)})";
                else if (Def.Kind == ItemKind.Scroll || Def.Kind == ItemKind.Potion)
                {
                    if (!Identified) s += " (unidentified)";
                    else s += $" (charges {Math.Max(0, Charges - ChargesUsed)})";
                }
                else if (Def.Kind == ItemKind.Gold) s += $" ({Quantity} gold pieces)";
                return s;
            }
        }

        public int RemainingCharges => Math.Max(0, Charges - ChargesUsed);
    }
}
