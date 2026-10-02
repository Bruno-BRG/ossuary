using System;

namespace Ossuary.Core
{
    /// <summary>
    /// The daily challenge: one seed and one fixed hero per calendar day (UTC), so every player faces the same
    /// dungeon with the same character and the scores compare. Pure functions of the date; the host supplies the clock.
    /// </summary>
    public static class Daily
    {
        public static string Label(int year, int month, int day) => $"{year:0000}-{month:00}-{day:00}";

        /// <summary>FNV-1a over the date label: stable across machines and runs.</summary>
        public static ulong SeedFor(string label)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in "ossuary-daily-" + label) { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        public static void HeroFor(ulong seed, out string race, out string role)
        {
            race = Entities.Races.All[(int)((seed >> 7) % (ulong)Entities.Races.All.Length)].Id;
            role = Entities.Roles.All[(int)((seed >> 19) % (ulong)Entities.Roles.All.Length)].Id;
        }
    }
}
