using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core.Items
{
    /// <summary>
    /// What a kind of item does just by being worn or wielded, by name: the numbers rings, amulets, robes, staves and enchanted
    /// helms, gloves, boots, cloaks and shields add. Affixes, enchantment and artifacts stack on top of these.
    /// </summary>
    public static class ItemEffects
    {
        static readonly Dictionary<string, ItemMods> Table = new Dictionary<string, ItemMods>
        {
            // ---- rings
            ["ring of fire resistance"] = new ItemMods { ResFire = 40 },
            ["ring of frost resistance"] = new ItemMods { ResCold = 40 },
            ["ring of storm resistance"] = new ItemMods { ResLightning = 40 },
            ["ring of poison resistance"] = new ItemMods { ResPoison = 50 },
            ["ring of the grave"] = new ItemMods { ResNecrotic = 40 },
            ["ring of spell power"] = new ItemMods { SpellPower = 2 },
            ["ring of focus"] = new ItemMods { SpellFocus = 10 },
            ["ring of the mage"] = new ItemMods { Mp = 8 },
            ["ring of vitality"] = new ItemMods { Hp = 12 },
            ["ring of evasion"] = new ItemMods { Evasion = 3 },
            ["ring of stealth"] = new ItemMods { Stealth = 2 },
            ["ring of vampirism"] = new ItemMods { LifeSteal = 10 },
            ["ring of flames"] = new ItemMods { ExtraType = DamageType.Fire, ExtraSides = 4 },
            ["ring of frost"] = new ItemMods { ExtraType = DamageType.Cold, ExtraSides = 4 },
            ["ring of sparks"] = new ItemMods { ExtraType = DamageType.Lightning, ExtraSides = 4 },
            ["ring of accuracy"] = new ItemMods { ToHit = 3 },
            ["ring of might"] = new ItemMods { Dmg = 2 },
            ["ring of intellect"] = new ItemMods { Int = 2 },
            ["ring of insight"] = new ItemMods { Wis = 2 },
            ["ring of the archmage"] = new ItemMods { Int = 2, SpellPower = 2, Mp = 6 },
            ["ring of warding"] = new ItemMods { Ac = 2, Evasion = 1 },
            ["ring of the fox"] = new ItemMods { Dex = 2, Evasion = 1 },
            ["ring of resistance"] = new ItemMods { ResFire = 20, ResCold = 20, ResLightning = 20 },
            ["ring of mana"] = new ItemMods { Mp = 12 },
            ["ring of the sage"] = new ItemMods { Int = 1, Wis = 1, Mp = 4 },
            ["ring of life"] = new ItemMods { Hp = 20 },
            ["ring of the grave-knight"] = new ItemMods { ResNecrotic = 30, Hp = 8 },
            ["ring of the assassin"] = new ItemMods { Stealth = 2, ToHit = 2 },
            ["ring of the berserker"] = new ItemMods { Dmg = 3, ToHit = -1, Hp = 6 },
            ["amulet of the phoenix"] = new ItemMods { ResFire = 40, Hp = 10 },
            ["amulet of the glacier"] = new ItemMods { ResCold = 40, Hp = 10 },
            ["amulet of the tempest"] = new ItemMods { ResLightning = 40, Evasion = 1 },
            ["amulet of the oracle"] = new ItemMods { Wis = 2, SpellFocus = 10 },
            ["amulet of the berserker"] = new ItemMods { Dmg = 2, Hp = 8 },
            ["amulet of the shadow"] = new ItemMods { Stealth = 3, Evasion = 2 },
            ["runed dagger"] = new ItemMods { SpellPower = 1 },
            ["witch's wand"] = new ItemMods { SpellPower = 1, Mp = 3 },
            ["sacrificial knife"] = new ItemMods { LifeSteal = 8 },
            ["thornwood staff"] = new ItemMods { Wis = 1, Stealth = 1 },
            ["crystal staff"] = new ItemMods { Mp = 8, SpellFocus = 8, SpellPower = 1 },
            ["shaman's furs"] = new ItemMods { ResCold = 20, Hp = 6 },
            ["storm-cloth robe"] = new ItemMods { ResLightning = 25, Mp = 4 },
            ["battle-priest's mail"] = new ItemMods { Wis = 1, SpellFocus = 4 },
            ["bone crown"] = new ItemMods { Mp = 3, ResNecrotic = 15 },
            ["plague doctor's mask"] = new ItemMods { ResPoison = 40 },
            ["iron halo"] = new ItemMods { Wis = 1 },
            ["cat's-eye circlet"] = new ItemMods { Evasion = 1, Stealth = 1 },
            ["gloves of the healer"] = new ItemMods { Wis = 1, Hp = 6 },
            ["gauntlets of flame"] = new ItemMods { ResFire = 30 },
            ["gauntlets of storms"] = new ItemMods { ResLightning = 30 },
            ["witch's gloves"] = new ItemMods { SpellPower = 1 },
            ["boots of the wind-walker"] = new ItemMods { Evasion = 3 },
            ["boots of deep stone"] = new ItemMods { Con = 1, ResFire = 15 },
            ["ghoul-leather boots"] = new ItemMods { ResNecrotic = 20, Stealth = 1 },
            ["feathered cloak"] = new ItemMods { Evasion = 2, Dex = 1 },
            ["cloak of the storm"] = new ItemMods { ResLightning = 25, Mp = 3 },
            ["mantle of the grave"] = new ItemMods { ResNecrotic = 30, SpellPower = 1 },
            ["duelist's buckler"] = new ItemMods { Evasion = 2 },
            ["shield of the sun"] = new ItemMods { ResFire = 20, ResNecrotic = 15 },
            ["silver band"] = default, ["gold band"] = default, ["bone ring"] = default, ["jade ring"] = default,
            ["charm"] = default, ["pendant"] = default, ["locket"] = default, ["talisman"] = default,

            // ---- amulets
            ["amulet versus poison"] = new ItemMods { ResPoison = 60 },
            // Cursed: strong on the neck, and they will not come off until a priest lifts the curse (Game.Services.cs).
            ["amulet of the leech"] = new ItemMods { Dmg = 2, LifeSteal = 10 },
            ["amulet of restless sleep"] = new ItemMods { Mp = 10, SpellPower = 1 },
            ["amulet of the hungry dead"] = new ItemMods { Str = 2, Con = 1 },
            ["amulet of health"] = new ItemMods { Hp = 15 },
            ["amulet of the magi"] = new ItemMods { Mp = 10, SpellFocus = 5 },
            ["amulet of warding"] = new ItemMods { Ac = 2, Evasion = 1 },
            ["amulet of resistance"] = new ItemMods { ResFire = 25, ResCold = 25, ResLightning = 25 },
            ["amulet of the grave"] = new ItemMods { ResNecrotic = 50, SpellPower = 1 },
            ["amulet of stealth"] = new ItemMods { Stealth = 2 },
            ["amulet of faith"] = new ItemMods { Wis = 2, ResNecrotic = 20 },
            ["amulet of the wolf"] = new ItemMods { Str = 1, Dex = 1 },
            ["amulet of vigor"] = new ItemMods { Vigor = 10 },
            ["amulet of spell power"] = new ItemMods { SpellPower = 3 },
            ["amulet of the sage"] = new ItemMods { Int = 1, Wis = 1, Mp = 4 },
            ["amulet of the hunter"] = new ItemMods { ToHit = 2, Stealth = 1 },

            // ---- staves and casters' weapons
            ["ashwood staff"] = new ItemMods { Mp = 4 },
            ["wizard's staff"] = new ItemMods { Mp = 6, SpellPower = 1 },
            ["bone staff"] = new ItemMods { SpellPower = 1, ResNecrotic = 20 },
            ["staff of the faithful"] = new ItemMods { SpellFocus = 5, Wis = 1 },
            ["druid's crook"] = new ItemMods { Wis = 1, Stealth = 1 },

            // ---- robes and light armour
            ["mage's robe"] = new ItemMods { Mp = 6, SpellFocus = 5 },
            ["archmage's robe"] = new ItemMods { Mp = 12, SpellPower = 2, SpellFocus = 10 },
            ["necromancer's shroud"] = new ItemMods { SpellPower = 1, ResNecrotic = 25 },
            ["druid's vestments"] = new ItemMods { Wis = 1, Stealth = 1 },
            ["priest's vestments"] = new ItemMods { Wis = 1, SpellFocus = 5 },
            ["shadowsilk tunic"] = new ItemMods { Evasion = 2, Stealth = 1 },
            ["dragonhide armour"] = new ItemMods { ResFire = 25, ResCold = 15 },
            ["mithril shirt"] = new ItemMods { Evasion = 1 },

            // ---- helms
            ["circlet"] = new ItemMods { Mp = 4 },
            ["wizard's hat"] = new ItemMods { Mp = 5, SpellPower = 1 },
            ["hood of shadows"] = new ItemMods { Stealth = 1, Evasion = 1 },
            ["horned helm"] = new ItemMods { Str = 1 },
            ["skull cap of the dead"] = new ItemMods { ResNecrotic = 20 },
            ["winged helm"] = new ItemMods { Dex = 1 },
            ["laurel of the sage"] = new ItemMods { Wis = 1, SpellFocus = 5 },
            ["crown of thorns"] = new ItemMods { SpellPower = 1, Hp = -4 },

            // ---- gloves
            ["gloves of dexterity"] = new ItemMods { Dex = 2 },
            ["gauntlets of ogre power"] = new ItemMods { Str = 2 },
            ["gloves of spellcasting"] = new ItemMods { SpellFocus = 8 },
            ["thieves' gloves"] = new ItemMods { Dex = 1, Stealth = 1 },
            ["mage's mitts"] = new ItemMods { Int = 1, Mp = 4 },
            ["gauntlets of the faithful"] = new ItemMods { Wis = 1 },
            ["bracers of defence"] = new ItemMods { Ac = 2 },

            // ---- boots
            ["boots of elvenkind"] = new ItemMods { Stealth = 2 },
            ["boots of striding"] = new ItemMods { Evasion = 1, Dex = 1 },
            ["sandals of the wind"] = new ItemMods { Evasion = 2 },
            ["boots of the north"] = new ItemMods { ResCold = 30 },
            ["boots of fire walking"] = new ItemMods { ResFire = 30 },
            ["boots of the mage"] = new ItemMods { Mp = 4, Evasion = 1 },

            // ---- cloaks
            ["cloak of protection"] = new ItemMods { Ac = 2 },
            ["cloak of the mage"] = new ItemMods { Mp = 5, SpellFocus = 5 },
            ["cloak of the bat"] = new ItemMods { Stealth = 2 },
            ["cloak of resistance"] = new ItemMods { ResFire = 15, ResCold = 15, ResLightning = 15 },
            ["cloak of shadows"] = new ItemMods { Evasion = 2, Stealth = 1 },
            ["cloak of fortitude"] = new ItemMods { Hp = 10 },
            ["wolf pelt"] = new ItemMods { Str = 1, ResCold = 20 },

            // ---- shields
            ["bone shield"] = new ItemMods { ResNecrotic = 20 },
            ["mirror shield"] = new ItemMods { ResLightning = 20, ResFire = 10 },
            ["aegis of the faithful"] = new ItemMods { Wis = 1, ResNecrotic = 20 },
            ["rune shield"] = new ItemMods { SpellFocus = 5, Mp = 3 },
        };

        public static IEnumerable<string> Names => Table.Keys;

        public static ItemMods For(string name) => name != null && Table.TryGetValue(name, out var m) ? m : default;
    }
}
