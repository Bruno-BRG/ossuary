using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Crafting for everyone. Recipes are data (<see cref="Trades.Recipes"/>); the craft verb lists what the pack, the
    /// place and the hero's trade ranks allow right now, and Craft builds the chosen one. Any hero learns any trade by
    /// working at it (or from a master in town); ranks unlock harder recipes and finer metals, raise the quality of
    /// gear and stop work from failing.
    /// </summary>
    public sealed partial class Game
    {
        public const string CraftPrompt = "Make what?";
        public const string RecipePrompt = "Every recipe";

        static bool IsRemains(Item it) => it.Def.Kind == ItemKind.Corpse && (it.Def.Name == "remains" || it.Def.Name == "skeleton corpse");

        static Item Find(Player p, Func<Item, bool> match) { foreach (var it in p.Inventory) if (match(it)) return it; return null; }

        static int Count(Player p, Func<Item, bool> match) { int n = 0; foreach (var it in p.Inventory) if (match(it)) n += Math.Max(1, it.Quantity); return n; }

        // ---------------------------------------------------------------- trades and ranks

        public int TradeXp(string trade) => Player.TradeXp.TryGetValue(trade, out int xp) ? xp : 0;
        public int TradeRank(string trade) => Trades.Rank(TradeXp(trade));

        /// <summary>Work in a trade: the xp lands and a new rank is announced.</summary>
        public void GainTrade(string trade, int amount)
        {
            if (amount <= 0 || Trades.Find(trade) == null) return;
            int before = TradeRank(trade);
            Player.TradeXp[trade] = TradeXp(trade) + amount;
            int now = TradeRank(trade);
            if (now > before) Say($"Your craft grows: {Trades.Find(trade).Title} is now {Trades.RankNames[now]}.", MessageKind.Good);
        }

        // ---------------------------------------------------------------- what can be made here

        /// <summary>One way to make a recipe now: the recipe, the metal of the bars (if any) and the stacks it takes.</summary>
        sealed class Plan
        {
            public RecipeDef Recipe;
            public MaterialDef Metal;
        }

        List<Plan> _plans = new List<Plan>();

        bool NearForge() => NearTile(TileKind.Forge);

        /// <summary>A station tile in one of the eight cells around the hero (the forge, a loom, a still).</summary>
        bool NearTile(TileKind k)
        {
            if (Map == null || Mode == GameMode.Overworld) return false;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (Map.InBounds(Player.X + dx, Player.Y + dy) && Map.Get(Player.X + dx, Player.Y + dy) == k) return true;
            return false;
        }

        public bool StationHere(Station s)
        {
            switch (s)
            {
                case Station.Forge: return NearForge();
                case Station.Loom: return NearTile(TileKind.Loom);
                case Station.Still: return NearTile(TileKind.Still);
                case Station.Workshop: return Mode == GameMode.TownMap;
                default: return true;
            }
        }

        static bool Matches(Need n, Item it)
        {
            switch (n.Name)
            {
                case "#remains": return IsRemains(it);
                case "#blade": return it.Def.Kind == ItemKind.Weapon && it.Def.Class == ItemClass.Blade;
                case "#light-armour": return it.Def.Kind == ItemKind.Armor && it.Def.AC <= 3;
                case "#gem": return it.Def.Name == "gemstone" || it.Def.Name == "piece of jade" || it.Def.Kind == ItemKind.Gem;
                case "#rock": return it.Def.Kind == ItemKind.Rock && !Materials.IsOre(it);
                default: return it.Def.Name == n.Name;
            }
        }

        /// <summary>The stacks a recipe would take from the pack, or null when something is missing.</summary>
        List<(Item, int)> Gather(RecipeDef r, MaterialDef metal)
        {
            var parts = new List<(Item, int)>();
            var used = new Dictionary<Item, int>();
            bool Take(Func<Item, bool> match, int count)
            {
                foreach (var it in Player.Inventory)
                {
                    if (count <= 0) break;
                    if (!match(it)) continue;
                    int have = Math.Max(1, it.Quantity) - (used.TryGetValue(it, out int u) ? u : 0);
                    if (have <= 0) continue;
                    int t = Math.Min(have, count);
                    used[it] = (used.TryGetValue(it, out int u2) ? u2 : 0) + t;
                    parts.Add((it, t)); count -= t;
                }
                return count <= 0;
            }
            if (r.Bars > 0 && !Take(i => i.Def.Name == Trades.BarOf(metal), r.Bars)) return null;
            foreach (var n in r.Needs) if (!Take(i => Matches(n, i), n.Count)) return null;
            return parts;
        }

        /// <summary>Every way the hero can make something here and now.</summary>
        List<Plan> Plans()
        {
            var list = new List<Plan>();
            foreach (var r in Trades.Recipes)
            {
                if (TradeRank(r.Trade) < r.Rank || !StationHere(r.Station)) continue;
                if (!Trades.TryDef(r.Product, out var def)) continue;
                if (r.Bars == 0)
                {
                    if (Gather(r, null) != null) list.Add(new Plan { Recipe = r });
                    continue;
                }
                foreach (var m in Materials.All)
                {
                    if (!m.Metal || !Materials.Takes(def, m) || TradeRank(r.Trade) < Math.Max(r.Rank, Trades.MetalRank(m))) continue;
                    if (Gather(r, m) != null) list.Add(new Plan { Recipe = r, Metal = m });
                }
            }
            return list;
        }

        /// <summary>The needs of a recipe as a line the player reads, each name already in their language.</summary>
        public static string NeedsText(RecipeDef r, MaterialDef metal)
        {
            var bits = new List<string>();
            string Word(string name)
            {
                switch (name)
                {
                    case "#remains": return Loc.T("remains");
                    case "#blade": return Loc.T("a blade");
                    case "#light-armour": return Loc.T("light armour");
                    case "#gem": return Loc.T("a gem");
                    case "#rock": return Loc.T("a stone");
                    default: return Loc.T(name);
                }
            }
            if (r.Bars > 0) bits.Add(r.Bars + " x " + (metal != null ? Loc.T(Trades.BarOf(metal)) : Loc.T("metal bar")));
            foreach (var n in r.Needs) bits.Add((n.Count > 1 ? n.Count + " x " : "") + Word(n.Name));
            return string.Join(" + ", bits);
        }

        static string StationWord(Station s) => s == Station.Forge ? "forge" : s == Station.Loom ? "loom" : s == Station.Still ? "still" : s == Station.Workshop ? "town workshop" : "anywhere";

        /// <summary>The finished item, before quality. Previews use it as is, so listing recipes draws no RNG.</summary>
        Item Product(RecipeDef r, MaterialDef metal)
        {
            if (!Trades.TryDef(r.Product, out var def)) return null;
            var it = new Item(def, Rng, NextUid()) { Identified = true, Quantity = r.Qty };
            if (metal != null && Materials.Takes(def, metal)) Materials.Set(it, metal);
            if (def.Name == "bone blade") { it.Rarity = Rarity.Magic; it.Prefix = "vampiric"; }
            if (def.Name == "bone-studded armour") it.Enchant = 1;
            it.Value = it.TradeValue;
            return it;
        }

        /// <summary>Previews of what the pack can make now. Each carries its plan index in <see cref="Item.Uid"/>.</summary>
        public List<Item> CraftChoices()
        {
            _plans = Plans();
            var list = new List<Item>();
            for (int i = 0; i < _plans.Count; i++)
            {
                var pl = _plans[i];
                var preview = Product(pl.Recipe, pl.Metal);
                if (preview == null) continue;
                preview.Uid = -1 - i;
                string qty = pl.Recipe.Qty > 1 ? pl.Recipe.Qty + " x " : "";
                preview.ArtifactName = qty + Loc.T(preview.Name) + "   [" + NeedsText(pl.Recipe, pl.Metal) + "]";
                list.Add(preview);
            }
            return list;
        }

        /// <summary>Every recipe in the game, as read-only rows: trade and rank, product, needs, and where. A dot marks what is out of reach.</summary>
        public List<Item> RecipeBook()
        {
            var list = new List<Item>();
            foreach (var r in Trades.Recipes)
            {
                if (!Trades.TryDef(r.Product, out var def)) continue;
                bool can = TradeRank(r.Trade) >= r.Rank;
                var row = new Item(def, Rng, -1) { Identified = true };
                row.ArtifactName = (can ? "" : "· ") + Loc.T(Trades.Find(r.Trade).Title) + " " + Loc.T(Trades.RankNames[r.Rank]) + ": "
                    + Loc.T(def.Name) + "   [" + NeedsText(r, null) + "]  @ " + Loc.T(StationWord(r.Station));
                list.Add(row);
            }
            return list;
        }

        /// <summary>Chance in a hundred that the work is spoiled: none for simple things, less with every rank above the recipe's.</summary>
        int FailChance(RecipeDef r) => r.Rank == 0 ? 0 : Math.Max(0, 10 * (r.Rank + 1 - TradeRank(r.Trade)));

        /// <summary>Builds the plan a preview stood for: the ingredients go, the product arrives. Costs a turn.</summary>
        public bool Craft(Item preview)
        {
            int index = (int)(-1 - preview.Uid);
            if (index < 0 || index >= _plans.Count) return false;
            var pl = _plans[index];
            var r = pl.Recipe;
            var parts = StationHere(r.Station) ? Gather(r, pl.Metal) : null;
            if (parts == null) { Say("You no longer have what that needs.", MessageKind.Warn); return false; }
            var made = Product(r, pl.Metal);
            if (made == null) return false;
            foreach (var (item, take) in parts)
            {
                item.Quantity -= take;
                if (item.Quantity <= 0) Player.Inventory.Remove(item);
            }
            int xp = 3 + 3 * r.Rank + (pl.Metal != null ? 2 * Trades.MetalRank(pl.Metal) : 0);
            int fail = FailChance(r);
            if (fail > 0 && Rng.Range(0, 100) < fail)
            {
                Say("You botch the work. The materials are ruined.", MessageKind.Warn);
                GainTrade(r.Trade, xp / 2);
            }
            else
            {
                Finish(made, r);
                Pack(made);
                GainTrade(r.Trade, xp);
                if (made.Quantity > 1) Say($"You make {made.Quantity} x {made.Name}.", MessageKind.Good);
                else Say($"You make {made.Name}.", MessageKind.Good);
                if (made.Title != null) NameMasterwork(made);
                CommissionMade(made);
            }
            if (Mode == GameMode.Dungeon || Mode == GameMode.TownMap) EndPlayerTurn();
            else if (Mode == GameMode.Overworld) World?.AdvanceTime(1);
            return true;
        }

        /// <summary>
        /// The hand shows in the work. Gear rolls crude, plain, fine or masterwork by rank (a masterwork weapon carries the
        /// masterwork edge and the maker's name); a ring or pendant draws a spell into its stone; a master makes one more of
        /// anything that comes by the batch.
        /// </summary>
        void Finish(Item it, RecipeDef r)
        {
            int rank = TradeRank(r.Trade);
            it.Maker = Player.CharName;
            if (it.Def.Kind.IsGear() && it.Def.Name != "bone blade" && it.Def.Name != "bone-studded armour")
            {
                int roll = Rng.Range(0, 100) + 12 * (rank - r.Rank) + 6 * rank;
                if (roll < 15) it.Enchant = -1;
                else if (roll >= 110)
                {
                    it.Enchant = 2; it.Rarity = Rarity.Magic;
                    if (it.Def.Kind == ItemKind.Weapon && it.Prefix == null) it.Prefix = "masterwork";
                    it.Engraving = "made by " + Player.CharName;
                    // Now and then a master's best work outgrows the workshop: it gets a name of its own and becomes a relic.
                    if (rank >= 3 && Rng.Range(0, 100) < NamedChance(rank)) { it.Enchant = 3; it.Title = Masterworks.Coin(Rng.Seed, it.Uid); }
                }
                else if (roll >= 85) { it.Enchant = 1; it.Rarity = Rarity.Magic; }
                it.Identified = true;
            }
            else if ((it.Def.Kind == ItemKind.Ring || it.Def.Kind == ItemKind.Amulet) && Catalogue.IsBlank(it.Def.Name))
            {
                ItemRoller.Roll(it, Rng, 2 + 3 * rank);
                it.Identified = true;
            }
            else if (it.Def.Kind == ItemKind.Ammo && rank >= 2 && r.Bars > 0) { it.Enchant = 1; if (rank >= 3) it.Quantity++; }
            else if (r.Qty > 1 && rank >= 3) it.Quantity++;
            it.Value = it.TradeValue;
        }

        /// <summary>Chance in a hundred that a master's masterwork gets a name: one in five for a master, one in three for a grandmaster.</summary>
        public static int NamedChance(int rank) => rank >= 4 ? 33 : rank >= 3 ? 20 : 0;

        // ---------------------------------------------------------------- throwing a molotov

        /// <summary>Lights the cloth and throws. Fire where it lands, and on whatever stands there.</summary>
        void ThrowAt(Item what, int x, int y)
        {
            if (what == null || !Player.Inventory.Contains(what)) { Say("You are not holding that."); return; }
            if (Map == null || !Map.InBounds(x, y)) { Say("You cannot throw it there."); return; }
            int dist = Pathfinder.Chebyshev(Player.X, Player.Y, x, y);
            if (dist > 7) { Say("That is too far to throw.", MessageKind.Info); return; }
            if (!Map.IsVisible(x, y) || !Fov.HasLine(Map, Player.X, Player.Y, x, y)) { Say("You have no clear line there.", MessageKind.Info); return; }
            if (!Tiles.Walkable(Map.Get(x, y))) { Say("It would shatter on stone. Aim at open floor.", MessageKind.Info); return; }

            if (--what.Quantity <= 0) Player.Inventory.Remove(what);
            MakeNoise(2);
            Say("The molotov bursts into flame!", MessageKind.Combat);
            int fromX = Player.X, fromY = Player.Y;
            Fx((tl, s) => { int land = FxLib.Bolt(tl, s, fromX, fromY, x, y, Elem.Fire, 'o', 2); return FxLib.Burst(tl, land, x, y, 1, Elem.Fire); });
            foreach (var d in new[] { (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int cx = x + d.Item1, cy = y + d.Item2;
                if (!Map.InBounds(cx, cy) || !Tiles.Walkable(Map.Get(cx, cy))) continue;
                if (Map.SurfaceAt(cx, cy) == SurfaceKind.Water) continue;
                PutSurface(cx, cy, SurfaceKind.Fire, 5);
            }
            var m = MonsterAt(x, y);
            if (m != null && !m.Ally)
            {
                m.Alert = 1; m.Dormant = false;
                SetAlight(m);
                if (!m.IsDead) ElementalDamage(m, Rng.Range(3, 9), DamageType.Fire);
            }
            Map.Version++;
            EndPlayerTurn();
        }
    }
}

