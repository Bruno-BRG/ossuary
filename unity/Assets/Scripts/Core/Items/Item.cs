using System;
using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    public enum ItemKind { Weapon, Armor, Shield, Ring, Amulet, Wand, Scroll, Potion, Food, Gold, Gem, Tool, Corpse, Container, Book, Ornament, Statuette, Rock }

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
        public string Engraving;

        public Item(ItemDef def, Rng rng, long uid = 0)
        {
            Def = def;
            Uid = uid;
            Value = def.Cost;
            if (def.Kind == ItemKind.Scroll || def.Kind == ItemKind.Potion || def.Kind == ItemKind.Wand)
            {
                Charges = def.Flags.HasFlag(ItemFlags.Reusable) ? 8 : rng.Range(4, 12);
            }
            if (def.Kind == ItemKind.Gold) Value = 0;
        }

        public string Name
        {
            get
            {
                string n = ArtifactName ?? Def.Name;
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
                else if (Def.Kind == ItemKind.Armor) s += $" (being worn, AC {Def.AC})";
                else if (Def.Kind == ItemKind.Shield) s += $" (being worn, AC {Def.AC})";
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
