using System;
using System.Collections.Generic;
using System.Linq;
using Ossuary.Core.Entities;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>One thing the hero did that the world should remember (docs/game/living-world.md, pillar 9).</summary>
    public sealed class Deed
    {
        public const string Killed = "killed", Struck = "struck", Helped = "helped", Failed = "failed", Quest = "quest", Jailed = "jailed", Bribed = "bribed", Cleared = "cleared", Stole = "stole", Burnt = "burnt", Escaped = "escaped";
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

        // ------------------------------------------------------------------ what the ledger changes

        /// <summary>A killing in a town reaches the towns nearby this many days later, and stays news for this long.</summary>
        public const int NewsDelay = 3, NewsLife = 40;

        /// <summary>The towns nearest to this one on the overworld (by tiles, ties by name), not counting itself.</summary>
        public List<string> NearbyTowns(string name, int k = 3)
        {
            var found = new List<string>();
            if (World == null || string.IsNullOrEmpty(name)) return found;
            int from = -1;
            for (int i = 0; i < World.Tiles.Length && from < 0; i++)
                if (World.Tiles[i].Feature == OverworldFeature.Town && World.Tiles[i].Name == name) from = i;
            if (from < 0) return found;
            int fx = from % World.W, fy = from / World.W;
            var near = new List<(int Dist, string Name)>();
            for (int i = 0; i < World.Tiles.Length; i++)
            {
                var t = World.Tiles[i];
                if (i == from || t.Feature != OverworldFeature.Town || string.IsNullOrEmpty(t.Name) || t.Name == name) continue;
                near.Add((Math.Max(Math.Abs(i % World.W - fx), Math.Abs(i / World.W - fy)), t.Name));
            }
            near.Sort((a, b) => a.Dist != b.Dist ? a.Dist.CompareTo(b.Dist) : string.CompareOrdinal(a.Name, b.Name));
            foreach (var n in near)
            {
                if (found.Count >= k) break;
                found.Add(n.Name);
            }
            return found;
        }

        /// <summary>The newest killing in a nearby town that has come round to this one by now; null when none has.</summary>
        public Deed NewsHere()
        {
            if (Town == null || World == null) return null;
            var near = NearbyTowns(Town.Name);
            Deed best = null;
            foreach (var d in Ledger)
            {
                if (d.Kind != Deed.Killed || !near.Contains(d.Where)) continue;
                int age = World.Day - d.Day;
                if (age < NewsDelay || age > NewsLife) continue;
                if (best == null || d.Day > best.Day || (d.Day == best.Day && d.Turn > best.Turn)) best = d;
            }
            return best;
        }

        /// <summary>What the gossip of a town says of a killing it has heard of.</summary>
        public static string NewsLine(Deed d) => TownText.L(
            $"They say a stranger killed {d.Subject} in {d.Where} and walked out of the gate.",
            $"Dizem que um estranho matou {d.Subject} em {d.Where} e saiu pelo portão.");

        /// <summary>What this town remembers of the hero, as a percentage on every price: killings here dearer, good deeds here a little cheaper.</summary>
        public int TownMemoryPct()
        {
            if (Town == null || Mode != GameMode.TownMap) return 0;
            int helped = 0;
            foreach (var d in Ledger) if (d.Kind == Deed.Helped && d.Where == Town.Name) helped++;
            return Math.Min(25, 5 * KillingsHere()) - Math.Min(10, 2 * helped);
        }

        /// <summary>The killings the ledger holds against the hero in this town, townspeople and the rest alike.</summary>
        public int KillingsHere()
        {
            if (Town == null || Mode != GameMode.TownMap) return 0;
            int n = 0;
            foreach (var d in Ledger) if (d.Kind == Deed.Killed && d.Where == Town.Name) n++;
            return n;
        }

        /// <summary>A healer will not tend a hand that has killed twice in the town: the ledger's refusal of a service.</summary>
        public bool HealingRefused() => KillingsHere() >= 2;

        /// <summary>The cellar under a town is quiet once its last dead one is down, and the town writes that down.</summary>
        void CellarCleared(Monster m)
        {
            if (Mode != GameMode.TownMap || Town == null || m.Floor >= 0 || m.Def.Name != "human zombie") return;
            foreach (var n in Town.Lurkers)
                if (n != m && n.Floor == m.Floor && !n.IsDead) return;
            RecordDeed(Deed.Cleared, m.Home != null ? m.Home.Name : Town.Name, 3);
            Say(TownText.L("The cellar is quiet now.", "O porão está em silêncio agora."), MessageKind.Good);
        }

        /// <summary>The deeds the ending remembers: the heaviest the hero did (weight 3 or more), told in the order they were done.</summary>
        public List<string> MatteringDeeds(int max = 6)
        {
            // The ledger is append-only, so its order is the order the deeds were done: the ranking is by weight, the telling by index.
            var heavy = Ledger.Select((d, i) => (Deed: d, Index: i)).Where(x => x.Deed.Weight >= 3 && x.Deed.Kind != "ending");
            var top = heavy.OrderByDescending(x => x.Deed.Weight).ThenBy(x => x.Index).Take(max).OrderBy(x => x.Index);
            return top.Select(x => DeedLine(x.Deed)).ToList();
        }

        static string DeedLine(Deed d)
        {
            string at = string.IsNullOrEmpty(d.Where) ? "" : " in " + d.Where;
            string em = string.IsNullOrEmpty(d.Where) ? "" : " em " + d.Where;
            switch (d.Kind)
            {
                case Deed.Killed: return TownText.L($"You killed {d.Subject}{at}.", $"Você matou {d.Subject}{em}.");
                case Deed.Struck: return TownText.L($"You struck {d.Subject}{at}.", $"Você atacou {d.Subject}{em}.");
                case Deed.Helped: return TownText.L($"You helped {d.Subject}{at}.", $"Você ajudou {d.Subject}{em}.");
                case Deed.Cleared: return TownText.L($"You cleared the cellar under {d.Subject}{at}.", $"Você limpou o porão sob {d.Subject}{em}.");
                case Deed.Jailed: return TownText.L($"You were jailed{at}, for {d.Weight} days.", $"Você foi preso{em}, por {d.Weight} dias.");
                default: return TownText.L($"You {d.Kind} {d.Subject}{at}.", $"Você {d.Kind} {d.Subject}{em}.");
            }
        }
    }
}
