using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Surfaces and elemental status: fire spreading through brush and oil, ice that makes creatures slip,
    /// water that carries lightning and puts out flames, burning and soaking as conditions.
    /// </summary>
    public sealed partial class Game
    {
        public SurfaceKind SurfaceAt(int x, int y) => Map == null ? SurfaceKind.None : Map.SurfaceAt(x, y);

        /// <summary>Only plain floor holds a surface; doors, stairs, altars and walls do not.</summary>
        bool CanHoldSurface(int x, int y)
        {
            if (Map == null || !Map.InBounds(x, y)) return false;
            var t = Map.Get(x, y);
            return t == TileKind.Floor || t == TileKind.FloorAlt;
        }

        // ------------------------------------------------------------ placing

        /// <summary>Lays a surface down, applying the interactions: fire does not take on water and melts ice, and so on.</summary>
        public void PutSurface(int x, int y, SurfaceKind kind, int turns = 0)
        {
            if (!CanHoldSurface(x, y)) return;
            var cur = Map.SurfaceAt(x, y);
            switch (kind)
            {
                case SurfaceKind.Fire:
                    if (cur == SurfaceKind.Water) { Douse(x, y); return; }                 // steam, and nothing burns
                    if (cur == SurfaceKind.Ice) { Map.SetSurface(x, y, SurfaceKind.Water, 0); Douse(x, y); return; }
                    if (cur == SurfaceKind.Oil) turns = Math.Max(turns, 10);
                    else if (cur == SurfaceKind.Grass) turns = Math.Max(turns, 5);
                    Map.SetSurface(x, y, SurfaceKind.Fire, Math.Max(2, turns));
                    ExposeToFire(x, y);
                    return;
                case SurfaceKind.Water:
                    Map.SetSurface(x, y, SurfaceKind.Water, turns);
                    Douse(x, y);
                    return;
                case SurfaceKind.Ice:
                    if (cur == SurfaceKind.Water) Map.SetSurface(x, y, SurfaceKind.Ice, turns > 0 ? turns : 40);
                    else if (cur == SurfaceKind.Fire) Map.SetSurface(x, y, SurfaceKind.None);
                    return;
                default:
                    if (cur == SurfaceKind.Fire) return;
                    Map.SetSurface(x, y, kind, turns);
                    return;
            }
        }

        /// <summary>Anything standing in a doused cell is soaked and stops burning.</summary>
        void Douse(int x, int y)
        {
            var a = ActorAt(x, y);
            if (a == null) return;
            a.WetTurns = Math.Max(a.WetTurns, 4);
            if (a.BurnTurns > 0) { a.BurnTurns = 0; if (a.IsPlayer) Say("The water puts you out.", MessageKind.Good); }
        }

        Actor ActorAt(int x, int y)
        {
            if (Player.X == x && Player.Y == y) return Player;
            return MonsterAt(x, y);
        }

        void ExposeToFire(int x, int y)
        {
            var a = ActorAt(x, y);
            if (a != null) SetAlight(a);
        }

        // ---------------------------------------------------------- creatures

        /// <summary>Resistance in percent for a monster. The dead ignore poison and half-ignore cold; some are made of fire or frost.</summary>
        public int MonsterResist(Monster m, DamageType t)
        {
            string n = m.Def.Name;
            switch (t)
            {
                case DamageType.Poison: return m.Def.Undead || m.Def.Skeleton || n.EndsWith(" mold", StringComparison.Ordinal) || n == "gas spore" ? 100 : 0;
                case DamageType.Cold:
                    if (n == "ice troll" || n == "lich" || n == "wandering wraith") return 100;
                    if (n == "fire giant") return -50;
                    return m.Def.Undead ? 50 : 0;
                case DamageType.Fire:
                    if (n == "fire giant" || n == "tiamat") return 100;
                    if (n == "ice troll") return -50;
                    return 0;
                default: return 0;
            }
        }

        /// <summary>Sets a creature on fire unless it is soaked or immune. Fire spreads to flammable ground under it.</summary>
        public void SetAlight(Actor a)
        {
            if (a.WetTurns > 0 || a.BurnTurns > 0) return;
            if (a is Player p)
            {
                if (p.ResistPct(DamageType.Fire) >= 90) return;
                a.BurnTurns = 4;
                Say("You catch fire!", MessageKind.Bad);
            }
            else if (a is Monster m)
            {
                if (MonsterResist(m, DamageType.Fire) >= 100) return;
                a.BurnTurns = 4;
                Say($"The {m.TheName} catches fire!", MessageKind.Good);
            }
            if (a.BurnTurns > 0) Cauterise(a);
            if (Map != null && SurfaceInfo.Flammable(Map.SurfaceAt(a.X, a.Y))) PutSurface(a.X, a.Y, SurfaceKind.Fire, 4);
        }

        /// <summary>Damage from burning, shock and the like, without the per-hit message. Credits kills to the player.</summary>
        void ElementalDamage(Monster m, int dmg, DamageType type)
        {
            int res = MonsterResist(m, type);
            if (res >= 100) return;
            dmg = Math.Max(1, dmg * (100 - res) / 100);
            m.HP -= dmg;
            if (m.HP <= 0)
            {
                Say($"The {m.TheName} {(type == DamageType.Fire ? "burns to death" : "is cooked by the current")}.", MessageKind.Kill);
                _killType = type;
                KillMonster(m);
            }
        }

        // -------------------------------------------------------------- lightning

        /// <summary>
        /// A shock in water runs through every connected water cell within reach: whoever stands in it
        /// pays, the caster included.
        /// </summary>
        void Conduct(int cx, int cy, int dmg, Monster skip)
        {
            if (SurfaceAt(cx, cy) != SurfaceKind.Water) return;
            var seen = new HashSet<int> { cy * Map.W + cx };
            var queue = new Queue<int>(); queue.Enqueue(cy * Map.W + cx);
            var hit = new List<Monster>();
            bool hitPlayer = false;
            while (queue.Count > 0 && seen.Count < 60)
            {
                int idx = queue.Dequeue(); int x = idx % Map.W, y = idx / Map.W;
                var m = MonsterAt(x, y);
                if (m != null && m != skip && !m.IsDead) hit.Add(m);
                if (Player.X == x && Player.Y == y) hitPlayer = true;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + Pathfinder.Dx8[k], ny = y + Pathfinder.Dy8[k];
                    if (!Map.InBounds(nx, ny) || Math.Max(Math.Abs(nx - cx), Math.Abs(ny - cy)) > 4) continue;
                    int ni = ny * Map.W + nx;
                    if (seen.Contains(ni) || Map.SurfaceAt(nx, ny) != SurfaceKind.Water) continue;
                    seen.Add(ni); queue.Enqueue(ni);
                }
            }
            if (hit.Count == 0 && !hitPlayer) return;
            Say("The shock runs through the water!", MessageKind.Combat);
            foreach (var m in hit) { if (m.Ally) continue; ElementalDamage(m, dmg, DamageType.Lightning); }
            if (hitPlayer)
            {
                int d = Player.ResistDamage(dmg, DamageType.Lightning);
                Player.HP -= d; HurtBy("an electric shock");
                Say($"The current shocks you for {d}!", MessageKind.Bad);
            }
        }

        // ------------------------------------------------------------ the tick

        /// <summary>Once per turn on the current level: fire spreads and dies, ice melts, puddles dry, creatures are exposed.</summary>
        void TickSurfaces()
        {
            if (Map == null) return;
            var p = Player;
            if (Map.SurfaceAt(p.X, p.Y) == SurfaceKind.Water) Douse(p.X, p.Y);
            if (Map.Surfaces.Count > 0)
            {
                var keys = new List<int>(Map.Surfaces.Keys);
                keys.Sort();
                foreach (int idx in keys)
                {
                    if (!Map.Surfaces.TryGetValue(idx, out var s)) continue;
                    int x = idx % Map.W, y = idx / Map.W;
                    switch (s.Kind)
                    {
                        case SurfaceKind.Fire:
                            ExposeToFire(x, y);
                            for (int k = 0; k < 8; k += 2)   // cardinal neighbours only
                            {
                                int nx = x + Pathfinder.Dx8[k], ny = y + Pathfinder.Dy8[k];
                                var ns = Map.SurfaceAt(nx, ny);
                                if (SurfaceInfo.Flammable(ns) && Rng.Chance(ns == SurfaceKind.Oil ? 85 : 70)) PutSurface(nx, ny, SurfaceKind.Fire, 4);
                                else if (ns == SurfaceKind.Ice && Rng.Chance(40)) PutSurface(nx, ny, SurfaceKind.Fire, 2);
                            }
                            if (--s.Turns <= 0) { Map.SetSurface(x, y, SurfaceKind.None); Stain(x, y, StainKind.Soot, "fire"); }
                            else Map.Surfaces[idx] = s;
                            break;
                        case SurfaceKind.Ice:
                            if (s.Turns > 0 && --s.Turns == 0) Map.SetSurface(x, y, SurfaceKind.Water, 0);
                            else Map.Surfaces[idx] = s;
                            break;
                        case SurfaceKind.Water:
                            if (s.Turns > 0)
                            {
                                if (--s.Turns == 0) Map.SetSurface(x, y, SurfaceKind.None);
                                else Map.Surfaces[idx] = s;
                            }
                            break;
                    }
                }
                foreach (var m in Monsters)
                    if (!m.IsDead && Map.SurfaceAt(m.X, m.Y) == SurfaceKind.Water) Douse(m.X, m.Y);
            }

            // Burning and soaking run down.
            if (p.WetTurns > 0 && Map.SurfaceAt(p.X, p.Y) != SurfaceKind.Water) p.WetTurns--;
            if (p.BurnTurns > 0)
            {
                if (p.WetTurns > 0 || Map.SurfaceAt(p.X, p.Y) == SurfaceKind.Water) { p.BurnTurns = 0; Say("The flames go out.", MessageKind.Good); }
                else
                {
                    int d = p.ResistDamage(Rng.Range(1, 4), DamageType.Fire);
                    p.HP -= d; HurtBy("burning");
                    Say($"You burn! (-{d})", MessageKind.Bad);
                    if (--p.BurnTurns == 0) Say("The flames die down.", MessageKind.Info);
                }
            }
            for (int i = Monsters.Count - 1; i >= 0; i--)
            {
                var m = Monsters[i];
                if (m.IsDead) continue;
                if (m.WetTurns > 0 && Map.SurfaceAt(m.X, m.Y) != SurfaceKind.Water) m.WetTurns--;
                if (m.BurnTurns <= 0) continue;
                if (m.WetTurns > 0) { m.BurnTurns = 0; continue; }
                m.BurnTurns--;
                ElementalDamage(m, Rng.Range(1, 4), DamageType.Fire);
                if (!m.IsDead && SurfaceInfo.Flammable(Map.SurfaceAt(m.X, m.Y))) PutSurface(m.X, m.Y, SurfaceKind.Fire, 4);
            }
        }

        /// <summary>Monsters keep out of flames they would not survive.</summary>
        bool AvoidsCell(Monster m, int x, int y) =>
            Map.SurfaceAt(x, y) == SurfaceKind.Fire && MonsterResist(m, DamageType.Fire) < 100;

        // ------------------------------------------------------------ spell shapes

        /// <summary>Burns the ground around a blast: flammable cells always, bare floor too when <paramref name="all"/>.</summary>
        void ScorchGround(int cx, int cy, int radius, bool all)
        {
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (!CanHoldSurface(x, y) || !Map.IsVisible(x, y)) continue;
                    if (all || SurfaceInfo.Flammable(Map.SurfaceAt(x, y))) PutSurface(x, y, SurfaceKind.Fire, all ? 4 : 5);
                }
        }

        /// <summary>The player's own cell after a surface changed under it.</summary>
        public string SurfaceNote(int x, int y)
        {
            var k = SurfaceAt(x, y);
            return k == SurfaceKind.None ? "" : SurfaceInfo.Name(k);
        }
    }
}
