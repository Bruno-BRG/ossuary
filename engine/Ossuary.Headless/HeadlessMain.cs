using System;
using Ossuary.Core;
using Ossuary.Core.Items;
using Ossuary.Core.Gen;

namespace Ossuary.Tools
{
    /// <summary>
    /// Console entry point for running the game headlessly.
    ///
    /// Ossuary.Core is an independent library, so the entire
    /// simulation and its whole ASCII interface can be exercised from a console.
    /// The test suite, frame dumps and soak runs use the same game implementation.
    /// They run independently of the desktop window.
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
                case "balance": return BalanceBot.Report(args);
                default:
                    Console.Error.WriteLine("unknown mode: " + mode);
                    Console.Error.WriteLine("modes: test | dump | soak | balance");
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
            bool town = Array.IndexOf(args, "town") >= 0;
            bool create = Array.IndexOf(args, "create") >= 0;
            if (!level && !over && !panels && !town && !create) { level = over = panels = town = create = true; }

            if (level) DumpLevel();
            if (over) DumpOverworld();
            if (panels) DumpPanels();
            if (town) DumpTown();
            if (create) DumpCreate();
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

        // The town as the player first sees it, then the whole map, so a visibility or
        // layout problem is obvious without a display.
        static void DumpTown()
        {
            var game = new Game(31337);
            var hud = new GameHud(game);
            hud.Ui.Resize(110, 36);
            game.LeaveToOverworld();
            game.EnterTown("Ravensgate");

            Console.WriteLine();
            Console.WriteLine("===== TOWN: first frame =====");
            Console.WriteLine(hud.Draw().ToAscii());
            Console.WriteLine("===== TOWN: full map (" + game.Map.W + "x" + game.Map.H + ", player at " + game.Player.X + "," + game.Player.Y + ") =====");
            Console.WriteLine(game.Map.ToAscii(false, game.Player.X, game.Player.Y));

            // Inside the buildings: every floor of the ones worth looking at, people drawn on top.
            foreach (var b in game.Town.Buildings)
            {
                if (b.Kind == BuildingKind.Cottage || b.Kind == BuildingKind.Stall) continue;
                for (int z = b.Up; z >= -b.Down; z--)
                {
                    var m = game.Town.FloorMap(z);
                    Console.WriteLine();
                    Console.WriteLine($"----- {b.Name} ({b.Kind}) floor {z}  services={b.Services} keeper={(b.Keeper != null ? b.Keeper.Name + "/" + b.Keeper.Role : "-")} -----");
                    for (int y = b.Y; y < b.Y + b.H; y++)
                    {
                        var row = new System.Text.StringBuilder();
                        for (int x = b.X; x < b.X + b.W; x++)
                        {
                            char c = m.GlyphOf(x, y);
                            foreach (var n in game.Town.Npcs) if (n.Floor == z && n.X == x && n.Y == y) c = n.Glyph == '@' ? '@' : n.Glyph;
                            row.Append(c);
                        }
                        Console.WriteLine(row.ToString());
                    }
                }
            }

            // The service menu of the first building that has one.
            foreach (var b in game.Town.Buildings)
            {
                if (b.Keeper == null || b.Services == Service.None || b.Kind == BuildingKind.Guild) continue;
                game.Player.Gold = 120;
                game.TalkBuilding = b; game.Talking = b.Keeper; game.ServiceNote = TownText.Greeting(b.Keeper);
                game.UiState.Active = Panel.Service; game.UiState.ServiceIndex = 1;
                Console.WriteLine();
                Console.WriteLine("===== SERVICE PANEL: " + b.Name + " =====");
                Console.WriteLine(hud.Draw().ToAscii());
                break;
            }
        }

