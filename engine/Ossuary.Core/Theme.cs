using System;
using System.Collections.Generic;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Every colour the game draws with, in one place ("Fósforo &amp; Osso", docs/visual.md).
    ///
    /// The palette is authored once, in the Ossuary preset (indigo darks, bone text,
    /// DawnBringer-derived accents). The other presets are *remaps* of it: Amber and
    /// Phosphor collapse every colour to luminance on a one-colour ramp, CGA snaps to the
    /// sixteen PC colours. So adding a preset never means re-authoring the tile table,
    /// and every preset is guaranteed to cover the same set of cells.
    ///
    /// Two kinds of colour come out of here:
    ///  * UI colours (public fields): already remapped, ready for TextBuilder.
    ///  * Map colours (TileColors, Terrain, ItemRaw, Mon): *raw* Ossuary-space colours.
    ///    Run them through <see cref="Shade"/> / <see cref="Terrain"/> so lighting is
    ///    applied before the remap; dimming after a CGA snap would leave the sixteen
    ///    colours.
    /// </summary>
    public sealed class Theme
    {
        // ----------------------------------------------------------------- current

        static Theme _current = new Theme(ThemePreset.Ossuary);

        /// <summary>The active theme. Swapped by <see cref="Use"/>; read it fresh each frame.</summary>
        public static Theme Current => _current;

        /// <summary>Kept for existing call sites: the active theme.</summary>
        public static Theme Default => _current;

        public static void Use(ThemePreset p) { _current = new Theme(p); }

        public readonly ThemePreset Preset;

        // --------------------------------------------------------------- UI colours

        public Rgb Void, Background, BackgroundDim, Panel, PanelHi, Rule, Frame;
        public Rgb Text, Dim, Label, Title, Accent;
        public Rgb Good, Bad, Warn, Danger, Gold, Magic, Info, Quest, Narrative, Combat, Kill, Death;
        public Rgb BarEmpty, Cursor;

        // ------------------------------------------------------------- raw palette

        static Rgb H(int hex) => Rgb.FromHex(hex);

        // Raw (Ossuary-space) anchors the lighting code needs.
        static readonly Rgb RawBackground = H(0x0D0B14);
        static readonly Rgb RawMemoryFloor = H(0x24223A);
        static readonly Rgb RawMemoryHigh = H(0x7478A8);
        static readonly Rgb RawMemoryWall = H(0x17142A);
        static readonly Rgb RawTorch = H(0xFFD9A0);
        // The warm pool the torch paints on whatever it lights (floor and wall backgrounds).
        static readonly Rgb RawTorchGlow = H(0x6A4220);

        readonly Dictionary<TileKind, KeyValuePair<Rgb, Rgb>> _tile = new Dictionary<TileKind, KeyValuePair<Rgb, Rgb>>();

        // Monochrome ramp (Amber / Phosphor): lo at zero luminance, top at full.
        readonly Rgb _lo, _top;

        static readonly int[] CgaHex =
        {
            0x000000, 0x0000AA, 0x00AA00, 0x00AAAA, 0xAA0000, 0xAA00AA, 0xAA5500, 0xAAAAAA,
            0x555555, 0x5555FF, 0x55FF55, 0x55FFFF, 0xFF5555, 0xFF55FF, 0xFFFF55, 0xFFFFFF,
        };
        static readonly Rgb[] Cga = BuildCga();
        static Rgb[] BuildCga() { var a = new Rgb[16]; for (int i = 0; i < 16; i++) a[i] = Rgb.FromHex(CgaHex[i]); return a; }

        public Theme(ThemePreset preset)
        {
            Preset = preset;
            if (preset == ThemePreset.Amber) { _lo = H(0x0C0700); _top = H(0xFFB000); }
            else if (preset == ThemePreset.Phosphor) { _lo = H(0x020B04); _top = H(0x33FF66); }

            Void = B(0x07060B);
            Background = B(0x0D0B14);
            BackgroundDim = B(0x0A0911);
            Panel = B(0x16121F);
            // CGA has no dark greys, so the raised surface (status bar, selected row) is the
            // classic PC blue; every other preset derives it from the palette.
            PanelHi = preset == ThemePreset.Cga ? Cga[1] : B(0x211B30);
            Rule = F(0x3A3150);
            Frame = F(0x8A6F30);
            // Bone snaps to light grey in CGA, which is too dim on the blue raised surface.
            Text = preset == ThemePreset.Cga ? Cga[15] : F(0xD8CFC0);
            Dim = F(0x7D7590);
            Label = F(0x5FCDE4);
            Title = F(0xF2B33D);
            Accent = F(0xFBF236);
            Good = F(0x99E550);
            Bad = F(0xD95763);
            Warn = F(0xDF7126);
            Danger = F(0xFF3B4F);
            Gold = F(0xFBD94A);
            Magic = F(0xB57EDC);
            Info = F(0x639BFF);
            Quest = F(0x5FE4C0);
            Narrative = F(0xCBB3E8);
            Combat = F(0xE0A080);
            Kill = F(0xC0E070);
            Death = F(0xFF3B4F);
            BarEmpty = F(0x3A3150);
            Cursor = F(0xFF9060);

            void T(TileKind k, int fg, int bg) { _tile[k] = new KeyValuePair<Rgb, Rgb>(H(fg), H(bg)); }
            const int Bg = 0x0D0B14;
            T(TileKind.Void, 0x07060B, 0x07060B);
            T(TileKind.Floor, 0x756C94, Bg);
            T(TileKind.FloorAlt, 0x8A6C50, 0x120F18);
            T(TileKind.Wall, 0x9A90AE, 0x2A2438);
            T(TileKind.WallAlt, 0xB0705A, 0x341C1C);
            T(TileKind.WallDark, 0x6A6880, 0x1E1B2A);
            T(TileKind.Pillar, 0xB8AED0, Bg);
            T(TileKind.ClosedDoor, 0xDF7126, 0x2E1A10);
            T(TileKind.OpenDoor, 0xB05A1C, 0x2E1A10);
            T(TileKind.LockedDoor, 0xFBD94A, 0x2E1A10);
            // A secret door must be indistinguishable from the wall it hides in.
            T(TileKind.HiddenDoor, 0x9A90AE, 0x2A2438);
            T(TileKind.StairsDown, 0xFFFFFF, 0x3F3F74);
            T(TileKind.StairsUp, 0xF2E6C8, 0x3F3F74);
            T(TileKind.LadderDown, 0xFFFFFF, 0x3F3F74);
            T(TileKind.Portal, 0xD77BBA, 0x2A1238);
            T(TileKind.Rubble, 0x8F7A5E, Bg);
            T(TileKind.Altar, 0xEAE4F4, 0x2A2438);
            T(TileKind.Fountain, 0x5FCDE4, 0x0E2A40);
            T(TileKind.Counter, 0xD9A55A, 0x3A2412);
            T(TileKind.Bed, 0xD88080, 0x2A1620);
            T(TileKind.Table, 0xC49A62, 0x22160E);
            T(TileKind.Barrel, 0xB08850, 0x22160E);
            T(TileKind.Shelf, 0x9FB0D8, 0x1E1B2E);
            T(TileKind.Forge, 0xFF8A2A, 0x4A1A08);
            T(TileKind.Tree, 0x58B04A, 0x10240E);
            T(TileKind.Board, 0xF2D77A, 0x2E1A10);
            T(TileKind.Grave, 0x8F8AA8, 0x14121E);
            T(TileKind.Hearth, 0xFF9A3A, 0x3A1408);
        }

        // ------------------------------------------------------------------ remap

        Rgb F(int hex) => Remap(H(hex), false);
        Rgb B(int hex) => Remap(H(hex), true);

        static float Lum(Rgb c) => (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;

        /// <summary>Maps a raw Ossuary-space colour into this preset.</summary>
        public Rgb Remap(Rgb c, bool background)
        {
            switch (Preset)
            {
                case ThemePreset.Amber:
                case ThemePreset.Phosphor:
                {
                    float l = Lum(c);
                    float t = background ? Math.Min(1f, l * 1.1f) * 0.4f
                                         : (float)Math.Pow(Math.Min(1f, l * 1.35f), 0.9);
                    return Rgb.Lerp(_lo, _top, t);
                }
                case ThemePreset.Cga:
                    return background ? CgaBackground(c) : CgaNearest(c, 0, 16);
                default:
                    return c;
            }
        }

        static Rgb CgaNearest(Rgb c, int from, int to)
        {
            int best = from, bd = int.MaxValue;
            for (int i = from; i < to; i++)
            {
                int dr = c.R - Cga[i].R, dg = c.G - Cga[i].G, db = c.B - Cga[i].B;
                int d = dr * dr + dg * dg + db * db;
                if (d < bd) { bd = d; best = i; }
            }
            return Cga[best];
        }

        // Backgrounds may only be the dark half, and near-black stays black: a lit
        // grey background would make the grey wall glyphs on it unreadable.
        static Rgb CgaBackground(Rgb c)
        {
            if (Lum(c) < 0.125f) return Cga[0];
            Rgb pick = CgaNearest(c, 1, 7);
            return pick;
        }

        // ------------------------------------------------------------ map: dungeon

        /// <summary>Raw foreground and background of a tile, before lighting.</summary>
        public void TileColors(TileKind k, out Rgb fg, out Rgb bg)
        {
            if (_tile.TryGetValue(k, out var p)) { fg = p.Key; bg = p.Value; }
            else { fg = H(0xD8CFC0); bg = RawBackground; }
        }

        /// <summary>Glyph and raw colours of a floor surface. Fire flickers with hash(x, y, turn), never with the simulation's RNG.</summary>
        public void SurfaceStyle(SurfaceKind k, int x, int y, int turn, out char glyph, out Rgb fg, out Rgb bg)
        {
            switch (k)
            {
                case SurfaceKind.Water: glyph = '≈'; fg = H(0x4F8FE0); bg = H(0x0E1E3A); break;
                case SurfaceKind.Ice: glyph = '≡'; fg = H(0xBFE6FF); bg = H(0x1A3048); break;
                case SurfaceKind.Fire:
                    {
                        bool flick = Hash01(x + turn * 3, y + turn) > 0.5f;
                        glyph = flick ? '▲' : '^'; fg = H(flick ? 0xFFB030 : 0xFF6020); bg = H(0x5A1408); break;
                    }
                case SurfaceKind.Oil: glyph = ','; fg = H(0xB08840); bg = H(0x1E160A); break;
                default: glyph = '"'; fg = H(0x58B04A); bg = H(0x10240E); break;
            }
        }

        /// <summary>Raw colour of an item glyph.</summary>
        public Rgb ItemRaw(Item it)
        {
            if (it.Rarity == Rarity.Artifact) return H(0xFF8A3D);
            if (it.Rarity == Rarity.Rare) return H(0xFFD24A);
            if (it.Rarity == Rarity.Magic) return H(0x6FA8FF);
            switch (it.Def.Kind)
            {
                case ItemKind.Weapon: return H(0xD0C0A0);
                case ItemKind.Armor: return H(0xA0B0C0);
                case ItemKind.Shield: return H(0xA0B0C0);
                case ItemKind.Helm: case ItemKind.Gloves: case ItemKind.Boots: case ItemKind.Cloak: return H(0xA0B0C0);
                case ItemKind.Ring: return H(0xFBF236);
                case ItemKind.Amulet: return H(0xFBF236);
                case ItemKind.Wand: return H(0xB57EDC);
                case ItemKind.Scroll: return H(0xF0F0E0);
                case ItemKind.Potion: return H(0x5FE4C0);
                case ItemKind.Food: return H(0xC0C080);
                case ItemKind.Gold: return H(0xFBD94A);
                case ItemKind.Gem: return H(0x5FCDE4);
                case ItemKind.Tool: return H(0xB0B0C0);
                case ItemKind.Book: return H(0xD0C090);
                case ItemKind.Ornament: return H(0xD77BBA);
                default: return H(0xD8CFC0);
            }
        }

        /// <summary>Item colour for UI lists (already remapped).</summary>
        public Rgb ItemColor(Item it) => Remap(ItemRaw(it), false);

        /// <summary>Raw colour of a monster definition (content data, hex).</summary>
        public static Rgb Mon(int hex) => H(hex);

        /// <summary>
        /// Torch lighting and memory, then the preset remap. <paramref name="light"/> runs
        /// from 0 (edge of the radius) to 1 (the player's own cell). A remembered cell is
        /// drawn as a blue monochrome ghost, whatever colour it was: it reads as memory,
        /// not as "the same room but dark".
        /// </summary>
        public void Shade(Rgb fg, Rgb bg, float light, bool remembered, out Rgb ofg, out Rgb obg)
        {
            if (remembered)
            {
                float l = Lum(fg);
                fg = Rgb.Lerp(RawMemoryFloor, RawMemoryHigh, Math.Min(1f, l * 1.3f));
                bg = bg.Equals(RawBackground) ? RawBackground : RawMemoryWall;
            }
            else
            {
                light = Math.Max(0f, Math.Min(1f, light));
                float pool = light * light;
                // Glyphs brighten towards the torch and warm up; backgrounds gain a warm
                // pool, so the lit room reads as a place and not as dots on black.
                fg = Rgb.Lerp(fg * (0.55f + 0.60f * light), RawTorch, 0.30f * pool);
                bg = Rgb.Lerp(bg * (0.60f + 0.40f * light), RawTorchGlow, 0.42f * pool);
            }
            ofg = Remap(fg, false);
            obg = Remap(bg, true);
        }

        /// <summary>
        /// Mood of a dungeon depth: crypt, catacombs, flooded vaults, bone pits, hellmouth.
        /// A raw colour pair is pushed towards it before lighting, so every level has its
        /// own palette while the tile table stays authored once.
        /// </summary>
        public static void DepthTint(int depth, out Rgb fgTint, out Rgb bgTint, out float amount)
        {
            int band = Math.Max(0, Math.Min(4, (depth - 1) / 3));
            switch (band)
            {
                case 1: fgTint = H(0x58C070); bgTint = H(0x0C2214); amount = 0.22f; break;
                case 2: fgTint = H(0x4A90E8); bgTint = H(0x0A1A34); amount = 0.22f; break;
                case 3: fgTint = H(0xE0B070); bgTint = H(0x2A1C0C); amount = 0.20f; break;
                case 4: fgTint = H(0xFF5A40); bgTint = H(0x32090B); amount = 0.28f; break;
                default: fgTint = H(0x8C86C8); bgTint = H(0x14102A); amount = 0.10f; break;
            }
        }

        /// <summary>Name of the mood band, shown beside the depth in the header.</summary>
        public static string DepthMood(int depth)
        {
            switch (Math.Max(0, Math.Min(4, (depth - 1) / 3)))
            {
                case 1: return "catacombs";
                case 2: return "flooded vaults";
                case 3: return "bone pits";
                case 4: return "hellmouth";
                default: return "crypt";
            }
        }

        // ---------------------------------------------------------- map: overworld

        struct Ter
        {
            public string Glyphs; public int Fg, Bg;
            public Ter(string g, int fg, int bg) { Glyphs = g; Fg = fg; Bg = bg; }
        }

        static Ter TerrainTable(OverworldTerrain t)
        {
            switch (t)
            {
                case OverworldTerrain.DeepWater: return new Ter("≈", 0x306082, 0x0A1830);
                case OverworldTerrain.Water: return new Ter("≈~", 0x5B6EE1, 0x10224A);
                case OverworldTerrain.Shallow: return new Ter("~", 0x639BFF, 0x16305A);
                case OverworldTerrain.Sand: return new Ter("·.░·", 0xD9A066, 0x2A2014);
                case OverworldTerrain.Grass: return new Ter("\"',·\"", 0x6ABE30, 0x0F1A0C);
                case OverworldTerrain.Forest: return new Ter("♣♠♣", 0x37946E, 0x0A1610);
                case OverworldTerrain.Hills: return new Ter("∩∩ⁿ", 0x8F974A, 0x16180C);
                case OverworldTerrain.Mountain: return new Ter("▲", 0x9BADB7, 0x1E2028);
                case OverworldTerrain.Swamp: return new Ter("⌠\",", 0x4B692F, 0x10140A);
                case OverworldTerrain.Snow: return new Ter("·*", 0xEAF2FF, 0x2A3040);
                case OverworldTerrain.Ash: return new Ter("·.", 0x696A6A, 0x141414);
                case OverworldTerrain.Ruins: return new Ter("▒", 0x6A6A72, 0x14131A);
                case OverworldTerrain.Road: return new Ter("·", 0x8A6F30, 0x1A140C);
                default: return new Ter("·", 0x4A4360, 0x0D0B14);
            }
        }

        /// <summary>Primary glyph of a terrain (the first of its variants).</summary>
        public static char TerrainGlyph(OverworldTerrain t) => TerrainTable(t).Glyphs[0];

        /// <summary>Glyph that marks an overworld feature (CP437 vocabulary).</summary>
        public static char FeatureGlyph(OverworldFeature f)
        {
            switch (f)
            {
                case OverworldFeature.Town: return '■';
                case OverworldFeature.Dungeon: return '▼';
                case OverworldFeature.Ruin: return 'π';
                case OverworldFeature.Cave: return 'Ω';
                case OverworldFeature.Mine: return '¥';
                case OverworldFeature.Keep: return 'Π';
                case OverworldFeature.Shrine: return '‡';
                case OverworldFeature.Bridge: return '═';
                case OverworldFeature.Signpost: return '†';
                default: return '·';
            }
        }

        /// <summary>
        /// Glyph, colours and day/night tint of one overworld cell. The variant glyph and a
        /// small luminance jitter come from hash(x, y) alone, so the world never shimmers
        /// between frames and the simulation is untouched.
        /// </summary>
        public void Terrain(OverworldTerrain t, OverworldFeature f, int x, int y, int hour,
                            out char glyph, out Rgb fg, out Rgb bg)
        {
            var ter = TerrainTable(t);
            float h = Hash01(x, y);
            Rgb rfg = H(ter.Fg), rbg = H(ter.Bg);
            glyph = ter.Glyphs[(int)(h * ter.Glyphs.Length) % ter.Glyphs.Length];
            rfg = rfg * (1f + (h - 0.5f) * 0.14f);

            if (f != OverworldFeature.None)
            {
                glyph = FeatureGlyph(f);
                switch (f)
                {
                    case OverworldFeature.Town: rfg = H(0xFBF236); rbg = H(0x3A2A08); break;
                    case OverworldFeature.Dungeon: rfg = H(0xFF3B4F); rbg = H(0x2A0A0E); break;
                    case OverworldFeature.Ruin: rfg = H(0x9A9488); break;
                    case OverworldFeature.Cave: rfg = H(0xB08050); break;
                    case OverworldFeature.Mine: rfg = H(0xC09060); break;
                    case OverworldFeature.Keep: rfg = H(0xC0B0A0); break;
                    case OverworldFeature.Shrine: rfg = H(0x5FE4C0); break;
                    case OverworldFeature.Bridge: rfg = H(0xA0A0B0); rbg = H(0x10224A); break;
                    default: rfg = H(0xD8CFC0); break;
                }
            }

            WorldLight(hour, out Rgb tint, out float amt, out float dim);
            rfg = Rgb.Lerp(rfg * dim, tint, amt);
            rbg = Rgb.Lerp(rbg * dim, tint, amt * 0.35f);
            fg = Remap(rfg, false);
            bg = Remap(rbg, true);
        }

        /// <summary>
        /// Time of day as a colour multiplier: rosy dawn, neutral noon, amber dusk, indigo
        /// night. Night is dimmed but never so far that the world stops being readable.
        /// </summary>
        public static void WorldLight(int hour, out Rgb tint, out float amount, out float dim)
        {
            if (hour >= 7 && hour < 17) { tint = Rgb.White; amount = 0f; dim = 1f; }
            else if (hour >= 5 && hour < 7) { tint = H(0xFFC8B0); amount = 0.10f; dim = 0.9f; }
            else if (hour >= 17 && hour < 20) { tint = H(0xFFB070); amount = 0.14f; dim = 0.92f; }
            else { tint = H(0x2A3A8A); amount = 0.18f; dim = 0.62f; }
        }

        /// <summary>Stable pseudo-random value in [0,1) from a cell coordinate.</summary>
        public static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ 0x5BD1E995u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        // ---------------------------------------------------------------- messages

        public Rgb MessageColor(MessageKind k)
        {
            switch (k)
            {
                case MessageKind.Good: return Good;
                case MessageKind.Bad: return Bad;
                case MessageKind.Combat: return Combat;
                case MessageKind.Kill: return Kill;
                case MessageKind.Info: return Info;
                case MessageKind.Warn: return Warn;
                case MessageKind.Death: return Death;
                case MessageKind.Narrative: return Narrative;
                case MessageKind.Quest: return Quest;
                default: return Text;
            }
        }

        /// <summary>Green, amber or red by how much of a bar is left.</summary>
        public Rgb Fraction(double frac) => frac > 0.6 ? Good : frac > 0.3 ? Warn : Danger;
    }
}
