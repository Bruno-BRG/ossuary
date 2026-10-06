using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// The town's workshops: a master in each teaches its trades up to journeyman for coin (mastery only comes from the
    /// work), and offers one commission a week, a thing to make and bring back for pay and standing.
    /// </summary>
    public sealed partial class Game
    {
        public static string[] TradesTaughtAt(BuildingKind k)
        {
            switch (k)
            {
                case BuildingKind.Smithy: return new[] { "blacksmith", "miner", "toolmaker" };
                case BuildingKind.Armoury: return new[] { "armourer", "leatherworker" };
                case BuildingKind.Alchemist: return new[] { "alchemist" };
                case BuildingKind.General: return new[] { "carpenter", "tailor", "bowyer" };
                case BuildingKind.Tavern: return new[] { "cook", "brewer", "musician", "luthier" };
                case BuildingKind.Inn: return new[] { "cook" };
                case BuildingKind.Emporium: return new[] { "jeweller", "scribe" };
                case BuildingKind.Library: return new[] { "scribe" };
                case BuildingKind.Guild: return new[] { "forager" };
                default: return new string[0];
            }
        }

        public static int LearnPrice(int rank) => 60 * (rank + 1) * (rank + 1);

        /// <summary>This building's commission this week (a function of town, week and building, never stored), or null.</summary>
        public Contract CommissionOffer(Building b)
        {
            if (b == null) return null;
            var taught = TradesTaughtAt(b.Kind);
            if (taught.Length == 0) return null;
            var pool = new List<RecipeDef>();
            foreach (var r in Trades.Recipes)
                if (Array.IndexOf(taught, r.Trade) >= 0 && r.Rank <= TradeRank(r.Trade) + 1 && Trades.TryDef(r.Product, out var d) && d.Kind != ItemKind.Material) pool.Add(r);
            if (pool.Count == 0) return null;
            ulong h = 1469598103934665603UL;
            foreach (char c in (Town?.Name ?? "road") + "|" + b.Kind) { h ^= c; h *= 1099511628211UL; }
            int week = World != null ? World.Day / 7 : 0;
            var rng = new Rng(h ^ (ulong)(week * 7919 + 3) ^ Rng.Seed);
            var pick = pool[rng.Range(0, pool.Count)];
            Trades.TryDef(pick.Product, out var def);
            var offer = new Contract { Kind = "make", Target = pick.Product, Branch = pick.Trade, Count = 1, Giver = Houses.Guild,
                Reward = Math.Max(15, def.Cost / 2 + 15 + 20 * pick.Rank) };
            foreach (var have in Contracts) if (have.Key == offer.Key) return null;
            return offer;
        }

        void AddWorkshopRows(Action<string, string, int, bool> add)
        {
            var b = TalkBuilding;
            if (b == null) return;
            foreach (string id in TradesTaughtAt(b.Kind))
            {
                int rank = TradeRank(id);
                if (rank >= 2) continue;
                add("learn:" + id, $"Learn the {Trades.Find(id).Name}'s craft ({Trades.RankNames[rank + 1]})", LearnPrice(rank), true);
            }
            var offer = CommissionOffer(b);
            if (offer != null) add("commission", $"Commission: make {offer.Target} ({offer.Reward} gold)", 0, Contracts.Count < MaxContracts);
            for (int i = 0; i < Contracts.Count; i++)
            {
                var c = Contracts[i];
                if (c.Kind != "make" || Array.IndexOf(TradesTaughtAt(b.Kind), c.Branch) < 0) continue;
                add("deliver:" + i, $"Deliver: {c.Target} ({c.Reward} gold)", 0, Find(Player, it => it.Def.Name == c.Target) != null);
            }
        }

        /// <summary>Runs a workshop row; false when the id is not one of them. The panel stays open.</summary>
        public bool WorkshopAction(string id)
        {
            if (id.StartsWith("learn:"))
            {
                string trade = id.Substring(6);
                var def = Trades.Find(trade);
                int rank = TradeRank(trade);
                if (def == null || rank >= 2 || !Pay(LearnPrice(rank))) return true;
                GainTrade(trade, Trades.RankXp[rank + 1] - TradeXp(trade));
                Tell("The master shows you the tricks of the trade.", MessageKind.Good);
                return true;
            }
            if (id == "commission")
            {
                var offer = CommissionOffer(TalkBuilding);
                if (offer != null) AcceptContract(offer);
                return true;
            }
            if (id.StartsWith("deliver:") && int.TryParse(id.Substring(8), out int i))
            {
                if (i < 0 || i >= Contracts.Count || Contracts[i].Kind != "make") return true;
                var c = Contracts[i];
                var thing = Find(Player, it => it.Def.Name == c.Target);
                if (thing == null) { Tell("You do not have it with you.", MessageKind.Warn); return true; }
                if (--thing.Quantity <= 0) Player.Inventory.Remove(thing);
                c.Done = c.Count;
                Contracts.RemoveAt(i);
                Player.Gold += c.Reward;
                ContractsDone++;
                AddRep(Houses.Guild, 5, null);
                GainTrade(c.Branch, 5);
                Tell($"Job done: {c.Describe()}. You are paid {c.Reward} gold.", MessageKind.Good);
                return true;
            }
            return false;
        }

        /// <summary>Something just came off the bench: a commission for it can be delivered now.</summary>
        void CommissionMade(Item made)
        {
            foreach (var c in Contracts)
                if (c.Kind == "make" && !c.Complete && c.Target == made.Def.Name) { c.Done = c.Count; Say("That will do for the commission. Bring it to the workshop.", MessageKind.Quest); }
        }
    }
}

