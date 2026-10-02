using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// Auto-walk helpers: auto-explore, travel to stairs and rest. All of it is a plain loop over ordinary
    /// player moves (see Commands.DoExplore), so the run log replays it exactly; nothing here keeps the Rng
    /// or the clock to itself. The only state is the set of cells explore has already served.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Cells (per map) that explore already walked to and should not choose again.</summary>
        readonly HashSet<long> _exploreDone = new HashSet<long>();

        long ExploreKey(int x, int y) => Map.Number * 20_000_000L + y * 4096 + x;

        /// <summary>The cell under the player has been served: an item pile was visited or a frontier was looked at.</summary>
        public void ExploreMarkHere()
        {
            if (Map == null) return;
            if (ExploreGoal(Player.X, Player.Y)) _exploreDone.Add(ExploreKey(Player.X, Player.Y));
        }

        /// <summary>A known, walkable cell worth walking to: a pile of items not yet visited, or an edge of the known map.</summary>
        public bool ExploreGoal(int x, int y)
        {
            if (_exploreDone.Contains(ExploreKey(x, y))) return false;
            var pile = GroundItems.At(Map.Number, x, y);
            if (pile != null && pile.Count > 0) return true;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + Pathfinder.Dx8[k], ny = y + Pathfinder.Dy8[k];
                if (Map.InBounds(nx, ny) && !Map.WasSeen(nx, ny)) return true;
            }
            return false;
        }

        /// <summary>
        /// Breadth-first search over cells the player has seen. Returns the first step toward the nearest
        /// cell (other than the player's own) where <paramref name="goal"/> holds.
        /// </summary>
        public bool AutoStep(Func<int, int, bool> goal, out int dx, out int dy)
        {
            dx = dy = 0;
            if (Map == null) return false;
            int w = Map.W, n = w * Map.H;
            var from = new int[n];
            for (int i = 0; i < n; i++) from[i] = -2;
            int start = Player.X + Player.Y * w;
            from[start] = -1;
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int cx = cur % w, cy = cur / w;
                if (cur != start && goal(cx, cy))
                {
                    int step = cur;
                    while (from[step] != start) step = from[step];
                    dx = step % w - Player.X; dy = step / w - Player.Y;
                    return true;
                }
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Pathfinder.Dx8[k], ny = cy + Pathfinder.Dy8[k];
                    if (!Map.InBounds(nx, ny) || !Map.WasSeen(nx, ny)) continue;
                    int ni = nx + ny * w;
                    if (from[ni] != -2 || !Map.CanStep(cx, cy, nx, ny, true)) continue;
                    if (Map.SurfaceAt(nx, ny) == SurfaceKind.Fire) continue;
                    from[ni] = cur;
                    queue.Enqueue(ni);
                }
            }
            return false;
        }

        /// <summary>Nearest remembered way down, else way up.</summary>
        public bool StairsStep(out int dx, out int dy, out bool down)
        {
            bool Down(int x, int y) { var t = Map.Get(x, y); return t == TileKind.StairsDown || t == TileKind.LadderDown; }
            down = true;
            if (AutoStep(Down, out dx, out dy)) return true;
            down = false;
            return AutoStep((x, y) => Map.Get(x, y) == TileKind.StairsUp, out dx, out dy);
        }

        public bool NeedsRest() => Player.HP < Player.MaxHP || Player.Mp < Player.MpMax;
    }
}
