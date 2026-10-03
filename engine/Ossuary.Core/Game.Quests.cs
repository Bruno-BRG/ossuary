using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// The quest engine. Quests are data (QuestBook); this runs them: start, count progress from the game's own events,
    /// advance steps, pay out, fail by the clock. State is a pure function of seed and keys, so saves need nothing extra.
    /// </summary>
    public sealed partial class Game
    {
        public readonly List<QuestState> Quests = new List<QuestState>();

        int Today => World != null ? World.Day : 0;

        public QuestState QuestOf(string id)
        {
            foreach (var q in Quests) if (q.Def.Id == id) return q;
            return null;
        }

        public bool QuestActive(string id) => QuestOf(id)?.Status == QStatus.Active;
        public bool QuestDone(string id) => QuestOf(id)?.Status == QStatus.Done;

        /// <summary>Starts a quest once (never again after it is active, done or failed).</summary>
        public bool StartQuest(string id) => QuestBook.All.TryGetValue(id, out var def) && StartQuest(def);

        /// <summary>Starts a quest built at run time (personal errands): same rules, the def comes from the caller.</summary>
        public bool StartQuest(QuestDef def)
        {
            if (QuestOf(def.Id) != null) return false;
            var q = new QuestState { Def = def, StartDay = Today, StepDay = Today };
            Quests.Add(q);
            Say(Loc.T("New quest") + ": " + Loc.T(def.Title) + ". (F7)", MessageKind.Quest);
            RecordDeed(Deed.Quest, def.Id, 1);
            QuestCheck();
            return true;
        }

        readonly Dictionary<string, int> _bestDepth = new Dictionary<string, int>();
        public int BestDepthIn(string branch) => _bestDepth.TryGetValue(branch ?? "", out int d) ? d : 0;

        /// <summary>The hero reached a level: remember the deepest per branch, then let Reach steps look.</summary>
        void QuestDepth()
        {
            if (BestDepthIn(Branch) < Depth) _bestDepth[Branch] = Depth;
            QuestCheck();
            CompanionRemark();
        }

        /// <summary>A monster the hero killed: Kill steps tick up.</summary>
        void QuestKill(Monster m)
        {
            foreach (var q in Quests)
            {
                var s = q.Current;
                if (s == null || s.Kind != ObjKind.Kill || s.Target != m.Def.Name) continue;
                if (s.Branch != null && s.Branch != Branch) continue;
                q.Progress++;
                if (q.Progress < s.Count) Say($"({q.Progress}/{s.Count}) " + Loc.T(s.Text), MessageKind.Info);
            }
            QuestCheck();
        }

        /// <summary>Talking to a person of a role: Talk steps for that role advance.</summary>
        void QuestTalk(Monster m)
        {
            foreach (var q in Quests)
            {
                var s = q.Current;
                if (s != null && s.Kind == ObjKind.Talk && s.Target == m.Role.ToString()) q.Progress = s.Count;
            }
            QuestCheck();
        }

        bool HasItemNamed(string name)
        {
            foreach (var it in Player.Inventory) if (it.Def.Name == name) return true;
            return Player.Amulet != null && Player.Amulet.Def.Name == name;
        }

        public bool HasItem(string name) => HasItemNamed(name);

        /// <summary>Takes one of an item out of the pack. False when there is none.</summary>
        public bool TakeItem(string name)
        {
            var it = Player.Inventory.Find(i => i.Def.Name == name);
            if (it == null) return false;
            if (--it.Quantity <= 0) Player.Inventory.Remove(it);
            return true;
        }

        bool StepMet(QuestState q, QuestStep s)
        {
            switch (s.Kind)
            {
                case ObjKind.Kill: case ObjKind.Talk: return q.Progress >= s.Count;
                case ObjKind.Reach: return BestDepthIn(s.Target) >= s.Count;
                case ObjKind.Flag: return s.Target == "truth.all" ? AllTruths : Flags.Contains(s.Target);
                case ObjKind.Item: return HasItemNamed(s.Target);
                case ObjKind.Wait: return Today - q.StepDay >= s.Count;
            }
            return false;
        }

        /// <summary>Advances every active quest as far as it can go, and fails the ones whose time ran out.</summary>
        public void QuestCheck()
        {
            for (int i = 0; i < Quests.Count; i++)
            {
                var q = Quests[i];
                for (int guard = 0; q.Status == QStatus.Active && guard < 16; guard++)
                {
                    var s = q.Current;
                    if (s == null || !StepMet(q, s)) break;
                    s.OnDone?.Invoke(this);
                    q.Step++; q.Progress = 0; q.StepDay = Today;
                    if (q.Step >= q.Def.Steps.Count) { CompleteQuest(q); break; }
                    Say(Loc.T("Quest") + ": " + Loc.T(q.Def.Title) + " - " + Loc.T(q.Current.Text), MessageKind.Quest);
                }
                if (q.Status == QStatus.Active && q.Def.Deadline > 0 && Today - q.StartDay > q.Def.Deadline) FailQuest(q);
            }
        }

        void CompleteQuest(QuestState q)
        {
            q.Status = QStatus.Done;
            if (q.Def.RewardGold > 0) Player.Gold += q.Def.RewardGold;
            RecordDeed(Deed.Quest, q.Def.Id + ".done", 2);
            Say(Loc.T("Quest complete") + ": " + Loc.T(q.Def.Title) + (q.Def.RewardGold > 0 ? $" (+{q.Def.RewardGold}g)" : ""), MessageKind.Quest);
            q.Def.OnComplete?.Invoke(this);
        }

        void FailQuest(QuestState q)
        {
            q.Status = QStatus.Failed;
            RecordDeed(Deed.Failed, q.Def.Id, 2);
            Say(Loc.T("Quest failed") + ": " + Loc.T(q.Def.Title), MessageKind.Warn);
            q.Def.OnFail?.Invoke(this);
        }
    }
}
