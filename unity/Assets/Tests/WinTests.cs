using System;
using Ossuary.Core;
using Ossuary.Core.Items;

namespace Ossuary.Tests
{
    /// <summary>
    /// Win-condition assertions. Wired into TestRunner.RunAll separately;
    /// this file only exposes Run() so parallel edits to CoreTests.cs never clash.
    /// </summary>
    public static class WinTests
    {
        static int _pass;
        static int _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;

            Test("amulet waits at the bottom", AmuletAtBottom);
            Test("surfacing with the amulet wins", VictoryWithAmulet);
            Test("surfacing empty-handed does not win", NoVictoryWithoutAmulet);
            Test("worn amulet also wins", VictoryWithWornAmulet);
            Test("bottom fallback replaces a missing amulet", FallbackReplacesMissing);

            Console.WriteLine($"==== win: {_pass} passed, {_fail} failed ====");
            // Throw instead of exiting so TestRunner.RunAll reports this like any
            // other suite failure, with the main summary line intact.
            if (_fail > 0) throw new Exception($"{_fail} win asserts failed");
        }

        static void Test(string name, Action fn)
        {
            try
            {
                fn();
                _pass++;
                Console.WriteLine("  ok   " + name);
            }
            catch (Exception e)
            {
                _fail++;
                Console.WriteLine("  FAIL " + name + ": " + e.Message);
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static Item FreshAmulet(Game g)
        {
            return new Item(Game.QuestAmuletDef, g.Rng, g.NextUid()) { Identified = true };
        }

        static void AmuletAtBottom()
        {
            var g = new Game(424242);
            g.DescendTo(Game.QuestBranch, g.QuestBottomDepth());
            Assert(g.IsQuestBottom(), "should be on the quest bottom");
            Assert(g.AmuletOnGround(), "no amulet of Yendor on the bottom level");
            Assert(!g.HasAmulet(), "fresh run should not carry the amulet");
        }

        static void VictoryWithAmulet()
        {
            var g = new Game(777001);
            g.DescendTo(Game.QuestBranch, g.QuestBottomDepth());
            g.Player.Inventory.Add(FreshAmulet(g));
            Assert(g.HasAmulet(), "player should have the amulet");
            g.LeaveToOverworld();
            Assert(g.Mode == GameMode.Won, "expected Won, got " + g.Mode);
            Assert(g.UiState.Active == Panel.Win, "expected the Win panel, got " + g.UiState.Active);
        }

        static void NoVictoryWithoutAmulet()
        {
            var g = new Game(777002);
            Assert(!g.HasAmulet(), "fresh run should not have the amulet");
            g.LeaveToOverworld();
            Assert(g.Mode == GameMode.Overworld, "expected Overworld, got " + g.Mode);
        }

        static void VictoryWithWornAmulet()
        {
            var g = new Game(777003);
            g.Player.Amulet = FreshAmulet(g);
            Assert(g.HasAmulet(), "worn amulet should count");
            g.LeaveToOverworld();
            Assert(g.Mode == GameMode.Won, "expected Won with worn amulet, got " + g.Mode);
        }

        static void FallbackReplacesMissing()
        {
            var g = new Game(777004);
            g.DescendTo(Game.QuestBranch, g.QuestBottomDepth());
            Assert(g.AmuletOnGround(), "setup: amulet should start on the ground");
            // Simulate a stale level generated before the quest existed.
            for (int y = 0; y < g.Map.H; y++)
                for (int x = 0; x < g.Map.W; x++)
                    GroundItems.RemoveCell(g.Map.Number, x, y);
            Assert(!g.AmuletOnGround(), "setup: ground should now be empty");
            g.EnsureQuestAmulet();
            Assert(g.AmuletOnGround(), "fallback should replace the missing amulet");
        }
    }
}
