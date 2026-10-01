using System;
using Ossuary.Core;
using Ossuary.Core.Gen;

namespace Ossuary.Tools
{
    /// <summary>
    /// Console entry point for running the game headlessly.
    ///
    /// Ossuary.Core deliberately has no UnityEngine references, so the entire
    /// simulation and its whole ASCII interface can be exercised without starting
    /// the editor. This is what makes the test suite and the frame dumps possible
    /// even while the editor holds the project lock.
    /// </summary>
    public static class HeadlessMain
    {
        public static int Main(string[] args)
        {
            // The UI is box-drawing and block characters; without UTF-8 the console
            // replaces every one of them and the dump becomes useless for review.
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

            string mode = args.Length > 0 ? args[0] : "test";
            switch (mode)
            {
                case "test": return RunTests(args);
                case "dump": return RunDumps(args);
                case "soak": return RunSoak(args);
                default:
                    Console.Error.WriteLine("unknown mode: " + mode);
                    Console.Error.WriteLine("modes: test | dump | soak");
                    return 2;
            }
        }

        static int RunTests(string[] args)
        {
            Ossuary.Tests.TestRunner.RunAll();
            return 0;
        }

        static int RunSoak(string[] args)
        {
            int seeds = args.Length > 1 ? int.Parse(args[1]) : 50;
            int turns = args.Length > 2 ? int.Parse(args[2]) : 500;
            Ossuary.Tests.TestRunner.RunSoakOnly(seeds, turns);
            return 0;
        }

        static int RunDumps(string[] args)
        {
            bool level = Array.IndexOf(args, "level") >= 0;
            bool over = Array.IndexOf(args, "overworld") >= 0;
            bool panels = Array.IndexOf(args, "panels") >= 0;
            if (!level && !over && !panels) { level = over = panels = true; }

            if (level) DumpLevel();
            if (over) DumpOverworld();
            if (panels) DumpPanels();
            return 0;
        }

        // -------------------------------------------------------------- dumps

        static void DumpLevel()
        {
            foreach (var style in new[] { LevelStyle.Rooms, LevelStyle.Cave, LevelStyle.Maze, LevelStyle.Barracks, LevelStyle.Warrens })
            {
                var rng = new Rng(2024);
                var opts = new GenOptions
                {
                    Width = 79, Height = 25, Style = style, WallStyle = 0, MaxRooms = 9,
                    AllowStairsUp = true, AllowStairsDown = true,
                };
                var map = DungeonGen.Generate(opts, rng, out _, out _);
                Console.WriteLine();
                Console.WriteLine($"===== {style}: {map.W}x{map.H}, {map.CountWalkable()} walkable =====");
                Console.WriteLine(map.ToAscii());
            }
        }

        static void DumpOverworld()
        {
            var game = new Game(777);
            game.LeaveToOverworld();

            var hud = new GameHud(game);
            hud.Ui.Resize(110, 38);
            Console.WriteLine();
            Console.WriteLine("===== OVERWORLD =====");
            Console.WriteLine(hud.Draw().ToAscii());
        }

        static void DumpPanels()
        {
            var game = new Game(31337);
            var hud = new GameHud(game);
            hud.Ui.Resize(110, 36);

            Console.WriteLine();
            Console.WriteLine("===== DUNGEON FRAME (turn 0) =====");
            Console.WriteLine(hud.Draw().ToAscii());

            // Play a little so the map is remembered and monsters have moved.
            var cmd = new Commands(game);
            var rng = game.Rng;
            string[] acts = { "move-n", "move-s", "move-e", "move-w", "move-se", "g", "s", "i" };
            for (int i = 0; i < 120 && game.Mode != GameMode.GameOver; i++)
            {
                try
                {
                    cmd.Execute(acts[rng.Range(0, acts.Length)]);
                    if (game.UiState.IsTargeting) game.ResolveTargeting(game.Player.X, game.Player.Y);
                    if (game.PendingChoice.Active && game.PendingChoice.Items.Count > 0) game.PendingChoice.Clear();
                    if (game.UiState.Active != Panel.None) game.UiState.Active = Panel.None;
                }
                catch (Exception e)
                {
                    Console.WriteLine("!! action threw: " + e.Message);
                    return;
                }
            }

            // Close any panel the bot happened to open, so this frame shows the world.
            game.UiState.Active = Panel.None;
            game.UiRequests.Clear();
            Console.WriteLine();
            Console.WriteLine($"===== DUNGEON FRAME (turn {game.Turn}, depth {game.Depth}, {game.Player.HP}/{game.Player.MaxHP} HP) =====");
            Console.WriteLine(hud.Draw().ToAscii());

            foreach (Panel p in new[] { Panel.Inventory, Panel.Character, Panel.Help, Panel.History, Panel.Discoveries })
            {
                game.UiState.Active = p;
                Console.WriteLine();
                Console.WriteLine($"===== PANEL: {p} =====");
                Console.WriteLine(hud.Draw().ToAscii());
            }
            game.UiState.Active = Panel.None;

            Console.WriteLine();
            Console.WriteLine("===== MESSAGE LOG (last 24) =====");
            int start = Math.Max(0, game.Log.Count - 24);
            for (int i = start; i < game.Log.Count; i++)
            {
                var m = game.Log[i];
                Console.WriteLine($"[{m.Turn,5}] {m.Text}");
            }
        }
    }
}