using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ossuary.Render
{
    /// <summary>
    /// Glyph source for the terminal renderer.
    ///
    /// The design deliberately does NOT copy glyph pixels into a private atlas. An
    /// earlier version did, and it was wrong in a way no assertion could see: Unity's
    /// dynamic font atlas is Alpha8, packs into a fixed page, and is rebuilt (as a new
    /// Texture2D, with a new UV layout) whenever it runs out of room. Reading those
    /// pixels back and re-packing them means trusting that the UV rect, the page and
    /// the row order all still agree at the moment of the copy. When they disagree the
    /// atlas fills with solid blocks or with fragments of other letters, and the game
    /// renders text made of the wrong characters — visible instantly in a
    /// screenshot, invisible in a coverage count.
    ///
    /// So the font's own texture is used as the atlas, and CharacterInfo supplies the
    /// UV rect for each glyph. Nothing is copied, so nothing can drift. Two rules keep
    /// the UVs valid:
    ///
    ///   * Every glyph is requested BEFORE any CharacterInfo is read, so the layout is
    ///     settled.
    ///   * The texture reference is captured AFTER that request, and no further request
    ///     is ever made, so no rebuild can invalidate the UVs.
    ///
    /// Cell size still comes from the font's advance and a fixed line-height ratio, so
    /// the grid stays uniform and the renderer can place characters by integer
    /// arithmetic at any window size.
    /// </summary>
    public sealed class GlyphAtlas
    {
        /// <summary>Every codepoint the game is allowed to display.</summary>
        public static readonly char[] Charset = BuildCharset();

        public readonly Font Font;
        public readonly int Size;
        public readonly string FontPath;

        /// <summary>Visible cell size in pixels.</summary>
        public readonly int CellW;
        public readonly int CellH;

        /// <summary>
        /// The font's texture, sampled directly.
        ///
        /// Resolved LAZILY. Font.material.mainTexture is not guaranteed to be available
        /// while the atlas is being built: the font's material and texture are created
        /// lazily too, and during early initialisation (edit-mode tooling, the first
        /// frame of a scene load) the property can still be null even though every
        /// GetCharacterInfo call already succeeds. Capturing that null here produced an
        /// atlas with correct metrics and no texture at all, and the renderer then drew
        /// every glyph as a solid quad — a blank-looking screen that reads as a shader
        /// or coverage bug rather than a missing texture.
        /// </summary>
        Texture _texture;

        public Texture Texture
        {
            get
            {
                if (_texture == null) ResolveTexture();
                return _texture;
            }
        }

        /// <summary>
        /// Identity of the current atlas texture, including its instance id. Used to
        /// detect that a pack operation replaced it, which is the signal that the UV
        /// layout moved and every rect captured so far is stale.
        /// </summary>
        string TextureKey()
        {
            var t = Texture;
            return t == null ? "none" : (t.GetInstanceID() + ":" + t.width + "x" + t.height);
        }

        void ResolveTexture()
        {
            if (Font == null) return;
            try
            {
                if (Font.material != null) _texture = Font.material.mainTexture;
            }
            catch (Exception e) { Debug.LogWarning("[GlyphAtlas] font texture unavailable: " + e.Message); }

            if (_texture == null) return;

            // Point filtering, and this is not a cosmetic choice.
            //
            // Unity packs glyphs one texel apart and GetCharacterInfo's UV rect covers
            // that whole padded box. With bilinear filtering, sampling anywhere inside
            // that box blends in the edge texels of the glyphs packed next door, so
            // every character grows a shadow of its neighbours — the screen fills with
            // correct-looking letters each trailed by fragments of other letters.
            // Nearest-neighbour sampling keeps each quad reading only its own glyph.
            try
            {
                _texture.filterMode = FilterMode.Point;
                _texture.wrapMode = TextureWrapMode.Clamp;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GlyphAtlas] could not set filtering on the font texture: " + e.Message);
            }
        }

        readonly Dictionary<char, Vector4> _uvOf = new Dictionary<char, Vector4>(512);
        readonly Dictionary<char, Vector4> _inkOf = new Dictionary<char, Vector4>(512);

        /// <summary>
        /// Height of the baseline above the cell's bottom edge, in pixels. Published so
        /// the renderer can place a glyph's ink box inside its cell without re-deriving
        /// the same ratio.
        /// </summary>
        public readonly int Baseline;

        /// <summary>
        /// Glyph box inside its cell, as (offsetX, offsetY, width, height) in pixels:
        /// offsetX from the cell's left edge, offsetY from the cell's bottom edge.
        /// The renderer draws the quad at this box rather than the whole advance, which
        /// is what stops a wide glyph such as a box-drawing character from bleeding into
        /// the neighbouring cell.
        /// </summary>
        public Vector4 InkOf(char c)
        {
            Vector4 v;
            return _inkOf.TryGetValue(c, out v) ? v : Vector4.zero;
        }

        public GlyphAtlas(Font font, int size, string fontPath)
        {
            Font = font;
            Size = size;
            FontPath = fontPath;

            // Charset positions, for renderers that want flat lookup tables.
            for (int i = 0; i < Charset.Length; i++) SlotOf[Charset[i]] = i;

            // Request everything first. Only then are the UVs read, so they describe a
            // layout that is already final.
            // Packing is lazy and REPEATED: RequestCharactersInTexture packs what fits, and
            // GetCharacterInfo for a still-unpacked glyph packs it too — each of those
            // can rebuild the atlas into a new texture with a new layout. The safe
            // order is therefore:
            //   1. touch every glyph until the packer stops changing anything
            //   2. capture the texture
            //   3. read the UVs, with nothing left to pack
            // Doing it in that order is what makes the UVs describe the texture that is
            // actually sampled. Reading UVs while packing is still ongoing yields UVs
            // from a layout that has since been thrown away, and each cell then draws a
            // neighbouring glyph's pixels — text made of the wrong letters.
            for (int pass = 0; pass < 4; pass++)
            {
                var before = TextureKey();
                font.RequestCharactersInTexture(new string(Charset), size, FontStyle.Normal);
                for (int i = 0; i < Charset.Length; i++)
                {
                    CharacterInfo warm;
                    try { font.GetCharacterInfo(Charset[i], out warm, size, FontStyle.Normal); }
                    catch { }
                }
                ResolveTexture();
                if (TextureKey() == before && pass > 0) break;   // layout has settled
            }

            CharacterInfo probe;
            if (!font.GetCharacterInfo('M', out probe, size, FontStyle.Normal))
                probe = new CharacterInfo();

            float advance = probe.advance > 0 ? probe.advance : size * 0.6f;

            // Cell size is the max of the advance and the widest glyph's ink, plus the
            // font's own padding. Using the advance alone looked right and was not:
            // 'M' and 'W' overhang their advance by a pixel or two, so their ink spilled
            // into the neighbouring cell and every line came out with letters fused
            // together. Measuring the real extents makes the grid provably wide enough.
            int widest = Mathf.CeilToInt(advance);
            int tallest = 1;
            CharacterInfo fit;
            for (int i = 0; i < Charset.Length; i++)
            {
                try { if (!font.GetCharacterInfo(Charset[i], out fit, size, FontStyle.Normal)) continue; }
                catch { continue; }
                int left = fit.minX;
                int right = fit.minX + fit.glyphWidth;
                if (left < 0) widest = Mathf.Max(widest, -left + fit.glyphWidth);
                else widest = Mathf.Max(widest, fit.glyphWidth);
                if (right > widest) widest = right;
                tallest = Mathf.Max(tallest, fit.minY + fit.glyphHeight);
            }

            CellW = Mathf.Max(1, widest + 1);
            CellH = Mathf.Max(CellW, tallest + Mathf.RoundToInt(size * 0.34f));

            // Baseline height above the cell floor, in pixels. A fixed fraction of the cell
            // rather than a font lookup, so swapping the typeface cannot reflow the UI:
            // a glyph's own minY is measured from the baseline, so this only has to be
            // roughly where the font puts its baseline for the ink to land centred.
            Baseline = Mathf.Clamp(Mathf.RoundToInt(CellH * 0.78f), 1, CellH - 1);

            ResolveTexture();

            for (int i = 0; i < Charset.Length; i++)
            {
                char c = Charset[i];
                CharacterInfo info;
                try { if (!font.GetCharacterInfo(c, out info, size, FontStyle.Normal)) continue; }
                catch { continue; }
                if (info.glyphWidth <= 0 || info.glyphHeight <= 0) continue;

                // The font's UV names are flipped vertically: uvBottomLeft.y sits ABOVE
                // uvTopRight.y (verified on device: sampling the stored rect as-is
                // draws the neighbouring glyph). The stored rect is therefore
                // bottom-normalised — y is the true bottom edge, h positive — so the
                // renderer can keep treating uv.y as the bottom without knowing this.
                _uvOf[c] = new Vector4(
                    info.uvBottomLeft.x,
                    info.uvTopRight.y,
                    info.uvTopRight.x - info.uvBottomLeft.x,
                    info.uvBottomLeft.y - info.uvTopRight.y);

                // Ink box inside the cell, in pixels. minX is the offset from the pen
                // position to the glyph's left edge; minY is the offset from the baseline
                // to its bottom, so the cell-relative bottom is Baseline + minY.
                _inkOf[c] = new Vector4(info.minX, Baseline + info.minY, info.glyphWidth, info.glyphHeight);
            }
        }

        /// <summary>UV rect of a glyph, or a zero rect when the font has no such glyph.</summary>
        public Vector4 UvOf(char c)
        {
            Vector4 v;
            return _uvOf.TryGetValue(c, out v) ? v : Vector4.zero;
        }

        /// <summary>
        /// Index of a codepoint within <see cref="Charset"/>, or -1 when it is not part
        /// of the game's vocabulary. Renderers use this to index a flat UV cache.
        /// It has to be a lookup rather than an arithmetic offset: the charset is
        /// contiguous only across printable ASCII, and jumps to scattered codepoints
        /// for box drawing, arrows and braille.
        /// </summary>
        public int IndexOf(char c)
        {
            int i;
            return SlotOf.TryGetValue(c, out i) ? i : -1;
        }

        /// <summary>Charset position by codepoint, so a renderer can build its own tables.</summary>
        public readonly Dictionary<char, int> SlotOf = new Dictionary<char, int>(512);

        public bool Has(char c) => _uvOf.ContainsKey(c);

        

        static char[] BuildCharset()
        {
            var sb = new StringBuilder(1400);
            for (char c = (char)32; c <= (char)126; c++) sb.Append(c);

            // Shades and blocks: floors, bars, depth.
            sb.Append("░▒▓█▀▄▌▐■□▪▫●○◌◇◆");

            // Box drawing, light, heavy, double, then rounded corners for panels.
            sb.Append("─│┌┐└┘├┤┬┴┼");
            sb.Append("━┃┏┓┗┛┣┫┳┻╋");
            sb.Append("═║╔╗╚╝╠╣╦╩╬");
            sb.Append("╭╮╰╯");

            // Arrows, pointers and marks.
            sb.Append("←↑→↓↔↕⇐⇒⇧▲▼◀▶△▽◁▷«»‹›");
            sb.Append("✚✦✧★☆❖⌂►▶·•…‰§¶†‡");

            // Weather.
            sb.Append("☀☁☂☃❄❅❆≈~^");

            // Braille, for the optional high-resolution shading mode.
            sb.Append("⠀⠁⠂⠃⠄⠅⠆⠇⡀⡁⡂⡃⡄⡅⡆⡇");
            sb.Append("⠈⠉⠊⠋⠌⠍⠎⠏⡈⡉⡊⡋⡌⡍⡎⡏");
            sb.Append("⠐⠑⠒⠓⠔⠕⠖⠗⡐⡑⡒⡓⡔⡕⡖⡗");
            sb.Append("⠘⠙⠚⠛⠜⠝⠞⠟⡘⡙⡚⡛⡜⡝⡞⡟");
            sb.Append("⠠⠡⠢⠣⠤⠥⠦⠧⡠⡡⡢⡣⡤⡥⡦⡧");
            sb.Append("⣀⣁⣂⣃⣄⣅⣆⣇⢀⢁⢂⢃⢄⢰⢆⢇");
            sb.Append("⣠⣡⣢⣣⣤⣥⣦⣧⢠⢡⢢⢣⢤⢥⢦⢧");
            sb.Append("⣰⣱⣲⣳⣴⣵⣶⣷⢰⢱⢲⢳⢴⢵⢶⢷");

            // Currency and punctuation that appear in messages.
            sb.Append("¤¢$€£¥");

            return sb.ToString().ToCharArray();
        }
    }
}