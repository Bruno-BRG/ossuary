using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>How a person carries themselves. Steers which lines they say and, later, how they react to violence.</summary>
    public enum Trait { Kind, Bitter, Proud, Coward, Greedy, Pious, Curious, Weary }

    /// <summary>What a person needs. Personal quests are born from this (see docs/game/living-world.md).</summary>
    public enum Want { None, KinLost, Debt, RareItem, Revenge, Passage, Peace }

    /// <summary>
    /// Who a townsperson is beyond their job. Generated once with the town from a private Rng (never the town's own stream,
    /// so adding or changing a persona never shifts the layout or the other people), and read-only afterwards.
    /// </summary>
    public sealed class Persona
    {
        public Trait Trait, Second;
        public Want Want;
        /// <summary>A secret id (empty = none). Revealed by reputation, a favour or a rumour.</summary>
        public string Secret = "";
        /// <summary>Needed to progress the story: knocked out instead of killed, and someone steps up if they are gone.</summary>
        public bool Essential;
        /// <summary>Carries a trouble of their own to ask the hero about; the quest comes from <see cref="Want"/>.</summary>
        public bool Troubled;
        /// <summary>Another person of the same town who takes the role over (set only on essentials).</summary>
        public Monster Successor;
        /// <summary>The post an apprentice is learning: set on the successor of an essential person, cleared when they take the post (Game.Succession.cs).</summary>
        public TownRole? Training;

        public bool Has(Trait t) => Trait == t || Second == t;

        public static Persona For(Rng r, TownRole role)
        {
            var p = new Persona { Trait = (Trait)r.Range(0, 8) };
            if (r.Chance(40)) { p.Second = (Trait)r.Range(0, 8); if (p.Second == p.Trait) p.Second = (Trait)(((int)p.Trait + 1) % 8); }
            else p.Second = p.Trait;
            p.Want = role == TownRole.Child || role == TownRole.Pet ? Want.None : (Want)r.Range(0, 7);
            p.Essential = IsEssentialRole(role);
            // About a third of the ordinary folk carry a trouble they will ask the hero about (personal quests).
            p.Troubled = IsPersonalRole(role) && (p.Want == Want.Revenge || p.Want == Want.Debt || p.Want == Want.RareItem || p.Want == Want.KinLost) && r.Chance(35);
            return p;
        }

        /// <summary>People who may ask the hero for a favour of their own. The beggar and the scholar speak their own authored lines (the Cult, the Reader), so a favour of theirs could never be asked.</summary>
        public static bool IsPersonalRole(TownRole role) =>
            role == TownRole.Citizen || role == TownRole.Adventurer || role == TownRole.Drunk || role == TownRole.Prisoner;

        /// <summary>The roles the main questline leans on (see docs/game/main-quest.md): the Elder, the Reader, the Captain, the High Priest and the Cult's voice, the beggar who is not.</summary>
        public static bool IsEssentialRole(TownRole role) =>
            role == TownRole.Elder || role == TownRole.Scholar || role == TownRole.Captain || role == TownRole.Priest || role == TownRole.Beggar;
    }

    /// <summary>What one person remembers about the hero. Written by deeds, read by lines, prices and quests.</summary>
    public sealed class NpcMemory
    {
        /// <summary>-100 hates you .. +100 trusts you.</summary>
        public int Disposition;
        public readonly HashSet<string> Flags = new HashSet<string>();
        /// <summary>Blows struck while this essential person lay out cold: the third removes them (Game.Crime.cs, Outrage).</summary>
        public int Outrages;

        public bool Has(string flag) => Flags.Contains(flag);
        public void Set(string flag) => Flags.Add(flag);
        public void Unset(string flag) => Flags.Remove(flag);
        public void Shift(int d) => Disposition = Math.Max(-100, Math.Min(100, Disposition + d));

        /// <summary>How the person stands with the hero, as one word for the conversation box: wary once struck, else by disposition.</summary>
        public string MoodKey => Has(Struck) ? "wary" : Disposition >= 25 ? "warm" : Disposition <= -25 ? "cold" : "neutral";

        /// <summary>An heir who owes the hero one favour before the post's business goes on (Game.Succession.cs).</summary>
        public const string Met = "met", Struck = "struck", Helped = "helped", Owes = "owes";
        /// <summary>An heir who took a counter over from a keeper who died (Game.Succession.cs, InstallShopHeirs).</summary>
        public const string Inherited = "inherited";
    }
}
