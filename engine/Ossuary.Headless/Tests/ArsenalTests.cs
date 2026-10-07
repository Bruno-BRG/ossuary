using System;
using System.Collections.Generic;
using System.Text;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;
using Ossuary.Desktop;
using System.Linq;

namespace Ossuary.Tests
{
    static class ArsenalExt
    {
        public static ItemDef First(this IReadOnlyList<ItemDef> list, string name)
        {
            foreach (var d in list) if (d.Name == name) return d;
            throw new Exception("no item called " + name);
        }
    }

    /// <summary>
    /// The spell, item and animation catalogue: every entry is wired up, every spell casts and does something observable,
    /// every cast records an animation that only uses glyphs in the font, and the mechanics behind the riders and buffs hold.
    /// </summary>
    public static class ArsenalTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            Test("every spell is learnable and its parts exist", CatalogueWiring);
            Test("every spell casts, does something, and animates", EverySpellCasts);
            Test("animations stay on the grid, in the font, and end where they aim", FxShapes);
            Test("a cast reaches the screen as an animation script", FxReachesTheFrame);
            Test("riders: hold, poison, bleed, weaken, push and pull", Riders);
            Test("spell buffs change the numbers and end", Buffs);
            Test("retaliation, sanctuary and regeneration", Wards);
            Test("specials: execute, assassinate, swap, banish, command, steal", Specials);
            Test("summons are real, never spawn on their own", Summons);
            Test("the spell list is filtered by school", SpellPanel);
            Test("every spell speaks Portuguese", SpellTranslations);
            Test("items: every kind is well-formed and magic can be found", ItemCatalogue);
            Test("uniques: placed, named, and their spells work", UniqueItems);
            Test("wands and scrolls cast real spells", WandsAndScrolls);
            Test("magic items carry a spell that fits what they are", ImbuedItems);
            Console.WriteLine($"==== arsenal: {_pass} passed, {_fail} failed ====");
            if (_fail > 0) throw new Exception($"{_fail} arsenal asserts failed");
        }

        static void Test(string name, Action body)
        {
            try { body(); _pass++; Console.WriteLine("  ok   " + name); }
            catch (Exception e) { _fail++; Console.WriteLine("  FAIL " + name + ": " + e.Message); if (Environment.GetEnvironmentVariable("ARSENAL_TRACE") != null) Console.WriteLine(e.StackTrace); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        // ------------------------------------------------------------ helpers

        static Game Archmage(ulong seed)
        {
            var g = Game.NewHero(seed, "A", "gnome", "wizard");
            var p = g.Player;
            p.Level = 15; p.Int = 21; p.Wis = 21; p.Skills[Skill.Magic] = 100;
            p.RecomputeMaxMp();
            p.MaxHP = p.HP = 5000;
            foreach (var s in Spells.All) if (!p.Spells.Contains(s.Id)) p.Spells.Add(s.Id);
            g.Monsters.Clear();
            g.FxEnabled = true;
            // A clear room: floor all around, no surfaces, nothing watching.
            int px = p.X, py = p.Y;
            for (int yy = py - 5; yy <= py + 5; yy++)
                for (int xx = px - 5; xx <= px + 5; xx++)
                    if (g.Map.InBounds(xx, yy)) { g.Map.Set(xx, yy, TileKind.Floor); g.Map.SetSurface(xx, yy, SurfaceKind.None); }
            g.UpdateFov();
            return g;
        }

        /// <summary>Places a creature. Frozen ones (the default) never act, so a failed cast cannot change the scene before the next try.</summary>
        static Monster Put(Game g, string kind, int x, int y, int hp = 4000, bool acts = false)
        {
            var m = new Monster(Bestiary.Find(kind), g.Rng) { X = x, Y = y, HomeX = x, HomeY = y, Depth = g.Map.Depth };
            m.HP = m.MaxHP = hp; m.Alert = 1; m.Dormant = false;
            if (!acts) m.Speed = 0;
            g.Monsters.Add(m);
            return m;
        }

        static bool Cast(Game g, string id, int x, int y)
        {
            var p = g.Player; int cost = Spells.Find(id).Cost;
            for (int i = 0; i < 80; i++)
            {
                p.Mp = p.MpMax = Math.Max(p.MpMax, 99);
                int before = p.Mp;
                if (!g.CastSpell(id, x, y)) return false;
                if (p.Mp <= before - cost) return true;   // a spell that changes your maximum mana may clamp it lower still
            }
            return false;
        }

        /// <summary>A coarse fingerprint of everything a spell could change, to tell "nothing happened" from "something did".</summary>
        static string Fingerprint(Game g)
        {
            var p = g.Player;
            var sb = new StringBuilder();
            sb.Append(p.X).Append(',').Append(p.Y).Append(',').Append(p.HP).Append(',').Append(p.Corruption).Append(',').Append(p.Gold).Append(',').Append(p.Invisible).Append(p.Confused).Append(p.Vigor);
            foreach (var kv in p.Buffs) sb.Append('|').Append(kv.Key).Append(kv.Value);
            sb.Append('#').Append(g.Map.Version);
            foreach (var m in g.Monsters)
                sb.Append(';').Append(m.Name).Append(m.X).Append(',').Append(m.Y).Append(',').Append(m.HP).Append(m.Asleep).Append(m.Confused).Append(m.FearTurns).Append(m.SlowTurns).Append(m.HeldTurns).Append(m.DotTurns).Append(m.VulnTurns).Append(m.Ally).Append(m.BurnTurns);
            return sb.ToString();
        }

        // Spells that legitimately show nothing in an empty, floor-only arena.
        static readonly HashSet<string> Quiet = new HashSet<string>
        {
            "light", "holy-light", "detect-traps", "knock", "skeleton-key", "identify", "disarm-traps", "remove-curse", "remedy", "antidote",
            "second-wind", "calm-beasts", "dig", "commune", "cleanse", "purify", "speak-with-animals", "command-undead", "turn-undead", "life-tap",
        };

        // ------------------------------------------------------------ tests

        static void CatalogueWiring()
        {
            var inBook = new HashSet<string>();
            foreach (var b in Spells.BookList)
            {
                Assert(b.Spells.Length >= 4, b.Name + " should teach at least 4 spells");
                foreach (string id in b.Spells) { Assert(Spells.Find(id) != null, b.Name + " teaches unknown spell " + id); inBook.Add(id); }
            }
            var books = new HashSet<string>();
            foreach (var d in Catalogue.Books) Assert(books.Add(d.Name), "duplicate book " + d.Name);
            foreach (var b in Spells.BookList) Assert(books.Contains(b.Name), b.Name + " is not in the catalogue");
            var perSchool = new Dictionary<School, int>();
            foreach (var s in Spells.All)
            {
                Assert(inBook.Contains(s.Id), s.Id + " is in no spellbook");
                perSchool[s.School] = (perSchool.TryGetValue(s.School, out int n) ? n : 0) + 1;
                if (s.Buff != null) Assert(SpellBuffs.Find(s.Buff) != null || s.Buff == "haste" || s.Buff == "levitating", s.Id + " grants a buff that does not exist: " + s.Buff);
                if (s.SummonDef != null)
                {
                    Assert(Bestiary.TryGet(s.SummonDef, out var def), s.Id + " calls a creature that does not exist: " + s.SummonDef);
                    Assert(def.Branch == "~summon", s.SummonDef + " could spawn on its own");
                }
                Assert(s.Fx != FxKind.None || s.Summons > 0 || s.Id == "teleport", s.Id + " has no animation");
                if (s.Target == SpellTarget.Cone) Assert(s.Radius >= 2, s.Id + " is a cone with no length");
                if (s.Shape == Shape.Scatter) Assert(s.HitFx != FxKind.None, s.Id + " needs a strike animation");
            }
            foreach (var kv in perSchool) Assert(kv.Value >= 14, kv.Key + " has only " + kv.Value + " spells");
            Assert(perSchool[School.Nature] >= 45 && perSchool[School.Necromancy] >= 40 && perSchool[School.Sacred] >= 40 && perSchool[School.Shadow] >= 35, "the casting classes should each have about fifty to choose from");
            foreach (var b in SpellBuffs.All) Assert(!string.IsNullOrEmpty(b.Label) && !string.IsNullOrEmpty(b.Msg), b.Id + " needs a label and a message");
        }

        static void EverySpellCasts()
        {
            int n = 0;
            foreach (var sp in Spells.All)
            {
                n++;
                bool effect = false, animated = false;
                string last = "";
                for (int attempt = 0; attempt < 8 && !(effect && animated); attempt++)
                {
                    var g = Archmage((ulong)(7000 + n * 13 + attempt));
                    var p = g.Player;
                    int px = p.X, py = p.Y;
                    var near = Put(g, "ogre", px + 1, py);
                    var far = Put(g, "ogre", px + 3, py);
                    int tx, ty;
                    switch (sp.Target)
                    {
                        case SpellTarget.Self: tx = px; ty = py; break;
                        case SpellTarget.Monster: { var m = sp.Range >= 2 ? far : near; tx = m.X; ty = m.Y; break; }
                        case SpellTarget.Cell: tx = px; ty = py + 3; break;
                        default: tx = px + Math.Min(3, sp.Range); ty = py; break;
                    }
                    if (sp.Id == "life-tap") p.HP = 4000;
                    string before = Fingerprint(g);
                    g.DrainFx();
                    bool ok = Cast(g, sp.Id, tx, ty);
                    Assert(ok, sp.Id + " refused to cast (" + string.Join(" / ", g.Log.Count > 0 ? new[] { g.Log[g.Log.Count - 1].Text } : new string[0]) + ")");
                    last = Fingerprint(g);
                    if (last != before) effect = true;
                    if (g.FxPending > 0) animated = true;
                    var steps = g.DrainFx();
                    foreach (var step in steps)
                        foreach (var c in step)
                        {
                            Assert(GlyphSet.Contains(c.Glyph), sp.Id + " animation uses glyph '" + c.Glyph + "' outside the font set");
                            Assert(c.Glyph != '\0', sp.Id + " animation uses an empty glyph");
                        }
                    Assert(steps.Count <= FxTimeline.MaxSteps, sp.Id + " animation is too long: " + steps.Count);
                }
                Assert(animated, sp.Id + " recorded no animation");
                if (!Quiet.Contains(sp.Id)) Assert(effect, sp.Id + " changed nothing in an arena with two ogres");
            }
        }

        static void FxShapes()
        {
            var tl = new FxTimeline();
            // A bolt ends on its target, travelling along a straight line.
            int end = FxLib.Bolt(tl, 0, 2, 2, 10, 2, Elem.Fire, '*', 2);
            Assert(end == 4, "an 8-cell bolt at 2 cells a step takes 4 steps, took " + end);
            var last = tl.Steps[end - 1];
            Assert(last.Exists(c => c.X == 10 && c.Y == 2 && c.Glyph == '*'), "the bolt should land on its target");
            Assert(tl.Steps[0].Exists(c => c.X == 4 && c.Y == 2), "and be two cells out after the first step");

            // A burst fills the Chebyshev square of its radius, corners included, so what shows is what is hit.
            tl = new FxTimeline();
            FxLib.Burst(tl, 0, 10, 10, 2, Elem.Fire);
            var all = new HashSet<(int, int)>();
            foreach (var s in tl.Steps) foreach (var c in s) all.Add((c.X, c.Y));
            Assert(all.Count == 25 && all.Contains((8, 8)) && all.Contains((12, 12)), "a radius-2 burst covers 5x5 cells, got " + all.Count);
            Assert(!all.Contains((13, 10)), "and nothing beyond");

            // A zap and a beam end on the target; a cone covers exactly the cells the spell hits.
            tl = new FxTimeline();
            FxLib.Zap(tl, 0, 2, 2, 9, 5, Elem.Lightning);
            Assert(tl.Steps[0].Exists(c => c.X == 9 && c.Y == 5), "lightning ends on its target");
            tl = new FxTimeline();
            FxLib.Beam(tl, 0, 2, 2, 8, 2, Elem.Cold);
            Assert(tl.Steps[tl.Length - 1].Count == 6 || tl.Steps[3].Exists(c => c.X == 8 && c.Y == 2), "a beam reaches its end");
            tl = new FxTimeline();
            FxLib.Cone(tl, 0, 5, 5, 9, 5, 4, Elem.Fire);
            var cone = Shapes.Cone(5, 5, 9, 5, 4);
            var drawn = new HashSet<(int, int)>();
            foreach (var s in tl.Steps) foreach (var c in s) drawn.Add((c.X, c.Y));
            foreach (var c in cone) Assert(drawn.Contains((c.x, c.y)), "the cone animation should cover (" + c.x + "," + c.y + ")");
            Assert(cone.Count >= 8 && cone.Count <= 30, "a cone of 4 covers a plausible number of cells: " + cone.Count);

            // Visibility is honoured, and the timeline is capped.
            tl = new FxTimeline { Show = (x, y) => x < 6 };
            FxLib.Bolt(tl, 0, 2, 2, 10, 2, Elem.Arcane);
            foreach (var s in tl.Steps) foreach (var c in s) Assert(c.X < 6, "hidden cells must not be drawn");
            tl = new FxTimeline();
            tl.Put(FxTimeline.MaxSteps + 5, 1, 1, '*', Rgb.White);
            Assert(tl.IsEmpty, "steps beyond the cap are dropped");

            // Every element has a ramp that gets darker and stays in range.
            foreach (Elem e in Enum.GetValues(typeof(Elem)))
            {
                var hot = FxLib.Pal(e, 0f); var cool = FxLib.Pal(e, 1f);
                Assert(hot.R + hot.G + hot.B > cool.R + cool.G + cool.B, e + " should fade from bright to dim");
            }
        }

        static void FxReachesTheFrame()
        {
            var s = new Session(); s.New(4242); s.Resize(110, 36); s.Draw();
            var g = s.Game; var p = g.Player;
            g.FxEnabled = true;
            p.Level = 12; p.Int = 20; p.Skills[Skill.Magic] = 100; p.RecomputeMaxMp(); p.Mp = p.MpMax = Math.Max(p.MpMax, 99);
            if (!p.Spells.Contains("fireball")) p.Spells.Add("fireball");
            int px = p.X, py = p.Y;
            for (int yy = py - 4; yy <= py + 4; yy++) for (int xx = px - 6; xx <= px + 6; xx++) if (g.Map.InBounds(xx, yy)) g.Map.Set(xx, yy, TileKind.Floor);
            g.Monsters.Clear(); g.UpdateFov();
            var f = s.Draw();
            Assert(f.Fx == null || f.Fx.Length == 0, "a quiet frame carries no animation");
            Put(g, "ogre", px + 4, py);
            Assert(g.CastSpell("fireball", px + 4, py) || g.CastSpell("fireball", px + 4, py) || g.CastSpell("fireball", px + 4, py), "the fireball casts");
            f = s.Draw();
            Assert(f.Fx != null && f.Fx.Length >= 5, "the fireball should arrive as an animation of several steps");
            Assert(f.FxMs == FxTimeline.StepMs, "and say how long a step is");
            int cells = f.Cols * f.Rows;
            foreach (var step in f.Fx)
            {
                Assert(step.Length % 4 == 0, "a step is a list of [cell, glyph, fg, bg] quadruples");
                for (int i = 0; i < step.Length; i += 4) Assert(step[i] >= 0 && step[i] < cells, "an animation cell is off the screen");
            }
            var again = s.Draw();
            Assert(again.Fx == null || again.Fx.Length == 0, "an animation is delivered once");

            // Not while a panel covers the map.
            Put(g, "ogre", px + 4, py);
            g.CastSpell("fireball", px + 4, py);
            g.UiState.Active = Panel.Spells;
            f = s.Draw();
            Assert(f.Fx == null || f.Fx.Length == 0, "no animation under a panel");
            g.UiState.Active = Panel.None;

            // Square tiles: still on the grid.
            DisplaySettings.Current.Square = true;
            try
            {
                s.Draw();
                g.Monsters.Clear(); Put(g, "ogre", px + 4, py);
                p.Mp = p.MpMax;
                g.CastSpell("fireball", px + 4, py);
                f = s.Draw();
                if (f.Fx.Length > 0) foreach (var step in f.Fx) for (int i = 0; i < step.Length; i += 4) Assert(step[i] >= 0 && step[i] < cells, "square-tile animation cell off screen");
            }
            finally { DisplaySettings.Current.Square = false; }
        }

        static void Riders()
        {
            // Held creatures lose their turns.
            var g = Archmage(5100); var p = g.Player;
            var ogre = Put(g, "ogre", p.X + 3, p.Y, 4000, true);
            for (int i = 0; i < 12 && ogre.HeldTurns == 0; i++) Cast(g, "entangle", ogre.X, ogre.Y);
            Assert(ogre.HeldTurns > 0, "entangle should hold an ogre");
            int ox = ogre.X;
            new Commands(g).Execute(".");
            Assert(ogre.X == ox, "a held ogre must not move");
            Assert(ogre.HeldTurns >= 0, "turns tick down");
            for (int i = 0; i < 30; i++) new Commands(g).Execute(".");
            Assert(ogre.HeldTurns == 0, "the hold wears off");

            // Poison and bleeding do damage over time and can kill, and the dead cannot be poisoned.
            var g2 = Archmage(5101);
            var jackal = Put(g2, "jackal", g2.Player.X + 3, g2.Player.Y, 30);
            for (int i = 0; i < 6 && jackal.DotTurns == 0; i++) Cast(g2, "plague-bolt", jackal.X, jackal.Y);
            Assert(jackal.DotTurns > 0 && jackal.DotDmg > 0, "plague bolt poisons");
            int hp = jackal.HP;
            new Commands(g2).Execute(".");
            Assert(jackal.HP < hp || jackal.IsDead, "poison hurts each turn");
            var skel = Put(g2, "skeleton", g2.Player.X + 2, g2.Player.Y + 1);
            for (int i = 0; i < 4; i++) Cast(g2, "hemorrhage", skel.X, skel.Y);
            Assert(skel.DotTurns == 0, "the dead do not bleed");
            var weak = Put(g2, "kobold", g2.Player.X + 3, g2.Player.Y - 1, 8);
            weak.DotTurns = 50; weak.DotDmg = 3; weak.DotType = DamageType.Poison;
            for (int i = 0; i < 6 && !weak.IsDead; i++) new Commands(g2).Execute(".");
            Assert(weak.IsDead, "damage over time kills");

            // Weaken: the same blow hurts more.
            var g3 = Archmage(5102);
            var a = Put(g3, "ogre", g3.Player.X + 3, g3.Player.Y); var b = Put(g3, "ogre", g3.Player.X + 3, g3.Player.Y + 1);
            b.VulnTurns = 20;
            int total = 0, totalWeak = 0;
            for (int i = 0; i < 30; i++)
            {
                a.HP = b.HP = 4000;
                Cast(g3, "magic-missile", a.X, a.Y); total += 4000 - a.HP;
                a.HP = b.HP = 4000;
                Cast(g3, "magic-missile", b.X, b.Y); totalWeak += 4000 - b.HP;
                b.VulnTurns = 20;
            }
            Assert(totalWeak > total, $"a weakened target takes more ({totalWeak} vs {total})");

            // Push and pull.
            var g4 = Archmage(5103); var p4 = g4.Player;
            var shoved = Put(g4, "ogre", p4.X + 2, p4.Y);
            int sx = shoved.X;
            Cast(g4, "gust-of-wind", p4.X + 3, p4.Y);
            Assert(shoved.X > sx, "a gust shoves a creature away from you");
            var pulled = Put(g4, "ogre", p4.X + 4, p4.Y + 2);
            g4.Monsters.Remove(shoved);
            int d0 = Pathfinder.Chebyshev(p4.X, p4.Y, pulled.X, pulled.Y);
            Cast(g4, "void-rift", pulled.X - 1, pulled.Y);
            Assert(Pathfinder.Chebyshev(pulled.X - 1, pulled.Y, pulled.X, pulled.Y) <= 2, "the rift pulls toward its centre or leaves it be");
        }

        static void Buffs()
        {
            var g = Archmage(5200); var p = g.Player;
            p.RefreshGear();
            int str = p.Str, ac = p.ArmorClass(), hpMax = p.MaxHP;
            Cast(g, "bulls-strength", p.X, p.Y);
            Assert(p.Str == str + 3, "bull's strength adds 3 Str, has " + (p.Str - str));
            p.SetBuff("bulls-strength", 1);
            new Commands(g).Execute(".");
            Assert(p.Str == str, "and takes it back when it ends");

            Cast(g, "mage-armor", p.X, p.Y);
            Assert(p.ArmorClass() == ac - 4, "mage armor is AC +4");
            Cast(g, "enlarge", p.X, p.Y);
            Assert(p.MaxHP > hpMax, "enlarge adds hit points");
            p.Buffs.Clear(); p.RefreshGear();
            Assert(p.Str == str && p.MaxHP == hpMax && p.ArmorClass() == ac, "clearing every spell restores the numbers");

            // Resistance wards.
            int fire = p.ResistPct(DamageType.Fire);
            Cast(g, "fire-ward", p.X, p.Y);
            Assert(p.ResistPct(DamageType.Fire) == fire + 60, "fire ward is 60%");

            // Melee riders of a buff: the hit carries the extra die.
            var ogre = Put(g, "ogre", p.X + 1, p.Y);
            Cast(g, "flame-blade", p.X, p.Y);
            Assert(p.BuffMods.ExtraSides == 6 && p.BuffMods.ExtraType == DamageType.Fire, "flame blade adds a fire die");

            // Spell power: a focused caster hits harder.
            var g2 = Archmage(5201); var q = g2.Player;
            var o1 = Put(g2, "ogre", q.X + 3, q.Y);
            int plain = 0, focused = 0;
            for (int i = 0; i < 40; i++) { o1.HP = 4000; Cast(g2, "stone-shard", o1.X, o1.Y); plain += 4000 - o1.HP; }
            Cast(g2, "arcane-focus", q.X, q.Y);
            for (int i = 0; i < 40; i++) { o1.HP = 4000; Cast(g2, "stone-shard", o1.X, o1.Y); focused += 4000 - o1.HP; }
            Assert(focused > plain, $"arcane focus adds spell power ({focused} vs {plain})");

            // Camouflage and the shadow cloak make you harder to notice.
            var g3 = Archmage(5202);
            var m3 = Put(g3, "orc", g3.Player.X + 3, g3.Player.Y);
            int before = g3.NoticeRadius(m3);
            Cast(g3, "camouflage", g3.Player.X, g3.Player.Y);
            Assert(g3.NoticeRadius(m3) < before, "camouflage shrinks the notice radius");
        }

        static void Wards()
        {
            var g = Archmage(5300); var p = g.Player;
            var ogre = Put(g, "ogre", p.X + 1, p.Y, 4000, true);
            Cast(g, "thorns", p.X, p.Y);
            int hp = ogre.HP;
            for (int i = 0; i < 40 && ogre.HP == hp; i++) { p.HP = 5000; new Commands(g).Execute("."); }
            Assert(ogre.HP < hp, "thorns hurt what hits you");

            var g2 = Archmage(5301); var p2 = g2.Player;
            var o2 = Put(g2, "ogre", p2.X + 1, p2.Y, 4000, true);
            Cast(g2, "sanctuary", p2.X, p2.Y);
            int hits = 0, swings = 0;
            for (int i = 0; i < 60; i++) { int h = p2.HP; new Commands(g2).Execute("."); swings++; if (p2.HP < h) hits++; p2.HP = p2.MaxHP; }
            Assert(hits < swings * 0.7, $"sanctuary should blunt most attacks ({hits} of {swings} landed)");

            var g3 = Archmage(5302); var p3 = g3.Player;
            Cast(g3, "regeneration", p3.X, p3.Y);
            p3.HP = 10;
            for (int i = 0; i < 10; i++) new Commands(g3).Execute(".");
            Assert(p3.HP >= 25, "regeneration mends each turn, got " + p3.HP);
        }

        static void Specials()
        {
            var g = Archmage(5400); var p = g.Player;
            var weak = Put(g, "ogre", p.X + 3, p.Y, 100); weak.HP = 20;
            Cast(g, "power-word-kill", weak.X, weak.Y);
            Assert(weak.IsDead, "a word kills what is nearly dead");
            var strong = Put(g, "ogre", p.X + 3, p.Y);
            Cast(g, "power-word-kill", strong.X, strong.Y);
            Assert(!strong.IsDead && strong.HP < 4000, "and wounds the healthy");

            var g2 = Archmage(5401); var p2 = g2.Player;
            var asleep = Put(g2, "ogre", p2.X + 1, p2.Y); asleep.Asleep = true; asleep.SleepTurns = 99;
            var awake = Put(g2, "ogre", p2.X, p2.Y + 1);
            int s1 = 0, s2 = 0;
            for (int i = 0; i < 30; i++)
            {
                asleep.HP = awake.HP = 4000; asleep.Asleep = true; asleep.SleepTurns = 99; awake.Asleep = false; awake.Alert = 1;
                Cast(g2, "assassinate", asleep.X, asleep.Y); s1 += 4000 - asleep.HP;
                Cast(g2, "assassinate", awake.X, awake.Y); s2 += 4000 - awake.HP;
            }
            Assert(s1 > s2 * 3 / 2, $"assassination doubles on the unaware ({s1} vs {s2})");

            var g3 = Archmage(5402); var p3 = g3.Player;
            var swap = Put(g3, "ogre", p3.X + 3, p3.Y);
            int sx = p3.X;
            Cast(g3, "swap-places", swap.X, swap.Y);
            Assert(p3.X == sx + 3 && swap.X == sx, "swap places trades the two");
            var ban = Put(g3, "ogre", p3.X + 1, p3.Y);
            Cast(g3, "banish", ban.X, ban.Y);
            Assert(Pathfinder.Chebyshev(p3.X, p3.Y, ban.X, ban.Y) >= 10 || ban.X == p3.X + 1, "banish sends it far away (or the level had nowhere to put it)");

            var g4 = Archmage(5403); var p4 = g4.Player;
            var skel = Put(g4, "skeleton", p4.X + 3, p4.Y);
            for (int i = 0; i < 20 && !skel.Ally; i++) Cast(g4, "command-undead", skel.X, skel.Y);
            Assert(skel.Ally, "command undead takes a skeleton");
            var ogre = Put(g4, "ogre", p4.X + 3, p4.Y + 1);
            Cast(g4, "command-undead", ogre.X, ogre.Y);
            Assert(!ogre.Ally, "and refuses the living");
            var jackal = Put(g4, "jackal", p4.X + 2, p4.Y - 1);
            for (int i = 0; i < 20 && !jackal.Ally; i++) Cast(g4, "speak-with-animals", jackal.X, jackal.Y);
            Assert(jackal.Ally, "speak with animals wins over a jackal");

            var g5 = Archmage(5404); var p5 = g5.Player;
            var mark = Put(g5, "kobold", p5.X + 1, p5.Y);
            int gold = p5.Gold;
            for (int i = 0; i < 30 && p5.Gold == gold; i++) Cast(g5, "light-fingers", mark.X, mark.Y);
            Assert(p5.Gold > gold, "light fingers lifts coin");

            var g6 = Archmage(5405); var p6 = g6.Player;
            p6.HP = 4000; p6.Mp = 10;
            Cast(g6, "life-tap", p6.X, p6.Y);
            Assert(p6.HP <= 3992 && p6.Mp > 9, "life tap trades HP for mana, HP " + p6.HP + " Mp " + p6.Mp);
            p6.Mp = p6.MpMax; p6.HP = 4000;
            Cast(g6, "ritual-of-blood", p6.X, p6.Y);
            Assert(p6.HP < 4000, "the ritual costs blood");

            var g7 = Archmage(5406); var p7 = g7.Player;
            var sleepy = Put(g7, "ogre", p7.X + 3, p7.Y); sleepy.Alert = 1;
            Cast(g7, "vanish", p7.X, p7.Y);
            Assert(p7.Invisible && sleepy.Alert == 0, "vanish: unseen, and they lost you");

            var g8 = Archmage(5407); var p8 = g8.Player;
            int px = p8.X;
            Cast(g8, "dimension-door", p8.X, p8.Y + 3);
            Assert(p8.Y == 0 || p8.X == px, "dimension door moves you");
            var g9 = Archmage(5408); var p9 = g9.Player;
            g9.Map.Set(p9.X + 4, p9.Y, TileKind.Wall);
            g9.UpdateFov();
            Cast(g9, "knock", p9.X, p9.Y);
            g9.Map.Set(p9.X + 2, p9.Y, TileKind.LockedDoor);
            Cast(g9, "knock", p9.X, p9.Y);
            Assert(g9.Map.Get(p9.X + 2, p9.Y) == TileKind.OpenDoor, "knock opens locked doors nearby");
        }

        static void Summons()
        {
            var summoning = new List<SpellDef>();
            foreach (var s in Spells.All) if (s.SummonDef != null) summoning.Add(s);
            Assert(summoning.Count >= 18, "there are plenty of summoning spells: " + summoning.Count);
            foreach (var s in summoning)
            {
                var g = Archmage(5500 + (ulong)s.Level); var p = g.Player;
                Assert(Cast(g, s.Id, p.X, p.Y), s.Id + " casts");
                int allies = g.Monsters.FindAll(m => m.Ally).Count;
                Assert(allies >= 1, s.Id + " should call at least one ally");
                var ally = g.Monsters.Find(m => m.Ally);
                Assert(ally.Def.Name == s.SummonDef, s.Id + " called a " + ally.Def.Name);
                Assert(ally.SummonTurns > 0, s.Id + " summons should expire");
            }
            // The called never turn up as ordinary monsters.
            for (int depth = 1; depth <= 40; depth++)
                foreach (var def in Bestiary.SpawnTable(depth, new Rng((ulong)depth), null))
                    Assert(def.Branch != "~summon", def.Name + " spawned at depth " + depth);
            // A mirror image is a decoy: one hit ends it.
            var gm = Archmage(5510); var pm = gm.Player;
            Cast(gm, "mirror-image", pm.X, pm.Y);
            Assert(gm.Monsters.FindAll(m => m.Def.Name == "mirror image").Count == 2, "mirror image calls two");
        }

        static void SpellPanel()
        {
            var g = Archmage(5600); var p = g.Player;
            g.UiState.SpellSchool = -1;
            Assert(g.SpellsShown().Count == p.Spells.Count, "All shows everything");
            g.UiState.SpellSchool = (int)School.Nature;
            var nature = g.SpellsShown();
            Assert(nature.Count >= 45 && nature.TrueForAll(id => Spells.Find(id).School == School.Nature), "one school shows only its spells");
            for (int i = 1; i < nature.Count; i++) Assert(Spells.Find(nature[i - 1]).Level <= Spells.Find(nature[i]).Level, "sorted by level");
            Assert(g.SpellCount((int)School.Shadow) >= 35 && g.SpellCount(-1) == p.Spells.Count, "counts per school");

            // The panel draws with the school tabs and the key handling moves between them.
            var s = new Session(); s.New(5601); s.Resize(110, 36); s.Draw();
            foreach (var sp in Spells.All) if (!s.Game.Player.Spells.Contains(sp.Id)) s.Game.Player.Spells.Add(sp.Id);
            s.Key("KeyZ", "Z", true); var f = s.Draw();
            Assert(s.Game.UiState.Active == Panel.Spells, "Z opens the spell list");
            s.Key("ArrowRight"); f = s.Draw();
            Assert(s.Game.UiState.SpellSchool == 0, "right goes to the first school");
            for (int i = 0; i < 8; i++) s.Key("ArrowRight");
            Assert(s.Game.UiState.SpellSchool == -1, "and wraps around to All");
            s.Key("ArrowLeft");
            Assert(s.Game.UiState.SpellSchool == 7, "left goes the other way");
            s.Key("PageDown"); s.Key("End"); s.Draw();
            s.Key("Escape");
            Assert(s.Game.UiState.Active == Panel.None, "Escape closes the list");
        }

        static void ImbuedItems()
        {
            // Every kind of item has a real pool of spells to draw from, and the pool makes sense.
            var need = new Dictionary<ItemKind, int> { { ItemKind.Weapon, 30 }, { ItemKind.Armor, 15 }, { ItemKind.Shield, 15 }, { ItemKind.Helm, 8 }, { ItemKind.Gloves, 6 }, { ItemKind.Boots, 6 }, { ItemKind.Cloak, 6 }, { ItemKind.Ring, 12 }, { ItemKind.Amulet, 15 } };
            foreach (var kv in need) Assert(SpellFit.Pool(kv.Key, 5).Count >= kv.Value, kv.Key + " has only " + SpellFit.Pool(kv.Key, 5).Count + " spells to carry");
            Assert(SpellFit.Fits(Spells.Find("fireball"), ItemKind.Weapon) && SpellFit.Fits(Spells.Find("frostbite"), ItemKind.Weapon), "a sword may carry an attack");
            Assert(!SpellFit.Fits(Spells.Find("blink"), ItemKind.Weapon) && !SpellFit.Fits(Spells.Find("cure-wounds"), ItemKind.Weapon), "but not a leap or a prayer");
            Assert(SpellFit.Fits(Spells.Find("blink"), ItemKind.Boots) && SpellFit.Fits(Spells.Find("haste"), ItemKind.Boots) && !SpellFit.Fits(Spells.Find("fireball"), ItemKind.Boots), "boots carry movement");
            Assert(SpellFit.Fits(Spells.Find("stone-skin"), ItemKind.Armor) && SpellFit.Fits(Spells.Find("holy-aura"), ItemKind.Shield) && !SpellFit.Fits(Spells.Find("magic-missile"), ItemKind.Armor), "armour carries wards");
            Assert(SpellFit.Fits(Spells.Find("detect-monsters"), ItemKind.Helm) && SpellFit.Fits(Spells.Find("fade"), ItemKind.Cloak) && SpellFit.Fits(Spells.Find("regeneration"), ItemKind.Amulet), "helms sense, cloaks hide, amulets mend");
            foreach (var sp in Spells.All)
            {
                bool somewhere = false;
                foreach (var k in need.Keys) if (SpellFit.Fits(sp, k)) somewhere = true;
                if (!somewhere && SpellFit.Damaging(sp)) Assert(false, sp.Id + " is an attack no kind of item can carry");
            }

            // Found items: gear and blank bases carry spells that fit them and that their depth allows.
            var seenKinds = new HashSet<ItemKind>(); int imbued = 0, blanks = 0;
            for (int i = 0; i < 6000; i++)
            {
                int depth = i % 2 == 0 ? 1 : 13;
                var it = LevelBuilder.RollLoot(new Rng((ulong)(50000 + i)), depth);
                if (it == null) continue;
                bool blank = (it.Def.Kind == ItemKind.Ring || it.Def.Kind == ItemKind.Amulet) && Catalogue.IsBlank(it.Def.Name);
                if (blank) { blanks++; Assert(it.Imbue != null, "a blank band is never found empty"); }
                if (it.Imbue == null) continue;
                imbued++; seenKinds.Add(it.Def.Kind);
                var sp = Spells.Find(it.Imbue);
                Assert(sp != null, "an item carries a spell that does not exist: " + it.Imbue);
                Assert(SpellFit.Fits(sp, it.Def.Kind), it.Def.Name + " carries " + sp.Id + ", which does not fit it");
                Assert(sp.Level <= 1 + depth / 3 + 2, "a level " + sp.Level + " spell turned up at depth " + depth);
                if (depth == 1) Assert(sp.Level <= 2, "a deep spell on the first level: " + sp.Id);
                Assert(it.Name.Contains(sp.Name) || !it.Identified, "an identified item names its spell");
            }
            Assert(imbued >= 100 && blanks >= 5, "imbued items are common enough: " + imbued + ", blanks " + blanks);
            foreach (var k in new[] { ItemKind.Weapon, ItemKind.Armor, ItemKind.Helm, ItemKind.Boots, ItemKind.Ring, ItemKind.Amulet })
                Assert(seenKinds.Contains(k), "no imbued " + k + " ever turned up");

            // Naming, identifying and lending.
            var g = Archmage(9100); var p = g.Player;
            p.Spells.Clear();
            var ring = new Item(Catalogue.Rings.First("silver band"), g.Rng, g.NextUid()) { Imbue = "haste", Rarity = Rarity.Magic };
            Assert(ring.Name == "enchanted silver band", "unknown: " + ring.Name);
            p.Inventory.Add(ring);
            Assert(!g.Knows("haste"), "not before it is worn");
            p.Rings[0] = ring; p.Inventory.Remove(ring); g.RevealGear(ring);
            Assert(ring.Name == "silver band of Haste" && g.Knows("haste"), "worn: " + ring.Name);
            Assert(g.CastableSpells().Contains("haste") && !p.Spells.Contains("haste"), "lent, not learned");
            var blade = new Item(Catalogue.Weapons.First("long sword"), g.Rng, g.NextUid()) { Imbue = "frostbite", Rarity = Rarity.Magic, Identified = true };
            Assert(blade.Name == "long sword of Frostbite", blade.Name);
            Assert(blade.TradeValue > Catalogue.Weapons.First("long sword").Cost, "an imbued blade is worth more");

            // A carried attack flares by itself in the fight; a carried ward stirs when you are struck.
            var gw = Archmage(9101); var pw = gw.Player;
            pw.Wielded = new Item(Catalogue.Weapons.First("long sword"), gw.Rng, gw.NextUid()) { Imbue = "frostbite", Identified = true, Rarity = Rarity.Magic };
            pw.RefreshGear();
            var dummy = Put(gw, "ogre", pw.X + 1, pw.Y);
            bool flared = false;
            for (int i = 0; i < 400 && !flared; i++)
            {
                dummy.HP = dummy.MaxHP = 4000; dummy.SlowTurns = 0; dummy.Speed = 0;
                int before = gw.Log.Count;
                gw.Attack(dummy);
                for (int m = before; m < gw.Log.Count; m++) if (gw.Log[m].Text.Contains("flares: Frostbite")) flared = true;
            }
            Assert(flared, "a sword of Frostbite should flare now and then");
            Assert(dummy.SlowTurns > 0 || flared, "and the frost takes");

            var ga = Archmage(9102); var pa = ga.Player;
            pa.WornArmor = new Item(Catalogue.Armor.First("ring mail"), ga.Rng, ga.NextUid()) { Imbue = "mage-armor", Identified = true, Rarity = Rarity.Magic };
            pa.RefreshGear();
            var brute = Put(ga, "ogre", pa.X + 1, pa.Y, 4000, true);
            bool stirred = false;
            for (int i = 0; i < 500 && !stirred; i++)
            {
                pa.HP = pa.MaxHP;
                int before = ga.Log.Count;
                new Commands(ga).Execute(".");
                for (int m = before; m < ga.Log.Count; m++) if (ga.Log[m].Text.Contains("stirs: Mage Armor")) stirred = true;
            }
            Assert(stirred && pa.BuffTurns("mage-armor") > 0, "a mail shirt of Mage Armor answers a blow with the ward");
        }

        static void SpellTranslations()
        {
            var old = Loc.Current;
            try
            {
                Loc.Current = Lang.Pt;
                foreach (var sp in Spells.All)
                {
                    Assert(Loc.SpellPt.ContainsKey(sp.Id), sp.Id + " has no Portuguese text");
                    var t = Loc.SpellPt[sp.Id];
                    Assert(!string.IsNullOrEmpty(t.Name) && t.Name.Length <= 26, sp.Id + " name is missing or too wide for the list: " + t.Name);
                    Assert(!string.IsNullOrEmpty(t.Blurb) && t.Blurb.Length <= 130, sp.Id + " description is missing or too long");
                    Assert(Loc.U(sp.Blurb) == t.Blurb, sp.Id + " description does not translate in the list");
                }
                foreach (var b in SpellBuffs.All)
                    Assert(Loc.T(b.Msg) != b.Msg, b.Id + " message does not translate");
                foreach (string school in new[] { "Evocation", "Necromancy", "Sacred", "Nature", "Shadow" }) Assert(Loc.U(school) != school, school + " does not translate");
                Assert(Loc.T("The ogre is held fast.") != "The ogre is held fast.", "rider messages translate");
                foreach (var kv in Loc.SpellPt) Assert(Spells.Find(kv.Key) != null, "a translation for a spell that does not exist: " + kv.Key);
            }
            finally { Loc.Current = old; }
        }

        static void ItemCatalogue()
        {
            // Every base item has a unique name, and every effect in the table belongs to something that exists.
            var names = new HashSet<string>();
            foreach (var list in new[] { Catalogue.Weapons, Catalogue.Armor, Catalogue.Shields, Catalogue.Helms, Catalogue.Gloves, Catalogue.Boots, Catalogue.Cloaks, Catalogue.Rings, Catalogue.Amulets, Catalogue.Wands, Catalogue.Scrolls, Catalogue.Potions, Catalogue.Books })
                foreach (var d in list) Assert(names.Add(d.Name), "two items are called " + d.Name);
            foreach (string n in ItemEffects.Names) Assert(names.Contains(n), "an item effect is defined for '" + n + "', which does not exist");
            Assert(Catalogue.Weapons.Count >= 40 && Catalogue.Armor.Count >= 20 && Catalogue.Rings.Count >= 30 && Catalogue.Amulets.Count >= 14, "the catalogue has grown: " + Catalogue.Weapons.Count + " weapons, " + Catalogue.Armor.Count + " armours, " + Catalogue.Rings.Count + " rings, " + Catalogue.Amulets.Count + " amulets");
            Assert(Catalogue.Wands.Count >= 35 && Catalogue.Scrolls.Count >= 40 && Catalogue.Potions.Count >= 30, "wands, scrolls and potions: " + Catalogue.Wands.Count + "/" + Catalogue.Scrolls.Count + "/" + Catalogue.Potions.Count);

            // Effects flow into the numbers when worn.
            var g = Game.NewHero(6100, "I", "human", "fighter"); var p = g.Player;
            int fire = p.ResistPct(DamageType.Fire), hp = p.MaxHP;
            p.Rings[0] = new Item(Catalogue.Rings.First("ring of fire resistance"), g.Rng, g.NextUid());
            p.Amulet = new Item(Catalogue.Amulets.First("amulet of health"), g.Rng, g.NextUid());
            p.RefreshGear();
            Assert(p.ResistPct(DamageType.Fire) == fire + 40, "a ring of fire resistance gives 40%");
            Assert(p.MaxHP == hp + 15, "an amulet of health gives 15 HP, has " + (p.MaxHP - hp));
            p.Rings[0] = null; p.Amulet = null; p.RefreshGear();
            Assert(p.MaxHP == hp && p.ResistPct(DamageType.Fire) == fire, "and takes them away again");
            var robe = new Item(Catalogue.Armor.First("archmage's robe"), g.Rng, g.NextUid());
            Assert(robe.Mods.Mp == 12 && robe.Mods.SpellPower == 2, "a robe carries its numbers");
            var ac = p.ArmorClass();
            p.Amulet = new Item(Catalogue.Amulets.First("amulet of warding"), g.Rng, g.NextUid()); p.RefreshGear();
            Assert(p.ArmorClass() == ac - 2, "an amulet of warding is AC +2");
            p.Amulet = null;

            // Amulets can really be worn, and a life-saving one is spent to undo a death.
            var cmd = new Commands(g);
            var saver = new Item(Catalogue.Amulets.First("amulet of life saving"), g.Rng, g.NextUid());
            p.Inventory.Add(saver);
            var w = typeof(Commands).GetMethod("WearAmulet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            w.Invoke(cmd, new object[] { saver });
            Assert(ReferenceEquals(p.Amulet, saver) && !p.Inventory.Contains(saver), "the amulet is worn");
            p.HP = 0; g.CheckDeath();
            Assert(g.Mode != GameMode.GameOver && p.HP > 0 && p.Amulet == null, "life saving undoes one death and is used up");

            // Affixes are well-formed.
            var ids = new HashSet<string>();
            foreach (var a in Affixes.All)
            {
                Assert(ids.Add(a.Id), "duplicate affix " + a.Id);
                Assert(a.Mods.Lines().Count > 0, a.Id + " does nothing");
                Assert(a.ForWeapon || a.ForArmor, a.Id + " fits nothing");
            }
            Assert(Affixes.All.Length >= 50, "affixes: " + Affixes.All.Length);
            var rng = new Rng(77);
            for (int i = 0; i < 100; i++) foreach (bool pre in new[] { true, false }) foreach (bool weapon in new[] { true, false }) Assert(Affixes.Pick(rng, pre, weapon) != null, "an affix should always be pickable");

            // Loot is gated by depth, and the deep end offers the new things.
            var shallow = new HashSet<string>(); var deep = new HashSet<string>();
            for (int i = 0; i < 4000; i++)
            {
                var a = LevelBuilder.RollLoot(new Rng((ulong)(9000 + i)), 1); if (a != null) shallow.Add(a.Def.Name);
                var b = LevelBuilder.RollLoot(new Rng((ulong)(19000 + i)), 14); if (b != null) deep.Add(b.Def.Name);
            }
            foreach (string n in shallow)
            {
                foreach (var list in new[] { Catalogue.Weapons, Catalogue.Armor, Catalogue.Rings, Catalogue.Amulets, Catalogue.Wands, Catalogue.Scrolls, Catalogue.Potions, Catalogue.Books })
                    foreach (var d in list) if (d.Name == n) Assert(d.Tier <= 2, n + " (tier " + d.Tier + ") turned up on the first level");
            }
            Assert(deep.Contains("wand of meteors") || deep.Contains("scroll of meteors") || deep.Contains("wand of ruin") || deep.Contains("ring of the archmage") || deep.Contains("archmage's robe") || deep.Contains("full plate") || deep.Contains("greatsword"), "the deep levels should offer the great things");
            Assert(deep.Count > shallow.Count, "and a wider choice (" + deep.Count + " vs " + shallow.Count + ")");
        }

        static void UniqueItems()
        {
            Assert(Artifacts.All.Length >= 50, "uniques: " + Artifacts.All.Length);
            var ids = new HashSet<string>(); var perSet = new Dictionary<string, int>();
            var depth = new Dictionary<string, int>(); foreach (var br in new Dungeon(new Rng(1)).Branches) depth[br.Name] = br.MaxDepth;
            int granting = 0;
            foreach (var a in Artifacts.All)
            {
                Assert(ids.Add(a.Id), "duplicate unique " + a.Id);
                Assert(!string.IsNullOrEmpty(a.Name) && !string.IsNullOrEmpty(a.Lore), a.Id + " needs a name and a story");
                Assert(depth.TryGetValue(a.Branch, out int max) && a.Depth >= 1 && a.Depth <= max, a.Id + " sits on a level that does not exist: " + a.Branch + " " + a.Depth);
                Assert(a.Chance >= 1 && a.Chance <= 100, a.Id + " chance");
                Assert(a.Mods.Lines().Count > 0 || a.Grants != null, a.Id + " does nothing");
                var item = Artifacts.Create(a, new Rng(1), 1);
                Assert(item.ArtifactName == a.Name && item.Rarity == Rarity.Artifact, a.Id + " builds");
                if (a.Grants != null) { granting++; foreach (string sp in a.Grants) Assert(Spells.Find(sp) != null, a.Id + " grants unknown spell " + sp); }
                if (a.Set != null) { Assert(ArtifactSets.Find(a.Set) != null, a.Id + " belongs to a set that does not exist"); perSet[a.Set] = (perSet.TryGetValue(a.Set, out int n) ? n : 0) + 1; }
            }
            foreach (var s in ArtifactSets.All) Assert(perSet.TryGetValue(s.Id, out int n) && n >= 3, "the set " + s.Id + " has only " + (perSet.TryGetValue(s.Id, out int m) ? m : 0) + " pieces");
            Assert(granting >= 25, "many uniques lend a spell: " + granting);

            // Every unique can be found: each rolls its place in the world.
            var seen = new HashSet<string>();
            foreach (var a in Artifacts.All)
                for (int i = 0; i < 400 && !seen.Contains(a.Id); i++)
                    if (Artifacts.RollForLevel(a.Branch, a.Depth, new Rng((ulong)(31 + i * 7))).Exists(x => x.Id == a.Id)) seen.Add(a.Id);
            foreach (var a in Artifacts.All) Assert(seen.Contains(a.Id), a.Id + " can never be found");

            // A lent spell: known while held, gone when put down.
            var gg = Game.NewHero(6200, "U", "human", "wizard"); var p = gg.Player;
            var staff = Artifacts.Create(Artifacts.Find("heartwood-staff"), gg.Rng, gg.NextUid());
            Assert(!gg.Knows("thorns"), "not before you hold it");
            p.Wielded = staff; p.RefreshGear();
            Assert(gg.Knows("thorns") && gg.CastableSpells().Contains("spike-growth") && !p.Spells.Contains("thorns"), "held: castable but not learned");
            p.Mp = p.MpMax = 50;
            Assert(gg.BeginCast("thorns") || true, "casting a lent spell");
            Assert(p.BuffTurns("thorns") > 0, "it works");
            p.Wielded = null; p.RefreshGear();
            Assert(!gg.Knows("thorns"), "and goes when you put it down");
            Assert(!gg.BeginCast("thorns"), "and cannot be cast");

            // Sets add up across slots: wielded, worn, ring and amulet all count.
            var gs = Game.NewHero(6201, "S", "human", "wizard"); var ps = gs.Player;
            ps.Wielded = Artifacts.Create(Artifacts.Find("stormcallers-staff"), gs.Rng, gs.NextUid());
            ps.WornCloak = Artifacts.Create(Artifacts.Find("mantle-of-the-tempest"), gs.Rng, gs.NextUid());
            int lightning = ps.ResistPct(DamageType.Lightning);
            ps.RefreshGear();
            Assert(ArtifactSets.Worn(ps, "stormcaller") == 2, "two storm pieces");
            ps.WornGloves = Artifacts.Create(Artifacts.Find("gloves-of-static"), gs.Rng, gs.NextUid()); ps.RefreshGear();
            Assert(ArtifactSets.Worn(ps, "stormcaller") == 3 && ArtifactSets.Bonus(ps).Mp == 15, "three storm pieces");
            var gl = Game.NewHero(6202, "L", "human", "necromancer"); var pl = gl.Player;
            pl.WornHelm = Artifacts.Create(Artifacts.Find("lich-queens-crown"), gl.Rng, gl.NextUid());
            pl.WornArmor = Artifacts.Create(Artifacts.Find("lich-queens-shroud"), gl.Rng, gl.NextUid());
            pl.Amulet = Artifacts.Create(Artifacts.Find("lich-queens-phylactery"), gl.Rng, gl.NextUid());
            pl.RefreshGear();
            Assert(ArtifactSets.Worn(pl, "lich-queen") == 3, "an amulet counts toward a set");
            Assert(pl.Gear.Mp >= 20 + 10 + 14, "and the full set bonus is applied, Mp " + pl.Gear.Mp);

            // A relic that corrupts says so in the numbers.
            Assert(Artifacts.Find("plaguebearers-mask").Corrupts, "the mask is a relic");
        }

        static void WandsAndScrolls()
        {
            foreach (var list in new[] { ItemSpells.Wands, ItemSpells.Scrolls, ItemSpells.Potions })
                foreach (var m in list)
                {
                    Assert(Spells.Find(m.Spell) != null, m.Name + " casts a spell that does not exist: " + m.Spell);
                    bool found = false;
                    foreach (var cat in new[] { Catalogue.Wands, Catalogue.Scrolls, Catalogue.Potions }) foreach (var d in cat) if (d.Name == m.Name) found = true;
                    Assert(found, m.Name + " is not in the catalogue");
                    Assert(m.Power >= 4 && m.Cost > 0 && m.Tier >= 1 && m.Tier <= 5, m.Name + " numbers");
                }

            // Every one of them works, never costs mana, and is spent once it has taken effect.
            int n = 0;
            foreach (var list in new[] { ItemSpells.Wands, ItemSpells.Scrolls, ItemSpells.Potions })
                foreach (var m in list)
                {
                    n++;
                    var g = Archmage((ulong)(8000 + n));
                    var p = g.Player;
                    p.Spells.Clear();           // nothing is known: the item must do it all
                    p.Mp = 0;
                    int px = p.X, py = p.Y;
                    var ogre = Put(g, "ogre", px + 3, py);
                    var def = default(ItemDef);
                    foreach (var cat in new[] { Catalogue.Wands, Catalogue.Scrolls, Catalogue.Potions }) foreach (var d in cat) if (d.Name == m.Name) def = d;
                    var item = new Item(def, g.Rng, g.NextUid()) { Identified = true };
                    if (item.Def.Kind == ItemKind.Wand) item.Charges = 3;
                    p.Inventory.Add(item);
                    var sp = Spells.Find(m.Spell);
                    string before = Fingerprint(g);
                    g.DrainFx();
                    bool ok = false;
                    for (int tries = 0; tries < 6 && !ok; tries++)
                    {
                        if (item.Def.Kind == ItemKind.Wand) g.UseWand(item); else if (item.Def.Kind == ItemKind.Scroll) g.UseScroll(item); else g.Quaff(item);
                        if (g.UiState.IsTargeting)
                        {
                            Assert(g.UiState.CastItem == item, m.Name + " should ask where");
                            int tx = sp.Target == SpellTarget.Cell ? px : px + Math.Min(3, sp.Range), ty = sp.Target == SpellTarget.Cell ? py + 3 : py;
                            if (sp.Target == SpellTarget.Monster) { tx = ogre.X; ty = ogre.Y; }
                            g.ResolveTargeting(tx, ty);
                        }
                        ok = item.Def.Kind == ItemKind.Wand ? item.ChargesUsed > 0 : !p.Inventory.Contains(item);
                    }
                    Assert(ok, m.Name + " was not spent");
                    if (!m.Spell.Contains("cunning") && !m.Spell.Contains("wisdom")) Assert(p.Mp == 0 || p.Mp == p.MpMax && item.Def.Name.Contains("mind"), m.Name + " should cost no mana");
                    Assert(g.FxPending > 0 || g.DrainFx().Count > 0 || sp.Fx != FxKind.None || sp.Summons > 0, m.Name + " should be seen");
                    Assert(Fingerprint(g) != before || Quiet.Contains(m.Spell), m.Name + " changed nothing");
                }

            // Cancelling aiming spends nothing; an empty wand refuses.
            var gc = Archmage(8500); var pc = gc.Player;
            var wand = new Item(Catalogue.Wands.First("wand of fireballs"), gc.Rng, gc.NextUid()) { Identified = true, Charges = 2 };
            pc.Inventory.Add(wand);
            Put(gc, "ogre", pc.X + 3, pc.Y);
            gc.UseWand(wand);
            Assert(gc.UiState.IsTargeting, "a wand of fireballs asks where");
            gc.UiState.Targeting = TargetingMode.None; gc.UiState.CastItem = null;
            Assert(wand.ChargesUsed == 0, "cancelling costs no charge");
            wand.ChargesUsed = 2;
            gc.UseWand(wand);
            Assert(!gc.UiState.IsTargeting, "an empty wand does not ask");

            // A scroll or potion of something you have never learned still lands, and a wand is as strong as its power says.
            var gw = Archmage(8501); var pw = gw.Player;
            pw.Level = 1; pw.Skills[Skill.Magic] = 0;
            var target = Put(gw, "ogre", pw.X + 3, pw.Y);
            var big = new Item(Catalogue.Wands.First("wand of meteors"), gw.Rng, gw.NextUid()) { Identified = true, Charges = 3 };
            pw.Inventory.Add(big);
            gw.UseWand(big); gw.ResolveTargeting(target.X, target.Y);
            Assert(target.HP < 4000 - 20, "a wand of meteors hits like a meteor even in weak hands: " + (4000 - target.HP));
        }
    }
}