        // The character-creation screen at each step, so its layout can be reviewed without a display.
        static void DumpCreate()
        {
            var game = new Game(31337);
            var hud = new GameHud(game);
            hud.Ui.Resize(110, 36);
            var c = game.UiState.Create;
            game.UiState.Active = Panel.Create;
            c.Name = "Morgana"; c.RaceIndex = 2; c.RoleIndex = 6;
            foreach (var step in new[] { CreateStep.Name, CreateStep.Race, CreateStep.Role, CreateStep.Confirm })
            {
                c.Step = step;
                Console.WriteLine();
                Console.WriteLine("===== CREATE: " + step + " =====");
                Console.WriteLine(hud.Draw().ToAscii());
            }
            var hero = Game.NewHero(31337, "Morgana", "ashen", "necromancer");
            var hud2 = new GameHud(hero); hud2.Ui.Resize(110, 36);
            hero.UiState.Active = Panel.Character;
            Console.WriteLine();
            Console.WriteLine("===== CHARACTER: ashen necromancer =====");
            Console.WriteLine(hud2.Draw().ToAscii());
            hero.Player.Spells.AddRange(new[] { "frost-ray", "sleep", "blink" });
            hero.Player.Mp -= 3;
            hero.UiState.Active = Panel.Spells; hero.UiState.SpellIndex = 1;
            Console.WriteLine();
            Console.WriteLine("===== SPELLS: ashen necromancer =====");
            Console.WriteLine(hud2.Draw().ToAscii());
            var knight = Game.NewHero(31337, "Aldric", "dwarf", "paladin");
            knight.Player.Level = 6; knight.Player.PendingAdvances = 2; knight.Player.Vigor -= 4;
            var hud3 = new GameHud(knight); hud3.Ui.Resize(110, 36);
            knight.UiState.Active = Panel.Abilities;
            Console.WriteLine();
            Console.WriteLine("===== ABILITIES: dwarf paladin =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            knight.UiState.Active = Panel.Advance; knight.UiState.AdvanceIndex = 9;
            Console.WriteLine();
            Console.WriteLine("===== ADVANCEMENT: dwarf paladin =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            var grng = new Rng(4);
            foreach (var d in new[] { Catalogue.Helms[3], Catalogue.Gloves[1], Catalogue.Boots[1], Catalogue.Cloaks[1] })
            {
                var piece = new Item(d, grng, 1) { Rarity = Rarity.Rare, Prefix = "sturdy", Suffix = "of-the-bear", Enchant = 2, Identified = true };
                knight.Player.Inventory.Add(piece); knight.Player.Wear(piece);
            }
            knight.Player.Inventory.Add(Artifacts.Create(Artifacts.Find("crown-drowned-king"), grng, 2));
            knight.Player.Inventory.Add(new Item(Catalogue.Weapons[2], grng, 3) { Rarity = Rarity.Magic, Prefix = "flaming" });
            knight.UiState.Active = Panel.Inventory;
            Console.WriteLine();
            Console.WriteLine("===== INVENTORY: geared paladin =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            knight.UiState.Active = Panel.Character;
            Console.WriteLine();
            Console.WriteLine("===== CHARACTER: geared paladin =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            knight.UiState.Active = Panel.None;
            Console.WriteLine();
            Console.WriteLine("===== SIDEBAR: geared paladin =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            for (int gy = 1; gy < 60; gy++) { if (Ossuary.Core.Entities.Gods.AtAltar(knight.Map.Number, 5, gy).Id == "khorr") { knight.OpenAltar(5, gy); break; } }
            knight.Player.God = "aurel"; knight.Player.Piety = 63; knight.Player.PrayerTimer = 212;
            knight.UiState.Active = Panel.Altar;
            Console.WriteLine();
            Console.WriteLine("===== ALTAR: stranger's altar =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            knight.Player.God = "khorr"; knight.UiState.AltarIndex = 1;
            Console.WriteLine();
            Console.WriteLine("===== ALTAR: own god =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            knight.UiState.Runs = new System.Collections.Generic.List<RunRecord>
            {
                new RunRecord { Name = "Mara", Race = "Dwarf", Role = "Fighter", Title = "Warrior", Level = 7, Outcome = "died", Cause = "a giant rat", Branch = "The Mines", Depth = 4, MaxDepth = 4, Turns = 2210, Kills = 31, Seed = "123456789", Date = "2026-10-02 18-01-22", Score = 1010 },
                new RunRecord { Name = "Ilse", Race = "Elf", Role = "Wizard", Title = "Mage", Level = 12, Outcome = "won", Branch = "The Dungeons", Depth = 1, MaxDepth = 12, Turns = 9001, Kills = 120, Seed = "42", Date = "2026-10-01 20-10-00", Score = 7400 },
            };
            knight.UiState.Active = Panel.Runs; knight.UiState.RunsIndex = 0;
            Console.WriteLine();
            Console.WriteLine("===== PAST RUNS =====");
            Console.WriteLine(hud3.Draw().ToAscii());
            Console.WriteLine();
            Console.WriteLine("===== MORGUE FILE =====");
            knight.Player.HP = 0; knight.CheckDeath();
            Console.WriteLine(Morgue.Text(knight, Morgue.Summarize(knight)));
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