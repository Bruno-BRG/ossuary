using System;
using System.Collections.Generic;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Magic;

namespace Ossuary.Tests
{
    /// <summary>
    /// Named survivors (docs/roadmap/v24-survivors.md): levels keep their monsters, a monster that met the hero and lived
    /// takes a name, survivors grow and are remembered, killing one is a heavy deed, and the last six riders reach the hero.
    /// </summary>
    public static class SurvivorTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            Test("a level keeps its monsters when the hero comes back", LevelKeepsMonsters);
            Test("the first dungeon level is not emptied by the opening", OpeningKeepsLevelOne);
            Test("two runs of one seed hand out the same uids", UidsReplay);
            Test("a monster that met the hero and lived takes a name", Promotion);
            Test("a survivor that outlives the hero again grows", Growth);
            Test("killing a survivor is a heavy deed", SlainIsADeed);
            Test("the last six riders reach the hero", RidersReach);
            Test("fear forbids an attack, and bleed draws blood", StatusesBite);
            Console.WriteLine($"==== survivors: {_pass} passed, {_fail} failed ====");
            if (_fail > 0) throw new Exception($"{_fail} survivor asserts failed");
        }

        static void Test(string name, Action fn)
        {
            try
            {
                fn();
                _pass++;
                Console.WriteLine("  ok   " + name);
            }
            catch (Exception e)
            {
                _fail++;
                Console.WriteLine("  FAIL " + name + ": " + e.Message);
            }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void LevelKeepsMonsters()
        {
            var g = new Game(2101);
            int before = g.Monsters.Count;
            Assert(before > 0, "level 1 has monsters");
            g.DescendTo("The Dungeons", 2);
            g.DescendTo("The Dungeons", 1);
            Assert(g.Monsters.Count > 0, "a revisited level is not empty");
            Assert(g.Monsters.Count <= before, "and holds no more than what was left there");
        }

        static void OpeningKeepsLevelOne()
        {
            var g = new Game(2106);
            g.BeginAtOverworld();
            g.DescendTo("The Dungeons", 1);
            Assert(g.Monsters.Count > 0, "the level the constructor built is not empty when the hero first descends into it");
        }

        static void UidsReplay()
        {
            var a = new Game(2107);
            var b = new Game(2107);
            Assert(a.Monsters[0].Uid == b.Monsters[0].Uid, "uids are handed out identically, so survivor names and follow rolls replay");
        }

        static Monster Ready(Game g)
        {
            var m = g.Monsters[0];
            m.Encountered = true;
            m.SurvivorHurtHero = true;
            return m;
        }

        static void Promotion()
        {
            var g = new Game(2102);
            var m = Ready(g);
            g.DescendTo("The Dungeons", 2);
            Assert(m.SurvivorId != null, "it is named");
            Assert(m.SurvivorTitles.Contains("Biter of Heroes"), "it wounded the hero, so that is its first title");
            Assert(m.Name != m.Def.Name, "it has a name of its own");
            Assert(g.Survivors.Exists(s => s.Id == m.SurvivorId), "the run remembers it");
        }

        static void Growth()
        {
            var g = new Game(2103);
            var m = Ready(g);
            g.DescendTo("The Dungeons", 2);
            g.DescendTo("The Dungeons", 1);
            int level = m.Level;
            m.Encountered = true;
            g.DescendTo("The Dungeons", 2);
            Assert(m.Level == level + 1, "outliving the hero again is worth a level");
            Assert(g.Survivors.Exists(s => s.Id == m.SurvivorId && s.Level == m.Level), "and the run's record keeps up");
        }

        static void SlainIsADeed()
        {
            var g = new Game(2104);
            var m = Ready(g);
            g.DescendTo("The Dungeons", 2);
            var survivor = g.Survivors.Find(s => s.Id == m.SurvivorId);
            g.KillMonster(m);
            Assert(survivor != null && survivor.Slain, "the run records the death");
            Assert(g.Ledger.Exists(d => d.Kind == Deed.Killed && d.Subject == m.Name && d.Weight >= 3),
                "killing a survivor is a deed worth the epilogue");
        }

        static void RidersReach()
        {
            var produced = new HashSet<Rider>();
            foreach (string id in MonsterSpells.AllIds())
            {
                var sp = Spells.Find(id);
                if (sp != null) produced.Add(sp.Rider);
            }
            foreach (var r in new[] { Rider.Slow, Rider.Weaken, Rider.Bleed, Rider.Fear, Rider.Sleep, Rider.Root })
            {
                Assert(MonsterSpells.ReachesHero(r), "a monster's " + r + " reaches the hero");
                Assert(produced.Contains(r), "a caster's pool carries " + r + ", so it is not dead content");
            }
        }

        static void StatusesBite()
        {
            var g = new Game(2105);
            var m = g.Monsters[0];
            g.Player.FearTurns = 3;
            Assert(!g.Attack(m), "fear forbids an attack");
            g.Player.FearTurns = 0;

            g.Player.HP = g.Player.MaxHP;
            int hp = g.Player.HP;
            g.Player.BleedTurns = 2;
            g.Player.BleedDmg = 5;
            g.Player.SlowTurns = 2;
            g.EndPlayerTurn();
            Assert(g.Player.HP < hp, "bleeding costs blood every turn");
            Assert(g.Player.BleedTurns == 1, "and wears off");
            Assert(g.Player.SlowTurns <= 1, "slow wears off too");
        }
    }
}
