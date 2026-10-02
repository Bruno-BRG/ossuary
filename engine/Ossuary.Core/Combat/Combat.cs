using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    public struct AttackResult
    {
        public bool Hit;
        public int Damage;
        public bool Dodged;
        public string Message;
        public bool Killed;
    }

    /// <summary>
    /// NetHack's model: roll to-hit against AC, then roll damage. Armour subtracts
    /// from AC, dexterity shifts the attacker's to-hit, and every monster attack has
    /// its own dice. Deterministic given the RNG stream, which is what lets the
    /// headless soak test replay a fight.
    /// </summary>
    public static class Battles
    {
        /// <summary>Melee attack from attacker to defender.</summary>
        public static AttackResult MeleeAttack(Monster attacker, Actor defender, Rng rng, int bonusToHit = 0, int bonusDamage = 0)
        {
            int toHit = 0;
            if (attacker.Def.ToHit != null && attacker.Def.ToHit.Length > 0)
                toHit = attacker.Def.ToHit[0];
            toHit += bonusToHit;
            // Aggression stands in for a monster's dexterity: faster predators swing more often.
            toHit += (attacker.Speed - 12) / 4;

            int ac = (defender is Player dp) ? dp.ArmorClass() : defender.AC;

            int roll = toHit + rng.Dice(20);
            int threshold = 20 - ac + 1;

            if (defender.IsPlayer)
            {
                int evade = ((Player)defender).Evasion();
                if (rng.Dice(100) < evade * 3) return new AttackResult { Dodged = true, Message = $"{defender.TheName} evades the {attacker.Name}'s attack." };
            }

            if (roll < threshold)
                return new AttackResult { Message = $"{defender.TheName} misses the {attacker.Name}." };

            // Pick which of the monster's attacks this swing uses, so a two-attack
            // monster like the jackal genuinely alternates bite and claw.
            int idx = (attacker.Def.DmgDice != null && attacker.Def.DmgDice.Length > 0)
                ? rng.Range(0, attacker.Def.DmgDice.Length)
                : 0;
            int dice = (attacker.Def.DmgDice != null && idx < attacker.Def.DmgDice.Length) ? attacker.Def.DmgDice[idx] : 1;
            int sides = (attacker.Def.DmgSides != null && idx < attacker.Def.DmgSides.Length) ? attacker.Def.DmgSides[idx] : 6;

            int dmg = rng.Roll(dice, sides, 0);
            dmg += bonusDamage;
            if (dmg < 1) dmg = 1;

            defender.HP -= dmg;
            bool killed = defender.HP <= 0;
            string verb = killed ? "kill" : "hit";
            string msg = $"The {attacker.Name} {verb}s the {defender.TheName} for {dmg} damage.";
            return new AttackResult { Hit = true, Damage = dmg, Message = msg, Killed = killed };
        }

        /// <summary>Player melee attack against a monster. Accounts for the wielded weapon and strength.</summary>
        public static AttackResult PlayerMelee(Player player, Monster target, Rng rng, out bool critical, int dmgMult = 1, int hitBonus = 0)
        {
            critical = false;
            int toHit = 2 + (player.Dex - 10) / 2 + SkillRanks.Rank(player.Skills[Skill.Combat]) + (player.BuffTurns("bless") > 0 ? 2 : 0)
                + player.PerkRank("weapon-master") + hitBonus;
            var wmods = player.Wielded != null ? player.Wielded.Mods : default(ItemMods);
            toHit += wmods.ToHit + player.GodMeleeHit;
            int dmgBonus = player.PerkRank("weapon-master") + wmods.Dmg + player.GodMeleeDmg, dice = 1, sides = 4;

            if (player.Wielded != null)
            {
                var w = player.Wielded.Def;
                toHit += w.ToHit;
                dice = Math.Max(1, w.Damage);
                sides = Math.Max(1, w.Sides);
                dmgBonus += w.DmgBonus;
            }

            // Strength scales the swing.
            int str = player.StrengthForDamage();
            if (str >= 16) dice += (str - 14) / 2;
            if (str <= 8) dice = Math.Max(1, dice - 1);

            int roll = toHit + rng.Dice(20);
            int ac = target.AC;
            int threshold = 20 - ac + 1;

            if (roll <= 2) return new AttackResult { Message = "You miss.", };
            if (roll < threshold) return new AttackResult { Message = "You miss the " + target.TheName + "." };
            if (roll == 20 - 1 || roll == 20)
            {
                critical = true;
                dice *= 2;
            }

            int dmg = rng.Roll(dice, sides, dmgBonus) * dmgMult;
            if (dmg < 1) dmg = 1;
            target.HP -= dmg;
            bool killed = target.HP <= 0;
            string critText = critical ? " with a critical hit" : "";
            string msg = $"You {(killed ? "kill" : "hit")} the {target.TheName}{critText} for {dmg} damage.";
            return new AttackResult { Hit = true, Damage = dmg, Message = msg, Killed = killed };
        }

        /// <summary>Ranged attack with to-hit reduced by distance, NetHack style.</summary>
        public static AttackResult PlayerRanged(Player player, Monster target, int distance, Rng rng, out bool critical, int dmgMult = 1, int hitBonus = 0)
        {
            critical = false;
            int toHit = 4 + (player.Dex - 10) + 2 * player.PerkRank("keen-eye") + hitBonus;
            int threshold = 20 - target.AC + 1 - distance / 2;
            int roll = toHit + rng.Dice(20);

            if (roll <= 2) return new AttackResult { Message = "The missile misses." };
            if (roll < threshold) return new AttackResult { Message = $"The missile misses the {target.TheName}." };
            if (roll >= 19) critical = true;

            int dmg = rng.Roll(2, 4, 0) * dmgMult;
            if (critical) dmg *= 2;
            target.HP -= dmg;
            bool killed = target.HP <= 0;
            return new AttackResult
            {
                Hit = true, Damage = dmg, Killed = killed,
                Message = $"The missile {(killed ? "kills" : "hits")} the {target.TheName} for {dmg} damage."
            };
        }
    }
}
