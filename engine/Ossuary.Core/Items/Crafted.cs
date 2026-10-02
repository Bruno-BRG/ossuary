namespace Ossuary.Core.Items
{
    /// <summary>
    /// What the player can make (see Game.Crafting). These defs live outside the loot tables on purpose: they are
    /// never found lying around, only built, so the random loot is exactly what it was.
    /// </summary>
    public static class Crafted
    {
        public static readonly ItemDef Molotov = new ItemDef
        {
            Name = "molotov", Glyph = '!', Kind = ItemKind.Tool, Class = ItemClass.Throw, Cost = 40, Weight = 12, Tier = 1, Flags = ItemFlags.Uncursed,
        };

        public static readonly ItemDef BoneBlade = new ItemDef
        {
            Name = "bone blade", Glyph = '/', Kind = ItemKind.Weapon, Class = ItemClass.Blade, Cost = 900, Weight = 25,
            ToHit = 2, Sides = 8, DmgBonus = 1, Speed = 2, Tier = 2, Flags = ItemFlags.Uncursed,
        };

        public static readonly ItemDef BoneArmour = new ItemDef
        {
            Name = "bone-studded armour", Glyph = '[', Kind = ItemKind.Armor, Cost = 500, Weight = 150, AC = 5, Tier = 2, Flags = ItemFlags.Uncursed,
        };
    }
}
