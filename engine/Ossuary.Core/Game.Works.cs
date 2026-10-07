using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>A named masterwork the hero sold: where, when, and who bought it off the shelf afterwards.</summary>
    public sealed class SoldWork
    {
        public string Title, Name, Town, Shop, Buyer;
        public long Uid;
        public int Day;
    }

    /// <summary>
    /// The hero's named works out in the world. A sold one sits on the shelf for a few days, then a townsperson buys it and carries
    /// it (look at them and you see it); the taverns talk about the maker and where the work went.
    /// </summary>
    public sealed partial class Game
    {
        public readonly List<SoldWork> SoldWorks = new List<SoldWork>();

        /// <summary>Days a sold named work sits on the shelf before someone in town buys it.</summary>
        public const int WorkShelfDays = 3;

        void WorkSold(Item it, Shop shop)
        {
            if (it?.Title == null || Town == null) return;
            SoldWorks.Add(new SoldWork { Title = it.Title, Name = it.Name, Town = Town.Name, Shop = shop?.Name, Uid = it.Uid, Day = MarketDay });
        }

        /// <summary>Entering a town: named works that waited long enough on a shelf go home with somebody.</summary>
        public void WorksFindBuyers()
        {
            if (Town == null) return;
            foreach (var w in SoldWorks)
            {
                if (w.Buyer != null || w.Town != Town.Name || MarketDay < w.Day + WorkShelfDays) continue;
                Item piece = null; Shop at = null;
                foreach (var s in Town.Shops) { piece = s.Stock.Find(i => i.Uid == w.Uid); if (piece != null) { at = s; break; } }
                if (piece == null) continue;   // the hero bought it back, or it went elsewhere
                var people = Town.Npcs.FindAll(n => !n.IsGuard && n.Role != TownRole.Pet && n.Role != TownRole.Beggar && !n.IsDead);
                if (people.Count == 0) continue;
                var buyer = people[(int)(Rumours.Hash(Rng.Seed, w.Title + "|buyer", w.Day) % (uint)people.Count)];
                at.Stock.Remove(piece);
                piece.AddOwner(TownText.L($"{at.Name}, who sold it to {buyer.Name}", $"{at.Name}, que o vendeu a {buyer.Name}"));
                buyer.Inventory.Add(piece);
                w.Buyer = buyer.Name;
            }
        }

        /// <summary>What the town says about the hero's named works, or null when there is nothing to say.</summary>
        string WorkRumour(int n)
        {
            string hero = Player.CharName;
            var sold = SoldWorks.FindAll(w => w.Buyer != null);
            if (sold.Count > 0 && n % 2 == 0)
            {
                var w = sold[n / 2 % sold.Count];
                return TownText.L($"They say {w.Buyer} of {w.Town} carries {w.Title}, made by {hero}, and will not put it down.",
                                  $"Dizem que {w.Buyer}, de {w.Town}, carrega {w.Title}, feito por {hero}, e não larga por nada.");
            }
            var shelved = SoldWorks.FindAll(w => w.Buyer == null);
            if (shelved.Count > 0)
            {
                var w = shelved[n % shelved.Count];
                return TownText.L($"They say {w.Title}, made by {hero}, is for sale in {w.Town}. Smiths go to look at it.",
                                  $"Dizem que {w.Title}, feito por {hero}, está à venda em {w.Town}. Ferreiros vão lá só para olhar.");
            }
            if (Player.Works.Count > 0)
            {
                string title = Player.Works[n % Player.Works.Count];
                int comma = title.IndexOf(", ", StringComparison.Ordinal);
                if (comma > 0) title = title.Substring(0, comma);
                return TownText.L($"They say {hero} forged a piece called {title}, and that it has a will of its own.",
                                  $"Dizem que {hero} forjou uma peça chamada {title}, e que ela tem vontade própria.");
            }
            return null;
        }

        /// <summary>A townsperson who carries a named work shows it.</summary>
        string CarriedWork(Monster m)
        {
            if (m?.Inventory == null) return null;
            foreach (var it in m.Inventory)
                if (it.Title != null) return $"{m.Name} carries {it.Title}, made by {it.Maker}.";
            return null;
        }
    }
}
