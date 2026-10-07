using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Bodies and wounds (docs/todo.md, Depth track 1). A hit that takes a real share of a creature's life lands on a
    /// body part and leaves a wound on top of the HP loss: legs make it limp, arms spoil its aim, a blow to the head stuns,
    /// eyes shorten its sight, and edges make it bleed. The hero's wounds mend over time (badly ones leave a scar);
    /// potions, rest at an inn and the temple speed that up.
    /// </summary>
    public partial class Game
    {
        /// <summary>
        /// Called after an attack resolves. Picks the part (one RNG draw; a called shot draws among the parts of the kind it
        /// aimed for), worsens or opens a wound and applies its effect. An aimed hit always leaves a mark; <paramref name="bump"/>
        /// makes it worse (techniques that go for the part).
        /// </summary>
        public void WoundFrom(Actor defender, AttackResult res, bool edged, bool critical, PartKind? aim = null, int bump = 0)
        {
            if (!res.Hit || res.Killed || defender.HP <= 0) return;
            var plan = Bodies.PlanOf(defender);
            if (plan == null) return;
            bool aimed = aim != null && Bodies.Has(plan, aim.Value);
            int sev = Bodies.Severity(res.Damage, defender.MaxHP, critical || res.Deep);
            if (aimed) sev = Math.Max(1, sev);
            if (sev > 0) sev = Math.Min(4, sev + bump);
            if (sev == 0) return;

            int total = 0;
            foreach (var bp in plan.Parts) if (!aimed || bp.Kind == aim.Value) total += bp.Weight;
            int roll = Rng.Range(0, total);
            BodyPart part = plan.Parts[0];
            foreach (var bp in plan.Parts)
            {
                if (aimed && bp.Kind != aim.Value) continue;
                if (roll < bp.Weight) { part = bp; break; }
                roll -= bp.Weight;
            }

            var w = defender.Wounds.Find(x => x.Part == part.Name);
            if (w != null && w.Severed) return;
            int before = w?.Severity ?? 0;
            int now = sev > before ? sev : Math.Min(4, before + 1);
            if (now == before) return;
            int limpBefore = Bodies.Limp(defender);
            bool bledBefore = Bodies.Bleeding(defender);
            bool blindBefore = Bodies.Blind(defender);
            if (w == null) { w = new Wound { Part = part.Name, Kind = part.Kind }; defender.Wounds.Add(w); }
            w.Severity = now; w.Worst = Math.Max(w.Worst, now); w.Edged = edged; w.HealIn = Bodies.HealTime(now);
            // An edge that lands a mangling blow on a limb, wing or tail of a monster can take it off.
            bool limb = part.Kind == PartKind.Arm || part.Kind == PartKind.Leg || part.Kind == PartKind.Wing || part.Kind == PartKind.Tail;
            if (!defender.IsPlayer && edged && limb && now == 4 && (sev >= 4 || critical)) w.Severed = true;
            if (edged && Bodies.Bleeds(defender)) w.Bleed = Math.Max(w.Bleed, Bodies.BleedTime(now, part.Kind));
            if (w.Severed && Bodies.Bleeds(defender)) w.Bleed = Math.Max(w.Bleed, 10);
            w.Bound = false;

            var m = defender as Monster;
            bool seen = defender.IsPlayer || Mode == GameMode.Overworld || (Map != null && Map.IsVisible(defender.X, defender.Y));
            if (defender.IsPlayer) Say($"Your {part.Name} is {w.Adjective}.", MessageKind.Bad);
            else if (seen) Say($"{defender.Subj}'s {part.Name} is {w.Adjective}.", w.Severed ? MessageKind.Good : MessageKind.Combat);

            if (part.Kind == PartKind.Head && now >= 3)
            {
                if (defender.IsPlayer) { Player.StunTurns = Math.Max(Player.StunTurns, 2); Player.Stunned = true; Say("You reel from the blow to your head.", MessageKind.Bad); }
                else if (m != null) { m.HeldTurns = Math.Max(m.HeldTurns, 1); if (seen) Say($"The {m.TheName} reels, stunned.", MessageKind.Combat); }
            }
            if (part.Kind == PartKind.Arm && now >= 4) LoseGrip(defender, part, seen);
            if (m != null && part.Kind == PartKind.Wing && now >= 3 && !m.Grounded && (m.Def.Flys || plan.Moves == PartKind.Wing))
            {
                m.Grounded = true;
                if (seen) Say($"The {m.TheName} crashes to the ground.", MessageKind.Good);
            }
            if (m != null && !blindBefore && Bodies.Blind(m) && seen) Say($"The {m.TheName} is blinded.", MessageKind.Good);
            int limp = Bodies.Limp(defender);
            if (limp > limpBefore)
            {
                if (defender.IsPlayer) Say(limp == 1 ? "You can only limp now." : "You can barely crawl.", MessageKind.Bad);
                else if (seen && m != null) Say(limp == 1 ? $"The {m.TheName} limps." : $"The {m.TheName} drags itself along.", MessageKind.Combat);
            }
            // A crippled creature with a mind of its own breaks and runs, once.
            if (m != null && !m.Fled && Bodies.Limp(m) > 0 && m.HP * 2 <= m.MaxHP && !m.Def.Mindless && !m.Def.Undead && m.BossId == null && !m.Townsperson && !m.Ally)
            {
                m.Fled = true;
                m.FearTurns = Math.Max(m.FearTurns, 10);
                if (seen) Say($"The {m.TheName} turns to flee.", MessageKind.Good);
            }
            if (defender.IsPlayer && !bledBefore && w.Bleed > 0) Say("You are bleeding.", MessageKind.Bad);
            if (defender.IsPlayer && part.Kind == PartKind.Eye) UpdateFov();
        }

        /// <summary>A mangled arm lets go: the hero drops a weapon held in the right hand; a monster drops what it carried to fight with.</summary>
        void LoseGrip(Actor who, BodyPart part, bool seen)
        {
            if (who.IsPlayer)
            {
                var p = Player;
                if (!part.Main || p.Wielded == null) return;
                var it = p.Wielded;
                p.Wielded = null;
                if (Map != null && Mode != GameMode.Overworld) { GroundItems.Add(Map.Number, p.X, p.Y, it); Map.Version++; }
                else p.Inventory.Add(it);
                Say($"Your grip fails, and you drop {it.Name}.", MessageKind.Bad);
                return;
            }
            if (!(who is Monster m) || m.Disarmed) return;
            m.Disarmed = true;
            bool dropped = false;
            for (int i = m.Inventory.Count - 1; i >= 0; i--)
            {
                var it = m.Inventory[i];
                if (it.Def.Kind != Items.ItemKind.Weapon) continue;
                m.Inventory.RemoveAt(i);
                if (Map != null && Mode != GameMode.Overworld) GroundItems.Add(Map.Number, m.X, m.Y, it);
                dropped = true;
            }
            if (Map != null) Map.Version++;
            if (seen) Say(dropped ? $"The {m.TheName} drops its weapon." : $"The {m.TheName} can no longer swing that arm.", MessageKind.Good);
        }

        /// <summary>A monster's speed after its legs (or wings): three quarters when limping, half when crawling.</summary>
        int MoveSpeed(Monster m)
        {
            int limp = Math.Min(2, Bodies.Limp(m) + (m.Grounded && Bodies.PlanOf(m)?.Moves != PartKind.Wing ? 1 : 0));
            return limp == 0 ? m.Speed : Math.Max(2, m.Speed * (4 - limp) / 4);
        }

        /// <summary>The hero's wounds each turn: open cuts bleed a point, closed ones count down to mending.</summary>
        void TickWounds()
        {
            var p = Player;
            if (p.Wounds.Count == 0) return;
            bool was = Bodies.Bleeding(p), bled = false;
            for (int i = p.Wounds.Count - 1; i >= 0; i--)
            {
                var w = p.Wounds[i];
                if (w.Bleed > 0) { w.Bleed--; p.HP -= 1; bled = true; continue; }
                w.HealIn -= w.Bound ? 2 : 1;
                if (w.HealIn > 0) continue;
                p.Wounds.RemoveAt(i);
                Healed(w);
            }
            if (bled) HurtBy("blood loss");
            if (was && !Bodies.Bleeding(p) && p.HP > 0) Say("Your bleeding stops.", MessageKind.Good);
        }

        void Healed(Wound w)
        {
            if (w.Worst >= 3)
            {
                bool face = w.Kind == PartKind.Head || w.Kind == PartKind.Eye;
                bool firstOnFace = face && !HasFaceScar(Player);
                if (!Player.Scars.Contains(w.Part)) Player.Scars.Add(w.Part);
                Say($"Your {w.Part} has healed, leaving a scar.", MessageKind.Good);
                if (firstOnFace) { Player.Cha = Math.Max(3, Player.Cha - 1); Say("The scar on your face will draw looks. (-1 Cha)", MessageKind.Info); }
            }
            else if (w.Worst >= 2) Say($"Your {w.Part} has healed.", MessageKind.Good);
        }

        public static bool HasFaceScar(Player p) { foreach (var s in p.Scars) if (s == "head" || s.EndsWith("eye", StringComparison.Ordinal)) return true; return false; }

        /// <summary>Scars make the hero look like someone who has won fights: a point of intimidation for each, up to two.</summary>
        public int Intimidation => Math.Min(2, Player.Scars.Count);

        /// <summary>The first time someone in town talks with a scarred hero, they look at the worst of it.</summary>
        void NoticeScar(Monster m)
        {
            if (m == null || m.Memory == null || Player.Scars.Count == 0 || m.Memory.Has("scar")) return;
            m.Memory.Set("scar");
            string part = Player.Scars[0];
            foreach (var s in Player.Scars) if (s == "head" || s.EndsWith("eye", StringComparison.Ordinal)) { part = s; break; }
            Say($"Eyes linger on the scar on your {part}.", MessageKind.Info);
        }

        /// <summary>
        /// Binding wounds with a bandage: every cut stops bleeding and every wound mends twice as fast until it is hurt again.
        /// Costs one bandage and a turn. Returns false when there was nothing to bind (no turn spent).
        /// </summary>
        public bool ApplyBandage(Items.Item bandage)
        {
            var p = Player;
            bool any = false;
            foreach (var w in p.Wounds) if (!w.Bound || w.Bleed > 0) any = true;
            if (!any) { Say(p.Wounds.Count == 0 ? "You have no wounds to bind." : "Your wounds are already bound.", MessageKind.Info); return false; }
            bool bled = Bodies.Bleeding(p);
            foreach (var w in p.Wounds) { w.Bleed = 0; w.Bound = true; }
            if (bandage.Quantity > 1) bandage.Quantity--; else p.Inventory.Remove(bandage);
            Say(bled ? "You bind your wounds, and the bleeding stops." : "You bind your wounds.", MessageKind.Good);
            p.GainSkill(Skill.Survival, 1);
            if (Mode == GameMode.Dungeon || Mode == GameMode.TownMap) EndPlayerTurn();
            return true;
        }

        /// <summary>Fire closes what it touches: burning stops every open cut.</summary>
        void Cauterise(Actor a)
        {
            if (!Bodies.Bleeding(a)) return;
            foreach (var w in a.Wounds) w.Bleed = 0;
            if (a.IsPlayer) Say("The flames sear your wounds shut.", MessageKind.Good);
            else if (a is Monster m && Map != null && Map.IsVisible(m.X, m.Y)) Say($"The flames seal the wounds of the {m.TheName}.", MessageKind.Info);
        }

        /// <summary>Hours on the road and in the wild count toward mending: each hour mends as much as thirty turns.</summary>
        const int TurnsPerHour = 30;

        void MendHours(int hours)
        {
            if (Player.Wounds.Count == 0 || hours <= 0) return;
            for (int i = 0; i < hours * TurnsPerHour && Player.Wounds.Count > 0 && Player.HP > 0; i++) TickWounds();
            if (Player.HP <= 0) CheckDeath();
        }

        /// <summary>Time passes outside the turn loop (travel, gathering, events): the world clock and the hero's wounds both move.</summary>
        void PassHours(int hours)
        {
            World?.AdvanceTime(hours);
            MendHours(hours);
        }

        /// <summary>Shift+F: the next part to aim for. Free; shown in the sidebar.</summary>
        public void CycleAim()
        {
            var p = Player;
            int i = Array.IndexOf(Bodies.AimCycle, p.Aim);
            p.Aim = Bodies.AimCycle[(i + 1) % Bodies.AimCycle.Length];
            int pen = Bodies.AimPenalty(p.Aim);
            Say(p.Aim == null ? "You stop aiming and strike wherever you can." : $"You aim for {Bodies.AimName(p.Aim)} (-{pen} to hit).", MessageKind.Info);
        }

        /// <summary>A blind monster next to its foe swings at a random neighbouring square half the time.</summary>
        bool WildSwing(Monster m)
        {
            if (!Bodies.Blind(m) || !Rng.Chance(50)) return false;
            int k = Rng.Range(0, 8);
            int x = m.X + Pathfinder.Dx8[k], y = m.Y + Pathfinder.Dy8[k];
            bool seen = Map.IsVisible(m.X, m.Y);
            if (x == Player.X && y == Player.Y) return false;     // it guessed right: the normal attack follows
            var other = MonsterAt(x, y);
            if (other == null || other.IsDead || other.Townsperson)
            {
                if (seen) Say($"The {m.TheName} swings wildly at nothing.", MessageKind.Good);
                return true;
            }
            var res = Battles.MeleeAttack(m, other, Rng);
            if (seen) Say(res.Hit ? $"The blind {m.Name} hits the {other.TheName} for {res.Damage}." : $"The blind {m.Name} lashes out at the {other.TheName} and misses.", MessageKind.Info);
            if (Bodies.Wounding(res.Kind)) WoundFrom(other, res, Bodies.EdgedAttack(res.Kind), false);
            if (res.Killed) { other.HP = 0; Monsters.Remove(other); Map.Version++; if (seen) Say($"The {other.Name} falls to the {m.Name}.", MessageKind.Info); }
            return true;
        }

        /// <summary>Healing magic, potions and rest: every wound stops bleeding and drops by <paramref name="steps"/> severities.</summary>
        public void MendWounds(int steps)
        {
            var p = Player;
            for (int i = p.Wounds.Count - 1; i >= 0; i--)
            {
                var w = p.Wounds[i];
                w.Bleed = 0;
                w.Severity -= steps;
                if (w.Severity > 0) { w.HealIn = Bodies.HealTime(w.Severity); continue; }
                p.Wounds.RemoveAt(i);
                Healed(w);
            }
        }

        void StopBleeding() { foreach (var w in Player.Wounds) w.Bleed = 0; }

        /// <summary>A monster's open wounds. Bleeding out counts as the hero's kill; regenerating monsters close a wound now and then.</summary>
        void TickMonsterWounds(Monster m)
        {
            if (m.Wounds.Count == 0) return;
            if (m.Def.Regenerates && Turn % 20 == 0) { int i = m.Wounds.FindIndex(x => !x.Severed); if (i >= 0) m.Wounds.RemoveAt(i); }
            foreach (var w in m.Wounds)
            {
                if (w.Bleed <= 0) continue;
                w.Bleed--;
                m.HP -= 1;
            }
            if (m.HP > 0) return;
            if (Map != null && Map.IsVisible(m.X, m.Y)) Say($"The {m.TheName} bleeds to death.", MessageKind.Kill);
            _killType = DamageType.Physical; _killSneak = false;
            KillMonster(m);
        }

        /// <summary>What the look command adds about a monster's body.</summary>
        void DescribeWounds(Monster m)
        {
            foreach (var w in m.Wounds) Say($"Its {w.Part} is {w.Adjective}.", MessageKind.Info);
            int limp = Bodies.Limp(m);
            if (limp > 0) Say(limp == 1 ? "It is limping." : "It is crawling.", MessageKind.Info);
            if (Bodies.Bleeding(m)) Say("It is bleeding.", MessageKind.Info);
            if (Bodies.Blind(m)) Say("It is blind.", MessageKind.Info);
            if (m.Grounded) Say("It cannot fly.", MessageKind.Info);
            if (m.Disarmed) Say("Its weapon arm is useless.", MessageKind.Info);
        }

        /// <summary>One line per wound (part, adjective, a bleeding mark) for the sheet and the morgue, already in the chosen language.</summary>
        public static List<string> WoundLines(Actor a)
        {
            var list = new List<string>();
            foreach (var w in a.Wounds) list.Add(Loc.T($"{w.Part} {w.Adjective}") + (w.Bleed > 0 ? " " + Loc.T("(bleeding)") : ""));
            return list;
        }

        public static List<string> ScarLines(Player p) => p.Scars.ConvertAll(s => Loc.T(s));
    }
}
