using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// Sound cues, as data. The Core never plays anything: it names what just happened ("hit", "kill", "hurt"...) and the host
    /// hands the names to the front end with the next frame, which turns them into short square-wave bleeps. Cues come from the
    /// kind of message the game just said, so every event that is already reported is also heard, with no call sites to maintain.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Loudest first: when several cues pile up in one frame only the strongest few are played.</summary>
        static readonly string[] CuePriority = { "death", "levelup", "kill", "hurt", "hit", "quest", "magic", "warn", "good", "stairs", "door", "pickup" };

        readonly List<string> _cues = new List<string>();

        void CueFor(MessageKind kind)
        {
            switch (kind)
            {
                case MessageKind.Death: Cue("death"); break;
                case MessageKind.Kill: Cue("kill"); break;
                case MessageKind.Bad: Cue("hurt"); break;
                case MessageKind.Combat: Cue("hit"); break;
                case MessageKind.Quest: Cue("quest"); break;
                case MessageKind.Warn: Cue("warn"); break;
                case MessageKind.Good: Cue("good"); break;
            }
        }

        public void Cue(string id) { if (!_cues.Contains(id)) _cues.Add(id); }

        /// <summary>The cues since the last call, strongest first, at most <paramref name="max"/>; the rest are dropped.</summary>
        public string[] DrainCues(int max = 3)
        {
            var result = new List<string>();
            foreach (string id in CuePriority)
                if (_cues.Contains(id) && result.Count < max) result.Add(id);
            _cues.Clear();
            return result.ToArray();
        }
    }
}
