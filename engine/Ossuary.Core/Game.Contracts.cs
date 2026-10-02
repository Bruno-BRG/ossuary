using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>A job from the notice board: hunt a kind of monster in a branch, or reach a depth in it.</summary>
    public sealed class Contract
    {
        /// <summary>"hunt" or "delve".</summary>
        public string Kind, Branch, Target, Giver;
        public int Count, Done, Reward;
        public bool Complete => Done >= Count;

        public string Describe() => Kind == "hunt"
            ? $"Hunt {Count} {Target} in {Branch}"
            : $"Reach depth {Count} of {Branch}";
        public string Key => Kind + "|" + Branch + "|" + Target + "|" + Count + "|" + Giver;
    }

    /// <summary>
    /// The Guild's notice board. Offers are a function of the town and the week (so they are the same every time you ask) and
    /// are never stored; only the contracts you take are. Progress is counted from the game's own events.
    /// </summary>
    public sealed partial class Game
    {
        public const int MaxContracts = 3;
        public readonly List<Contract> Contracts = new List<Contract>();
        public int ContractsDone;

        static readonly string[] BranchNames = { "The Dungeons", "The Mines of Dwarfdeep", "The Warrens", "The Sunken Vaults", "The Ashen Spire" };

        /// <summary>The three offers this town has this week, minus any the hero already holds.</summary>
        public List<Contract> ContractOffers()
        {
            var list = new List<Contract>();
            string town = Town?.Name ?? "road";
            int week = World != null ? World.Day / 7 : 0;
            ulong h = 1469598103934665603UL;
            foreach (char c in town) { h ^= c; h *= 1099511628211UL; }
            var rng = new Rng(h ^ (ulong)(week * 7919 + 11) ^ Rng.Seed);
            for (int i = 0; i < 3; i++)
            {
                string branch = BranchNames[rng.Range(0, BranchNames.Length)];
                var c = new Contract { Branch = branch, Giver = i == 1 ? Houses.Watch : Houses.Guild };
                if (rng.Chance(60))
                {
                    int depth = rng.Range(1, 6);
                    var table = Bestiary.SpawnTable(depth, rng, branch);
                    var def = table.Count > 0 ? table[rng.Range(0, table.Count)] : Bestiary.Find("jackal");
                    c.Kind = "hunt"; c.Target = def.Name; c.Count = rng.Range(3, 7);
                    c.Reward = 30 + c.Count * (10 + def.Level * 8);
                }
                else
                {
                    int cap = 8;
                    c.Kind = "delve"; c.Target = ""; c.Count = rng.Range(3, cap + 1);
                    c.Reward = 60 + 40 * c.Count;
                }
                bool held = false;
                foreach (var have in Contracts) if (have.Key == c.Key) held = true;
                if (!held) list.Add(c);
            }
            return list;
        }

        public bool AcceptContract(Contract c)
        {
            if (Contracts.Count >= MaxContracts) { Tell("You are already carrying as many jobs as you can finish.", MessageKind.Warn); return false; }
            Contracts.Add(c);
            Tell($"You take the job: {c.Describe()}. It pays {c.Reward} gold.", MessageKind.Good);
            return true;
        }

        public bool TurnInContract(Contract c)
        {
            if (!c.Complete || !Contracts.Remove(c)) return false;
            Player.Gold += c.Reward;
            ContractsDone++;
            AddRep(c.Giver, 10, null);
            Tell($"Job done: {c.Describe()}. You are paid {c.Reward} gold.", MessageKind.Good);
            return true;
        }

        /// <summary>A monster the hero killed: hunts in that branch tick up.</summary>
        void ContractKill(Monster m)
        {
            foreach (var c in Contracts)
            {
                if (c.Kind != "hunt" || c.Complete || c.Branch != Branch || c.Target != m.Def.Name) continue;
                c.Done++;
                if (c.Complete) Say($"The job is done: {c.Describe()}. Report to the board.", MessageKind.Quest);
                else Say($"({c.Done}/{c.Count}) {c.Describe()}", MessageKind.Info);
            }
        }

        /// <summary>The hero reached a new depth: delve contracts for that branch check themselves.</summary>
        void ContractDepth()
        {
            foreach (var c in Contracts)
            {
                if (c.Kind != "delve" || c.Complete || c.Branch != Branch) continue;
                c.Done = Math.Max(c.Done, Depth);
                if (c.Complete) Say($"You have reached depth {Depth}: {c.Describe()}. Report to the board.", MessageKind.Quest);
            }
        }
    }
}
