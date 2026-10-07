using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>What a town's market remembers about the hero: what was sold into it, the counter they rent, the orders filled.</summary>
    public sealed class TownMarket
    {
        /// <summary>Goods class to units sold here lately; it melts away by <see cref="Game.GlutRecovery"/> a day.</summary>
        public readonly Dictionary<string, int> Glut = new Dictionary<string, int>();
        public int GlutDay;
        /// <summary>The hero's counter: paid up to this day (exclusive), the goods on it, the coin waiting and the price they asked.</summary>
        public int RentUntil = -1;
        public readonly List<Item> Counter = new List<Item>();
        public int Takings;
        public int SettledDay;
        public int Tier = 1;
        public readonly HashSet<string> OrdersDone = new HashSet<string>();
        /// <summary>The market-day traders' wares on the stalls, and the two-day bucket they came for.</summary>
        public readonly List<Item> Visitors = new List<Item>();
        public int VisitBucket = -1;
        /// <summary>The week whose caravan news has been folded into the prices.</summary>
        public int CaravanWeek = -1;
    }

    /// <summary>A price the hero saw, for the journal: what a class of goods fetched in a town on a day.</summary>
    public sealed class PriceNote
    {
        public string Town, Class;
        public int Pct, Day;
    }

    /// <summary>One line of a town's weekly order board: somebody wants a thing, or offers one.</summary>
    public sealed class MarketOrder
    {
        public string Id, Item;
        public ItemDef Def;
        public bool Wanted;
        public int Price;
    }

    /// <summary>
    /// The economy of the towns. Prices are a base value moved by the town's tastes, by what the hero has already sold into it
    /// (a glut that recovers over days), by the week's caravan (cheap goods, or a raided road and a shortage), by market day,
    /// by Cha, the trader's mood and the Guild, and by a master's name on the thing. Everything that varies is a pure function
    /// of the seed, the town and the day; only what the hero did is stored. Money also leaves: gate tolls, stall rent, Guild dues.
    /// </summary>
    public sealed partial class Game
    {
        public const int GlutRecovery = 2, GlutStep = 6;
        public const string StallPrompt = "Set out what?";
        static readonly int[] TierChance = { 55, 30, 10 }, TierPct = { 80, 100, 140 };
        public static readonly string[] TierNames = { "cheap", "fair", "dear" };

        readonly Dictionary<string, TownMarket> _markets = new Dictionary<string, TownMarket>();
        public readonly List<PriceNote> PriceHistory = new List<PriceNote>();
        /// <summary>Guild dues are paid up to this day (exclusive): members sell for more.</summary>
        public int DuesUntil = -1;
        public int TollsPaid, RentPaid;

        int MarketDay => World != null ? World.Day : 0;

        public TownMarket Market => Town == null ? null : MarketOf(Town.Name);

        public TownMarket MarketOf(string town)
        {
            if (!_markets.TryGetValue(town, out var m)) { m = new TownMarket { GlutDay = MarketDay, SettledDay = MarketDay }; _markets[town] = m; }
            return m;
        }

        // ------------------------------------------------------------ goods and values

        /// <summary>The class a thing trades as: a town likes or gluts on classes, not on single items.</summary>
        public static string GoodsClass(ItemDef d)
        {
            switch (d.Kind)
            {
                case ItemKind.Weapon: return "weapons";
                case ItemKind.Armor: case ItemKind.Shield: case ItemKind.Helm: case ItemKind.Gloves: case ItemKind.Boots: case ItemKind.Cloak: return "armour";
                case ItemKind.Potion: return "potions";
                case ItemKind.Scroll: case ItemKind.Book: return "books and scrolls";
                case ItemKind.Wand: return "wands";
                case ItemKind.Ring: case ItemKind.Amulet: case ItemKind.Gem: case ItemKind.Ornament: case ItemKind.Statuette: return "jewellery";
                case ItemKind.Food: case ItemKind.Corpse: return "food";
                case ItemKind.Material: case ItemKind.Rock: return "raw goods";
                default: return "tools";
            }
        }

        public static readonly string[] Classes = { "weapons", "armour", "potions", "books and scrolls", "wands", "jewellery", "food", "raw goods", "tools" };

        /// <summary>Goods and food come in stacks and are priced by the stack, so ten logs are not sold for the price of one.</summary>
        public static int StackValue(Item it)
        {
            int v = Math.Max(1, it.TradeValue);
            var k = it.Def.Kind;
            if (k == ItemKind.Material || k == ItemKind.Food || k == ItemKind.Rock) v *= Math.Max(1, it.Quantity);
            return v;
        }

        /// <summary>The hero's own work, signed, from a hand that has reached master in some trade: it sells on the name.</summary>
        public bool MastersWork(Item it)
        {
            if (it.Engraving == null || !it.Engraving.StartsWith("made by ")) return false;
            foreach (var kv in Player.TradeXp) if (Trades.Rank(kv.Value) >= 3) return true;
            return false;
        }

        ulong TownSalt(string town) { ulong h = 14695981039346656037UL; foreach (char c in town ?? "") { h ^= c; h *= 1099511628211UL; } return h; }

        /// <summary>A town's lasting taste for a class, -15..+15 percent: a mining town pays little for ore and much for bread.</summary>
        public int TownTaste(string town, string cls) => (int)(Rumours.Hash(Rng.Seed ^ 0x7A57EUL, town + "|" + cls, 0) % 31) - 15;

        /// <summary>The trader's mood today, -8..+8 percent in the hero's favour.</summary>
        public int TraderMood(Shop shop) => shop == null ? 0 : (int)(Rumours.Hash(Rng.Seed ^ 0x300DUL, shop.Name, MarketDay) % 17) - 8;

        public static string MoodWord(int mood) => mood >= 5 ? "cheerful" : mood <= -5 ? "sour" : "even";

        /// <summary>Cha above or below 10 moves every price a point per point, up to ten.</summary>
        int ChaPct => Math.Max(-10, Math.Min(10, Player.Cha - 10));

        public int GlutOf(string town, string cls)
        {
            var m = MarketOf(town);
            int today = MarketDay;
            if (today > m.GlutDay)
            {
                int melt = (today - m.GlutDay) * GlutRecovery;
                foreach (var k in new List<string>(m.Glut.Keys)) m.Glut[k] = Math.Max(0, m.Glut[k] - melt);
                m.GlutDay = today;
            }
            return m.Glut.TryGetValue(cls, out int g) ? g : 0;
        }

        void AddGlut(string town, string cls, Item it)
        {
            GlutOf(town, cls);
            var m = MarketOf(town);
            int units = it.Def.Kind == ItemKind.Material || it.Def.Kind == ItemKind.Food || it.Def.Kind == ItemKind.Rock ? 1 + it.Quantity / 5 : 1;
            m.Glut[cls] = Math.Min(12, (m.Glut.TryGetValue(cls, out int g) ? g : 0) + units);
        }

        // ------------------------------------------------------------ caravans

        public enum CaravanNews { None, Arrived, Raided }

        /// <summary>
        /// This week's road: a caravan came in with cheap goods of one class, or the road was raided and that class is short.
        /// The more dangerous the region, the likelier a raid. A pure function of the seed, the town and the week.
        /// </summary>
        public CaravanNews CaravanThisWeek(out string cls)
        {
            cls = null;
            if (Town == null || World == null) return CaravanNews.None;
            int week = MarketDay / 7;
            uint h = Rumours.Hash(Rng.Seed ^ 0xCA7A7UL, Town.Name, week);
            cls = Classes[(h >> 8) % (uint)Classes.Length];
            int danger = World.RegionAt(World.PlayerX, World.PlayerY).Depth;
            int raid = Math.Min(45, 12 + danger * 2);
            int roll = (int)(h % 100);
            if (roll < raid) return CaravanNews.Raided;
            if (roll < raid + 45) return CaravanNews.Arrived;
            cls = null;
            return CaravanNews.None;
        }

        /// <summary>A caravan that comes in buys up the town's surplus of what it carries: prices travel with it.</summary>
        void FoldCaravan()
        {
            var m = Market;
            if (m == null) return;
            int week = MarketDay / 7;
            if (m.CaravanWeek == week) return;
            m.CaravanWeek = week;
            if (CaravanThisWeek(out string cls) == CaravanNews.Arrived) m.Glut.Remove(cls);
        }

        public static string CaravanLine(CaravanNews n, string cls)
        {
            if (n == CaravanNews.Arrived) return $"A caravan is in from the road: {cls} are cheap this week.";
            if (n == CaravanNews.Raided) return $"The road was raided and the carts never came: {cls} are short this week.";
            return "";
        }

        // ------------------------------------------------------------ prices

        bool AtStall(Shop shop) { if (Town == null || shop == null) return false; foreach (var b in Town.Buildings) if (b.Shop == shop) return b.Kind == BuildingKind.Stall; return false; }

        /// <summary>What a trader here asks of the hero for a class, in percent of its value.</summary>
        public int BuyPct(Shop shop, string cls)
        {
            if (Town == null) return 100;
            int pct = 100 + TownTaste(Town.Name, cls) - Math.Min(20, GlutOf(Town.Name, cls) * 2) - ChaPct - TraderMood(shop);
            var news = CaravanThisWeek(out string c);
            if (c == cls) pct += news == CaravanNews.Raided ? 40 : news == CaravanNews.Arrived ? -15 : 0;
            return Math.Max(50, pct);
        }

        /// <summary>What a trader here pays the hero for a class, in percent of half its value.</summary>
        public int SellPct(Shop shop, string cls)
        {
            if (Town == null) return 100;
            int pct = 100 + TownTaste(Town.Name, cls) - GlutOf(Town.Name, cls) * GlutStep + ChaPct + TraderMood(shop);
            var news = CaravanThisWeek(out string c);
            if (c == cls) pct += news == CaravanNews.Raided ? 25 : news == CaravanNews.Arrived ? -10 : 0;
            if (TownEventToday() == TownEventKind.Market && AtStall(shop)) pct += 15;
            if (DuesUntil > MarketDay) pct += 10;
            pct = pct * (100 + RepOf(Houses.Guild) / 5) / 100;
            return Math.Max(25, Math.Min(200, pct));
        }

        public int SellPrice(Shop shop, Item it)
        {
            if (it.Def.Kind == ItemKind.Gold) return it.Quantity;
            int v = StackValue(it) / 2 * SellPct(shop, GoodsClass(it.Def)) / 100;
            if (MastersWork(it)) v = v * 125 / 100;
            return Math.Max(1, v);
        }

        void NotePrice(string cls, int pct)
        {
            if (Town == null) return;
            PriceHistory.RemoveAll(n => n.Town == Town.Name && n.Class == cls);
            PriceHistory.Add(new PriceNote { Town = Town.Name, Class = cls, Pct = pct, Day = MarketDay });
            if (PriceHistory.Count > 24) PriceHistory.RemoveAt(0);
        }

        /// <summary>Stepping up to a counter shows what this trader pays for the classes on their shelves.</summary>
        void NoteShopPrices(Shop shop)
        {
            var seen = new HashSet<string>();
            foreach (var it in shop.Stock) { string c = GoodsClass(it.Def); if (seen.Add(c)) NotePrice(c, SellPct(shop, c)); }
        }

        // ------------------------------------------------------------ the market square

        bool IsMarketStall(Building b) => b != null && b.Kind == BuildingKind.Stall && (b.Services & Service.Market) != 0;

        /// <summary>On market days traders from elsewhere lay out their wares on the stalls; they pack them up when it ends.</summary>
        void RefreshStall(Shop shop)
        {
            var m = Market;
            if (m == null || !AtStall(shop) || World == null) return;
            int bucket = MarketDay / 2;
            if (m.VisitBucket != bucket)
            {
                foreach (var s in Town.Shops) s.Stock.RemoveAll(it => m.Visitors.Contains(it));
                m.Visitors.Clear();
                m.VisitBucket = bucket;
                if (TownEventToday() == TownEventKind.Market)
                {
                    int depth = Math.Max(1, World.RegionAt(World.PlayerX, World.PlayerY).Depth / 3);
                    foreach (var s in Town.Shops)
                    {
                        if (!AtStall(s)) continue;
                        var rng = new Rng(Rng.Seed ^ TownSalt(Town.Name + "|" + s.Name) ^ (ulong)(bucket * 92821 + 7));
                        for (int i = 0; i < 3; i++)
                        {
                            var it = LevelBuilder.RollLoot(rng, depth + 1);
                            if (it.Def.Kind == ItemKind.Gold) continue;
                            it.Identified = true;
                            s.Stock.Add(it); m.Visitors.Add(it);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------ the order board

        /// <summary>This week's board in this town: two things wanted (often a craft), one thing offered cheap. Never stored.</summary>
        public List<MarketOrder> OrdersThisWeek()
        {
            var list = new List<MarketOrder>();
            if (Town == null || World == null) return list;
            int week = MarketDay / 7;
            var rng = new Rng(Rng.Seed ^ TownSalt(Town.Name + "|orders") ^ (ulong)(week * 7907 + 11));
            var m = Market;
            for (int i = 0; i < 2; i++)
            {
                string name; int value; ItemDef def;
                if (rng.Range(0, 100) < 60)
                {
                    var r = Trades.Recipes[rng.Range(0, Trades.Recipes.Length)];
                    if (!Trades.TryDef(r.Product, out var d) || d.Kind == ItemKind.Material) continue;
                    name = d.Name; def = d; value = Math.Max(10, d.Cost) + 15 * r.Rank;
                }
                else
                {
                    var it = LevelBuilder.RollLoot(rng, 2);
                    if (it.Def.Kind == ItemKind.Gold || it.Def.Kind == ItemKind.Corpse) continue;
                    name = it.Def.Name; def = it.Def; value = Math.Max(10, it.Def.Cost);
                }
                string id = $"{Town.Name}|{week}|w{i}";
                if (m.OrdersDone.Contains(id)) continue;
                list.Add(new MarketOrder { Id = id, Item = name, Def = def, Wanted = true, Price = Math.Max(15, value * 3 / 2) });
            }
            {
                var it = LevelBuilder.RollLoot(rng, 3);
                string id = $"{Town.Name}|{week}|o";
                if (it.Def.Kind != ItemKind.Gold && it.Def.Kind != ItemKind.Corpse && !m.OrdersDone.Contains(id))
                    list.Add(new MarketOrder { Id = id, Item = it.Def.Name, Def = it.Def, Wanted = false, Price = Math.Max(5, Haggle(Math.Max(10, it.Def.Cost) * 60 / 100, Houses.Guild)) });
            }
            return list;
        }

        // ------------------------------------------------------------ the hero's own counter

        public int RentPrice => Town == null ? 0 : Town.Size == "city" ? 45 : Town.Size == "town" ? 30 : Town.Size == "village" ? 20 : 12;

        public bool Renting => Market != null && Market.RentUntil > MarketDay;

        /// <summary>The days since the last look are counted out: each day each thing on the counter may sell, by the price asked.</summary>
        public void SettleStall()
        {
            var m = Market;
            if (m == null) return;
            int last = Math.Min(MarketDay, m.RentUntil);
            for (int d = m.SettledDay + 1; d <= last; d++)
                for (int i = m.Counter.Count - 1; i >= 0; i--)
                {
                    var it = m.Counter[i];
                    int chance = TierChance[m.Tier] + (TownEventKind.Market == TownEventOn(d) ? 15 : 0);
                    if (Rumours.Hash(Rng.Seed ^ 0x57A11UL, Town.Name + "|" + it.Uid, d) % 100 >= (uint)chance) continue;
                    string cls = GoodsClass(it.Def);
                    int pct = TierPct[m.Tier] + TownTaste(Town.Name, cls);
                    int coin = Math.Max(1, StackValue(it) * pct / 100);
                    if (MastersWork(it)) coin = coin * 125 / 100;
                    m.Takings += coin;
                    m.Counter.RemoveAt(i);
                    AddGlut(Town.Name, cls, it);
                    NotePrice(cls, pct * 2);
                }
            m.SettledDay = Math.Max(m.SettledDay, MarketDay);
        }

        /// <summary>The town's event on any day (the same schedule as <see cref="TownEventToday"/>).</summary>
        TownEventKind TownEventOn(int day)
        {
            if (Town == null) return TownEventKind.None;
            uint h = Rumours.Hash(Rng.Seed ^ 0xE7E47UL, Town.Name, day / 2);
            if (h % 100 >= 45) return TownEventKind.None;
            return (TownEventKind)(1 + (int)((h >> 8) % 5));
        }

        public void StallPut(Item it)
        {
            var m = Market;
            if (m == null || !Renting) { Tell("You have no counter here.", MessageKind.Warn); return; }
            if (it.Def.Kind == ItemKind.Gold || Player.IsWorn(it) || Player.Wielded == it) { Tell("That is not for sale.", MessageKind.Warn); return; }
            if (m.Counter.Count >= 8) { Tell("Your counter is full.", MessageKind.Warn); return; }
            Player.Inventory.Remove(it);
            m.Counter.Add(it);
            Tell($"You set out {it.Name}.", MessageKind.Info);
        }

        public int StallAsk(Item it) { var m = Market; return m == null ? 0 : Math.Max(1, StackValue(it) * (TierPct[m.Tier] + TownTaste(Town.Name, GoodsClass(it.Def))) / 100); }

        // ------------------------------------------------------------ service rows

        void AddMarketRows(Action<string, string, int, bool> add)
        {
            var b = TalkBuilding;
            if (!IsMarketStall(b)) return;
            SettleStall();
            var m = Market;
            foreach (var o in OrdersThisWeek())
            {
                if (o.Wanted) add("order:" + o.Id, $"Wanted: {o.Item} (pays {o.Price} gold)", 0, Find(Player, it => it.Def.Name == o.Item) != null);
                else add("order:" + o.Id, $"Offered: {o.Item}", o.Price, true);
            }
            if (!Renting) add("rent", $"Rent a counter for a week", RentPrice, true);
            else
            {
                add("stall-put", $"Set out goods ({m.Counter.Count}/8, {m.RentUntil - MarketDay} days left)", 0, Player.Inventory.Count > 0 && m.Counter.Count < 8);
                add("stall-tier", $"Asking price: {TierNames[m.Tier]}", 0, true);
            }
            if (m.Takings > 0) add("stall-collect", $"Collect your takings ({m.Takings} gold)", 0, true);
            if (m.Counter.Count > 0) add("stall-back", $"Take back your goods ({m.Counter.Count})", 0, true);
        }

        /// <summary>Runs a market row; false when the id is none of ours. The panel stays open.</summary>
        public bool MarketAction(string id)
        {
            var m = Market;
            if (m == null) return false;
            if (id.StartsWith("order:"))
            {
                string oid = id.Substring(6);
                var o = OrdersThisWeek().Find(x => x.Id == oid);
                if (o == null) return true;
                if (o.Wanted)
                {
                    var thing = Find(Player, it => it.Def.Name == o.Item);
                    if (thing == null) { Tell("You do not have it with you.", MessageKind.Warn); return true; }
                    if (--thing.Quantity <= 0) Player.Inventory.Remove(thing);
                    Player.Gold += o.Price;
                    AddRep(Houses.Guild, 2, null);
                    Tell($"You hand over the {o.Item} and are paid {o.Price} gold.", MessageKind.Good);
                }
                else
                {
                    if (!Pay(o.Price)) return true;
                    Player.Inventory.Add(new Item(o.Def, Rng, NextUid()) { Identified = true });
                    Tell($"You buy the {o.Item} for {o.Price} gold.", MessageKind.Good);
                }
                m.OrdersDone.Add(oid);
                return true;
            }
            switch (id)
            {
                case "rent":
                    if (Renting || !Pay(RentPrice)) return true;
                    RentPaid += RentPrice;
                    SettleStall();
                    m.RentUntil = MarketDay + 7;
                    m.SettledDay = MarketDay;
                    Tell("The counter is yours for a week. Set out your goods and come back for the coin.", MessageKind.Good);
                    return true;
                case "stall-put":
                    PushChoice(StallPrompt, Player.Inventory.FindAll(it => it.Def.Kind != ItemKind.Gold));
                    UiState.ChoiceIndex = 0;
                    return true;
                case "stall-tier":
                    SettleStall();
                    m.Tier = (m.Tier + 1) % 3;
                    Tell($"You will ask a {TierNames[m.Tier]} price.", MessageKind.Info);
                    return true;
                case "stall-collect":
                    Player.Gold += m.Takings;
                    Tell($"You collect {m.Takings} gold from your counter.", MessageKind.Good);
                    m.Takings = 0;
                    return true;
                case "stall-back":
                    foreach (var it in m.Counter) Player.Inventory.Add(it);
                    Tell($"You pack up {m.Counter.Count} unsold things.", MessageKind.Info);
                    m.Counter.Clear();
                    return true;
                case "dues":
                    int price = DuesPrice;
                    if (DuesUntil > MarketDay || !Pay(price)) return true;
                    DuesUntil = MarketDay + 7;
                    AddRep(Houses.Guild, 1, null);
                    Tell("Your dues are paid: the Guild's traders pay its members better this week.", MessageKind.Good);
                    return true;
            }
            return false;
        }

        public int DuesPrice => 20 + Player.Level * 3;

        // ------------------------------------------------------------ tolls

        /// <summary>A town or a city takes a toll at the gate; the Watch's friends walk in free. Money has to leave somewhere.</summary>
        void GateToll()
        {
            if (Town == null) return;
            int toll = Town.Size == "city" ? 5 : Town.Size == "town" ? 2 : 0;
            if (toll == 0 || RepOf(Houses.Watch) >= 25) return;
            if (Player.Gold < toll) { Say("The gate guard looks at your empty purse and waves you through.", MessageKind.Info); return; }
            Player.Gold -= toll;
            TollsPaid += toll;
            Say($"You pay the gate toll: {toll} gold.", MessageKind.Info);
        }

        /// <summary>Entering a town: the toll, then the road's news for the week.</summary>
        void ArriveAtMarket()
        {
            GateToll();
            FoldCaravan();
            SettleStall();
            var news = CaravanThisWeek(out string cls);
            if (news != CaravanNews.None) Say(CaravanLine(news, cls), MessageKind.Info);
            var m = Market;
            if (m.Takings > 0) Say($"Word reaches you: your counter has {m.Takings} gold waiting.", MessageKind.Good);
        }
    }
}
