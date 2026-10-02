using System;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Stealth and noise. How far away a monster notices the player is its Vision, trimmed by the Stealth skill,
    /// the light-feet perk and Sylk, and then shifted by how loud the player's last action was: fighting, casting
    /// and forcing doors carry; heavy armour clatters; standing still or searching is quiet. Pure arithmetic: no Rng.
    /// </summary>
    public sealed partial class Game
    {
        int _loud;           // loudest action this turn (0 = none)
        bool _quiet;         // the player held still
        int _noticeShift;    // what the monsters of this turn heard

        public void ResetNoise() { _loud = 0; _quiet = false; }
        public void MakeNoise(int loudness) { if (loudness > _loud) _loud = loudness; }
        public void BeQuiet() { _quiet = true; }

        /// <summary>Clatter of heavy body armour while walking: chain and splint +1, plate +2.</summary>
        public int ArmourClatter()
        {
            int ac = Player.WornArmor != null ? Player.WornArmor.Def.AC : 0;
            return ac >= 7 ? 2 : ac >= 5 ? 1 : 0;
        }

        /// <summary>Squares of Vision the hero's stealth takes off every monster (Stealth 25 per square, light feet, Sylk).</summary>
        public int StealthReduction() =>
            Player.Skills[Skill.Stealth] / 25 + 2 * Player.PerkRank("light-feet") + (Player.God == "sylk" && Player.GodTier >= 2 ? 1 : 0);

        /// <summary>Called once per turn, before the monsters move: turns this turn's actions into a notice shift.</summary>
        void ResolveNoise()
        {
            _noticeShift = _loud > 0 ? _loud : _quiet ? -2 : 0;
            ResetNoise();
        }

        /// <summary>How far away this monster would notice the player right now.</summary>
        public int NoticeRadius(Monster m) => Math.Max(1, m.Def.Vision - StealthReduction() + _noticeShift);

        /// <summary>
        /// Stealth improves by getting away with it: now and then, when a monster stands inside its normal sight
        /// but outside the shortened notice radius, the hero learns a little.
        /// </summary>
        void LearnFromHiding(Monster m, int dist)
        {
            if (m.Alert == 0 && dist > NoticeRadius(m) && dist <= m.Def.Vision && (Turn % 8) == 0) Player.GainSkill(Skill.Stealth, 1);
        }
    }
}
