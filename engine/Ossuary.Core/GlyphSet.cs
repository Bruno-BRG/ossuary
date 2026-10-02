namespace Ossuary.Core
{
    /// <summary>
    /// Every glyph the game may draw, in one place.
    ///
    /// The renderer's bitmap font (unscii-16, desktop/src/font.ts) covers far more
    /// than this, so the list is no longer a size limit; it is a *contract*. The test
    /// suite asserts that every tile, terrain, feature, item, monster and UI marker is
    /// contained here, and a second test checks this list against the font file itself,
    /// so a glyph missing from the font fails a test instead of rendering as '?'.
    ///
    /// ASCII 32-126 is always in. Add to Extra first, then use the glyph.
    /// </summary>
    public static class GlyphSet
    {
        // Shades and blocks, box drawing (single, double, rounded), arrows, and the
        // CP437-style symbols the map vocabulary uses (see docs/visual.md section 3.2).
        public const string Extra =
            "░▒▓█▀▄▌▐■□▪▫●○◇◆◘◙" +
            "─│┌┐└┘├┤┬┴┼═║╔╗╚╝╠╣╦╩╬╡╞╭╮╰╯" +
            "←↑→↓▲▼◀▶►◄◎◊" +
            "·•…°±≈≡∩∞ⁿ♣♠♥♦♪☺☻♂♀πΩΠ‡†¥Φ⌠⌡≥≤√«»×÷" +
            "◐◑◒◓★☆▁▂▃▄▅▆▇◢◣◤◥⇧";

        public static bool Contains(char c)
        {
            if (c >= 32 && c <= 126) return true;
            return Extra.IndexOf(c) >= 0;
        }
    }
}
