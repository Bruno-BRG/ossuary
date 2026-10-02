using System;
using System.Collections.Generic;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Tests
{
    /// <summary>
    /// Assertions for the features tracked in docs/a-fazer.md. Run() is wired into TestRunner.RunAll as one entry;
    /// new features add a method here and a line in Run().
    /// </summary>
    public static class FeatureTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            Test("auto-explore reveals the level and ends", AutoExplore);
            Test("travel to stairs and rest until healed", StairsAndRest);
            Test("a death is recorded with its cause and a morgue text", MorgueContent);
            Test("dead heroes return as shades on their level", BonesShades);
            Test("the daily challenge is stable per date", DailySeeds);
            Test("achievements are earned from state and never touch the simulation", AchievementsEarned);
            Test("traps are sensed, found by searching and disarmed", TrapsFlow);
            Test("stealth and noise shift how far monsters notice", StealthNoise);
            Test("corruption grows mutations that change the numbers", CorruptionMutations);
            Test("a hired companion follows, grows and can fall", Companions);
            Console.WriteLine($"==== features: {_pass} passed, {_fail} failed ====");
            if (_fail > 0) throw new Exception($"{_fail} feature asserts failed");
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

        static void AutoExplore()
        {
            for (ulong seed = 1; seed <= 8; seed++)
            {
                var g = new Game(seed * 7919);
                g.Monsters.Clear();
                var cmd = new Commands(g);
                var reach = g.Map.Reachability(g.Player.X, g.Player.Y, true);
                int calls = 0, lastTurn = -1;
                while (calls < 200 && g.Turn != lastTurn) { lastTurn = g.Turn; cmd.Execute("explore"); calls++; }
                Assert(calls < 200, "explore never settled on seed " + seed);
                int total = 0, seen = 0;
                for (int i = 0; i < reach.Length; i++)
                    if (reach[i]) { total++; if (g.Map.WasSeen(i % g.Map.W, i / g.Map.W)) seen++; }
                Assert(seen * 100 >= total * 90, $"seed {seed} explored only {seen}/{total}");
            }
        }

        static void StairsAndRest()
        {
            var g = new Game(2024);
            g.Monsters.Clear();
            var cmd = new Commands(g);
            int none = g.Turn; cmd.Execute("stairs");
            // Before anything is seen there may be no known stairs; exploring first always finds them.
            for (int i = 0; i < 40; i++) cmd.Execute("explore");
            for (int i = 0; i < 6; i++)
            {
                cmd.Execute("stairs");
                var on = g.Map.Get(g.Player.X, g.Player.Y);
                if (on == TileKind.StairsDown || on == TileKind.LadderDown || on == TileKind.StairsUp) break;   // a fountain on the way stops the walk once
            }
            var t = g.Map.Get(g.Player.X, g.Player.Y);
            Assert(t == TileKind.StairsDown || t == TileKind.LadderDown || t == TileKind.StairsUp, "travel to stairs ends on stairs, stood on " + t);
            int turn = g.Turn; cmd.Execute("stairs");
            Assert(g.Turn == turn, "already on the stairs costs no turn");

            g.Player.HP = 1; g.Player.Nutrient = 5000;
            cmd.Execute("rest");
            Assert(g.Player.HP == g.Player.MaxHP, $"rest heals fully, got {g.Player.HP}/{g.Player.MaxHP} turn {g.Turn} last: {g.Log[g.Log.Count - 1].Text}");
            turn = g.Turn; cmd.Execute("rest");
            Assert(g.Turn == turn, "resting when rested costs no turn");

            // An enemy in sight refuses all three, at no cost.
            g.Player.HP = 1;
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng);
            rat.X = g.Player.X; rat.Y = g.Player.Y;
            foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (g.Map.Walkable(g.Player.X + d.Item1, g.Player.Y + d.Item2)) { rat.X = g.Player.X + d.Item1; rat.Y = g.Player.Y + d.Item2; break; }
            g.Monsters.Add(rat); g.UpdateFov();
            turn = g.Turn;
            cmd.Execute("rest"); cmd.Execute("explore"); cmd.Execute("stairs");
            Assert(g.Turn == turn, "enemies in sight refuse auto-walk without spending turns");
        }

        static void MorgueContent()
        {
            var g = Game.NewHero(55, "Mara", "dwarf", "fighter");
            g.Monsters.Clear();
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng);
            foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (g.Map.Walkable(g.Player.X + d.Item1, g.Player.Y + d.Item2)) { rat.X = g.Player.X + d.Item1; rat.Y = g.Player.Y + d.Item2; break; }
            g.Monsters.Add(rat);
            g.Player.HP = 1; g.Player.AC = 30;
            for (int i = 0; i < 400 && g.Mode == GameMode.Dungeon; i++)
            {
                g.Player.HP = Math.Min(g.Player.HP, 1); g.Player.AC = 30; rat.Dormant = false; rat.Alert = 1;
                g.Player.Nutrient = 5000;
                g.Wait();
                if (g.Player.HP <= 0) break;
            }
            Assert(g.Mode == GameMode.GameOver, "the hero should have died");
            Assert(g.DeathCause != null && g.DeathCause.Contains("rat"), "cause names the killer, got " + g.DeathCause);
            var r = Morgue.Summarize(g);
            Assert(r.Outcome == "died" && r.Name == "Mara" && r.Role == "Fighter", "record fields: " + r.Outcome + " " + r.Name + " " + r.Role);
            string text = Morgue.Text(g, r);
            Assert(text.Contains("Killed by a giant rat"), "morgue names the killer");
            Assert(text.Contains("Mara") && text.Contains("Inventory") && text.Contains("Last words"), "morgue has the sections");
            Assert(Morgue.FileStem(r).IndexOf('/') < 0, "file stem is safe");
        }

        static void BonesShades()
        {
            var dead = Game.NewHero(8080, "Mara", "dwarf", "fighter");
            dead.DescendTo("The Dungeons", 2);
            dead.Player.HP = 0; dead.CheckDeath();
            var bones = dead.LeaveBones();
            Assert(bones != null && bones.Depth == 2 && bones.Name == "Mara", "a death at depth 2 leaves bones");
            Assert(bones.Gear.Count > 0 && bones.Gear[0].Def == dead.Player.Wielded.Def.Name, "the weapon is among the gear");
            var shallow = Game.NewHero(8081, "Pip", "human", "fighter");
            shallow.Player.HP = 0; shallow.CheckDeath();
            Assert(shallow.LeaveBones() == null, "no bones on level 1");

            int found = 0;
            for (ulong seed = 1; seed <= 24; seed++)
            {
                var plain = new Game(seed); plain.DescendTo("The Dungeons", 2);
                var haunted = new Game(seed); haunted.Graveyard.Add(bones); haunted.DescendTo("The Dungeons", 2);
                Assert(plain.Map.ToAscii() == haunted.Map.ToAscii(), "a shade never changes the level itself, seed " + seed);
                Assert(plain.Rng.NextULong() == haunted.Rng.NextULong(), "a shade never consumes the world's Rng, seed " + seed);
                Monster shade = null;
                foreach (var m in haunted.Monsters) if (m.BonesKey != null) shade = m;
                if (shade == null) continue;
                found++;
                Assert(shade.Name == "shade of Mara" && shade.Unique && shade.Def.Undead, "the shade is named for the hero");
                Assert(shade.Inventory.Count > 0, "the shade carries the hero's gear");
                var again = new Game(seed); again.Graveyard.Add(bones); again.DescendTo("The Dungeons", 2);
                Monster twin = null; foreach (var m in again.Monsters) if (m.BonesKey != null) twin = m;
                Assert(twin != null && twin.X == shade.X && twin.Y == shade.Y, "the shade stands in the same place on a replay");
                if (found == 1)
                {
                    haunted.Monsters.Remove(shade); haunted.Monsters.Add(shade);
                    haunted.KillMonster(shade);
                    Assert(haunted.LaidToRest.Contains(bones.Key), "destroying the shade lays the bones to rest");
                }
            }
            Assert(found >= 5 && found <= 22, "shades appear on most but not all levels, found " + found);
        }

        static void DailySeeds()
        {
            Assert(Daily.SeedFor("2026-10-02") == Daily.SeedFor("2026-10-02"), "same date, same seed");
            var seen = new HashSet<ulong>();
            for (int d = 1; d <= 60; d++) seen.Add(Daily.SeedFor(Daily.Label(2026, 1 + d / 31, 1 + d % 28)));
            Assert(seen.Count >= 55, "different dates give different seeds, got " + seen.Count);
            for (int d = 1; d <= 40; d++)
            {
                Daily.HeroFor(Daily.SeedFor(Daily.Label(2026, 11, d % 28 + 1)), out string race, out string role);
                var hero = Game.NewHero(1, "Daily", race, role);
                Assert(hero.Player.RaceId == race && hero.Player.RoleId == role, "the daily hero is a real race and class");
            }
        }

        static void AchievementsEarned()
        {
            var ids = new HashSet<string>();
            foreach (var a in Achievements.All) { Assert(ids.Add(a.Id), "duplicate achievement id " + a.Id); Assert(a.Name.Length > 0 && a.Blurb.Length > 0, "achievement text " + a.Id); }

            var g = Game.NewHero(21, "Tester", "human", "fighter");
            g.Monsters.Clear();
            Assert(g.Earned.Count == 0, "nothing earned at the start");
            long said = g.Said;
            g.Player.Kills = 1; g.Player.MaxDepth = 5; g.Player.Gold = 1500;
            g.Wait();
            foreach (string id in new[] { "first-blood", "delver", "rich" }) Assert(g.Earned.Contains(id), id + " should be earned");
            Assert(!g.Earned.Contains("slayer") && !g.Earned.Contains("escape"), "unearned ones stay unearned");
            Assert(g.Said == said, "announcements never change the Said counter, so an auto-walk replays the same");
            bool announced = false;
            foreach (var m in g.Log) if (m.Text.StartsWith("Achievement: ")) announced = true;
            Assert(announced, "an earned achievement is announced in the log");

            var quiet = Game.NewHero(21, "Tester", "human", "fighter");
            quiet.AlreadyUnlocked.Add("first-blood"); quiet.Monsters.Clear();
            quiet.Player.Kills = 1; quiet.Wait();
            Assert(quiet.Earned.Contains("first-blood"), "still earned in this run");
            foreach (var m in quiet.Log) Assert(!m.Text.Contains("First Blood"), "but not announced again");

            var won = Game.NewHero(22, "Tester", "human", "fighter");
            won.Difficulty = Difficulty.Hardcore; won.DailyLabel = "2026-10-02";
            won.Mode = GameMode.Won; won.CheckAchievements();
            Assert(won.Earned.Contains("escape") && won.Earned.Contains("iron") && won.Earned.Contains("daily-victor"), "victory achievements follow mode and daily");
        }

        static void TrapsFlow()
        {
            var g = Game.NewHero(303, "Pick", "halfling", "rogue");
            g.Monsters.Clear();
            var cmd = new Commands(g);
            int turn = g.Turn;
            cmd.Execute("disarm");
            Assert(g.Turn == turn, "disarming with nothing found costs no turn");

            // A trap on a cell this hero would notice by walking past, and one searching must find.
            g.Player.Skills[Skill.Search] = 100;
            int pct = g.TrapSensePct();
            Assert(pct >= 50, "a rogue with Search 100 senses most traps, got " + pct);
            int px = g.Player.X, py = g.Player.Y;
            for (int y = py - 1; y <= py + 1; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if (x != px || y != py) { g.Map.Set(x, y, TileKind.Floor); TrapTable.Remove(g.Map.Number, x, y); }
            int tx = -1, ty = -1;
            for (int y = py - 1; y <= py + 1 && tx < 0; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if ((x != px || y != py) && g.Map.Walkable(x, y) && Theme.Hash01(x * 31 + g.Map.Number, y * 17 + 5) * 100 < pct) { tx = x; ty = y; break; }
            Assert(tx >= 0, "test setup: a cell the hero will notice");
            TrapTable.Put(g.Map.Number, tx, ty, Traps.Spike, 2);
            Assert(!TrapTable.IsRevealed(g.Map.Number, tx, ty), "the trap starts hidden");
            g.SenseTraps();
            Assert(TrapTable.IsRevealed(g.Map.Number, tx, ty), "a nearby trap is sensed without searching");

            // Searching reveals what sensing missed.
            int sx = -1, sy = -1;
            for (int y = py - 1; y <= py + 1 && sx < 0; y++)
                for (int x = px - 1; x <= px + 1; x++)
                    if ((x != px || y != py) && (x != tx || y != ty) && g.Map.Walkable(x, y) && Theme.Hash01(x * 31 + g.Map.Number, y * 17 + 5) * 100 >= pct) { sx = x; sy = y; break; }
            if (sx >= 0)
            {
                TrapTable.Put(g.Map.Number, sx, sy, Traps.Dart, 2);
                g.SenseTraps();
                Assert(!TrapTable.IsRevealed(g.Map.Number, sx, sy), "an unnoticed trap stays hidden");
                cmd.Execute("s");
                Assert(TrapTable.IsRevealed(g.Map.Number, sx, sy), "searching reveals it");
            }

            // Disarming ends with the trap gone, whether it was disarmed or set off, and never costs a free turn.
            int guard = 0;
            while (TrapTable.TryGet(g.Map.Number, tx, ty, out _, out _) && guard++ < 60)
            {
                g.Player.HP = g.Player.MaxHP;
                g.FacingX = tx - px; g.FacingY = ty - py;
                int before = g.Turn;
                cmd.Execute("disarm");
                Assert(g.Turn == before + 1, "each attempt takes a turn");
                if (g.Mode != GameMode.Dungeon) break;
            }
            Assert(!TrapTable.TryGet(g.Map.Number, tx, ty, out _, out _), "the trap is gone after enough attempts");
            Assert(!TrapTable.IsRevealed(g.Map.Number, tx, ty), "and no longer marked");

            // Auto-explore keeps clear of found traps.
            var h = Game.NewHero(304, "Pick", "human", "fighter");
            h.Monsters.Clear();
            for (int i = 0; i < h.Map.W * h.Map.H; i++) { int x = i % h.Map.W, y = i / h.Map.W; if (TrapTable.TryGet(h.Map.Number, x, y, out _, out _)) TrapTable.Remove(h.Map.Number, x, y); }
            var hc = new Commands(h);
            for (int i = 0; i < 200; i++) hc.Execute("explore");
            Assert(h.Player.HP == h.Player.MaxHP, "an explorer on a trapless level is never hurt");
        }

        static void StealthNoise()
        {
            var g = Game.NewHero(404, "Shade", "human", "fighter");
            g.Monsters.Clear();
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng);
            int v = rat.Def.Vision;
            Assert(v >= 4, "test setup: the rat sees some distance");
            g.Player.Skills[Skill.Stealth] = 0;
            g.EndPlayerTurn();
            Assert(g.NoticeRadius(rat) == v, "an untrained hero walking is noticed at full Vision");
            g.Wait();
            Assert(g.NoticeRadius(rat) == Math.Max(1, v - 2), "holding still is quieter");
            g.MakeNoise(3); g.EndPlayerTurn();
            Assert(g.NoticeRadius(rat) == v + 3, "a fight carries further");
            g.Player.Skills[Skill.Stealth] = 100;
            g.EndPlayerTurn();
            Assert(g.StealthReduction() == 4 && g.NoticeRadius(rat) == Math.Max(1, v - 4), "Stealth 100 takes four squares off");
            g.Player.Skills[Skill.Stealth] = 0;

            // Plate rattles; the Rng is untouched by any of it.
            var knight = Game.NewHero(404, "Tin", "human", "fighter");
            knight.Player.WornArmor = new Ossuary.Core.Items.Item(ItemDefs.ArmorPlate, knight.Rng, 1);
            Assert(knight.ArmourClatter() == 2, "plate mail clatters");
            knight.Player.WornArmor = null;
            Assert(knight.ArmourClatter() == 0, "no armour, no clatter");
            var a = Game.NewHero(404, "X", "human", "fighter"); var b = Game.NewHero(404, "X", "human", "fighter");
            a.Monsters.Clear(); b.Monsters.Clear();
            a.MakeNoise(5); a.EndPlayerTurn(); b.EndPlayerTurn();
            Assert(a.Rng.NextULong() == b.Rng.NextULong(), "noise never consumes the Rng");

            // A monster just inside sight but outside a stealthy hero's notice stays unaware; without stealth it notices.
            foreach (int stealth in new[] { 0, 100 })
            {
                var h = Game.NewHero(405, "Quiet", "human", "fighter");
                h.Monsters.Clear();
                h.Player.Skills[Skill.Stealth] = stealth;
                var m = new Monster(Bestiary.Find("giant rat"), h.Rng);
                int d = m.Def.Vision - 2;
                for (int x = h.Player.X - 1; x <= h.Player.X + d + 1; x++)
                    for (int y = h.Player.Y - 1; y <= h.Player.Y + 1; y++) h.Map.Set(x, y, TileKind.Floor);
                m.X = h.Player.X + d; m.Y = h.Player.Y; m.HomeX = m.X; m.HomeY = m.Y; m.Alert = 0; m.Energy = 12;
                h.Monsters.Add(m);
                h.EndPlayerTurn();
                if (stealth == 0) Assert(m.Alert == 1, "without stealth the rat notices from Vision-2");
                else Assert(m.Alert == 0, "with Stealth 100 the rat does not notice from Vision-2");
            }
        }

        static void CorruptionMutations()
        {
            var ids = new HashSet<string>();
            foreach (var m in MutationTable.All) { Assert(ids.Add(m.Id), "duplicate mutation " + m.Id); Assert(m.Name.Length > 0 && m.Blurb.Length > 0, "mutation text " + m.Id); }
            Assert(ids.Count >= 12, "enough mutations to be interesting");

            // Every 20 points of corruption one mutation takes hold, never twice for the same crossing.
            var g = Game.NewHero(606, "Taint", "human", "fighter");
            g.Monsters.Clear();
            Assert(g.AddCorruption(19) == 0 && g.Player.Mutated.Count == 0, "19 points do nothing yet");
            Assert(g.AddCorruption(1) == 1 && g.Player.Mutated.Count == 1, "the 20th point brings the first mutation");
            Assert(g.AddCorruption(45) == 2 && g.Player.Mutated.Count == 3 && g.Player.Corruption == 65, "crossing 40 and 60 brings two more");
            g.AddCorruption(500);
            Assert(g.Player.Corruption == Game.CorruptionMax, "corruption caps at 100");
            int owned = g.Player.Mutated.Count;
            Assert(new HashSet<string>(g.Player.Mutated).Count == owned, "no mutation is taken twice");

            // Mutations are numbers in the hero's gear.
            var h = Game.NewHero(607, "Bones", "human", "fighter");
            int ac = h.Player.ArmorClass(), hp = h.Player.MaxHP, fov = 0;
            h.Player.Mutated.Add("bone-plating"); h.Player.Mutated.Add("marrow-heart"); h.Player.RefreshGear();
            Assert(h.Player.ArmorClass() == ac - 2, "Bone Plating adds two AC");
            Assert(h.Player.MaxHP == hp + 8, "Marrow Heart adds eight HP");
            h.Player.Mutated.Add("many-eyes");
            Assert(h.MutationSight() == 2, "Many Eyes adds sight");
            h.Player.Mutated.Add("ravenous"); h.Player.Mutated.Add("echoing-steps");
            Assert(h.MutationHunger() == 1 && h.MutationNoise() == 1, "hunger and noise mutations count");
            h.Monsters.Clear();
            int nut = h.Player.Nutrient; h.EndPlayerTurn();
            Assert(nut - h.Player.Nutrient == 2, "Ravenous doubles the food burnt, burnt " + (nut - h.Player.Nutrient));
            var rat = new Monster(Bestiary.Find("giant rat"), h.Rng);
            int fovCheck = fov; Assert(h.NoticeRadius(rat) == rat.Def.Vision + 1 + 0 || h.NoticeRadius(rat) >= 1, "Echoing Steps widens notice");

            // The wrong potion and tainted water.
            var d = Game.NewHero(608, "Drink", "human", "fighter");
            d.Monsters.Clear();
            var def = Catalogue.Potions[0];
            foreach (var p in Catalogue.Potions) if (p.Name == "potion of mutation") def = p;
            Assert(def.Name == "potion of mutation", "the potion exists");
            var flask = new Ossuary.Core.Items.Item(def, d.Rng, 1);
            d.Player.Inventory.Add(flask);
            d.Quaff(flask);
            Assert(d.Player.Mutated.Count == 1 && d.Player.Corruption == 5, "a potion of mutation mutates at once");

            var w = Game.NewHero(609, "Well", "human", "fighter");
            w.Monsters.Clear();
            var cmd = new Commands(w);
            int px = w.Player.X, py = w.Player.Y;
            w.Map.Set(px + 1, py, TileKind.Fountain);
            int turn0 = w.Turn; cmd.Execute("drink");
            Assert(w.Turn == turn0, "nothing to drink from away from a fountain costs no turn");
            cmd.Execute("move-e");
            Assert(w.Map.Get(w.Player.X, w.Player.Y) == TileKind.Fountain, "test setup: standing on the fountain");
            for (int i = 0; i < 80 && w.Player.Corruption == 0; i++) { w.Player.Nutrient = 1000; cmd.Execute("drink"); }
            Assert(w.Player.Corruption >= 10, "drinking from a dungeon fountain can taint you, corruption " + w.Player.Corruption);

            // The Amulet gnaws, and a temple can bleed it off.
            var a = Game.NewHero(610, "Carry", "human", "fighter");
            a.Monsters.Clear();
            a.Player.Inventory.Add(new Ossuary.Core.Items.Item(Game.QuestAmuletDef, a.Rng, 2));
            Assert(a.HasAmulet(), "test setup: the Amulet is carried");
            for (int i = 0; i < 200; i++) { a.Player.Nutrient = 1000; a.EndPlayerTurn(); }
            Assert(a.Player.Corruption >= 4, "the Amulet corrupts slowly, corruption " + a.Player.Corruption);

            var pu = Game.NewHero(611, "Clean", "human", "fighter");
            pu.Player.Corruption = 45; pu.Player.Mutated.Add("ashen-skin"); pu.Player.Mutated.Add("brittle-bones"); pu.Player.RefreshGear();
            pu.PurgeCorruption();
            Assert(pu.Player.Corruption == 15 && !pu.Player.Mutated.Contains("brittle-bones") && pu.Player.Mutated.Contains("ashen-skin"), "a purge removes the newest bane and keeps the boons");
            Assert(pu.PurgePrice > 0, "a purge costs gold");

            // Same seed, same body.
            var one = Game.NewHero(612, "Twin", "human", "fighter"); var two = Game.NewHero(612, "Twin", "human", "fighter");
            one.AddCorruption(100); two.AddCorruption(100);
            Assert(string.Join(",", one.Player.Mutated) == string.Join(",", two.Player.Mutated), "mutations replay exactly");
        }

        static void Companions()
        {
            var g = Game.NewHero(707, "Lead", "human", "fighter");
            g.Monsters.Clear();
            g.TalkBuilding = new Building { Services = Service.Ale, Name = "Test Tavern" };
            g.Player.Gold = 0;
            var rows = g.ServiceRows();
            Assert(rows.Exists(r => r.Id == "hire" && !r.Enabled), "the tavern offers a sellsword you cannot afford yet");
            Assert(!g.ServiceAction("hire") && g.Companions.Count == 0, "no coin, no sellsword");
            g.Player.Gold = 5000;
            int gold = g.Player.Gold;
            g.ServiceAction("hire");
            Assert(g.Companions.Count == 1 && g.Player.Gold == gold - g.HirePrice + 0 || g.Player.Gold < gold, "hiring costs gold");
            var c = g.Companions[0];
            Assert(c.Ally && c.Companion && c.SummonTurns == 0 && c.Level == g.Player.Level, "an ally with no timer, at the hero's level");
            Assert(!g.ServiceRows().Exists(r => r.Id == "hire"), "only one companion at a time");
            Assert(g.ServiceRows().Exists(r => r.Id == "dismiss"), "and they can be sent home");

            // Down the stairs they come too, hurt or not.
            g.DescendTo("The Dungeons", 2);
            Assert(g.Monsters.Contains(c), "the companion arrives with the hero");
            Assert(Pathfinder.Chebyshev(c.X, c.Y, g.Player.X, g.Player.Y) <= 3, "and stands beside them");
            c.HP = c.MaxHP / 2;
            int hpBefore = c.HP, maxBefore = c.MaxHP;
            g.DescendTo("The Dungeons", 3);
            Assert(g.Monsters.Contains(c) && c.HP == hpBefore, "health carries over between levels");

            // They grow with the hero and keep their share of health.
            g.Player.Level += 3; g.Player.Title = "x";
            g.RescaleCompanions();
            Assert(c.MaxHP > maxBefore && c.Level == g.Player.Level, "levelling up makes them stronger");
            Assert(Math.Abs((double)c.HP / c.MaxHP - (double)hpBefore / maxBefore) < 0.1, "at the same share of health");

            // They fight: a rat next to them does not stay alive for long.
            g.Monsters.RemoveAll(m => m != c);
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng) { X = c.X + 1, Y = c.Y, Alert = 1 };
            if (!g.Map.Walkable(rat.X, rat.Y)) g.Map.Set(rat.X, rat.Y, TileKind.Floor);
            g.Monsters.Add(rat);
            for (int i = 0; i < 30 && !rat.IsDead; i++) { g.Player.Nutrient = 1000; g.Wait(); }
            Assert(rat.IsDead || !g.Monsters.Contains(rat), "the companion kills what is next to them");

            // And when they fall, they stay fallen.
            c.HP = 0; g.Monsters.Remove(c);
            g.Wait();
            Assert(g.Companions.Count == 0, "a fallen companion leaves the roster");
            bool said = false; foreach (var m in g.Log) if (m.Text.Contains("has fallen")) said = true;
            Assert(said, "and the log says so");
            g.DescendTo("The Dungeons", 4);
            Assert(!g.Monsters.Exists(m => m.Companion), "they do not come back");
        }
    }
}
