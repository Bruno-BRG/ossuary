using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Blood, fluids and traces. Wounded things bleed by kind (blood, the ichor of insects and demons, the slime of oozes; the
    /// dead and the made leave nothing), the hero's boots carry blood and mud on as footprints, fire leaves soot, and a crawling
    /// creature leaves drag marks. Everything fades. The trail is read both ways: a bleeding hero is smelled from further off,
    /// and the hero can follow a wounded creature's trail (Shift+M).
    /// </summary>
    public sealed partial class Game
    {
        int _trailX = -1, _trailY = -1;
        bool _scented;

        /// <summary>What a creature leaves when it bleeds, or None.</summary>
        public static StainKind BloodOf(Actor a)
        {
            if (a is Player) return StainKind.Blood;
            if (!(a is Monster m)) return StainKind.None;
            string n = m.Def.Name ?? "";
            if (n.Contains("jelly") || n.Contains("ooze") || n.Contains("mold") || n.Contains("slime") || n.Contains("blob") || n.Contains("pudding")) return StainKind.Slime;
            if (!Bodies.Bleeds(m)) return StainKind.None;
            char g = m.Def.Glyph;
            if (g == 'a' || g == 'S' || g == 's' || g == '&' || g == 'x' || n.Contains("demon") || n.Contains("imp")) return StainKind.Ichor;
            return StainKind.Blood;
        }

        static string WhoseName(Actor a) => a is Player ? "you" : (a as Monster)?.Name ?? "something";

        /// <summary>Leaves a stain on a floor cell; a fresh trail is never painted over by footprints.</summary>
        public void Stain(int x, int y, StainKind kind, string source, int dx = 0, int dy = 0)
        {
            if (kind == StainKind.None || Map == null || Mode == GameMode.Overworld || !CanHoldSurface(x, y)) return;
            var old = Map.StainAt(x, y);
            if (kind == StainKind.Footprints && old.Kind != StainKind.None && old.Kind != StainKind.Footprints && Turn - old.Turn < 60) return;
            Map.Stains[y * Map.W + x] = new Stain { Kind = kind, Turn = Turn, Source = source, Dx = (sbyte)Math.Sign(dx), Dy = (sbyte)Math.Sign(dy) };
        }

        /// <summary>A hit draws blood: a stain under the one hurt, and a spatter beside them on a hard blow (placed by hash, not the Rng).</summary>
        public void Splatter(Actor a, int damage)
        {
            if (a == null || damage <= 0) return;
            var kind = BloodOf(a);
            if (kind == StainKind.None) return;
            Stain(a.X, a.Y, kind, WhoseName(a));
            if (damage >= 6)
            {
                int k = (int)(Rumours.Hash(Rng.Seed, "spatter", Turn * 31 + a.X * 7 + a.Y) % 8);
                Stain(a.X + Pathfinder.Dx8[k], a.Y + Pathfinder.Dy8[k], kind, WhoseName(a));
            }
        }

        /// <summary>Once a turn: trails of the bleeding and the crawling, footprints and mud from the hero's boots, and stains fading.</summary>
        void TickStains()
        {
            if (Map == null || Mode == GameMode.Overworld) return;
            var p = Player;
            int dx = _trailX < 0 ? 0 : p.X - _trailX, dy = _trailY < 0 ? 0 : p.Y - _trailY;
            bool moved = (dx != 0 || dy != 0) && Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1;
            if (Bodies.Bleeding(p)) Stain(p.X, p.Y, StainKind.Blood, "you", dx, dy);
            else if (moved)
            {
                var from = Map.StainAt(_trailX, _trailY);
                bool wetFeet = Map.SurfaceAt(_trailX, _trailY) == SurfaceKind.Water;
                if (wetFeet && Map.SurfaceAt(p.X, p.Y) == SurfaceKind.None) Stain(p.X, p.Y, StainKind.Mud, "you", dx, dy);
                else if ((from.Kind == StainKind.Blood || from.Kind == StainKind.Mud || from.Kind == StainKind.Ichor) && Turn - from.Turn < 200 && Map.StainAt(p.X, p.Y).Kind == StainKind.None)
                    Stain(p.X, p.Y, StainKind.Footprints, "you", dx, dy);
            }
            _trailX = p.X; _trailY = p.Y;
            foreach (var m in Monsters)
            {
                if (m.IsDead || m.Townsperson) continue;
                if (Bodies.Bleeding(m)) Stain(m.X, m.Y, BloodOf(m), m.Name);
                else if (Bodies.Limp(m) >= 2) Stain(m.X, m.Y, StainKind.Drag, m.Name);
            }
            if (Turn % 10 == 0 && Map.Stains.Count > 0)
            {
                var gone = new List<int>();
                foreach (var kv in Map.Stains) if (Turn - kv.Value.Turn > StainInfo.Life(kv.Value.Kind)) gone.Add(kv.Key);
                foreach (int k in gone) Map.Stains.Remove(k);
            }
            if (!Bodies.Bleeding(p)) _scented = false;
        }

        /// <summary>A bleeding hero is smelled: hunting things nearby come, even out of sight. True when this one caught the scent.</summary>
        bool SmellsBlood(Monster m, int dist)
        {
            if (m.Townsperson || m.Ally || m.Def.Mindless || m.Alert == 1 || dist > 10 || !Bodies.Bleeding(Player)) return false;
            m.Alert = 1; m.Dormant = false;
            if (!_scented) { _scented = true; Say("Somewhere, something has caught the scent of your blood.", MessageKind.Warn); }
            return true;
        }

        public static string StainAge(int age) => age < 50 ? "fresh" : age < 200 ? "drying" : "old";

        /// <summary>How a stain reads when looked at: "fresh blood, left by a kobold".</summary>
        public string StainLine(Stain s)
        {
            string who = s.Source == "you" ? "you" : "a " + s.Source;
            return $"{StainAge(Turn - s.Turn)} {StainInfo.Name(s.Kind)}, left by {who}";
        }

        /// <summary>
        /// Follows a wounded creature's trail: one step toward the freshest blood, ichor, slime or drag marks something else left
        /// within twenty squares. Says where it leads.
        /// </summary>
        public bool FollowTrail()
        {
            if (Map == null || Mode != GameMode.Dungeon) { Say("There is no trail to follow here.", MessageKind.Info); return false; }
            int best = -1, bestTurn = int.MinValue;
            foreach (var kv in Map.Stains)
            {
                var s = kv.Value;
                if (!StainInfo.Trail(s.Kind) || s.Source == "you" || Turn - s.Turn > 300) continue;
                int x = kv.Key % Map.W, y = kv.Key / Map.W;
                if (x == Player.X && y == Player.Y) continue;
                if (Pathfinder.Chebyshev(x, y, Player.X, Player.Y) > 20 || !Map.WasSeen(x, y)) continue;
                if (s.Turn > bestTurn) { bestTurn = s.Turn; best = kv.Key; }
            }
            if (best < 0) { Say("You find no trail to follow.", MessageKind.Info); return false; }
            int tx = best % Map.W, ty = best / Map.W;
            var st = Map.Stains[best];
            int sx = Math.Sign(tx - Player.X), sy = Math.Sign(ty - Player.Y);
            Say($"You follow the trail of {StainInfo.Name(st.Kind)} {DirWord(sx, sy)}: a {st.Source}, wounded.", MessageKind.Info);
            Player.GainSkill(Skill.Survival, 1);
            return TryMovePlayer(sx, sy);
        }

        public static string DirWord(int dx, int dy)
        {
            string ns = dy < 0 ? "north" : dy > 0 ? "south" : "";
            string ew = dx < 0 ? "west" : dx > 0 ? "east" : "";
            return ns.Length > 0 && ew.Length > 0 ? ns + "-" + ew : ns + ew;
        }
    }
}
