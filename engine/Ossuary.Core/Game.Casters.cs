using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>
    /// Monsters that cast real spells (the pools are in Magic/MonsterSpells.cs). A caster in sight of the hero, with one of its spells in reach,
    /// may spend its turn on that spell instead of moving. Only spells that reach the hero are picked, so every cast lands: no mana, no failure roll.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>
        /// A caster's turn. The hero must be in its sight, and one of the pool's spells must reach him; then the roll decides.
        /// Returns true when it cast, which spends the turn.
        /// </summary>
        bool MonsterCasts(Monster m, int dist, string[] pool, int chance)
        {
            if (pool == null || m.Ally || m.Townsperson || m.CastCooldown > 0 || Map == null) return false;
            if (!Map.IsCurrentlyVisible(m.X, m.Y) || !Fov.HasLine(Map, m.X, m.Y, Player.X, Player.Y)) return false;
            var inReach = new List<SpellDef>();
            foreach (string id in pool)
            {
                var sp = Spells.Find(id);
                if (sp != null && dist <= MonsterSpells.Reach(sp)) inReach.Add(sp);
            }
            if (inReach.Count == 0 || !Rng.Chance(chance)) return false;
            m.CastCooldown = MonsterSpells.Cooldown;
            CastAtHero(m, inReach[Rng.Range(0, inReach.Count)]);
            return true;
        }

        /// <summary>
        /// The cast itself: the animation starts at the caster and ends on the hero, then the damage and the riders the spell carries.
        /// The caster's level stands in for the Magic rank a hero's spell would have.
        /// </summary>
        void CastAtHero(Monster m, SpellDef sp)
        {
            var p = Player;
            Say($"The {m.TheName} casts {sp.Name}!", MessageKind.Info);
            Cue(SpellCue(sp.Elem));
            PlaySpellFx(sp, m.X, m.Y, p.X, p.Y);
            if (sp.Dice > 0)
            {
                int dmg = p.ResistDamage(Rng.Roll(MonsterSpells.Dice(sp), sp.Sides, sp.Flat + m.Level / 4), sp.Type);
                p.HP -= dmg;
                HurtBy(Article(m));
                Say($"{sp.Verb ?? "Magic strikes"} you for {dmg} damage.", MessageKind.Bad);
            }
            ApplyRiderToHero(m, sp);
            Map.Version++;
            CheckDeath();
        }

        /// <summary>The status a monster's spell leaves on the hero: a chance to take hold, and a chance the hero shrugs it off.</summary>
        void ApplyRiderToHero(Monster m, SpellDef sp)
        {
            var p = Player;
            if (sp.Rider == Rider.None || !MonsterSpells.ReachesHero(sp.Rider) || p.HP <= 0) return;
            if (Rng.Dice(100) > sp.RiderPct) return;
            if (Rng.Chance(20)) { Say("You shake it off.", MessageKind.Info); return; }
            int turns = sp.RiderTurns + m.Level / 4;
            switch (sp.Rider)
            {
                case Rider.Burn:
                    SetAlight(p);
                    break;
                case Rider.Confuse:
                    p.Confused = true; p.ConfusionTurns = Math.Max(p.ConfusionTurns, turns);
                    Say("You stagger, confused.", MessageKind.Bad);
                    break;
                case Rider.Blind:
                    p.Blinded = true; p.BlindTurns = Math.Max(p.BlindTurns, turns);
                    Say("The world goes dark for a moment.", MessageKind.Bad);
                    break;
                case Rider.Stun:
                    p.Stunned = true; p.StunTurns = Math.Max(p.StunTurns, turns);
                    Say("You reel, stunned.", MessageKind.Bad);
                    break;
                case Rider.Poison:
                    PoisonPlayer(Rng.Roll(1, 4, m.Level / 4));
                    break;
            }
        }
    }
}
