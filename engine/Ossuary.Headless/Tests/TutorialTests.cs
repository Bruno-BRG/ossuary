using System;
using System.Collections.Generic;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Tests
{
    /// <summary>
    /// First-time hints (docs/superpowers/specs/2026-10-14-tutorials-design.md): each shows once per install,
    /// never while off, closes only on its own action, and changes neither the turn, the RNG nor the hero.
    /// </summary>
    public static class TutorialTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            Test("a hint shows once, and is recorded for the host", ShowsOnce);
            Test("with tutorials off no hint ever shows", OffNeverShows);
            Test("a hint the install has seen stays quiet", SeenStaysQuiet);
            Test("each hint closes on its own action only", DoneEvents);
            Test("the opening on the overworld shows no dungeon hint", OpeningIsQuiet);
            Test("a level-up shows the perk hint, a human's starting perk does not", LevelUp);
            Test("a purchase in a shop shows the shop card", ShopPurchase);
            Test("hints change neither the turn, the RNG nor the hero", ReplayNeutral);
            Test("every hint and first-run string has its Portuguese", Portuguese);
            Console.WriteLine($"==== tutorials: {_pass} passed, {_fail} failed ====");
            if (_fail > 0) throw new Exception($"{_fail} tutorial asserts failed");
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

        static void ShowsOnce()
        {
            var g = new Game(1001) { TutorialsOn = true, SeenHints = new HashSet<string>() };
            g.EndPlayerTurn();   // the first turn underground
            Assert(g.UiState.Hint == "dungeon", "the first turn underground shows the dungeon hint");
            Assert(g.SeenHints.Contains("dungeon") && g.NewlySeen.Contains("dungeon"), "and records it as seen, for the host to write");
            g.HintDone("explore");
            Assert(g.UiState.Hint == null, "its own action closes the card");
            g.EndPlayerTurn();
            Assert(g.UiState.Hint == null, "a hint seen once is not shown again in the same run");
        }

        static void OffNeverShows()
        {
            var g = new Game(1002) { TutorialsOn = false };
            for (int i = 0; i < 3; i++) g.EndPlayerTurn();
            g.ShowHint("dungeon");
            g.ShowHint("wounded");
            Assert(g.UiState.Hint == null, "with tutorials off no card opens");
            Assert(g.SeenHints.Count == 0 && g.NewlySeen.Count == 0, "and nothing is recorded");
        }

        static void SeenStaysQuiet()
        {
            var g = new Game(1003) { TutorialsOn = true, SeenHints = new HashSet<string> { "dungeon" } };
            g.EndPlayerTurn();
            Assert(g.UiState.Hint == null, "a hint already in the install's seen set does not open");
            Assert(g.NewlySeen.Count == 0, "and is not recorded again");
        }

        static void DoneEvents()
        {
            var g = new Game(1004);
            g.ShowHint("dungeon");
            g.HintDone("bandage");
            Assert(g.UiState.Hint == "dungeon", "an unrelated action leaves the card up");
            new Commands(g).Execute("explore");
            Assert(g.UiState.Hint == null, "auto-explore closes the dungeon card");

            g.ShowHint("shop");
            new Commands(g).Execute("examine");
            Assert(g.UiState.Hint == null, "examining an item closes the shop card");

            foreach (var (id, done) in new[] { ("crafting", "recipes"), ("casting", "spells"), ("town", "talk"),
                                               ("gathering", "craft"), ("altar", "sacrifice"), ("jobs", "journal") })
            {
                g.ShowHint(id);
                g.HintDone(done);
                Assert(g.UiState.Hint == null, id + " closes on " + done);
            }

            g.ShowHint("wounded");
            var band = g.Player.Inventory.Find(i => i.Def.Name == "bandage");
            g.Player.Wounds.Add(new Wound { Part = "torso", Kind = PartKind.Torso, Severity = 2, Worst = 2, Edged = true, Bleed = 5, HealIn = 250 });
            Assert(g.ApplyBandage(band), "test setup: a bandage binds the wound");
            Assert(g.UiState.Hint == null, "binding a wound closes the wounded card");
        }

        static void OpeningIsQuiet()
        {
            var g = new Game(1005) { TutorialsOn = true, SeenHints = new HashSet<string>() };
            g.BeginAtOverworld();
            Assert(g.UiState.Hint == null && !g.SeenHints.Contains("dungeon"), "the game's first level is built underground, but no card opens for it");
        }

        static void LevelUp()
        {
            var g = new Game(1006) { TutorialsOn = true, SeenHints = new HashSet<string> { "dungeon" } };
            g.Player.PendingAdvances = 1;   // a human starts with one pending at level 1
            g.EndPlayerTurn();
            Assert(g.UiState.Hint == null, "a starting perk is not a level-up");
            g.Player.Level = 2;
            g.EndPlayerTurn();
            Assert(g.UiState.Hint == "levelup", "a pending perk after level 1 shows the perk hint");
        }

        static void ShopPurchase()
        {
            var g = new Game(1008) { TutorialsOn = true, SeenHints = new HashSet<string>() };
            g.EnterTown("Ravensgate");
            g.HintDone("talk");   // the town card opens on arrival; one card at a time
            g.OpenShop(g.Town.Shops[0]);
            var shop = g.CurrentShop;
            var item = shop.Stock.Find(i => i.Def.Kind != ItemKind.Gold);
            Assert(item != null, "test setup: the shop has something to sell");
            g.Player.Gold = 100000;
            Assert(g.BuyFromShop(shop, item), "test setup: the purchase goes through");
            Assert(g.UiState.Hint == "shop", "buying shows the shop card");
        }

        /// <summary>The same commands with hints on and off end at the same turn, hero and random stream.</summary>
        static void ReplayNeutral()
        {
            var (turnOn, hpOn, xOn, yOn, rngOn) = Play(true);
            var (turnOff, hpOff, xOff, yOff, rngOff) = Play(false);
            Assert(turnOn == turnOff, "the same turn with hints on and off");
            Assert(hpOn == hpOff && xOn == xOff && yOn == yOff, "the same hero with hints on and off");
            Assert(rngOn == rngOff, "the same random stream with hints on and off");
        }

        static (int turn, int hp, int x, int y, ulong rng) Play(bool tutorials)
        {
            var g = new Game(1007) { TutorialsOn = tutorials, SeenHints = new HashSet<string>() };
            var cmd = new Commands(g);
            for (int i = 0; i < 40; i++) cmd.Execute("explore");
            return (g.Turn, g.Player.HP, g.Player.X, g.Player.Y, g.Rng.NextULong());
        }

        static void Portuguese()
        {
            var before = Loc.Current;
            Loc.Current = Lang.Pt;
            try
            {
                foreach (var h in Tutorials.All) Assert(Loc.T(h.Text) != h.Text, "Portuguese for the " + h.Id + " card");
                foreach (string s in new[] { "[Esc] close", "Tutorials", "Reset tutorials", "First steps", "Have you played before?",
                                             "Yes, I already know the game", "No, show me the hints", "You can change this later in F2.",
                                             "Shows a short card the first time each system comes up.", "Every hint can show again.",
                                             "Tutorials will show again.", "On", "Off", "↑↓ select   Enter choose" })
                    Assert(Loc.T(s) != s, "Portuguese for \"" + s + "\"");
            }
            finally { Loc.Current = before; }
        }
    }
}
