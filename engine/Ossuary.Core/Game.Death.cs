using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>Why the run ended, kept for the death screen, the morgue file and the run history.</summary>
    public sealed partial class Game
    {
        /// <summary>Who or what last hurt the player ("a giant rat", "starvation") and on which turn.</summary>
        public string LastHurtBy;
        public int LastHurtTurn;
        /// <summary>Set when the run ended by death; null while alive. "Abandoned" runs say so.</summary>
        public string DeathCause;
        public bool Abandoned;

        public void HurtBy(string cause) { LastHurtBy = cause; LastHurtTurn = Turn; }

        public static string Article(Monster m)
        {
            if (m.Unique) return m.Name;
            char c = char.ToLowerInvariant(m.Name.Length > 0 ? m.Name[0] : 'x');
            return ((c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u') ? "an " : "a ") + m.Name;
        }

        string CauseOfDeath()
        {
            if (LastHurtBy != null && Turn - LastHurtTurn <= 2) return LastHurtBy;
            if (Player.Nutrient <= -100) return "starvation";
            return LastHurtBy ?? "unknown causes";
        }
    }
}
