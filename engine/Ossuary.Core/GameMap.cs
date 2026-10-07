using System;
using System.Collections.Generic;
using System.Text;

namespace Ossuary.Core
{
    /// <summary>
    /// A single level. Holds terrain plus the per-cell memory the player has of it,
    /// which is what lets the "remembered map" render while out of sight.
    /// </summary>
    public sealed class GameMap
    {
        public readonly int W;
        public readonly int H;
        readonly TileKind[] _tiles;
        readonly TileKind[] _remem;      // what the player remembers (never un-remembered)
        readonly bool[] _visible;        // recomputed every turn
        readonly byte[] _known;          // 0 unseen, 1 seen, 2 currently visible

        public string BranchName = "";
        public int Depth = 1;
        public string LevelName = "";
        public int Number;               // global unique level id
        /// <summary>Sparse floor surfaces (water, ice, fire, oil, brush), keyed by cell index.</summary>
        public readonly Dictionary<int, Surface> Surfaces = new Dictionary<int, Surface>();
        /// <summary>Blood, ichor, slime, mud, soot, footprints and drag marks, by cell (see <see cref="Stain"/>).</summary>
        public readonly Dictionary<int, Stain> Stains = new Dictionary<int, Stain>();
        /// <summary>Engraved text on a floor cell, by cell.</summary>
        public readonly Dictionary<int, string> Engravings = new Dictionary<int, string>();
        /// <summary>The legend an engraving or a tomb teaches when read ("e:3", "f:12"), by cell.</summary>
        public readonly Dictionary<int, string> EngravingLegends = new Dictionary<int, string>();
        /// <summary>The rooms the generator laid out, with what each was for (null for towns and caves).</summary>
        public List<Gen.Room> Rooms;

        public Stain StainAt(int x, int y) => InBounds(x, y) && Stains.TryGetValue(y * W + x, out var s) ? s : default;
        public string EngravingAt(int x, int y) => InBounds(x, y) && Engravings.TryGetValue(y * W + x, out var s) ? s : null;
        public int Version;              // bumped on any change, drives render dirty checks
        public bool Generated;

        public GameMap(int w, int h)
        {
            W = w; H = h;
            _tiles = new TileKind[w * h];
            _remem = new TileKind[w * h];
            _visible = new bool[w * h];
            _known = new byte[w * h];
        }

        public int Index(int x, int y) => y * W + x;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public TileKind Get(int x, int y) => InBounds(x, y) ? _tiles[y * W + x] : TileKind.Void;
        public TileKind Get(int x, int y, out bool ok)
        {
            ok = InBounds(x, y);
            return ok ? _tiles[y * W + x] : TileKind.Void;
        }

        public void Set(int x, int y, TileKind t)
        {
            if (!InBounds(x, y)) return;
            _tiles[y * W + x] = t;
            Version++;
        }

        /// <summary>Unchecked write used by the generators, which stay in bounds by construction.</summary>
        public void SetRaw(int idx, TileKind t) { _tiles[idx] = t; }

        /// <summary>Bulk write of a full terrain layer, used by the cellular-automata cave passes.</summary>
        public void ReplaceRaw(TileKind[] layer)
        {
            if (layer == null || layer.Length != _tiles.Length) return;
            Array.Copy(layer, _tiles, _tiles.Length);
            Version++;
        }

        public SurfaceKind SurfaceAt(int x, int y) =>
            InBounds(x, y) && Surfaces.TryGetValue(y * W + x, out var s) ? s.Kind : SurfaceKind.None;

        public void SetSurface(int x, int y, SurfaceKind kind, int turns = 0)
        {
            if (!InBounds(x, y)) return;
            if (kind == SurfaceKind.None) Surfaces.Remove(y * W + x);
            else Surfaces[y * W + x] = new Surface { Kind = kind, Turns = turns };
            Version++;
        }

        public TileKind GetRaw(int idx) => _tiles[idx];
        public TileKind[] GetRawBuffer() => _tiles;

        public TileKind Remembered(int x, int y) => InBounds(x, y) ? _remem[y * W + x] : TileKind.Void;

