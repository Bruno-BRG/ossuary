using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// Travel marks: the hero marks cells on a dungeon level and walks back to the nearest one with the marks key
    /// (Shift+'). A mark is a plain record of a cell, kept in the order it was set, so a walk never depends on hash
    /// order. Nothing here keeps the Rng or the clock: the walk is ordinary turns (Commands.DoAutoWalk), and the marks
    /// replay from the run log.
    /// </summary>
    public sealed partial class Game
    {
        readonly List<(int Map, int X, int Y)> _marks = new List<(int Map, int X, int Y)>();

        /// <summary>Sets a mark on the hero's cell, or takes it off if it is marked. Returns true when it is now marked.</summary>
        public bool ToggleMarkHere()
        {
            if (Map == null) return false;
            int i = _marks.FindIndex(m => m.Map == Map.Number && m.X == Player.X && m.Y == Player.Y);
            if (i >= 0) { _marks.RemoveAt(i); return false; }
            _marks.Add((Map.Number, Player.X, Player.Y));
            return true;
        }

        /// <summary>The cell is marked on the current level.</summary>
        public bool IsMarked(int x, int y) => Map != null && _marks.Exists(m => m.Map == Map.Number && m.X == x && m.Y == y);

        /// <summary>Some mark is set on the current level.</summary>
        public bool HasMarkHere() => Map != null && _marks.Exists(m => m.Map == Map.Number);

        /// <summary>A mark on the current level other than the hero's own cell: a place the marks walk can go.</summary>
        public bool HasOtherMarkHere() =>
            Map != null && _marks.Exists(m => m.Map == Map.Number && (m.X != Player.X || m.Y != Player.Y));

        /// <summary>
        /// The first step toward the nearest other mark, and that mark (tx, ty). A walk picks its target once and keeps it:
        /// re-picking the nearest mark at every step would turn the hero back at the mark it has just left.
        /// </summary>
        public bool MarkStep(out int dx, out int dy, out int tx, out int ty) => AutoStep(IsMarked, out dx, out dy, out tx, out ty);

        /// <summary>The first step toward the chosen mark (tx, ty).</summary>
        public bool MarkStepTo(int tx, int ty, out int dx, out int dy) => AutoStep((x, y) => x == tx && y == ty, out dx, out dy);
    }
}
