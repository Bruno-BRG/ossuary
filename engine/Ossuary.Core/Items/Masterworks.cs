namespace Ossuary.Core.Items
{
    /// <summary>Proper names for named masterworks: two halves picked by a hash of the seed and the piece, so naming draws no RNG.</summary>
    public static class Masterworks
    {
        static readonly string[] Heads =
        {
            "Ash", "Grave", "Bone", "Night", "Ember", "Gloam", "Thorn", "Hollow", "Marrow", "Dusk", "Cinder", "Rime", "Storm", "Mourn",
            "Lantern", "Raven", "Kin", "Oath", "Wick", "Hearth", "Salt", "Vigil", "Barrow", "Candle",
        };

        static readonly string[] Tails =
        {
            "tooth", "song", "ward", "fang", "brand", "hymn", "wake", "keeper", "shard", "bite", "mourn", "light", "fall", "weald",
            "bane", "rest", "warden", "bell", "thorn", "kiss", "glass", "marrow",
        };

        public static string Coin(ulong seed, long uid)
        {
            ulong h = Rumours.Hash(seed ^ 0x3A57E7UL, "masterwork", (int)(uid & 0x7FFFFFFF));
            string head = Heads[(int)(h % (ulong)Heads.Length)];
            int t = (int)((h / 97) % (ulong)Tails.Length);
            if (head.ToLowerInvariant() == Tails[t]) t = (t + 1) % Tails.Length;
            return head + Tails[t];
        }
    }
}
