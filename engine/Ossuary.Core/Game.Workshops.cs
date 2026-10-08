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
        public JobOffer CommissionOffer(Building b)
        {
            if (b == null) return null;
            var taught = TradesTaughtAt(b.Kind);
            if (taught.Length == 0) return null;
            var pool = new List<RecipeDef>();
            foreach (var r in Trades.Recipes)
                if (Array.IndexOf(taught, r.Trade) >= 0 && r.Rank <= TradeRank(r.Trade) + 1 && Trades.TryDef(r.Product, out var d) && d.Kind != ItemKind.Material) pool.Add(r);
            if (pool.Count == 0) return null;
            ulong h = 1469598103934665603UL;
            string town = Town?.Name ?? "road";
            foreach (char c in town + "|" + b.Kind) { h ^= c; h *= 1099511628211UL; }
            int week = World != null ? World.Day / 7 : 0;
            var rng = new Rng(h ^ (ulong)(week * 7919 + 3) ^ Rng.Seed);
            var pick = pool[rng.Range(0, pool.Count)];
            Trades.TryDef(pick.Product, out var def);
            var offer = new JobOffer { Id = $"guild.make.{town}.{b.Kind}.{week}", Kind = "make", Target = pick.Product, Branch = pick.Trade, Count = 1, Giver = Houses.Guild,
                Reward = Math.Max(15, def.Cost / 2 + 15 + 20 * pick.Rank) };
            return QuestOf(offer.Id) != null ? null : offer;
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
            if (offer != null) add("commission", $"Commission: make {offer.Target} ({offer.Reward} gold)", 0, ActiveJobs().Count < MaxContracts);
            foreach (var q in ActiveJobs())
            {
                var made = q.Def.Steps[0];
                if (made.Kind != ObjKind.Item || Array.IndexOf(TradesTaughtAt(b.Kind), made.Branch) < 0) continue;
                if (q.Current?.Kind != ObjKind.Flag && !HasItem(made.Target)) continue;   // shown once the product is in hand or made
                add("deliver:" + q.Def.Id, $"Deliver: {made.Target} ({q.Def.RewardGold} gold)", 0, HasItem(made.Target));
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
                if (offer != null) AcceptJob(offer);
                return true;
            }
            if (id.StartsWith("deliver:"))
            {
                string qid = id.Substring(8);
                var q = QuestOf(qid);
                if (q == null || q.Status != QStatus.Active) return true;
                string product = q.Def.Steps[0].Target;
                if (!HasItem(product)) { Tell("You do not have it with you.", MessageKind.Warn); return true; }
                QuestCheck();              // the product in the pack meets the first step (no turn may have ended yet)
                TakeItem(product);
                Flags.Add(DeliverFlag(qid));
                QuestCheck();              // pays, Guild +5 and trade xp 5 (OnComplete)
                return true;
            }
            return false;
        }
    }
}

