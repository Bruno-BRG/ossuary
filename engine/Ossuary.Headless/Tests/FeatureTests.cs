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
    }
}
