using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// A rival party that goes down the Dungeons on its own clock: one level every four days from day 3. Nobody simulates it
    /// turn by turn; where it is on a given day is arithmetic. The hero hears of it in taverns and can race it.
    /// </summary>
    public sealed partial class Game
    {
        static readonly string[] RivalNames = { "the Ash Company", "Maren's Blades", "the Grey Lanterns", "the Penny Knives", "Brother Voss and his five", "the Hollow Crows" };
        public const int RivalRaceDepth = 7;

        public string RivalName => RivalNames[(int)(Rumours.Hash(Rng.Seed, "rival", 0) % (uint)RivalNames.Length)];

        /// <summary>How deep the rival party is on a world day (0 before they set out).</summary>
        public static int RivalDepthOn(int day) => Math.Max(0, Math.Min(10, (day - 3) / 4));

        /// <summary>The day the rival reaches a given level.</summary>
        public static int RivalDayAt(int depth) => 3 + 4 * depth;

        public QuestDef RivalQuest()
        {
            string name = RivalName;
            var def = new QuestDef
            {
                Id = "rival.race", Track = QuestDef.Rival, Giver = "the tavern",
                Deadline = Math.Max(4, RivalDayAt(RivalRaceDepth) - Today), RewardGold = 150,
                Title = TownText.L("A race to the bottom", "Uma corrida até o fundo"),
            };
            def.Step(ObjKind.Reach, TownText.L("Reach level 7 of The Dungeons before the rival party does.", "Chegue ao nível 7 das Masmorras antes do grupo rival."), "The Dungeons", RivalRaceDepth, "The Dungeons", name);
            def.OnComplete = g => { g.Flags.Add("rival.beaten"); g.AddRep(Houses.Guild, 5, "beating a rival party to the bottom"); };
            def.OnFail = g => { g.Flags.Add("rival.won"); g.AddRep(Houses.Guild, -3, "losing the race"); g.Say(Loc.T("The rival party reached level 7 first, and they are not shy about it."), MessageKind.Warn); };
            return def;
        }

        /// <summary>What the tavern says about the rival today.</summary>
        public string RivalReport()
        {
            string name = RivalName;
            int d = RivalDepthOn(Today);
            if (Flags.Contains("rival.beaten")) return $"{name} came back from the dark a step behind you, and the whole tavern knows it.";
            if (Flags.Contains("rival.won")) return $"{name} reached level {RivalRaceDepth} before you, and they buy rounds in your name.";
            if (d <= 0) return $"{name} are buying rope and arguing about the stairs. They have not set out yet.";
            return $"{name} went down the Dungeons and were last seen on level {d}.";
        }
    }
}
