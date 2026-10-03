using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core.Magic
{
    public enum School { Evocation, Conjuration, Alteration, Illusion, Necromancy, Sacred, Nature, Shadow }

    /// <summary>
    /// What a spell aims at. Self: no cursor. Monster: a hostile creature. Cell: an empty floor
    /// cell. Area: any visible cell (the effect spreads from it). Line: a direction; the effect
    /// runs from you through the cell. Cone: a direction; the effect fans out from you.
    /// </summary>
    public enum SpellTarget { Self, Monster, Cell, Area, Line, Cone }

    /// <summary>Who a recipe spell hits: the target, everything around a cell, a piercing line, a cone, everything around you, a chain of hops, or a few random targets in view.</summary>
    public enum Shape { None, Single, Ball, Line, Cone, Nova, Chain, Scatter }

    /// <summary>What a recipe spell does to the creatures it hits besides damage.</summary>
    public enum Rider { None, Burn, Slow, Fear, Sleep, Confuse, Blind, Stun, Root, Poison, Bleed, Weaken, Charm }

    /// <summary>A spell as data. Effects live in Game.Magic.cs / Game.Magic.Effects.cs / Game.Magic.Recipes.cs, keyed by <see cref="Id"/>.</summary>
    public sealed class SpellDef
    {
        public string Id, Name, Blurb;
        public int Level;          // 1..5
        public School School;
        public int Cost;           // Mp
        public SpellTarget Target;
        public int Range;          // cells, for every target but Self
        public int Radius;         // Area/Cone/Nova size
        public int Summons;        // creatures called; the cast is refused without room for them

        // ---- Recipe: what the spell does, as numbers. Spells with a hand-written effect leave these empty.
        public DamageType Type = DamageType.Physical;
        public int Dice, Sides = 6, Div = 3, Flat;      // damage: (Dice + level / Div) d Sides + Flat (+ Magic rank)
        public string Verb;                             // "Fire engulfs"
        public Rider Rider; public int RiderPct = 100, RiderTurns = 6;
        public int HealDice, HealSides, HealFlat;
        public string Buff; public int BuffTurns;
        public string SummonDef; public int SummonTurns = 80;
        public SurfaceKind Surface; public int SurfaceTurns;
        public bool HasSurface;
        public int Push;                                // cells knocked back (negative: pulled in)
        public int DrainPct;                            // percent of the damage dealt that you heal
        public int Hops;                                // chain length
        public int Hits;                                // scatter: how many strikes
        public int Corrupt;                             // corruption the cast costs
        public string Special;                          // a hand-coded effect, by name (Game.Magic.Recipes.cs)

        // ---- Look: how the cast is animated (see Fx.cs). HitFx is the strike on each victim for Scatter.
        public FxKind Fx = FxKind.None;
        public Elem Elem = Elem.Arcane;
        public char Glyph = '*';
        public FxKind HitFx = FxKind.None;

        public Shape Shape
        {
            get
            {
                if (Hops > 0) return Shape.Chain;
                if (Hits > 0) return Shape.Scatter;
                switch (Target)
                {
                    case SpellTarget.Monster: return Shape.Single;
                    case SpellTarget.Area: return Shape.Ball;
                    case SpellTarget.Line: return Shape.Line;
                    case SpellTarget.Cone: return Shape.Cone;
                    case SpellTarget.Self: return Radius > 0 && (Dice > 0 || Rider != Rider.None || Push != 0) ? Shape.Nova : Shape.None;
                }
                return Shape.None;
            }
        }

        public SpellDef Dmg(DamageType type, int dice, int sides, int div = 3, int flat = 0, string verb = null) { Type = type; Dice = dice; Sides = sides; Div = div; Flat = flat; Verb = verb; return this; }
        public SpellDef Ride(Rider r, int pct = 100, int turns = 6) { Rider = r; RiderPct = pct; RiderTurns = turns; return this; }
        public SpellDef Heal(int dice, int sides, int flat = 0) { HealDice = dice; HealSides = sides; HealFlat = flat; return this; }
        public SpellDef Aura(string buff, int turns) { Buff = buff; BuffTurns = turns; return this; }
        public SpellDef Call(string def, int count, int turns) { SummonDef = def; Summons = count; SummonTurns = turns; return this; }
        public SpellDef Surf(SurfaceKind kind, int turns = 0) { Surface = kind; SurfaceTurns = turns; HasSurface = true; return this; }
        public SpellDef Shove(int cells) { Push = cells; return this; }
        public SpellDef Drain(int pct) { DrainPct = pct; return this; }
        public SpellDef Chain(int hops) { Hops = hops; return this; }
        public SpellDef Scatter(int hits, FxKind hitFx) { Hits = hits; HitFx = hitFx; return this; }
        public SpellDef Taint(int n) { Corrupt = n; return this; }
        public SpellDef Spec(string id) { Special = id; return this; }
        public SpellDef Look(FxKind fx, Elem elem, char glyph = '*') { Fx = fx; Elem = elem; Glyph = glyph; return this; }
        public SpellDef Say(string verb) { Verb = verb; return this; }
    }

    public static partial class Spells
    {
        static SpellDef S(string id, string name, int level, School school, int cost, SpellTarget target, int range, string blurb, int radius = 0, int summons = 0)
            => new SpellDef { Id = id, Name = name, Level = level, School = school, Cost = cost, Target = target, Range = range, Blurb = blurb, Radius = radius, Summons = summons };

        static SpellDef[] _all;
        static Dictionary<string, SpellDef> _byId;

        /// <summary>Every spell, grouped by school. Built once from the per-school tables.</summary>
        public static SpellDef[] All
        {
            get
            {
                if (_all == null)
                {
                    var l = new List<SpellDef>();
                    l.AddRange(Evocation()); l.AddRange(Conjuration()); l.AddRange(Alteration()); l.AddRange(Illusion());
                    l.AddRange(Necromancy()); l.AddRange(Sacred()); l.AddRange(Nature()); l.AddRange(Shadow());
                    _all = l.ToArray();
                    var d = new Dictionary<string, SpellDef>();
                    foreach (var sp in _all) d[sp.Id] = sp;
                    _byId = d;
                }
                return _all;
            }
        }

        public static SpellDef Find(string id)
        {
            var _ = All;
            return id != null && _byId.TryGetValue(id, out var sp) ? sp : null;
        }

        public static string[] InBook(string bookName)
        {
            foreach (var b in BookList) if (b.Name == bookName) return b.Spells;
            return new string[0];
        }

        /// <summary>Buff ids a spell leaves on the player, for the status bar and expiry messages.</summary>
        public static string BuffLabel(string id)
        {
            var bd = SpellBuffs.Find(id);
            if (bd != null) return bd.Label;
            switch (id)
            {
                case "ward": return "Ward";
                case "stone-skin": return "Stone Skin";
                case "haste": return "Haste";
                case "bless": return "Bless";
                case "invisibility": return "Invisible";
                case "revive": return "Revive";
                case "ossify": return "Ossified";
                case "flame": return "Flame";
                case "levitating": return "Levitate";
                default: return id;
            }
        }

        static int Stat(Player p) => Roles.Find(p.RoleId).MpStat == 'W' ? p.Wis : p.Int;

        /// <summary>Percent chance a cast fizzles: harder spells fail more; the casting stat, Magic skill and light armour help.</summary>
        public static int FailPct(Player p, SpellDef s)
        {
            int pct = 20 + 10 * s.Level - 3 * (Stat(p) - 10) - p.Skills[Skill.Magic] / 4;
            pct -= 5 * p.PerkRank("focus") + p.Gear.SpellFocus;
            if (p.WornArmor != null) pct += p.WornArmor.Def.AC * 2;
            if (p.WornShield != null) pct += p.WornShield.Def.AC * 3;
            return Math.Max(0, Math.Min(95, pct));
        }

        /// <summary>Percent chance to learn a spell from its book in one attempt.</summary>
        public static int LearnPct(Player p, SpellDef s)
        {
            int pct = 70 + 4 * (Stat(p) - 10) + p.Skills[Skill.Magic] / 4 - 18 * (s.Level - 1);
            return Math.Max(5, Math.Min(95, pct));
        }
    }
}
