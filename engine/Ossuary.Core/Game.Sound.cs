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
        static readonly string[] CuePriority = { "death", "levelup", "kill", "hurt", "hit", "quest", "magic", "warn", "good", "stairs", "door", "pickup", "rest" };

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

        /// <summary>
        /// What the music should be about right now, as a name the front end maps to a track: "boss-gaoler", "combat", "tavern", "temple",
        /// "shop", "town", "town-night", "road", "road-night", or the branch ("dungeon", "mines", "warrens", "sunken", "spire", "annex").
        /// Empty when there is nothing to say (the hero is dead or the run is won).
        /// </summary>
        public string MusicScene()
        {
            if (Mode == GameMode.GameOver || Mode == GameMode.Won) return "";
            bool night = World != null && World.IsNight;
            switch (Mode)
            {
                case GameMode.Overworld: return night ? "road-night" : "road";
                case GameMode.TownMap:
                    if (UiState.Active == Panel.Shop) return "shop";
                    if (UiState.Active == Panel.Altar) return "temple";
                    var inside = InsideBuilding();
                    if (inside != null && (inside.Kind == BuildingKind.Tavern || inside.Kind == BuildingKind.Inn)) return "tavern";
                    if (inside != null && inside.Kind == BuildingKind.Temple) return "temple";
                    return night ? "town-night" : "town";
            }
            if (Map != null)
            {
                for (int i = 0; i < Monsters.Count; i++)
                {
                    var m = Monsters[i];
                    if (m.BossId != null && m.HP > 0 && !m.Dormant && Map.IsCurrentlyVisible(m.X, m.Y)) return "boss-" + m.BossId;
                }
                if (UiState.Active == Panel.Altar) return "temple";
                if (HostileInView()) return "combat";
            }
            switch (Branch)
            {
                case "The Mines of Dwarfdeep": return "mines";
                case "The Warrens": return "warrens";
                case "The Sunken Vaults": return "sunken";
                case "The Ashen Spire": return "spire";
                case "The Annex": return "annex";
                default: return "dungeon";
            }
        }

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
