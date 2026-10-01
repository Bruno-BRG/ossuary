namespace Ossuary.Core
{
    /// <summary>
    /// Every glyph the game may draw, in one place.
    ///
    /// The terminal renderer (GlyphAtlas, which lives outside Core because it
    /// needs UnityEngine) builds its texture from exactly this list, and the
    /// test suite asserts that every tile, terrain, feature, item, monster and
    /// UI cursor is contained in it. Anything outside renders as '?'.
    ///
    /// Kept deliberately small (~140 codepoints): Unity's dynamic font atlas
    /// evicts glyphs past a few hundred, which once produced garbage UVs and
    /// the infamous unreadable screen. ASCII 32-126 is always in; Extra holds
    /// the symbols the UI actually uses (bars, single box drawing, arrows and
    /// the ▶▼✚✦ markers). Add here first, then use.
    /// </summary>
    public static class GlyphSet
    {
        public const string Extra = "░▒▓█▀▄▌▐■□▪▫●○◇◆─│┌┐└┘├┤┬┴┼╭╮╰╯←↑→↓▲▼◀▶·•…✚✦";

        public static bool Contains(char c)
        {
            if (c >= 32 && c <= 126) return true;
            return Extra.IndexOf(c) >= 0;
        }
    }
}
