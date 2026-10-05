using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Bodies and wounds (docs/roadmap/depth.md, section 1). A hit that takes a real share of a creature's life lands on a
    /// body part and leaves a wound on top of the HP loss: legs make it limp, arms spoil its aim, a blow to the head stuns,
    /// eyes shorten its sight, and edges make it bleed. The hero's wounds mend over time (badly ones leave a scar);
    /// potions, rest at an inn and the temple speed that up.
    /// </summary>
    public partial class Game
    {
        /// <summary>Called after an attack resolves. Picks the part (one RNG draw), worsens or opens a wound and applies its effect.</summary>
        public void WoundFrom(Actor defender, AttackResult res, bool edged, bool critical)
        {
            if (!res.Hit || res.Killed || defender.HP <= 0) return;
            var plan = Bodies.PlanOf(defender);
            if (plan == null) return;
            int sev = Bodies.Severity(res.Damage, defender.MaxHP, critical);
            if (sev == 0) return;

            int roll = Rng.Range(0, plan.TotalWeight);
            BodyPart part = plan.Parts[0];
            foreach (var bp in plan.Parts) { if (roll < bp.Weight) { part = bp; break; } roll -= bp.Weight; }

            var w = defender.Wounds.Find(x => x.Part == part.Name);
            int before = w?.Severity ?? 0;
            int now = sev > before ? sev : Math.Min(4, before + 1);
            if (now == before) return;
            int limpBefore = Bodies.Limp(defender);
            bool bledBefore = Bodies.Bleeding(defender);
            if (w == null) { w = new Wound { Part = part.Name, Kind = part.Kind }; defender.Wounds.Add(w); }
            w.Severity = now; w.Worst = Math.Max(w.Worst, now); w.Edged = edged; w.HealIn = Bodies.HealTime(now);
            if (edged && Bodies.Bleeds(defender)) w.Bleed = Math.Max(w.Bleed, Bodies.BleedTime(now, part.Kind));

            var m = defender as Monster;
            bool seen = defender.IsPlayer || Mode == GameMode.Overworld || (Map != null && Map.IsVisible(defender.X, defender.Y));
            if (defender.IsPlayer) Say($"Your {part.Name} is {w.Adjective}.", MessageKind.Bad);
            else if (seen) Say($"{defender.Subj}'s {part.Name} is {w.Adjective}.", MessageKind.Combat);

            if (part.Kind == PartKind.Head && now >= 3)
            {
                if (defender.IsPlayer) { Player.StunTurns = Math.Max(Player.StunTurns, 2); Player.Stunned = true; Say("You reel from the blow to your head.", MessageKind.Bad); }
                else if (m != null) { m.HeldTurns = Math.Max(m.HeldTurns, 1); if (seen) Say($"The {m.TheName} reels, stunned.", MessageKind.Combat); }
            }
            int limp = Bodies.Limp(defender);
            if (limp > limpBefore)
            {
                if (defender.IsPlayer) Say(limp == 1 ? "You can only limp now." : "You can barely crawl.", MessageKind.Bad);
                else if (seen && m != null) Say(limp == 1 ? $"The {m.TheName} limps." : $"The {m.TheName} drags itself along.", MessageKind.Combat);
            }
            if (defender.IsPlayer && !bledBefore && w.Bleed > 0) Say("You are bleeding.", MessageKind.Bad);
            if (defender.IsPlayer && part.Kind == PartKind.Eye) UpdateFov();
        }

        /// <summary>A monster's speed after its legs (or wings): three quarters when limping, half when crawling.</summary>
        int MoveSpeed(Monster m)
        {
            int limp = Bodies.Limp(m);
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
                if (--w.HealIn > 0) continue;
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
                if (!Player.Scars.Contains(w.Part)) Player.Scars.Add(w.Part);
                Say($"Your {w.Part} has healed, leaving a scar.", MessageKind.Good);
            }
            else if (w.Worst >= 2) Say($"Your {w.Part} has healed.", MessageKind.Good);
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
            if (m.Def.Regenerates && Turn % 20 == 0) m.Wounds.RemoveAt(0);
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
