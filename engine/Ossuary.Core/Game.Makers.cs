using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Crafters elsewhere: the town's own hands keep working while the hero is away. Each week the smith, the armourer, the general
    /// store's tailor and fletcher and the alchemist set out something new under their name, and on market days the rival party
    /// sells what it made and what it brought up from below. Every piece is a pure function of seed, town, shop and week.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Local work a shop keeps on its shelves at most; the oldest unsold piece goes to a cousin in the next village.</summary>
        const int LocalShelf = 4;

        void LocalCraftsmen(Shop shop)
        {
            if (Town == null || World == null || shop == null) return;
            int week = MarketDay / 7;
            if (shop.LocalWeek == week) return;
            bool first = shop.LocalWeek < 0;
            shop.LocalWeek = week;
            if (first) return;   // the shelves were stocked when the town was built
            var rng = new Rng(Rng.Seed ^ TownSalt(Town.Name + "|" + shop.Name + "|work") ^ (ulong)(week * 4099 + 17));
            Item made = null;
            switch (shop.Kind)
            {
                case ShopKind.Weapon: made = TownGen.LocalWork(shop, rng, Catalogue.Weapons); break;
                case ShopKind.Armor: made = TownGen.LocalWork(shop, rng, Catalogue.Armor); break;
                case ShopKind.General:
                    {
                        shop.Maker ??= TownText.GivenName(rng);
                        string[] goods = { "cloak", "arrow", "cloth", "candle", "leather boots" };
                        if (Trades.TryDef(goods[rng.Range(0, goods.Length)], out var d))
                        {
                            made = new Item(d, rng, NextUid()) { Identified = true, Maker = shop.Maker, Quantity = d.Kind == ItemKind.Ammo ? 12 : 1 };
                            shop.Stock.Add(made);
                        }
                        break;
                    }
                case ShopKind.potion:
                    {
                        shop.Maker ??= TownText.GivenName(rng);
                        string[] brews = { "potion of healing", "potion of healing", "potion of speed", "potion of see invisible" };
                        if (Trades.TryDef(brews[rng.Range(0, brews.Length)], out var d))
                        {
                            made = new Item(d, rng, NextUid()) { Identified = true, Maker = shop.Maker };
                            shop.Stock.Add(made);
                        }
                        break;
                    }
            }
            if (made == null) return;
            var mine = shop.Stock.FindAll(i => i.Maker != null && i.Maker == shop.Maker);
            if (mine.Count > LocalShelf) shop.Stock.Remove(mine[0]);
            Say($"{shop.Maker} has set out new work this week.", MessageKind.Info);
        }

        /// <summary>On market days the rival party takes a stall: arrows they fletched on the road, and something they brought up from below.</summary>
        void RivalStall(Shop stall, int bucket)
        {
            int depth = RivalDepthOn(Today);
            if (depth <= 0 || stall == null) return;
            var rng = new Rng(Rng.Seed ^ TownSalt(Town.Name + "|rival") ^ (ulong)(bucket * 6151 + 3));
            string party = RivalName;
            if (Trades.TryDef("arrow", out var arrow))
            {
                var bundle = new Item(arrow, rng, NextUid()) { Identified = true, Maker = party, Quantity = 10 };
                stall.Stock.Add(bundle); Market.Visitors.Add(bundle);
            }
            for (int tries = 0; tries < 6; tries++)
            {
                var it = LevelBuilder.RollLoot(rng, depth + 1, "The Dungeons");
                if (!it.Def.Kind.IsGear()) continue;
                it.Identified = true;
                HandedDown(it, $"{party}, who brought it up from The Dungeons {depth}", $"{Loc.PtOf(party)}, que o trouxeram das Masmorras {depth}");
                stall.Stock.Add(it); Market.Visitors.Add(it);
                break;
            }
        }
    }
}
