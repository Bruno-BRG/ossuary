using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>Active class abilities, paid in Vigor. Learned through perks (Progression).</summary>
    public sealed partial class Game
    {
        /// <summary>
        /// Starts an ability. Self abilities resolve now; adjacent ones strike the only adjacent
        /// hostile directly, otherwise (and for ranged ones) the targeting cursor opens.
        /// Nothing is spent until the ability resolves.
        /// </summary>
        public bool BeginAbility(string id)
        {
            var ab = Abilities.Find(id);
            if (ab == null || !Player.Abilities.Contains(id)) { Say("You have no such ability."); return false; }
            if (Map == null) { Say("There is nowhere to use that."); return false; }
            if (Player.Asleep || Player.Stunned) { Say("You cannot focus."); return false; }
            if (Player.Vigor < ab.Cost) { Say($"{ab.Name} needs {ab.Cost} Vigor (you have {Player.Vigor}).", MessageKind.Warn); return false; }
            if (ab.Target == AbilityTarget.Self) return UseAbility(id, Player.X, Player.Y);

            var near = NearestVisibleHostile(ab.Range);
            if (ab.Target == AbilityTarget.Adjacent && near != null && Pathfinder.Chebyshev(Player.X, Player.Y, near.X, near.Y) == 1)
            {
                int adjacent = 0;
                foreach (var m in Monsters) if (!m.IsDead && !m.Ally && Pathfinder.Chebyshev(Player.X, Player.Y, m.X, m.Y) == 1) adjacent++;
                if (adjacent == 1) return UseAbility(id, near.X, near.Y);
            }
            UiState.PendingAbility = id;
            PushTargeting(TargetingMode.Ability);
            if (near != null) { UiState.TargetX = near.X; UiState.TargetY = near.Y; }
            return true;
        }

        /// <summary>Resolves an ability at a cell. Returns true when a turn passed.</summary>
        public bool UseAbility(string id, int tx, int ty)
        {
            var ab = Abilities.Find(id);
            var p = Player;
            if (ab == null || !p.Abilities.Contains(id) || Map == null) return false;
            if (p.Vigor < ab.Cost) { Say("You are too winded."); return false; }

            Monster target = null;
            if (ab.Target != AbilityTarget.Self)
            {
                int dist = Pathfinder.Chebyshev(p.X, p.Y, tx, ty);
                if (dist > ab.Range) { Say("That is out of reach."); return false; }
                target = MonsterAt(tx, ty);
                if (target == null || target.IsDead) { Say($"{ab.Name} needs a foe to aim at."); return false; }
                if (target.Ally) { Say("Not on your own ally."); return false; }
                if (ab.Target == AbilityTarget.Ranged && (!Map.IsVisible(tx, ty) || !Fov.HasLine(Map, p.X, p.Y, tx, ty))) { Say("You have no clear shot."); return false; }
            }
            if (id == "shield-bash" && p.WornShield == null) { Say("You need a shield for that."); return false; }

            p.Vigor -= ab.Cost;
            p.VigorTimer = 0;
            GodsOnAbility(id);
            switch (id)
            {
                case "power-strike": Blow(target, 2, 2, "power strike"); break;
                case "hamstring":
                    {
                        int tx2 = target.X, ty2 = target.Y;
                        Fx((tl, s) => FxLib.Slash(tl, s, tx2, ty2, Elem.Blood));
                        Blow(target, 1, 0, "hamstring", PartKind.Leg, 1);
                        break;
                    }
                case "disarming-blow":
                    {
                        int tx2 = target.X, ty2 = target.Y;
                        Fx((tl, s) => FxLib.Slash(tl, s, tx2, ty2, Elem.Wind));
                        Blow(target, 1, 0, "disarming blow", PartKind.Arm, 2);
                        break;
                    }
                case "skull-crack":
                    {
                        int tx2 = target.X, ty2 = target.Y;
                        Fx((tl, s) => FxLib.Shatter(tl, s, tx2, ty2, Elem.Earth, 1));
                        Blow(target, 1, 0, "skull crack", PartKind.Head, 2);
                        break;
                    }
                case "lunge":
                    {
                        int fx0 = p.X, fy0 = p.Y, tx2 = target.X, ty2 = target.Y;
                        if (Pathfinder.Chebyshev(p.X, p.Y, target.X, target.Y) > 1)
                        {
                            int sx = p.X + Math.Sign(target.X - p.X), sy = p.Y + Math.Sign(target.Y - p.Y);
                            if (!Map.CanStep(p.X, p.Y, sx, sy, false) || MonsterAt(sx, sy) != null) { p.Vigor += ab.Cost; Say("Something is in the way.", MessageKind.Warn); return false; }
                            p.X = sx; p.Y = sy;
                            UpdateFov();
                        }
                        Fx((tl, s) => FxLib.Bolt(tl, s, fx0, fy0, tx2, ty2, Elem.Wind, '-', 1));
                        Blow(target, 2, 0, "lunge");
                        break;
                    }
                case "backstab":
                    {
                        bool unaware = target.Asleep || target.Alert == 0 || target.FearTurns > 0 || target.Confused;
                        Blow(target, unaware ? 3 : 2, 2, unaware ? "backstab" : "stab");
                        break;
                    }
                case "holy-strike":
                    {
                        bool alive = Blow(target, 1, 0, "holy strike");
                        if (alive) Hurt(target, Rng.Roll(2, 6, 0), DamageType.Holy, "Light sears");
                        break;
                    }
                case "shield-bash":
                    {
                        bool alive = Blow(target, 1, 0, "shield bash");
                        if (alive) { target.Energy -= 24; Say($"The {target.TheName} reels.", MessageKind.Good); }
                        break;
                    }
                case "cleave":
                    {
                        int swung = 0;
                        foreach (var m in new List<Monster>(Monsters))
                        {
                            if (m.IsDead || m.Ally || Pathfinder.Chebyshev(p.X, p.Y, m.X, m.Y) != 1) continue;
                            Blow(m, 1, 0, "cleave"); swung++;
                        }
                        if (swung == 0) Say("Your blade cuts only air.", MessageKind.Info);
                        break;
                    }
                case "aimed-shot":
                    {
                        int dist = Pathfinder.Chebyshev(p.X, p.Y, target.X, target.Y);
                        var res = Battles.PlayerRanged(p, target, dist, Rng, out _, 2, 4);
                        if (res.Hit) target.Asleep = false;
                        Say(res.Message, res.Killed ? MessageKind.Kill : MessageKind.Combat);
                        WoundFrom(target, res, true, false);
                        p.GainSkill(Skill.Combat, 2);
                        if (res.Killed) KillMonster(target); else { target.Alert = 1; target.Dormant = false; }
                        break;
                    }
                case "second-wind": Heal(Math.Max(1, p.MaxHP / 4)); break;
                case "lay-on-hands":
                    Heal(Math.Max(1, p.MaxHP / 2));
                    p.PoisonResist = 0;
                    break;
                case "war-cry":
                    {
                        int scared = 0;
                        foreach (var m in Monsters)
                        {
                            if (m.IsDead || m.Ally || m.Def.Mindless || m.Def.Undead || !Map.IsVisible(m.X, m.Y)) continue;
                            if (Pathfinder.Chebyshev(p.X, p.Y, m.X, m.Y) > 6 || Resists(m, 15, 8)) continue;
                            m.FearTurns = 8 + p.Level / 2; m.Asleep = false; scared++;
                        }
                        Say(scared > 0 ? $"Your roar scatters {scared} foe(s)!" : "Your roar echoes, and nothing flees.", MessageKind.Good);
                        break;
                    }
                case "vanish":
                    p.SetBuff("invisibility", 12);
                    p.Invisible = true;
                    Say("You melt into the shadows.", MessageKind.Good);
                    break;
            }
            Map.Version++;
            EndPlayerTurn();
            return true;
        }

        /// <summary>
        /// One melee blow with a multiplier. A technique names the part it goes for (<paramref name="aim"/>, no penalty, the wound
        /// <paramref name="bump"/> steps worse); otherwise the hero's own aim applies with its usual cost. Returns true when the target survives.
        /// </summary>
        bool Blow(Monster target, int mult, int hitBonus, string label, PartKind? aim = null, int bump = 0)
        {
            bool unaware = target.Asleep || target.Alert == 0 || target.FearTurns > 0 || target.Confused;
            if (aim == null) { aim = Player.Aim; hitBonus -= Bodies.AimPenalty(aim); }
            var res = Battles.PlayerMelee(Player, target, Rng, out bool crit, mult, hitBonus);
            if (res.Killed) { _killSneak = unaware; _killType = DamageType.Physical; }
            if (res.Hit) target.Asleep = false;
            Say(res.Message, res.Killed ? MessageKind.Kill : MessageKind.Combat);
            WoundFrom(target, res, Bodies.EdgedWeapon(Player), crit, aim, bump);
            AfterBlow(res, crit);
            if (res.Hit) MeleeProcs(target, res.Damage, !res.Killed);
            Player.GainSkill(Skill.Combat, res.Hit ? 1 : 0);
            if (res.Killed) { KillMonster(target); return false; }
            if (res.Hit) { target.Alert = 1; target.Dormant = false; }
            return res.Hit && !target.IsDead;
        }

        /// <summary>Reading the manual of strikes: the drilled roles learn every technique their level allows; the rest wait.</summary>
        void StudyStrikes()
        {
            var p = Player;
            if (Array.IndexOf(Abilities.Drilled, p.RoleId) < 0) { Say("The drills assume years in a shield wall you never stood in.", MessageKind.Info); return; }
            if (p.Blinded || p.Confused) { Say("You cannot read like this."); return; }
            int learned = 0, next = 0;
            foreach (var t in Abilities.Techniques())
            {
                if (p.Abilities.Contains(t.Id)) continue;
                if (t.Level > p.Level) { if (next == 0 || t.Level < next) next = t.Level; continue; }
                p.Abilities.Add(t.Id);
                learned++;
                Say($"You learn {t.Name}!", MessageKind.Good);
            }
            if (learned == 0) Say(next > 0 ? $"You know every drill you are ready for. The next asks for level {next}." : "You already know every strike in the manual.", MessageKind.Info);
            else { p.GainSkill(Skill.Combat, 2 * learned); if (Mode == GameMode.Dungeon || Mode == GameMode.TownMap) EndPlayerTurn(); }
        }
    }
}
