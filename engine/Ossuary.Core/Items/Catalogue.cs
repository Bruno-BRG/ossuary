using System;
using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    /// <summary>The static catalogue. Everything the world can hold is defined here.</summary>
    public static partial class Catalogue
    {
        static ItemDef[] _helms, _gloves, _boots, _cloaks;
        static ItemDef[] _weapons, _armor, _shields, _rings, _amulets, _wands, _scrolls, _potions, _food, _tools, _misc, _corpses, _books, _ornaments;

        public static IReadOnlyList<ItemDef> Weapons => _weapons;
        public static IReadOnlyList<ItemDef> Armor => _armor;
        public static IReadOnlyList<ItemDef> Shields => _shields;
        public static IReadOnlyList<ItemDef> Helms => _helms;
        public static IReadOnlyList<ItemDef> Gloves => _gloves;
        public static IReadOnlyList<ItemDef> Boots => _boots;
        public static IReadOnlyList<ItemDef> Cloaks => _cloaks;
        public static IReadOnlyList<ItemDef> Rings => _rings;
        public static IReadOnlyList<ItemDef> Amulets => _amulets;
        public static IReadOnlyList<ItemDef> Wands => _wands;
        public static IReadOnlyList<ItemDef> Scrolls => _scrolls;
        public static IReadOnlyList<ItemDef> Potions => _potions;
        public static IReadOnlyList<ItemDef> Food => _food;
        public static IReadOnlyList<ItemDef> Tools => _tools;
        public static IReadOnlyList<ItemDef> Misc => _misc;
        public static IReadOnlyList<ItemDef> Corpses => _corpses;
        public static IReadOnlyList<ItemDef> Books => _books;
        public static IReadOnlyList<ItemDef> Ornaments => _ornaments;

        /// <summary>A piece of wielded or worn gear by its base name.</summary>
        public static bool TryFindGear(string name, out ItemDef def)
        {
            foreach (var list in new[] { _weapons, _armor, _shields, _helms, _gloves, _boots, _cloaks })
                foreach (var d in list) if (d.Name == name) { def = d; return true; }
            def = default;
            return false;
        }

        static Catalogue()
        {
            _weapons = new[] {
                W("dagger",           '/', ItemClass.Blade,   200,  10, 2,  4, 0, 1, 0, 0),
                W("short sword",      '/', ItemClass.Blade,   400,  30, 1,  6, 0, 1, 1, 0),
                W("long sword",       '/', ItemClass.Blade,   800,  40, 1,  8, 0, 2, 2, 0),
                W("sabre",            '/', ItemClass.Blade,  1200,  40, 1,  9, 0, 2, 2, 0),
                W("war axe",          ')', ItemClass.Axe,     800,  60, 1,  6, 0, 1, 1, 0),
                W("battle axe",       ')', ItemClass.Axe,    1200, 100, 1,  8, 0, 2, 1, 0),
                W("mace",             ')', ItemClass.Club,    500,  40, 1,  6, 0, 1, 1, 0),
                W("war hammer",       ')', ItemClass.Club,   1000,  50, 1,  4, 2, 2, 1, 0),
                W("quarterstaff",     '/', ItemClass.Polearm, 500,  40, 1,  6, 0, 2, 0, ItemFlags.TwoHanded),
                W("spear",            ')', ItemClass.Polearm, 300,  30, 1,  6, 0, 1, 0, 0),
                W("trident",          ')', ItemClass.Polearm, 500,  40, 1,  7, 0, 1, 0, ItemFlags.TwoHanded),
                W("crossbow",         '}', ItemClass.Bow,    1000,  50, 0,  2, 0, 0, 1, ItemFlags.TwoHanded),
                W("short bow",        '}', ItemClass.Bow,     600,  30, 0,  2, 0, 0, 1, ItemFlags.TwoHanded),
                W("sling",            '}', ItemClass.Throw,   50,   3, 0,  2, 0, 0, 1, 0),
                W("whip",             ')', ItemClass.Whip,   300,   3, 0,  3, 0, 2, 1, 0),
                W("axe",              ')', ItemClass.Axe,     400,  60, 1,  6, 0, 1, 1, 0),
                W("elven bow",        '}', ItemClass.Bow,    4000,  35, 0,  2, 0, 0, 1, ItemFlags.TwoHanded | ItemFlags.Special),
                W("scimitar",         '/', ItemClass.Blade,  3000,  30, 1,  8, 0, 1, 1, ItemFlags.Special),
                W("flail",            ')', ItemClass.Club,   2000,  50, 1,  7, 0, 1, 1, ItemFlags.Special),
                W("quarterstaff of...",'/', ItemClass.Polearm, 500,  40, 1,  6, 0, 2, 0, ItemFlags.TwoHanded | ItemFlags.Cursed),
            };

            _armor = new[] {
                A("leather armour",   '[', 150,  50,  2, 0),
                A("ring mail",        '[', 500, 120,  3, 0),
                A("scale mail",       '[', 900, 200,  4, 0),
                A("chain mail",       '[',1200, 300,  5, 0),
                A("splint mail",      '[',2000, 400,  6, 0),
                A("plate mail",       '[',3000, 450,  7, 0),
                A("elven leather",    '[', 800,  60,  3, 1),
            };

            _helms = new[] {
                P(ItemKind.Helm, "leather cap",    '[',  40, 10, 1, 0),
                P(ItemKind.Helm, "orcish helm",    '[',  60, 30, 1, 0),
                P(ItemKind.Helm, "dwarvish helm",  '[', 120, 50, 2, 0),
                P(ItemKind.Helm, "great helm",     '[', 250, 60, 3, 1),
            };

            _gloves = new[] {
                P(ItemKind.Gloves, "leather gloves", '[',  40, 10, 1, 0),
                P(ItemKind.Gloves, "gauntlets",      '[', 150, 30, 2, 1),
            };

            _boots = new[] {
                P(ItemKind.Boots, "leather boots", '[',  40, 20, 1, 0),
                P(ItemKind.Boots, "iron boots",    '[', 120, 50, 2, 1),
            };

            _cloaks = new[] {
                P(ItemKind.Cloak, "cloak",              '[',   40, 10, 1, 0),
                P(ItemKind.Cloak, "cloak of elvenkind", '[', 1200, 50, 2, 1),
            };

            _shields = new[] {
                S("buckler",      '[',  30,  15, 1),
                S("small shield", '[',  50,  30, 1),
                S("shield",       '[', 100,  60, 2),
                S("large shield", '[', 200, 100, 3),
            };

            _rings = new[] {
                R("ring of protection",   '=', 300),
                R("ring of strength",     '=', 350),
                R("ring of dexterity",    '=', 350),
                R("ring of constitution", '=', 350),
                R("ring of invisibility", '=', 350),
                R("ring of warning",      '=', 300),
                R("ring of searching",    '=', 200),
                R("ring of slow digestion",'=',200),
                R("ring of sustain ability",'=',250),
                R("ring of aggravate monster",'=',400),
            };

            _amulets = new[] {
                R("amulet of ESP",           '"', 150),
                R("amulet of life saving",   '"', 300),
                R("amulet of strangulation", '"', 300, ItemFlags.Cursed),
                R("amulet versus poison",    '"', 200),
            };

            _wands = new[] {
                M("wand of light",          '/', 100),
                M("wand of striking",       '/', 200),
                M("wand of create monster", '/', 200),
                M("wand of digging",        '/', 200),
                M("wand of cold",           '/', 200),
                M("wand of fire",           '/', 200),
                M("wand of lightning",      '/', 200),
                M("wand of teleportation",  '/', 200),
                M("wand of opening",        '/', 200),
                M("wand of locking",        '/', 200),
                M("wand of nothing",        '/', 100, ItemFlags.Cursed),
            };

            _scrolls = new[] {
                M("scroll of identify",    '?',  50),
                M("scroll of mapping",     '?',  50),
                M("scroll of destroy armor",'?', 100),
                M("scroll of confuse monster",'?',100),
                M("scroll of fire",        '?', 100),
                M("scroll of earth",       '?', 100),
                M("scroll of punishment",  '?', 100),
                M("scroll of charging",    '?', 150),
                M("scroll of enchant weapon",'?', 200),
                M("scroll of enchant armour",'?', 200),
                M("scroll of teleportation",'?', 150),
                M("scroll of genocide",    '?', 400),
            };

            _potions = new[] {
                M("potion of healing",      '!', 100),
                M("potion of extra healing",'!', 150),
                M("potion of full healing", '!', 200),
                M("potion of poison",       '!', 150),
                M("potion of sleeping",     '!', 150),
                M("potion of confusion",    '!', 150),
                M("potion of hallucination",'!', 150),
                M("potion of speed",        '!', 200),
                M("potion of levitation",   '!', 200),
                M("potion of acid",         '!', 200),
                M("potion of oil",          '!', 250),
                M("potion of the mind",     '!', 220),
                M("potion of see invisible",'!', 200),
                M("potion of gain ability", '!', 300),
                M("potion of gain level",   '!', 500, ItemFlags.Special),
                M("potion of mutation",     '!', 300, ItemFlags.Special),
            };

            // Wands, scrolls and potions that are spells in a container (ItemSpells.cs) join the older, hand-written ones.
            _wands = WithSpellItems(_wands, ItemSpells.Wands, '/');
            _scrolls = WithSpellItems(_scrolls, ItemSpells.Scrolls, '?');
            _potions = WithSpellItems(_potions, ItemSpells.Potions, '!');

            // F(name, glyph, cost, nutrition, weight). The weights are per ration and are
            // deliberately small: they are compared against CarryingCapacity(), which
            // is in the low hundreds, so a "weight" of 800 made a single starting
            // ration weigh more than the player could carry.
            _food = new[] {
                F("food ration",        '%',  45, 800, 20),
                F("cram ration",        '%',  35, 600, 15),
                F("tripe ration",       '%',  25, 400, 12),
                F("lemon",              '%',  10,  20,  1),
                F("apple",              '%',   8,  30,  2),
                F("orange",             '%',   8,  30,  2),
                F("pear",               '%',   8,  30,  2),
                F("carrot",             '%',   7,  30,  1),
            };

            _tools = new[] {
                T("pick-axe",          '(', 100, 30, ItemClass.Heavy),
                T("lock pick",         '(',  20,  5, ItemClass.Light),
                T("large box",         '(',   8, 40, ItemClass.None),
                T("oilskin sack",      '(',  10, 20, ItemClass.None),
                T("chest",             '(',  16, 50, ItemClass.None),
                T("bag of holding",    '(', 100, 20, ItemClass.None, ItemFlags.Special),
                T("magic lamp",        '(',  50, 10, ItemClass.Light, ItemFlags.Special),
                T("candle",            '(',   5,  2, ItemClass.Light),
                T("tinning kit",       '(',  50,  5, ItemClass.Light),
                T("mirror",            '(',  10,  2, ItemClass.Light),
                T("blindfold",         '(',  50,  2, ItemClass.Light),
                T("unicorn horn",      '(', 100, 20, ItemClass.None, ItemFlags.Special),
            };

            AddMoreItems();

            var books = new List<ItemDef>();
            foreach (var bk in Magic.Spells.BookList) books.Add(new ItemDef { Name = bk.Name, Glyph = '+', Kind = ItemKind.Book, Cost = bk.Cost, Weight = 40, Tier = bk.Tier, Flags = ItemFlags.Uncursed });
            books.Add(B("a book of stone lore",  '+', 120));
            books.Add(B("a guidebook to the deep",'+', 80));
            _books = books.ToArray();

            _ornaments = new[] {
                O("valuable necklace",  '"', 600),
                O("gemstone",           '*', 400),
                O("pearls",             '"', 300),
                O("gold locket",        '"', 200),
            };

            _misc = new[] {
                new ItemDef { Name = "gold piece", Glyph = '$', Kind = ItemKind.Gold, Cost = 1, Weight = 1 },
                new ItemDef { Name = "rock", Glyph = '*', Kind = ItemKind.Rock, Cost = 1, Weight = 2, Flags = ItemFlags.TwoHanded },
                new ItemDef { Name = "silver piece", Glyph = '$', Kind = ItemKind.Gold, Cost = 10, Weight = 1 },
                new ItemDef { Name = "piece of jade", Glyph = '*', Kind = ItemKind.Gem, Cost = 60, Weight = 1, Flags = ItemFlags.Valuable },
            };

            _corpses = new[] {
                new ItemDef { Name = "small corpse", Glyph = '%', Kind = ItemKind.Corpse, Cost = 0, Weight = 20 },
                new ItemDef { Name = "human corpse", Glyph = '%', Kind = ItemKind.Corpse, Cost = 0, Weight = 80 },
            };
        }

        static ItemDef P(ItemKind kind, string n, char g, int cost, int wt, int ac, int tier)
            => new ItemDef { Name = n, Glyph = g, Kind = kind, Cost = cost, Weight = wt, AC = ac, Tier = tier, Flags = ItemFlags.Uncursed };

        static ItemDef S(string n, char g, int cost, int wt, int ac)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Shield, Cost = cost, Weight = wt, AC = ac, Tier = 0, Flags = ItemFlags.Uncursed };

        static ItemDef W(string n, char g, ItemClass c, int cost, int wt, int th, int sides, int dmg, int speed, int tier, ItemFlags f = 0)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Weapon, Class = c, Cost = cost, Weight = wt, ToHit = th, Sides = sides, DmgBonus = dmg, Speed = speed, Tier = tier, Flags = f | ItemFlags.Uncursed };

        static ItemDef A(string n, char g, int cost, int wt, int ac, int tier)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Armor, Cost = cost, Weight = wt, AC = ac, Tier = tier, Flags = ItemFlags.Uncursed };

        static ItemDef R(string n, char g, int cost, ItemFlags f = 0)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Ring, Cost = cost, Tier = 1, Flags = f | ItemFlags.Uncursed | ItemFlags.Reusable };

        static ItemDef M(string n, char g, int cost, ItemFlags f = 0)
            => new ItemDef { Name = n, Glyph = g, Kind = n.StartsWith("scroll") ? ItemKind.Scroll : n.StartsWith("potion") ? ItemKind.Potion : ItemKind.Wand, Cost = cost, Tier = 2, Flags = f | ItemFlags.Uncursed };

        static ItemDef F(string n, char g, int cost, int nutrition, int weight)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Food, Cost = cost, Nutrition = nutrition, Weight = weight, Tier = 0, Flags = ItemFlags.Uncursed };

        static ItemDef T(string n, char g, int cost, int wt, ItemClass c, ItemFlags f = 0)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Tool, Cost = cost, Weight = wt, Class = c, Tier = 0, Flags = f | ItemFlags.Uncursed };

        static ItemDef[] WithSpellItems(ItemDef[] old, MagicItemDef[] more, char glyph)
        {
            var list = new List<ItemDef>(old);
            foreach (var m in more)
            {
                int at = list.FindIndex(d => d.Name == m.Name);
                if (at >= 0) { var d = list[at]; d.Tier = m.Tier; list[at] = d; continue; }   // an older item: keep it, take the tier
                var def = M(m.Name, glyph, m.Cost);
                def.Tier = m.Tier;
                list.Add(def);
            }
            return list.ToArray();
        }

        static ItemDef B(string n, char g, int cost)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Book, Cost = cost, Weight = 40, Tier = 1, Flags = ItemFlags.Uncursed };

        static ItemDef O(string n, char g, int cost)
            => new ItemDef { Name = n, Glyph = g, Kind = ItemKind.Ornament, Cost = cost, Weight = 3, Tier = 2, Flags = ItemFlags.Valuable | ItemFlags.Uncursed };
    }
}
