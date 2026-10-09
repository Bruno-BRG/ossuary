using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>The look of an effect: colour ramp and particle glyphs. Colours live here, never in the spell data.</summary>
    public enum Elem { Arcane, Fire, Cold, Lightning, Necrotic, Holy, Poison, Nature, Shadow, Earth, Blood, Water, Mind, Wind }

    /// <summary>How a spell looks when cast (what <see cref="FxLib"/> pieces it strings together). Custom: the effect code records it itself.</summary>
    public enum FxKind
    {
        None, Bolt, Ball, Beam, Zap, Cone, Nova, Meteor, Pillar, Rain, Cloud, Eruption, Shatter, Slash, Rise, Swirl, Drain, Mark, Wave, Teleport, Implode, Flash, Custom
    }

    /// <summary>One overlay cell of an effect, in map coordinates. The host turns these into screen cells.</summary>
    public struct FxCell
    {
        public int X, Y;
        public char Glyph;
        public Rgb Fg;
        public bool HasBg;
        public Rgb Bg;
    }

    /// <summary>
    /// A short animation as a list of steps, each a set of overlay cells. The simulation resolves a spell at once;
    /// this only records how it should look, and the front end plays it over the finished frame (about 45 ms a step).
    /// Pure data: no Rng, no state, so recording never changes what the game does.
    /// </summary>
    public sealed class FxTimeline
    {
        public const int StepMs = 45;
        public const int MaxSteps = 64;

        public readonly List<List<FxCell>> Steps = new List<List<FxCell>>();
        /// <summary>Which cells may show an effect (the game passes "in view"). Null shows everything.</summary>
        public Func<int, int, bool> Show;
        /// <summary>The first step at which a projectile of this frame arrives, where its effect lands; -1 when nothing flies.</summary>
        public int Impact = -1;

        public bool IsEmpty => Steps.Count == 0;
        public int Length => Steps.Count;

        /// <summary>Notes that a projectile arrives at this step. The earliest arrival of the frame is kept.</summary>
        public void Arrive(int step) { if (Impact < 0 || step < Impact) Impact = step; }

        public void Clear() { Steps.Clear(); Impact = -1; }

        public void Put(int step, int x, int y, char glyph, Rgb fg, Rgb? bg = null)
        {
            if (step < 0 || step >= MaxSteps) return;
            if (Show != null && !Show(x, y)) return;
            while (Steps.Count <= step) Steps.Add(new List<FxCell>());
            var cells = Steps[step];
            var c = new FxCell { X = x, Y = y, Glyph = glyph, Fg = fg, HasBg = bg.HasValue, Bg = bg ?? default };
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].X == x && cells[i].Y == y) { cells[i] = c; return; }
            cells.Add(c);
        }
    }

    /// <summary>Geometry shared by the effect code and the animations, so what you see is what was hit.</summary>
    public static class Shapes
    {
        /// <summary>Cells on the straight line from (x0,y0) to (x1,y1), both ends included.</summary>
        public static List<(int x, int y)> Line(int x0, int y0, int x1, int y1)
        {
            var l = new List<(int, int)>();
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
            int x = x0, y = y0;
            for (int guard = 0; guard < 400; guard++)
            {
                l.Add((x, y));
                if (x == x1 && y == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
            return l;
        }

        /// <summary>Cells in a cone from the origin toward (tx,ty): out to <paramref name="len"/>, widening about 0.6 cells per cell of distance.</summary>
        public static List<(int x, int y, int d)> Cone(int x0, int y0, int tx, int ty, int len)
        {
            var l = new List<(int, int, int)>();
            double dx = tx - x0, dy = ty - y0, m = Math.Sqrt(dx * dx + dy * dy);
            if (m < 0.5) return l;
            double ux = dx / m, uy = dy / m;
            for (int y = y0 - len; y <= y0 + len; y++)
                for (int x = x0 - len; x <= x0 + len; x++)
                {
                    if (x == x0 && y == y0) continue;
                    double rx = x - x0, ry = y - y0;
                    double along = rx * ux + ry * uy, across = Math.Abs(-rx * uy + ry * ux);
                    if (along < 0.9 || along > len + 0.4) continue;
                    if (across > 0.6 * along + 0.45) continue;
                    l.Add((x, y, Math.Max(1, (int)Math.Round(along))));
                }
            return l;
        }

        /// <summary>Stable value in [0,1) from a cell and a salt. Used instead of Rng so effects never touch the simulation.</summary>
        public static float Hash(int x, int y, int salt = 0) => Theme.Hash01(x * 31 + salt * 7, y * 17 + salt * 13);
    }

    /// <summary>The effect vocabulary. Each call writes into a timeline from a start step and returns the step after it ends.</summary>
    public static class FxLib
    {
        static readonly int[][] Ramp =
        {
            new[] { 0xFFE8FF, 0xE090FF, 0xA060F0, 0x402070 },   // Arcane
            new[] { 0xFFF2B0, 0xFFB030, 0xE0501A, 0x6A2010 },   // Fire
            new[] { 0xF0FFFF, 0x9CE8FF, 0x4A9CE8, 0x1E3C78 },   // Cold
            new[] { 0xFFFFFF, 0xFFF070, 0xB090FF, 0x4A3A90 },   // Lightning
            new[] { 0xD8FFD0, 0x7CE06C, 0x8040B0, 0x301848 },   // Necrotic
            new[] { 0xFFFFF0, 0xFFF0A0, 0xF0C050, 0x7A5A20 },   // Holy
            new[] { 0xF0FF90, 0xA8E030, 0x5C9020, 0x284010 },   // Poison
            new[] { 0xD8FFB0, 0x78D860, 0x3C9840, 0x184020 },   // Nature
            new[] { 0xD0C0F0, 0x9078C8, 0x5A4290, 0x281C48 },   // Shadow
            new[] { 0xF0E0B0, 0xC8A868, 0x8C6A3C, 0x40301A },   // Earth
            new[] { 0xFFB0B0, 0xE03040, 0xA01828, 0x480C14 },   // Blood
            new[] { 0xE0F4FF, 0x80C0FF, 0x3C78D0, 0x183060 },   // Water
            new[] { 0xFFE0F4, 0xFF90D0, 0xC0509C, 0x602050 },   // Mind
            new[] { 0xF0FFFF, 0xC0E8E0, 0x80B0B0, 0x3A5050 },   // Wind
        };

        static readonly string[] Sparks =
        {
            "*+o.", "*^'`", "*+x'", "*/\\|", "x:;.", "+*'.", "o:.~", "&%\"'", "#%:.", "#%:,", "o*'.", "o~'.", "*?!.", "~=-'",
        };

        /// <summary>Colour of an element: t = 0 is the hottest, brightest end of its ramp; 1 is the dimmest.</summary>
        public static Rgb Pal(Elem e, float t)
        {
            var r = Ramp[(int)e];
            t = Math.Max(0f, Math.Min(1f, t)) * 3f;
            int i = Math.Min(2, (int)t);
            return Rgb.Lerp(Rgb.FromHex(r[i]), Rgb.FromHex(r[i + 1]), t - i);
        }

        static Rgb Glow(Elem e, float t) => Pal(e, 0.7f + t * 0.3f) * 0.55f;

        static char Spark(Elem e, int x, int y, int salt, int from = 0)
        {
            string s = Sparks[(int)e];
            int n = s.Length - from;
            return s[from + Math.Min(n - 1, (int)(Shapes.Hash(x, y, salt) * n))];
        }

        // ------------------------------------------------------------ travelling

        /// <summary>A missile from one cell to another, with a fading tail. Returns the step after it lands.</summary>
        public static int Bolt(FxTimeline tl, int s, int x0, int y0, int x1, int y1, Elem e, char head = '*', int speed = 2)
        {
            var path = Shapes.Line(x0, y0, x1, y1);
            int n = path.Count - 1;
            if (n <= 0) return s;
            int steps = (n + speed - 1) / speed;
            if (head == '\0')
            {
                int dx = x1 - x0, dy = y1 - y0, ax = Math.Abs(dx), ay = Math.Abs(dy);
                head = ax >= 2 * ay ? '-' : ay >= 2 * ax ? '|' : dx * dy > 0 ? '\\' : '/';
            }
            for (int st = 0; st < steps; st++)
            {
                int idx = Math.Min(n, (st + 1) * speed);
                tl.Put(s + st, path[idx].x, path[idx].y, head, Pal(e, 0f), Glow(e, 0f));
                for (int k = 1; k <= 3; k++)
                {
                    int j = idx - k;
                    if (j < 1) break;
                    tl.Put(s + st, path[j].x, path[j].y, k == 1 ? Spark(e, path[j].x, path[j].y, st, 0) : '·', Pal(e, 0.25f * k));
                }
            }
            tl.Arrive(s + steps);
            return s + steps;
        }

        static char BeamGlyph(int dx, int dy)
        {
            int ax = Math.Abs(dx), ay = Math.Abs(dy);
            if (ax >= 2 * ay) return '─';
            if (ay >= 2 * ax) return '│';
            return dx * dy > 0 ? '╲' : '╱';
        }

        /// <summary>A steady ray drawn out along a line, held, then faded.</summary>
        public static int Beam(FxTimeline tl, int s, int x0, int y0, int x1, int y1, Elem e, int hold = 3)
        {
            var path = Shapes.Line(x0, y0, x1, y1);
            int n = path.Count - 1;
            if (n <= 0) return s;
            char g = BeamGlyph(x1 - x0, y1 - y0);
            int reveal = 3, total = reveal + hold + 2;
            for (int st = 0; st < total; st++)
            {
                int upto = st < reveal ? Math.Min(n, (int)Math.Ceiling(n * (st + 1) / (double)reveal)) : n;
                for (int k = 1; k <= upto; k++)
                {
                    bool live = st < reveal + hold;
                    float flick = Shapes.Hash(path[k].x, path[k].y, st) * 0.35f;
                    tl.Put(s + st, path[k].x, path[k].y, live ? (k == n ? '*' : g) : '·', live ? Pal(e, flick) : Pal(e, 0.75f), live ? Glow(e, flick) : (Rgb?)null);
                }
            }
            return s + total;
        }

        /// <summary>A jagged flickering bolt of lightning between two cells.</summary>
        public static int Zap(FxTimeline tl, int s, int x0, int y0, int x1, int y1, Elem e = Elem.Lightning, int flickers = 4)
        {
            var path = Shapes.Line(x0, y0, x1, y1);
            int n = path.Count - 1;
            if (n <= 0) return s;
            bool horizontal = Math.Abs(x1 - x0) >= Math.Abs(y1 - y0);
            for (int f = 0; f < flickers; f++)
            {
                var pts = new List<(int x, int y)>();
                for (int k = 0; k <= n; k++)
                {
                    int x = path[k].x, y = path[k].y;
                    if (k > 0 && k < n)
                    {
                        int jit = (int)(Shapes.Hash(x, y, f + 3) * 3) - 1;
                        if (horizontal) y += jit; else x += jit;
                    }
                    pts.Add((x, y));
                }
                for (int k = 1; k <= n; k++)
                {
                    int px = pts[k].x - pts[k - 1].x, py = pts[k].y - pts[k - 1].y;
                    char g = px == 0 ? '|' : py == 0 ? '-' : px * py > 0 ? '\\' : '/';
                    tl.Put(s + f, pts[k].x, pts[k].y, k == n ? '*' : g, Pal(e, f * 0.2f + (k % 3 == 0 ? 0f : 0.25f)), f < 2 ? Glow(e, f * 0.5f) : (Rgb?)null);
                }
            }
            return s + flickers;
        }

        /// <summary>Lightning that hops through a list of cells, two steps a hop.</summary>
        public static int Chain(FxTimeline tl, int s, List<(int x, int y)> points, Elem e = Elem.Lightning)
        {
            int at = s;
            for (int i = 1; i < points.Count; i++)
            {
                Zap(tl, at, points[i - 1].x, points[i - 1].y, points[i].x, points[i].y, e, 3);
                Flash(tl, at + 1, points[i].x, points[i].y, e);
                at += 2;
            }
            return at + 3;
        }

        /// <summary>Particles streaming from one cell to another (life being drawn, mana siphoned).</summary>
        public static int Drain(FxTimeline tl, int s, int fromX, int fromY, int toX, int toY, Elem e)
        {
            var path = Shapes.Line(fromX, fromY, toX, toY);
            int n = path.Count - 1;
            if (n <= 0) return s;
            int total = n + 5;
            for (int st = 0; st < total; st++)
                for (int p = 0; p < 3; p++)
                {
                    int idx = st - p * 2;
                    if (idx < 1 || idx > n) continue;
                    tl.Put(s + st, path[idx].x, path[idx].y, p == 0 ? 'o' : '·', Pal(e, p * 0.3f));
                }
            return s + total;
        }

        /// <summary>A shock front rolling outward along a line: wide and short-lived.</summary>
        public static int Wave(FxTimeline tl, int s, int x0, int y0, int tx, int ty, int len, Elem e)
        {
            var cone = Shapes.Cone(x0, y0, tx, ty, len);
            foreach (var c in cone)
            {
                int st = s + c.d - 1;
                tl.Put(st, c.x, c.y, Spark(e, c.x, c.y, c.d), Pal(e, 0.1f));
                tl.Put(st + 1, c.x, c.y, '·', Pal(e, 0.6f));
            }
            return s + len + 2;
        }

        // ------------------------------------------------------------ areas

        /// <summary>A cone of flame, frost or poison fanned out from the caster, filling in as it reaches out.</summary>
        public static int Cone(FxTimeline tl, int s, int x0, int y0, int tx, int ty, int len, Elem e)
        {
            var cells = Shapes.Cone(x0, y0, tx, ty, len);
            int end = s + len + 3;
            foreach (var c in cells)
                for (int st = c.d - 1; st < len + 3; st++)
                {
                    float age = (st - (c.d - 1)) / 4f;
                    if (age > 0.9f) break;
                    if (age > 0.3f && Shapes.Hash(c.x, c.y, st) > 0.7f) continue;
                    tl.Put(s + st, c.x, c.y, age < 0.25f ? '*' : Spark(e, c.x, c.y, st, 1), Pal(e, age * 0.9f), age < 0.5f ? Glow(e, age) : (Rgb?)null);
                }
            return end;
        }

        /// <summary>A burst that spreads ring by ring (Chebyshev, so it matches the cells that are hit), then lingers as embers.</summary>
        public static int Burst(FxTimeline tl, int s, int cx, int cy, int r, Elem e, bool fill = true)
        {
            for (int k = 0; k <= r; k++)
                for (int y = cy - k; y <= cy + k; y++)
                    for (int x = cx - k; x <= cx + k; x++)
                    {
                        int d = Math.Max(Math.Abs(x - cx), Math.Abs(y - cy));
                        if (d != k) continue;
                        bool corner = Math.Abs(x - cx) == k && Math.Abs(y - cy) == k && k > 1;
                        char g = k == 0 ? '*' : corner ? 'o' : k == r ? 'O' : '*';
                        for (int st = k; st < r + 4; st++)
                        {
                            float age = (st - k) / 4f;
                            if (!fill && st > k + 1) break;
                            if (age > 0.25f && Shapes.Hash(x, y, st) > 0.65f) continue;
                            char gg = age < 0.25f ? g : Spark(e, x, y, st, 1);
                            tl.Put(s + st, x, y, gg, Pal(e, Math.Min(0.95f, 0.05f + age * 0.9f)), age < 0.5f ? Glow(e, age) : (Rgb?)null);
                        }
                    }
            return s + r + 4;
        }

        /// <summary>A ring that sweeps outward and thins away; nothing lingers in the middle.</summary>
        public static int Nova(FxTimeline tl, int s, int cx, int cy, int r, Elem e)
        {
            for (int k = 1; k <= r; k++)
                for (int y = cy - k; y <= cy + k; y++)
                    for (int x = cx - k; x <= cx + k; x++)
                    {
                        if (Math.Max(Math.Abs(x - cx), Math.Abs(y - cy)) != k) continue;
                        tl.Put(s + k - 1, x, y, k == r ? 'O' : 'o', Pal(e, 0.05f), Glow(e, 0f));
                        tl.Put(s + k, x, y, '·', Pal(e, 0.5f));
                    }
            return s + r + 2;
        }

        /// <summary>A ring that draws in toward a cell: gathering, binding, an implosion before a vanishing.</summary>
        public static int Implode(FxTimeline tl, int s, int cx, int cy, int r, Elem e)
        {
            for (int k = r; k >= 1; k--)
                for (int y = cy - k; y <= cy + k; y++)
                    for (int x = cx - k; x <= cx + k; x++)
                    {
                        if (Math.Max(Math.Abs(x - cx), Math.Abs(y - cy)) != k) continue;
                        tl.Put(s + (r - k), x, y, k == 1 ? '*' : 'o', Pal(e, (r - k) * 0.15f), Glow(e, 0f));
                    }
            tl.Put(s + r, cx, cy, '*', Pal(e, 0f), Glow(e, 0f));
            tl.Put(s + r + 1, cx, cy, '·', Pal(e, 0.6f));
            return s + r + 2;
        }

        /// <summary>A drifting cloud of shade glyphs (gas, mist, smoke).</summary>
        public static int Cloud(FxTimeline tl, int s, int cx, int cy, int r, Elem e, int steps = 6)
        {
            for (int st = 0; st < steps; st++)
                for (int y = cy - r; y <= cy + r; y++)
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        int dx = x - cx, dy = y - cy;
                        if (dx * dx + dy * dy > r * r + 1) continue;
                        float h = Shapes.Hash(x, y, st);
                        if (h > 0.7f) continue;
                        float fade = st / (float)steps;
                        tl.Put(s + st, x, y, h < 0.3f ? '▒' : '░', Pal(e, 0.35f + fade * 0.5f), Glow(e, fade));
                    }
            return s + steps;
        }

        /// <summary>Things falling from above over an area (hail, rain of arrows, acid, ash).</summary>
        public static int Rain(FxTimeline tl, int s, int cx, int cy, int r, Elem e, char drop = '\'', int steps = 6)
        {
            for (int st = 0; st < steps; st++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    float h = Shapes.Hash(x, cy, 5);
                    if (h > 0.75f) continue;
                    int lag = (int)(h * 4);
                    int row = cy - r - 2 + (st - lag) * 2;
                    int landRow = cy + (int)(Shapes.Hash(x, cy, 9) * (2 * r + 1)) - r;
                    if (st - lag < 0) continue;
                    if (row < landRow) tl.Put(s + st, x, row, drop, Pal(e, 0.1f + h * 0.3f));
                    else if (row < landRow + 2) tl.Put(s + st, x, landRow, '*', Pal(e, 0.15f), Glow(e, 0f));
                }
            return s + steps + 1;
        }

        /// <summary>Spikes or pillars thrusting up out of the floor, spreading outward from a cell.</summary>
        public static int Eruption(FxTimeline tl, int s, int cx, int cy, int r, Elem e, char spike = '^')
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    int d = Math.Max(Math.Abs(x - cx), Math.Abs(y - cy));
                    if (d > 0 && Shapes.Hash(x, y, 2) > 0.8f) continue;
                    int at = s + d;
                    tl.Put(at, x, y, '*', Pal(e, 0.1f), Glow(e, 0f));
                    tl.Put(at + 1, x, y, spike, Pal(e, 0.2f), Glow(e, 0.2f));
                    tl.Put(at + 2, x, y, spike, Pal(e, 0.35f));
                    tl.Put(at + 3, x, y, '·', Pal(e, 0.7f));
                }
            return s + r + 4;
        }

        /// <summary>Spokes flying out of a cell: shards, sparks, a shattering.</summary>
        public static int Shatter(FxTimeline tl, int s, int cx, int cy, Elem e, int reach = 2)
        {
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 }, dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
            tl.Put(s, cx, cy, '*', Pal(e, 0f), Glow(e, 0f));
            for (int k = 1; k <= reach; k++)
                for (int i = 0; i < 8; i++)
                {
                    tl.Put(s + k, cx + dx[i] * k, cy + dy[i] * k, k == reach ? '·' : (i % 2 == 0 ? '+' : 'x'), Pal(e, 0.1f + k * 0.25f));
                    if (k < reach) tl.Put(s + k + 1, cx + dx[i] * k, cy + dy[i] * k, '·', Pal(e, 0.7f));
                }
            return s + reach + 2;
        }

        // ------------------------------------------------------------ on a cell

        /// <summary>A quick pop on one cell.</summary>
        public static int Flash(FxTimeline tl, int s, int x, int y, Elem e)
        {
            tl.Put(s, x, y, '*', Pal(e, 0f), Glow(e, 0f));
            tl.Put(s + 1, x, y, 'O', Pal(e, 0.25f), Glow(e, 0.2f));
            tl.Put(s + 2, x, y, 'o', Pal(e, 0.5f));
            tl.Put(s + 3, x, y, '·', Pal(e, 0.8f));
            return s + 4;
        }

        /// <summary>A cross of cuts: blades, claws, talons.</summary>
        public static int Slash(FxTimeline tl, int s, int x, int y, Elem e)
        {
            tl.Put(s, x, y, '\\', Pal(e, 0f), Glow(e, 0f));
            tl.Put(s + 1, x, y, '/', Pal(e, 0.1f), Glow(e, 0f));
            tl.Put(s + 2, x, y, 'X', Pal(e, 0.35f));
            tl.Put(s + 3, x, y, '·', Pal(e, 0.7f));
            return s + 4;
        }

        /// <summary>Motes rising from a cell (healing, blessing, a soul leaving).</summary>
        public static int Rise(FxTimeline tl, int s, int x, int y, Elem e, int height = 3, char mote = '+')
        {
            int total = height + 3;
            for (int st = 0; st < total; st++)
                for (int i = 0; i < 3; i++)
                {
                    int rise = st - i;
                    if (rise < 0 || rise > height) continue;
                    int ox = i - 1;
                    tl.Put(s + st, x + ox, y - rise, i == 1 ? mote : '\'', Pal(e, rise / (float)(height + 1)));
                }
            return s + total;
        }

        /// <summary>Sparks orbiting a cell: a ward, an enchantment, a held breath.</summary>
        public static int Swirl(FxTimeline tl, int s, int cx, int cy, Elem e, int steps = 6)
        {
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 }, dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
            for (int st = 0; st < steps; st++)
                for (int a = 0; a < 3; a++)
                {
                    int k = (st * 2 + a * 3) % 8;
                    int t = (k + 7) % 8;
                    tl.Put(s + st, cx + dx[k], cy + dy[k], '*', Pal(e, 0.05f), Glow(e, 0f));
                    tl.Put(s + st, cx + dx[t], cy + dy[t], '·', Pal(e, 0.5f));
                }
            return s + steps;
        }

        /// <summary>A column of light dropped from above onto a cell (judgement, smiting, a summons from on high).</summary>
        public static int Pillar(FxTimeline tl, int s, int x, int y, Elem e, int height = 6)
        {
            for (int st = 0; st < 3; st++)
            {
                int upto = (int)Math.Ceiling(height * (st + 1) / 3.0);
                for (int k = 0; k <= upto; k++) tl.Put(s + st, x, y - k, k == 0 ? '*' : '│', Pal(e, 0.05f + k * 0.04f), Glow(e, 0f));
            }
            for (int st = 3; st < 6; st++)
                for (int k = 0; k <= height; k++)
                    tl.Put(s + st, x, y - k, st < 5 ? (k == 0 ? 'O' : '│') : '·', Pal(e, 0.3f + (st - 3) * 0.2f), st < 4 ? Glow(e, 0.3f) : (Rgb?)null);
            return s + 6;
        }

        /// <summary>A rock, comet or star dropped from far above and landing in a burst.</summary>
        public static int Meteor(FxTimeline tl, int s, int tx, int ty, int r, Elem e)
        {
            int land = Bolt(tl, s, tx + 6, ty - 6, tx, ty, e, '@', 2);
            return Burst(tl, land, tx, ty, r, e);
        }

        /// <summary>A symbol blinking over a cell: a curse, a mark, a charm.</summary>
        public static int Mark(FxTimeline tl, int s, int x, int y, Elem e, char glyph = '?')
        {
            for (int st = 0; st < 6; st++)
                if (st % 2 == 0) tl.Put(s + st, x, y, glyph, Pal(e, 0.05f), Glow(e, 0f));
                else tl.Put(s + st, x, y, glyph, Pal(e, 0.4f));
            return s + 6;
        }

        /// <summary>Gather at one cell, vanish, and bloom at another.</summary>
        public static int Teleport(FxTimeline tl, int s, int x0, int y0, int x1, int y1, Elem e)
        {
            int mid = Implode(tl, s, x0, y0, 2, e);
            return Burst(tl, mid, x1, y1, 2, e, false);
        }

        /// <summary>Something appears out of nothing: rising motes and a flash.</summary>
        public static int Summon(FxTimeline tl, int s, int x, int y, Elem e)
        {
            int a = Swirl(tl, s, x, y, e, 3);
            Rise(tl, s + 1, x, y, e, 2, '*');
            return Flash(tl, a, x, y, e);
        }
    }
}
