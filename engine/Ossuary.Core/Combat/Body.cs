using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>What a body part does when it is hurt.</summary>
    public enum PartKind { Head, Torso, Arm, Leg, Wing, Tail, Eye }

    public sealed class BodyPart
    {
        public readonly string Name;
        public readonly PartKind Kind;
        public readonly int Weight;
        /// <summary>The arm that holds the weapon (the hero's right arm): its wounds cost the most to-hit.</summary>
        public readonly bool Main;
        public BodyPart(string name, PartKind kind, int weight, bool main = false) { Name = name; Kind = kind; Weight = weight; Main = main; }
    }

    /// <summary>
    /// A body plan: the parts a hit can land on, weighted by size, and what the creature moves with. Monsters get theirs
    /// from their glyph (see <see cref="Bodies.PlanFor"/>), so a new bestiary entry needs no extra data.
    /// </summary>
    public sealed class BodyPlan
    {
        public readonly string Id;
        public readonly BodyPart[] Parts;
        /// <summary>Legs for walkers, wings for fliers: crippling enough of these makes the creature limp.</summary>
        public readonly PartKind Moves;
        public readonly int TotalWeight;
        public BodyPlan(string id, PartKind moves, params BodyPart[] parts)
        {
            Id = id; Moves = moves; Parts = parts;
            foreach (var p in parts) TotalWeight += p.Weight;
        }

        public int CountMovers() { int n = 0; foreach (var p in Parts) if (p.Kind == Moves) n++; return n; }

        public BodyPart Find(string name) { foreach (var p in Parts) if (p.Name == name) return p; return null; }
    }

    /// <summary>One injury on one part. Severity 1 (grazed/bruised) to 4 (mangled/crushed).</summary>
    public sealed class Wound
    {
        public string Part;
        public PartKind Kind;
        public int Severity;
        /// <summary>The worst this wound has been; 3 or more leaves a scar when it heals.</summary>
        public int Worst;
        /// <summary>Cut by an edge or point (bleeds; "cut", "torn") rather than struck by something blunt ("battered", "broken").</summary>
        public bool Edged;
        public int Bleed;
        /// <summary>Turns until it mends by itself (the hero only).</summary>
        public int HealIn;
        /// <summary>Cut clean off (monsters only): it never mends and counts as the worst wound there is.</summary>
        public bool Severed;
        /// <summary>Bound with a bandage: it stopped bleeding and mends twice as fast.</summary>
        public bool Bound;

        public string Adjective => Severed ? "severed" : Bodies.Adjective(Severity, Edged);
    }

    public static class Bodies
    {
        static BodyPart P(string n, PartKind k, int w, bool main = false) => new BodyPart(n, k, w, main);

        public static readonly BodyPlan Humanoid = new BodyPlan("humanoid", PartKind.Leg,
            P("head", PartKind.Head, 10), P("torso", PartKind.Torso, 36),
            P("right arm", PartKind.Arm, 12, true), P("left arm", PartKind.Arm, 12),
            P("right leg", PartKind.Leg, 13), P("left leg", PartKind.Leg, 13),
            P("right eye", PartKind.Eye, 2), P("left eye", PartKind.Eye, 2));

        public static readonly BodyPlan Quadruped = new BodyPlan("quadruped", PartKind.Leg,
            P("head", PartKind.Head, 14), P("body", PartKind.Torso, 36),
            P("front left leg", PartKind.Leg, 9), P("front right leg", PartKind.Leg, 9),
            P("hind left leg", PartKind.Leg, 9), P("hind right leg", PartKind.Leg, 9),
            P("tail", PartKind.Tail, 6), P("right eye", PartKind.Eye, 2), P("left eye", PartKind.Eye, 2));

        public static readonly BodyPlan Insect = new BodyPlan("insect", PartKind.Leg,
            P("head", PartKind.Head, 16), P("thorax", PartKind.Torso, 30), P("abdomen", PartKind.Torso, 24),
            P("foreleg", PartKind.Leg, 10), P("middle leg", PartKind.Leg, 10), P("hind leg", PartKind.Leg, 10));

        public static readonly BodyPlan Winged = new BodyPlan("winged", PartKind.Wing,
            P("head", PartKind.Head, 16), P("body", PartKind.Torso, 36),
            P("left wing", PartKind.Wing, 20), P("right wing", PartKind.Wing, 20),
            P("right eye", PartKind.Eye, 4), P("left eye", PartKind.Eye, 4));

        public static readonly BodyPlan Dragon = new BodyPlan("dragon", PartKind.Leg,
            P("head", PartKind.Head, 12), P("body", PartKind.Torso, 34),
            P("front left leg", PartKind.Leg, 8), P("front right leg", PartKind.Leg, 8),
            P("hind left leg", PartKind.Leg, 8), P("hind right leg", PartKind.Leg, 8),
            P("left wing", PartKind.Wing, 8), P("right wing", PartKind.Wing, 8),
            P("tail", PartKind.Tail, 6));

        /// <summary>Snakes move with the length behind the head: a broken tail leaves them crawling.</summary>
        public static readonly BodyPlan Serpent = new BodyPlan("serpent", PartKind.Tail,
            P("head", PartKind.Head, 18), P("body", PartKind.Torso, 46), P("tail", PartKind.Tail, 32),
            P("right eye", PartKind.Eye, 2), P("left eye", PartKind.Eye, 2));

        public static readonly BodyPlan[] All = { Humanoid, Quadruped, Insect, Winged, Dragon, Serpent };

        /// <summary>The plan of an actor; null for things with no body to break (moulds, eyes, wraiths, elementals, swarms, illusions).</summary>
        public static BodyPlan PlanOf(Actor a) => a is Monster m ? PlanFor(m.Def) : Humanoid;

        public static BodyPlan PlanFor(MonsterDef d)
        {
            string n = d.Name ?? "";
            if (n.Contains("swarm") || n.Contains("mirror image") || n.Contains("shadow double") || n.Contains("spectral") || n.Contains("spiritual")
                || n.Contains("elemental") || n.Contains("wraith") || n == "werenothing") return null;
            if (n == "grid bug") return Insect;
            if (n.Contains("snake") || n.Contains("viper") || n.Contains("python") || n.Contains("adder") || n.Contains("serpent")) return Serpent;
            switch (d.Glyph)
            {
                case '*': case '/': case 'e': case 'F': case 'j': case 'b': case 'P': case 'v': return null;
                case 'd': case 'q': case 'r': case 'x': case 'f': case 'C': case 'u': return Quadruped;
                case 'S': case 'a': case 's': return Insect;
                case 'B': return Winged;
                case 'D': return Dragon;
                default: return Humanoid;
            }
        }

        /// <summary>Bone, stone and the dead do not bleed.</summary>
        public static bool Bleeds(Actor a)
        {
            if (!(a is Monster m)) return true;
            var d = m.Def;
            return !(d.Undead || d.Skeleton || d.Glyph == 'g' || (d.Name ?? "").Contains("treant") || (d.Name ?? "").Contains("golem") || (d.Name ?? "").Contains("sentinel"));
        }

        static readonly string[] EdgedWords = { "", "grazed", "cut", "torn", "mangled" };
        static readonly string[] BluntWords = { "", "bruised", "battered", "broken", "crushed" };
        public static IEnumerable<string> AllAdjectives() { for (int i = 1; i <= 4; i++) { yield return EdgedWords[i]; yield return BluntWords[i]; } yield return "severed"; }
        public static string Adjective(int severity, bool edged) => (edged ? EdgedWords : BluntWords)[Math.Max(1, Math.Min(4, severity))];

        /// <summary>
        /// How bad a hit is, from the share of the defender's life it took: under 15% leaves no lasting mark, then
        /// grazed (15%), cut (30%), torn (50%), mangled (75%). A critical counts as 15 points more.
        /// </summary>
        public static int Severity(int damage, int maxHp, bool critical)
        {
            int pct = damage * 100 / Math.Max(1, maxHp) + (critical ? 15 : 0);
            if (pct < 15) return 0;
            if (pct < 30) return 1;
            if (pct < 50) return 2;
            if (pct < 75) return 3;
            return 4;
        }

        public static bool EdgedAttack(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Bite: case AttackKind.Claw: case AttackKind.ClawOrBite: case AttackKind.Pierce:
                case AttackKind.PierceOrHit: case AttackKind.PierceOrClaw: case AttackKind.HitOrClaw: return true;
                default: return false;
            }
        }

        /// <summary>Touches, drains and blasts hurt without breaking anything in particular.</summary>
        public static bool Wounding(AttackKind k) => k != AttackKind.Touch && k != AttackKind.Drain && k != AttackKind.Explode;

        static readonly string[] BluntWeapons = { "mace", "club", "hammer", "staff", "flail", "morning star", "aklys", "maul", "cudgel" };
        static readonly string[] PiercingWeapons = { "dagger", "spear", "trident", "rapier", "stiletto", "pike", "lance", "kris" };

        /// <summary>A weapon that drives in rather than slicing or crushing: its hard blows leave deep wounds.</summary>
        public static bool PiercingWeapon(Entities.Player p)
        {
            if (p.Wielded == null) return false;
            string n = p.Wielded.Def.Name ?? "";
            foreach (var w in PiercingWeapons) if (n.Contains(w)) return true;
            return false;
        }

        public static bool EdgedWeapon(Entities.Player p)
        {
            if (p.Wielded == null) return false;
            string n = p.Wielded.Def.Name ?? "";
            foreach (var b in BluntWeapons) if (n.Contains(b)) return false;
            return true;
        }

        public static Wound Worst(Actor a, PartKind kind)
        {
            Wound best = null;
            foreach (var w in a.Wounds) if (w.Kind == kind && (best == null || w.Severity > best.Severity)) best = w;
            return best;
        }

        /// <summary>0 = walks fine, 1 = limping (a quarter slower), 2 = crawling (half speed). Counts broken legs, or wings for fliers.</summary>
        public static int Limp(Actor a)
        {
            if (a.Wounds.Count == 0) return 0;
            var plan = PlanOf(a);
            if (plan == null) return 0;
            int movers = plan.CountMovers(), bad = 0;
            foreach (var w in a.Wounds) if (w.Kind == plan.Moves && w.Severity >= 3) bad++;
            if (movers == 0 || bad == 0) return 0;
            return Math.Min(2, bad * 2 / movers);
        }

        /// <summary>To-hit lost to hurt arms. The weapon arm counts in full, the other at half; monsters use their worst arm.</summary>
        public static int ArmPenalty(Actor a)
        {
            int pen = 0;
            foreach (var w in a.Wounds)
            {
                if (w.Kind != PartKind.Arm || w.Severity < 2) continue;
                int p = w.Severity == 2 ? 1 : w.Severity == 3 ? 2 : 4;
                if (a.IsPlayer) { var bp = Humanoid.Find(w.Part); if (bp == null || !bp.Main) p /= 2; pen += p; }
                else pen = Math.Max(pen, p);
            }
            return pen;
        }

        /// <summary>Sight lost to hurt eyes: two squares for each eye that is cut or worse.</summary>
        public static int EyePenalty(Actor a)
        {
            int n = 0;
            foreach (var w in a.Wounds) if (w.Kind == PartKind.Eye && w.Severity >= 2) n += 2;
            return n;
        }

        public static bool Bleeding(Actor a) { foreach (var w in a.Wounds) if (w.Bleed > 0) return true; return false; }

        /// <summary>Every eye it has is cut or worse: it fights by sound and swings wild.</summary>
        public static bool Blind(Actor a)
        {
            var plan = PlanOf(a);
            if (plan == null || a.Wounds.Count == 0) return false;
            int eyes = 0, bad = 0;
            foreach (var p in plan.Parts) if (p.Kind == PartKind.Eye) eyes++;
            foreach (var w in a.Wounds) if (w.Kind == PartKind.Eye && w.Severity >= 2) bad++;
            return eyes > 0 && bad >= eyes;
        }

        /// <summary>Does this plan have a part of that kind to aim at?</summary>
        public static bool Has(BodyPlan plan, PartKind kind) { if (plan != null) foreach (var p in plan.Parts) if (p.Kind == kind) return true; return false; }

        /// <summary>What a called shot costs to hit: the smaller the part, the harder.</summary>
        public static int AimPenalty(PartKind? aim)
        {
            if (aim == null) return 0;
            switch (aim.Value)
            {
                case PartKind.Eye: return 5;
                case PartKind.Head: return 3;
                case PartKind.Torso: return 0;
                default: return 2;
            }
        }

        /// <summary>The parts a called shot cycles through, in order (null = wherever it lands).</summary>
        public static readonly PartKind?[] AimCycle = { null, PartKind.Head, PartKind.Arm, PartKind.Leg, PartKind.Eye, PartKind.Wing, PartKind.Tail };

        public static string AimName(PartKind? aim) => aim == null ? "anywhere" : aim.Value == PartKind.Eye ? "the eyes" : aim.Value == PartKind.Arm ? "the arms"
            : aim.Value == PartKind.Leg ? "the legs" : aim.Value == PartKind.Wing ? "the wings" : aim.Value == PartKind.Tail ? "the tail" : "the head";

        /// <summary>Turns a wound of this severity takes to mend by itself.</summary>
        public static int HealTime(int severity) => severity <= 1 ? 80 : severity == 2 ? 250 : severity == 3 ? 900 : 2000;

        /// <summary>Turns a fresh cut bleeds: deeper cuts and wounds to the trunk bleed longer.</summary>
        public static int BleedTime(int severity, PartKind kind) => severity < 2 ? 0 : (severity == 2 ? 3 : severity == 3 ? 5 : 8) + (kind == PartKind.Torso ? 2 : 0);
    }
}
