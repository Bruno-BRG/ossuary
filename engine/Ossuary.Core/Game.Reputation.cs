using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>The four houses of the world that remember what you do: the Watch, the Temple, the Guild and the Cult of the Drowned.</summary>
    public static class Houses
    {
        public const string Watch = "watch", Temple = "temple", Guild = "guild", Cult = "cult";
        public static readonly string[] All = { Watch, Temple, Guild, Cult };

        public static string Name(string id)
        {
            switch (id)
            {
                case Watch: return "the Watch";
                case Temple: return "the Temple";
                case Guild: return "the Guild";
                case Cult: return "the Cult of the Drowned";
                default: return id;
            }
        }

        public static string Standing(int rep) =>
            rep >= 60 ? "revered" : rep >= 25 ? "trusted" : rep > -25 ? "known" : rep > -60 ? "distrusted" : "hated";
    }

    /// <summary>
    /// Reputation, -100..100 per house. It is earned by doing what a house values and lost by what it hates, and it changes
    /// what the world charges you: shop prices, temple fees, the inn, and what the Cult will sell in the dark.
    /// </summary>
    public sealed partial class Game
    {
        public int RepOf(string house) => Player.Rep.TryGetValue(house, out int v) ? v : 0;

        /// <summary>Moves a house's opinion of the hero, clamped, and says so when a standing is crossed.</summary>
        public void AddRep(string house, int delta, string reason = null)
        {
            if (delta == 0) return;
            int before = RepOf(house);
            int after = Math.Max(-100, Math.Min(100, before + delta));
            Player.Rep[house] = after;
            string was = Houses.Standing(before), now = Houses.Standing(after);
            if (was != now) Say($"{Houses.Name(house)} now holds you {now}.", delta > 0 ? MessageKind.Good : MessageKind.Warn);
            else if (reason != null && Math.Abs(delta) >= 3) Say($"{Houses.Name(house)} {(delta > 0 ? "thinks the better of" : "thinks the worse of")} you for {reason}.", delta > 0 ? MessageKind.Info : MessageKind.Warn);
        }

        /// <summary>What a house's regard does to a price: its friends pay up to 20% less, its enemies up to 20% more (the clamp allows 25%).</summary>
        public int Haggle(int price, string house, int favourPct = 5)
        {
            int rep = RepOf(house);
            int pct = Math.Max(75, Math.Min(125, 100 - rep / favourPct));
            pct = pct * (100 - EventDiscount()) / 100;
            if (Mode == GameMode.TownMap && World != null)
            {
                // What the hero knows has become public: the Holds and the League resent the ones who say it.
                string region = RegionHere();
                if (Flags.Contains("truth.vote") && region == "The Iron Hills") pct = pct * 110 / 100;
                if (Flags.Contains("truth.order") && region == "The Verdant Reach") pct = pct * 110 / 100;
            }
            return Math.Max(price > 0 ? 1 : 0, price * pct / 100);
        }
    }
}
