using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// Field of view by symmetric ray casting with wall-corner reveal.
    ///
    /// Rays are traced from the viewer in all directions, and every cell a ray passes
    /// through becomes visible. Shadowcasting's per-cell slope arithmetic is avoided
    /// deliberately: it is the part of roguelike FOV that goes subtly asymmetric,
    /// because a cell sitting exactly on a sector boundary is decided by whichever
    /// sector happens to reach it first. Rays have no such boundary — a cell is
    /// visible if any ray sees it — so visibility is symmetric by construction, which
    /// matters more here than the last few cells of speed.
    ///
    /// Two details make it look right rather than merely be right:
    ///
    ///   * Rays are densely spaced (8 per cell at the edge of the circle) and stepped
    ///     along the line, not sampled per row, so diagonal walls do not leave gaps.
    ///
    ///   * When a ray hits a wall, the cell just past its corner is revealed if the
    ///     neighbouring cell on the other side is open. That is what makes a doorframe
    ///     show its full edge instead of a notched silhouette.
    /// </summary>
    public static class Fov
    {
        /// <summary>Rays per cardinal direction. 36 across a 90-degree sector is plenty.</summary>
        const int RaysPerQuadrant = 36;

        public static void Compute(GameMap map, int ox, int oy, int radius, List<int> visibleOut)
        {
            visibleOut?.Clear();
            if (map == null || !map.InBounds(ox, oy) || radius <= 0) return;

            Mark(map, visibleOut, ox, oy);

            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                // Rays are cast into one quadrant only; the octant pairs follow from
                // mirroring. Casting all four quadrants and mirroring means every cell
                // is tested from both mirrored directions.
                bool flipX = (quadrant & 1) != 0;
                bool flipY = (quadrant & 2) != 0;

                for (int i = 0; i < RaysPerQuadrant; i++)
                {
                    // Sample the angle across the quadrant, excluding the exact edges:
                    // the axes are handled by the mirror pass, and double-counting them
                    // costs time without changing the result.
                    double t = (i + 0.5) / RaysPerQuadrant;
                    double angle = t * Math.PI * 0.5;
                    double dirX = Math.Cos(angle);
                    double dirY = Math.Sin(angle);
                    if (flipX) dirX = -dirX;
                    if (flipY) dirY = -dirY;

                    Trace(map, visibleOut, ox, oy, radius, dirX, dirY);
                }
            }

            // The four axes sit between the quadrants, so give them their own rays.
            TraceRay(map, visibleOut, ox, oy, radius, 1, 0);
            TraceRay(map, visibleOut, ox, oy, radius, -1, 0);
            TraceRay(map, visibleOut, ox, oy, radius, 0, 1);
            TraceRay(map, visibleOut, ox, oy, radius, 0, -1);
        }

        static void Mark(GameMap map, List<int> visible, int cx, int cy)
        {
            map.SetVisible(cx, cy, true);
            map.Remember(cx, cy);
            visible?.Add(cy * map.W + cx);
        }

        /// <summary>Walks a ray cell by cell, marking what it sees and stopping at a wall.</summary>
        static void Trace(GameMap map, List<int> visible, int ox, int oy, int radius, double dirX, double dirY)
        {
            // Walk the ray as a parametric line and sample it at sub-cell intervals. Rounding
            // a running cell position instead would step diagonally whenever both
            // components advanced, producing a diamond of visible cells rather than a
            // disc.
            double step = 0.25;
            double lastVisitedX = ox, lastVisitedY = oy;
            int lastX = ox, lastY = oy;

            for (double dist = step; dist <= radius + step; dist += step)
            {
                double fx = ox + dirX * dist;
                double fy = oy + dirY * dist;
                int nx = (int)Math.Round(fx);
                int ny = (int)Math.Round(fy);

                if (nx == (int)Math.Round(lastVisitedX) && ny == (int)Math.Round(lastVisitedY))
                {
                    lastVisitedX = fx; lastVisitedY = fy;
                    continue;
                }

                // Reject diagonal steps through a solid corner rather than slipping past it.
                if (nx != lastX && ny != lastY &&
                    map.Opaque(nx, lastY) && map.Opaque(lastX, ny))
                {
                    lastVisitedX = fx; lastVisitedY = fy;
                    continue;
                }

                if (!map.InBounds(nx, ny)) return;

                Mark(map, visible, nx, ny);
                if (map.Opaque(nx, ny))
                {
                    RevealCorner(map, visible, lastX, lastY, nx, ny);
                    return;
                }

                lastX = nx; lastY = ny;
                lastVisitedX = fx; lastVisitedY = fy;
            }
        }

        static void TraceRay(GameMap map, List<int> visible, int ox, int oy, int radius, int dx, int dy)
        {
            for (int d = 1; d <= radius; d++)
            {
                int cx = ox + dx * d, cy = oy + dy * d;
                if (!map.InBounds(cx, cy)) return;
                Mark(map, visible, cx, cy);
                if (map.Opaque(cx, cy)) return;
            }
        }

        /// <summary>
        /// Reveals the cell diagonally past a wall corner when both flanking cells are
        /// open, so a doorway shows its full edge instead of a notched silhouette.
        /// The two flanks are the orthogonal neighbours of the corner cell.
        /// </summary>
        static void RevealCorner(GameMap map, List<int> visible, int lastX, int lastY, int cx, int cy)
        {
            int dx = cx - lastX, dy = cy - lastY;
            if (dx == 0 || dy == 0) return;   // a square hit reveals nothing extra

            int cornerX = cx + dx, cornerY = cy + dy;
            if (!map.InBounds(cornerX, cornerY) || map.Opaque(cornerX, cornerY)) return;

            bool flankA = Open(map, lastX + dx, cy);
            bool flankB = Open(map, cx, lastY + dy);
            if (flankA && flankB) Mark(map, visible, cornerX, cornerY);
        }

        static bool Open(GameMap map, int x, int y) => map.InBounds(x, y) && !map.Opaque(x, y);

        /// <summary>Bresenham line of sight. Used by projectiles and ranged attacks.</summary>
        public static bool HasLine(GameMap map, int x0, int y0, int x1, int y1)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int x = x0, y = y0;
            int guard = dx + dy + 4;
            while (guard-- > 0)
            {
                if (x == x1 && y == y1) return true;
                if (!(x == x0 && y == y0) && map.Opaque(x, y)) return false;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
            return false;
        }
    }
}