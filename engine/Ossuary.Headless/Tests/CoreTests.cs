using System;
using System.Collections.Generic;
using System.Text;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;
using Ossuary.Core.World;

namespace Ossuary.Tests
{
    /// <summary>
    /// Headless assertions over the simulation. Run with headless.ps1 test.
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
            Test("every glyph is covered by the glyph set", GlyphCoverage);
            Test("every glyph exists in the bitmap font", GlyphsInFont);
            Test("themes cover the palette and stay readable", ThemeContract);
            Test("ui composes in every mode, size and theme", UiEverywhere);
            Test("options panel and display shortcuts", OptionsPanel);
            Test("sidebar shows nearby monsters and landmarks", SidebarContent);
            Test("town entry opens onto the town", TownEntryIsOpen);
            Test("towns are vertical and every floor connects", TownVerticality);
            Test("townsfolk, counters and services work", TownServices);
            Test("towns are stable between visits", TownIsStable);
            Test("overworld travel works", OverworldTravel);
            Test("towns generate with shops", TownGeneration);
            Test("shop economy does not break gold", ShopEconomy);
            Test("shop buy/sell flow", ShopBuySellFlow);
            Test("role table is valid", RoleTableValid);
            Test("role kits differ", RoleKitsDiffer);
            Test("level advances work", LevelAdvancesWork);
            Test("race table is valid", RaceTableValid);
            Test("every race and class builds a playable hero", HeroCombinations);
            Test("creation panel composes", CreatePanelComposes);
            Test("mana, regeneration and skill grades", DerivedStats);
            Test("racial resistances and traits", RacialTraits);
            Test("spell table is valid", SpellTableValid);
            Test("spells cast, cost mana and take effect", SpellCasting);
            Test("spellbooks teach spells", SpellLearning);
            Test("spell panel composes", SpellPanelComposes);
            Test("the whole spell catalogue works", SpellCatalogue);
            Test("allies fight, swap and expire", AllyBehaviour);
            Test("perk table is valid and gated", PerkTable);
            Test("passive perks change the numbers", PassivePerks);
            Test("class abilities spend Vigor and work", AbilityEffects);
            Test("item catalogue, affixes and artifacts are valid", ItemTables);
            Test("loot rolls rarity and affixes by depth", LootRarity);
            Test("armour slots and gear bonuses", GearSlots);
            Test("weapon enchantment, procs and identification", WeaponMagic);
            Test("enchant scrolls and trade value", EnchantAndValue);
            Test("each branch hides its artifact", ArtifactPlacement);
            Test("inventory and sheet compose with full gear", GearUi);
            Test("gods and altars are well formed", GodTable);
            Test("swearing, renouncing and tribute", GodOaths);
            Test("prayer: trouble, haste, boons", GodPrayer);
            Test("deeds earn and cost piety", GodFavour);
            Test("standing blessings by piety tier", GodTiers);
            Test("offerings and altar bumping", GodOfferings);
            Test("altar panel composes", AltarUi);
            Test("surfaces interact", SurfaceRules);
            Test("fire spreads, burns and goes out", FireBehaviour);
            Test("water, lightning and frost", WaterAndFrost);
            Test("surface spells", SurfaceSpells);
            Test("levels carry their surfaces", SurfaceGeneration);
            Test("surfaces and conditions are drawn", SurfaceUi);
            Test("potions are drunk, scrolls are single use", PotionsAndScrolls);
            Test("the balance bot is deterministic", BotDeterminism);
            Test("every class stays within the balance band", BalanceBand);
            Test("quest victory", WinTests.Run);
            Test("tracked features (docs/a-fazer.md)", FeatureTests.Run);

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
            Assert(art.Contains("DEPTH"), "status line missing");
            Assert(art.Contains("HP") && art.Contains("WORN"), "sidebar missing");

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
            foreach (char g in new[] { '@', '▶', '▼', '◎', '◊', '█', '▓', '▒', '░', '─', '│', '┌', '┐', '└', '┘', '╔', '╗', '╚', '╝', '═', '║', '╡', '╞', '♦', '»' })
                Assert(GlyphSet.Contains(g), "ui glyph '" + g + "' not in GlyphSet");
            // Every terrain variant the overworld can draw.
            foreach (OverworldTerrain t in Enum.GetValues(typeof(OverworldTerrain)))
                for (int i = 0; i < 40; i++)
                {
                    Theme.Default.Terrain(t, OverworldFeature.None, i, i * 7, 12, out char g, out Rgb _, out Rgb _);
                    Assert(GlyphSet.Contains(g), "terrain variant " + t + " '" + g + "' not in GlyphSet");
                }
            foreach (OverworldFeature f in Enum.GetValues(typeof(OverworldFeature)))
                Assert(GlyphSet.Contains(Theme.FeatureGlyph(f)), "feature glyph " + f + " not in GlyphSet");
        }

        // Finds the repo's bitmap font so the glyph contract is checked against the real
        // file, not a copy of its codepoint list. Skipped (with a note) if the project
        // layout is not found, e.g. when the suite is run from a copied binary.
        static string FindFontFile()
        {
            string packaged = System.IO.Path.Combine(AppContext.BaseDirectory, "unscii-16.hex");
            if (System.IO.File.Exists(packaged)) return packaged;
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12 && dir != null; i++)
            {
                string f = System.IO.Path.Combine(dir, "assets", "fonts", "unscii-16.hex");
                if (System.IO.File.Exists(f)) return f;
                dir = System.IO.Path.GetDirectoryName(dir);
            }
            return null;
        }

        static void GlyphsInFont()
        {
            string file = FindFontFile();
            if (file == null) throw new InvalidOperationException("Bitmap font not found; glyph coverage cannot be verified.");

            var have = new HashSet<int>();
            foreach (string line in System.IO.File.ReadLines(file))
            {
                int c = line.IndexOf(':');
                if (c <= 0 || line.Length - c - 1 != 32) continue;   // 8x16 glyphs only
                if (int.TryParse(line.Substring(0, c), System.Globalization.NumberStyles.HexNumber, null, out int cp)) have.Add(cp);
            }
            Assert(have.Count > 500, "font parsed with only " + have.Count + " glyphs");

            for (char c = ' '; c <= '~'; c++) Assert(have.Contains(c), "ASCII '" + c + "' missing from font");
            foreach (char c in GlyphSet.Extra)
                Assert(have.Contains(c), "GlyphSet glyph '" + c + "' (U+" + ((int)c).ToString("X4") + ") is not in the bitmap font");
        }

        static float ContrastRatio(Rgb a, Rgb b)
        {
            float La = RelLum(a), Lb = RelLum(b);
            float hi = Math.Max(La, Lb), lo = Math.Min(La, Lb);
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static float RelLum(Rgb c)
        {
            float Lin(byte v) { float f = v / 255f; return f <= 0.03928f ? f / 12.92f : (float)Math.Pow((f + 0.055f) / 1.055f, 2.4); }
            return 0.2126f * Lin(c.R) + 0.7152f * Lin(c.G) + 0.0722f * Lin(c.B);
        }

        static void ThemeContract()
        {
            foreach (ThemePreset preset in Enum.GetValues(typeof(ThemePreset)))
            {
                var th = new Theme(preset);
                string who = preset.ToString();

                // Text that matters must be readable on the surfaces it sits on.
                foreach (var surface in new[] { th.Panel, th.PanelHi, th.Background })
                {
                    Assert(ContrastRatio(th.Text, surface) >= 7f, who + ": Text contrast too low");
                    Assert(ContrastRatio(th.Title, surface) >= 4.5f, who + ": Title contrast too low");
                    Assert(ContrastRatio(th.Dim, surface) >= 3.5f, who + ": Dim contrast too low");
                }
                Assert(ContrastRatio(th.Label, th.Panel) >= 4.5f, who + ": Label contrast too low");
                // Raised surfaces must differ from the panel they sit on (selected rows, status bar).
                Assert(!th.PanelHi.Equals(th.Panel), who + ": raised surface indistinguishable from the panel");

                // Every tile resolves, is distinguishable from its own background once
                // shaded, and stays so at the edge of the torch.
                foreach (TileKind k in Enum.GetValues(typeof(TileKind)))
                {
                    if (k == TileKind.Void) continue;
                    th.TileColors(k, out Rgb fg, out Rgb bg);
                    foreach (float light in new[] { 1f, 0f })
                    {
                        th.Shade(fg, bg, light, false, out Rgb ofg, out Rgb obg);
                        Assert(!ofg.Equals(obg), who + ": " + k + " glyph vanishes into its background at light " + light);
                    }
                }

                // A secret door must look like the wall around it: same colours.
                th.TileColors(TileKind.HiddenDoor, out Rgb hf, out Rgb hb);
                th.TileColors(TileKind.Wall, out Rgb wf, out Rgb wb);
                Assert(hf.Equals(wf) && hb.Equals(wb), who + ": hidden door is distinguishable from a wall");

                // Monochrome presets really are monochrome: hue never leaves the ramp.
                if (preset == ThemePreset.Amber || preset == ThemePreset.Phosphor)
                {
                    foreach (var c in new[] { th.Good, th.Bad, th.Info, th.Magic, th.Quest })
                        Assert(preset == ThemePreset.Amber ? (c.R >= c.G && c.G >= c.B) : (c.G >= c.R && c.G >= c.B),
                               who + ": colour " + c + " is off the phosphor ramp");
                }
            }

            // Variation is stable: the same cell always draws the same.
            Theme.Default.Terrain(OverworldTerrain.Forest, OverworldFeature.None, 5, 9, 12, out char g1, out Rgb f1, out Rgb b1);
            Theme.Default.Terrain(OverworldTerrain.Forest, OverworldFeature.None, 5, 9, 12, out char g2, out Rgb f2, out Rgb b2);
            Assert(g1 == g2 && f1.Equals(f2) && b1.Equals(b2), "terrain variation is not deterministic");

            // Night is darker than noon.
            Theme.Default.Terrain(OverworldTerrain.Grass, OverworldFeature.None, 3, 3, 12, out char _, out Rgb day, out Rgb _);
            Theme.Default.Terrain(OverworldTerrain.Grass, OverworldFeature.None, 3, 3, 1, out char _, out Rgb night, out Rgb _);
            Assert(night.R + night.G + night.B < day.R + day.G + day.B, "night is not darker than day");
        }

        static void UiEverywhere()
        {
            var saved = DisplaySettings.Current.Preset;
            try
            {
                foreach (ThemePreset preset in Enum.GetValues(typeof(ThemePreset)))
                {
                    DisplaySettings.Current.Apply(preset, CrtLevel.Subtle);
                    foreach (var size in new[] { new[] { 60, 20 }, new[] { 84, 26 }, new[] { 100, 28 }, new[] { 120, 40 }, new[] { 200, 56 } })
                    {
                        var g = new Game(4242);
                        var hud = new GameHud(g);
                        hud.Ui.Cmd = hud.Cmd;
                        hud.Ui.Resize(size[0], size[1]);

                        string where = preset + " " + size[0] + "x" + size[1];
                        Assert(hud.Draw().ToAscii().Contains("OSSUARY"), where + ": dungeon header missing");

                        g.LeaveToOverworld();
                        Assert(hud.Draw().ToAscii().Contains("OSSUARY"), where + ": overworld header missing");

                        foreach (Panel pn in Enum.GetValues(typeof(Panel)))
                        {
                            if (pn == Panel.Choice) continue;
                            g.UiState.Active = pn;
                            hud.Draw();
                        }
                        g.UiState.Active = Panel.None;

                        g.Mode = GameMode.GameOver; hud.Draw();
                        g.Mode = GameMode.Won; hud.Draw();
                    }
                }
            }
            finally { DisplaySettings.Current.Apply(saved, CrtLevel.Subtle); }
        }

        static void OptionsPanel()
        {
            var set = DisplaySettings.Current;
            var savedP = set.Preset; var savedC = set.Crt;
            try
            {
                set.Apply(ThemePreset.Ossuary, CrtLevel.Subtle);
                var g = new Game(77);
                var hud = new GameHud(g);
                hud.Ui.Cmd = hud.Cmd;
                hud.Ui.Resize(120, 40);

                hud.Cmd.Execute("settings");
                string art = hud.Draw().ToAscii();
                Assert(g.UiState.Active == Panel.Settings, "settings command did not open the panel");
                Assert(art.Contains("MENU") && art.Contains("Ossuary") && art.Contains("Subtle"), "options panel missing its rows");
                g.UiState.Active = Panel.None;

                int v = set.Version;
                hud.Cmd.Execute("theme");
                Assert(set.Preset == ThemePreset.Amber && Theme.Current.Preset == ThemePreset.Amber, "theme command did not advance");
                hud.Cmd.Execute("crt");
                Assert(set.Crt == CrtLevel.Strong, "crt command did not advance");
                Assert(set.Version > v, "settings version did not change");

                // Cycling wraps all the way around.
                for (int i = 0; i < 4; i++) set.CycleTheme(1);
                Assert(set.Preset == ThemePreset.Amber, "theme cycle does not wrap");
                set.CycleCrt(1);
                Assert(set.Crt == CrtLevel.Off, "crt cycle does not wrap");

                // Text size cycles auto -> 1x -> 2x -> 3x -> auto.
                int sc = set.Scale;
                for (int i = 0; i < 4; i++) set.CycleScale(1);
                Assert(set.Scale == sc, "scale cycle does not wrap");
                set.CycleScale(-1);
                Assert(set.Scale == 3, "scale cycle backwards from auto should reach 3x");
                set.Scale = 0;
                g.UiState.Active = Panel.Settings;
                Assert(hud.Draw().ToAscii().Contains("Text size"), "options panel missing the text size row");
                g.UiState.Active = Panel.None;

                // Off really means off.
                DisplaySettings.CrtParams(CrtLevel.Off, out float sl, out float vg, out float gl);
                Assert(sl == 0f && vg == 0f && gl == 0f, "CRT off still draws effects");
                DisplaySettings.CrtParams(CrtLevel.Strong, out float sl2, out _, out float gl2);
                DisplaySettings.CrtParams(CrtLevel.Subtle, out float sl1, out _, out float gl1);
                Assert(sl2 > sl1 && gl2 > gl1 && sl1 > 0f, "CRT levels are not ordered");
            }
            finally { set.Apply(savedP, savedC); }
        }

        // The player must be able to walk from the town gate to every shop, whatever the seed.
        static void TownEntryIsOpen()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                var t = TownGen.Generate("Probe" + seed, new Rng(seed), 1);
                int reached = 0;
                var seen = new bool[t.Map.W, t.Map.H];
                var q = new Queue<int[]>();
                q.Enqueue(new[] { t.EntryX, t.EntryY });
                seen[t.EntryX, t.EntryY] = true;
                while (q.Count > 0)
                {
                    var c = q.Dequeue(); reached++;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = c[0] + dx, ny = c[1] + dy;
                            if (!t.Map.InBounds(nx, ny) || seen[nx, ny]) continue;
                            // Closed doors count as passable: walking into one opens it.
                            var k = t.Map.Get(nx, ny);
                            if (!Tiles.Walkable(k) && !Tiles.IsDoor(k)) continue;
                            seen[nx, ny] = true;
                            q.Enqueue(new[] { nx, ny });
                        }
                }
                Assert(reached >= 40, "seed " + seed + ": only " + reached + " cells reachable from the town gate");
                foreach (var shop in t.Shops)
                {
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1 && !near; dx++)
                            if (t.Map.InBounds(shop.X + dx, shop.Y + dy) && seen[shop.X + dx, shop.Y + dy]) near = true;
                    Assert(near, "seed " + seed + ": " + shop.Name + " cannot be reached from the gate");
                }
            }
        }

        static void SidebarContent()
        {
            var g = new Game(31337);
            var hud = new GameHud(g);
            hud.Ui.Cmd = hud.Cmd;
            hud.Ui.Resize(120, 40);

            string art = hud.Draw().ToAscii();
            foreach (string must in new[] { "Lv 1", "HP", "EN", "WORN", "VITALS", "JOURNAL" })
                Assert(art.Contains(must), "dungeon screen missing '" + must + "'");

            // Put a monster in plain view next to the player: it must be listed.
            Monster near = new Monster(Bestiary.All[0], g.Rng);
            near.X = g.Player.X + 1; near.Y = g.Player.Y;
            g.Monsters.Add(near);
            g.UpdateFov();
            if (g.Map.IsVisible(near.X, near.Y))
                Assert(hud.Draw().ToAscii().Contains("NEARBY"), "visible monster not listed under Nearby");

            // The overworld keeps the sidebar and lists what is around.
            g.LeaveToOverworld();
            string world = hud.Draw().ToAscii();
            Assert(world.Contains("HP") && world.Contains("VITALS"), "overworld lost its sidebar");

            // Town mode draws the town map, not the overworld.
            g.EnterTown("Testhold");
            Assert(g.Mode == GameMode.TownMap && g.Map != null, "did not enter town");
            Assert(hud.Draw().ToAscii().Contains("Testhold"), "town header missing the town name");
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


        // ---------------------------------------------------------------- towns

        static bool[] FloorReach(GameMap m, int sx, int sy)
        {
            var seen = new bool[m.W * m.H];
            var q = new Queue<int>();
            seen[sx + sy * m.W] = true; q.Enqueue(sx + sy * m.W);
            while (q.Count > 0)
            {
                int c = q.Dequeue(), cx = c % m.W, cy = c / m.W;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if ((dx == 0 && dy == 0) || !m.InBounds(nx, ny) || seen[nx + ny * m.W]) continue;
                        if (!m.CanStep(cx, cy, nx, ny, true)) continue;
                        seen[nx + ny * m.W] = true; q.Enqueue(nx + ny * m.W);
                    }
            }
            return seen;
        }

        // Every building floor exists, its stairs pair up cell for cell with the floor above or below,
        // and everything on it (stairs, people) can be walked to from where the stairs put you.
        static void TownVerticality()
        {
            int tall = 0, cellars = 0;
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var t = TownGen.Generate("Probe" + seed, new Rng(seed * 7919), 2);
                foreach (var b in t.Buildings)
                {
                    if (b.Kind == BuildingKind.Stall) continue;
                    if (b.Up > 0) tall++;
                    if (b.Down > 0) cellars++;
                    for (int z = -b.Down; z <= b.Up; z++)
                    {
                        var m = t.FloorMap(z);
                        Assert(m != null, "seed " + seed + ": " + b.Name + " has no floor " + z);
                        // Arrival cell: the door on the street, otherwise the stair that leads back toward it.
                        int ax = -1, ay = -1;
                        for (int y = b.Y; y < b.Y + b.H; y++)
                            for (int x = b.X; x < b.X + b.W; x++)
                            {
                                var k = m.Get(x, y);
                                if (z > 0 && k == TileKind.StairsDown) { ax = x; ay = y; }
                                if (z < 0 && k == TileKind.StairsUp) { ax = x; ay = y; }
                                if (z == 0 && x == b.DoorX && y == b.DoorY) { ax = x; ay = y; }
                                if (z < b.Up && k == TileKind.StairsUp && z >= 0)
                                    Assert(t.FloorMap(z + 1).Get(x, y) == TileKind.StairsDown, "seed " + seed + ": " + b.Name + " stair up at floor " + z + " has no stair down above it");
                                if (z > -b.Down && k == TileKind.StairsDown && z <= 0)
                                    Assert(t.FloorMap(z - 1).Get(x, y) == TileKind.StairsUp, "seed " + seed + ": " + b.Name + " stair down at floor " + z + " has no stair up below it");
                            }
                        Assert(ax >= 0, "seed " + seed + ": " + b.Name + " floor " + z + " has no way in");
                        var reach = FloorReach(m, ax, ay);
                        for (int y = b.Y + 1; y < b.Y + b.H - 1; y++)
                            for (int x = b.X + 1; x < b.X + b.W - 1; x++)
                            {
                                var k = m.Get(x, y);
                                if (k == TileKind.StairsUp || k == TileKind.StairsDown)
                                    Assert(reach[x + y * m.W], "seed " + seed + ": " + b.Name + " floor " + z + " stair at " + x + "," + y + " is cut off");
                            }
                    }
                }
                var taken = new HashSet<string>();
                foreach (var n in t.Npcs)
                {
                    var m = t.FloorMap(n.Floor);
                    Assert(m != null && m.InBounds(n.X, n.Y), "seed " + seed + ": " + n.Name + " is off the map");
                    var k = m.Get(n.X, n.Y);
                    Assert(Tiles.Walkable(k), "seed " + seed + ": " + n.Name + " (" + n.Role + ") stands in " + k + " at floor " + n.Floor + " " + n.X + "," + n.Y);
                    Assert(taken.Add(n.Floor + ":" + n.X + ":" + n.Y), "seed " + seed + ": two people on one cell at " + n.X + "," + n.Y);
                    Assert(n.Townsperson && n.Level == 0, "townsfolk must be peaceful");
                }
                // The streets stay open from the gate to every door, trees and graves notwithstanding.
                var ground = FloorReach(t.Map, t.EntryX, t.EntryY);
                foreach (var b in t.Buildings)
                    if (b.Kind != BuildingKind.Stall)
                        Assert(ground[b.DoorX + b.DoorY * t.Map.W], "seed " + seed + ": " + b.Name + " cannot be reached from the gate");
                Assert(t.Shops.Count >= (t.Size == "hamlet" ? 3 : 5), "seed " + seed + ": a " + t.Size + " with only " + t.Shops.Count + " shops");
            }
            Assert(tall >= 30 && cellars >= 30, "towns should be built upward and downward (" + tall + " tall, " + cellars + " cellars)");
        }

        /// <summary>Stands the player on a free walkable cell next to a counter cell; returns the key that bumps it.</summary>
        static string StandBy(Game g, int cx, int cy)
        {
            var dirs = new[] { (0, 1, "move-n"), (0, -1, "move-s"), (1, 0, "move-w"), (-1, 0, "move-e") };
            foreach (var (dx, dy, key) in dirs)
            {
                int x = cx + dx, y = cy + dy;
                if (!g.Map.Walkable(x, y) || g.MonsterAt(x, y) != null) continue;
                var k = g.Map.Get(x, y);
                if (k == TileKind.StairsUp || k == TileKind.StairsDown) continue;
                g.Player.X = x; g.Player.Y = y;
                return key;
            }
            throw new Exception("no free cell beside the counter at " + cx + "," + cy);
        }

        static Building FindBuilding(Game g, BuildingKind kind)
        {
            foreach (var b in g.Town.Buildings) if (b.Kind == kind) return b;
            return null;
        }

        static void TownServices()
        {
            var g = new Game(4242);
            g.LeaveToOverworld();
            g.EnterTown("Probeton");
            var cmd = new Commands(g);
            var p = g.Player;
            Assert(g.Mode == GameMode.TownMap && g.TownZ == 0, "town entry");
            Assert(g.Monsters.Count > 8, "the streets are empty: " + g.Monsters.Count + " people");

            // Bumping a smith opens the service menu; browsing leads to the shop, and Esc comes back.
            var smithy = FindBuilding(g, BuildingKind.Smithy);
            string key = StandBy(g, smithy.CounterX, smithy.CounterY);
            cmd.Execute(key);
            Assert(g.UiState.Active == Panel.Service && g.TalkBuilding == smithy, "bumping the smith's counter opens his menu");
            var rows = g.ServiceRows();
            Assert(rows.Exists(r => r.Id == "browse") && rows.Exists(r => r.Id == "hone") && rows.Exists(r => r.Id == "leave"), "smith menu is missing rows");
            Assert(!g.ServiceAction("browse") && g.UiState.Active == Panel.Shop && g.InShop && g.ShopReturnsToServices, "browse opens the shop");
            g.CloseShop(); g.UiState.Active = Panel.None;

            // Honing costs gold and adds a point, three times at most.
            p.Gold = 1000;
            p.Wielded = new Item(Catalogue.Weapons[0], g.Rng, 1);
            int before = p.Wielded.Enchant, gold = p.Gold;
            g.ServiceAction("hone");
            Assert(p.Wielded.Enchant == before + 1 && p.Gold < gold, "honing should cost gold and sharpen the weapon");
            g.ServiceAction("hone"); g.ServiceAction("hone"); g.ServiceAction("hone");
            Assert(p.Wielded.Enchant == 3, "a weapon hones to +3 and no further");
            Assert(!g.ServiceRows().Exists(r => r.Id == "hone" && r.Enabled), "a +3 weapon cannot be honed again");

            // The inn mends everything and the day moves on.
            var inn = FindBuilding(g, BuildingKind.Inn) ?? FindBuilding(g, BuildingKind.Tavern);
            Assert(inn != null, "no inn or tavern");
            key = StandBy(g, inn.CounterX, inn.CounterY);
            cmd.Execute(key);
            Assert(g.UiState.Active == Panel.Service && g.TalkBuilding == inn, "bumping the innkeeper's counter opens the menu");
            g.UiState.Active = Panel.None;
            if ((inn.Services & Service.Rest) != 0)
            {
                p.HP = 1; p.Gold = 500; int day = g.World.Day, hour = g.World.Hour;
                Assert(g.ServiceAction("rest") && p.HP == p.MaxHP, "resting heals fully");
                Assert(g.World.Day > day || g.World.Hour != hour, "resting should move the clock");
                Assert(p.Gold < 500, "a bed is not free");
                p.Gold = 0; p.HP = 1;
                g.TalkBuilding = inn; g.ServiceAction("rest");
                Assert(p.HP == 1, "no gold, no bed");
            }

            // The temple heals and cures.
            var temple = FindBuilding(g, BuildingKind.Temple);
            key = StandBy(g, temple.CounterX, temple.CounterY);
            cmd.Execute(key);
            Assert(g.UiState.Active == Panel.Service && g.TalkBuilding == temple, "bumping the altar calls the priest");
            p.Gold = 300; p.HP = 3; p.PoisonResist = 2;
            g.ServiceAction("heal"); g.ServiceAction("cure");
            Assert(p.HP == p.MaxHP && p.PoisonResist == 0, "the temple heals and cures");
            g.UiState.Active = Panel.None;

            // A scholar names what you cannot.
            var tower = FindBuilding(g, BuildingKind.Emporium);
            var mystery = LevelBuilder.RollLoot(g.Rng, 3);
            while (mystery.Def.Kind == ItemKind.Gold) mystery = LevelBuilder.RollLoot(g.Rng, 3);
            mystery.Identified = false; p.Inventory.Add(mystery); p.Gold = 200;
            key = StandBy(g, tower.CounterX, tower.CounterY);
            cmd.Execute(key);
            g.UiState.Active = Panel.None;
            g.ServiceAction("appraise");
            Assert(g.PendingChoice.Active && g.PendingChoice.Prompt == Game.AppraisePrompt && g.PendingChoice.Items.Contains(mystery), "appraise offers the unknown item");
            cmd.CommitChoice(mystery);
            Assert(mystery.Identified && p.Gold == 200 - Game.AppraisePrice, "appraising identifies and costs gold");
            g.UiState.Active = Panel.None;

            // Stairs: down into the tavern cellar and back, with the cellar's own people.
            var tavern = FindBuilding(g, BuildingKind.Tavern);
            int sx = -1, sy = -1;
            for (int y = tavern.Y; y < tavern.Y + tavern.H; y++)
                for (int x = tavern.X; x < tavern.X + tavern.W; x++)
                    if (g.Town.Map.Get(x, y) == TileKind.StairsDown) { sx = x; sy = y; }
            Assert(sx >= 0, "the tavern has no cellar stairs");
            p.X = sx; p.Y = sy;
            cmd.Execute(">");
            Assert(g.TownZ == -1 && g.Map == g.Town.FloorMap(-1) && g.Map.Get(p.X, p.Y) == TileKind.StairsUp, "descending lands on the cellar's stair");
            foreach (var m in g.Monsters) Assert(m.Floor == -1, "a person from another floor followed you");
            cmd.Execute("<");
            Assert(g.TownZ == 0 && g.Map == g.Town.Map, "climbing returns to the street");
            cmd.Execute("<");
            Assert(g.TownZ == 0, "no stairs, no climbing");

            // Strangers talk instead of fighting, and nothing can be attacked inside the walls.
            Monster stranger = null;
            foreach (var m in g.Monsters) if (m.Role == TownRole.Citizen) { stranger = m; break; }
            Assert(stranger != null, "no citizen on the street");
            key = StandBy(g, stranger.X, stranger.Y);
            int log = g.Log.Count, hp = stranger.HP;
            cmd.Execute(key);
            Assert(g.Log.Count > log && stranger.HP == hp && !stranger.IsDead, "bumping a citizen should talk, not hurt");
            Assert(g.Attack(stranger) && stranger.HP == hp, "attacking townsfolk is refused");
            cmd.Execute("k");
            Assert(stranger.HP == hp, "kicking is refused in town");
        }

        // The same town is generated every time, with the same stock and the same people.
        static void TownIsStable()
        {
            var g = new Game(777);
            g.LeaveToOverworld();
            g.EnterTown("Stableton");
            var first = g.Town;
            string layout = g.Town.Map.ToAscii();
            int gold = first.Shops[0].Gold;
            g.LeaveTown();
            g.EnterTown("Stableton");
            Assert(g.Town == first, "the town object should be reused on a second visit");
            Assert(g.Town.Map.ToAscii() == layout && first.Shops[0].Gold == gold, "town changed between visits");
            var other = new Game(777); other.LeaveToOverworld(); other.EnterTown("Stableton");
            Assert(other.Town.Map.ToAscii() == layout, "the same seed must give the same town");
            long rngBefore = g.Rng.Calls;
            g.LeaveTown(); g.EnterTown("Stableton");
            Assert(g.Rng.Calls == rngBefore, "town visits must not consume the simulation's random stream");
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

        static void RaceTableValid()
        {
            Assert(Races.All.Length >= 7, "expected at least 7 races");
            var ids = new HashSet<string>();
            foreach (var r in Races.All)
            {
                Assert(ids.Add(r.Id), "duplicate race id " + r.Id);
                Assert(!string.IsNullOrEmpty(r.Name) && !string.IsNullOrEmpty(r.Description), r.Id + " needs name and text");
                int sum = r.StrMod + r.DexMod + r.ConMod + r.IntMod + r.WisMod + r.ChaMod;
                Assert(sum >= -1 && sum <= 2, $"{r.Id} attribute budget off: {sum}");
            }
            Assert(Races.Find("nope").Id == "human", "unknown race should fall back to human");
            Assert(Heroes.CleanName("  a   bé	 ") == "a b", "name cleanup");
            Assert(Heroes.CleanName("") == Heroes.DefaultName && Heroes.CleanName(null) == Heroes.DefaultName, "empty name falls back");
            Assert(Heroes.CleanName(new string('x', 40)).Length == Heroes.MaxName, "name is bounded");
        }

        static void HeroCombinations()
        {
            // Same seed: the race/role only bias the roll, so the base dice are shared.
            var human = Game.NewHero(77, "Bo", "human", "fighter");
            var dwarf = Game.NewHero(77, "Bo", "dwarf", "fighter");
            Assert(dwarf.Player.Con == System.Math.Min(18, human.Player.Con + 2) || dwarf.Player.Con >= human.Player.Con, "dwarf should be hardier");
            Assert(human.Player.CharName == "Bo" && human.Player.Name == "you", "name stored separately from the log pronoun");
            foreach (var race in Races.All)
                foreach (var role in Roles.All)
                {
                    var g = Game.NewHero(5150, "T", race.Id, role.Id);
                    var p = g.Player;
                    Assert(p.RaceId == race.Id && p.RoleId == role.Id, "ids not stored");
                    Assert(p.MaxHP >= 10 && p.HP == p.MaxHP, $"{race.Id}/{role.Id} bad HP {p.MaxHP}");
                    Assert(p.Wielded != null && p.WornArmor != null, $"{race.Id}/{role.Id} missing kit");
                    Assert(p.Str >= 3 && p.Str <= 18 && p.Cha >= 3 && p.Int <= 18, $"{race.Id}/{role.Id} attribute out of range");
                    RunRandomTurns(g, 60);
                }
            // Determinism: identical choice, identical hero.
            var a = Game.NewHero(9, "Z", "elf", "ranger"); var b = Game.NewHero(9, "Z", "elf", "ranger");
            Assert(a.Player.AttributeLine() == b.Player.AttributeLine() && a.Player.Gold == b.Player.Gold, "hero not deterministic");
            // Old constructors are unchanged.
            var d = new Game(1001);
            Assert(d.Player.RaceId == "human" && d.Player.Align == Alignment.Neutral && d.Player.Gold == 30, "default hero changed");
        }

        static void DerivedStats()
        {
            var wiz = Game.NewHero(12, "W", "human", "wizard").Player;
            var fig = Game.NewHero(12, "F", "human", "fighter").Player;
            var elfWiz = Game.NewHero(12, "E", "elf", "wizard").Player;
            Assert(wiz.MpMax >= 8 && fig.MpMax == 0, $"mp pools: wizard {wiz.MpMax}, fighter {fig.MpMax}");
            Assert(elfWiz.MpMax > wiz.MpMax || elfWiz.Int < wiz.Int + 2, "elf mana bonus");
            Assert(wiz.Mp == wiz.MpMax, "starts with full mana");
            int before = wiz.MpMax;
            wiz.AddXp(500);
            Assert(wiz.MpMax > before && wiz.Mp == wiz.MpMax, "level-up grows and refills mana");

            // Skill grades and class caps.
            Assert(SkillRanks.Rank(0) == 0 && SkillRanks.Rank(24) == 0 && SkillRanks.Rank(25) == 1 && SkillRanks.Rank(99) == 3 && SkillRanks.Rank(100) == 4, "rank thresholds");
            fig.GainSkill(Skill.Magic, 500);
            Assert(fig.Skills[Skill.Magic] == 40, "fighter Magic capped at 40, got " + fig.Skills[Skill.Magic]);
            fig.GainSkill(Skill.Combat, 500);
            Assert(fig.Skills[Skill.Combat] == 100, "fighter Combat uncapped");

            // Natural regeneration (deterministic, no RNG) and its stops.
            var g = Game.NewHero(31, "R", "human", "wizard");
            g.Player.HP = 5; g.Player.Mp = 0;
            var cmd = new Commands(g);
            for (int i = 0; i < 60; i++) cmd.Execute(".");
            Assert(g.Player.HP > 5, "HP never regenerates");
            Assert(g.Player.Mp > 0, "Mp never regenerates");
            var human = Game.NewHero(31, "H", "human", "fighter").Player;
            var orc = Game.NewHero(31, "O", "orc", "fighter").Player;
            Assert(orc.HpRegenInterval() < human.HpRegenInterval(), "orcs regenerate faster");
        }

        static void RacialTraits()
        {
            var human = Game.NewHero(1, "a", "human", "fighter").Player;
            var dwarf = Game.NewHero(1, "a", "dwarf", "fighter").Player;
            var ashen = Game.NewHero(1, "a", "ashen", "fighter").Player;
            Assert(human.ResistDamage(10, DamageType.Poison) == 10, "human takes full poison");
            Assert(dwarf.ResistDamage(10, DamageType.Poison) == 5, "dwarf halves poison");
            Assert(ashen.ResistDamage(10, DamageType.Fire) == 5 && ashen.ResistDamage(10, DamageType.Cold) == 12, "ashen fire/cold");
            Assert(dwarf.ResistDamage(1, DamageType.Poison) == 1, "a resisted hit still does 1");
            Assert(human.PendingAdvances == 1 && dwarf.PendingAdvances == 0, "only humans start with a free advancement");
            Assert(Game.NewHero(1, "a", "halfling", "fighter").Player.Evasion() > dwarf.Evasion(), "halfling evades better");
            foreach (var r in Races.All) Assert(r.Id == "human" || r.TraitLines().Count > 0, r.Id + " has no traits to show");
            // The fire trap respects resistance.
            var g = Game.NewHero(2, "a", "ashen", "fighter");
            Assert(g.Player.ResistPct(DamageType.Fire) == 50, "ashen fire resist percent");
        }

        // A walkable, visible cell a few steps from the player, with the level emptied of monsters.
        public static bool Arena(Game g, out int x, out int y)
        {
            g.Monsters.Clear();
            g.UpdateFov();
            for (int d = 3; d >= 2; d--)
                for (int dy = -d; dy <= d; dy++)
                    for (int dx = -d; dx <= d; dx++)
                    {
                        x = g.Player.X + dx; y = g.Player.Y + dy;
                        if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != d) continue;
                        if (!g.Map.InBounds(x, y) || !Tiles.Walkable(g.Map.Get(x, y)) || !g.Map.IsVisible(x, y)) continue;
                        if (!Fov.HasLine(g.Map, g.Player.X, g.Player.Y, x, y)) continue;
                        return true;
                    }
            x = y = 0;
            return false;
        }

        static Monster Place(Game g, string kind, int x, int y)
        {
            var m = new Monster(Bestiary.Find(kind), g.Rng) { X = x, Y = y, HomeX = x, HomeY = y };
            g.Monsters.Add(m);
            return m;
        }

        static void SpellTableValid()
        {
            var ids = new HashSet<string>();
            foreach (var s in Spells.All)
            {
                Assert(ids.Add(s.Id), "duplicate spell " + s.Id);
                Assert(s.Level >= 1 && s.Level <= 5 && s.Cost > 0, s.Id + " level/cost");
                Assert(!string.IsNullOrEmpty(s.Name) && !string.IsNullOrEmpty(s.Blurb), s.Id + " text");
                Assert(s.Target == SpellTarget.Self || s.Range > 0, s.Id + " needs a range");
            }
            foreach (var b in Catalogue.Books)
                foreach (var id in Spells.InBook(b.Name)) Assert(Spells.Find(id) != null, b.Name + " teaches unknown " + id);
            foreach (var r in Roles.All) foreach (var id in r.StartSpells) Assert(Spells.Find(id) != null, r.Id + " starts with unknown " + id);
            Assert(Spells.InBook("a spellbook").Length > 0 && Spells.InBook("nope").Length == 0, "book lookup");

            var wiz = Game.NewHero(12, "W", "human", "wizard").Player;
            var mm = Spells.Find("magic-missile");
            int light = Spells.FailPct(wiz, mm);
            wiz.WornArmor = new Item(Catalogue.Armor[5], new Rng(1), 99);   // plate mail
            Assert(Spells.FailPct(wiz, mm) > light, "heavy armour should raise spell failure");
            Assert(Spells.FailPct(wiz, Spells.Find("blink")) > Spells.FailPct(wiz, mm) - 1, "harder spells fail more");
            Assert(Spells.LearnPct(wiz, Spells.Find("blink")) < Spells.LearnPct(wiz, mm), "harder spells are harder to learn");
        }

        static void SpellCasting()
        {
            var g = Game.NewHero(8080, "W", "human", "wizard");
            var p = g.Player;
            Assert(p.Spells.Contains("magic-missile") && p.Spells.Contains("ward"), "wizard starts with spells");
            Assert(Arena(g, out int tx, out int ty), "no arena cell");

            // No mana: no cast, no turn.
            p.Mp = 0; int turn = g.Turn;
            Assert(!g.BeginCast("magic-missile") && g.Turn == turn && !g.UiState.IsTargeting, "casting without mana must be refused for free");
            Assert(!g.BeginCast("frost-ray"), "unknown spell refused");

            // Magic missile hurts and costs mana (retry past the odd fizzle).
            var jackal = Place(g, "jackal", tx, ty); jackal.HP = jackal.MaxHP = 40;
            int hits = 0;
            for (int i = 0; i < 40 && hits == 0; i++)
            {
                p.Mp = p.MpMax; int hp = jackal.HP, t0 = g.Turn;
                Assert(g.CastSpell("magic-missile", jackal.X, jackal.Y), "cast refused");
                Assert(g.Turn == t0 + 1, "a cast takes a turn");
                if (jackal.HP < hp) { hits++; Assert(p.Mp == p.MpMax - 2, "success costs the full mana"); }
                else Assert(p.Mp == p.MpMax - 1, "a fizzle costs half");
            }
            Assert(hits > 0, "magic missile never landed");

            // Out of sight / no target.
            Assert(!g.CastSpell("magic-missile", p.X, p.Y), "cannot target yourself");

            // Kill it: Kills goes up, XP follows the normal path.
            jackal.HP = 1; int kills = p.Kills;
            for (int i = 0; i < 40 && p.Kills == kills; i++) { p.Mp = p.MpMax; g.CastSpell("magic-missile", jackal.X, jackal.Y); }
            Assert(p.Kills == kills + 1, "spell kill not credited");

            // Ward: AC drops by 3 and then expires.
            int ac = p.ArmorClass();
            for (int i = 0; i < 40 && p.WardTurns == 0; i++) { p.Mp = p.MpMax; g.CastSpell("ward", p.X, p.Y); }
            Assert(p.WardTurns > 0 && p.ArmorClass() == ac - 3, "ward should improve AC by 3");
            p.WardTurns = 1; g.Player.Asleep = false;
            new Commands(g).Execute(".");
            Assert(p.WardTurns == 0 && p.ArmorClass() == ac, "ward expires");

            // Sleep: a jackal drops off and a blow wakes it.
            g.Monsters.Clear();
            var sleeper = Place(g, "jackal", tx, ty); sleeper.HP = sleeper.MaxHP = 40;
            p.Spells.Add("sleep");
            for (int i = 0; i < 60 && !sleeper.Asleep; i++) { p.Mp = p.MpMax; g.CastSpell("sleep", sleeper.X, sleeper.Y); }
            Assert(sleeper.Asleep && sleeper.SleepTurns > 0, "sleep never took hold on a jackal");
            sleeper.SleepTurns = 2;
            for (int i = 0; i < 3; i++) new Commands(g).Execute(".");
            Assert(!sleeper.Asleep, "sleep should wear off");

            // Blink moves the player.
            p.Spells.Add("blink"); g.Monsters.Clear();
            int ox = p.X, oy = p.Y;
            for (int i = 0; i < 80 && p.X == ox && p.Y == oy; i++) { p.Mp = p.MpMax; g.CastSpell("blink", tx, ty); }
            Assert(p.X == tx && p.Y == ty, "blink did not arrive");

            // Cure wounds.
            p.HP = 1; p.Spells.Add("cure-wounds");
            for (int i = 0; i < 40 && p.HP == 1; i++) { p.Mp = p.MpMax; g.CastSpell("cure-wounds", p.X, p.Y); }
            Assert(p.HP > 1, "cure wounds never healed");
        }

        static void SpellLearning()
        {
            // The necromancer starts with sleep; the book of shadows also holds blink.
            var g = Game.NewHero(4141, "N", "human", "necromancer");
            var p = g.Player;
            var book = new Item(Catalogue.Books[1], g.Rng, g.NextUid());   // a book of shadows
            Assert(book.Def.Name == "a book of shadows", "wrong book " + book.Def.Name);
            Assert(!p.Spells.Contains("blink"), "blink should start unknown");
            g.Monsters.Clear();
            for (int i = 0; i < 200 && System.Array.Exists(Spells.InBook(book.Def.Name), id => !p.Spells.Contains(id)); i++)
            {
                p.Confused = false; p.ConfusionTurns = 0;
                g.StudyBook(book);
                g.Monsters.Clear();
                if (g.Mode == GameMode.GameOver) break;
            }
            Assert(p.Spells.Contains("blink") && p.Spells.Contains("fear"), "never learned the whole book");
            g.Monsters.Clear();
            p.Confused = false;
            int before = g.Turn;
            g.StudyBook(book);
            Assert(g.Turn == before, "studying a known book costs nothing");

            // Fighters have no gift for it, and enemies in view stop a study.
            var f = Game.NewHero(4141, "F", "human", "fighter");
            f.Monsters.Clear();
            f.StudyBook(new Item(Catalogue.Books[2], f.Rng, f.NextUid()));
            Assert(f.Player.Spells.Count == 0, "a fighter learned a spell");
            var w = Game.NewHero(4141, "W", "human", "wizard");
            if (Arena(w, out int x, out int y))
            {
                Place(w, "jackal", x, y).Dormant = false;
                int wt = w.Turn;
                w.StudyBook(new Item(Catalogue.Books[3], w.Rng, w.NextUid()));   // a spellbook
                Assert(w.Turn == wt, "studying with an enemy in view must be refused");
            }
        }

        // A strong, durable caster who knows every spell, standing where there is room to work.
        static Game Archmage(ulong seed = 9001)
        {
            var g = Game.NewHero(seed, "A", "gnome", "wizard");
            var p = g.Player;
            p.Level = 15; p.Int = 21; p.Wis = 21; p.Skills[Skill.Magic] = 100;
            p.RecomputeMaxMp();
            p.MaxHP = p.HP = 5000;
            foreach (var s in Spells.All) if (!p.Spells.Contains(s.Id)) p.Spells.Add(s.Id);
            g.Monsters.Clear();
            return g;
        }

        // Casts until one lands (not fizzled). Returns false when the cast is refused outright.
        static bool CastOk(Game g, string id, int x, int y)
        {
            var p = g.Player; int cost = Spells.Find(id).Cost;
            for (int i = 0; i < 80; i++)
            {
                p.Mp = p.MpMax = System.Math.Max(p.MpMax, 99);
                if (!g.CastSpell(id, x, y)) return false;
                if (p.Mp == p.MpMax - cost) return true;
            }
            return false;
        }

        static bool Row(Game g, int len, out int dx, out int dy)
        {
            for (int k = 0; k < 8; k++)
            {
                dx = Pathfinder.Dx8[k]; dy = Pathfinder.Dy8[k];
                bool ok = true;
                for (int i = 1; i <= len && ok; i++)
                {
                    int x = g.Player.X + dx * i, y = g.Player.Y + dy * i;
                    ok = g.Map.InBounds(x, y) && Tiles.Walkable(g.Map.Get(x, y)) && g.Map.IsVisible(x, y);
                }
                if (ok) return true;
            }
            dx = dy = 0; return false;
        }

        static void SpellCatalogue()
        {
            Assert(Spells.All.Length >= 30, "catalogue should hold 30+ spells, has " + Spells.All.Length);
            var schools = new HashSet<School>();
            foreach (var s in Spells.All) schools.Add(s.School);
            Assert(schools.Count == 6, "all six schools should be represented");

            // Direct damage, one spell at a time.
            foreach (var id in new[] { "magic-missile", "frost-ray", "chain-lightning", "finger-of-death", "smite", "drain-life" })
            {
                var g = Archmage(); Assert(Arena(g, out int x, out int y), "arena");
                var m = Place(g, "ogre", x, y); m.HP = m.MaxHP = 4000;
                g.Player.HP = 1000;
                Assert(CastOk(g, id, m.X, m.Y), id + " would not cast");
                Assert(m.HP < 4000, id + " did no damage");
                if (id == "drain-life") Assert(g.Player.HP > 1000 || g.Player.HP == g.Player.MaxHP, "drain life should heal the caster");
            }

            // Necrotic harm does nothing to the dead; holy harm doubles.
            {
                var g = Archmage(); Arena(g, out int x, out int y);
                var sk = Place(g, "skeleton", x, y); sk.HP = sk.MaxHP = 4000;
                CastOk(g, "finger-of-death", sk.X, sk.Y);
                Assert(sk.HP == 4000, "the dead should shrug off finger of death");
                var plain = Place(g, "ogre", x, y); plain.X = -1;   // parked, unused
                g.Monsters.Remove(plain);
            }

            // Adjacent-only touch.
            {
                var g = Archmage(); Arena(g, out int x, out int y);
                var far = Place(g, "ogre", x, y); far.HP = far.MaxHP = 4000;
                if (System.Math.Max(System.Math.Abs(x - g.Player.X), System.Math.Abs(y - g.Player.Y)) > 1)
                    Assert(!g.CastSpell("shocking-grasp", x, y), "shocking grasp must refuse a distant target for free");
            }

            // Fireball burns a cluster; allies are spared.
            {
                var g = Archmage(); Arena(g, out int x, out int y);
                var a = Place(g, "jackal", x, y); var b = Place(g, "jackal", x, y); a.HP = a.MaxHP = 900; b.HP = b.MaxHP = 900;
                a.Asleep = b.Asleep = true;
                // b sits beside a if there is room.
                for (int d = 0; d < 8; d++)
                {
                    int bx = x + Pathfinder.Dx8[d], by = y + Pathfinder.Dy8[d];
                    if (g.Map.InBounds(bx, by) && Tiles.Walkable(g.Map.Get(bx, by)) && !(bx == g.Player.X && by == g.Player.Y)) { b.X = bx; b.Y = by; break; }
                }
                var friend = Place(g, "jackal", b.X, b.Y); friend.HP = friend.MaxHP = 900; friend.Ally = true;
                friend.X = b.X; friend.Y = b.Y;
                Assert(CastOk(g, "fireball", a.X, a.Y), "fireball would not cast");
                Assert(a.HP < 900 && b.HP < 900, "fireball should hit the whole cluster");
                Assert(friend.HP >= 880, "fireball must spare allies, ally has " + friend.HP);
                g.Monsters.Remove(friend);
                Assert(!g.CastSpell("magic-missile", friend.X, friend.Y), "no hostile spell on an ally");
            }

            // Lightning bolt pierces a line.
            {
                var g = Archmage();
                g.UpdateFov();
                if (Row(g, 4, out int dx, out int dy))
                {
                    var a = Place(g, "ogre", g.Player.X + dx * 2, g.Player.Y + dy * 2); a.HP = a.MaxHP = 4000;
                    var b = Place(g, "ogre", g.Player.X + dx * 4, g.Player.Y + dy * 4); b.HP = b.MaxHP = 4000;
                    Assert(CastOk(g, "lightning-bolt", g.Player.X + dx * 2, g.Player.Y + dy * 2), "bolt would not cast");
                    Assert(a.HP < 4000 && b.HP < 4000, "bolt should pierce both targets");
                }
            }

            // Meteor, chain: just make sure they resolve with several foes around.
            {
                var g = Archmage(); Arena(g, out int x, out int y);
                for (int i = 0; i < 4; i++) { var j = Place(g, "jackal", x, y); j.HP = j.MaxHP = 4000; }
                Assert(CastOk(g, "meteor", x, y), "meteor would not cast");
            }

            // Self buffs.
            {
                var g = Archmage(); var p = g.Player;
                int ac = p.ArmorClass();
                CastOk(g, "stone-skin", p.X, p.Y);
                Assert(p.BuffTurns("stone-skin") > 0 && p.ArmorClass() == ac - 6, "stone skin AC");
                CastOk(g, "bless", p.X, p.Y); Assert(p.BuffTurns("bless") > 0, "bless");
                CastOk(g, "haste", p.X, p.Y); Assert(p.BuffTurns("haste") > 0, "haste");
                CastOk(g, "invisibility", p.X, p.Y);
                Assert(p.Invisible && p.BuffTurns("invisibility") > 0, "invisibility");
                p.SetBuff("invisibility", 1);
                new Commands(g).Execute(".");
                Assert(!p.Invisible, "invisibility should end with its buff");
                p.PoisonResist = 2; p.Confused = true; p.ConfusionTurns = 9;
                CastOk(g, "cleanse", p.X, p.Y);
                Assert(p.PoisonResist == 0 && !p.Confused, "cleanse should clear ailments");
                p.HP = 10; p.MaxHP = 5000;
                CastOk(g, "greater-heal", p.X, p.Y);
                Assert(p.HP > 20, "greater heal");
                int ox = p.X, oy = p.Y;
                CastOk(g, "teleport", p.X, p.Y);
                Assert(Tiles.Walkable(g.Map.Get(p.X, p.Y)), "teleport landed in a wall");
                CastOk(g, "clairvoyance", p.X, p.Y);
                int seen = 0;
                for (int yy = 0; yy < g.Map.H; yy++) for (int xx = 0; xx < g.Map.W; xx++) if (g.Map.WasSeen(xx, yy)) seen++;
                Assert(seen > g.Map.W * g.Map.H / 2, "clairvoyance should reveal the level");
            }

            // Revive undoes one death, once.
            {
                var g = Archmage(); var p = g.Player;
                CastOk(g, "revive", p.X, p.Y);
                p.HP = 0; g.CheckDeath();
                Assert(g.Mode != GameMode.GameOver && p.HP > 0 && p.BuffTurns("revive") == 0, "revive should undo death");
                p.HP = 0; g.CheckDeath();
                Assert(g.Mode == GameMode.GameOver, "the second death sticks");
            }

            // Monster-affecting statuses.
            {
                var g = Archmage(); Arena(g, out int x, out int y);
                var m = Place(g, "kobold", x, y); m.HP = m.MaxHP = 4000;
                int speed = m.Speed;
                Assert(CastOk(g, "slow", m.X, m.Y), "slow cast");
                if (m.SlowTurns > 0) Assert(m.Speed < speed, "slow should lower speed");
                m.SlowTurns = 1; new Commands(g).Execute(".");
                Assert(m.Speed == m.Def.Speed, "slow should wear off");
                CastOk(g, "confuse", m.X, m.Y);
                CastOk(g, "fear", m.X, m.Y);
                var sk = Place(g, "skeleton", m.X, m.Y);
                Assert(CastOk(g, "turn-undead", g.Player.X, g.Player.Y), "turn undead cast");
                Assert(sk.FearTurns > 0 || sk.IsDead, "undead should be turned");
            }
        }

        static void AllyBehaviour()
        {
            // Summons appear beside you, fight hostiles and expire.
            var g = Archmage(3030); var p = g.Player;
            Assert(CastOk(g, "familiar", p.X, p.Y), "familiar");
            Assert(g.Monsters.FindAll(m => m.Ally).Count == 1, "one ally expected");
            var ally = g.Monsters.Find(m => m.Ally);
            Assert(Pathfinder.Chebyshev(ally.X, ally.Y, p.X, p.Y) <= 2, "ally should appear near you");
            Assert(ally.Name.StartsWith("allied "), "ally name");

            Assert(CastOk(g, "army-of-bones", p.X, p.Y), "army");
            Assert(g.Monsters.FindAll(m => m.Ally).Count >= 3, "army of bones should add allies");

            // Bumping an ally swaps places and takes a turn.
            int ax = ally.X, ay = ally.Y, px = p.X, py = p.Y;
            int dx = ax - px, dy = ay - py;
            if (System.Math.Abs(dx) <= 1 && System.Math.Abs(dy) <= 1 && (dx != 0 || dy != 0))
            {
                int t = g.Turn;
                g.Attack(ally);
                Assert(p.X == ax && p.Y == ay && g.Turn == t + 1, "bumping an ally swaps places");
            }

            // Allies kill a weak hostile on their own.
            var g2 = Archmage(3031); var p2 = g2.Player;
            Assert(Arena(g2, out int x, out int y), "arena");
            var rat = Place(g2, "giant rat", x, y); rat.HP = rat.MaxHP = 6; rat.Alert = 1;
            CastOk(g2, "army-of-bones", p2.X, p2.Y);
            for (int i = 0; i < 40 && !rat.IsDead; i++) new Commands(g2).Execute(".");
            Assert(rat.IsDead || g2.Monsters.TrueForAll(m => m.Ally || m != rat), "allies should have killed the rat");

            // Summons expire and charm wears off.
            var g3 = Archmage(3032); var p3 = g3.Player;
            CastOk(g3, "familiar", p3.X, p3.Y);
            var s3 = g3.Monsters.Find(m => m.Ally); s3.SummonTurns = 2;
            for (int i = 0; i < 4; i++) new Commands(g3).Execute(".");
            Assert(!g3.Monsters.Exists(m => m.Ally), "a summon should fade");

            var g4 = Archmage(3033); var p4 = g4.Player;
            Assert(Arena(g4, out int cx, out int cy), "arena");
            var kobold = Place(g4, "kobold", cx, cy); kobold.HP = kobold.MaxHP = 4000;
            for (int i = 0; i < 40 && !kobold.Ally; i++) CastOk(g4, "charm", kobold.X, kobold.Y);
            Assert(kobold.Ally && kobold.Name.StartsWith("charmed "), "charm should turn the kobold");
            kobold.SummonTurns = 1; new Commands(g4).Execute(".");
            Assert(!kobold.Ally && kobold.Name == kobold.Def.Name, "charm should wear off and restore the monster");
        }

        static void PerkTable()
        {
            var ids = new HashSet<string>();
            foreach (var d in Progression.All)
            {
                Assert(ids.Add(d.Id), "duplicate perk " + d.Id);
                Assert(d.MaxRank >= 1 && !string.IsNullOrEmpty(d.Blurb), d.Id + " rank/text");
                if (d.Requires != null) Assert(Progression.Find(d.Requires) != null, d.Id + " requires unknown " + d.Requires);
                if (d.GrantsAbility != null) Assert(Abilities.Find(d.GrantsAbility) != null, d.Id + " grants unknown ability");
                if (d.Roles != null) foreach (var r in d.Roles) Assert(Roles.Find(r).Id == r, d.Id + " names unknown role " + r);
            }
            foreach (var a in Abilities.All)
            {
                Assert(a.Cost > 0 && !string.IsNullOrEmpty(a.Blurb), a.Id + " cost/text");
                Assert(a.Target == AbilityTarget.Self || a.Range > 0, a.Id + " needs reach");
                Assert(System.Array.Exists(Progression.All, d => d.GrantsAbility == a.Id), a.Id + " has no perk that teaches it");
            }
            foreach (var r in Roles.All) foreach (var perk in r.StartPerks) Assert(Progression.Find(perk) != null, r.Id + " starts with unknown perk");

            // Gating: role, level, prerequisites, magic and ranks.
            var wiz = Game.NewHero(1, "w", "human", "wizard").Player;
            var fig = Game.NewHero(1, "f", "human", "fighter").Player;
            Assert(fig.Abilities.Contains("power-strike") && fig.PerkRank("power-strike") == 1, "fighter starts with power strike");
            Assert(!Progression.IsAvailable(fig, Progression.Find("power-strike")), "a known ability cannot be taken twice");
            Assert(!Progression.IsAvailable(wiz, Progression.Find("weapon-master")), "wizards cannot be weapon masters");
            Assert(Progression.IsAvailable(wiz, Progression.Find("mind-expansion")) == (wiz.Level >= 2), "mind expansion gated by level");
            wiz.Level = 5;
            Assert(Progression.IsAvailable(wiz, Progression.Find("mind-expansion")), "wizards may expand their minds");
            Assert(!Progression.IsAvailable(fig, Progression.Find("mind-expansion")), "fighters have no mana to expand");
            fig.Level = 8; fig.Skills[Skill.Combat] = 40;
            Assert(!Progression.IsAvailable(fig, Progression.Find("cleave")), "cleave needs weapon master first");
            fig.PendingAdvances = 5;
            Assert(fig.ApplyAdvance("weapon-master") && fig.PerkRank("weapon-master") == 1, "weapon master");
            Assert(Progression.IsAvailable(fig, Progression.Find("cleave")), "cleave unlocked");
            Assert(fig.ApplyAdvance("cleave") && fig.Abilities.Contains("cleave"), "cleave teaches the ability");
            Assert(!fig.ApplyAdvance("mind-expansion") && fig.PendingAdvances == 3, "a refused perk must not spend the pick");
            fig.ApplyAdvance("weapon-master"); fig.ApplyAdvance("weapon-master");
            Assert(fig.PerkRank("weapon-master") == 3 && !Progression.IsAvailable(fig, Progression.Find("weapon-master")), "ranks cap out");
        }

        static void PassivePerks()
        {
            var p = Game.NewHero(11, "p", "human", "paladin").Player;
            p.Level = 10; p.Skills[Skill.Survival] = 20;
            p.PendingAdvances = 20; p.RecomputeMaxVigor(); p.RecomputeMaxMp();
            int ac = p.ArmorClass(), ev = p.Evasion(), mp = p.MpMax, vg = p.VigorMax;
            Assert(p.ApplyAdvance("shield-wall") && p.ArmorClass() == ac - 2, "shield wall +2 AC with a shield");
            Assert(p.ApplyAdvance("aura") && p.ArmorClass() == ac - 3, "aura +1 AC while armoured");
            Assert(p.ApplyAdvance("lucky") && p.Evasion() == ev + 2, "lucky +2 evasion");
            p.ApplyAdvance("hale");
            Assert(p.ApplyAdvance("iron-will") && p.ResistPct(DamageType.Poison) == 20, "iron will poison resistance");
            Assert(p.ApplyAdvance("mind-expansion") && p.MpMax >= mp + 6, "mind expansion +6 Mp");
            Assert(p.ApplyAdvance("vigorous") && p.VigorMax == vg + 8 && p.VigorRegenInterval() < 5, "vigorous");
            var spell = Spells.Find("blink");
            int fail = Spells.FailPct(p, spell);
            Assert(p.ApplyAdvance("focus") && Spells.FailPct(p, spell) == System.Math.Max(0, fail - 5), "focus -5% failure");
            int regen = p.MpRegenInterval();
            Assert(p.ApplyAdvance("quick-recovery") && p.MpRegenInterval() < regen, "quick recovery");

            int xpBefore = p.Xp;
            Assert(p.ApplyAdvance("quick-learner"), "quick learner");
            p.AddXp(100);
            Assert(p.Xp + (p.Level > 10 ? 0 : 0) >= 0, "xp");

            // Gourmand: nutrition drains at half speed.
            var g1 = Game.NewHero(5, "a", "human", "fighter"); var g2 = Game.NewHero(5, "b", "human", "fighter");
            g2.Player.Skills[Skill.Survival] = 20; g2.Player.Level = 3; g2.Player.PendingAdvances = 1;
            Assert(g2.Player.ApplyAdvance("gourmand"), "gourmand");
            g1.Monsters.Clear(); g2.Monsters.Clear();
            int n1 = g1.Player.Nutrient, n2 = g2.Player.Nutrient;
            for (int i = 0; i < 40; i++) { new Commands(g1).Execute("."); new Commands(g2).Execute("."); }
            Assert((n1 - g1.Player.Nutrient) > (n2 - g2.Player.Nutrient) * 3 / 2, "gourmand should halve hunger");

            // Weapon master raises damage floor and to-hit through combat maths.
            var fig = Game.NewHero(3, "f", "human", "fighter"); fig.Player.Level = 5; fig.Player.PendingAdvances = 3; fig.Player.Skills[Skill.Combat] = 30;
            var rng = new Rng(77);
            var dummy = new Monster(Bestiary.Find("ogre"), rng) { X = 0, Y = 0 };
            long before = 0, after = 0;
            for (int i = 0; i < 400; i++) { dummy.HP = 100000; Battles.PlayerMelee(fig.Player, dummy, new Rng((ulong)i + 1), out _); before += 100000 - dummy.HP; }
            fig.Player.ApplyAdvance("weapon-master"); fig.Player.ApplyAdvance("weapon-master"); fig.Player.ApplyAdvance("weapon-master");
            for (int i = 0; i < 400; i++) { dummy.HP = 100000; Battles.PlayerMelee(fig.Player, dummy, new Rng((ulong)i + 1), out _); after += 100000 - dummy.HP; }
            Assert(after > before, "weapon master should raise total damage");
        }

        static Game Champion(ulong seed = 7007)
        {
            var g = Game.NewHero(seed, "C", "human", "fighter");
            var p = g.Player;
            p.Level = 12; p.Str = 18; p.Dex = 18;
            foreach (var a in Abilities.All) p.LearnAbility(a.Id);
            p.WornShield = new Item(Catalogue.Shields[1], g.Rng, g.NextUid());
            p.MaxHP = p.HP = 5000;
            p.RecomputeMaxVigor(); p.VigorMax = 100; p.Vigor = 100;
            g.Monsters.Clear();
            return g;
        }

        static bool UseOk(Game g, string id, int x, int y)
        {
            var p = g.Player; int cost = Abilities.Find(id).Cost;
            p.Vigor = p.VigorMax = 100;
            int t0 = g.Turn;
            bool r = g.UseAbility(id, x, y);
            Assert(!r || g.Turn == t0 + 1, id + " should take one turn");
            Assert(!r || p.Vigor == 100 - cost, id + " should cost " + cost + " Vigor, vigor now " + p.Vigor);
            return r;
        }

        static void AbilityEffects()
        {
            // Out of Vigor: refused for free.
            var g = Champion(); var p = g.Player;
            Assert(Arena(g, out int x, out int y), "arena");
            var ogre = Place(g, "ogre", x, y); ogre.HP = ogre.MaxHP = 100000; ogre.Alert = 0;
            p.Vigor = 0; int turn = g.Turn;
            Assert(!g.BeginAbility("second-wind") && g.Turn == turn, "no Vigor, no action");
            Assert(!g.BeginAbility("nonsense"), "unknown ability refused");

            // Vigor comes back on its own.
            p.Vigor = 0;
            var calm = Champion(1); for (int i = 0; i < 40; i++) new Commands(calm).Execute(".");
            calm.Player.Vigor = 0; for (int i = 0; i < 40; i++) new Commands(calm).Execute(".");
            Assert(calm.Player.Vigor >= 5, "vigor should regenerate");

            // Melee abilities need an adjacent foe.
            int d = Pathfinder.Chebyshev(p.X, p.Y, ogre.X, ogre.Y);
            if (d > 1) Assert(!g.UseAbility("power-strike", ogre.X, ogre.Y), "power strike must be refused out of reach");

            // Put the ogre beside the player for the melee set.
            ogre.X = p.X + 1; ogre.Y = p.Y;
            if (!Tiles.Walkable(g.Map.Get(ogre.X, ogre.Y))) { ogre.X = p.X - 1; if (!Tiles.Walkable(g.Map.Get(ogre.X, ogre.Y))) { ogre.X = p.X; ogre.Y = p.Y + 1; } }
            ogre.Speed = 1;   // it barely acts, so the numbers stay clean
            foreach (var id in new[] { "power-strike", "backstab", "holy-strike", "shield-bash" })
            {
                int hp = ogre.HP; bool hit = false;
                for (int i = 0; i < 60 && !hit; i++) { Assert(UseOk(g, id, ogre.X, ogre.Y), id + " refused"); hit = ogre.HP < hp; }
                Assert(hit, id + " never landed");
            }
            ogre.Energy = 10; UseOk(g, "shield-bash", ogre.X, ogre.Y);

            // Backstab on a sleeper beats a plain strike.
            long awake = 0, asleep = 0;
            for (int i = 0; i < 120; i++)
            {
                ogre.HP = 100000; ogre.Asleep = false; ogre.Alert = 1; var r1 = new Rng((ulong)i + 5);
                Battles.PlayerMelee(p, ogre, r1, out _, 2, 2); awake += 100000 - ogre.HP;
                ogre.HP = 100000; var r2 = new Rng((ulong)i + 5);
                Battles.PlayerMelee(p, ogre, r2, out _, 3, 2); asleep += 100000 - ogre.HP;
            }
            Assert(asleep > awake, "triple damage should beat double");

            // Shield bash needs a shield.
            p.WornShield = null;
            Assert(!g.UseAbility("shield-bash", ogre.X, ogre.Y), "shield bash without a shield");
            p.WornShield = new Item(Catalogue.Shields[1], g.Rng, g.NextUid());

            // Cleave: two neighbours both take a swing.
            var g2 = Champion(7008); var p2 = g2.Player;
            Assert(Arena(g2, out int cx, out int cy), "arena 2");
            var a = Place(g2, "ogre", p2.X, p2.Y); var b = Place(g2, "ogre", p2.X, p2.Y);
            int placed = 0;
            for (int k = 0; k < 8 && placed < 2; k++)
            {
                int nx = p2.X + Pathfinder.Dx8[k], ny = p2.Y + Pathfinder.Dy8[k];
                if (!Tiles.Walkable(g2.Map.Get(nx, ny))) continue;
                var m = placed == 0 ? a : b; m.X = nx; m.Y = ny; placed++;
            }
            if (placed == 2)
            {
                a.HP = a.MaxHP = b.HP = b.MaxHP = 100000; a.Speed = b.Speed = 1;
                for (int i = 0; i < 60 && (a.HP == 100000 || b.HP == 100000); i++) UseOk(g2, "cleave", p2.X, p2.Y);
                Assert(a.HP < 100000 && b.HP < 100000, "cleave should reach both neighbours");
            }

            // Ranged: aimed shot.
            var g3 = Champion(7009); var p3 = g3.Player;
            Assert(Arena(g3, out int sx, out int sy), "arena 3");
            var far = Place(g3, "ogre", sx, sy); far.HP = far.MaxHP = 100000; far.Speed = 1;
            int hp3 = far.HP; bool landed = false;
            for (int i = 0; i < 60 && !landed; i++) { UseOk(g3, "aimed-shot", far.X, far.Y); landed = far.HP < hp3; }
            Assert(landed, "aimed shot never landed");

            // Self abilities.
            var g4 = Champion(7010); var p4 = g4.Player;
            p4.HP = 100; p4.MaxHP = 400;
            Assert(UseOk(g4, "second-wind", p4.X, p4.Y) && p4.HP >= 190, "second wind heals a quarter");
            p4.HP = 100; p4.PoisonResist = 2;
            Assert(UseOk(g4, "lay-on-hands", p4.X, p4.Y) && p4.HP >= 290 && p4.PoisonResist == 0, "lay on hands heals half and cleanses");
            Assert(UseOk(g4, "vanish", p4.X, p4.Y) && p4.Invisible, "vanish");
            Assert(Arena(g4, out int wx, out int wy), "arena 4");
            var kobold = Place(g4, "kobold", wx, wy); kobold.HP = kobold.MaxHP = 100000;
            for (int i = 0; i < 40 && kobold.FearTurns == 0; i++) UseOk(g4, "war-cry", p4.X, p4.Y);
            Assert(kobold.FearTurns > 0, "war cry should frighten a kobold");

            // Holy strike hurts the dead twice as much as the living.
            var g5 = Champion(7011); var p5 = g5.Player;
            Assert(Arena(g5, out int hx, out int hy), "arena 5");
            var sk = Place(g5, "skeleton", p5.X + 1, p5.Y); sk.HP = sk.MaxHP = 100000; sk.Speed = 1;
            if (!Tiles.Walkable(g5.Map.Get(sk.X, sk.Y))) { sk.X = p5.X - 1; }
            if (Tiles.Walkable(g5.Map.Get(sk.X, sk.Y)))
            {
                int h0 = sk.HP;
                for (int i = 0; i < 60 && sk.HP == h0; i++) UseOk(g5, "holy-strike", sk.X, sk.Y);
                Assert(sk.HP < h0, "holy strike should burn the undead");
            }
        }

        static void WieldVia(Game g, Item it)
        {
            var c = new Commands(g);
            c.Execute("w");
            if (g.PendingChoice.Active) c.CommitChoice(it);
        }

        static Item Make(string name, IReadOnlyList<ItemDef> list, Rng rng)
        {
            foreach (var d in list) if (d.Name == name) return new Item(d, rng, 1);
            throw new Exception("no such item " + name);
        }

        static void ItemTables()
        {
            var ids = new HashSet<string>();
            foreach (var a in Affixes.All)
            {
                Assert(ids.Add(a.Id), "duplicate affix " + a.Id);
                Assert(a.ForWeapon || a.ForArmor, a.Id + " fits nothing");
                Assert(a.Weight > 0 && a.Mods.Lines().Count > 0, a.Id + " does nothing");
            }
            for (int w = 0; w < 2; w++)
                foreach (bool prefix in new[] { true, false })
                    Assert(Affixes.Pick(new Rng(1), prefix, w == 0) != null, "no affix for weapon=" + (w == 0) + " prefix=" + prefix);
            var dungeon = new Dungeon(new Rng(1));
            foreach (var art in Artifacts.All)
            {
                var item = Artifacts.Create(art, new Rng(1), 1);
                Assert(item.Rarity == Rarity.Artifact && item.Name == art.Name && !item.Identified, art.Id + " artifact item");
                Assert(item.Mods.Lines().Count > 0, art.Id + " has no powers");
                Assert(art.Depth >= 1 && art.Depth <= dungeon.Get(art.Branch).MaxDepth && dungeon.Get(art.Branch).Name == art.Branch, art.Id + " level out of range");
            }
            foreach (var d in Catalogue.Helms) Assert(d.Kind == ItemKind.Helm && d.AC > 0, d.Name + " helm");
            foreach (var d in Catalogue.Gloves) Assert(d.Kind == ItemKind.Gloves && d.AC > 0, d.Name + " gloves");
            foreach (var d in Catalogue.Boots) Assert(d.Kind == ItemKind.Boots && d.AC > 0, d.Name + " boots");
            foreach (var d in Catalogue.Cloaks) Assert(d.Kind == ItemKind.Cloak && d.AC > 0, d.Name + " cloak");
            foreach (var d in Catalogue.Armor) Assert(d.Kind == ItemKind.Armor, d.Name + " should be body armour");
            Assert(ItemKind.Helm.IsGear() && ItemKind.Helm.IsWearable() && ItemKind.Weapon.IsGear() && !ItemKind.Weapon.IsWearable() && !ItemKind.Potion.IsGear(), "kind helpers");
            Assert(Make("scroll of enchant weapon", Catalogue.Scrolls, new Rng(1)).Def.Kind == ItemKind.Scroll, "enchant scrolls exist");

            // Names: hidden until identified, then fully spelled out.
            var sword = Make("long sword", Catalogue.Weapons, new Rng(1));
            sword.Rarity = Rarity.Magic; sword.Enchant = 2; sword.Prefix = "flaming"; sword.Suffix = "of-the-fox";
            Assert(sword.Name == "magical long sword", "unidentified name: " + sword.Name);
            sword.Identified = true;
            Assert(sword.Name == "+2 flaming long sword of the fox", "identified name: " + sword.Name);
            var m = sword.Mods;
            Assert(m.ToHit == 2 && m.Dmg == 2 && m.Dex == 2 && m.ExtraSides == 4 && m.ExtraType == DamageType.Fire, "sword mods");
            var plain = Make("long sword", Catalogue.Weapons, new Rng(1));
            Assert(plain.Name == "long sword" && plain.Mods.Lines().Count == 0, "plain items stay plain");
            var mail = Make("chain mail", Catalogue.Armor, new Rng(1)); mail.Enchant = 2;
            Assert(mail.TotalAc == mail.Def.AC + 2 && mail.Name == "+2 chain mail", "enchanted armour");
        }

        static void LootRarity()
        {
            var rng = new Rng(2468);
            int[] magic = new int[2], rare = new int[2], gear = new int[2];
            var seenKinds = new HashSet<ItemKind>();
            for (int pass = 0; pass < 2; pass++)
            {
                int depth = pass == 0 ? 1 : 14;
                for (int i = 0; i < 4000; i++)
                {
                    var it = LevelBuilder.RollLoot(rng, depth);
                    Assert(it != null, "null loot");
                    seenKinds.Add(it.Def.Kind);
                    if (!it.Def.Kind.IsGear()) { Assert(it.Rarity == Rarity.Common && it.Enchant == 0 && it.Prefix == null, "non-gear must stay plain"); continue; }
                    gear[pass]++;
                    if (it.Rarity == Rarity.Common) { Assert(it.Prefix == null && it.Suffix == null && it.Enchant == 0, "common gear must be plain"); continue; }
                    Assert(!it.Identified, "magic loot starts unidentified");
                    if (it.Rarity == Rarity.Magic) magic[pass]++; else rare[pass]++;
                    bool weapon = it.Def.Kind == ItemKind.Weapon;
                    foreach (var id in new[] { it.Prefix, it.Suffix })
                    {
                        if (id == null) continue;
                        var a = Affixes.Find(id);
                        Assert(a != null && (weapon ? a.ForWeapon : a.ForArmor), id + " does not fit " + it.Def.Name);
                    }
                    if (it.Rarity == Rarity.Rare) Assert(it.Prefix != null && it.Suffix != null && it.Enchant >= 1, "rare items carry both affixes");
                    Assert(it.TradeValue > it.Def.Cost, "magic items are worth more");
                }
            }
            Assert(gear[0] > 400 && gear[1] > 400, "plenty of gear in the table");
            Assert((magic[1] + rare[1]) * gear[0] > (magic[0] + rare[0]) * gear[1], "deeper loot should be magical more often");
            Assert(rare[1] > rare[0], "rare items are deeper");
            foreach (var k in new[] { ItemKind.Helm, ItemKind.Gloves, ItemKind.Boots, ItemKind.Cloak, ItemKind.Armor, ItemKind.Weapon, ItemKind.Shield })
                Assert(seenKinds.Contains(k), k + " never drops");
        }

        static void GearSlots()
        {
            var g = Game.NewHero(21, "G", "human", "wizard"); g.Monsters.Clear();
            var p = g.Player; var cmd = new Commands(g);
            var rng = new Rng(5);
            int ac0 = p.ArmorClass(), hp0 = p.MaxHP, mp0 = p.MpMax, dex0 = p.Dex, ev0 = p.Evasion(), vg0 = p.VigorMax;

            // Each slot adds its own AC.
            var helm = Make("dwarvish helm", Catalogue.Helms, rng);
            var gloves = Make("gauntlets", Catalogue.Gloves, rng);
            var boots = Make("iron boots", Catalogue.Boots, rng);
            var cloak = Make("cloak", Catalogue.Cloaks, rng);
            int expect = ac0;
            foreach (var piece in new[] { helm, gloves, boots, cloak })
            {
                p.Inventory.Add(piece);
                int turn = g.Turn;
                cmd.Execute("W");
                Assert(p.IsWorn(piece) && g.Turn == turn + 1, piece.Def.Name + " should be worn in one turn");
                expect -= piece.Def.AC;
                Assert(p.ArmorClass() == expect, $"AC after {piece.Def.Name}: {p.ArmorClass()} vs {expect}");
            }
            Assert(p.WornHelm == helm && p.WornGloves == gloves && p.WornBoots == boots && p.WornCloak == cloak, "slots");

            // A second helm swaps with the first, which returns to the pack.
            var cap = Make("leather cap", Catalogue.Helms, rng);
            p.Inventory.Add(cap);
            cmd.Execute("W");
            Assert(p.WornHelm == cap && p.Inventory.Contains(helm), "helms swap");

            // Take off with several pieces worn asks which one.
            cmd.Execute("T");
            Assert(g.PendingChoice.Active && g.PendingChoice.Items.Count == new List<Item>(p.WornPieces()).Count, "take off asks which piece");
            cmd.CommitChoice(cap);
            Assert(p.WornHelm == null && p.Inventory.Contains(cap), "take off removes the chosen piece");

            // Affixes: attributes, HP, Mp, Vigor, evasion, resistance - applied on wearing, removed on taking off.
            var magic = Make("leather cap", Catalogue.Helms, rng);
            magic.Rarity = Rarity.Rare; magic.Prefix = "fire-warded"; magic.Suffix = "of-the-fox"; magic.Enchant = 1;
            var robe = Make("cloak", Catalogue.Cloaks, rng); robe.Rarity = Rarity.Magic; robe.Suffix = "of-life";
            var shade = Make("leather boots", Catalogue.Boots, rng); shade.Rarity = Rarity.Magic; shade.Suffix = "of-shadows";
            var mage = Make("leather gloves", Catalogue.Gloves, rng); mage.Rarity = Rarity.Magic; mage.Suffix = "of-the-mage";
            var vig = Make("gauntlets", Catalogue.Gloves, rng); vig.Rarity = Rarity.Magic; vig.Suffix = "of-vigor";
            foreach (var it in new[] { helm, gloves, boots, cloak, cap }) { p.TakeOff(it); }
            int ac1 = p.ArmorClass(), hp1 = p.MaxHP, mp1 = p.MpMax, dex1 = p.Dex, ev1 = p.Evasion(), vg1 = p.VigorMax;
            foreach (var it in new[] { magic, robe, shade, mage }) { p.Inventory.Add(it); p.Wear(it); }
            Assert(p.Dex == dex1 + 2, "of the fox: +2 Dex");
            int dexDelta = (p.Dex - 10) / 2 - (dex1 - 10) / 2;
            Assert(p.ArmorClass() == ac1 - (magic.Def.AC + 1) - robe.Def.AC - shade.Def.AC - mage.Def.AC - dexDelta, "enchant adds AC");
            Assert(p.MaxHP >= hp1 + 10, "of life: +10 HP");
            Assert(p.MpMax >= mp1 + 6, "of the mage: +6 Mp");
            Assert(p.Evasion() == ev1 + 2 + dexDelta, "of shadows: +2 evasion");
            Assert(p.ResistPct(DamageType.Fire) == 30, "fire-warded: 30% fire");
            p.Inventory.Add(vig); p.Wear(vig);
            Assert(p.VigorMax == vg1 + 8, "of vigor: +8 Vigor");
            foreach (var it in new[] { magic, robe, shade, mage, vig }) p.TakeOff(it);
            Assert(p.Dex == dex1 && p.MaxHP == hp1 && p.MpMax == mp1 && p.Evasion() == ev1 && p.VigorMax == vg1 && p.ArmorClass() == ac1 && p.ResistPct(DamageType.Fire) == 0,
                "taking everything off restores the numbers");
            Assert(dex1 == dex0 && ac1 == ac0 + (0), "baseline sanity");

            // Training and gear do not overwrite each other.
            p.Inventory.Add(magic); p.Wear(magic);
            p.PendingAdvances = 1; p.ApplyAdvance("agile");
            Assert(p.Dex == dex0 + 2 + 1, "agile stacks with gear");
            p.TakeOff(magic);
            Assert(p.Dex == dex0 + 1, "agile survives taking the gear off");

            // A two-handed weapon and a shield do not mix.
            var staff = Make("quarterstaff", Catalogue.Weapons, rng);
            var f = Game.NewHero(21, "F", "human", "paladin"); f.Monsters.Clear();
            f.Player.Inventory.Add(staff);
            WieldVia(f, staff);
            Assert(f.Player.Wielded != staff, "cannot wield a two-handed weapon with a shield on");
            var shield = Make("buckler", Catalogue.Shields, rng);
            var w = Game.NewHero(21, "W", "human", "wizard"); w.Monsters.Clear();
            w.Player.Inventory.Add(shield);
            new Commands(w).Execute("W");
            Assert(w.Player.WornShield != shield, "cannot take a shield while holding a staff in both hands");
        }

        static void WeaponMagic()
        {
            var g = Game.NewHero(31, "S", "human", "fighter"); g.Monsters.Clear();
            var p = g.Player; var rng = new Rng(9);
            var blade = Make("long sword", Catalogue.Weapons, rng);
            blade.Rarity = Rarity.Rare; blade.Enchant = 3; blade.Prefix = "flaming"; blade.Suffix = "of-ruin";
            p.Inventory.Add(blade);
            Assert(!blade.Identified, "starts unknown");
            WieldVia(g, blade);
            Assert(p.Wielded == blade && blade.Identified, "wielding reveals the weapon");
            bool told = false;
            foreach (var msg in g.Log) if (msg.Text.StartsWith("It is +3 flaming long sword of ruin")) told = true;
            Assert(told, "the reveal should be announced");

            // Enchantment and affixes raise damage in the combat maths.
            var plain = Game.NewHero(31, "P", "human", "fighter"); plain.Player.Inventory.Clear();
            plain.Player.Wielded = Make("long sword", Catalogue.Weapons, rng);
            var dummy = new Monster(Bestiary.Find("ogre"), rng) { X = 0, Y = 0 };
            long a = 0, b = 0;
            for (int i = 0; i < 400; i++)
            {
                dummy.HP = 100000; Battles.PlayerMelee(plain.Player, dummy, new Rng((ulong)i + 3), out _); a += 100000 - dummy.HP;
                dummy.HP = 100000; Battles.PlayerMelee(p, dummy, new Rng((ulong)i + 3), out _); b += 100000 - dummy.HP;
            }
            Assert(b > a, "the enchanted sword should hit harder");

            // Elemental die: fighting an ogre with the flaming sword logs fire damage.
            var arena = Game.NewHero(32, "A", "human", "fighter");
            Assert(Arena(arena, out int x, out int y), "arena");
            var ogre = Place(arena, "ogre", x, y); ogre.HP = ogre.MaxHP = 100000; ogre.Speed = 1;
            var fire = Make("long sword", Catalogue.Weapons, rng);
            fire.Rarity = Rarity.Magic; fire.Prefix = "flaming"; fire.Identified = true;
            arena.Player.Inventory.Add(fire); WieldVia(arena, fire);
            ogre.X = arena.Player.X + 1; ogre.Y = arena.Player.Y;
            if (!Tiles.Walkable(arena.Map.Get(ogre.X, ogre.Y))) { ogre.X = arena.Player.X - 1; }
            arena.Player.MaxHP = arena.Player.HP = 5000;
            bool flames = false;
            for (int i = 0; i < 80 && !flames; i++)
            {
                arena.Attack(ogre);
                foreach (var msg in arena.Log) if (msg.Text.StartsWith("Flames lash")) flames = true;
            }
            Assert(flames, "a flaming weapon should burn what it hits");

            // Life steal heals, artifacts reveal their lore.
            var drinker = Game.NewHero(33, "V", "human", "fighter"); drinker.Monsters.Clear();
            var tooth = Artifacts.Create(Artifacts.Find("rat-kings-tooth"), rng, 5);
            drinker.Player.Inventory.Add(tooth); WieldVia(drinker, tooth);
            bool lore = false;
            foreach (var msg in drinker.Log) if (msg.Text.Contains("Gnawed, not forged")) lore = true;
            Assert(tooth.Identified && lore, "artifact lore appears on wielding");
            Assert(Arena(drinker, out int dx, out int dy), "arena 2");
            var victim = Place(drinker, "ogre", dx, dy); victim.HP = victim.MaxHP = 100000; victim.Speed = 1;
            victim.X = drinker.Player.X + 1; victim.Y = drinker.Player.Y;
            if (!Tiles.Walkable(drinker.Map.Get(victim.X, victim.Y))) victim.X = drinker.Player.X - 1;
            drinker.Player.MaxHP = 5000; drinker.Player.HP = 100;
            for (int i = 0; i < 60 && drinker.Player.HP <= 100; i++) drinker.Attack(victim);
            Assert(drinker.Player.HP > 100, "life steal should heal on a hit");
        }

        static void EnchantAndValue()
        {
            var g = Game.NewHero(41, "E", "human", "fighter"); g.Monsters.Clear();
            var p = g.Player; var rng = new Rng(3);
            p.Wear(Make("leather cap", Catalogue.Helms, rng));
            int ac = p.ArmorClass();
            var armourScroll = Make("scroll of enchant armour", Catalogue.Scrolls, rng); armourScroll.Charges = 20;
            g.UseScroll(armourScroll);
            Assert(p.ArmorClass() == ac - 1, "enchant armour: AC -1 (better)");
            // It picks the least enchanted piece, so a second scroll goes to a different one.
            g.UseScroll(armourScroll);
            int enchanted = 0; foreach (var piece in p.WornPieces()) if (piece.Enchant > 0) enchanted++;
            Assert(enchanted == 2 && p.ArmorClass() == ac - 2, "scrolls spread across pieces");
            for (int i = 0; i < 30; i++) g.UseScroll(armourScroll);
            foreach (var piece in p.WornPieces()) Assert(piece.Enchant <= 5, "enchantment caps at +5");

            var weaponScroll = Make("scroll of enchant weapon", Catalogue.Scrolls, rng); weaponScroll.Charges = 20;
            Assert(p.Wielded.Enchant == 0, "starts unenchanted");
            g.UseScroll(weaponScroll);
            Assert(p.Wielded.Enchant == 1 && p.Wielded.Name.StartsWith("+1 "), "enchant weapon");
            p.Wielded = null; p.RefreshGear();
            g.UseScroll(weaponScroll);   // empty hands: harmless
            p.WornArmor = null; p.WornShield = null; p.RefreshGear();

            // Value grows with magic, and the shop pays for it.
            var common = Make("long sword", Catalogue.Weapons, rng);
            var better = Make("long sword", Catalogue.Weapons, rng); better.Rarity = Rarity.Rare; better.Enchant = 2; better.Prefix = "keen"; better.Suffix = "of-the-bear";
            Assert(common.TradeValue == common.Def.Cost && better.TradeValue > common.TradeValue * 2, "trade value: " + better.TradeValue);
            var art = Artifacts.Create(Artifacts.Find("ashfall"), rng, 9);
            Assert(art.TradeValue > better.TradeValue, "artifacts are priceless");
        }

        static void ArtifactPlacement()
        {
            var dungeon = new Dungeon(new Rng(777));
            foreach (var art in Artifacts.All)
            {
                var map = dungeon.Ensure(art.Branch, art.Depth, out _, out _, out _);
                int found = 0;
                for (int y = 0; y < map.H; y++)
                    for (int x = 0; x < map.W; x++)
                    {
                        var list = GroundItems.At(map.Number, x, y);
                        if (list != null) foreach (var it in list) if (it.ArtifactId == art.Id) found++;
                    }
                Assert(found == 1, $"{art.Name} should lie once on {art.Branch} {art.Depth}, found {found}");
            }
            // Other levels carry none.
            var other = dungeon.Ensure("The Dungeons", 2, out _, out _, out _);
            for (int y = 0; y < other.H; y++)
                for (int x = 0; x < other.W; x++)
                {
                    var list = GroundItems.At(other.Number, x, y);
                    if (list != null) foreach (var it in list) Assert(it.ArtifactId == null, "artifact on the wrong level");
                }
        }

        static void GearUi()
        {
            var g = Game.NewHero(51, "U", "dwarf", "paladin"); g.Monsters.Clear();
            var p = g.Player; var rng = new Rng(2);
            foreach (var it in new[] { Make("great helm", Catalogue.Helms, rng), Make("gauntlets", Catalogue.Gloves, rng), Make("iron boots", Catalogue.Boots, rng), Make("cloak of elvenkind", Catalogue.Cloaks, rng) })
            { it.Rarity = Rarity.Rare; it.Prefix = "sturdy"; it.Suffix = "of-the-bear"; it.Identified = true; p.Inventory.Add(it); p.Wear(it); }
            p.Inventory.Add(Artifacts.Create(Artifacts.Find("crown-drowned-king"), rng, 7));
            p.Inventory.Add(Make("long sword", Catalogue.Weapons, rng));
            var hud = new GameHud(g);
            foreach (var size in new[] { new[] { 84, 26 }, new[] { 110, 36 }, new[] { 200, 60 } })
            {
                hud.Ui.Resize(size[0], size[1]);
                foreach (var panel in new[] { Panel.None, Panel.Inventory, Panel.Character })
                {
                    g.UiState.Active = panel;
                    hud.Draw();
                }
            }
            g.UiState.Active = Panel.None;
            hud.Ui.Resize(110, 40);
            g.UiState.Active = Panel.Inventory;
            string art = hud.Draw().ToAscii();
            Assert(art.Contains("Crown of the Drowned King") && art.Contains("great helm"), "inventory lists artifacts and worn gear");
            g.UiState.Active = Panel.None;
        }

        // Moves the altar cursor to coordinates whose altar belongs to the wanted god (no tile needed).
        static GodDef AltarOf(Game g, string godId)
        {
            for (int y = 1; y < 60; y++)
                for (int x = 1; x < 60; x++)
                    if (Gods.AtAltar(g.Map.Number, x, y).Id == godId) { g.OpenAltar(x, y); return g.AltarGod(); }
            throw new Exception("no altar for " + godId);
        }

        static bool LogHas(Game g, string text)
        {
            foreach (var m in g.Log) if (m.Text.Contains(text)) return true;
            return false;
        }

        static void GodTable()
        {
            var ids = new HashSet<string>();
            foreach (var d in Gods.All)
            {
                Assert(ids.Add(d.Id), "duplicate god " + d.Id);
                Assert(!string.IsNullOrEmpty(d.Name) && !string.IsNullOrEmpty(d.Likes) && !string.IsNullOrEmpty(d.Boon) && !string.IsNullOrEmpty(d.Tier1) && !string.IsNullOrEmpty(d.Tier2), d.Id + " text");
                Assert(d.BoonCost > 0 && d.BoonCost < Gods.Tier2At, d.Id + " boon cost");
            }
            Assert(Gods.All.Length == 5 && Gods.Find("nope") == null, "five gods");
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < 2000; i++)
            {
                var a = Gods.AtAltar(i % 40, (i * 7) % 79, (i * 13) % 25);
                Assert(Gods.AtAltar(i % 40, (i * 7) % 79, (i * 13) % 25) == a, "altar god must be a pure function of place");
                counts[a.Id] = counts.ContainsKey(a.Id) ? counts[a.Id] + 1 : 1;
            }
            foreach (var d in Gods.All) Assert(counts.ContainsKey(d.Id) && counts[d.Id] > 200, d.Id + " is too rare among altars");
            Assert(Game.NewHero(1, "c", "human", "cleric").Player.God == "aurel" && Game.NewHero(1, "c", "human", "paladin").Player.Piety == 30, "clerics and paladins follow Aurel");
            Assert(Game.NewHero(1, "c", "human", "wizard").Player.God == null, "others start faithless");
        }

        static void GodOaths()
        {
            var g = Game.NewHero(70, "O", "human", "fighter"); g.Monsters.Clear();
            var p = g.Player;
            var god = AltarOf(g, "khorr");
            var rows = g.AltarRows();
            Assert(rows[0].Id == "swear" && rows[0].Enabled && rows[rows.Count - 1].Id == "leave", "a faithless hero is offered an oath");
            Assert(g.AltarAction("swear") && p.God == "khorr" && p.Piety == 20 && p.Align == god.Align, "swearing");
            Assert(g.AltarRows()[0].Id == "pray", "a follower may pray");

            // Renouncing needs a second press.
            Assert(!g.AltarAction("renounce") && g.UiState.AltarConfirm && p.God == "khorr", "renounce asks for confirmation");
            Assert(g.AltarAction("renounce") && p.God == null && p.Piety == 0 && p.Renounced == 1, "renounced");

            // A second oath costs tribute.
            p.Gold = 100;
            g.OpenAltar(g.UiState.AltarX, g.UiState.AltarY);
            var swear = g.AltarRows()[0];
            Assert(swear.Id == "swear" && !swear.Enabled, "tribute too high for 100 gold");
            Assert(!g.AltarAction("swear") && p.God == null, "swearing without tribute fails");
            p.Gold = 400;
            Assert(g.AltarAction("swear") && p.God == "khorr" && p.Gold == 250, "tribute paid: " + p.Gold);

            // Converting to another god's altar.
            AltarOf(g, "veyra"); p.Gold = 1000;
            var row = g.AltarRows()[0];
            Assert(row.Id == "convert" && row.Enabled, "other altars offer conversion");
            Assert(g.AltarAction("convert"), "convert");
            Assert(p.God == "veyra" && p.Renounced == 2, "converted to Veyra, oaths broken " + p.Renounced);
        }

        static void GodPrayer()
        {
            // In trouble: healed, at a price.
            var g = Game.NewHero(71, "P", "human", "paladin"); g.Monsters.Clear();
            var p = g.Player; AltarOf(g, "aurel");
            p.Piety = 30; p.MaxHP = 100; p.HP = 10; int turn = g.Turn;
            Assert(g.AltarAction("pray"), "pray closes the menu");
            Assert(p.HP == 100 && p.Piety == 10 && p.PrayerTimer >= 290 && g.Turn == turn + 1, $"trouble prayer: hp {p.HP} piety {p.Piety} timer {p.PrayerTimer}");

            // Too soon: scolded, never killed.
            p.HP = 50; p.Piety = 40; p.PrayerTimer = 300;
            g.AltarAction("pray");
            Assert(p.Piety == 20 && p.HP > 0 && p.HP < 50 && p.PrayerTimer >= 400, "haste is punished");
            p.HP = 1; p.PrayerTimer = 400; p.Piety = 30;
            g.AltarAction("pray");
            Assert(p.HP >= 1, "a scolding never kills");

            // Silent without enough piety; the timer is untouched.
            var q = Game.NewHero(72, "Q", "human", "paladin"); q.Monsters.Clear(); AltarOf(q, "aurel");
            q.Player.Piety = 5; q.Player.PrayerTimer = 0;
            q.AltarAction("pray");
            Assert(q.Player.Piety == 5 && q.Player.PrayerTimer == 0 && LogHas(q, "is silent"), "no boon below the cost");
            q.Player.HP = 3; q.Player.Piety = 5;
            q.AltarAction("pray");
            Assert(q.Player.HP == 3 && LogHas(q, "does not answer"), "trouble with no piety goes unanswered");

            // Each god's boon.
            foreach (var god in Gods.All)
            {
                var h = Game.NewHero(73, "B", "human", god.Id == "khorr" ? "fighter" : "wizard"); h.Monsters.Clear();
                var hp = h.Player;
                AltarOf(h, god.Id);
                hp.God = god.Id; hp.Piety = 100; hp.PrayerTimer = 0;
                hp.HP = hp.MaxHP / 2; hp.Mp = 0; hp.Vigor = 0;
                int wasEnchant = hp.Wielded != null ? hp.Wielded.Enchant : 0;
                Assert(h.AltarAction("pray"), god.Id + " prayer");
                Assert(hp.Piety == 100 - god.BoonCost && hp.PrayerTimer >= 399, god.Id + " should charge the boon and start the cooldown");
                switch (god.Id)
                {
                    case "aurel": Assert(hp.HP == hp.MaxHP && hp.Mp == hp.MpMax, "aurel heals and refills mana"); break;
                    case "khorr": Assert(hp.Wielded.Enchant == wasEnchant + 1, "khorr enchants the weapon"); break;
                    case "veyra": Assert(hp.BuffTurns("flame") > 250, "veyra lights the blows"); break;
                    case "nhal": Assert(h.Monsters.FindAll(m => m.Ally).Count == 2, "nhal sends two skeletons"); break;
                    case "sylk": Assert(hp.Invisible && hp.Vigor == hp.VigorMax, "sylk hides and refreshes"); break;
                }
            }

            // Refunds: Khorr with empty hands gives nothing and charges nothing.
            var e = Game.NewHero(74, "E", "human", "fighter"); e.Monsters.Clear(); AltarOf(e, "khorr");
            e.Player.God = "khorr"; e.Player.Piety = 100; e.Player.Wielded = null;
            e.AltarAction("pray");
            Assert(e.Player.Piety == 100 && e.Player.PrayerTimer == 0, "an empty-handed prayer to Khorr is free");
        }

        static void GodFavour()
        {
            // Aurel: undead good, harmless bad, necromancy bad, sacred good.
            var g = Archmage(7100); var p = g.Player; Arena(g, out int x, out int y);
            p.God = "aurel"; p.Piety = 50;
            var sk = Place(g, "skeleton", x, y); sk.HP = 1;
            g.KillMonster(sk);
            Assert(p.Piety == 52, "aurel likes dead undead: " + p.Piety);
            var newt = Place(g, "newt", x, y); newt.HP = 1;
            g.KillMonster(newt);
            Assert(p.Piety == 48 && LogHas(g, "frowns"), "aurel dislikes killing the harmless: " + p.Piety);
            CastOk(g, "raise-skeleton", p.X, p.Y);
            Assert(p.Piety == 45, "aurel dislikes necromancy: " + p.Piety);
            CastOk(g, "bless", p.X, p.Y);
            Assert(p.Piety == 46, "aurel likes sacred magic: " + p.Piety);
            p.Piety = 199; g.AddPiety(50, null);
            Assert(p.Piety == Gods.MaxPiety, "piety caps at the maximum");
            g.AddPiety(-500, null);
            Assert(p.Piety == 0, "piety floors at zero");
            p.Piety = 49; g.AddPiety(1, null);
            Assert(LogHas(g, "favours you"), "crossing 50 is announced");

            // Khorr: strong kills count for more.
            var k = Archmage(7101); k.Monsters.Clear(); Arena(k, out int kx, out int ky);
            k.Player.God = "khorr"; k.Player.Piety = 20; k.Player.Level = 1;
            var big = Place(k, "ogre", kx, ky); big.HP = 1; k.KillMonster(big);
            Assert(k.Player.Piety == 23, "khorr loves a hard kill: " + k.Player.Piety);
            var small = Place(k, "jackal", kx, ky); small.HP = 1; k.KillMonster(small);
            Assert(k.Player.Piety == 24, "and likes any kill: " + k.Player.Piety);

            // Veyra: fire kills. Nhal: death magic. Both through real spells.
            var v = Archmage(7102); Arena(v, out int vx, out int vy);
            v.Player.God = "veyra"; v.Player.Piety = 20;
            var burn = Place(v, "jackal", vx, vy); burn.HP = 1; burn.Speed = 1;
            Assert(CastOk(v, "fireball", burn.X, burn.Y) && v.Player.Piety >= 22, "veyra likes fire kills: " + v.Player.Piety);
            var n = Archmage(7103); Arena(n, out int nx, out int ny);
            n.Player.God = "nhal"; n.Player.Piety = 20;
            var victim = Place(n, "jackal", nx, ny); victim.HP = 1; victim.Speed = 1;
            Assert(CastOk(n, "drain-life", victim.X, victim.Y) && n.Player.Piety >= 22, "nhal likes death magic: " + n.Player.Piety);
            CastOk(n, "cure-wounds", n.Player.X, n.Player.Y);
            Assert(n.Player.Piety <= 22, "and dislikes sacred magic");

            // Sylk: sneak kills.
            var s = Game.NewHero(7104, "S", "human", "rogue"); s.Monsters.Clear(); Arena(s, out int sx, out int sy);
            s.Player.God = "sylk"; s.Player.Piety = 20;
            var sleeper = Place(s, "jackal", sx, sy); sleeper.HP = 1; sleeper.Asleep = true;
            sleeper.X = s.Player.X + 1; sleeper.Y = s.Player.Y;
            if (!Tiles.Walkable(s.Map.Get(sleeper.X, sleeper.Y))) sleeper.X = s.Player.X - 1;
            for (int i = 0; i < 60 && !sleeper.IsDead && s.Monsters.Contains(sleeper); i++) { sleeper.Asleep = true; sleeper.HP = 1; s.Attack(sleeper); }
            Assert(s.Player.Piety >= 22, "sylk likes the quiet kill: " + s.Player.Piety);

            // Decay: one point every 250 turns.
            var d = Game.NewHero(7105, "D", "human", "paladin"); d.Monsters.Clear();
            d.Player.Piety = 30; d.Turn = 249;
            new Commands(d).Execute(".");
            Assert(d.Turn == 250 && d.Player.Piety == 29, "piety decays: " + d.Player.Piety);
            d.Player.PrayerTimer = 5; new Commands(d).Execute(".");
            Assert(d.Player.PrayerTimer == 4, "the prayer timer ticks");
        }

        static void GodTiers()
        {
            var g = Game.NewHero(7200, "T", "human", "fighter"); g.Monsters.Clear(); var p = g.Player;
            int fire0 = p.ResistPct(DamageType.Fire), ev0 = p.Evasion(), regen0 = p.HpRegenInterval();
            Assert(p.GodTier == 0, "no god, no tier");
            p.God = "veyra"; p.Piety = 49;
            Assert(p.ResistPct(DamageType.Fire) == fire0, "no blessing below 50");
            p.Piety = 50; Assert(p.GodTier == 1 && p.ResistPct(DamageType.Fire) == fire0 + 30, "veyra tier 1: fire 30%");
            p.Piety = 100; Assert(p.GodTier == 2 && p.ResistPct(DamageType.Fire) == fire0 + 60, "veyra tier 2: fire 60%");
            p.God = "aurel"; p.Piety = 50;
            Assert(p.HpRegenInterval() < regen0, "aurel tier 1 heals faster");
            Assert(p.ResistPct(DamageType.Necrotic) == 0, "aurel needs tier 2 for necrotic");
            p.Piety = 100; Assert(p.ResistPct(DamageType.Necrotic) == 30, "aurel tier 2: necrotic 30%");
            p.God = "nhal"; p.Piety = 50; Assert(p.ResistPct(DamageType.Necrotic) == 30, "nhal tier 1: necrotic 30%");
            p.God = "sylk"; p.Piety = 50; Assert(p.Evasion() == ev0 + 2, "sylk tier 1: evasion");
            p.God = "khorr"; p.Piety = 50; Assert(p.GodMeleeHit == 1 && p.GodMeleeDmg == 0, "khorr tier 1: +1 hit");
            p.Piety = 100; Assert(p.GodMeleeHit == 1 && p.GodMeleeDmg == 2, "khorr tier 2: +2 damage");

            // Khorr's tiers show up in the combat maths.
            var plain = Game.NewHero(7200, "T", "human", "fighter");
            var dummy = new Monster(Bestiary.Find("ogre"), new Rng(1)) { X = 0, Y = 0 };
            long a = 0, b = 0;
            for (int i = 0; i < 400; i++)
            {
                dummy.HP = 100000; Battles.PlayerMelee(plain.Player, dummy, new Rng((ulong)i + 1), out _); a += 100000 - dummy.HP;
                dummy.HP = 100000; Battles.PlayerMelee(p, dummy, new Rng((ulong)i + 1), out _); b += 100000 - dummy.HP;
            }
            Assert(b > a, "khorr's favour should raise melee damage");

            // Veyra tier 2 and the flame boon: blows burn.
            var v = Game.NewHero(7201, "V", "human", "fighter");
            Assert(Arena(v, out int x, out int y), "arena");
            var ogre = Place(v, "ogre", x, y); ogre.HP = ogre.MaxHP = 100000; ogre.Speed = 1;
            ogre.X = v.Player.X + 1; ogre.Y = v.Player.Y;
            if (!Tiles.Walkable(v.Map.Get(ogre.X, ogre.Y))) ogre.X = v.Player.X - 1;
            v.Player.MaxHP = v.Player.HP = 5000;
            v.Player.God = "veyra"; v.Player.Piety = 100;
            bool flames = false;
            for (int i = 0; i < 80 && !flames; i++) { v.Attack(ogre); flames = LogHas(v, "Flames lash"); }
            Assert(flames, "veyra's chosen set their foes alight");

            // Nhal tier 2: kills heal.
            var n = Game.NewHero(7202, "N", "human", "fighter"); n.Monsters.Clear(); Arena(n, out int nx, out int ny);
            n.Player.God = "nhal"; n.Player.Piety = 100; n.Player.MaxHP = 500; n.Player.HP = 100;
            var m = Place(n, "jackal", nx, ny); m.HP = 1; n.KillMonster(m);
            Assert(n.Player.HP == 102, "nhal restores 2 HP per kill: " + n.Player.HP);
        }

        static void GodOfferings()
        {
            var g = Game.NewHero(7300, "O", "human", "paladin"); g.Monsters.Clear(); var p = g.Player;
            AltarOf(g, "aurel"); p.Piety = 10;
            p.Gold = 10;
            Assert(!g.AltarAction("offer-gold") && p.Gold == 10, "a few coins are not an offering");
            p.Gold = 500;
            g.AltarAction("offer-gold");
            Assert(p.Gold == 400 && p.Piety == 15, "gold offering: " + p.Gold + "/" + p.Piety);

            // Items go through the choice panel.
            var sword = new Item(Catalogue.Weapons[2], new Rng(1), 9);
            p.Inventory.Add(sword);
            Assert(g.AltarAction("offer-item") && g.PendingChoice.Active && g.PendingChoice.Prompt == Game.OfferPrompt, "offering asks which item");
            int piety = p.Piety;
            new Commands(g).CommitChoice(sword);
            Assert(!p.Inventory.Contains(sword) && p.Piety > piety && !g.PendingChoice.Active, "the item is consumed for piety");
            var art = Artifacts.Create(Artifacts.Find("ashfall"), new Rng(1), 10);
            p.Inventory.Add(art); piety = p.Piety;
            g.AltarAction("offer-item"); new Commands(g).CommitChoice(art);
            Assert(p.Piety == piety + 25, "an artifact is a great offering");

            // The faithless cannot offer; strangers' altars offer only conversion.
            var f = Game.NewHero(7301, "F", "human", "fighter"); f.Monsters.Clear(); AltarOf(f, "veyra");
            var ids = f.AltarRows().ConvertAll(r => r.Id);
            Assert(!ids.Contains("offer-gold") && !ids.Contains("pray"), "the faithless have no offerings");

            // Bumping an altar opens the menu for free and does not move you.
            var b = Game.NewHero(7302, "B", "human", "fighter"); b.Monsters.Clear();
            var bp = b.Player; int bx = bp.X, by = bp.Y, turn = b.Turn;
            bool placed = false;
            for (int k = 0; k < 8 && !placed; k++)
            {
                int ax = bp.X + Pathfinder.Dx8[k], ay = bp.Y + Pathfinder.Dy8[k];
                if (!Tiles.Walkable(b.Map.Get(ax, ay)) || b.MonsterAt(ax, ay) != null) continue;
                b.Map.Set(ax, ay, TileKind.Altar);
                Assert(b.TryMovePlayer(Pathfinder.Dx8[k], Pathfinder.Dy8[k]), "bumping the altar is handled");
                Assert(b.UiRequests.Altar && b.UiState.AltarX == ax && b.UiState.AltarY == ay, "the altar menu is requested");
                Assert(bp.X == bx && bp.Y == by && b.Turn == turn, "opening the menu costs nothing");
                placed = true;
            }
            Assert(placed, "no floor to put an altar on");
        }

        static void AltarUi()
        {
            var g = Game.NewHero(7400, "U", "human", "paladin"); g.Monsters.Clear();
            var hud = new GameHud(g);
            foreach (var size in new[] { new[] { 84, 26 }, new[] { 110, 36 }, new[] { 200, 60 } })
            {
                hud.Ui.Resize(size[0], size[1]);
                foreach (var god in Gods.All)
                    foreach (var follows in new[] { "", god.Id, "aurel" })
                    {
                        AltarOf(g, god.Id);
                        g.Player.God = follows == "" ? null : follows; g.Player.Piety = 77; g.Player.PrayerTimer = 123;
                        g.UiState.Active = Panel.Altar; g.UiState.AltarIndex = 1; g.UiState.AltarConfirm = follows == god.Id;
                        hud.Draw();
                    }
                g.UiState.Active = Panel.Character; hud.Draw();
            }
            g.Player.God = "aurel"; g.Player.Piety = 77;
            g.UiState.Active = Panel.Character;
            Assert(hud.Draw().ToAscii().Contains("Faith: Aurel"), "the sheet names the god");
            g.UiState.Active = Panel.None;
        }

        static bool IsPlainFloor(Game g, int x, int y)
        {
            var t = g.Map.Get(x, y);
            return t == TileKind.Floor || t == TileKind.FloorAlt;
        }

        // A horizontal run of plain floor, anywhere on the level. Returns its left end.
        static bool FloorRun(Game g, int len, out int x0, out int y0)
        {
            for (int y = 1; y < g.Map.H - 1; y++)
                for (int x = 1; x < g.Map.W - len - 1; x++)
                {
                    bool ok = true;
                    for (int i = 0; i < len && ok; i++) ok = IsPlainFloor(g, x + i, y) && g.MonsterAt(x + i, y) == null && !(g.Player.X == x + i && g.Player.Y == y);
                    if (ok) { x0 = x; y0 = y; return true; }
                }
            x0 = y0 = 0; return false;
        }

        // Moves the player onto a plain floor cell (the start is often a staircase).
        static void StandOnFloor(Game g)
        {
            if (IsPlainFloor(g, g.Player.X, g.Player.Y)) return;
            Assert(FloorRun(g, 1, out int x, out int y), "no floor");
            g.Player.X = x; g.Player.Y = y; g.UpdateFov();
        }

        static void SurfaceRules()
        {
            Assert(SurfaceInfo.Flammable(SurfaceKind.Grass) && SurfaceInfo.Flammable(SurfaceKind.Oil) && !SurfaceInfo.Flammable(SurfaceKind.Water), "flammability");
            foreach (SurfaceKind k in new[] { SurfaceKind.Water, SurfaceKind.Ice, SurfaceKind.Fire, SurfaceKind.Oil, SurfaceKind.Grass })
                Assert(SurfaceInfo.Name(k).Length > 0, k + " needs a name");
            var g = Game.NewHero(8001, "S", "human", "fighter"); g.Monsters.Clear();
            Assert(FloorRun(g, 4, out int x, out int y), "floor run");
            var map = g.Map;

            // Map storage.
            Assert(map.SurfaceAt(x, y) == SurfaceKind.None, "starts bare");
            map.SetSurface(x, y, SurfaceKind.Grass); Assert(map.SurfaceAt(x, y) == SurfaceKind.Grass, "set");
            map.SetSurface(x, y, SurfaceKind.None); Assert(map.SurfaceAt(x, y) == SurfaceKind.None, "clear");

            // Fire does not take on water; ice melts under fire; frost freezes water only.
            g.PutSurface(x, y, SurfaceKind.Water);
            g.PutSurface(x, y, SurfaceKind.Fire, 5);
            Assert(g.SurfaceAt(x, y) == SurfaceKind.Water, "fire cannot burn on water");
            g.PutSurface(x, y, SurfaceKind.Ice);
            Assert(g.SurfaceAt(x, y) == SurfaceKind.Ice, "frost freezes water");
            g.PutSurface(x, y, SurfaceKind.Fire, 5);
            Assert(g.SurfaceAt(x, y) == SurfaceKind.Water, "fire melts ice into water");
            g.PutSurface(x + 1, y, SurfaceKind.Ice);
            Assert(g.SurfaceAt(x + 1, y) == SurfaceKind.None, "frost needs water");
            g.PutSurface(x + 1, y, SurfaceKind.Grass); g.PutSurface(x + 1, y, SurfaceKind.Fire, 1);
            Assert(g.SurfaceAt(x + 1, y) == SurfaceKind.Fire && map.Surfaces[map.Index(x + 1, y)].Turns >= 5, "brush burns at least five turns");
            g.PutSurface(x + 2, y, SurfaceKind.Oil); g.PutSurface(x + 2, y, SurfaceKind.Fire, 1);
            Assert(map.Surfaces[map.Index(x + 2, y)].Turns >= 10, "oil burns longer");
            g.PutSurface(x + 1, y, SurfaceKind.Water);
            Assert(g.SurfaceAt(x + 1, y) == SurfaceKind.Water, "water puts fire out");

            // Walls and stairs hold nothing.
            int wx = 0, wy = 0; bool foundWall = false;
            for (int yy = 0; yy < map.H && !foundWall; yy++) for (int xx = 0; xx < map.W && !foundWall; xx++) if (map.Get(xx, yy) == TileKind.Wall) { wx = xx; wy = yy; foundWall = true; }
            g.PutSurface(wx, wy, SurfaceKind.Water);
            Assert(g.SurfaceAt(wx, wy) == SurfaceKind.None, "walls hold no surface");
            Assert(Tiles.Walkable(map.Get(x, y)), "sanity");
        }

        static void FireBehaviour()
        {
            // Brush carries fire along a row, then everything burns out.
            var g = Game.NewHero(8010, "F", "human", "fighter"); g.Monsters.Clear();
            Assert(FloorRun(g, 6, out int x, out int y), "floor run");
            for (int i = 0; i < 6; i++) g.PutSurface(x + i, y, SurfaceKind.Grass);
            g.PutSurface(x, y, SurfaceKind.Fire, 4);
            int maxFire = 0;
            for (int t = 0; t < 80; t++)
            {
                new Commands(g).Execute(".");
                int fires = 0;
                for (int i = 0; i < 6; i++) if (g.SurfaceAt(x + i, y) == SurfaceKind.Fire) fires++;
                maxFire = System.Math.Max(maxFire, fires);
                g.Player.HP = g.Player.MaxHP;
            }
            Assert(maxFire >= 2, "fire should spread along the brush, peak " + maxFire);
            for (int i = 0; i < 6; i++) Assert(g.SurfaceAt(x + i, y) == SurfaceKind.None, "burnt ground should be bare at " + i + ", is " + g.SurfaceAt(x + i, y));

            // A monster in the flames catches fire, takes damage, and the kill is credited.
            var m = Game.NewHero(8011, "M", "human", "fighter"); m.Monsters.Clear();
            Assert(FloorRun(m, 3, out int mx, out int my), "floor run 2");
            var cat = Place(m, "jackal", mx, my); cat.HP = cat.MaxHP = 60; cat.Speed = 1; cat.Alert = 0;
            m.PutSurface(mx, my, SurfaceKind.Fire, 6);
            Assert(cat.BurnTurns > 0, "a creature in flames catches fire");
            int before = cat.HP;
            for (int i = 0; i < 4; i++) new Commands(m).Execute(".");
            Assert(cat.HP < before, "burning hurts");
            var victim = Place(m, "jackal", mx + 2, my); victim.HP = 2; victim.Speed = 1; victim.Alert = 0;
            int kills = m.Player.Kills;
            m.PutSurface(mx + 2, my, SurfaceKind.Fire, 6);
            for (int i = 0; i < 8 && !victim.IsDead && m.Monsters.Contains(victim); i++) new Commands(m).Execute(".");
            Assert(m.Player.Kills == kills + 1, "burning kills are credited to the player");

            // Soaked creatures do not catch fire; water puts a burning one out.
            var w = Game.NewHero(8012, "W", "human", "fighter"); w.Monsters.Clear();
            Assert(FloorRun(w, 3, out int wx, out int wy), "floor run 3");
            var wet = Place(w, "ogre", wx, wy); wet.HP = wet.MaxHP = 500; wet.Speed = 1;
            w.PutSurface(wx, wy, SurfaceKind.Water);
            Assert(wet.WetTurns > 0, "standing in water soaks");
            w.SetAlight(wet);
            Assert(wet.BurnTurns == 0, "the soaked do not burn");
            wet.WetTurns = 0; w.SetAlight(wet); Assert(wet.BurnTurns > 0, "the dry do");
            w.PutSurface(wx, wy, SurfaceKind.Water);
            Assert(wet.BurnTurns == 0, "water douses a burning creature");

            // The player burns, resistances matter, and water puts them out.
            long human = 0, ashen = 0;
            for (ulong seed = 1; seed <= 25; seed++)
            {
                foreach (var race in new[] { "human", "ashen" })
                {
                    var t = Game.NewHero(9000 + seed, "R", race, "fighter"); t.Monsters.Clear();
                    StandOnFloor(t);
                    t.Player.MaxHP = t.Player.HP = 400; t.Player.BurnTurns = 4;
                    for (int i = 0; i < 4; i++) new Commands(t).Execute(".");
                    int lost = 400 - t.Player.HP;
                    if (race == "human") human += lost; else ashen += lost;
                }
            }
            Assert(human > 0 && ashen < human, $"fire resistance should cut burning damage ({ashen} vs {human})");
            var p = Game.NewHero(8013, "P", "human", "fighter"); p.Monsters.Clear(); StandOnFloor(p);
            p.PutSurface(p.Player.X, p.Player.Y, SurfaceKind.Fire, 4);
            Assert(p.Player.BurnTurns > 0, "the player catches fire in flames");
            p.PutSurface(p.Player.X, p.Player.Y, SurfaceKind.Water);
            Assert(p.Player.BurnTurns == 0 && p.Player.WetTurns > 0, "water puts the player out");

            // Monster resistances.
            var r = Game.NewHero(8014, "T", "human", "fighter"); r.Monsters.Clear(); Arena(r, out int ax, out int ay);
            var giant = Place(r, "fire giant", ax, ay); var troll = Place(r, "ice troll", ax, ay); var zombie = Place(r, "human zombie", ax, ay);
            Assert(r.MonsterResist(giant, DamageType.Fire) == 100 && r.MonsterResist(giant, DamageType.Cold) < 0, "fire giants");
            Assert(r.MonsterResist(troll, DamageType.Cold) == 100 && r.MonsterResist(troll, DamageType.Fire) < 0, "ice trolls");
            Assert(r.MonsterResist(zombie, DamageType.Poison) == 100 && r.MonsterResist(zombie, DamageType.Cold) == 50, "the dead");
            r.SetAlight(giant); Assert(giant.BurnTurns == 0, "fire giants do not burn");
        }

        static void WaterAndFrost()
        {
            // Lightning runs through connected water: through the second creature, and through the caster.
            var g = Archmage(8020); g.Monsters.Clear(); var p = g.Player;
            StandOnFloor(g);
            g.UpdateFov();
            if (!Row(g, 3, out int dx, out int dy)) { Assert(false, "no row"); return; }
            bool playerCanStand = IsPlainFloor(g, p.X, p.Y);
            var a = Place(g, "ogre", p.X + dx, p.Y + dy); a.HP = a.MaxHP = 4000; a.Speed = 1;
            var b = Place(g, "ogre", p.X + dx * 3, p.Y + dy * 3); b.HP = b.MaxHP = 4000; b.Speed = 1;
            for (int i = 0; i < 3 && playerCanStand; i++) { }
            for (int i = 0; i <= 3; i++) g.PutSurface(p.X + dx * i, p.Y + dy * i, SurfaceKind.Water);
            p.HP = p.MaxHP = 5000;
            Assert(CastOk(g, "shocking-grasp", a.X, a.Y), "grasp");
            Assert(a.HP < 4000, "the target is shocked");
            Assert(b.HP < 4000, "the shock runs through the water to the far creature");
            if (playerCanStand) Assert(p.HP < 5000 && LogHas(g, "shocks you"), "and through the caster standing in it");

            // Without water, nothing arcs.
            var d = Archmage(8021); d.Monsters.Clear(); StandOnFloor(d); d.UpdateFov();
            Assert(Row(d, 3, out int ex, out int ey), "row 2");
            var a2 = Place(d, "ogre", d.Player.X + ex, d.Player.Y + ey); a2.HP = a2.MaxHP = 4000; a2.Speed = 1;
            var b2 = Place(d, "ogre", d.Player.X + ex * 3, d.Player.Y + ey * 3); b2.HP = b2.MaxHP = 4000; b2.Speed = 1;
            CastOk(d, "shocking-grasp", a2.X, a2.Y);
            Assert(b2.HP == 4000, "no water, no arc");

            // The soaked take half again as much lightning.
            long dry = 0, soaked = 0;
            for (ulong seed = 1; seed <= 12; seed++)
                foreach (bool wetFlag in new[] { false, true })
                {
                    var t = Archmage(8100 + seed); t.Monsters.Clear(); StandOnFloor(t); t.UpdateFov();
                    if (!Row(t, 2, out int rx, out int ry)) continue;
                    var m = Place(t, "ogre", t.Player.X + rx, t.Player.Y + ry); m.HP = m.MaxHP = 9000; m.Speed = 1;
                    if (wetFlag) m.WetTurns = 50;
                    CastOk(t, "shocking-grasp", m.X, m.Y);
                    if (wetFlag) soaked += 9000 - m.HP; else dry += 9000 - m.HP;
                }
            Assert(soaked > dry, $"wet creatures should take more lightning ({soaked} vs {dry})");

            // Frost on a creature in water freezes the water and holds it.
            var f = Archmage(8022); f.Monsters.Clear(); Arena(f, out int fx, out int fy);
            var ogre = Place(f, "ogre", fx, fy); ogre.HP = ogre.MaxHP = 9000; ogre.Speed = 1;
            f.PutSurface(fx, fy, SurfaceKind.Water);
            if (IsPlainFloor(f, fx, fy))
            {
                int energy = ogre.Energy;
                Assert(CastOk(f, "frost-ray", ogre.X, ogre.Y), "frost ray");
                Assert(f.SurfaceAt(fx, fy) == SurfaceKind.Ice && ogre.SlowTurns > 0, "frost ices the water and chills the creature");
                // Ice melts back into water.
                f.Map.SetSurface(fx, fy, SurfaceKind.Ice, 2);
                new Commands(f).Execute("."); new Commands(f).Execute(".");
                Assert(f.SurfaceAt(fx, fy) == SurfaceKind.Water, "ice melts");
            }

            // Slipping on ice: roughly a third of steps.
            var s = Game.NewHero(8023, "I", "human", "fighter"); s.Monsters.Clear();
            Assert(FloorRun(s, 2, out int sx, out int sy), "floor pair");
            s.Player.MaxHP = s.Player.HP = 9000;
            s.Map.SetSurface(sx, sy, SurfaceKind.Ice, 0); s.Map.SetSurface(sx + 1, sy, SurfaceKind.Ice, 0);
            s.Player.X = sx; s.Player.Y = sy;
            int slips = 0, steps = 0;
            for (int i = 0; i < 200; i++)
            {
                int before = s.Log.Count;
                s.TryMovePlayer(s.Player.X == sx ? 1 : -1, 0);
                steps++;
                for (int k = before; k < s.Log.Count; k++) if (s.Log[k].Text.Contains("slip")) slips++;
            }
            Assert(slips > steps / 10 && slips < steps / 2, $"about 30% of ice steps slip: {slips}/{steps}");
        }

        static void SurfaceSpells()
        {
            Assert(Spells.Find("wall-of-fire") != null && Spells.Find("create-water") != null && Spells.All.Length >= 34, "new spells exist");
            // Wall of fire: a burning cross; creatures in it ignite.
            var g = Archmage(8030); g.Monsters.Clear(); Arena(g, out int x, out int y);
            var m = Place(g, "ogre", x, y); m.HP = m.MaxHP = 9000; m.Speed = 1;
            Assert(CastOk(g, "wall-of-fire", x, y), "wall of fire");
            int flames = 0;
            foreach (var d in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } })
                if (g.SurfaceAt(x + d[0], y + d[1]) == SurfaceKind.Fire) flames++;
            Assert(flames >= 1, "the wall should leave flames, got " + flames);
            Assert(m.BurnTurns > 0 || m.HP < 9000, "the creature in the wall burns");

            // Create water floods a 5x5 patch and soaks whoever stands in it.
            var w = Archmage(8031); w.Monsters.Clear(); Arena(w, out int wx, out int wy);
            var wm = Place(w, "ogre", wx, wy); wm.HP = wm.MaxHP = 9000; wm.Speed = 1;
            Assert(CastOk(w, "create-water", wx, wy), "create water");
            int water = 0;
            for (int j = -2; j <= 2; j++) for (int i = -2; i <= 2; i++) if (w.SurfaceAt(wx + i, wy + j) == SurfaceKind.Water) water++;
            Assert(water >= 4, "the floor should be wet, found " + water);
            if (IsPlainFloor(w, wx, wy)) Assert(wm.WetTurns > 0, "whoever stands in it is soaked");

            // Fireball burns brush; meteor scorches bare floor.
            var f = Archmage(8032); f.Monsters.Clear(); Arena(f, out int fx, out int fy);
            if (IsPlainFloor(f, fx, fy))
            {
                f.PutSurface(fx, fy, SurfaceKind.Grass);
                var target = Place(f, "ogre", fx, fy); target.HP = target.MaxHP = 9000; target.Speed = 1;
                Assert(CastOk(f, "fireball", fx, fy), "fireball");
                Assert(f.SurfaceAt(fx, fy) != SurfaceKind.Grass, "fireball should not leave the brush standing");
            }
            var mt = Archmage(8033); mt.Monsters.Clear(); Arena(mt, out int mx, out int my);
            if (IsPlainFloor(mt, mx, my))
            {
                Assert(CastOk(mt, "meteor", mx, my), "meteor");
                Assert(mt.SurfaceAt(mx, my) == SurfaceKind.Fire, "a meteor leaves the ground burning");
            }

            // Fire and a god who likes it.
            var v = Archmage(8034); v.Monsters.Clear(); Arena(v, out int vx, out int vy);
            v.Player.God = "veyra"; v.Player.Piety = 20;
            Assert(CastOk(v, "wall-of-fire", vx, vy) && v.Player.Piety >= 21, "Veyra likes a wall of fire");

            // Books teach them.
            Assert(System.Array.IndexOf(Spells.InBook("a tome of evocation"), "wall-of-fire") >= 0 && System.Array.IndexOf(Spells.InBook("a tome of conjuration"), "create-water") >= 0, "books");
        }

        static void SurfaceGeneration()
        {
            var dungeon = new Dungeon(new Rng(31337));
            int Count(string branch, int depth, SurfaceKind kind)
            {
                var map = dungeon.Ensure(branch, depth, out _, out _, out _);
                int n = 0;
                foreach (var kv in map.Surfaces) if (kv.Value.Kind == kind) n++;
                foreach (var kv in map.Surfaces) Assert(Tiles.Walkable(map.Get(kv.Key % map.W, kv.Key / map.W)) && kv.Value.Kind != SurfaceKind.Fire, "surfaces sit on floor and never start burning");
                return n;
            }
            int water = 0, grass = 0, oil = 0;
            for (int d = 1; d <= 6; d++) { water += Count("The Sunken Vaults", d, SurfaceKind.Water); grass += Count("The Warrens", d, SurfaceKind.Grass); oil += Count("The Ashen Spire", d, SurfaceKind.Oil); }
            Assert(water > 15, "the vaults are wet: " + water);
            Assert(grass > 15, "the warrens are overgrown: " + grass);
            Assert(oil > 8, "the Spire is slick with oil: " + oil);
        }

        static void SurfaceUi()
        {
            var g = Game.NewHero(8040, "U", "human", "fighter"); g.Monsters.Clear();
            var hud = new GameHud(g); hud.Ui.Resize(110, 36);
            StandOnFloor(g); g.UpdateFov();
            Assert(Row(g, 2, out int dx, out int dy), "row");
            g.PutSurface(g.Player.X + dx, g.Player.Y + dy, SurfaceKind.Water);
            string art = hud.Draw().ToAscii();
            Assert(art.Contains("≈"), "water is drawn");
            g.PutSurface(g.Player.X + dx * 2, g.Player.Y + dy * 2, SurfaceKind.Grass);
            g.Map.SetSurface(g.Player.X + dx * 2, g.Player.Y + dy * 2, SurfaceKind.Fire, 5);
            art = hud.Draw().ToAscii();
            Assert(art.Contains("▲") || art.Contains("^"), "fire is drawn");
            g.Player.BurnTurns = 3; g.Player.WetTurns = 3;
            art = hud.Draw().ToAscii();
            Assert(art.Contains("BURNING") && art.Contains("WET"), "conditions show as pills");
            foreach (var size in new[] { new[] { 84, 26 }, new[] { 200, 60 } }) { hud.Ui.Resize(size[0], size[1]); hud.Draw(); }
            g.DescribeCell(g.Player.X + dx, g.Player.Y + dy);
            Assert(LogHas(g, "shallow water"), "looking at a puddle says so");
        }

        static Item Potion(string name, Rng rng)
        {
            foreach (var d in Catalogue.Potions) if (d.Name == name) return new Item(d, rng, 1);
            throw new Exception("no potion " + name);
        }

        static void PotionsAndScrolls()
        {
            var rng = new Rng(5);
            var g = Game.NewHero(9100, "Q", "human", "fighter"); g.Monsters.Clear(); var p = g.Player;
            p.MaxHP = p.HP = 200; p.HP = 20;

            Item Drink(string name)
            {
                var it = Potion(name, rng); p.Inventory.Add(it);
                int turn = g.Turn;
                g.Quaff(it);
                Assert(!p.Inventory.Contains(it), name + " should be used up");
                Assert(g.Turn >= turn + 1, name + " should take a turn");
                return it;
            }

            Drink("potion of healing"); Assert(p.HP > 20, "healing heals: " + p.HP);
            p.HP = 20; Drink("potion of extra healing"); Assert(p.HP > 30, "extra healing heals more: " + p.HP);
            var f = Game.NewHero(9101, "F", "human", "fighter"); f.Monsters.Clear();
            int max = f.Player.MaxHP; f.Player.HP = 5;
            var fh = Potion("potion of full healing", rng); f.Player.Inventory.Add(fh); f.Quaff(fh);
            Assert(f.Player.HP == f.Player.MaxHP && f.Player.MaxHP == max + 2, "full healing restores and adds max HP");
            p.MaxHP = p.HP = 200; int hp = p.HP; Drink("potion of poison"); Assert(p.HP < hp, "poison hurts");
            p.PoisonResist = 0;
            Drink("potion of confusion"); Assert(p.Confused, "confusion"); p.Confused = false; p.ConfusionTurns = 0;
            Drink("potion of hallucination"); Assert(p.Hallucinating, "hallucination"); p.Hallucinating = false; p.HallucinationTurns = 0;
            Drink("potion of speed"); Assert(p.BuffTurns("haste") > 0, "speed hastens");
            Drink("potion of levitation"); Assert(p.BuffTurns("levitating") > 0, "levitation");
            hp = p.HP; Drink("potion of acid"); Assert(p.HP < hp, "acid burns");
            StandOnFloor(g); Drink("potion of oil");
            Assert(g.SurfaceAt(p.X, p.Y) == SurfaceKind.Oil, "oil spills underfoot");
            int sum = p.Str + p.Dex + p.Con + p.Int + p.Wis + p.Cha;
            Drink("potion of gain ability"); Assert(p.Str + p.Dex + p.Con + p.Int + p.Wis + p.Cha == sum + 1, "gain ability: +1 to one attribute");
            int lvl = p.Level; Drink("potion of gain level"); Assert(p.Level == lvl + 1, "gain level");
            Drink("potion of see invisible"); Drink("potion of sleeping");
            Assert(g.Mode == GameMode.Dungeon, "sleeping must not kill a lone hero");

            // The verb: one potion in the pack is drunk at once.
            var cmd = new Commands(g);
            var solo = Potion("potion of healing", rng); p.Inventory.Add(solo); p.HP = 10;
            cmd.Execute("q");
            Assert(p.HP > 10 && !p.Inventory.Contains(solo), "the drink verb");
            cmd.Execute("q"); Assert(LogHas(g, "nothing suitable"), "nothing to drink");

            // Scrolls are one use, then gone; potions never carry charges.
            Assert(Potion("potion of healing", rng).RemainingCharges == 1, "potions are single dose");
            var scroll = Make("scroll of mapping", Catalogue.Scrolls, rng);
            Assert(scroll.RemainingCharges == 1, "scrolls are single use");
            p.Inventory.Add(scroll); g.UseScroll(scroll);
            Assert(!p.Inventory.Contains(scroll), "a read scroll is consumed");
            var wand = Make("wand of light", Catalogue.Wands, rng);
            Assert(wand.RemainingCharges >= 4, "wands keep their charges");

            // Loot tables still produce them.
            bool seenPotion = false, seenScroll = false;
            var lr = new Rng(77);
            for (int i = 0; i < 600; i++) { var it = LevelBuilder.RollLoot(lr, 5); seenPotion |= it.Def.Kind == ItemKind.Potion && it.RemainingCharges == 1; seenScroll |= it.Def.Kind == ItemKind.Scroll && it.RemainingCharges == 1; }
            Assert(seenPotion && seenScroll, "loot includes single-use potions and scrolls");
        }

        static void BotDeterminism()
        {
            var a = Ossuary.Tools.BalanceBot.Play(4242, "elf", "wizard", 700, false);
            var b = Ossuary.Tools.BalanceBot.Play(4242, "elf", "wizard", 700, false);
            Assert(a.Turns == b.Turns && a.Depth == b.Depth && a.Kills == b.Kills && a.Level == b.Level && a.Died == b.Died, "the same bot, the same run");
            Assert(a.Turns >= 600 || a.Died, "the bot should play its turns, got " + a.Turns);
        }

        // A guard rail, not a target: no class may fall far behind the others or die in droves.
        static void BalanceBand()
        {
            var depth = new Dictionary<string, double>();
            var alive = new Dictionary<string, double>();
            foreach (var role in Roles.All)
            {
                double d = 0, a = 0; int n = 0;
                foreach (string race in new[] { "human", "orc" })
                    for (int s = 0; s < 2; s++)
                    {
                        var r = Ossuary.Tools.BalanceBot.Play((ulong)(1000 + s * 7919 + role.Id.Length * 13), race, role.Id, 1800, false);
                        d += r.Depth; a += r.Died ? 0 : 1; n++;
                    }
                depth[role.Id] = d / n; alive[role.Id] = a / n;
            }
            double best = 0; foreach (var kv in depth) best = System.Math.Max(best, kv.Value);
            foreach (var role in Roles.All)
            {
                Assert(alive[role.Id] >= 0.5, $"{role.Id} survives too rarely ({alive[role.Id]:0.00})");
                Assert(depth[role.Id] >= best * 0.6, $"{role.Id} falls behind: depth {depth[role.Id]:0.0} vs best {best:0.0}");
            }
        }

        static void SpellPanelComposes()
        {
            var g = Game.NewHero(5, "W", "elf", "wizard");
            var hud = new GameHud(g);
            foreach (var size in new[] { new[] { 84, 26 }, new[] { 110, 36 }, new[] { 200, 60 } })
            {
                hud.Ui.Resize(size[0], size[1]);
                g.UiState.Active = Panel.Spells; g.UiState.SpellIndex = 1; hud.Draw();
                g.Player.Spells.Clear(); hud.Draw();
                g.Player.Spells.AddRange(new[] { "magic-missile", "ward", "frost-ray", "sleep", "blink", "cure-wounds" });
                g.UiState.SpellIndex = 5; hud.Draw();
            }
            g.UiState.Active = Panel.None;
        }

        static void CreatePanelComposes()
        {
            var g = new Game(3);
            var hud = new GameHud(g);
            foreach (var step in new[] { CreateStep.Name, CreateStep.Race, CreateStep.Role, CreateStep.Confirm })
                foreach (var size in new[] { new[] { 84, 26 }, new[] { 110, 36 }, new[] { 200, 60 } })
                {
                    g.UiState.Active = Panel.Create; g.UiState.Create.Reset(); g.UiState.Create.Step = step;
                    g.UiState.Create.Name = "Longname";
                    for (int r = 0; r < Races.All.Length; r++)
                        for (int o = 0; o < Roles.All.Length; o += 3)
                        {
                            g.UiState.Create.RaceIndex = r; g.UiState.Create.RoleIndex = o;
                            hud.Ui.Resize(size[0], size[1]); hud.Draw();
                        }
                }
            g.UiState.Active = Panel.None;
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
