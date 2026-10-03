using System.Collections.Generic;

namespace Ossuary.Core
{
    public sealed partial class Game
    {
        /// <summary>What the hero has been told about the world (newest last). Truth is kept for the epilogue, not shown in play.</summary>
        public readonly List<Rumour> LearnedRumours = new List<Rumour>();

        public void LearnRumour(Rumour r)
        {
            foreach (var have in LearnedRumours) if (have.Text == r.Text) return;
            LearnedRumours.Add(r);
        }

        /// <summary>
        /// Asking for news: every third answer is a fact about the world (a boss, a place) told through the teller's temperament,
        /// the rest is the town's gossip.
        /// </summary>
        public string HearRumour()
        {
            int n = _talkCount++ + (Talking?.Voice ?? 0);
            if (n % 3 == 2)
            {
                var r = Rumours.Tell(this, Talking, n);
                if (r != null) return r.Text;
            }
            return TownText.Rumor(n);
        }
    }
}
