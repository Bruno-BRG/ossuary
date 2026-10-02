using System;
using System.Collections.Generic;

namespace Ossuary.Core.Entities
{
    /// <summary>Damage families. Races, items and (later) monsters speak this language.</summary>
    public enum DamageType { Physical, Fire, Cold, Lightning, Poison, Necrotic, Holy }

    /// <summary>
    /// Skill grades. A skill still rises by use (0..100); the grade is only a name and a
    /// small flat bonus, so nothing new has to be tracked or saved.
    /// </summary>
    public static class SkillRanks
    {
        public static readonly string[] Names = { "Novice", "Trained", "Skilled", "Expert", "Master" };

        /// <summary>0 Novice (0-24), 1 Trained (25-49), 2 Skilled (50-74), 3 Expert (75-99), 4 Master (100).</summary>
        public static int Rank(int value) => value >= 100 ? 4 : value < 0 ? 0 : value / 25;

        public static string Name(int value) => Names[Rank(value)];
    }
}
