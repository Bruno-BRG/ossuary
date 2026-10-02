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
        public static int Score(RunRecord r, Player p) =>
            (r.MaxDepth * 100 + r.Kills * 10 + r.Level * 50 + p.Gold / 10 + (r.Outcome == "won" ? 5000 : 0))
            * (r.Mode == "Hardcore" ? 3 : 2) / (r.Mode == "Classic" ? 3 : 2);

        public static string Text(Game g, RunRecord r)
        {
            var p = g.Player;
            var sb = new StringBuilder();
            void Line(string s = "") => sb.Append(s).Append('\n');
            void Head(string s) { Line(); Line(Loc.T(s)); Line(new string('-', Loc.T(s).Length)); }

            Line("OSSUARY — " + Loc.T("morgue"));
            Line(new string('=', 40));
            Line($"{r.Name}, {r.Race} {r.Role} ({r.Title}), {Loc.T("level")} {r.Level}");
            Line(r.Outcome == "won" ? Loc.T("Escaped with the Amulet of Yendor.")
                : r.Outcome == "abandoned" ? Loc.T("Abandoned the run.")
                : Loc.T("Killed by") + " " + Loc.T(r.Cause) + ".");
            Line($"{Loc.T("Deepest level")} {r.MaxDepth}   {Loc.T("Turns")} {r.Turns}   {Loc.T("Kills")} {r.Kills}   {Loc.T("Score")} {r.Score}");
            Line($"{Loc.T(r.Outcome == "won" ? "Ended in" : "Ended on")} {r.Branch} {r.Depth}");
            if (r.God.Length > 0) Line($"{Loc.T("Follower of")} {r.God} ({Loc.T("piety")} {p.Piety})");
            if (r.Mode != "Normal") Line(Loc.T("Mode") + ": " + Loc.T(r.Mode));
            Line($"{Loc.T("Seed")} {r.Seed}" + (r.Date.Length > 0 ? "   " + r.Date : ""));

            Head("Attributes");
            Line($"HP {Math.Max(0, p.HP)}/{p.MaxHP}   MP {p.Mp}/{p.MpMax}   AC {p.AC}");
            Line($"Str {p.Str}  Dex {p.Dex}  Con {p.Con}  Int {p.Int}  Wis {p.Wis}  Cha {p.Cha}");
            var skills = new List<string>();
            foreach (var kv in p.Skills) if (kv.Value > 0) skills.Add($"{kv.Key} {kv.Value}");
            if (skills.Count > 0) Line(Loc.T("Skills") + ": " + string.Join(", ", skills));

            var perks = new List<string>();
            foreach (var kv in p.Perks)
                perks.Add((Progression.Find(kv.Key)?.Name ?? kv.Key) + (kv.Value > 1 ? " x" + kv.Value : ""));
            if (perks.Count > 0) { Head("Perks"); Line(string.Join(", ", perks)); }

            if (p.Spells.Count > 0)
            {
                var names = new List<string>();
                foreach (string id in p.Spells) names.Add(Spells.Find(id)?.Name ?? id);
                Head("Spells"); Line(string.Join(", ", names));
            }

            Head("Equipment");
            if (p.Wielded != null) Line(Loc.T("Wielding") + ": " + p.Wielded.Name);
            foreach (var piece in p.WornPieces()) Line(Loc.T("Wearing") + ": " + piece.Name);
            for (int i = 0; i < p.Rings.Length; i++) if (p.Rings[i] != null) Line(Loc.T("Ring") + ": " + p.Rings[i].Name);
            if (p.Amulet != null) Line(Loc.T("Amulet") + ": " + p.Amulet.Name);

            Head("Inventory");
            if (p.Inventory.Count == 0) Line(Loc.T("(empty)"));
            foreach (var it in p.Inventory) Line("  " + it.Name);
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
