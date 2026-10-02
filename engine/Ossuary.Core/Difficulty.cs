using System;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>How the run is played. Chosen at creation and fixed for the run (saves carry it, replays need it).</summary>
    public enum Difficulty
    {
        /// <summary>The standard game.</summary>
        Normal,
        /// <summary>No hunger: food is a curiosity. For learning the dungeon.</summary>
        Classic,
        /// <summary>One life, one save: the save is erased the moment the run is resumed, so quitting is the only save.</summary>
        Hardcore,
        /// <summary>Challenge: the run opens on depth 5 with a few levels and potions, and nothing to go back to.</summary>
        Dive,
        /// <summary>Challenge: no weapon, no armour, no shield. One extra advancement to make up for it.</summary>
        Naked,
        /// <summary>Skills do not rise by use: every point is bought with experience (Shift+N), Sil style.</summary>
        Trained,
    }

    public static class Difficulties
    {
        public static readonly Difficulty[] All = { Difficulty.Normal, Difficulty.Classic, Difficulty.Hardcore, Difficulty.Dive, Difficulty.Naked, Difficulty.Trained };

        public static string Name(Difficulty d) => d.ToString();

        public static string Blurb(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Classic: return "No hunger. Food is only a luxury.";
                case Difficulty.Hardcore: return "One save: it is erased when you resume. No quicksave.";
                case Difficulty.Dive: return "Start on depth 5, a few levels up. Score x2.";
                case Difficulty.Naked: return "No weapon, armour or shield. One more advancement. Score x2.";
                case Difficulty.Trained: return "Skills rise only when you buy them with XP (Shift+N).";
                default: return "The standard game.";
            }
        }

        public static Difficulty Parse(string s) => Enum.TryParse(s, out Difficulty d) && Array.IndexOf(All, d) >= 0 ? d : Difficulty.Normal;

        /// <summary>Score multiplier as a fraction (numerator, denominator).</summary>
        public static void ScoreFactor(string mode, out int num, out int den)
        {
            num = 1; den = 1;
            if (mode == "Hardcore") { num = 3; den = 2; }
            else if (mode == "Classic") { num = 2; den = 3; }
            else if (mode == "Dive" || mode == "Naked") { num = 2; den = 1; }
        }
    }

    public sealed partial class Game
    {
        /// <summary>Set by the host right after the game is built, before the first key.</summary>
        public Difficulty Difficulty = Difficulty.Normal;

        /// <summary>Sets up a challenge run. Called once by the host right after the difficulty is set.</summary>
        public void ApplyChallenge()
        {
            var p = Player;
            if (Difficulty == Difficulty.Naked)
            {
                p.Wielded = null; p.WornArmor = null; p.WornShield = null;
                p.PendingAdvances++;
                p.RefreshGear();
            }
            else if (Difficulty == Difficulty.Trained)
            {
                p.Trained = true; p.TrainXp = 60;
            }
            else if (Difficulty == Difficulty.Dive)
            {
                while (p.Level < 4) p.AddXp(p.XpNext);
                p.HP = p.MaxHP; p.Mp = p.MpMax;
                foreach (var d in Catalogue.Potions)
                    if (d.Name == "potion of healing") p.Inventory.Add(new Item(d, Rng, NextUid()) { Identified = true, Quantity = 2 });
                foreach (var d in Catalogue.Food)
                    if (d.Name == "food ration") { foreach (var it in p.Inventory) if (it.Def.Name == d.Name) it.Quantity += 2; }
                DescendTo("The Dungeons", 5);
            }
        }
    }
}
