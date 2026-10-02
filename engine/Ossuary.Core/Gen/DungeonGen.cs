using System;
using System.Collections.Generic;

namespace Ossuary.Core.Gen
{
    public enum LevelStyle
    {
        Rooms,      // Rogue / early NetHack: disjoint rooms, thin corridors
        Maze,       // corridor labyrinth with wide halls
        Cave,       // cellular-automata caverns
        Barracks,   // Dwarf Fortress-ish: halls in a grid, doors everywhere
        Fort,       // keep/stronghold: concentric walls, vault in the middle
        Warrens,    // Caves of Qud-ish: organic cells, pillars, ruined walls
        Ruins,      // rooms that fell down: breached walls, rubble, debris
        Catacombs,  // a lattice of crypts off a grid of narrow passages
    }

    public enum SpecialRoom
    {
        None = 0,
        Vault,
        Barracks,
        Temple,
        Shrine,
        Garden,
        Mine,
        BarracksOrc,
        Forge,
        Crypt,
        Keep,
        Fort,
        Fountain,
        Treasure,
        Portal,
        Ziggurat,
        Slaughterhouse,
        GardenFountain,
        Dormitory,
        Hall,
    }

    public struct Room
    {
        public int X, Y, W, H;
        public bool IsSpecial;
        public SpecialRoom Kind;
        public int CenterX => X + W / 2;
        public int CenterY => Y + H / 2;
        public int Area => W * H;
        public bool Overlaps(Room o) => X < o.X + o.W && X + W > o.X && Y < o.Y + o.H && Y + H > o.Y;
    }

    public struct GenOptions
    {
        public int Width, Height;
        public LevelStyle Style;
        public int MaxRooms;
        public int WallStyle;   // which wall tile to use
        public bool Darkness;   // most levels start unlit
        public bool AllowStairsUp;
        public bool AllowStairsDown;
        public int StartX, StartY;
        public int Seed;
    }

    /// <summary>
    /// Level generation. Every style is a plug-in over the same post-process
    /// (connectivity repair, doors, stairs, validation) so a generated level is
    /// always playable no matter which generator produced the raw layout.
    /// </summary>
    public static class DungeonGen
    {
        public static GameMap Generate(GenOptions o, Rng rng, out List<SpecialRoom> specials, out List<int> startCells)
        {
            specials = new List<SpecialRoom>();
            startCells = new List<int>();
            var map = new GameMap(o.Width, o.Height);
            map.Number = rng.Range(1, int.MaxValue / 2);

            switch (o.Style)
            {
                case LevelStyle.Maze: GenMaze(map, o, rng); break;
                case LevelStyle.Cave: GenCave(map, o, rng); break;
                case LevelStyle.Barracks: GenBarracks(map, o, rng); break;
                case LevelStyle.Fort: GenFort(map, o, rng); break;
                case LevelStyle.Warrens: GenWarrens(map, o, rng); break;
                case LevelStyle.Ruins: GenRuins(map, o, rng); break;
                case LevelStyle.Catacombs: GenCatacombs(map, o, rng); break;
                default: GenRooms(map, o, rng); break;
            }

            // Doors go in before the repair pass, because connectivity has to treat a closed
            // door as something the player can open rather than as a dead end.
            AddDoors(map, rng);
            RepairConnectivity(map, rng);
            Validate(map, o, rng, specials, startCells);
            return map;
        }

        static TileKind WallTile(GenOptions o) => o.WallStyle == 1 ? TileKind.WallAlt : TileKind.Wall;

        // ---------------------------------------------------------------- rooms

        static void GenRooms(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.Wall);
            int attempts = o.MaxRooms * 3;
            var rooms = new List<Room>();
            for (int i = 0; i < attempts; i++)
            {
                int w = rng.Range(4, 11);
                int h = rng.Range(3, 7);
                int x = rng.Range(2, map.W - w - 3);
                int y = rng.Range(2, map.H - h - 3);
                var r = new Room { X = x, Y = y, W = w, H = h };
                bool ok = true;
                for (int j = 0; j < rooms.Count; j++)
                {
                    if (rooms[j].Overlaps(new Room { X = x - 1, Y = y - 1, W = w + 2, H = h + 2 })) { ok = false; break; }
                }
                if (!ok) continue;
                rooms.Add(r);
                map.Stamp(x, y, w, h, TileKind.Floor, true);
                if (rooms.Count >= o.MaxRooms) break;
            }

