using System;
using System.Collections.Generic;
using System.Text;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Flattens game state into the text the renderer draws. Deliberately separate
    /// from both the simulation and the rendering framework: the renderer consumes these strings and
    /// never has to know what a GameMap is.
    /// </summary>
    public sealed class TextBuilder
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        char[,] _chars;
        Rgb[,] _colors;
        Rgb[,] _bg;
        bool[,] _bold;

        readonly System.Collections.Generic.List<int> _shimmer = new System.Collections.Generic.List<int>();

        /// <summary>Marks a cell as moving water: the front end shimmers it between frames, without asking the engine for anything.</summary>
        public void Shimmer(int x, int y) { if (x >= 0 && y >= 0 && x < Width && y < Height) _shimmer.Add(y * Width + x); }
        public int[] ShimmerCells() => _shimmer.ToArray();

        public TextBuilder(int w, int h) { Resize(w, h); }

        public void Resize(int w, int h)
        {
            if (_chars == null || w != Width || h != Height)
            {
                Width = w; Height = h;
                _chars = new char[w, h];
                _colors = new Rgb[w, h];
                _bg = new Rgb[w, h];
                _bold = new bool[w, h];
                Clear();
            }
        }

        public void Clear(Rgb? bg = null)
        {
            _shimmer.Clear();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _chars[x, y] = ' ';
                    _colors[x, y] = Theme.Default.Text;
                    _bg[x, y] = bg ?? Theme.Default.Void;
                    _bold[x, y] = false;
                }
            }
        }

        public char CharAt(int x, int y) => (x < 0 || y < 0 || x >= Width || y >= Height) ? ' ' : _chars[x, y];
        public Rgb ColorAt(int x, int y) => (x < 0 || y < 0 || x >= Width || y >= Height) ? Rgb.Black : _colors[x, y];
        public Rgb BgAt(int x, int y) => (x < 0 || y < 0 || x >= Width || y >= Height) ? Rgb.Black : _bg[x, y];
        public bool BoldAt(int x, int y) => (x < 0 || y < 0 || x >= Width || y >= Height) ? false : _bold[x, y];

        public void Put(int x, int y, char c, Rgb fg, bool bold = false, Rgb? bg = null)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            _chars[x, y] = c;
            _colors[x, y] = fg;
            _bold[x, y] = bold;
            if (bg.HasValue) _bg[x, y] = bg.Value;
        }

        public void Write(int x, int y, string s, Rgb fg, bool bold = false, Rgb? bg = null)
        {
            if (s == null) return;
            s = Loc.U(s);
            for (int i = 0; i < s.Length; i++) Put(x + i, y, s[i], fg, bold, bg);
        }

        /// <summary>Writes clipped to a maximum width, so long messages never wrap unexpectedly.</summary>
        public int WriteClipped(int x, int y, string s, Rgb fg, int maxWidth, bool bold = false, Rgb? bg = null)
        {
            if (s == null) return x;
            s = Loc.U(s);
            int n = Math.Min(s.Length, maxWidth);
            for (int i = 0; i < n; i++) Put(x + i, y, s[i], fg, bold, bg);
            return x + n;
        }

        public void FillRect(int x, int y, int w, int h, char c, Rgb fg, Rgb bg, bool bold = false)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    Put(xx, yy, c, fg, bold, bg);
        }

        /// <summary>Single-line box (┌─┐) for regions: map, log. Panels use <see cref="DoubleBox"/>.</summary>
        public void Box(int x, int y, int w, int h, Rgb fg, bool bold = false, Rgb? bg = null)
        {
            Frame(x, y, w, h, fg, bold, bg, "┌┐└┘─│");
        }

        /// <summary>Rounded single-line box (╭─╮) for the map and the journal.</summary>
        public void RoundBox(int x, int y, int w, int h, Rgb fg, bool bold = false, Rgb? bg = null)
        {
            Frame(x, y, w, h, fg, bold, bg, "╭╮╰╯─│");
        }

        /// <summary>Double-line box (╔═╗) for modal panels and the sidebar.</summary>
        public void DoubleBox(int x, int y, int w, int h, Rgb fg, bool bold = false, Rgb? bg = null)
        {
            Frame(x, y, w, h, fg, bold, bg, "╔╗╚╝═║");
        }

        // set = top-left, top-right, bottom-left, bottom-right, horizontal, vertical.
        void Frame(int x, int y, int w, int h, Rgb fg, bool bold, Rgb? bg, string set)
        {
            if (w < 2 || h < 2) return;
            Put(x, y, set[0], fg, bold, bg);
            Put(x + w - 1, y, set[1], fg, bold, bg);
            Put(x, y + h - 1, set[2], fg, bold, bg);
            Put(x + w - 1, y + h - 1, set[3], fg, bold, bg);
            for (int i = 1; i < w - 1; i++)
            {
                Put(x + i, y, set[4], fg, bold, bg);
                Put(x + i, y + h - 1, set[4], fg, bold, bg);
            }
            for (int j = 1; j < h - 1; j++)
            {
                Put(x, y + j, set[5], fg, bold, bg);
                Put(x + w - 1, y + j, set[5], fg, bold, bg);
            }
        }

        /// <summary>
        /// Darkens everything already drawn. Used behind a modal panel so the screen
        /// underneath reads as "paused" and the panel's edge does not slice through
        /// sidebar text. <paramref name="post"/> lets a limited palette (CGA) re-snap.
        /// </summary>
        public void DimAll(float amount, Func<Rgb, bool, Rgb> post = null)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Rgb fg = _colors[x, y].Dim(amount), bg = _bg[x, y].Dim(amount);
                    if (post != null) { fg = post(fg, false); bg = post(bg, true); }
                    _colors[x, y] = fg;
                    _bg[x, y] = bg;
                }
            }
        }

        /// <summary>Darkens one existing cell in place (a drop shadow under a panel).</summary>
        public void Darken(int x, int y, float amount)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            _colors[x, y] = _colors[x, y].Dim(amount);
            _bg[x, y] = _bg[x, y].Dim(amount);
        }

        /// <summary>Horizontal rule, NetHack's classic window divider.</summary>
        public void HLine(int x, int y, int w, Rgb fg, char c = '─')
        {
            for (int i = 0; i < w; i++) Put(x + i, y, c, fg);
        }

        public string Row(int y)
        {
            var sb = new StringBuilder(Width);
            for (int x = 0; x < Width; x++) sb.Append(_chars[x, y]);
            return sb.ToString();
        }

        public string ToAscii()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < Height; y++) { sb.Append(Row(y)); sb.Append('\n'); }
            return sb.ToString();
        }
    }
}