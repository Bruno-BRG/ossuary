using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>What a quest step waits for. Progress is counted from the game's own events (kills, depth, flags, items, the clock).</summary>
    public enum ObjKind { Kill, Reach, Flag, Item, Talk, Wait, Escort }

    public sealed class QuestStep
    {
        public string Text, Hint;
        public ObjKind Kind;
        /// <summary>Kill: monster name. Reach: branch. Flag: flag id. Item: item name. Talk: TownRole name. Wait: unused.</summary>
        public string Target;
        public string Branch;
        public int Count = 1;
        public Action<Game> OnDone;
    }

    /// <summary>A quest as data: a track, a giver, ordered steps, a reward and, when it can expire, a deadline in days.</summary>
    public sealed class QuestDef
    {
        public const string Main = "Main", Guild = "Guild", Watch = "Watch", Temple = "Temple", Cult = "Cult", Personal = "Personal", Rival = "Rival", Region = "Region";
        public string Id, Track, Title, Giver;
        public readonly List<QuestStep> Steps = new List<QuestStep>();
        public int RewardGold, Deadline;
        public Action<Game> OnComplete, OnFail;

        public QuestDef Step(ObjKind kind, string text, string target = null, int count = 1, string branch = null, string hint = null, Action<Game> done = null)
        {
            Steps.Add(new QuestStep { Kind = kind, Text = text, Target = target, Count = count, Branch = branch, Hint = hint, OnDone = done });
            return this;
        }
    }

    public enum QStatus { Active, Done, Failed }

    /// <summary>The saved part: which step, how far, since when. Everything else is the definition.</summary>
    public sealed class QuestState
    {
        public QuestDef Def;
        public int Step, Progress, StartDay, StepDay;
        public QStatus Status;
        public QuestStep Current => Status == QStatus.Active && Step < Def.Steps.Count ? Def.Steps[Step] : null;
        public int DaysLeft(int today) => Def.Deadline <= 0 ? -1 : Math.Max(0, Def.Deadline - (today - StartDay));
    }
}