            for (int i = 1; i < rooms.Count; i++)
            {
                var a = rooms[i - 1];
                var b = rooms[i];
                if (rng.Chance(50)) CarveCorridor(map, a.CenterX, a.CenterY, b.CenterX, b.CenterY, rng);
                else CarveCorridor(map, a.CenterX, a.CenterY, b.X, b.Y, rng);
                CarveCorridor(map, a.CenterX, a.CenterY, b.CenterX + b.W - 1, b.Y + b.H - 1, rng);
            }

            DecorateRooms(map, rooms, rng);
        }

        static void DecorateRooms(GameMap map, List<Room> rooms, Rng rng)
        {
            if (rooms.Count == 0) return;
            // Reserve the two largest rooms for specials.
            rooms.Sort((a, b) => b.Area.CompareTo(a.Area));
            int specials = Math.Min(3, rooms.Count);
            var specialsSeen = new List<SpecialRoom>();
            for (int i = 0; i < specials; i++)
            {
                var kind = PickSpecial(rng, specialsSeen);
                specialsSeen.Add(kind);
                rooms[i] = DecorateSpecial(map, rooms[i], kind, rng);
            }
            foreach (var r in rooms)
            {
                if (r.IsSpecial) continue;
                if (rng.Chance(18)) map.Set(r.CenterX, r.CenterY, TileKind.Fountain);
                else if (rng.Chance(22)) map.Set(r.X + rng.Range(0, r.W - 1), r.Y + rng.Range(0, r.H - 1), TileKind.Altar);
                if (rng.Chance(12))
                {
                    // Pillars in bigger rooms.
                    for (int px = r.X + 1; px < r.X + r.W - 1; px += 2)
                    {
                        for (int py = r.Y + 1; py < r.Y + r.H - 1; py += 2)
                            map.Set(px, py, TileKind.Rubble);
                    }
                }
            }
        }

        static SpecialRoom PickSpecial(Rng rng, List<SpecialRoom> seen)
        {
            SpecialRoom[] pool =
            {
                SpecialRoom.Vault, SpecialRoom.Barracks, SpecialRoom.Temple, SpecialRoom.Shrine,
                SpecialRoom.Garden, SpecialRoom.Mine, SpecialRoom.Forge, SpecialRoom.Crypt,
                SpecialRoom.Keep, SpecialRoom.Treasure, SpecialRoom.Ziggurat, SpecialRoom.Dormitory,
            };
            for (int i = 0; i < 20; i++)
            {
                var c = rng.Pick(pool);
                if (!seen.Contains(c)) return c;
            }
            return SpecialRoom.Vault;
        }

        /// <summary>
        /// Marks specials on the map so the level builder knows what it is looking at.
        /// Returns the updated room: a List indexer cannot be passed by ref, so the
        /// caller writes the result back into the list.
        /// </summary>
        public static Room DecorateSpecial(GameMap map, Room r, SpecialRoom kind, Rng rng)
        {
            r.IsSpecial = true;
            r.Kind = kind;
            switch (kind)
            {
                case SpecialRoom.Vault:
                case SpecialRoom.Treasure:
                    // Thin walls, a locked door, and open floor: reads as "treasure, guarded".
                    for (int x = r.X; x < r.X + r.W; x++)
                        for (int y = r.Y; y < r.Y + r.H; y++)
                            if (map.Walkable(x, y) && (x == r.X || x == r.X + r.W - 1 || y == r.Y || y == r.Y + r.H - 1))
                                map.Set(x, y, TileKind.WallAlt);
                    break;
                case SpecialRoom.Fort:
                case SpecialRoom.Keep:
                    for (int x = r.X; x < r.X + r.W; x++)
                        for (int y = r.Y; y < r.Y + r.H; y++)
                            if (map.Walkable(x, y) && (x == r.X || x == r.X + r.W - 1 || y == r.Y || y == r.Y + r.H - 1))
                                map.Set(x, y, TileKind.WallAlt);
                    break;
                case SpecialRoom.Garden:
                    for (int i = 0; i < r.Area / 3; i++)
                    {
                        int x = rng.Range(r.X, r.X + r.W), y = rng.Range(r.Y, r.Y + r.H);
                        if (map.Walkable(x, y)) map.Set(x, y, TileKind.FloorAlt);
                    }
                    break;
                case SpecialRoom.Fountain:
                    map.Set(r.CenterX, r.CenterY, TileKind.Fountain);
                    break;
                case SpecialRoom.Temple:
                case SpecialRoom.Shrine:
                    map.Set(r.CenterX, r.CenterY, TileKind.Altar);
                    break;
            }
            return r;
        }

