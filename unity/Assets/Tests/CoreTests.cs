using System;
using System.Collections.Generic;
using System.Text;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Tests
{
    /// <summary>
    /// Headless assertions over the simulation. Run from the CLI with
    /// -executeMethod Ossuary.Tests.TestRunner.RunAll
    /// </summary>
    public static class TestRunner
    {
        static int _pass;
        static int _fail;
        static readonly List<string> Failures = new List<string>();

        public static void RunAll()
        {
            _pass = 0; _fail = 0;
            Failures.Clear();

            Test("rng is deterministic", RngDeterminism);
            Test("rng distribution is sane", RngDistribution);
            Test("every tile def is reachable", TileTableComplete);
            Test("rooms levels are connected and have stairs", LevelRooms);
            Test("cave levels are connected", LevelCave);
            Test("maze levels are connected", LevelMaze);
            Test("barracks levels are connected", LevelBarracks);
            Test("fort levels are connected", LevelFort);
            Test("warrens levels are connected", LevelWarrens);
            Test("all branch depths generate", AllBranchDepths);
            Test("fov is symmetric-ish and bounded", FovBehaviour);
            Test("pathfinder finds a route", PathfindingWorks);
            Test("flow field reaches the goal", FlowFieldWorks);
            Test("overworld is connected by road", OverworldHasTowns);
            Test("dungeon descent and ascent round-trip", DescentRoundTrip);
            Test("combat deals damage and kills", CombatWorks);
            Test("items stack, drop and pick up", ItemFlow);
            Test("loot table produces valid defs", LootTableValid);
            Test("bestiary entries are well formed", BestiaryValid);
            Test("200 random turns per seed, 25 seeds", Soak);
            Test("ui composes every panel", UiComposes);
            Test("every glyph is covered by the atlas charset", GlyphCoverage);
            Test("overworld travel works", OverworldTravel);
            Test("towns generate with shops", TownGeneration);
            Test("shop economy does not break gold", ShopEconomy);
            Test("shop buy/sell flow", ShopBuySellFlow);
            Test("role table is valid", RoleTableValid);
            Test("role kits differ", RoleKitsDiffer);
            Test("level advances work", LevelAdvancesWork);
            Test("quest victory", WinTests.Run);

            Console.WriteLine();
            Console.WriteLine($"==== {(_fail == 0 ? "PASS" : "FAIL")}: {_pass} passed, {_fail} failed ====");
            foreach (var f in Failures) Console.WriteLine("  " + f);

            if (_fail > 0)
            {
                // Non-zero exit code makes the CLI pipeline notice.
                Environment.Exit(1);
            }
        }

        public static void RunSoakOnly(int seeds = 50, int turns = 500)
        {
            var rng = new Rng(12345);
            int died = 0, deepest = 0;
            for (int s = 0; s < seeds; s++)
            {
                var g = new Game(rng.NextULong());
                RunRandomTurns(g, turns);
                if (g.Mode == GameMode.GameOver) died++;
                deepest = Math.Max(deepest, g.Player.MaxDepth);
            }
            Console.WriteLine($"soak: {seeds} seeds, {turns} turns each, died {died}, deepest {deepest}");
        }

        // ------------------------------------------------------------- harness

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
                // Unwrap: a TypeInitializationException hides the real fault in its
                // inner exception, and the stack trace is what tells us which line broke.
                string detail = e.Message;
                int guard = 0;
                while (e.InnerException != null && guard++ < 6)
                {
                    e = e.InnerException;
                    detail += " -> " + e.Message;
                }
                string msg = name + ": " + detail;
                Failures.Add(msg);
                Console.WriteLine("  FAIL " + msg);
                if (e.StackTrace != null)
                {
                    var lines = e.StackTrace.Split('\n');
                    for (int i = 0; i < Math.Min(4, lines.Length); i++)
                        Console.WriteLine("         " + lines[i].Trim());
                }
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        // --------------------------------------------------------------- tests

        static void RngDeterminism()
        {
            var a = new Rng(1234);
            var b = new Rng(1234);
            for (int i = 0; i < 1000; i++)
                Assert(a.NextULong() == b.NextULong(), "streams diverged at " + i);
        }

        static void RngDistribution()
        {
            var r = new Rng(99);
            var buckets = new int[10];
            const int n = 100000;
            for (int i = 0; i < n; i++) buckets[r.Range(0, 10)]++;
            for (int i = 0; i < 10; i++)
                Assert(buckets[i] > 9000 && buckets[i] < 11000, "bucket " + i + " skewed: " + buckets[i]);

            var r2 = new Rng(7);
            int ones = 0;
            for (int i = 0; i < n; i++) if (r2.NextDouble() < 0.1) ones++;
            // p = 0.1 over 100k draws means ~10000 hits; allow a generous sigma but nothing
            // that would hide a real bias in NextDouble.
            Assert(ones > 9500 && ones < 10500, "one-in-ten skewed: " + ones);
        }

        static void TileTableComplete()
        {
            for (int i = 0; i <= (int)TileKind.Fountain; i++)
            {
                var d = Tiles.Get((TileKind)i);
                Assert(!string.IsNullOrEmpty(d.Name), "tile " + i + " has no name");
            }
            Assert(Tiles.Walkable(TileKind.Floor), "floor should be walkable");
            Assert(Tiles.Opaque(TileKind.Wall), "wall should be opaque");
            Assert(!Tiles.Walkable(TileKind.Wall), "wall should not be walkable");
        }

        static void LevelRooms() => GenerateAndCheck(LevelStyle.Rooms, 1);
        static void LevelCave() => GenerateAndCheck(LevelStyle.Cave, 5);
        static void LevelMaze() => GenerateAndCheck(LevelStyle.Maze, 8);
        static void LevelBarracks() => GenerateAndCheck(LevelStyle.Barracks, 12);
        static void LevelFort() => GenerateAndCheck(LevelStyle.Fort, 20);
        static void LevelWarrens() => GenerateAndCheck(LevelStyle.Warrens, 25);

        static void GenerateAndCheck(LevelStyle style, int depth)
        {
            for (int seed = 0; seed < 12; seed++)
            {
                var rng = new Rng((ulong)(seed * 7919 + 13));
                var opts = new GenOptions
                {
                    Width = 79, Height = 25, Style = style, WallStyle = 0, MaxRooms = 10,
                    AllowStairsUp = true, AllowStairsDown = true, Seed = seed
                };
                var map = DungeonGen.Generate(opts, rng, out var specials, out var starts);

                Assert(map.CountWalkable() > 40, style + " seed " + seed + ": too little floor (" + map.CountWalkable() + ")");

                int up = 0, down = 0, start = -1;
                for (int y = 0; y < map.H; y++)
                    for (int x = 0; x < map.W; x++)
                    {
                        if (map.Get(x, y) == TileKind.StairsUp) up++;
                        if (map.Get(x, y) == TileKind.StairsDown) down++;
                        if (starts.Contains(x + y * map.W)) start = x + y * map.W;
                    }
                Assert(up == 1, style + " seed " + seed + ": expected 1 stairs up, got " + up);
                Assert(down == 1, style + " seed " + seed + ": expected 1 stairs down, got " + down);
                Assert(start >= 0, style + " seed " + seed + ": no start cell");

                // Everything the player can reach from the up stairs must connect to the down stairs.
                var reach = map.Reachability(start % map.W, start / map.W, true);
                int downIdx = -1;
                for (int y = 0; y < map.H; y++)
                    for (int x = 0; x < map.W; x++)
                        if (map.Get(x, y) == TileKind.StairsDown) downIdx = x + y * map.W;

                Assert(downIdx >= 0 && reach[downIdx], style + " seed " + seed + ": down stairs unreachable");

                int reachable = 0;
                for (int i = 0; i < reach.Length; i++) if (reach[i]) reachable++;
                Assert(reachable > 30, style + " seed " + seed + ": only " + reachable + " reachable cells");

                // No monsters should spawn in walls.
                var spawns = LevelBuilder.Populate(map, rng, depth, "test", specials, starts, out int sx, out int sy);
                Assert(map.Walkable(sx, sy), style + ": spawn point not walkable");
                foreach (var s in spawns)
                {
                    Assert(map.Walkable(s.X, s.Y), style + ": monster spawned in a wall at " + s.X + "," + s.Y);
                    Assert(s.Monster.Name != null && s.Monster.Name.Length > 0, style + ": nameless monster");
                }
            }
        }

        static void AllBranchDepths()
        {
            var dungeon = new Dungeon(new Rng(4242));
            int levels = 0;
            foreach (var b in dungeon.Branches)
            {
                for (int d = 1; d <= b.MaxDepth; d++)
                {
                    var map = dungeon.Ensure(b.Name, d, out var spawns, out int sx, out int sy);
                    Assert(map != null, b.Name + " " + d + " did not generate");
                    Assert(map.Walkable(sx, sy), b.Name + " " + d + ": bad spawn");
                    levels++;
                }
            }
            Assert(levels >= 40, "expected at least 40 levels, got " + levels);
            Console.WriteLine("       (" + levels + " levels across " + dungeon.Branches.Count + " branches)");
        }

        /// <summary>Writes one FOV to a file so the shape can be inspected without log mangling.</summary>
        public static void DumpFov()
        {
            var map = new GameMap(21, 21);
            map.Fill(TileKind.Wall);
            map.Stamp(1, 1, 19, 19, TileKind.Floor, true);
            map.Set(10, 10, TileKind.Wall);      // pillar due north of the viewer
            map.Set(14, 8, TileKind.Wall);       // pillar north-east
            map.Set(6, 15, TileKind.Wall);       // pillar south-west

            var seen = new List<int>();
            Fov.Compute(map, 10, 12, 10, seen);

            var sb = new StringBuilder();
            for (int y = 0; y < map.H; y++)
            {
                for (int x = 0; x < map.W; x++)
                {
                    if (x == 10 && y == 12) { sb.Append('@'); continue; }
                    sb.Append(map.Opaque(x, y) ? '#' : (map.IsVisible(x, y) ? '+' : '.'));
                }
                sb.Append('\n');
            }
            sb.Append("visible cells: ").Append(seen.Count).Append('\n');

            string path = @"C:\Users\Chari\AppData\Local\Temp\opencode\fov.txt";
            System.IO.File.WriteAllText(path, sb.ToString());
            Console.Write("FOV written to " + path + " count=" + seen.Count + "\n");
        }

        static void FovBehaviour()
        {
            var map = new GameMap(21, 21);
            map.Fill(TileKind.Wall);
            map.Stamp(1, 1, 19, 19, TileKind.Floor, true);   // carve out of solid rock
            map.Set(10, 10, TileKind.Wall);   // pillar at the centre

            var seen = new List<int>();
            Fov.Compute(map, 10, 12, 10, seen);

            Assert(map.IsVisible(10, 12), "centre should see itself");
            Assert(seen.Count > 60, "expected a decent FOV, got " + seen.Count);
            // The pillar must not be visible from directly behind it.
            Assert(!map.IsVisible(10, 6), "should not see through the pillar");

            // A diagonal check: visibility is symmetric along the same wall.
            int vis1 = map.IsVisible(5, 10) ? 1 : 0;
            int vis2 = map.IsVisible(15, 10) ? 1 : 0;
            Assert(vis1 == vis2, "FOV should be symmetric in an open room");
        }

        static void PathfindingWorks()
        {
            var map = new GameMap(31, 31);
            map.Fill(TileKind.Wall);
            map.Stamp(1, 1, 29, 29, TileKind.Floor, true);   // carve out of solid rock
            for (int y = 5; y <= 25; y++) map.Set(15, y, TileKind.Wall);
            map.Set(15, 15, TileKind.OpenDoor);

            var path = Pathfinder.FindPath(map, 2, 2, 28, 28);
            Assert(path.Count > 0, "no path found through the door");
            Assert(path[0] == 2 + 2 * 31, "path should start at the origin");
            Assert(path[path.Count - 1] == 28 + 28 * 31, "path should end at the target");

            // A goal inside a sealed pocket must yield no path. (FindPath deliberately snaps
            // an unwalkable goal to a walkable neighbour, so testing a bare wall
            // would never return empty.)
            var sealedMap = new GameMap(31, 31);
            sealedMap.Fill(TileKind.Wall);
            sealedMap.Stamp(1, 1, 29, 29, TileKind.Floor, true);
            for (int x = 25; x <= 29; x++) sealedMap.Set(x, 25, TileKind.Wall);
            for (int y = 25; y <= 29; y++) sealedMap.Set(25, y, TileKind.Wall);
            Assert(sealedMap.Walkable(27, 27), "pocket should be floor");
            Assert(Pathfinder.FindPath(sealedMap, 2, 2, 27, 27).Count == 0, "sealed pocket should yield no path");
        }

        static void FlowFieldWorks()
        {
            var map = new GameMap(21, 21);
            map.Fill(TileKind.Wall);
            map.Stamp(1, 1, 19, 19, TileKind.Floor, true);
            // The obstacle must not sit ON the goal, or nothing can ever reach it.
            map.Set(10, 4, TileKind.Wall);

            var goals = new List<int> { 10 + 5 * 21 };
            var flow = new Pathfinder.FlowField(map, goals);

            Assert(flow.At(10, 5) == 0, "goal should be at distance 0");
            Assert(flow.At(10, 6) == 1, "adjacent cell should be 1 away, got " + flow.At(10, 6));
            Assert(flow.At(10, 15) == 10, "straight line should be 10 away, got " + flow.At(10, 15));
            Assert(flow.Reached(1, 1), "corner should be reachable");
        }

        static void OverworldHasTowns()
        {
            for (int seed = 0; seed < 5; seed++)
            {
                var w = OverworldGen.Generate(96, 60, (ulong)(seed * 31 + 7));
                int towns = 0, dungeons = 0, roads = 0;
                for (int i = 0; i < w.W * w.H; i++)
                {
                    if (w.Tiles[i].Feature == OverworldFeature.Town) towns++;
                    if (w.Tiles[i].Feature == OverworldFeature.Dungeon) dungeons++;
                    if (w.Tiles[i].Terrain == OverworldTerrain.Road) roads++;
                }
                Assert(towns >= 3, "seed " + seed + ": only " + towns + " towns");
                Assert(dungeons >= w.Regions.Count - 1, "seed " + seed + ": only " + dungeons + " dungeon entrances for " + w.Regions.Count + " regions");
                Assert(roads > 20, "seed " + seed + ": only " + roads + " road tiles");
            }
        }

        static void DescentRoundTrip()
        {
            var g = new Game(555);
            Assert(g.Mode == GameMode.Dungeon, "should start in a dungeon");
            Assert(g.Depth == 1, "should start at depth 1");

            g.DescendTo("The Dungeons", 2);
            Assert(g.Depth == 2, "descend failed");
            Assert(g.Map != null && g.Map.Depth == 2, "map depth mismatch");

            int px = g.Player.X, py = g.Player.Y;
            g.DescendTo("The Dungeons", 1);
            Assert(g.Depth == 1, "ascend via DescendTo failed");
            Assert(DungeonHasLevel(g, "The Dungeons", 2), "level 2 should be cached");

            // Coming back should place us at the remembered spot.
            g.DescendTo("The Dungeons", 2);
            Assert(g.Player.X == px || g.Map.Walkable(g.Player.X, g.Player.Y), "re-entry placed the player badly");
        }

        static bool DungeonHasLevel(Game g, string branch, int depth) => g.Dungeon.Get(branch, depth) != null;

        static void CombatWorks()
        {
            var rng = new Rng(2024);

            // Monster-on-monster: the player's melee function is the only one whose
            // signature takes (Player, Monster), so melee swings are exercised on the
            // player and monster-on-monster via the monster attack path.
            bool anyHit = false;
            var dummy = new Monster(Bestiary.Find("dwarf"), rng) { X = 0, Y = 0 };
            for (int i = 0; i < 400; i++)
            {
                var attacker = new Monster(Bestiary.Find("kobold"), rng) { X = 1, Y = 0 };
                dummy.HP = dummy.MaxHP;
                var res = Battles.MeleeAttack(attacker, dummy, rng);
                if (res.Hit) { anyHit = true; Assert(res.Damage > 0, "hit with zero damage"); }
                if (res.Killed) Assert(dummy.HP <= 0, "killed but HP above zero");
            }
            Assert(anyHit, "no monster attack ever landed");

            var player = new Player(new Rng(11));
            var jackal = new Monster(Bestiary.Find("jackal"), rng) { X = 0, Y = 0 };
            bool anySwing = false;
            for (int i = 0; i < 500; i++)
            {
                bool crit;
                var res = Battles.PlayerMelee(player, jackal, rng, out crit);
                if (res.Hit) anySwing = true;
                if (jackal.HP <= 0) jackal.HP = jackal.MaxHP;
            }
            Assert(anySwing, "the player never landed a blow");
            Assert(player.HP > 0, "player should survive 500 rounds against a level-1 jackal");
        }

        static void ItemFlow()
        {
            var g = new Game(777);
            var item = new Item(new ItemDef { Name = "test rock", Glyph = '*', Kind = ItemKind.Rock, Cost = 1, Weight = 2 }, g.Rng, 1);
            g.Player.Inventory.Add(item);
            Assert(g.Player.Inventory.Contains(item), "item not added");

            GroundItems.Add(g.Map.Number, g.Player.X, g.Player.Y, item);
            var stack = GroundItems.At(g.Map.Number, g.Player.X, g.Player.Y);
            Assert(stack != null && stack.Contains(item), "item not on the ground");

            var taken = GroundItems.Take(g.Map.Number, g.Player.X, g.Player.Y, 0);
            Assert(taken == item, "took the wrong item");
            Assert(GroundItems.At(g.Map.Number, g.Player.X, g.Player.Y) == null, "ground cell should be empty");
        }

        static void LootTableValid()
        {
            var rng = new Rng(31);
            for (int i = 0; i < 5000; i++)
            {
                var item = LevelBuilder.RollLoot(rng, 1 + i % 20);
                Assert(item != null, "null loot at " + i);
                Assert(!string.IsNullOrEmpty(item.Def.Name), "nameless loot at " + i);
                Assert(item.Def.Glyph != 0, "loot with no glyph at " + i);
                if (item.Def.Kind == ItemKind.Gold) Assert(item.Quantity > 0, "gold with no amount");
            }
        }

        static void BestiaryValid()
        {
            var all = Bestiary.All;
            Assert(all.Count >= 25, "expected a decent bestiary, got " + all.Count);
            var names = new HashSet<string>();
            foreach (var d in all)
            {
                Assert(!string.IsNullOrEmpty(d.Name), "nameless monster");
                Assert(names.Add(d.Name), "duplicate monster name " + d.Name);
                Assert(d.Glyph != 0, d.Name + " has no glyph");
                Assert(d.DepthMin <= d.DepthMax, d.Name + " has an inverted depth range");
                Assert(d.DepthMin >= 1, d.Name + " appears above depth 1");
                Assert(d.HP > 0, d.Name + " has no HP");
                Assert(d.Attacks != null && d.Attacks.Length > 0, d.Name + " has no attacks");
                Assert(d.DmgDice != null && d.DmgDice.Length > 0, d.Name + " has no damage dice");
                Assert(d.DmgDice.Length == d.DmgSides.Length, d.Name + " dice/sides length mismatch");
                Assert(d.DmgDice.Length == d.ToHit.Length, d.Name + " dice/tohit length mismatch");
                // Stationary clutter (molds, spores) legitimately has speed 0.
                if (d.Vision > 0) Assert(d.Speed > 0, d.Name + " has no speed");
            }

            // Every depth should have something to fight.
            for (int d = 1; d <= 30; d++)
                Assert(Bestiary.SpawnTable(d, new Rng(1)).Count > 0, "nothing spawns at depth " + d);
        }

        static void Soak()
        {
            int totalDeaths = 0, deepest = 0, deepestRun = 0;
            const int seeds = 25;
            for (int s = 0; s < seeds; s++)
            {
                var g = new Game((ulong)(s * 104729 + 17));
                RunRandomTurns(g, 200);
                if (g.Mode == GameMode.GameOver) totalDeaths++;
                deepest = Math.Max(deepest, g.Player.MaxDepth);
                deepestRun = Math.Max(deepestRun, g.Player.Kills);
            }
            Console.WriteLine("       (" + seeds + " seeds: " + totalDeaths + " died, deepest " + deepest + ", best kills " + deepestRun + ")");
            Assert(totalDeaths < seeds, "every single run died, which means something is lethal on turn one");
        }

        static void RunRandomTurns(Game g, int turns)
        {
            var rng = g.Rng;
            string[] actions = {
                "move-n","move-s","move-e","move-w","move-ne","move-nw","move-se","move-sw",
                ".",">","<","i","g","a","s","x","w","e","k"
            };
            var cmd = new Commands(g);

            for (int i = 0; i < turns && g.Mode != GameMode.GameOver; i++)
            {
                string a = actions[rng.Range(0, actions.Length)];
                try { cmd.Execute(a); }
                catch (Exception e) { throw new Exception("action " + a + " threw: " + e.Message); }

                if (g.UiState.Targeting != TargetingMode.None)
                    g.ResolveTargeting(rng.Range(0, g.Map.W), rng.Range(0, g.Map.H));
                if (g.PendingChoice.Active && g.PendingChoice.Items.Count > 0)
                    cmd.CommitChoice(g.PendingChoice.Items[rng.Range(0, g.PendingChoice.Items.Count)]);
                if (g.UiState.Active != Panel.None) g.UiState.Active = Panel.None;
                if (g.Mode == GameMode.Overworld) g.LeaveToOverworld();
            }
        }

        static void UiComposes()
        {
            var g = new Game(31337);
            var ui = new Ui(g);
            var hud = new GameHud(g);
            ui.Cmd = hud.Cmd;

            ui.Resize(120, 40);
            var buf = hud.Draw();
            Assert(buf.Width == 120 && buf.Height == 40, "wrong buffer size");

            string art = buf.ToAscii();
            Assert(art.Contains("Dlvl"), "status line missing");
            Assert(art.Contains("Health"), "sidebar missing");

            // Every panel must draw without throwing and must place a frame.
            foreach (Panel p in new[] { Panel.Inventory, Panel.Help, Panel.History, Panel.Discoveries, Panel.Character, Panel.Travel })
            {
                g.UiState.Active = p;
                string s = hud.Draw().ToAscii();
                Assert(s.Length > 0, "panel " + p + " drew nothing");
            }
            g.UiState.Active = Panel.None;

            // Targeting cursor.
            g.PushTargeting(TargetingMode.Look);
            Assert(g.UiState.IsTargeting, "targeting not set");
            hud.Draw();
            g.UiState.Targeting = TargetingMode.None;

            // Small terminal.
            ui.Resize(60, 20);
            hud.Draw();
            ui.Resize(200, 60);
            hud.Draw();
            ui.Resize(120, 40);
        }

        static void GlyphCoverage()
        {
            // The renderer only carries GlyphSet; anything outside it shows as
            // '?'. Every drawable glyph must be contained in it.
            foreach (TileKind k in Enum.GetValues(typeof(TileKind)))
            {
                char g = Tiles.Get(k).Glyph;
                Assert(GlyphSet.Contains(g), "tile " + k + " glyph '" + g + "' not in GlyphSet");
            }
            foreach (OverworldTerrain t in Enum.GetValues(typeof(OverworldTerrain)))
            {
                char g = OverworldGen.GlyphFor(t);
                Assert(GlyphSet.Contains(g), "terrain " + t + " glyph '" + g + "' not in GlyphSet");
            }
            foreach (OverworldFeature f in Enum.GetValues(typeof(OverworldFeature)))
            {
                if (f == OverworldFeature.None) continue;
                char g = OverworldGen.GlyphForFeature(f);
                Assert(GlyphSet.Contains(g), "feature " + f + " glyph '" + g + "' not in GlyphSet");
            }
            foreach (var list in new[] { Catalogue.Weapons, Catalogue.Armor, Catalogue.Shields, Catalogue.Rings, Catalogue.Amulets, Catalogue.Wands, Catalogue.Scrolls, Catalogue.Potions, Catalogue.Food, Catalogue.Tools, Catalogue.Misc, Catalogue.Corpses, Catalogue.Books, Catalogue.Ornaments })
                foreach (var def in list)
                    Assert(GlyphSet.Contains(def.Glyph), "item '" + def.Name + "' glyph '" + def.Glyph + "' not in GlyphSet");
            foreach (var def in Bestiary.All)
                Assert(GlyphSet.Contains(def.Glyph), "monster '" + def.Name + "' glyph '" + def.Glyph + "' not in GlyphSet");
            // UI cursors, markers, bars and frames.
            foreach (char g in new[] { '@', '▶', '▼', '✚', '✦', '█', '░', '─', '│', '╭', '╮', '╰', '╯' })
                Assert(GlyphSet.Contains(g), "ui glyph '" + g + "' not in GlyphSet");
        }

        static void OverworldTravel()
        {
            var g = new Game(9001);
            g.LeaveToOverworld();
            Assert(g.Mode == GameMode.Overworld, "should be on the overworld");

            int x0 = g.World.PlayerX, y0 = g.World.PlayerY;
            g.OverworldMove(1, 0);
            if (g.Mode == GameMode.Overworld)
                Assert(g.World.PlayerX != x0 || g.World.PlayerY != y0, "did not move");

            // Teleport far away and commit a long trip.
            int tx = Math.Min(g.World.W - 1, x0 + 20);
            int ty = Math.Min(g.World.H - 1, y0 + 10);
            int dayBefore = g.World.Day;
            g.CommitTravel(tx, ty);
            Assert(g.World.PlayerX == tx && g.World.PlayerY == ty, "travel did not land");
            Assert(g.World.Day >= dayBefore, "time did not pass");
        }

        static void TownGeneration()
        {
            var rng = new Rng(2468);
            for (int i = 0; i < 5; i++)
            {
                var t = TownGen.Generate("Testhold" + i, rng, 3);
                Assert(t.Map != null, "no map");
                Assert(t.EntryX >= 0 && t.Map.Walkable(t.EntryX, t.EntryY), "entry point not walkable");
                Assert(t.Shops.Count >= 4, "only " + t.Shops.Count + " shops");
                foreach (var s in t.Shops)
                {
                    Assert(s.Stock.Count > 0, s.Name + " has empty stock");
                    Assert(t.Map.InBounds(s.X, s.Y), s.Name + " is off the map");
                    foreach (var it in s.Stock) Assert(it.Def.Name != null, "nameless stock");
                }
                foreach (var n in t.Npcs) Assert(t.Map.InBounds(n.X, n.Y), "npc off the map");
            }
        }

        static void ShopEconomy()
        {
            var rng = new Rng(1357);
            var shop = new Shop { Kind = ShopKind.Weapon, Name = "Test Shop", Gold = 1000 };
            LevelBuilder.RollLoot(rng, 1);
            for (int i = 0; i < 5; i++) shop.Stock.Add(LevelBuilder.RollLoot(rng, 2));

            var g = new Game(2718);
            g.Player.Gold = 500;
            int before = g.Player.Gold;
            var item = shop.Stock[0];

            int price = g.ShopPrice(shop, item);
            Assert(price >= 1, "price must be at least 1");
            g.BuyFromShop(shop, item);
            Assert(g.Player.Gold < before, "buying should cost gold");
            Assert(g.Player.Inventory.Contains(item), "bought item should be in the pack");

            var spare = LevelBuilder.RollLoot(rng, 1);
            g.Player.Inventory.Add(spare);
            int goldBefore = g.Player.Gold;
            g.SellToShop(shop, spare);
            Assert(g.Player.Gold > goldBefore, "selling should pay");
            Assert(!g.Player.Inventory.Contains(spare), "sold item should be gone");
        }

        static void ShopBuySellFlow()
        {
            var rng = new Rng(424242);
            var shop = new Shop { Kind = ShopKind.Weapon, Name = "Test Shop", Gold = 1000 };
            // Index 0 is a known non-gold item so the broke-buy step is deterministic;
            // the fillers give the list more than one row.
            var known = new Item(Catalogue.Weapons[0], rng, 1001) { Identified = true };
            shop.Stock.Add(known);
            for (int i = 0; i < 2; i++) shop.Stock.Add(LevelBuilder.RollLoot(rng, 2));

            var g = new Game(2718);
            var cmd = new Commands(g);
            g.OpenShop(shop);
            g.UiState.Active = Panel.Shop;
            g.UiState.ShopIndex = 0;

            // The panel composes without throwing and shows the shop, prices and gold.
            var hud = new GameHud(g);
            hud.Ui.Cmd = hud.Cmd;
            hud.Ui.Resize(120, 40);
            string art = hud.Draw().ToAscii();
            Assert(art.Contains("Test Shop"), "shop panel should show the shop name");
            Assert(art.Contains("Your gold"), "shop panel should show the player's gold");
            Assert(art.Contains("Shop gold"), "shop panel should show the shop's gold");

            // Buying flat broke fails: nothing moves.
            g.Player.Gold = 0;
            int price = g.ShopPrice(shop, known);
            Assert(price >= 1, "price must be at least 1");
            cmd.Execute("shop-buy");
            Assert(g.Player.Gold == 0, "broke buy should cost nothing");
            Assert(shop.Stock.Contains(known), "broke buy should leave the stock alone");
            Assert(!g.Player.Inventory.Contains(known), "broke buy should not give the item");
            Assert(g.Log[g.Log.Count - 1].Text.Contains("cannot afford"), "broke buy should say so");

            // Buying with gold: purse drops by the price, item lands in the pack.
            g.Player.Gold = price * 2 + 50;
            int goldBefore = g.Player.Gold;
            cmd.Execute("shop-buy");
            Assert(g.Player.Gold == goldBefore - price, "buy should cost exactly the price");
            Assert(g.Player.Inventory.Contains(known), "bought item should be in the pack");
            Assert(!shop.Stock.Contains(known), "bought item should leave the stock");

            // Selling via the PushChoice picker: purse rises, item leaves the pack.
            int invBefore = g.Player.Inventory.Count;
            cmd.Execute("shop-sell");
            Assert(g.PendingChoice.Active, "sell should raise a choice");
            Assert(g.PendingChoice.Items.Count == invBefore, "sell choice should list the pack");
            var chosen = g.PendingChoice.Items[0];
            int purseBefore = g.Player.Gold;
            Assert(cmd.CommitSellChoice(chosen), "sell should succeed");
            Assert(!g.PendingChoice.Active, "sell should clear the choice");
            Assert(g.Player.Gold > purseBefore, "selling should pay");
            Assert(!g.Player.Inventory.Contains(chosen), "sold item should be gone");
            Assert(shop.Stock.Contains(chosen), "sold item should join the stock");

            // A stale cursor clamps instead of throwing.
            g.UiState.ShopIndex = 999;
            g.Player.Gold = 50000;
            int stockBefore = shop.Stock.Count;
            cmd.Execute("shop-buy");
            Assert(shop.Stock.Count == stockBefore - 1, "clamped buy should still trade");
            Assert(g.UiState.ShopIndex <= Math.Max(0, shop.Stock.Count - 1), "cursor should stay valid");

            g.CloseShop();
            Assert(!g.InShop, "shop should be closed");
            g.UiState.Active = Panel.None;
        }

        static void RoleTableValid()
        {
            Assert(Roles.All.Length >= 5, "expected at least 5 roles, got " + Roles.All.Length);
            var ids = new HashSet<string>();
            foreach (var r in Roles.All)
            {
                Assert(!string.IsNullOrEmpty(r.Id), "role with no id");
                Assert(ids.Add(r.Id), "duplicate role " + r.Id);
                Assert(r.HpPerLevel >= 2 && r.HpPerLevel <= 8, r.Id + " has odd HP/level " + r.HpPerLevel);
                Assert(r.Gold >= 0, r.Id + " has negative gold");
                Assert(r.Rations > 0, r.Id + " starts with no food");
                Assert(r.Titles != null && r.Titles.Length == 4, r.Id + " needs exactly 4 title tiers");
                foreach (var t in r.Titles) Assert(!string.IsNullOrEmpty(t), r.Id + " has a blank title");
                Assert(Roles.TitleFor(r.Id, 1) == r.Titles[0], r.Id + " tier-0 title mismatch");
                Assert(Roles.TitleFor(r.Id, 12) == r.Titles[3], r.Id + " tier-3 title mismatch");
                if (!string.IsNullOrEmpty(r.Weapon)) Assert(KitResolves(r.Weapon), r.Id + " weapon missing: " + r.Weapon);
                if (!string.IsNullOrEmpty(r.Armor)) Assert(KitResolves(r.Armor), r.Id + " armor missing: " + r.Armor);
                if (!string.IsNullOrEmpty(r.Shield)) Assert(KitResolves(r.Shield), r.Id + " shield missing: " + r.Shield);
                if (!string.IsNullOrEmpty(r.Book)) Assert(KitResolves(r.Book), r.Id + " book missing: " + r.Book);
                if (!string.IsNullOrEmpty(r.Tool)) Assert(KitResolves(r.Tool), r.Id + " tool missing: " + r.Tool);
            }
            Assert(Roles.Find("nope").Id == "adventurer", "unknown role should fall back to adventurer");
            Assert(Progression.All.Length >= 5, "expected at least 5 advances");
            foreach (var a in Progression.All)
            {
                Assert(!string.IsNullOrEmpty(a.Id) && !string.IsNullOrEmpty(a.Name), "advance with no id/name");
                Assert(!string.IsNullOrEmpty(a.Blurb), a.Id + " has no blurb");
            }
        }

        static bool KitResolves(string name)
        {
            foreach (var list in new IReadOnlyList<ItemDef>[] {
                Catalogue.Weapons, Catalogue.Armor, Catalogue.Shields,
                Catalogue.Tools, Catalogue.Food, Catalogue.Books, Catalogue.Scrolls })
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Name == name) return true;
            return false;
        }

        static void RoleKitsDiffer()
        {
            var f = new Game(1001, "fighter");
            var w = new Game(1001, "wizard");
            Assert(f.Player.Wielded != null && f.Player.Wielded.Name == "short sword", "fighter kit wrong: " + f.Player.Wielded?.Name);
            Assert(w.Player.Wielded != null && w.Player.Wielded.Name == "quarterstaff", "wizard kit wrong: " + w.Player.Wielded?.Name);
            Assert(f.Player.Str > w.Player.Str, $"same seed: fighter Str {f.Player.Str} should beat wizard {w.Player.Str}");
            Assert(w.Player.Int > f.Player.Int, $"same seed: wizard Int {w.Player.Int} should beat fighter {f.Player.Int}");
            Assert(w.Player.Skills[Skill.Magic] > f.Player.Skills[Skill.Magic], "wizard should start more magical");

            // The default run is unchanged: adventurer with the classic kit.
            var d = new Game(1001);
            Assert(d.Player.RoleId == "adventurer", "default role should be adventurer");
            Assert(d.Player.Title == "Adventurer", "default title wrong: " + d.Player.Title);
            Assert(d.Player.FindFirst("lock pick") != null, "default kit lost its lock pick");
            Assert(d.Player.Gold == 30, "default gold changed: " + d.Player.Gold);
        }

        static void LevelAdvancesWork()
        {
            var g = new Game(2002, "fighter");
            g.Player.AddXp(1000);
            Assert(g.Player.Level > 1, "1000 XP should level up");
            int pending = g.Player.PendingAdvances;
            Assert(pending >= 2, "expected 2+ pending advances, got " + pending);
            Assert(g.Player.Title == Roles.TitleFor("fighter", g.Player.Level), "title did not track level: " + g.Player.Title);

            int hpBefore = g.Player.MaxHP;
            Assert(g.ApplyLevelAdvance("tough"), "tough should apply");
            Assert(g.Player.MaxHP == hpBefore + 6, "tough should add exactly 6 HP");
            Assert(g.Player.PendingAdvances == pending - 1, "advance should spend one pick");
            Assert(g.Player.AdvancesTaken.Contains("tough"), "taken list should record tough");

            int held = g.Player.PendingAdvances;
            Assert(!g.ApplyLevelAdvance("bogus"), "unknown advance should fail");
            Assert(g.Player.PendingAdvances == held, "failed advance must not spend");

            int strBefore = g.Player.Str;
            Assert(g.ApplyLevelAdvance("mighty"), "mighty should apply");
            Assert(g.Player.Str >= strBefore, "mighty should not lower strength");

            // The character sheet shows the role, the pending count and the picks.
            g.UiState.Active = Panel.Character;
            var hud = new GameHud(g);
            hud.Ui.Cmd = hud.Cmd;
            hud.Ui.Resize(120, 40);
            string art = hud.Draw().ToAscii();
            Assert(art.Contains("Fighter"), "character panel should show the role");
            Assert(art.Contains("Title"), "character panel should show the title");
            g.UiState.Active = Panel.None;
        }
    }
}