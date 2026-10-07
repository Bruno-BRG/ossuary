using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// The haggle verb and the trader's memory. At a counter the hero offers 90, 75 or 60 percent for the thing under the cursor;
    /// Cha, the trader's mood, the Guild and what this trader remembers decide the answer. A refused offer sours the trader for
    /// the rest of the day. What a trader remembers is read from the ledger: being flooded with one kind of goods, being sold a
    /// cursed or ruined thing, and good deals struck; it colours their greeting and their mood.
    /// </summary>
    public sealed partial class Game
    {
        public const string HagglePrompt = "Offer how much?";
        public const string Cheated = "cheated", Flooded = "flooded", Haggled = "haggled", Soured = "soured";
        static readonly int[] OfferPct = { 90, 75, 60 };
        static readonly int[] OfferOdds = { 70, 45, 20 };

        Item _haggleFor;

        // ---------------------------------------------------------------- memory

        int Remembers(Shop shop, string kind, int days = 10000)
        {
            if (shop == null) return 0;
            int n = 0, today = MarketDay;
            foreach (var d in Ledger) if (d.Kind == kind && d.Subject == shop.Name && today - d.Day <= days) n++;
            return n;
        }

        public bool SourToday(Shop shop)
        {
            if (shop == null) return false;
            foreach (var d in Ledger) if (d.Kind == Soured && d.Subject == shop.Name && d.Day == MarketDay) return true;
            return false;
        }

        /// <summary>The trader's mood from what they remember: soured today, cheated this month, flooded this week, or pleased.</summary>
        public int MemoryMood(Shop shop)
        {
            if (shop == null) return 0;
            int m = 0;
            if (SourToday(shop)) m -= 8;
            m -= 4 * Math.Min(2, Remembers(shop, Cheated, 30));
            m -= 2 * Math.Min(2, Remembers(shop, Flooded, 7));
            m += Math.Min(3, Remembers(shop, Haggled) / 2);
            return m;
        }

        /// <summary>What the trader says when the hero steps up, read from the ledger; null when they have nothing to hold against you.</summary>
        public string TraderGreeting(Shop shop)
        {
            if (shop == null) return null;
            string who = shop.Keeper?.Name ?? "The trader";
            if (SourToday(shop)) return $"{who} folds their arms. \"You again. Buy, or go.\"";
            if (Remembers(shop, Cheated) > 0) return $"{who} looks at you hard. \"The last thing you sold me was rotten. I have not forgotten.\"";
            if (Remembers(shop, Flooded, 7) > 0) return $"{who} sighs. \"Not more of the same, I hope. I am still selling the last lot.\"";
            if (Remembers(shop, Haggled) >= 2) return $"{who} grins. \"My favourite haggler. Go easy on me today.\"";
            return null;
        }

        /// <summary>A sale the trader will remember: one class too many this week, or a thing that turns out cursed or ruined.</summary>
        void TraderNotes(Shop shop, Item sold, string cls)
        {
            if (shop == null || Town == null) return;
            if (GlutOf(Town.Name, cls) >= 6 && Remembers(shop, Flooded, 0) == 0) RecordDeed(Flooded, shop.Name, 1);
            if ((sold.Def.Flags & ItemFlags.Cursed) != 0 || sold.Condition >= 2) RecordDeed(Cheated, shop.Name, 2);
        }

        // ---------------------------------------------------------------- haggling

        /// <summary>Opens the offers for the thing under the shop cursor.</summary>
        public void BeginHaggle()
        {
            var shop = CurrentShop;
            if (shop == null || !InShop || shop.Stock.Count == 0) { Say("There is nothing here to haggle over."); return; }
            if (SourToday(shop)) { Say(TraderGreeting(shop), MessageKind.Warn); return; }
            var item = shop.Stock[Math.Max(0, Math.Min(UiState.ShopIndex, shop.Stock.Count - 1))];
            if (item.Def.Kind == ItemKind.Gold) { Say("Gold is not haggled over."); return; }
            _haggleFor = item;
            int full = ShopPrice(shop, item);
            var rows = new List<Item>();
            for (int i = 0; i < OfferPct.Length; i++)
            {
                var row = new Item(item.Def, Rng, -1 - i) { Identified = true };
                row.ArtifactName = $"Offer {OfferPct[i]}%: {Math.Max(1, full * OfferPct[i] / 100)} gold for {item.Name}";
                rows.Add(row);
            }
            PushChoice(HagglePrompt, rows);
            UiState.ChoiceIndex = 0;
        }

        /// <summary>Chance in a hundred that this offer is taken.</summary>
        public int HaggleOdds(Shop shop, int offer)
        {
            int odds = OfferOdds[offer] + 3 * ChaPct + 2 * TraderMood(shop) + RepOf(Houses.Guild) / 10;
            return Math.Max(5, Math.Min(95, odds));
        }

        public bool ResolveHaggle(Item row)
        {
            var shop = CurrentShop;
            var item = _haggleFor;
            _haggleFor = null;
            if (shop == null || item == null || !shop.Stock.Contains(item) || row == null) return false;
            int offer = (int)(-1 - row.Uid);
            if (offer < 0 || offer >= OfferPct.Length) return false;
            int price = Math.Max(1, ShopPrice(shop, item) * OfferPct[offer] / 100);
            if (Player.Gold < price) { Say("You cannot afford even that."); return false; }
            string who = shop.Keeper?.Name ?? "The trader";
            if (Rng.Range(0, 100) < HaggleOdds(shop, offer))
            {
                Player.Gold -= price;
                shop.Gold += price;
                shop.Stock.Remove(item);
                Pack(item);
                RecordDeed(Haggled, shop.Name, 1);
                Say($"{who} takes {price} gold for {item.Name}. \"Robbery. Go on.\"", MessageKind.Good);
                if (UiState.ShopIndex >= shop.Stock.Count) UiState.ShopIndex = Math.Max(0, shop.Stock.Count - 1);
                return true;
            }
            RecordDeed(Soured, shop.Name, 1);
            Say($"{who} goes red. \"Is that what you think my work is worth?\" They will not bargain with you again today.", MessageKind.Warn);
            return false;
        }
    }
}
