using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// A* + Dijkstra flow fields over the 8-way grid. Monsters use the flow field
    /// (one search per level, shared by all of them) while one-off queries (the
    /// player clicking a tile, travelling to a town) use A*.
    /// </summary>
    public static class Pathfinder
    {
        public const int Unreachable = int.MaxValue;

        public static int Manhattan(int ax, int ay, int bx, int by) => Math.Abs(ax - bx) + Math.Abs(ay - by);

        public static int Chebyshev(int ax, int ay, int bx, int by) => Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));

        /// <summary>Octile distance, the correct heuristic for 8-way movement.</summary>
        public static double Octile(int ax, int ay, int bx, int by)
        {
            int dx = Math.Abs(ax - bx), dy = Math.Abs(ay - by);
            return (dx + dy) + (1.41421356 - 2) * Math.Min(dx, dy);
        }

        public static readonly int[] Dx8 = { 0, 1, 1, 1, 0, -1, -1, -1 };
        public static readonly int[] Dy8 = { 1, 1, 0, -1, -1, -1, 0, 1 };

        /// <summary>
        /// Dijkstra outward from every goal, giving each cell its distance to the
        /// nearest goal and a next step. Recomputed only when the map changes.
        /// </summary>
        public sealed class FlowField
        {
            public readonly int[] Dist;
            public readonly sbyte[] StepX;
            public readonly sbyte[] StepY;
            public readonly int W;
            public readonly int H;

            public FlowField(GameMap map, IEnumerable<int> goals)
            {
                W = map.W; H = map.H;
                Dist = new int[W * H];
                StepX = new sbyte[W * H];
                StepY = new sbyte[W * H];
                for (int i = 0; i < Dist.Length; i++) Dist[i] = Unreachable;

                var open = new PriorityQueue();
                foreach (int g in goals)
                {
                    if (g < 0 || g >= Dist.Length) continue;
                    Dist[g] = 0;
                    open.Push(g, 0);
                }

                while (open.Count > 0)
                {
                    int cur = open.Pop();
                    int cd = Dist[cur];
                    int cx = cur % W, cy = cur / W;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = cx + Dx8[k], ny = cy + Dy8[k];
                        if (!map.InBounds(nx, ny)) continue;
                        int ni = nx + ny * W;
                        if (!map.Walkable(nx, ny)) continue;
                        if (Dx8[k] != 0 && Dy8[k] != 0 &&
                            (map.BlocksMove(cx + Dx8[k], cy) || map.BlocksMove(cx, cy + Dy8[k]))) continue;
                        int nd = cd + 1;
                        if (nd < Dist[ni])
                        {
                            Dist[ni] = nd;
                            // Step points back toward the goal.
                            StepX[ni] = (sbyte)Dx8[k];
                            StepY[ni] = (sbyte)Dy8[k];
                            open.Push(ni, nd);
                        }
                    }
                }
            }

            public int At(int x, int y) => (x < 0 || y < 0 || x >= W || y >= H) ? Unreachable : Dist[y * W + x];
            public bool Reached(int x, int y) => At(x, y) != Unreachable;
        }

        /// <summary>A* path from start to goal. Returns null when no route exists.</summary>
        public static List<int> FindPath(GameMap map, int sx, int sy, int gx, int gy, bool allowDiagonal = true)
        {
            var result = new List<int>();
            if (!map.InBounds(sx, sy) || !map.InBounds(gx, gy)) return result;
            if (!map.Walkable(gx, gy))
            {
                // Path to the closest walkable neighbour of the goal instead.
                int bx = -1, by = -1, bd = int.MaxValue;
                for (int k = 0; k < 8; k++)
                {
                    int nx = gx + Dx8[k], ny = gy + Dy8[k];
                    if (!map.Walkable(nx, ny)) continue;
                    int d = Manhattan(nx, ny, sx, sy);
                    if (d < bd) { bd = d; bx = nx; by = ny; }
                }
                if (bx < 0) return result;
                gx = bx; gy = by;
            }
            if (sx == gx && sy == gy) { result.Add(sx + sy * map.W); return result; }

            int n = map.W * map.H;
            var gScore = new int[n];
            var came = new int[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) { gScore[i] = Unreachable; came[i] = -1; }

            var open = new PriorityQueue();
            int start = sx + sy * map.W;
            gScore[start] = 0;
            open.Push(start, 0);

            int limit = n * 4;
            while (open.Count > 0 && limit-- > 0)
            {
                int cur = open.Pop();
                if (closed[cur]) continue;
                closed[cur] = true;
                int cx = cur % map.W, cy = cur / map.W;
                if (cx == gx && cy == gy) break;

                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dx8[k], ny = cy + Dy8[k];
                    if (!map.InBounds(nx, ny)) continue;
                    int ni = nx + ny * map.W;
                    if (closed[ni] || !map.Walkable(nx, ny)) continue;
                    if (Dx8[k] != 0 && Dy8[k] != 0 && !allowDiagonal) continue;
                    if (allowDiagonal && Dx8[k] != 0 && Dy8[k] != 0 &&
                        (map.BlocksMove(cx + Dx8[k], cy) || map.BlocksMove(cx, cy + Dy8[k]))) continue;
                    int step = (Dx8[k] != 0 && Dy8[k] != 0) ? 14 : 10;
                    int ng = gScore[cur] + step;
                    if (ng >= gScore[ni]) continue;
                    gScore[ni] = ng;
                    came[ni] = cur;
                    open.Push(ni, ng + (int)(Octile(nx, ny, gx, gy) * 10));
                }
            }

            if (gScore[gx + gy * map.W] >= Unreachable) return result;
            int walk = gx + gy * map.W;
            while (walk >= 0) { result.Add(walk); walk = came[walk]; }
            result.Reverse();
            return result;
        }

        /// <summary>Minimal binary heap — avoids pulling in a full container package for one use.</summary>
        sealed class PriorityQueue
        {
            int[] _items = new int[64];
            int[] _keys = new int[64];
            int _count;

            public int Count => _count;

            public void Push(int item, int key)
            {
                if (_count == _items.Length)
                {
                    System.Array.Resize(ref _items, _items.Length * 2);
                    System.Array.Resize(ref _keys, _keys.Length * 2);
                }
                int i = _count++;
                _items[i] = item; _keys[i] = key;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (_keys[p] <= _keys[i]) break;
                    Swap(p, i);
                    i = p;
                }
            }

            public int Pop()
            {
                int top = _items[0];
                _count--;
                if (_count > 0)
                {
                    _items[0] = _items[_count];
                    _keys[0] = _keys[_count];
                    int i = 0;
                    while (true)
                    {
                        int l = i * 2 + 1, r = l + 1, best = i;
                        if (l < _count && _keys[l] < _keys[best]) best = l;
                        if (r < _count && _keys[r] < _keys[best]) best = r;
                        if (best == i) break;
                        Swap(best, i);
                        i = best;
                    }
                }
                return top;
            }

            void Swap(int a, int b)
            {
                int ti = _items[a]; _items[a] = _items[b]; _items[b] = ti;
                int tk = _keys[a]; _keys[a] = _keys[b]; _keys[b] = tk;
            }
        }
    }
}
