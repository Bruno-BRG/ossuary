using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>One thing the hero did that the world should remember (docs/game/living-world.md, pillar 9).</summary>
    public sealed class Deed
    {
        public const string Killed = "killed", Struck = "struck", Contract = "contract", Helped = "helped", Failed = "failed", Quest = "quest", Jailed = "jailed", Bribed = "bribed";
        public string Kind, Subject, Where;
        public int Day, Turn, Weight;
    }

    /// <summary>
    /// The WorldLedger: an append-only record of deeds. It is written from the game's own choke points and only read by what
    /// shows the world remembering (lines, prices, bounty, epilogue), so it is a pure function of seed and keys and needs no save of its own.
    /// </summary>
    public sealed partial class Game
    {
        public readonly List<Deed> Ledger = new List<Deed>();

        public Deed RecordDeed(string kind, string subject, int weight = 1)
        {
            var d = new Deed
            {
                Kind = kind, Subject = subject ?? "", Weight = weight, Turn = Turn,
                Day = World != null ? World.Day : 0,
                Where = Town != null && (Mode == GameMode.TownMap) ? Town.Name : Branch,
            };
            Ledger.Add(d);
            return d;
        }

        public int DeedCount(string kind, string subject = null)
        {
            int n = 0;
            foreach (var d in Ledger) if (d.Kind == kind && (subject == null || d.Subject == subject)) n++;
            return n;
        }

        /// <summary>The hero raised a hand to a townsperson (refused, but seen): they remember, and so does the Watch.</summary>
        void StruckAt(Monster m)
        {
            if (m.Memory == null) return;
            bool first = !m.Memory.Has(NpcMemory.Struck);
            m.Memory.Set(NpcMemory.Struck);
            m.Memory.Shift(-30);
            RecordDeed(Deed.Struck, m.Name);
            if (first && m.Role != TownRole.Pet) AddRep(Houses.Watch, -3, "The Watch hears you threatened a citizen.");
        }

        /// <summary>The hero spoke to a townsperson.</summary>
        void MetPerson(Monster m)
        {
            if (m.Memory == null) return;
            m.Memory.Set(NpcMemory.Met);
            foreach (string f in m.Memory.Flags)
                if (f.StartsWith("kin.of.")) { Flags.Add("found." + f.Substring(7)); break; }
            QuestTalk(m);
        }
    }
}
