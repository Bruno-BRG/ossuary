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
    /// from both the simulation and Unity: the renderer consumes these strings and
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
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _chars[x, y] = ' ';
                    _colors[x, y] = Rgb.FromHex(0xB8B0A0);
                    _bg[x, y] = bg ?? Rgb.FromHex(0x0A0A0C);
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
            for (int i = 0; i < s.Length; i++) Put(x + i, y, s[i], fg, bold, bg);
        }

        /// <summary>Writes clipped to a maximum width, so long messages never wrap unexpectedly.</summary>
        public int WriteClipped(int x, int y, string s, Rgb fg, int maxWidth, bool bold = false, Rgb? bg = null)
        {
            if (s == null) return x;
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

        /// <summary>Single-line box drawn with box-drawing glyphs. Used for every panel frame.</summary>
        public void Box(int x, int y, int w, int h, Rgb fg, bool bold = false, Rgb? bg = null)
        {
            if (w < 2 || h < 2) return;
            Put(x, y, '╭', fg, bold, bg);
            Put(x + w - 1, y, '╮', fg, bold, bg);
            Put(x, y + h - 1, '╰', fg, bold, bg);
            Put(x + w - 1, y + h - 1, '╯', fg, bold, bg);
            for (int i = 1; i < w - 1; i++)
            {
                Put(x + i, y, '─', fg, bold, bg);
                Put(x + i, y + h - 1, '─', fg, bold, bg);
            }
            for (int j = 1; j < h - 1; j++)
            {
                Put(x, y + j, '│', fg, bold, bg);
                Put(x + w - 1, y + j, '│', fg, bold, bg);
            }
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