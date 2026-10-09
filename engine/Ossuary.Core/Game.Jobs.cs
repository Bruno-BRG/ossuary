using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// A job the notice board or a workshop offers, as a template with no progress. Taking it (BuildJob) makes it a quest on
    /// the Guild track, and the quest engine owns its progress, reward and one-shot rule (docs/game/guild-jobs.md).
    /// </summary>
    public sealed class JobOffer
    {
        /// <summary>"hunt", "delve" or "make" (a commission).</summary>
        public string Id, Kind, Branch, Target, Giver;
        public int Count, Reward, Index;

        public string Describe() => Kind == "hunt" ? $"Hunt {Count} {Target} in {Branch}"
            : Kind == "make" ? $"Craft for the town: {Target}"
            : $"Reach depth {Count} of {Branch}";
    }

    /// <summary>
    /// The Guild's notice board and the jobs it hands out. Offers are a function of the town and the week and are never
    /// stored; only the jobs the hero takes are, as quests. Progress is counted from the game's own events.
    /// </summary>
    public sealed partial class Game
    {
        public const int MaxContracts = 3;

        static readonly string[] BranchNames = { "The Dungeons", "The Mines of Dwarfdeep", "The Warrens", "The Sunken Vaults", "The Ashen Spire" };

        /// <summary>The flag that marks a finished hunt or delve as ready to hand in, at any notice board.</summary>
        public static string ReportFlag(string questId) => "guild.report." + questId;

        /// <summary>The flag that a workshop sets when it takes a commission's product.</summary>
        public static string DeliverFlag(string questId) => "guild.deliver." + questId;

        /// <summary>
        /// The three offers of a town in a week. It draws from its own generator, so reading the board never moves the
        /// simulation's Rng.
        /// </summary>
        public List<JobOffer> GenerateJobs(string town, int week)
        {
            var list = new List<JobOffer>();
            ulong h = 1469598103934665603UL;
            foreach (char c in town) { h ^= c; h *= 1099511628211UL; }
            var rng = new Rng(h ^ (ulong)(week * 7919 + 11) ^ Rng.Seed);
            for (int i = 0; i < 3; i++)
            {
                string branch = BranchNames[rng.Range(0, BranchNames.Length)];
                var o = new JobOffer { Id = $"guild.{town}.{week}.{i}", Index = i, Branch = branch, Giver = i == 1 ? Houses.Watch : Houses.Guild };
                if (rng.Chance(60))
                {
                    int depth = rng.Range(1, 6);
                    var table = Bestiary.SpawnTable(depth, rng, branch);
                    var def = table.Count > 0 ? table[rng.Range(0, table.Count)] : Bestiary.Find("jackal");
                    o.Kind = "hunt"; o.Target = def.Name; o.Count = rng.Range(3, 7);
                    o.Reward = 30 + o.Count * (10 + def.Level * 8);
                }
                else
                {
                    int cap = 8;
                    o.Kind = "delve"; o.Target = ""; o.Count = rng.Range(3, cap + 1);
                    o.Reward = 60 + 40 * o.Count;
                }
                list.Add(o);
            }
            return list;
        }

        /// <summary>
        /// The board here this week: the generator's offers, minus those the hero has already taken, reported or failed this
        /// week, and minus a delve to a depth the hero has already reached (it would be ready on acceptance).
        /// </summary>
        public List<JobOffer> JobOffers()
        {
            var list = new List<JobOffer>();
            foreach (var o in GenerateJobs(Town?.Name ?? "road", World != null ? World.Day / 7 : 0))
            {
                if (QuestOf(o.Id) != null) continue;
                if (o.Kind == "delve" && BestDepthIn(o.Branch) >= o.Count) continue;
                list.Add(o);
            }
            return list;
        }

        /// <summary>The Guild jobs the hero is carrying: the active quests on the Guild track.</summary>
        public List<QuestState> ActiveJobs() => Quests.FindAll(q => q.Def.Track == QuestDef.Guild && q.Status == QStatus.Active);

        /// <summary>Finished Guild jobs, counted from the quest engine: Hired Hand and the morgue read it.</summary>
        public int ContractsDone => Quests.FindAll(q => q.Def.Track == QuestDef.Guild && q.Status == QStatus.Done).Count;

        /// <summary>Takes a job: it becomes a quest on the Guild track. Three at a time, commissions included.</summary>
        public bool AcceptJob(JobOffer o)
        {
            if (ActiveJobs().Count >= MaxContracts) { Tell("You are already carrying as many jobs as you can finish.", MessageKind.Warn); return false; }
            return StartQuest(BuildJob(o));
        }

        /// <summary>
        /// The quest a taken job becomes (docs/game/guild-jobs.md, D2): its objective, then the report or the delivery. The
        /// giver's standing, and for a commission the Guild's and the trade's, come on completion.
        /// </summary>
        public QuestDef BuildJob(JobOffer o)
        {
            string giver = o.Giver, branch = o.Branch;
            var def = new QuestDef { Id = o.Id, Track = QuestDef.Guild, Title = o.Describe(), Giver = Houses.Name(giver), RewardGold = o.Reward };
            if (o.Kind == "make")
            {
                def.Step(ObjKind.Item, o.Describe(), o.Target, 1, branch, null,
                        g => g.Say("That will do for the commission. Bring it to the workshop.", MessageKind.Quest))
                   .Step(ObjKind.Flag, TownText.L("Bring it to the workshop.", "Leve à oficina."), DeliverFlag(o.Id), 1, null,
                        TownText.L("A workshop that teaches the trade.", "Uma oficina que ensina o ofício."));
                def.OnComplete = g => { g.AddRep(Houses.Guild, 5, null); g.GainTrade(branch, 5); };
                return def;
            }
            if (o.Kind == "delve") def.Step(ObjKind.Reach, o.Describe(), branch, o.Count, branch);
            else def.Step(ObjKind.Kill, o.Describe(), o.Target, o.Count, branch);
            def.Step(ObjKind.Flag, TownText.L("Report to any notice board.", "Avise em qualquer quadro de avisos."), ReportFlag(o.Id), 1, null,
                     TownText.L("Any notice board in the Ossuary's towns.", "Qualquer quadro de avisos das cidades do Ossuary."));
            def.OnComplete = g => g.AddRep(giver, 10, null);
            return def;
        }

        /// <summary>A job's objective as (done, needed). A job past its objective reads as done; a delve counts the deepest level reached.</summary>
        public (int Done, int Need) JobCount(QuestState q)
        {
            var s = q.Def.Steps[0];
            bool passed = q.Step > 0;
            int done = s.Kind == ObjKind.Reach
                ? (passed ? s.Count : Math.Min(BestDepthIn(s.Target), s.Count))
                : (passed ? s.Count : Math.Min(q.Progress, s.Count));
            return (done, s.Count);
        }

        /// <summary>The Character panel's note for a job: "ready" at its report or delivery step, otherwise the count.</summary>
        public string JobProgressText(QuestState q)
        {
            if (q.Current?.Kind == ObjKind.Flag) return "ready";
            var (done, need) = JobCount(q);
            return $"{done}/{need}";
        }
    }
}
