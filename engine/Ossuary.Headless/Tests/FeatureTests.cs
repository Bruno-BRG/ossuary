using System;
using System.Collections.Generic;
using Ossuary.Core;
using Ossuary.Core.Entities;

namespace Ossuary.Tests
{
    /// <summary>
    /// Assertions for the features tracked in docs/a-fazer.md. Run() is wired into TestRunner.RunAll as one entry;
    /// new features add a method here and a line in Run().
    /// </summary>
    public static class FeatureTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            Test("auto-explore reveals the level and ends", AutoExplore);
            Test("travel to stairs and rest until healed", StairsAndRest);
            Test("a death is recorded with its cause and a morgue text", MorgueContent);
            Test("dead heroes return as shades on their level", BonesShades);
            Test("the daily challenge is stable per date", DailySeeds);
            Test("achievements are earned from state and never touch the simulation", AchievementsEarned);
            Test("traps are sensed, found by searching and disarmed", TrapsFlow);
            Console.WriteLine($"==== features: {_pass} passed, {_fail} failed ====");
            if (_fail > 0) throw new Exception($"{_fail} feature asserts failed");
        }

        static void Test(string name, Action body)
        {
            try { body(); _pass++; Console.WriteLine("  ok   " + name); }
            catch (Exception e) { _fail++; Console.WriteLine("  FAIL " + name + ": " + e.Message); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void AutoExplore()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var g = new Game(seed * 7919);
                g.Monsters.Clear();
                var cmd = new Commands(g);
                var reach = g.Map.Reachability(g.Player.X, g.Player.Y, true);
                int calls = 0, lastTurn = -1;
                while (calls < 200 && g.Turn != lastTurn) { lastTurn = g.Turn; cmd.Execute("explore"); calls++; }
                Assert(calls < 200, "explore never settled on seed " + seed);
                int total = 0, seen = 0;
                for (int i = 0; i < reach.Length; i++)
                    if (reach[i]) { total++; if (g.Map.WasSeen(i % g.Map.W, i / g.Map.W)) seen++; }
                Assert(seen * 100 >= total * 90, $"seed {seed} explored only {seen}/{total}");
            }
        }

        static void StairsAndRest()
        {
            var g = new Game(2024);
            g.Monsters.Clear();
            var cmd = new Commands(g);
            int none = g.Turn; cmd.Execute("stairs");
            // Before anything is seen there may be no known stairs; exploring first always finds them.
            for (int i = 0; i < 40; i++) cmd.Execute("explore");
            cmd.Execute("stairs");
            var t = g.Map.Get(g.Player.X, g.Player.Y);
            Assert(t == TileKind.StairsDown || t == TileKind.LadderDown || t == TileKind.StairsUp, "travel to stairs ends on stairs, stood on " + t);
            int turn = g.Turn; cmd.Execute("stairs");
            Assert(g.Turn == turn, "already on the stairs costs no turn");

            g.Player.HP = 1; g.Player.Nutrient = 5000;
            cmd.Execute("rest");
            Assert(g.Player.HP == g.Player.MaxHP, $"rest heals fully, got {g.Player.HP}/{g.Player.MaxHP} turn {g.Turn} last: {g.Log[g.Log.Count - 1].Text}");
            turn = g.Turn; cmd.Execute("rest");
            Assert(g.Turn == turn, "resting when rested costs no turn");

            // An enemy in sight refuses all three, at no cost.
            g.Player.HP = 1;
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng);
            rat.X = g.Player.X; rat.Y = g.Player.Y;
            foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (g.Map.Walkable(g.Player.X + d.Item1, g.Player.Y + d.Item2)) { rat.X = g.Player.X + d.Item1; rat.Y = g.Player.Y + d.Item2; break; }
            g.Monsters.Add(rat); g.UpdateFov();
            turn = g.Turn;
            cmd.Execute("rest"); cmd.Execute("explore"); cmd.Execute("stairs");
            Assert(g.Turn == turn, "enemies in sight refuse auto-walk without spending turns");
        }

        static void MorgueContent()
        {
            var g = Game.NewHero(55, "Mara", "dwarf", "fighter");
            g.Monsters.Clear();
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng);
            foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (g.Map.Walkable(g.Player.X + d.Item1, g.Player.Y + d.Item2)) { rat.X = g.Player.X + d.Item1; rat.Y = g.Player.Y + d.Item2; break; }
            g.Monsters.Add(rat);
            g.Player.HP = 1; g.Player.AC = 30;
            for (int i = 0; i < 400 && g.Mode == GameMode.Dungeon; i++)
            {
                g.Player.HP = Math.Min(g.Player.HP, 1); g.Player.AC = 30; rat.Dormant = false; rat.Alert = 1;
                g.Player.Nutrient = 5000;
                g.Wait();
                if (g.Player.HP <= 0) break;
            }
            Assert(g.Mode == GameMode.GameOver, "the hero should have died");
            Assert(g.DeathCause != null && g.DeathCause.Contains("rat"), "cause names the killer, got " + g.DeathCause);
            var r = Morgue.Summarize(g);
            Assert(r.Outcome == "died" && r.Name == "Mara" && r.Role == "Fighter", "record fields: " + r.Outcome + " " + r.Name + " " + r.Role);
            string text = Morgue.Text(g, r);
            Assert(text.Contains("Killed by a giant rat"), "morgue names the killer");
            Assert(text.Contains("Mara") && text.Contains("Inventory") && text.Contains("Last words"), "morgue has the sections");
            Assert(Morgue.FileStem(r).IndexOf('/') < 0, "file stem is safe");
        }

        static void BonesShades()
        {
            var dead = Game.NewHero(8080, "Mara", "dwarf", "fighter");
            dead.DescendTo("The Dungeons", 2);
            dead.Player.HP = 0; dead.CheckDeath();
            var bones = dead.LeaveBones();
            Assert(bones != null && bones.Depth == 2 && bones.Name == "Mara", "a death at depth 2 leaves bones");
            Assert(bones.Gear.Count > 0 && bones.Gear[0].Def == dead.Player.Wielded.Def.Name, "the weapon is among the gear");
            var shallow = Game.NewHero(8081, "Pip", "human", "fighter");
            shallow.Player.HP = 0; shallow.CheckDeath();
            Assert(shallow.LeaveBones() == null, "no bones on level 1");

            int found = 0;
            for (ulong seed = 1; seed <= 24; seed++)
            {
                var plain = new Game(seed); plain.DescendTo("The Dungeons", 2);
                var haunted = new Game(seed); haunted.Graveyard.Add(bones); haunted.DescendTo("The Dungeons", 2);
                Assert(plain.Map.ToAscii() == haunted.Map.ToAscii(), "a shade never changes the level itself, seed " + seed);
                Assert(plain.Rng.NextULong() == haunted.Rng.NextULong(), "a shade never consumes the world's Rng, seed " + seed);
                Monster shade = null;
                foreach (var m in haunted.Monsters) if (m.BonesKey != null) shade = m;
                if (shade == null) continue;
                found++;
                Assert(shade.Name == "shade of Mara" && shade.Unique && shade.Def.Undead, "the shade is named for the hero");
                Assert(shade.Inventory.Count > 0, "the shade carries the hero's gear");
                var again = new Game(seed); again.Graveyard.Add(bones); again.DescendTo("The Dungeons", 2);
                Monster twin = null; foreach (var m in again.Monsters) if (m.BonesKey != null) twin = m;
                Assert(twin != null && twin.X == shade.X && twin.Y == shade.Y, "the shade stands in the same place on a replay");
                if (found == 1)
                {
                    haunted.Monsters.Remove(shade); haunted.Monsters.Add(shade);
                    haunted.KillMonster(shade);
                    Assert(haunted.LaidToRest.Contains(bones.Key), "destroying the shade lays the bones to rest");
                }
            }
            Assert(found >= 5 && found <= 22, "shades appear on most but not all levels, found " + found);
        }

        static void DailySeeds()
        {
            Assert(Daily.SeedFor("2026-10-02") == Daily.SeedFor("2026-10-02"), "same date, same seed");
            var seen = new HashSet<ulong>();
            for (int d = 1; d <= 60; d++) seen.Add(Daily.SeedFor(Daily.Label(2026, 1 + d / 31, 1 + d % 28)));
            Assert(seen.Count >= 55, "different dates give different seeds, got " + seen.Count);
            for (int d = 1; d <= 40; d++)
            {
                Daily.HeroFor(Daily.SeedFor(Daily.Label(2026, 11, d % 28 + 1)), out string race, out string role);
                var hero = Game.NewHero(1, "Daily", race, role);
                Assert(hero.Player.RaceId == race && hero.Player.RoleId == role, "the daily hero is a real race and class");
            }
        }

        static void AchievementsEarned()
        {
            var ids = new HashSet<string>();
            foreach (var a in Achievements.All) { Assert(ids.Add(a.Id), "duplicate achievement id " + a.Id); Assert(a.Name.Length > 0 && a.Blurb.Length > 0, "achievement text " + a.Id); }

            var g = Game.NewHero(21, "Tester", "human", "fighter");
            g.Monsters.Clear();
            Assert(g.Earned.Count == 0, "nothing earned at the start");
            long said = g.Said;
            g.Player.Kills = 1; g.Player.MaxDepth = 5; g.Player.Gold = 1500;
            g.Wait();
            foreach (string id in new[] { "first-blood", "delver", "rich" }) Assert(g.Earned.Contains(id), id + " should be earned");
            Assert(!g.Earned.Contains("slayer") && !g.Earned.Contains("escape"), "unearned ones stay unearned");
            Assert(g.Said == said, "announcements never change the Said counter, so an auto-walk replays the same");
            bool announced = false;
            foreach (var m in g.Log) if (m.Text.StartsWith("Achievement: ")) announced = true;
            Assert(announced, "an earned achievement is announced in the log");

            var quiet = Game.NewHero(21, "Tester", "human", "fighter");
            quiet.AlreadyUnlocked.Add("first-blood"); quiet.Monsters.Clear();
            quiet.Player.Kills = 1; quiet.Wait();
            Assert(quiet.Earned.Contains("first-blood"), "still earned in this run");
            foreach (var m in quiet.Log) Assert(!m.Text.Contains("First Blood"), "but not announced again");

            var won = Game.NewHero(22, "Tester", "human", "fighter");
            won.Difficulty = Difficulty.Hardcore; won.DailyLabel = "2026-10-02";
            won.Mode = GameMode.Won; won.CheckAchievements();
            Assert(won.Earned.Contains("escape") && won.Earned.Contains("iron") && won.Earned.Contains("daily-victor"), "victory achievements follow mode and daily");
        }

        static void TrapsFlow()
        {
            var g = Game.NewHero(303, "Pick", "halfling", "rogue");
            g.Monsters.Clear();
            var cmd = new Commands(g);
            int turn = g.Turn;
            cmd.Execute("disarm");
            Assert(g.Turn == turn, "disarming with nothing found costs no turn");

            // A trap on a cell this hero would notice by walking past, and one searching must find.
            g.Player.Skills[Skill.Search] = 100;
            int pct = g.TrapSensePct();
            Assert(pct >= 50, "a rogue with Search 100 senses most traps, got " + pct);
            int px = g.Player.X, py = g.Player.Y;
            for (int y = py - 1; y <= py + 1; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if (x != px || y != py) { g.Map.Set(x, y, TileKind.Floor); TrapTable.Remove(g.Map.Number, x, y); }
            int tx = -1, ty = -1;
            for (int y = py - 1; y <= py + 1 && tx < 0; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if ((x != px || y != py) && g.Map.Walkable(x, y) && Theme.Hash01(x * 31 + g.Map.Number, y * 17 + 5) * 100 < pct) { tx = x; ty = y; break; }
            Assert(tx >= 0, "test setup: a cell the hero will notice");
            TrapTable.Put(g.Map.Number, tx, ty, Traps.Spike, 2);
            Assert(!TrapTable.IsRevealed(g.Map.Number, tx, ty), "the trap starts hidden");
            g.SenseTraps();
            Assert(TrapTable.IsRevealed(g.Map.Number, tx, ty), "a nearby trap is sensed without searching");

            // Searching reveals what sensing missed.
            int sx = -1, sy = -1;
            for (int y = py - 1; y <= py + 1 && sx < 0; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if ((x != px || y != py) && (x != tx || y != ty) && g.Map.Walkable(x, y) && Theme.Hash01(x * 31 + g.Map.Number, y * 17 + 5) * 100 >= pct) { sx = x; sy = y; break; }
            if (sx >= 0)
            {
                TrapTable.Put(g.Map.Number, sx, sy, Traps.Dart, 2);
                g.SenseTraps();
                Assert(!TrapTable.IsRevealed(g.Map.Number, sx, sy), "an unnoticed trap stays hidden");
                cmd.Execute("s");
                Assert(TrapTable.IsRevealed(g.Map.Number, sx, sy), "searching reveals it");
            }

            // Disarming ends with the trap gone, whether it was disarmed or set off, and never costs a free turn.
            int guard = 0;
            while (TrapTable.TryGet(g.Map.Number, tx, ty, out _, out _) && guard++ < 60)
            {
                g.Player.HP = g.Player.MaxHP;
                g.FacingX = tx - px; g.FacingY = ty - py;
                int before = g.Turn;
                cmd.Execute("disarm");
                Assert(g.Turn == before + 1, "each attempt takes a turn");
                if (g.Mode != GameMode.Dungeon) break;
            }
            Assert(!TrapTable.TryGet(g.Map.Number, tx, ty, out _, out _), "the trap is gone after enough attempts");
            Assert(!TrapTable.IsRevealed(g.Map.Number, tx, ty), "and no longer marked");

            // Auto-explore keeps clear of found traps.
            var h = Game.NewHero(304, "Pick", "human", "fighter");
            h.Monsters.Clear();
            for (int i = 0; i < h.Map.W * h.Map.H; i++) { int x = i % h.Map.W, y = i / h.Map.W; if (TrapTable.TryGet(h.Map.Number, x, y, out _, out _)) TrapTable.Remove(h.Map.Number, x, y); }
            var hc = new Commands(h);
            for (int i = 0; i < 200; i++) hc.Execute("explore");
            Assert(h.Player.HP == h.Player.MaxHP, "an explorer on a trapless level is never hurt");
        }
    }
}