        // ---------------------------------------------------------------- ruins

        /// <summary>Ordinary rooms, then time: walls give way to breaches and rubble, floors fill with debris.</summary>
        static void GenRuins(GameMap map, GenOptions o, Rng rng)
        {
            GenRooms(map, o, rng);
            var cells = new List<(int, int)>();
            for (int y = 1; y < map.H - 1; y++)
                for (int x = 1; x < map.W - 1; x++)
                {
                    TileKind t = map.Get(x, y);
                    if (t == TileKind.Wall || t == TileKind.WallAlt)
                    {
                        bool nextToFloor = false;
                        for (int k = 0; k < 4; k++) if (map.Walkable(x + Dirs.Dx4[k], y + Dirs.Dy4[k])) nextToFloor = true;
                        if (nextToFloor) cells.Add((x, y));
                    }
                    else if (t == TileKind.Floor && rng.Chance(14)) map.SetRaw(x + y * map.W, TileKind.FloorAlt);
                }
            foreach (var (x, y) in cells)
            {
                int roll = rng.Range(0, 100);
                if (roll < 7) map.SetRaw(x + y * map.W, TileKind.Floor);          // a breach
                else if (roll < 16) map.SetRaw(x + y * map.W, TileKind.Rubble);   // something fell
                else if (roll < 21) map.SetRaw(x + y * map.W, TileKind.Pillar);   // what is left of a column
            }
        }

        // ------------------------------------------------------------ catacombs

