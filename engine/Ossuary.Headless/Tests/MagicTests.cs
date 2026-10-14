using System;
using System.Collections.Generic;
using System.Linq;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Magic;
using Ossuary.Desktop;

namespace Ossuary.Tests
{
    /// <summary>
    /// Version 22's magic (docs/roadmap/magic.md): monsters that cast real spells, the Sallow Magister, ice and arcing lightning,
    /// projectiles that land on arrival, and one cast sound per element. Run() is wired into CoreTests.RunAll like the other feature suites.
    /// </summary>
    public static class MagicTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            var old = Loc.Current;
            try
            {
                Loc.Current = Lang.En;
                Test("monster spell pools are recipes that can reach the hero", PoolsAreRecipes);
                Test("a sorcerer casts at the hero and the spell lands", SorcererCasts);
                Test("a caster waits out its cooldown after a cast", CasterCooldown);
                Test("the Sallow Magister waits on Dungeons 7 and casts", MagisterCasts);
                Test("lightning on ice runs along the ice", IceConducts);
                Test("lightning on a wet creature arcs to the next wet one", WetArcs);
                Test("a projectile's effect lands at its arrival step", ImpactAtArrival);
                Test("every element has its own cast sound", ElementCues);
                Test("the save format is version 23", SaveVersion);
            }
            finally { Loc.Current = old; }
            Console.WriteLine($"  magic: {_pass} passed, {_fail} failed");
            if (_fail > 0) throw new Exception($"{_fail} magic asserts failed");
        }

        static void Test(string name, Action body)
        {
            try { body(); _pass++; Console.WriteLine("  ok   " + name); }
            catch (Exception e) { _fail++; Console.WriteLine("  FAIL " + name + ": " + e.Message); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A hero who knows every spell and casts well, so spells rarely fizzle.</summary>
        static Game Mage(ulong seed)
        {
            var g = Game.NewHero(seed, "M", "gnome", "wizard");
            var p = g.Player;
            p.Level = 15; p.Int = 21; p.Wis = 21; p.Skills[Skill.Magic] = 100;
            p.RecomputeMaxMp();
            p.MaxHP = p.HP = 5000;
            foreach (var s in Spells.All) if (!p.Spells.Contains(s.Id)) p.Spells.Add(s.Id);
            g.Monsters.Clear();
            return g;
        }

        static Monster Place(Game g, string kind, int x, int y)
        {
            var m = new Monster(Bestiary.Find(kind), g.Rng) { X = x, Y = y, HomeX = x, HomeY = y };
            g.Monsters.Add(m);
            return m;
        }

        static bool LogHas(Game g, string text)
        {
            foreach (var m in g.Log) if (m.Text.Contains(text)) return true;
            return false;
        }

        /// <summary>Casts until the cast is not a fizzle. Returns false when the cast is refused outright.</summary>
        static bool CastOk(Game g, string id, int x, int y)
        {
            var p = g.Player; int cost = Spells.Find(id).Cost;
            for (int i = 0; i < 80; i++)
            {
                p.Mp = p.MpMax = Math.Max(p.MpMax, 99);
                if (!g.CastSpell(id, x, y)) return false;
                if (p.Mp == p.MpMax - cost) return true;
            }
            return false;
        }

        /// <summary>A visible, walkable cell with a clear line from the hero, at a distance between the two bounds, away from creatures.</summary>
        static bool Spot(Game g, int dmin, int dmax, out int sx, out int sy)
        {
            var p = g.Player;
            for (int d = dmin; d <= dmax; d++)
                for (int dy = -d; dy <= d; dy++)
                    for (int dx = -d; dx <= d; dx++)
                    {
                        int x = p.X + dx, y = p.Y + dy;
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != d) continue;
                        if (!g.Map.InBounds(x, y) || !Tiles.Walkable(g.Map.Get(x, y)) || !g.Map.IsVisible(x, y)) continue;
                        if (g.MonsterAt(x, y) != null || !Fov.HasLine(g.Map, p.X, p.Y, x, y)) continue;
                        sx = x; sy = y;
                        return true;
                    }
            sx = sy = 0;
            return false;
        }

        /// <summary>A visible, walkable cell next to (x, y), not the hero's, free of creatures.</summary>
        static bool Beside(Game g, int x, int y, int skipX, int skipY, out int bx, out int by)
        {
            for (int k = 0; k < 8; k++)
            {
                int nx = x + Pathfinder.Dx8[k], ny = y + Pathfinder.Dy8[k];
                if (nx == skipX && ny == skipY) continue;
                if (!g.Map.InBounds(nx, ny) || !Tiles.Walkable(g.Map.Get(nx, ny)) || !g.Map.IsVisible(nx, ny)) continue;
                if (g.MonsterAt(nx, ny) != null || (nx == g.Player.X && ny == g.Player.Y)) continue;
                bx = nx; by = ny;
                return true;
            }
            bx = by = 0;
            return false;
        }

        /// <summary>
        /// Lets turns pass with one caster held at its spot (it cannot walk up to the hero and melee), until the log reports a cast.
        /// Returns true when it cast within the turns given.
        /// </summary>
        static bool UntilCast(Game g, Monster caster, int x, int y, string words, int turns = 200)
        {
            for (int t = 0; t < turns; t++)
            {
                caster.X = x; caster.Y = y;
                g.Player.HP = g.Player.MaxHP;
                g.EndPlayerTurn();
                if (LogHas(g, words)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ tests

        static void PoolsAreRecipes()
        {
            var ids = MonsterSpells.AllIds().ToList();
            Assert(ids.Count >= 10, "the casters have a real pool of spells");
            foreach (string id in ids)
            {
                var sp = Spells.Find(id);
                Assert(sp != null, id + " is a spell");
                Assert(sp.Dice > 0, id + " deals its damage from data, so a monster can cast it");
                Assert(sp.Hops == 0 && sp.Hits == 0 && sp.Special == null, id + " is a plain recipe, not a hand-coded spell");
                var shape = sp.Shape;
                Assert(shape == Shape.Single || shape == Shape.Ball || shape == Shape.Cone || shape == Shape.Line || shape == Shape.Nova,
                    id + " has a shape a monster can aim at the hero");
            }
            foreach (string name in new[] { "orc shaman", "dark acolyte", "sorcerer" })
            {
                Assert(MonsterSpells.ForCreature(name) != null, name + " casts");
                Assert(Bestiary.All.Any(d => d.Name == name), name + " is in the bestiary");
            }
            Assert(MonsterSpells.ForCreature("jackal") == null, "a jackal casts nothing");
            Assert(MonsterSpells.ForBoss("sallow-magister", true).Length > MonsterSpells.ForBoss("sallow-magister", false).Length, "the second phase adds spells");
            Assert(MonsterSpells.ForBoss("gaoler", false) == null, "the other bosses keep their own habits");
        }

        static void SorcererCasts()
        {
            var g = Mage(8201);
            Assert(Spot(g, 3, 4, out int x, out int y), "a spot in view");
            var s = Place(g, "sorcerer", x, y); s.Alert = 1;
            g.Player.HP = g.Player.MaxHP = 5000;
            Assert(UntilCast(g, s, x, y, "The sorcerer casts "), "the sorcerer casts within a few turns");
            Assert(g.Player.HP < 5000, "the spell hurts the hero");
        }

        static void CasterCooldown()
        {
            var g = Mage(8202);
            Assert(Spot(g, 3, 4, out int x, out int y), "a spot in view");
            var s = Place(g, "sorcerer", x, y); s.Alert = 1;
            g.Player.HP = g.Player.MaxHP = 5000;
            Assert(UntilCast(g, s, x, y, "The sorcerer casts "), "a cast to start from");
            Assert(s.CastCooldown == MonsterSpells.Cooldown, "the cast sets the cooldown");
        }

        static void MagisterCasts()
        {
            var b = Bosses.ForLevel("The Dungeons", 7);
            Assert(b != null && b.Id == "sallow-magister" && b.Name == "Sallow Magister", "the boss waits on Dungeons 7");
            var g = Mage(8203);
            Assert(Spot(g, 3, 4, out int x, out int y), "a spot in view");
            var boss = g.CreateBoss(b, x, y);
            g.Monsters.Add(boss);
            boss.Alert = 1;
            Assert(boss.HP == 100 && boss.BossId == "sallow-magister", "the boss has its own health and id");
            g.Player.HP = g.Player.MaxHP = 5000;
            Assert(UntilCast(g, boss, x, y, "The Sallow Magister casts "), "the boss casts within a few turns");
        }

        static void IceConducts()
        {
            var g = Mage(8204);
            Assert(Spot(g, 3, 4, out int x, out int y), "a spot in view");
            var a = Place(g, "ogre", x, y); a.HP = a.MaxHP = 4000; a.Speed = 1;
            Assert(Beside(g, x, y, g.Player.X, g.Player.Y, out int bx, out int by), "a cell beside it");
            var b = Place(g, "ogre", bx, by); b.HP = b.MaxHP = 4000; b.Speed = 1;
            g.Map.SetSurface(x, y, SurfaceKind.Ice, 50);
            g.Map.SetSurface(bx, by, SurfaceKind.Ice, 50);
            Assert(CastOk(g, "thunderstrike", x, y), "the bolt lands");
            Assert(a.HP < 4000, "the struck creature takes the bolt");
            Assert(b.HP < 4000, "the shock runs along the ice to the creature beside it");
            Assert(LogHas(g, "along the ice"), "and the log says so");
        }

        static void WetArcs()
        {
            var g = Mage(8205);
            Assert(Spot(g, 3, 4, out int x, out int y), "a spot in view");
            var a = Place(g, "ogre", x, y); a.HP = a.MaxHP = 4000; a.Speed = 1; a.WetTurns = 50;
            Assert(Beside(g, x, y, g.Player.X, g.Player.Y, out int bx, out int by), "a cell beside it");
            var b = Place(g, "ogre", bx, by); b.HP = b.MaxHP = 4000; b.Speed = 1; b.WetTurns = 50;
            // A dry creature two cells off: the arc passes it over, because only wet creatures take it.
            int cx = -1, cy = -1;
            for (int k = 0; k < 8 && cx < 0; k++)
            {
                int nx = bx + Pathfinder.Dx8[k], ny = by + Pathfinder.Dy8[k];
                if (nx == x && ny == y) continue;
                if (!g.Map.InBounds(nx, ny) || !Tiles.Walkable(g.Map.Get(nx, ny)) || !g.Map.IsVisible(nx, ny) || g.MonsterAt(nx, ny) != null) continue;
                if (nx == g.Player.X && ny == g.Player.Y) continue;
                if (Pathfinder.Chebyshev(x, y, nx, ny) > 3) continue;
                cx = nx; cy = ny;
            }
            Assert(cx >= 0, "a dry cell in reach of the arc");
            var c = Place(g, "ogre", cx, cy); c.HP = c.MaxHP = 4000; c.Speed = 1;
            Assert(CastOk(g, "thunderstrike", x, y), "the bolt lands on the wet creature");
            Assert(a.HP < 4000, "the wet creature takes the bolt");
            Assert(b.HP < 4000, "the lightning arcs to the wet creature beside it");
            Assert(c.HP == 4000, "and not to the dry one");
        }

        static void ImpactAtArrival()
        {
            var g = Mage(8206);
            g.FxEnabled = true;
            Assert(Spot(g, 4, 6, out int x, out int y), "a spot in view");
            Assert(CastOk(g, "fireball", x, y), "the fireball is cast");
            int hit = g.FxImpact;
            var steps = g.DrainFx();
            Assert(hit > 0 && hit < steps.Count, $"the fireball lands after its flight (impact step {hit} of {steps.Count})");
            Assert(steps[hit].Count > 0, "the burst starts where the fireball lands");
            Assert(g.FxImpact == -1, "draining clears the impact");

            var h = Mage(8207);
            h.FxEnabled = true;
            Assert(CastOk(h, "thunderclap", h.Player.X, h.Player.Y), "the nova is cast");
            Assert(h.FxImpact == -1, "a nova has no projectile, so it lands at once");
            h.DrainFx();
        }

        static void ElementCues()
        {
            var cues = new HashSet<string>();
            foreach (Elem e in Enum.GetValues(typeof(Elem))) cues.Add(Game.SpellCue(e));
            Assert(cues.Count == 14, "fourteen elements, fourteen cast sounds");
            Assert(!cues.Contains("magic"), "the old generic cue is gone");
            var g = Mage(8208);
            Assert(Spot(g, 3, 4, out int x, out int y), "a spot in view");
            Assert(CastOk(g, "fireball", x, y), "the fireball is cast");
            Assert(g.DrainCues(14).Contains("fire"), "casting a fire spell sounds the fire cue");
        }

        static void SaveVersion()
        {
            Assert(new SaveData().Version == 23, "a save made now is format 23");
        }
    }
}
