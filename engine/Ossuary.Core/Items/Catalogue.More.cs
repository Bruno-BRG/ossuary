using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    /// <summary>More bases for every slot. Their numbers live here; what they do beyond armour and damage is in <see cref="ItemEffects"/>.</summary>
    public static partial class Catalogue
    {
        static readonly HashSet<string> Blank = new HashSet<string> { "silver band", "gold band", "bone ring", "jade ring", "charm", "pendant", "locket", "talisman" };

        /// <summary>A ring or amulet with no power of its own: it is only ever found enchanted, carrying a spell.</summary>
        public static bool IsBlank(string name) => Blank.Contains(name);

        static ItemDef Tier(ItemDef d, int tier) { d.Tier = tier; return d; }

        static ItemDef[] Join(ItemDef[] a, params ItemDef[] b)
        {
            var l = new List<ItemDef>(a); l.AddRange(b);
            return l.ToArray();
        }

        static void AddMoreItems()
        {
            const ItemFlags Two = ItemFlags.TwoHanded, Sp = ItemFlags.Special;
            _weapons = Join(_weapons,
                W("club", ')', ItemClass.Club, 30, 30, 0, 4, 0, 0, 0),
                W("hand axe", ')', ItemClass.Axe, 250, 30, 0, 5, 0, 1, 1),
                W("stiletto", '/', ItemClass.Blade, 250, 8, 3, 4, 0, 1, 1),
                W("main gauche", '/', ItemClass.Blade, 300, 10, 2, 4, 1, 1, 1),
                W("javelin", ')', ItemClass.Throw, 150, 10, 1, 6, 0, 0, 1),
                W("ashwood staff", '/', ItemClass.Polearm, 450, 40, 0, 6, 0, 2, 1, Two),
                W("kris", '/', ItemClass.Blade, 700, 12, 2, 5, 1, 1, 2),
                W("rapier", '/', ItemClass.Blade, 900, 25, 3, 6, 1, 1, 2),
                W("falchion", '/', ItemClass.Blade, 700, 45, 0, 7, 1, 1, 2),
                W("bastard sword", '/', ItemClass.Blade, 1100, 55, 1, 8, 1, 2, 2),
                W("morning star", ')', ItemClass.Club, 600, 45, 0, 7, 1, 1, 2),
                W("flanged mace", ')', ItemClass.Club, 800, 45, 0, 8, 0, 1, 2),
                W("war pick", ')', ItemClass.Axe, 700, 40, 0, 6, 2, 1, 2),
                W("glaive", ')', ItemClass.Polearm, 900, 75, 0, 9, 0, 2, 2, Two),
                W("pike", ')', ItemClass.Polearm, 600, 70, 0, 8, 0, 2, 2, Two),
                W("halberd", ')', ItemClass.Polearm, 1000, 90, 0, 10, 0, 2, 2, Two),
                W("druid's crook", '/', ItemClass.Polearm, 800, 35, 0, 6, 0, 2, 2, Two),
                W("greatsword", '/', ItemClass.Blade, 1800, 80, 0, 12, 0, 2, 3, Two),
                W("great axe", ')', ItemClass.Axe, 1600, 110, -1, 12, 0, 2, 3, Two),
                W("maul", ')', ItemClass.Club, 1400, 100, -1, 12, 0, 2, 3, Two),
                W("dwarven waraxe", ')', ItemClass.Axe, 2200, 90, 0, 10, 2, 1, 3),
                W("katana", '/', ItemClass.Blade, 2500, 40, 2, 10, 1, 1, 3, Sp),
                W("wizard's staff", '/', ItemClass.Polearm, 1500, 40, 0, 6, 0, 2, 3, Two),
                W("bone staff", '/', ItemClass.Polearm, 1200, 40, 0, 6, 0, 2, 3, Two),
                W("staff of the faithful", '/', ItemClass.Polearm, 1200, 40, 0, 6, 0, 2, 3, Two),
                W("elven blade", '/', ItemClass.Blade, 3200, 25, 3, 8, 2, 1, 4, Sp));

            _armor = Join(_armor,
                A("padded armour", '[', 100, 30, 1, 0),
                A("studded leather", '[', 400, 60, 3, 1),
                A("brigandine", '[', 700, 150, 4, 2),
                A("mage's robe", '[', 700, 15, 1, 2),
                A("druid's vestments", '[', 900, 20, 2, 2),
                A("priest's vestments", '[', 900, 20, 2, 2),
                A("banded mail", '[', 1600, 330, 6, 2),
                A("necromancer's shroud", '[', 1500, 15, 1, 3),
                A("shadowsilk tunic", '[', 1800, 15, 2, 3),
                A("half plate", '[', 2600, 420, 7, 3),
                A("full plate", '[', 4200, 500, 8, 4),
                A("archmage's robe", '[', 4000, 15, 2, 4),
                A("mithril shirt", '[', 5000, 40, 5, 4),
                A("dragonhide armour", '[', 6000, 120, 6, 4));

            _helms = Join(_helms,
                P(ItemKind.Helm, "coif of mail", '[', 150, 30, 2, 1),
                P(ItemKind.Helm, "circlet", '[', 300, 5, 1, 2),
                P(ItemKind.Helm, "wizard's hat", '[', 500, 5, 1, 2),
                P(ItemKind.Helm, "hood of shadows", '[', 700, 5, 1, 2),
                P(ItemKind.Helm, "horned helm", '[', 400, 50, 2, 2),
                P(ItemKind.Helm, "visored helm", '[', 350, 60, 3, 2),
                P(ItemKind.Helm, "skull cap of the dead", '[', 800, 20, 1, 3),
                P(ItemKind.Helm, "winged helm", '[', 700, 40, 2, 3),
                P(ItemKind.Helm, "laurel of the sage", '[', 900, 5, 1, 3),
                P(ItemKind.Helm, "crown of thorns", '[', 1200, 10, 1, 3));

            _gloves = Join(_gloves,
                P(ItemKind.Gloves, "gloves of dexterity", '[', 600, 10, 1, 2),
                P(ItemKind.Gloves, "gloves of spellcasting", '[', 800, 10, 1, 2),
                P(ItemKind.Gloves, "thieves' gloves", '[', 700, 10, 1, 2),
                P(ItemKind.Gloves, "mage's mitts", '[', 600, 10, 1, 2),
                P(ItemKind.Gloves, "gauntlets of the faithful", '[', 700, 35, 2, 2),
                P(ItemKind.Gloves, "gauntlets of ogre power", '[', 900, 40, 2, 3),
                P(ItemKind.Gloves, "bracers of defence", '[', 800, 20, 1, 3));

            _boots = Join(_boots,
                P(ItemKind.Boots, "sandals of the wind", '[', 600, 5, 1, 2),
                P(ItemKind.Boots, "boots of striding", '[', 700, 25, 1, 2),
                P(ItemKind.Boots, "boots of the mage", '[', 700, 15, 1, 2),
                P(ItemKind.Boots, "boots of elvenkind", '[', 900, 20, 1, 2),
                P(ItemKind.Boots, "boots of the north", '[', 800, 40, 1, 2),
                P(ItemKind.Boots, "boots of fire walking", '[', 800, 40, 1, 3));

            _cloaks = Join(_cloaks,
                P(ItemKind.Cloak, "wolf pelt", '[', 500, 40, 1, 1),
                P(ItemKind.Cloak, "cloak of protection", '[', 700, 10, 1, 2),
                P(ItemKind.Cloak, "cloak of the mage", '[', 900, 10, 1, 2),
                P(ItemKind.Cloak, "cloak of the bat", '[', 800, 10, 1, 2),
                P(ItemKind.Cloak, "cloak of fortitude", '[', 900, 10, 1, 2),
                P(ItemKind.Cloak, "cloak of resistance", '[', 1200, 10, 1, 3),
                P(ItemKind.Cloak, "cloak of shadows", '[', 1000, 10, 1, 3));

            _shields = Join(_shields,
                Tier(S("kite shield", '[', 250, 90, 3), 1),
                Tier(S("tower shield", '[', 400, 140, 4), 2),
                Tier(S("bone shield", '[', 600, 60, 2), 2),
                Tier(S("rune shield", '[', 1000, 60, 2), 3),
                Tier(S("mirror shield", '[', 900, 60, 3), 3),
                Tier(S("aegis of the faithful", '[', 1200, 70, 3), 3));

            _rings = Join(_rings,
                Tier(R("silver band", '=', 100), 0), Tier(R("gold band", '=', 150), 0), Tier(R("bone ring", '=', 120), 0), Tier(R("jade ring", '=', 200), 1),
                Tier(R("ring of fire resistance", '=', 400), 1), Tier(R("ring of frost resistance", '=', 400), 1), Tier(R("ring of storm resistance", '=', 400), 1),
                Tier(R("ring of poison resistance", '=', 400), 1), Tier(R("ring of the grave", '=', 450), 2),
                Tier(R("ring of accuracy", '=', 400), 1), Tier(R("ring of evasion", '=', 450), 2), Tier(R("ring of stealth", '=', 400), 1),
                Tier(R("ring of might", '=', 500), 2), Tier(R("ring of flames", '=', 600), 2), Tier(R("ring of frost", '=', 600), 2), Tier(R("ring of sparks", '=', 600), 2),
                Tier(R("ring of the mage", '=', 500), 2), Tier(R("ring of focus", '=', 500), 2), Tier(R("ring of vitality", '=', 500), 2),
                Tier(R("ring of intellect", '=', 600), 3), Tier(R("ring of insight", '=', 600), 3), Tier(R("ring of spell power", '=', 800), 3),
                Tier(R("ring of vampirism", '=', 900), 3), Tier(R("ring of the archmage", '=', 2500), 4));

            _amulets = Join(_amulets,
                Tier(R("charm", '"', 150), 0), Tier(R("pendant", '"', 200), 0), Tier(R("locket", '"', 250), 1), Tier(R("talisman", '"', 300), 1),
                Tier(R("amulet of stealth", '"', 400), 1), Tier(R("amulet of health", '"', 500), 2), Tier(R("amulet of warding", '"', 600), 2),
                Tier(R("amulet of vigor", '"', 500), 2), Tier(R("amulet of the wolf", '"', 600), 2), Tier(R("amulet of the hunter", '"', 600), 2),
                Tier(R("amulet of faith", '"', 700), 3), Tier(R("amulet of the grave", '"', 800), 3), Tier(R("amulet of resistance", '"', 800), 3),
                Tier(R("amulet of the sage", '"', 700), 3), Tier(R("amulet of the magi", '"', 1200), 3), Tier(R("amulet of spell power", '"', 1500), 4));

            // ---- the second round
            _weapons = Join(_weapons,
                W("spiked club", ')', ItemClass.Club, 300, 35, 0, 6, 2, 1, 1),
                W("cutlass", '/', ItemClass.Blade, 900, 35, 1, 7, 0, 1, 2),
                W("estoc", '/', ItemClass.Blade, 1000, 30, 3, 7, 1, 1, 2),
                W("runed dagger", '/', ItemClass.Blade, 900, 10, 2, 4, 1, 1, 2),
                W("witch's wand", '/', ItemClass.Blade, 800, 5, 1, 3, 0, 0, 2),
                W("sacrificial knife", '/', ItemClass.Blade, 1100, 10, 1, 4, 1, 1, 2),
                W("thornwood staff", '/', ItemClass.Polearm, 900, 40, 0, 6, 0, 2, 2, Two),
                W("lucerne hammer", ')', ItemClass.Polearm, 1100, 90, 0, 10, 1, 2, 3, Two),
                W("bardiche", ')', ItemClass.Axe, 1400, 100, -1, 11, 1, 2, 3, Two),
                W("claymore", '/', ItemClass.Blade, 2600, 90, 0, 14, 0, 2, 3, Two),
                W("crystal staff", '/', ItemClass.Polearm, 2200, 45, 0, 6, 0, 2, 4, Two));
            _armor = Join(_armor,
                A("quilted gambeson", '[', 200, 40, 2, 0),
                A("shaman's furs", '[', 700, 90, 2, 1),
                A("storm-cloth robe", '[', 1400, 15, 1, 3),
                A("battle-priest's mail", '[', 1500, 200, 4, 3),
                A("lamellar", '[', 1700, 220, 5, 3));
            _helms = Join(_helms,
                P(ItemKind.Helm, "bone crown", '[', 600, 30, 1, 2),
                P(ItemKind.Helm, "plague doctor's mask", '[', 700, 10, 1, 2),
                P(ItemKind.Helm, "iron halo", '[', 900, 40, 2, 3),
                P(ItemKind.Helm, "cat's-eye circlet", '[', 900, 5, 1, 3));
            _gloves = Join(_gloves,
                P(ItemKind.Gloves, "gloves of the healer", '[', 700, 10, 1, 2),
                P(ItemKind.Gloves, "gauntlets of flame", '[', 900, 40, 2, 3),
                P(ItemKind.Gloves, "gauntlets of storms", '[', 900, 40, 2, 3),
                P(ItemKind.Gloves, "witch's gloves", '[', 900, 10, 1, 3));
            _boots = Join(_boots,
                P(ItemKind.Boots, "boots of the wind-walker", '[', 900, 15, 1, 3),
                P(ItemKind.Boots, "boots of deep stone", '[', 800, 60, 2, 2),
                P(ItemKind.Boots, "ghoul-leather boots", '[', 800, 20, 1, 2));
            _cloaks = Join(_cloaks,
                P(ItemKind.Cloak, "feathered cloak", '[', 900, 10, 1, 2),
                P(ItemKind.Cloak, "cloak of the storm", '[', 1100, 10, 1, 3),
                P(ItemKind.Cloak, "mantle of the grave", '[', 1100, 10, 1, 3));
            _shields = Join(_shields,
                Tier(S("duelist's buckler", '[', 400, 20, 1), 2),
                Tier(S("shield of the sun", '[', 1100, 60, 3), 3),
                Tier(S("rampart", '[', 1500, 160, 5), 3));
            _rings = Join(_rings,
                Tier(R("ring of warding", '=', 700), 2), Tier(R("ring of the fox", '=', 600), 2), Tier(R("ring of resistance", '=', 900), 3),
                Tier(R("ring of mana", '=', 900), 3), Tier(R("ring of the sage", '=', 800), 3), Tier(R("ring of life", '=', 1000), 3),
                Tier(R("ring of the grave-knight", '=', 900), 3), Tier(R("ring of the assassin", '=', 900), 3), Tier(R("ring of the berserker", '=', 800), 3));
            _amulets = Join(_amulets,
                Tier(R("amulet of the phoenix", '"', 900), 3), Tier(R("amulet of the glacier", '"', 900), 3), Tier(R("amulet of the tempest", '"', 900), 3),
                Tier(R("amulet of the oracle", '"', 1100), 3), Tier(R("amulet of the berserker", '"', 900), 3), Tier(R("amulet of the shadow", '"', 1000), 3));

            // Amulets were catalogued with the ring helper, so they were rings that happened to be called amulets. They are worn at the neck.
            _amulets = System.Array.ConvertAll(_amulets, d => { d.Kind = ItemKind.Amulet; return d; });
        }
    }
}
