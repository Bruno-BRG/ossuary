namespace Ossuary.Core
{
    /// <summary>How the run is played. Chosen at creation and fixed for the run (saves carry it, replays need it).</summary>
    public enum Difficulty
    {
        /// <summary>The standard game.</summary>
        Normal,
        /// <summary>No hunger: food is a curiosity. For learning the dungeon.</summary>
        Classic,
        /// <summary>One life, one save: the save is erased the moment the run is resumed, so quitting is the only save.</summary>
        Hardcore,
    }

    public static class Difficulties
    {
        public static readonly Difficulty[] All = { Difficulty.Normal, Difficulty.Classic, Difficulty.Hardcore };

        public static string Name(Difficulty d) =>
            d == Difficulty.Classic ? "Classic" : d == Difficulty.Hardcore ? "Hardcore" : "Normal";

        public static string Blurb(Difficulty d) =>
            d == Difficulty.Classic ? "No hunger. Food is only a luxury."
            : d == Difficulty.Hardcore ? "One save: it is erased when you resume. No quicksave."
            : "The standard game.";

        public static Difficulty Parse(string s) =>
            s == "Classic" ? Difficulty.Classic : s == "Hardcore" ? Difficulty.Hardcore : Difficulty.Normal;
    }

    public sealed partial class Game
    {
        /// <summary>Set by the host right after the game is built, before the first key.</summary>
        public Difficulty Difficulty = Difficulty.Normal;
    }
}
