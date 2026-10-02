using System;

namespace Ossuary.Core
{
    /// <summary>
    /// Direction tables. Kept in one place so the generators, the FOV and the
    /// movement code all agree on what "north" and "diagonal" mean.
    /// </summary>
    public static class Dirs
    {
        /// <summary>Cardinal steps, clockwise from north.</summary>
        public static readonly int[] Dx4 = { 0, 1, 0, -1 };
        public static readonly int[] Dy4 = { -1, 0, 1, 0 };

        /// <summary>Cardinal steps plus the four diagonals, cardinals first.</summary>
        public static readonly int[] Dx8 = { 0, 1, 1, 1, 0, -1, -1, -1 };
        public static readonly int[] Dy8 = { -1, -1, 0, 1, 1, 1, 0, -1 };

        /// <summary>Clockwise from north, so index 0 is "up" for the minimap rose.</summary>
        public static readonly string[] Names4 = { "north", "east", "south", "west" };

        public static int Opposite(int d) => (d + 2) & 3;

        /// <summary>Angle from a delta, in eighths of a turn, 0 = north and clockwise.</summary>
        public static int DirectionFromDelta(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return 0;
            int ax = Math.Abs(dx), ay = Math.Abs(dy);
            if (ax * 2 >= ay * 3) return dx > 0 ? 2 : 6;
            if (ay * 2 >= ax * 3) return dy > 0 ? 4 : 0;
            if (dx > 0) return dy > 0 ? 3 : 1;
            return dy > 0 ? 5 : 7;
        }

        public static int FromKeys(int dx, int dy) => DirectionFromDelta(dx, dy);
    }
}
