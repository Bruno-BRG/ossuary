using System;
using System.Collections.Generic;
using System.Text;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>One finished run, as plain data: the unit of the run history and the morgue file.</summary>
    public sealed class RunRecord
    {
        /// <summary>"died", "won" or "abandoned".</summary>
        public string Outcome { get; set; } = "died";
        public string Name { get; set; } = "";
        public string Race { get; set; } = "";
        public string Role { get; set; } = "";
        public string Title { get; set; } = "";
        public int Level { get; set; }
        public string Cause { get; set; } = "";
        public string Branch { get; set; } = "";
        public int Depth { get; set; }
        public int MaxDepth { get; set; }
        public int Turns { get; set; }
        public int Kills { get; set; }
        public string God { get; set; } = "";
        public string Seed { get; set; } = "";
        /// <summary>Filled in by the host (the Core keeps no clock).</summary>
        public string Date { get; set; } = "";
        public int Score { get; set; }
        public string Mode { get; set; } = "Normal";
        /// <summary>The daily challenge date ("2026-10-02") when this was a daily run, else empty.</summary>
        public string Daily { get; set; } = "";
    }

    /// <summary>The end-of-run summary and the morgue file text. Pure functions of the finished game.</summary>
    public static class Morgue
    {
        public static RunRecord Summarize(Game g)
        {
            var p = g.Player;
            string outcome = g.Mode == GameMode.Won ? "won" : g.Abandoned ? "abandoned" : "died";
            var race = Races.Find(p.RaceId);
            var role = Roles.Find(p.RoleId);
            var r = new RunRecord
            {
                Outcome = outcome,
                Name = p.CharName,
                Race = race?.Name ?? p.RaceId,
                Role = role?.Name ?? p.RoleId,
                Title = p.Title,
                Level = p.XpLevel > 0 ? p.XpLevel : p.Level,
                Cause = outcome == "won" ? "escaped with the Amulet" : g.DeathCause ?? "unknown causes",
                Branch = g.Branch,
                Depth = g.Depth,
                MaxDepth = p.MaxDepth,
                Turns = g.Turn,
                Kills = p.Kills,
                God = p.God != null ? Gods.Find(p.God)?.Name ?? p.God : "",
                Seed = g.Rng.Seed.ToString(),
                Mode = Difficulties.Name(g.Difficulty),
            };
            r.Score = Score(r, p);
            return r;
        }

        /// <summary>A plain, comparable number for the history screen: depth and kills count, victory counts a lot.</summary>
        public static int Score(RunRecord r, Player p)
        {
            Difficulties.ScoreFactor(r.Mode, out int num, out int den);
            return BaseScore(r, p) * num / den;
        }

        static int BaseScore(RunRecord r, Player p) =>
            r.MaxDepth * 100 + r.Kills * 10 + r.Level * 50 + p.Gold / 10 + (r.Outcome == "won" ? 5000 : 0);

        public static string Text(Game g, RunRecord r)
        {
            var p = g.Player;
            var sb = new StringBuilder();
            void Line(string s = "") => sb.Append(s).Append('\n');
            void Head(string s) { Line(); Line(Loc.T(s)); Line(new string('-', Loc.T(s).Length)); }

            Line("OSSUARY — " + Loc.T("morgue"));
            Line(new string('=', 40));
            Line($"{r.Name}, {Loc.U(r.Race)} {Loc.U(r.Role)} ({Loc.U(r.Title)}), {Loc.T("level")} {r.Level}");
            Line(r.Outcome == "won" ? Loc.T("Escaped with the Amulet of Yendor.")
                : r.Outcome == "abandoned" ? Loc.T("Abandoned the run.")
                : Loc.T("Killed by") + " " + Loc.T(r.Cause) + ".");
            Line($"{Loc.T("Deepest level")} {r.MaxDepth}   {Loc.T("Turns")} {r.Turns}   {Loc.T("Kills")} {r.Kills}   {Loc.T("Score")} {r.Score}");
            // The whole line goes through the dictionary, so "em As Masmorras" can contract to "nas Masmorras".
            Line(Loc.T($"Ended {(r.Outcome == "won" ? "in" : "on")} {r.Branch} {r.Depth}"));
            if (r.God.Length > 0) Line($"{Loc.T("Follower of")} {r.God} ({Loc.T("piety")} {p.Piety})");
            if (r.Mode != "Normal") Line(Loc.T("Mode") + ": " + Loc.T(r.Mode));
            if (r.Daily.Length > 0) Line(Loc.T("Daily") + ": " + r.Daily);
            Line($"{Loc.T("Seed")} {r.Seed}" + (r.Date.Length > 0 ? "   " + r.Date : ""));

            Head("Attributes");
            // The layout is the reader's; only the labels are ours to translate.
            Line($"{Loc.U("HP")} {Math.Max(0, p.HP)}/{p.MaxHP}   {Loc.U("MP")} {p.Mp}/{p.MpMax}   {Loc.U("AC")} {p.AC}");
            Line($"{Loc.U("Str")} {p.Str}  {Loc.U("Dex")} {p.Dex}  {Loc.U("Con")} {p.Con}  {Loc.U("Int")} {p.Int}  {Loc.U("Wis")} {p.Wis}  {Loc.U("Cha")} {p.Cha}");
            var skills = new List<string>();
            foreach (var kv in p.Skills) if (kv.Value > 0) skills.Add($"{Loc.U(kv.Key.ToString())} {kv.Value}");
            if (skills.Count > 0) Line(Loc.T("Skills") + ": " + string.Join(", ", skills));

            var perks = new List<string>();
            foreach (var kv in p.Perks)
                perks.Add(Loc.U(Progression.Find(kv.Key)?.Name ?? kv.Key) + (kv.Value > 1 ? " x" + kv.Value : ""));
            if (perks.Count > 0) { Head("Perks"); Line(string.Join(", ", perks)); }

            if (p.Spells.Count > 0)
            {
                var names = new List<string>();
                foreach (string id in p.Spells) names.Add(Loc.U(Spells.Find(id)?.Name ?? id));
                Head("Spells"); Line(string.Join(", ", names));
            }

            if (p.Corruption > 0 || p.Mutated.Count > 0)
            {
                var muts = new List<string>();
                foreach (string id in p.Mutated) { var mu = MutationTable.Find(id); if (mu != null) muts.Add(Loc.T(mu.Name)); }
                Head("Corruption");
                Line($"{p.Corruption}/{Game.CorruptionMax}" + (muts.Count > 0 ? "  " + string.Join(", ", muts) : ""));
            }

            if (p.Wounds.Count > 0 || p.Scars.Count > 0)
            {
                Head("Body");
                var wl = Game.WoundLines(p);
                if (wl.Count > 0) Line(Loc.T("Wounds") + ": " + string.Join(", ", wl));
                if (p.Scars.Count > 0) Line(Loc.T("Scars") + ": " + string.Join(", ", Game.ScarLines(p)));
            }

            if (p.Rep.Count > 0 || g.ContractsDone > 0)
            {
                Head("Standing");
                foreach (string house in Houses.All) Line($"{Loc.T(Houses.Name(house))}: {g.RepOf(house)} ({Loc.T(Houses.Standing(g.RepOf(house)))})");
                if (g.ContractsDone > 0) Line($"{Loc.T("Jobs done")}: {g.ContractsDone}");
            }

            Head("Equipment");
            if (p.Wielded != null) Line(Loc.T("Wielding") + ": " + Loc.U(p.Wielded.Name));
            foreach (var piece in p.WornPieces()) Line(Loc.T("Wearing") + ": " + Loc.U(piece.Name));
            for (int i = 0; i < p.Rings.Length; i++) if (p.Rings[i] != null) Line(Loc.T("Ring") + ": " + Loc.U(p.Rings[i].Name));
            if (p.Amulet != null) Line(Loc.T("Amulet") + ": " + Loc.U(p.Amulet.Name));

            Head("Inventory");
            if (p.Inventory.Count == 0) Line(Loc.T("(empty)"));
            foreach (var it in p.Inventory) Line("  " + Loc.U(it.Name));
            Line($"  {p.Gold} {Loc.T("gold")}");

            Head("Last words");
            int from = Math.Max(0, g.Transcript.Count - 20);
            for (int i = from; i < g.Transcript.Count; i++) Line("  " + g.Transcript[i].Text);
            return sb.ToString();
        }

        /// <summary>Safe file name stem for a record: date and hero name.</summary>
        public static string FileStem(RunRecord r)
        {
            var sb = new StringBuilder();
            foreach (char c in (r.Date.Length > 0 ? r.Date.Replace(':', '-').Replace(' ', '_') + "-" : "") + r.Name)
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }
    }
}
