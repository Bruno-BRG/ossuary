using System.Collections.Generic;

namespace Ossuary.Core.Entities
{
    /// <summary>Who an ability acts on. Adjacent and Ranged open the targeting cursor.</summary>
    public enum AbilityTarget { Self, Adjacent, Ranged }

    /// <summary>An active class ability, paid in Vigor. Learned through the perk that grants it.</summary>
    public sealed class AbilityDef
    {
        public string Id, Name, Blurb;
        public int Cost;
        public AbilityTarget Target;
        public int Range;
        /// <summary>A martial technique: learned from the manual of strikes once the hero reaches this level (0 = a perk ability).</summary>
        public int Level;
    }

    public static class Abilities
    {
        /// <summary>The Fighter's book of strikes: reading it teaches every technique the hero is ready for.</summary>
        public const string StrikesBook = "a manual of strikes";

        /// <summary>The roles that can follow the manual's drills.</summary>
        public static readonly string[] Drilled = { "fighter", "paladin", "adventurer" };

        static AbilityDef A(string id, string name, int cost, AbilityTarget t, int range, string blurb)
            => new AbilityDef { Id = id, Name = name, Cost = cost, Target = t, Range = range, Blurb = blurb };

        static AbilityDef S(string id, string name, int cost, AbilityTarget t, int range, int level, string blurb)
            => new AbilityDef { Id = id, Name = name, Cost = cost, Target = t, Range = range, Level = level, Blurb = blurb };

        public static readonly AbilityDef[] All = {
            A("power-strike", "Power Strike", 4, AbilityTarget.Adjacent, 1, "A heavy blow: double damage and +2 to hit."),
            A("second-wind", "Second Wind", 8, AbilityTarget.Self, 0, "Catch your breath: recover a quarter of your HP."),
            A("cleave", "Cleave", 6, AbilityTarget.Self, 0, "One sweeping attack on every adjacent foe."),
            A("shield-bash", "Shield Bash", 4, AbilityTarget.Adjacent, 1, "Slam with your shield: light damage, and the foe loses its next turns."),
            A("war-cry", "War Cry", 8, AbilityTarget.Self, 0, "A roar that sends the living nearby running."),
            A("lay-on-hands", "Lay on Hands", 10, AbilityTarget.Self, 0, "Heal half your HP and burn out poison."),
            A("holy-strike", "Holy Strike", 6, AbilityTarget.Adjacent, 1, "A blow wreathed in light: extra holy damage, double against the dead."),
            A("backstab", "Backstab", 4, AbilityTarget.Adjacent, 1, "Triple damage against a sleeping, fleeing, confused or unaware foe; otherwise double."),
            A("vanish", "Vanish", 8, AbilityTarget.Self, 0, "Slip out of sight for a few turns."),
            A("aimed-shot", "Aimed Shot", 5, AbilityTarget.Ranged, 8, "Take your time: +4 to hit and double damage at range."),
            S("hamstring", "Hamstring", 3, AbilityTarget.Adjacent, 1, 1, "Cut low at the legs: the wound there is a step worse."),
            S("disarming-blow", "Disarming Blow", 5, AbilityTarget.Adjacent, 1, 3, "Go for the weapon arm, two steps worse; mangled, it drops its weapon."),
            S("skull-crack", "Skull Crack", 6, AbilityTarget.Adjacent, 1, 5, "A blow to the head, two steps worse: it reels, stunned."),
            S("lunge", "Lunge", 4, AbilityTarget.Ranged, 2, 7, "Close two squares in one stride and strike for double damage."),
        };

        public static IEnumerable<AbilityDef> Techniques() { foreach (var a in All) if (a.Level > 0) yield return a; }

        public static AbilityDef Find(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }
    }
}
