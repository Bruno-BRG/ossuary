using System;
using System.Collections.Generic;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>
    /// Recording of spell and ability animations (see Fx.cs). The simulation never reads this back: effects are resolved first,
    /// and the host drains the timeline once per frame and hands it to the front end to play.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Off by default (headless runs, soaks, replays): nothing is recorded and nothing costs time.</summary>
        public bool FxEnabled;
        readonly FxTimeline _fx = new FxTimeline();

        /// <summary>The steps recorded since the last call (each a list of map-space cells), and the timeline is emptied.</summary>
        public List<List<FxCell>> DrainFx()
        {
            var steps = new List<List<FxCell>>(_fx.Steps);
            _fx.Clear();
            return steps;
        }

        public int FxPending => _fx.Length;

        /// <summary>The step at which the first projectile of the pending animation lands, or -1 when none flies (read it before <see cref="DrainFx"/>).</summary>
        public int FxImpact => _fx.Impact;

        /// <summary>Runs a recording function when effects are on. It receives the timeline and the step to start at (after whatever is already queued).</summary>
        void Fx(Func<FxTimeline, int, int> record)
        {
            if (!FxEnabled || Map == null) return;
            _fx.Show = (x, y) => Map != null && Map.InBounds(x, y) && (Map.IsVisible(x, y) || (x == Player.X && y == Player.Y));
            record(_fx, _fx.Length);
        }

        /// <summary>The last cell a straight shot from the hero toward (tx,ty) reaches before a wall or the range runs out.</summary>
        (int x, int y) LineEnd(int tx, int ty, int range) => LineEnd(Player.X, Player.Y, tx, ty, range);

        /// <summary>The same from any caster (a monster's line starts at the monster).</summary>
        (int x, int y) LineEnd(int px, int py, int tx, int ty, int range)
        {
            int dx = tx - px, dy = ty - py, steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps == 0) return (px, py);
            int ex = px, ey = py;
            for (int k = 1; k <= range; k++)
            {
                int x = px + (int)Math.Round((double)dx * k / steps), y = py + (int)Math.Round((double)dy * k / steps);
                if (!Map.InBounds(x, y) || !Tiles.Walkable(Map.Get(x, y))) break;
                ex = x; ey = y;
            }
            return (ex, ey);
        }

        /// <summary>Plays the animation a spell is defined with, from the hero toward the cell it was cast on.</summary>
        void PlaySpellFx(SpellDef sp, int tx, int ty) => PlaySpellFx(sp, Player.X, Player.Y, tx, ty);

        /// <summary>The same from any caster: a monster's spell starts at the monster (see Game.Casters.cs).</summary>
        void PlaySpellFx(SpellDef sp, int px, int py, int tx, int ty)
        {
            if (!FxEnabled || sp.Fx == FxKind.None || sp.Fx == FxKind.Custom) return;
            Elem e = sp.Elem;
            int r = Math.Max(1, sp.Radius);
            bool self = sp.Target == SpellTarget.Self;
            if (self) { tx = px; ty = py; }
            Fx((tl, s) =>
            {
                switch (sp.Fx)
                {
                    case FxKind.Bolt: { int a = FxLib.Bolt(tl, s, px, py, tx, ty, e, sp.Glyph); return FxLib.Flash(tl, a, tx, ty, e); }
                    case FxKind.Ball: { int a = FxLib.Bolt(tl, s, px, py, tx, ty, e, sp.Glyph); return FxLib.Burst(tl, a, tx, ty, r, e); }
                    case FxKind.Beam: { (int x, int y) end = sp.Target == SpellTarget.Line ? LineEnd(px, py, tx, ty, sp.Range) : (tx, ty); int a = FxLib.Beam(tl, s, px, py, end.x, end.y, e); return a; }
                    case FxKind.Zap: { (int x, int y) end = sp.Target == SpellTarget.Line ? LineEnd(px, py, tx, ty, sp.Range) : (tx, ty); int a = FxLib.Zap(tl, s, px, py, end.x, end.y, e); return a; }
                    case FxKind.Cone: return FxLib.Cone(tl, s, px, py, tx, ty, Math.Max(3, sp.Radius), e);
                    case FxKind.Wave: return FxLib.Wave(tl, s, px, py, tx, ty, Math.Max(3, sp.Radius), e);
                    case FxKind.Nova: { FxLib.Swirl(tl, s, px, py, e, 2); return FxLib.Nova(tl, s + 1, px, py, r, e); }
                    case FxKind.Meteor: return FxLib.Meteor(tl, s, tx, ty, r, e);
                    case FxKind.Pillar: return FxLib.Pillar(tl, s, tx, ty, e);
                    case FxKind.Rain: return FxLib.Rain(tl, s, tx, ty, r, e, sp.Glyph);
                    case FxKind.Cloud: return FxLib.Cloud(tl, s, tx, ty, r, e);
                    case FxKind.Eruption: { int a = self ? s : FxLib.Bolt(tl, s, px, py, tx, ty, e, sp.Glyph, 3); return FxLib.Eruption(tl, a, tx, ty, r, e, sp.Glyph == '*' ? '^' : sp.Glyph); }
                    case FxKind.Shatter: { int a = self ? s : FxLib.Bolt(tl, s, px, py, tx, ty, e, sp.Glyph); return FxLib.Shatter(tl, a, tx, ty, e); }
                    case FxKind.Slash: { int a = self ? s : FxLib.Bolt(tl, s, px, py, tx, ty, e, sp.Glyph, 3); return FxLib.Slash(tl, a, tx, ty, e); }
                    case FxKind.Rise: return FxLib.Rise(tl, s, tx, ty, e, 3, sp.Glyph);
                    case FxKind.Swirl: { FxLib.Swirl(tl, s, tx, ty, e, 6); return FxLib.Rise(tl, s + 2, tx, ty, e, 2, '·'); }
                    case FxKind.Drain: { int a = FxLib.Drain(tl, s, tx, ty, px, py, e); return FxLib.Flash(tl, a - 3, px, py, e); }
                    case FxKind.Mark: { int a = self ? s : FxLib.Bolt(tl, s, px, py, tx, ty, e, sp.Glyph, 3); return FxLib.Mark(tl, a, tx, ty, e, sp.Glyph); }
                    case FxKind.Teleport: return FxLib.Teleport(tl, s, px, py, tx, ty, e);
                    case FxKind.Implode: return FxLib.Implode(tl, s, tx, ty, Math.Max(2, r), e);
                    case FxKind.Flash: return FxLib.Flash(tl, s, tx, ty, e);
                }
                return s;
            });
        }
    }
}
