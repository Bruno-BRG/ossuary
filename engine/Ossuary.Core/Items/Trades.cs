using System;
using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    /// <summary>Where a recipe can be worked: anywhere (a knife and a fire will do), at a town workshop, or beside a forge.</summary>
    public enum Station { Anywhere, Workshop, Forge }

    public sealed class TradeDef
    {
        /// <summary>Lowercase, as it reads inside a sentence ("a journeyman blacksmith"); <see cref="Title"/> heads a list.</summary>
        public string Id, Name, Blurb;
        public string Title => char.ToUpperInvariant(Name[0]) + Name.Substring(1);
    }

    public sealed class Need
    {
        /// <summary>An item name, or a token: #remains, #blade, #light-armour, #gem.</summary>
        public string Name;
        public int Count = 1;
    }

    public sealed class RecipeDef
    {
        public string Id, Trade, Product;
        /// <summary>Trade rank needed (0 novice .. 4 grandmaster).</summary>
        public int Rank;
        public Station Station;
        public Need[] Needs;
        /// <summary>Metal bars of one kind the piece takes; the product is made of that metal.</summary>
        public int Bars;
        public int Qty = 1;
    }

    /// <summary>
    /// Crafting as data: the trades anyone can learn, their ranks, the raw and worked goods of the world, and every
    /// recipe. The rules that use them are in Game.Crafting.cs, Game.Gathering.cs and Game.Music.cs.
    /// </summary>
    public static class Trades
    {
        public static readonly TradeDef[] All =
        {
            T("blacksmith", "forges weapons from metal bars"),
            T("armourer", "forges mail, helms, gauntlets and shields"),
            T("bowyer", "bends bows and slings"),
            T("leatherworker", "tans hides and sews leather gear"),
            T("tailor", "spins thread, weaves cloth, cuts cloaks"),
            T("jeweller", "sets stones in rings and amulets"),
            T("alchemist", "brews potions from herbs"),
            T("scribe", "makes parchment and ink, and writes scrolls"),
            T("carpenter", "saws planks, staves, boxes and bucklers"),
            T("toolmaker", "makes picks, lock picks, rods and lamps"),
            T("luthier", "builds lutes, harps, drums and flutes"),
            T("cook", "turns meat, fish and grain into meals"),
            T("brewer", "brews ale and mead"),
            T("miner", "digs ore and smelts it into bars"),
            T("musician", "plays for coin, or to calm what listens"),
            T("forager", "finds herbs, fibre, honey and wood"),
        };

        static TradeDef T(string id, string blurb) => new TradeDef { Id = id, Name = id, Blurb = blurb };

        public static TradeDef Find(string id) { foreach (var t in All) if (t.Id == id) return t; return null; }

        public static readonly int[] RankXp = { 0, 20, 60, 150, 300 };
        public static readonly string[] RankNames = { "Novice", "Apprentice", "Journeyman", "Master", "Grandmaster" };

        public static int Rank(int xp) { int r = 0; for (int i = 1; i < RankXp.Length; i++) if (xp >= RankXp[i]) r = i; return r; }

        /// <summary>The rank a metal asks of the hand that works it.</summary>
        public static int MetalRank(MaterialDef m)
        {
            switch (m?.Id)
            {
                case "steel": case "silver": return 1;
                case "cold-iron": return 2;
                case "mithril": case "adamantine": return 3;
                default: return 0;
            }
        }

        // ---------------------------------------------------------------- goods

        static ItemDef G(string name, char glyph, int cost, int weight) =>
            new ItemDef { Name = name, Glyph = glyph, Kind = ItemKind.Material, Cost = cost, Weight = weight, Tier = 1, Flags = ItemFlags.Uncursed };

        static ItemDef Meal(string name, int cost, int nutrition, int weight) =>
            new ItemDef { Name = name, Glyph = '%', Kind = ItemKind.Food, Cost = cost, Nutrition = nutrition, Weight = weight, Tier = 1, Flags = ItemFlags.Uncursed };

        static ItemDef Instrument(string name, int cost, int weight) =>
            new ItemDef { Name = name, Glyph = '(', Kind = ItemKind.Tool, Class = ItemClass.Light, Cost = cost, Weight = weight, Tier = 1, Flags = ItemFlags.Uncursed };

        /// <summary>One bar per metal (see <see cref="Materials"/>), named "metal bar".</summary>
        public static readonly ItemDef[] Bars =
        {
            G("copper bar", '=', 15, 10), G("bronze bar", '=', 25, 10), G("iron bar", '=', 30, 10), G("steel bar", '=', 70, 10),
            G("silver bar", '=', 120, 11), G("cold iron bar", '=', 100, 10), G("mithril bar", '=', 450, 5), G("adamantine bar", '=', 650, 12),
        };

        /// <summary>The metal a bar is ("steel bar" is steel), or null.</summary>
        public static MaterialDef BarMetal(string name)
        {
            if (name == null || !name.EndsWith(" bar")) return null;
            string metal = name.Substring(0, name.Length - 4);
            foreach (var m in Materials.All) if (m.Metal && m.Name == metal) return m;
            return null;
        }

        public static string BarOf(MaterialDef m) => m.Name + " bar";

        /// <summary>Raw and worked goods that are neither food nor finished gear.</summary>
        public static readonly ItemDef[] Goods =
        {
            G("log", '-', 4, 20), G("plank", '-', 6, 8), G("raw hide", '%', 5, 15), G("leather", '-', 15, 8),
            G("flax", '"', 2, 2), G("thread", '"', 4, 1), G("cloth", '-', 12, 4),
            G("healing herb", '"', 8, 1), G("swiftroot", '"', 15, 1), G("nightshade", '"', 10, 1),
            G("glass flask", '!', 5, 3), G("parchment", '?', 10, 1), G("ink", '!', 12, 2),
            G("tallow", '%', 3, 3), G("barley", '"', 2, 2), G("honey", '!', 6, 3), G("raw meat", '%', 3, 10), G("raw fish", '%', 3, 6),
        };

        public static readonly ItemDef[] Meals =
        {
            Meal("roast meat", 12, 500, 10), Meal("grilled fish", 10, 400, 6), Meal("hearty stew", 25, 900, 15),
            Meal("loaf of bread", 8, 400, 6), Meal("honey cake", 15, 350, 4), Meal("mug of ale", 6, 150, 5), Meal("bottle of mead", 14, 200, 6),
        };

        public static readonly ItemDef[] Instruments =
        {
            Instrument("flute", 30, 3), Instrument("drum", 45, 20), Instrument("tambourine", 40, 6), Instrument("lute", 120, 25),
            Instrument("horn", 90, 12), Instrument("fiddle", 160, 15), Instrument("harp", 260, 35),
        };

        /// <summary>Tools that only crafting brings into the world.</summary>
        public static readonly ItemDef[] NewTools =
        {
            new ItemDef { Name = "fishing rod", Glyph = '(', Kind = ItemKind.Tool, Class = ItemClass.Light, Cost = 25, Weight = 10, Tier = 1, Flags = ItemFlags.Uncursed },
        };

        public static bool IsInstrument(Item it) { if (it == null) return false; foreach (var d in Instruments) if (d.Name == it.Def.Name) return true; return false; }

        static Dictionary<string, ItemDef> _defs;

        /// <summary>Any def a recipe can name, from the catalogue, the crafted list or the goods above.</summary>
        public static bool TryDef(string name, out ItemDef def)
        {
            if (_defs == null)
            {
                var d = new Dictionary<string, ItemDef>();
                void Add(IEnumerable<ItemDef> list) { foreach (var x in list) if (x.Name != null && !d.ContainsKey(x.Name)) d[x.Name] = x; }
                Add(Catalogue.Weapons); Add(Catalogue.Armor); Add(Catalogue.Shields); Add(Catalogue.Helms); Add(Catalogue.Gloves); Add(Catalogue.Boots);
                Add(Catalogue.Cloaks); Add(Catalogue.Rings); Add(Catalogue.Amulets); Add(Catalogue.Potions); Add(Catalogue.Scrolls); Add(Catalogue.Food);
                Add(Catalogue.Tools); Add(Catalogue.Ornaments); Add(Catalogue.Misc);
                Add(new[] { Crafted.Molotov, Crafted.BoneBlade, Crafted.BoneArmour });
                Add(Ammo.All);
                Add(Materials.Ores); Add(Bars); Add(Goods); Add(Meals); Add(Instruments); Add(NewTools);
                _defs = d;
            }
            return _defs.TryGetValue(name, out def);
        }

        // ---------------------------------------------------------------- recipes

        static RecipeDef X(string trade, int rank, Station st, string product, string needs, int bars = 0, int qty = 1)
        {
            var list = new List<Need>();
            if (!string.IsNullOrEmpty(needs))
                foreach (string part in needs.Split(new[] { " + " }, StringSplitOptions.None))
                {
                    int sp = part.IndexOf(' ');
                    if (sp > 0 && int.TryParse(part.Substring(0, sp), out int n)) list.Add(new Need { Name = part.Substring(sp + 1), Count = n });
                    else list.Add(new Need { Name = part });
                }
            return new RecipeDef { Id = trade + ":" + product, Trade = trade, Rank = rank, Station = st, Product = product, Needs = list.ToArray(), Bars = bars, Qty = qty };
        }

        /// <summary>A second recipe for the same product needs its own id.</summary>
        static RecipeDef Named(string id, RecipeDef r) { r.Id = id; return r; }

        const Station Any = Station.Anywhere, Shop = Station.Workshop, Forge = Station.Forge;

        public static readonly RecipeDef[] Recipes =
        {
            // miner: ore into bars
            X("miner", 0, Forge, "copper bar", "copper ore"),
            X("miner", 1, Forge, "bronze bar", "2 copper ore"),
            X("miner", 0, Forge, "iron bar", "iron ore"),
            X("miner", 1, Forge, "steel bar", "2 iron ore"),
            X("miner", 2, Forge, "cold iron bar", "2 iron ore"),
            X("miner", 1, Forge, "silver bar", "silver ore"),
            X("miner", 3, Forge, "mithril bar", "2 mithril ore"),
            X("miner", 3, Forge, "adamantine bar", "2 adamantine ore"),

            // blacksmith: weapons in the bar's metal
            X("blacksmith", 0, Forge, "dagger", "", 1),
            X("blacksmith", 0, Forge, "short sword", "", 2),
            X("blacksmith", 1, Forge, "long sword", "", 3),
            X("blacksmith", 2, Forge, "sabre", "", 3),
            X("blacksmith", 2, Forge, "scimitar", "", 3),
            X("blacksmith", 0, Forge, "axe", "log", 2),
            X("blacksmith", 1, Forge, "war axe", "log", 2),
            X("blacksmith", 2, Forge, "battle axe", "log", 3),
            X("blacksmith", 0, Forge, "mace", "log", 2),
            X("blacksmith", 1, Forge, "war hammer", "log", 3),
            X("blacksmith", 2, Forge, "flail", "log", 2),
            X("blacksmith", 0, Forge, "spear", "log", 1),
            X("blacksmith", 1, Forge, "trident", "log", 2),
            X("blacksmith", 0, Any, "bone blade", "#blade + #remains"),

            // armourer: mail, helms, gauntlets, shields in the bar's metal
            X("armourer", 0, Forge, "ring mail", "leather", 3),
            X("armourer", 1, Forge, "scale mail", "leather", 4),
            X("armourer", 1, Forge, "chain mail", "", 4),
            X("armourer", 2, Forge, "splint mail", "leather", 5),
            X("armourer", 3, Forge, "plate mail", "leather", 6),
            X("armourer", 0, Forge, "orcish helm", "", 1),
            X("armourer", 1, Forge, "dwarvish helm", "", 2),
            X("armourer", 2, Forge, "great helm", "", 3),
            X("armourer", 1, Forge, "gauntlets", "leather", 2),
            X("armourer", 1, Forge, "iron boots", "2 iron bar + leather"),
            X("armourer", 0, Forge, "small shield", "plank", 1),
            X("armourer", 1, Forge, "shield", "2 plank", 2),
            X("armourer", 2, Forge, "large shield", "2 plank", 3),
            X("armourer", 0, Any, "bone-studded armour", "#light-armour + 2 #remains"),

            // carpenter
            X("carpenter", 0, Shop, "plank", "log", 0, 2),
            X("carpenter", 0, Shop, "quarterstaff", "log"),
            X("carpenter", 0, Shop, "buckler", "2 plank"),
            X("carpenter", 0, Shop, "large box", "3 plank"),
            X("carpenter", 1, Shop, "chest", "4 plank + iron bar"),

            // bowyer
            X("bowyer", 0, Shop, "sling", "leather + thread"),
            X("bowyer", 0, Shop, "short bow", "log + 2 thread"),
            X("bowyer", 2, Shop, "crossbow", "2 plank + iron bar + 2 thread"),
            X("bowyer", 3, Shop, "elven bow", "2 log + 3 thread + mithril bar"),
            // fletching: shafts and fire-hardened points anywhere, metal heads at the forge (silver arrows for the dead)
            X("bowyer", 0, Any, "arrow", "log + thread", 0, 8),
            Named("bowyer:headed-arrow", X("bowyer", 1, Forge, "arrow", "log + thread", 1, 12)),
            X("bowyer", 1, Forge, "crossbow bolt", "plank", 1, 10),
            X("bowyer", 0, Any, "sling stone", "#rock", 0, 6),

            // leatherworker
            X("leatherworker", 0, Any, "leather", "raw hide"),
            X("leatherworker", 0, Shop, "leather armour", "3 leather + thread"),
            X("leatherworker", 0, Shop, "leather cap", "leather"),
            X("leatherworker", 0, Shop, "leather gloves", "leather"),
            X("leatherworker", 0, Shop, "leather boots", "2 leather"),
            X("leatherworker", 1, Shop, "whip", "2 leather"),
            X("leatherworker", 1, Shop, "oilskin sack", "leather + tallow"),
            X("leatherworker", 3, Shop, "elven leather", "4 leather + 2 thread + mithril bar"),

            // tailor
            X("tailor", 0, Any, "thread", "2 flax"),
            X("tailor", 0, Shop, "cloth", "3 thread"),
            X("tailor", 0, Shop, "cloak", "2 cloth"),
            X("tailor", 0, Shop, "blindfold", "cloth"),
            X("tailor", 0, Any, "bandage", "cloth", 0, 3),
            X("tailor", 3, Shop, "cloak of elvenkind", "4 cloth + 2 thread + #gem"),

            // jeweller (a band or pendant takes a spell from the stone; see Game.Crafting)
            X("jeweller", 1, Forge, "silver band", "silver bar + #gem"),
            X("jeweller", 2, Forge, "pendant", "2 silver bar + #gem"),
            X("jeweller", 0, Forge, "gold locket", "bronze bar"),
            X("jeweller", 2, Forge, "valuable necklace", "silver bar + pearls"),

            // alchemist
            X("alchemist", 0, Shop, "potion of healing", "2 healing herb + glass flask"),
            X("alchemist", 0, Any, "potion of extra healing", "2 potion of healing"),
            X("alchemist", 3, Shop, "potion of full healing", "2 potion of extra healing + swiftroot"),
            X("alchemist", 1, Shop, "potion of speed", "2 swiftroot + glass flask"),
            X("alchemist", 0, Shop, "potion of poison", "nightshade + glass flask"),
            X("alchemist", 1, Shop, "potion of sleeping", "nightshade + healing herb + glass flask"),
            X("alchemist", 0, Shop, "potion of oil", "2 tallow + glass flask"),
            X("alchemist", 2, Shop, "potion of see invisible", "2 swiftroot + nightshade + glass flask"),
            X("alchemist", 0, Any, "molotov", "potion of oil + candle"),

            // scribe
            X("scribe", 0, Shop, "parchment", "raw hide", 0, 2),
            X("scribe", 0, Shop, "ink", "nightshade + tallow + glass flask"),
            X("scribe", 0, Shop, "scroll of identify", "parchment + ink"),
            X("scribe", 1, Shop, "scroll of mapping", "parchment + ink + swiftroot"),
            X("scribe", 2, Shop, "scroll of enchant weapon", "parchment + 2 ink + steel bar"),
            X("scribe", 2, Shop, "scroll of enchant armour", "parchment + 2 ink + leather"),
            X("scribe", 3, Shop, "scroll of teleportation", "2 parchment + 2 ink + swiftroot"),

            // toolmaker
            X("toolmaker", 0, Forge, "pick-axe", "2 iron bar + log"),
            X("toolmaker", 0, Forge, "lock pick", "iron bar"),
            X("toolmaker", 1, Forge, "tinning kit", "iron bar + copper bar"),
            X("toolmaker", 1, Forge, "mirror", "silver bar"),
            X("toolmaker", 0, Any, "candle", "2 tallow + thread"),
            X("toolmaker", 0, Shop, "fishing rod", "log + 2 thread"),

            // luthier
            X("luthier", 0, Shop, "flute", "log"),
            X("luthier", 0, Shop, "drum", "log + leather"),
            X("luthier", 0, Shop, "tambourine", "plank + leather + copper bar"),
            X("luthier", 1, Shop, "lute", "2 plank + 3 thread"),
            X("luthier", 1, Shop, "horn", "2 bronze bar"),
            X("luthier", 2, Shop, "fiddle", "2 plank + 4 thread"),
            X("luthier", 3, Shop, "harp", "3 plank + 6 thread + bronze bar"),

            // cook (any fire will do)
            X("cook", 0, Any, "roast meat", "raw meat"),
            X("cook", 0, Any, "grilled fish", "raw fish"),
            X("cook", 0, Any, "tallow", "raw meat", 0, 2),
            X("cook", 1, Any, "hearty stew", "2 raw meat + carrot"),
            X("cook", 0, Shop, "loaf of bread", "2 barley"),
            X("cook", 1, Shop, "honey cake", "barley + honey"),
            X("cook", 1, Any, "food ration", "2 roast meat + loaf of bread"),

            // brewer
            X("brewer", 0, Shop, "mug of ale", "2 barley", 0, 2),
            X("brewer", 1, Shop, "bottle of mead", "2 honey"),
        };

        public static RecipeDef FindRecipe(string id) { foreach (var r in Recipes) if (r.Id == id) return r; return null; }
    }
}