        /// <summary>Narrow passages on a grid, with a crypt in every block, most of them with graves and a door onto the nearest passage.</summary>
        static void GenCatacombs(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.WallDark);
            const int strideX = 8, strideY = 6, offX = 2, offY = 2;
            int cols = (map.W - 1 - offX) / strideX, rows = (map.H - 1 - offY) / strideY;
            // The passages: one-wide lines along every block edge.
            for (int gx = 0; gx <= cols; gx++)
                for (int y = offY; y <= offY + rows * strideY && y < map.H - 1; y++) map.SetRaw(offX + gx * strideX + y * map.W, TileKind.Floor);
            for (int gy = 0; gy <= rows; gy++)
                for (int x = offX; x <= offX + cols * strideX && x < map.W - 1; x++) map.SetRaw(x + (offY + gy * strideY) * map.W, TileKind.Floor);
            // The crypts.
            for (int gy = 0; gy < rows; gy++)
                for (int gx = 0; gx < cols; gx++)
                {
                    if (rng.Chance(12)) continue;                       // solid rock: a block nobody dug
                    int cx = offX + gx * strideX, cy = offY + gy * strideY;
                    map.Stamp(cx + 2, cy + 2, 5, 3, TileKind.Floor, true);
                    // A door onto one of the four passages around the block.
                    int side = rng.Range(0, 4);
                    int dx = side == 0 ? cx + 1 : side == 1 ? cx + 7 : cx + 4, dy = side == 2 ? cy + 1 : side == 3 ? cy + 5 : cy + 3;
                    map.SetRaw(dx + dy * map.W, TileKind.Floor);
                    if (rng.Chance(50)) map.Set(cx + 4, cy + 3, TileKind.Grave);
                    else if (rng.Chance(40)) map.Set(cx + 3, cy + 3, TileKind.Grave);
                    if (rng.Chance(30)) map.Set(cx + 5, cy + 3, TileKind.Grave);
                }
        }

        // ----------------------------------------------------------------- maze

        static void GenMaze(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.Wall);
            // Classic odd-coordinate maze: cell (cx,cy) is centred on (2cx+1, 2cy+1), so a
            // passage between two cells is exactly one tile long. Using the cell's
            // own centre (rather than an offset) keeps every index in bounds.
            int cellW = 2;
            int cx = (map.W - 1) / cellW;
            int cy = (map.H - 1) / cellW;
            if (cx < 2 || cy < 2) return;
            var carve = new bool[cx * cy];
            var stack = new Stack<int>();
            int start = rng.Range(0, cx * cy);
            stack.Push(start);
            carve[start] = true;
            while (stack.Count > 0)
            {
                int cur = stack.Peek();
                int x = cur % cx, y = cur / cx;
                var options = new List<int>();
                if (x > 0 && !carve[cur - 1]) options.Add(0);
                if (x < cx - 1 && !carve[cur + 1]) options.Add(1);
                if (y > 0 && !carve[cur - cx]) options.Add(2);
                if (y < cy - 1 && !carve[cur + cx]) options.Add(3);
                if (options.Count == 0) { stack.Pop(); continue; }
                int dir = rng.Pick(options);
                int nx = x, ny = y;
                switch (dir)
                {
                    case 0: nx--; break;
                    case 1: nx++; break;
                    case 2: ny--; break;
                    default: ny++; break;
                }

                int mx = x * cellW + 1, my = y * cellW + 1;   // this cell's centre
                int tx = nx * cellW + 1, ty = ny * cellW + 1; // neighbour's centre
                map.Set(mx, my, TileKind.Floor);
                map.Set(tx, ty, TileKind.Floor);
                // Carve the shared wall tile between them.
                map.Set((mx + tx) / 2, (my + ty) / 2, TileKind.Floor);

                int ni = nx + ny * cx;
                carve[ni] = true;
                stack.Push(ni);
            }

            // Knock a few walls out into chambers so it isn't pure corridor.
            int rooms = Math.Max(3, o.MaxRooms);
            for (int i = 0; i < rooms; i++)
            {
                int w = rng.Range(3, 8), h = rng.Range(3, 6);
                int x = rng.Range(1, map.W - w - 2), y = rng.Range(1, map.H - h - 2);
                for (int yy = y; yy < y + h; yy++)
                    for (int xx = x; xx < x + w; xx++)
                        if (map.Get(xx, yy) == TileKind.Wall && rng.Chance(75)) map.SetRaw(xx + yy * map.W, TileKind.Floor);
            }
        }

        // ----------------------------------------------------------------- cave

        static void GenCave(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.WallDark);
            for (int i = 0; i < map.W * map.H; i++) map.SetRaw(i, rng.Chance(46) ? TileKind.Floor : TileKind.WallDark);

            int passes = 5;
            for (int p = 0; p < passes; p++)
            {
                var next = new TileKind[map.W * map.H];
                for (int y = 1; y < map.H - 1; y++)
                {
                    for (int x = 1; x < map.W - 1; x++)
                    {
                        int walls = 0;
                        for (int ky = -1; ky <= 1; ky++)
                            for (int kx = -1; kx <= 1; kx++)
                                if (map.Get(x + kx, y + ky) == TileKind.WallDark) walls++;
                        next[x + y * map.W] = walls >= 5 ? TileKind.WallDark : TileKind.Floor;
                    }
                }
                Array.Copy(next, 0, map.GetRawBuffer(), 0, map.W * map.H);
            }

            // Only the largest connected cave counts as the "level".
            var open = new List<int>();
            for (int i = 0; i < map.W * map.H; i++) if (map.GetRaw(i) == TileKind.Floor) open.Add(i);
            if (open.Count == 0) { map.SetRaw(1 + map.W, TileKind.Floor); return; }

            // Flood from the largest region; seal the rest.
            var best = new List<int>();
            var visited = new bool[map.W * map.H];
            foreach (int seed in open)
            {
                if (visited[seed]) continue;
                var region = new List<int>();
                var q = new Queue<int>();
                q.Enqueue(seed); visited[seed] = true;
                while (q.Count > 0)
                {
                    int c = q.Dequeue();
                    region.Add(c);
                    int x = c % map.W, y = c / map.W;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + Dirs.Dx4[k], ny = y + Dirs.Dy4[k];
                        if (!map.InBounds(nx, ny) || map.Get(nx, ny) != TileKind.Floor) continue;
                        int ni = nx + ny * map.W;
                        if (visited[ni]) continue;
                        visited[ni] = true;
                        q.Enqueue(ni);
                    }
                }
                if (region.Count > best.Count) best = region;
            }
            for (int i = 0; i < map.W * map.H; i++)
                if (map.GetRaw(i) == TileKind.Floor && !visited[i]) map.SetRaw(i, TileKind.WallDark);

            // Smooth the walls a little so caves do not look like TV static.
            for (int i = 0; i < 2; i++)
            {
                var next = new TileKind[map.W * map.H];
                for (int y = 0; y < map.H; y++)
                {
                    for (int x = 0; x < map.W; x++)
                    {
                        int walls = 0;
                        for (int ky = -1; ky <= 1; ky++)
                            for (int kx = -1; kx <= 1; kx++)
                                if (map.Get(x + kx, y + ky) == TileKind.WallDark) walls++;
                        next[x + y * map.W] = walls >= 6 ? TileKind.WallDark : TileKind.Floor;
                    }
                }
                Array.Copy(next, 0, map.GetRawBuffer(), 0, map.W * map.H);
            }
        }

        // ------------------------------------------------------------- barracks

        static void GenBarracks(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.Wall);
            int gw = rng.Range(3, 6);
            int gh = rng.Range(3, 6);
            int cw = map.W / gw, ch = map.H / gh;

            // Index rooms by their grid position. A flat list was wrong here: cells too
            // small to hold a room were skipped, so list index stopped matching the grid
            // index and corridors were carved between unrelated rooms.
            var cells = new Room[gw * gh];
            var placed = new bool[gw * gh];
            for (int gy = 0; gy < gh; gy++)
            {
                for (int gx = 0; gx < gw; gx++)
                {
                    int x = gx * cw + 1, y = gy * ch + 1;
                    int w = cw - 2, h = ch - 2;
                    if (w < 3 || h < 3) continue;
                    map.Stamp(x, y, w, h, TileKind.Floor, true);
                    var r = new Room { X = x, Y = y, W = w, H = h };
                    cells[gy * gw + gx] = r;
                    placed[gy * gw + gx] = true;
                    if (rng.Chance(30) && w > 5 && h > 5) DecorateSpecial(map, r, SpecialRoom.Barracks, rng);
                }
            }

            // Corridors run between the centres of neighbouring cells, so each one starts
            // inside the room it serves rather than on the grid boundary.
            for (int gy = 0; gy < gh; gy++)
            {
                for (int gx = 0; gx < gw; gx++)
                {
                    int i = gy * gw + gx;
                    if (!placed[i]) continue;
                    if (gx + 1 < gw && placed[i + 1]) Link(cells, i, i + 1, map, rng);
                    if (gy + 1 < gh && placed[i + gw]) Link(cells, i, i + gw, map, rng);
                }
            }

            // Anything still isolated gets a corridor to the nearest placed cell, so the
            // level is one connected space rather than several islands.
            for (int gy = 0; gy < gh; gy++)
            {
                for (int gx = 0; gx < gw; gx++)
                {
                    int i = gy * gw + gx;
                    if (!placed[i]) continue;
                    bool linked = (gx > 0 && placed[i - 1]) || (gx + 1 < gw && placed[i + 1]) ||
                                   (gy > 0 && placed[i - gw]) || (gy + 1 < gh && placed[i + gw]);
                    if (linked) continue;
                    int best = -1, bestD = int.MaxValue;
                    for (int j = 0; j < cells.Length; j++)
                    {
                        if (j == i || !placed[j]) continue;
                        int d = Math.Abs(j % gw - gx) + Math.Abs(j / gw - gy);
                        if (d >= bestD) continue;
                        bestD = d; best = j;
                    }
                    if (best >= 0) Link(cells, i, best, map, rng);
                }
            }
        }

        /// <summary>Corridor between the centres of two placed grid cells.</summary>
        static void Link(Room[] cells, int a, int b, GameMap map, Rng rng)
        {
            CarveCorridor(map, cells[a].CenterX, cells[a].CenterY, cells[b].CenterX, cells[b].CenterY, rng);
        }

        // ----------------------------------------------------------------- fort

        static void GenFort(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.WallDark);
            int cx = map.W / 2, cy = map.H / 2;
            int layers = 3;
            for (int l = 0; l < layers; l++)
            {
                int w = 30 - l * 9, h = 21 - l * 6;
                int x0 = cx - w / 2, y0 = cy - h / 2;
                for (int x = x0; x <= x0 + w; x++)
                {
                    for (int y = y0; y <= y0 + h; y++)
                    {
                        if (!map.InBounds(x, y)) continue;
                        bool edge = x == x0 || x == x0 + w || y == y0 || y == y0 + h;
                        if (edge) map.Set(x, y, TileKind.WallAlt);
                        else if (map.Get(x, y) == TileKind.WallDark) map.Set(x, y, TileKind.Floor);
                    }
                }
                // Gateways through each ring.
                if (l < layers - 1)
                {
                    int gx = x0 + w / 2;
                    map.Set(gx, y0, TileKind.Floor);
                    map.Set(gx, y0 + h, TileKind.Floor);
                    int gy = y0 + h / 2;
                    map.Set(x0, gy, TileKind.Floor);
                    map.Set(x0 + w, gy, TileKind.Floor);
                }
            }
            // The vault chamber sits dead centre.
            for (int x = cx - 3; x <= cx + 3; x++)
                for (int y = cy - 3; y <= cy + 3; y++)
                    map.Set(x, y, TileKind.WallAlt);
            for (int x = cx - 2; x <= cx + 2; x++)
                for (int y = cy - 2; y <= cy + 2; y++)
                    map.Set(x, y, TileKind.Floor);
            map.Set(cx, cy - 2, TileKind.Altar);
            map.Set(cx + 2, cy + 2, TileKind.Fountain);
        }

        // -------------------------------------------------------------- warrens

        static void GenWarrens(GameMap map, GenOptions o, Rng rng)
        {
            map.Fill(TileKind.WallDark);
            // Organic cell growth: scatter seeds, join nearby ones.
            var seeds = new List<int>();
            int count = Math.Max(6, o.MaxRooms);
            for (int i = 0; i < count; i++)
            {
                int x = rng.Range(4, map.W - 5), y = rng.Range(4, map.H - 5);
                seeds.Add(x + y * map.W);
                int r = rng.Range(3, 7);
                for (int yy = -r; yy <= r; yy++)
                    for (int xx = -r; xx <= r; xx++)
                    {
                        if (xx * xx + yy * yy > r * r) continue;
                        int nx = x + xx, ny = y + yy;
                        if (!map.InBounds(nx, ny)) continue;
                        if (map.Get(nx, ny) == TileKind.WallDark) map.Set(nx, ny, TileKind.Floor);
                    }
            }
            for (int i = 1; i < seeds.Count; i++)
            {
                int ax = seeds[i - 1] % map.W, ay = seeds[i - 1] / map.W;
                int bx = seeds[i] % map.W, by = seeds[i] / map.W;
                CarveCorridor(map, ax, ay, bx, by, rng);
            }
            // Break the remaining islands out.
            for (int i = 0; i < map.W * map.H; i++)
            {
                if (map.GetRaw(i) == TileKind.Floor) continue;
                int x = i % map.W, y = i / map.W;
                int floor = 0;
                for (int k = 0; k < 8; k++)
                    if (map.Get(x + Pathfinder.Dx8[k], y + Pathfinder.Dy8[k]) == TileKind.Floor) floor++;
                if (floor >= 5 && rng.Chance(30)) map.SetRaw(i, TileKind.Floor);
            }
            // Standing stones, Caves of Qud style.
            for (int i = 0; i < count; i++)
            {
                int x = rng.Range(3, map.W - 4), y = rng.Range(3, map.H - 4);
                if (!map.Walkable(x, y)) continue;
                for (int d = 0; d < 8; d++)
                {
                    int nx = x + Pathfinder.Dx8[d] * 2, ny = y + Pathfinder.Dy8[d] * 2;
                    if (map.Walkable(nx, ny)) map.Set(nx, ny, TileKind.Rubble);
                }
            }
        }

        // ----------------------------------------------------------- corridors

        public static void CarveCorridor(GameMap map, int x0, int y0, int x1, int y1, Rng rng)
        {
            int x = x0, y = y0;
            bool horizFirst = rng.Chance(50);
            int guard = map.W + map.H;
            while ((x != x1 || y != y1) && guard-- > 0)
            {
                if (horizFirst ? (x != x1) : (y == y1))
                {
                    if (x != x1) x += Math.Sign(x1 - x);
                }
                else
                {
                    if (y != y1) y += Math.Sign(y1 - y);
                }
                if (map.InBounds(x, y)) map.Set(x, y, TileKind.Floor);
                if (rng.Chance(10))
                {
                    int w = rng.Range(1, 3);
                    for (int i = 1; i <= w; i++)
                    {
                        if (map.InBounds(x + i, y)) map.Set(x + i, y, TileKind.Floor);
                        if (map.InBounds(x, y + i)) map.Set(x, y + i, TileKind.Floor);
                    }
                }
            }
        }

        // ------------------------------------------------------------ post-pass

        /// <summary>
        /// Seals off anything the player could not reach.
        ///
        /// Two details matter. First, this runs AFTER doors are placed, so a closed
        /// door across a one-wide corridor must not make the far side unreachable.
        /// Reachability is therefore measured with doors treated as passable — the
        /// player can always open one. Second, one 8-way pass is enough: sealing on
        /// 4-way reachability first and re-measuring 8-way used to convert every
        /// diagonally-joined pair of rooms to wall and collapse the level.
        /// </summary>
        static void RepairConnectivity(GameMap map, Rng rng)
        {
            int start = FindAnyFloor(map);
            if (start < 0) { ForceFloor(map); return; }

            var reach = ReachableTreatingDoors(map, start % map.W, start / map.W);
            for (int i = 0; i < map.W * map.H; i++)
            {
                if (Tiles.Walkable(map.GetRaw(i)) && !reach[i]) map.SetRaw(i, WallFor(map, i));
            }
        }

        /// <summary>8-way flood fill where closed and locked doors do not block.</summary>
        static bool[] ReachableTreatingDoors(GameMap map, int sx, int sy)
        {
            var seen = new bool[map.W * map.H];
            if (!map.InBounds(sx, sy) || !Passable(map.Get(sx, sy))) return seen;

            var q = new Queue<int>();
            int start = sx + sy * map.W;
            seen[start] = true;
            q.Enqueue(start);

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cx = cur % map.W, cy = cur / map.W;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Pathfinder.Dx8[k], ny = cy + Pathfinder.Dy8[k];
                    if (!map.InBounds(nx, ny)) continue;
                    int ni = nx + ny * map.W;
                    if (seen[ni]) continue;
                    if (!Passable(map.Get(nx, ny))) continue;
                    // No cutting through wall corners.
                    if (Pathfinder.Dx8[k] != 0 && Pathfinder.Dy8[k] != 0 &&
                        (!Passable(map.Get(cx + Pathfinder.Dx8[k], cy)) || !Passable(map.Get(cx, cy + Pathfinder.Dy8[k]))))
                        continue;
                    seen[ni] = true;
                    q.Enqueue(ni);
                }
            }
            return seen;
        }

        /// <summary>Passable for level-repair purposes: walkable, or a door the player can open.</summary>
        static bool Passable(TileKind t)
        {
            if (Tiles.Walkable(t)) return true;
            if (t == TileKind.ClosedDoor || t == TileKind.LockedDoor || t == TileKind.HiddenDoor) return true;
            return false;
        }

        static TileKind WallFor(GameMap map, int i)
        {
            return map.GetRaw(i) == TileKind.Floor ? TileKind.Wall : map.GetRaw(i);
        }

        public static void ForceFloor(GameMap map)
        {
            for (int y = 1; y < map.H - 1; y++)
                for (int x = 1; x < map.W - 1; x++)
                    map.Set(x, y, TileKind.Floor);
        }

        public static int FindAnyFloor(GameMap map)
        {
            for (int i = 0; i < map.W * map.H; i++) if (map.Walkable(i % map.W, i / map.W)) return i;
            return -1;
        }

        /// <summary>Places doors on thresholds between floor and wall, NetHack style.</summary>
        static void AddDoors(GameMap map, Rng rng)
        {
            var candidates = new List<int>();
            for (int y = 1; y < map.H - 1; y++)
            {
                for (int x = 1; x < map.W - 1; x++)
                {
                    if (!map.Walkable(x, y)) continue;
                    TileKind t = map.Get(x, y);
                    if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;

                    // A door belongs where a corridor meets a room: solid on one
                    // side, open ahead, and pinched on both flanks. The previous
                    // test ("exactly one wall neighbour") also matched floor beside
                    // a pillar, which carpeted the level with doors.
                    for (int k = 0; k < 4; k++)
                    {
                        int dx = Dirs.Dx4[k], dy = Dirs.Dy4[k];
                        if (!IsRock(map.Get(x + dx, y + dy))) continue;

                        int px = -dy, py = dx;          // perpendicular pair
                        if (!map.Walkable(x + px, y + py)) continue;
                        if (!map.Walkable(x - px, y - py)) continue;
                        if (!map.Walkable(x - dx, y - dy)) continue;

                        candidates.Add(x + y * map.W);
                        break;
                    }
                }
            }
            foreach (int c in candidates)
            {
                if (!rng.Chance(55)) continue;
                int roll = rng.Range(0, 100);
                TileKind door = roll < 12 ? TileKind.LockedDoor
                           : roll < 45 ? TileKind.ClosedDoor
                           : TileKind.OpenDoor;
                map.SetRaw(c, door);
            }
        }

        static bool IsRock(TileKind t)
        {
            return t == TileKind.Wall || t == TileKind.WallAlt || t == TileKind.WallDark;
        }

        static void Validate(GameMap map, GenOptions o, Rng rng, List<SpecialRoom> specials, List<int> startCells)
        {
            var up = new List<int>();
            var down = new List<int>();
            var open = new List<int>();
            for (int i = 0; i < map.W * map.H; i++)
            {
                if (!map.Walkable(i % map.W, i / map.W)) continue;
                open.Add(i);
            }
            if (open.Count == 0) { ForceFloor(map); startCells.Add(1 + map.W); return; }

            rng.Shuffle(open);
            if (o.AllowStairsUp && open.Count > 0)
            {
                int i = open[0];
                map.SetRaw(i, TileKind.StairsUp);
                up.Add(i);
                startCells.Add(i);
            }
            else
            {
                startCells.Add(open[0]);
            }
            if (o.AllowStairsDown && open.Count > 1)
            {
                // Pick the down stairs by PATH distance from the up stairs, not
                // geometric distance: a cell can be two steps away and still be
                // sealed behind a wall, which is how a level ends up with
                // unreachable stairs. Doors count as passable for this search
                // because the player can always open one, but the chosen cell
                // must additionally be reachable without opening anything.
                int sx = startCells[0] % map.W, sy = startCells[0] / map.W;
                var dist = PathDistance(map, sx, sy);
                var strict = map.Reachability(sx, sy, true);

                int best = -1, bestD = -1;
                for (int i = 0; i < open.Count; i++)
                {
                    int cell = open[i];
                    if (cell == startCells[0]) continue;
                    if (!strict[cell]) continue;
                    int d = dist[cell];
                    if (d > bestD) { bestD = d; best = cell; }
                }

                if (best < 0 || bestD < 4)
                {
                    // Degenerate level: carve a plain corridor from the entrance and
                    // hang the stairs at the far end so the level is always traversable.
                    best = CarveGuaranteedRoute(map, sx, sy);
                }

                map.SetRaw(best, TileKind.StairsDown);
                down.Add(best);
            }
        }

        /// <summary>BFS step count from a cell over strictly walkable tiles; -1 where unreachable.</summary>
        static int[] PathDistance(GameMap map, int sx, int sy)
        {
            int n = map.W * map.H;
            var dist = new int[n];
            for (int i = 0; i < n; i++) dist[i] = -1;

            if (!map.Walkable(sx, sy)) return dist;

            var q = new Queue<int>();
            int start = sx + sy * map.W;
            dist[start] = 0;
            q.Enqueue(start);

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cx = cur % map.W, cy = cur / map.W;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dirs.Dx8[k], ny = cy + Dirs.Dy8[k];
                    if (!map.InBounds(nx, ny) || !map.Walkable(nx, ny)) continue;
                    if (Dirs.Dx8[k] != 0 && Dirs.Dy8[k] != 0 &&
                        (map.BlocksMove(cx + Dirs.Dx8[k], cy) || map.BlocksMove(cx, cy + Dirs.Dy8[k]))) continue;
                    int ni = nx + ny * map.W;
                    if (dist[ni] >= 0) continue;
                    dist[ni] = dist[cur] + 1;
                    q.Enqueue(ni);
                }
            }
            return dist;
        }

        /// <summary>
        /// Last-resort layout: a straight L corridor from the entrance to the far
        /// corner. Guarantees a traversable level even when the generator produced
        /// a degenerate one, and returns the cell to place the stairs in.
        /// </summary>
        static int CarveGuaranteedRoute(GameMap map, int sx, int sy)
        {
            int fx = Math.Max(1, map.W - 2);
            int fy = Math.Max(1, map.H - 2);
            CarveLine(map, sx, sy, fx, sy);
            CarveLine(map, fx, sy, fx, fy);

            int target = fx + fy * map.W;
            if (map.Walkable(fx, fy)) return target;

            // The corner was sealed by something solid; take any walkable cell that
            // the new corridor actually reaches.
            for (int y = 1; y < map.H - 1; y++)
            {
                for (int x = 1; x < map.W - 1; x++)
                {
                    if (map.Walkable(x, y) && (x >= sx || y >= sy)) return x + y * map.W;
                }
            }
            return FindAnyFloor(map);
        }

        /// <summary>Deterministic L-shaped carve; no randomness, so the fallback layout is reproducible.</summary>
        static void CarveLine(GameMap map, int x0, int y0, int x1, int y1)
        {
            int x = x0, y = y0;
            int guard = map.W + map.H + 4;
            while (guard-- > 0)
            {
                if (!map.InBounds(x, y)) break;
                if (!map.Walkable(x, y)) map.Set(x, y, TileKind.Floor);
                if (x == x1 && y == y1) break;
                if (x != x1) x += Math.Sign(x1 - x);
                else if (y != y1) y += Math.Sign(y1 - y);
            }
        }
    }
}
