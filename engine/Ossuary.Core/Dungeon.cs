using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>A branch of the dungeon: a stack of levels with its own theme and level styles.</summary>
    public sealed class Branch
    {
        public string Name;
        public string Adjective;
        public int MaxDepth = 12;
        public LevelStyle[] Styles;
        public int[] WallStyles;      // per depth band
        public string EntryText;
        public bool AscendPossible = true;
        /// <summary>For an optional side branch: the branch and depth whose portal leads into it (null for the main five).</summary>
        public string Parent;
        public int ParentDepth;

        public Branch(string name, string adj, int maxDepth, LevelStyle[] styles, int[] walls, string entry)
        {
            Name = name; Adjective = adj; MaxDepth = maxDepth; Styles = styles; WallStyles = walls; EntryText = entry;
        }

        public LevelStyle StyleFor(int depth)
        {
            if (Styles == null || Styles.Length == 0) return LevelStyle.Rooms;
            return Styles[Math.Min(Styles.Length - 1, Math.Max(0, depth - 1))];
        }

        public int WallStyleFor(int depth)
        {
            if (WallStyles == null || WallStyles.Length == 0) return 0;
            return WallStyles[Math.Min(WallStyles.Length - 1, Math.Max(0, depth - 1))];
        }
    }

    /// <summary>
    /// Holds every level the player has visited, keyed by (branch, depth), plus the
    /// current one. Levels are generated lazily on first descent and then persisted,
    /// which is what makes backtracking meaningful.
    /// </summary>
    public sealed class Dungeon
    {
        public readonly List<Branch> Branches = new List<Branch>();
        readonly Dictionary<string, GameMap> _levels = new Dictionary<string, GameMap>();
        readonly Dictionary<string, int[]> _levelEntrances = new Dictionary<string, int[]>();
        public Rng Rng;
        public int Width = 79;
        public int Height = 25;

        public Dungeon(Rng rng)
        {
            Rng = rng;
            BuildBranches();
        }

        void BuildBranches()
        {
            Branches.Add(new Branch("The Dungeons", "dungeon", 10,
                new[] { LevelStyle.Rooms, LevelStyle.Ruins, LevelStyle.Barracks, LevelStyle.Maze, LevelStyle.Catacombs, LevelStyle.Cave, LevelStyle.Ruins, LevelStyle.Maze, LevelStyle.Fort, LevelStyle.Cave },
                new[] { 0, 0, 0, 1, 0, 2, 0, 1, 1, 2 },
                "You enter the dungeons beneath the earth."));

            Branches.Add(new Branch("The Mines of Dwarfdeep", "mining", 8,
                new[] { LevelStyle.Warrens, LevelStyle.Cave, LevelStyle.Warrens, LevelStyle.Cave, LevelStyle.Maze, LevelStyle.Cave, LevelStyle.Fort, LevelStyle.Cave },
                new[] { 2, 2, 2, 2, 2, 2, 1, 2 },
                "The air grows cold and the walls turn to living stone."));

            Branches.Add(new Branch("The Warrens", "warren", 9,
                new[] { LevelStyle.Warrens, LevelStyle.Warrens, LevelStyle.Cave, LevelStyle.Warrens, LevelStyle.Warrens, LevelStyle.Cave, LevelStyle.Warrens, LevelStyle.Fort, LevelStyle.Cave },
                new[] { 2, 2, 2, 2, 2, 2, 2, 1, 2 },
                "Something has been living here a long time."));

            Branches.Add(new Branch("The Sunken Vaults", "sunken", 12,
                new[] { LevelStyle.Cave, LevelStyle.Catacombs, LevelStyle.Fort, LevelStyle.Cave, LevelStyle.Ruins, LevelStyle.Maze, LevelStyle.Catacombs, LevelStyle.Cave, LevelStyle.Warrens, LevelStyle.Cave, LevelStyle.Fort, LevelStyle.Cave },
                new[] { 2, 1, 1, 2, 1, 1, 1, 2, 2, 2, 1, 2 },
                "Black water drips from a ceiling you cannot see."));

            Branches.Add(new Branch("The Ashen Spire", "ashen", 15,
                new[] { LevelStyle.Fort, LevelStyle.Maze, LevelStyle.Fort, LevelStyle.Barracks, LevelStyle.Fort, LevelStyle.Ruins, LevelStyle.Fort, LevelStyle.Barracks, LevelStyle.Fort, LevelStyle.Maze, LevelStyle.Fort, LevelStyle.Catacombs, LevelStyle.Fort, LevelStyle.Cave, LevelStyle.Fort },
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1 },
                "The stone here is warm, and it should not be."));

            // An optional side branch, reached by a portal on Dungeons 4: short, brutal, and worth it.
            Branches.Add(new Branch("The Annex", "annex", 3,
                new[] { LevelStyle.Catacombs, LevelStyle.Maze, LevelStyle.Fort },
                new[] { 1, 0, 1 },
                "A door that should not have opened, and has, and has closed behind you.")
            { Parent = "The Dungeons", ParentDepth = 4, AscendPossible = false });

            // A second side branch, behind a portal deep in the Mines: the court the dwarves walled up and the dead kept.
            Branches.Add(new Branch("The Hollow Court", "court", 3,
                new[] { LevelStyle.Fort, LevelStyle.Catacombs, LevelStyle.Fort },
                new[] { 1, 1, 1 },
                "Velvet gone to rot, a long hall of empty thrones, and someone humming a dance.")
            { Parent = "The Mines of Dwarfdeep", ParentDepth = 5, AscendPossible = false });
        }

        /// <summary>The side branch whose portal stands on this level, or null.</summary>
        public Branch SideBranchAt(string branch, int depth)
        {
            foreach (var b in Branches) if (b.Parent == branch && b.ParentDepth == depth) return b;
            return null;
        }

        public Branch Get(string name)
        {
            for (int i = 0; i < Branches.Count; i++) if (Branches[i].Name == name) return Branches[i];
            return Branches[0];
        }

        static string Key(string branch, int depth) => branch + "/" + depth;

        public bool Has(string branch, int depth) => _levels.ContainsKey(Key(branch, depth));

        public GameMap Get(string branch, int depth)
        {
            string k = Key(branch, depth);
            if (_levels.TryGetValue(k, out var m)) return m;
            return null;
        }

        public GameMap Ensure(string branchName, int depth, out List<LevelBuilder.SpawnPoint> spawns, out int sx, out int sy)
        {
            string k = Key(branchName, depth);
            if (_levels.TryGetValue(k, out var existing))
            {
                spawns = null; sx = 0; sy = 0;
                if (_levelEntrances.TryGetValue(k, out var e)) { sx = e[0]; sy = e[1]; }
                return existing;
            }

            var b = Get(branchName);
            int mapW = Width;
            int mapH = Height;
            var levelRng = Rng.Fork();
            // A per-level seed makes levels reproducible from (world seed, branch, depth).
            var opts = new GenOptions
            {
                Width = mapW,
                Height = mapH,
                Style = b.StyleFor(depth),
                WallStyle = b.WallStyleFor(depth),
                MaxRooms = Math.Min(18, 6 + depth / 2),
                AllowStairsUp = depth > 1 || b.AscendPossible,
                AllowStairsDown = depth < b.MaxDepth,
                Seed = levelRng.Range(1, int.MaxValue),
            };

            var map = DungeonGen.Generate(opts, levelRng, out var specials, out var starts);
            spawns = LevelBuilder.Populate(map, levelRng, depth, b.Name, specials, starts, out sx, out sy);

            // The quest goal waits on the deepest floor of the main branch.
            if (branchName == Game.QuestBranch && depth == b.MaxDepth)
                Game.PlaceQuestAmulet(map, levelRng, GroundItems.NextUid());

            // A side branch is entered by a portal, and left by the same one: you arrive standing on it,
            // so its depth 1 has no stairs up. Every other branch keeps them: they lead to daylight.
            if (depth == 1 && b.Parent != null)
            {
                for (int y = 0; y < map.H; y++)
                    for (int x = 0; x < map.W; x++)
                        if (map.Get(x, y) == TileKind.StairsUp) map.Set(x, y, TileKind.Floor);
                map.Set(sx, sy, TileKind.Portal);
            }

            _levels[k] = map;
            _levelEntrances[k] = new[] { sx, sy };
            return map;
        }

        public void Remember(string branch, int depth, int x, int y)
        {
            string k = Key(branch, depth);
            if (_levelEntrances.ContainsKey(k)) _levelEntrances[k] = new[] { x, y };
            else _levelEntrances[k] = new[] { x, y };
        }

        public int LevelCount => _levels.Count;

        public IEnumerable<string> VisitedKeys => _levels.Keys;
    }
}
