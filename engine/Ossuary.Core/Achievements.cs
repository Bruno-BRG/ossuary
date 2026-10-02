using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    public sealed class AchievementDef
    {
        public string Id, Name, Blurb;
        public Func<Game, bool> Test;
        public AchievementDef(string id, string name, string blurb, Func<Game, bool> test) { Id = id; Name = name; Blurb = blurb; Test = test; }
    }

    /// <summary>
    /// Local achievements, as data. They are pure functions of the game state, so a replay earns exactly the same
    /// ones. The host keeps which are unlocked across runs; the Core only says which are earned in this one.
    /// </summary>
    public static class Achievements
    {
        public static readonly AchievementDef[] All =
        {
            new AchievementDef("first-blood", "First Blood", "Kill something.", g => g.Player.Kills >= 1),
            new AchievementDef("slayer", "Slayer", "Kill 100 creatures in one run.", g => g.Player.Kills >= 100),
            new AchievementDef("delver", "Delver", "Reach depth 5.", g => g.Player.MaxDepth >= 5),
            new AchievementDef("deep-delver", "Deep Delver", "Reach depth 10.", g => g.Player.MaxDepth >= 10),
            new AchievementDef("abyss", "Into the Abyss", "Reach depth 15.", g => g.Player.MaxDepth >= 15),
            new AchievementDef("veteran", "Veteran", "Reach level 10.", g => g.Player.Level >= 10),
            new AchievementDef("champion", "Champion", "Reach level 20.", g => g.Player.Level >= 20),
            new AchievementDef("survivor", "Survivor", "Live through 5000 turns.", g => g.Turn >= 5000),
            new AchievementDef("pious", "Pious", "Reach 100 piety with a god.", g => g.Player.God != null && g.Player.Piety >= 100),
            new AchievementDef("scholar", "Scholar", "Know ten spells.", g => g.Player.Spells.Count >= 10),
            new AchievementDef("rich", "Rich", "Carry 1000 gold.", g => g.Player.Gold >= 1000),
            new AchievementDef("relic", "Relic Hunter", "Hold an artifact.", HoldsArtifact),
            new AchievementDef("shade-breaker", "Shade Breaker", "Lay a dead hero's shade to rest.", g => g.LaidToRest.Count > 0),
            new AchievementDef("explorer", "Cartographer", "See six regions of the world.", g => g.World != null && g.RegionsSeen() >= 6),
            new AchievementDef("hired-hand", "Hired Hand", "Finish three Guild jobs.", g => g.ContractsDone >= 3),
            new AchievementDef("well-liked", "Well Liked", "Be revered by any house.", g => { foreach (var h in Houses.All) if (g.RepOf(h) >= 60) return true; return false; }),
            new AchievementDef("boss-slayer", "Boss Slayer", "Kill a branch boss.", g => g.BossesSlain.Count >= 1),
            new AchievementDef("kingslayer", "Kingslayer", "Kill three branch bosses in one run.", g => g.BossesSlain.Count >= 3),
            new AchievementDef("mutant", "Mutant", "Carry three mutations at once.", g => g.Player.Mutated.Count >= 3),
            new AchievementDef("pacifist", "Light Footed", "Reach depth 3 without killing anything.", g => g.Player.MaxDepth >= 3 && g.Player.Kills == 0),
            new AchievementDef("escape", "Out of the Pit", "Escape with the Amulet.", g => g.Mode == GameMode.Won),
            new AchievementDef("iron", "Iron Will", "Escape with the Amulet in Hardcore.", g => g.Mode == GameMode.Won && g.Difficulty == Difficulty.Hardcore),
            new AchievementDef("daily-victor", "Daily Victor", "Escape with the Amulet in a daily challenge.", g => g.Mode == GameMode.Won && g.DailyLabel.Length > 0),
        };

        static bool HoldsArtifact(Game g)
        {
            var p = g.Player;
            if (p.Wielded != null && p.Wielded.Rarity == Rarity.Artifact) return true;
            foreach (var piece in p.WornPieces()) if (piece.Rarity == Rarity.Artifact) return true;
            foreach (var it in p.Inventory) if (it.Rarity == Rarity.Artifact) return true;
            return false;
        }

        public static AchievementDef Find(string id)
        {
            foreach (var a in All) if (a.Id == id) return a;
            return null;
        }
    }

    public sealed partial class Game
    {
        /// <summary>Achievements earned in this run (deterministic from the simulation).</summary>
        public readonly HashSet<string> Earned = new HashSet<string>();
        /// <summary>Unlocked in earlier runs; set by the host. Only decides whether the log announces one.</summary>
        public HashSet<string> AlreadyUnlocked = new HashSet<string>();
        /// <summary>Date label of the daily challenge being played, or empty. Set by the host.</summary>
        public string DailyLabel = "";

        /// <summary>
        /// Looks for newly earned achievements. Announcements go straight to the log without bumping
        /// <see cref="Said"/>, so they can never change what a replayed run does.
        /// </summary>
        public void CheckAchievements()
        {
            foreach (var a in Achievements.All)
            {
                if (Earned.Contains(a.Id) || !a.Test(this)) continue;
                Earned.Add(a.Id);
                if (AlreadyUnlocked.Contains(a.Id)) continue;
                var m = new Message(Loc.T("Achievement: ") + Loc.T(a.Name), MessageKind.Quest, Turn);
                Log.Add(m); Transcript.Add(m);
            }
        }
    }
}