        /// <summary>Burn the entire level into memory (scroll of magic mapping).</summary>
        public void RememberAll()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                _remem[i] = _tiles[i];
                if (_known[i] < 1) _known[i] = 1;
            }
            Version++;
        }
        public bool IsVisible(int x, int y) => InBounds(x, y) && _visible[y * W + x];
        public bool WasSeen(int x, int y) => InBounds(x, y) && _known[y * W + x] > 0;
        public bool IsCurrentlyVisible(int x, int y) => InBounds(x, y) && _known[y * W + x] >= 2;

        public void Remember(int x, int y)
        {
            if (!InBounds(x, y)) return;
            int i = y * W + x;
            _remem[i] = _tiles[i];
            if (_known[i] < 1) _known[i] = 1;
        }

        public void SetVisible(int x, int y, bool v)
        {
            if (!InBounds(x, y)) return;
            int i = y * W + x;
            _visible[i] = v;
            // Never demote a cell from "seen" back to "unseen"; just release it from
            // the currently-lit band. Math.Max(byte,byte) vs Math.Max(int,int) is
            // ambiguous in recent BCLs, so do the comparison in int space.
            int current = _known[i];
            byte want = v ? (byte)2 : (byte)(current > 1 ? current : 1);
            if (_known[i] != want) { _known[i] = want; Version++; }
        }

        public void ClearVisibility()
        {
            for (int i = 0; i < _known.Length; i++)
            {
                if (_known[i] == 2) _known[i] = 1;
                _visible[i] = false;
            }
        }

        public void Fill(TileKind t)
        {
            for (int i = 0; i < _tiles.Length; i++) { _tiles[i] = t; _remem[i] = t; }
            Version++;
        }

        public bool Walkable(int x, int y) => InBounds(x, y) && Tiles.Walkable(_tiles[y * W + x]);
        public bool Opaque(int x, int y) => !InBounds(x, y) || Tiles.Opaque(_tiles[y * W + x]);
        public bool BlocksMove(int x, int y) => !InBounds(x, y) || Tiles.BlocksMove(_tiles[y * W + x]);

        public char GlyphOf(int x, int y) => Tiles.Get(Get(x, y)).Glyph;

        /// <summary>Diagonal move legality: no cutting through wall corners.</summary>
        public bool CanStep(int fromX, int fromY, int toX, int toY, bool allowDiagonal)
        {
            int dx = toX - fromX, dy = toY - fromY;
            if (dx == 0 && dy == 0) return false;
            if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1) return false;
            if (!Walkable(toX, toY)) return false;
            if (dx != 0 && dy != 0 && !allowDiagonal) return false;
            if (dx != 0 && dy != 0)
            {
                if (BlocksMove(fromX + dx, fromY) || BlocksMove(fromX, fromY + dy)) return false;
            }
            return true;
        }

        /// <summary>Plain-ASCII dump, used by the headless tests and the level preview.</summary>
        public string ToAscii(bool rememberedOnly = false, int markX = -1, int markY = -1)
        {
            var sb = new StringBuilder(W * (H + 1));
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (x == markX && y == markY) { sb.Append('@'); continue; }
                    TileKind t = rememberedOnly ? Remembered(x, y) : Get(x, y);
                    sb.Append(Tiles.Get(t).Glyph);
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Flood fill from a start cell; returns everything reachable by walking.</summary>
        public bool[] Reachability(int startX, int startY, bool allowDiagonal = false)
        {
            var seen = new bool[W * H];
            if (!Walkable(startX, startY)) return seen;
            var q = new Queue<int>();
            int s0 = startX + startY * W;
            seen[s0] = true;
            q.Enqueue(s0);
            Span<int> dx = stackalloc int[8] { 1, -1, 0, 0, 1, 1, -1, -1 };
            Span<int> dy = stackalloc int[8] { 0, 0, 1, -1, 1, -1, 1, -1 };
            int limit = allowDiagonal ? 8 : 4;
            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cx = cur % W, cy = cur / W;
                for (int k = 0; k < limit; k++)
                {
                    int nx = cx + dx[k], ny = cy + dy[k];
                    if (!InBounds(nx, ny)) continue;
                    int ni = nx + ny * W;
                    if (seen[ni]) continue;
                    if (!Walkable(nx, ny)) continue;
                    if (allowDiagonal && dx[k] != 0 && dy[k] != 0 &&
                        (BlocksMove(cx + dx[k], cy) || BlocksMove(cx, cy + dy[k]))) continue;
                    seen[ni] = true;
                    q.Enqueue(ni);
                }
            }
            return seen;
        }

        public int CountWalkable()
        {
            int n = 0;
            for (int i = 0; i < _tiles.Length; i++) if (Tiles.Walkable(_tiles[i])) n++;
            return n;
        }

        public void Stamp(int x, int y, int w, int h, TileKind fill, bool overwriteSolid)
        {
            for (int yy = y; yy < y + h; yy++)
            {
                for (int xx = x; xx < x + w; xx++)
                {
                    if (!InBounds(xx, yy)) continue;
                    if (!overwriteSolid && BlocksMove(xx, yy)) continue;
                    SetRaw(xx + yy * W, fill);
                }
            }
            Version++;
        }
    }
}
