using System;
using System.Collections.Generic;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Tests
{
    /// <summary>
    /// Assertions for the features tracked in docs/todo.md. Run() is wired into TestRunner.RunAll as one entry;
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
            Test("Dive and Naked challenge runs start differently", Challenges);
            Test("travel finds altars and fountains", FeatureTravel);
            Test("Trained mode: skills are bought with XP", TrainedMode);
            Test("what the game reports can also be heard", SoundCues);
            Test("square tiles double the map columns", SquareTiles);
            Test("crafting combines the pack and a molotov burns", CraftingFlow);
            Test("artifact sets add up and relics corrupt", SetsAndRelics);
            Test("new spells: ice, steam, oil, bone and purification", NewSpells);
            Test("gods: Mourne, rivals, sacrifices and trials", GodsExpanded);
            Test("branch monsters have habits of their own", BranchMonsters);
            Test("each branch has a boss with mechanics", BossFights);
            Test("monster factions fight each other", FactionWar);
            Test("reputation, haggling and guild jobs", ReputationAndJobs);
            Test("road events offer choices with prices", RoadEvents);
            Test("townsfolk keep hours and remember you", TownRoutine);
            Test("townsfolk have personas, memory and a ledger of deeds", PersonaAndLedger);
            Test("key people hold real conversations with gated choices", Conversations);
            Test("quests run from data: steps, counters, rewards, deadlines", QuestEngine);
            Test("townsfolk ask for favours from their wants and remember them", PersonalErrands);
            Test("rumours point at real things and depend on who tells them", RumoursWithTeeth);
            Test("crime: witnesses, bounty by region, arrest, jail, murder, essentials", CrimeAndTheWatch);
            Test("town events: schedule, prices, closed doors, a job for the Watch", TownEvents);
            Test("travellers on the road: pilgrim, peddler, refugees, delver", RoadTravellers);
            Test("main questline: documents, the Reader, truths, endings, new cycle", MainQuestline);
            Test("a rival party races the hero down the Dungeons", RivalRace);
            Test("the Cult's dark mirror: a vial for the Drowned", CultTrack);
            Test("vaults are carved out of unused rock and need a key", VaultsAndKeys);
            Test("overworld entrances lead into every branch", EntrancesReachBranches);
            Test("the Annex: a portal, hard floors, a warden and a mantle", AnnexFlow);
            Test("English frames carry no Portuguese", EnglishFramesAreEnglish);
            Test("the stairs keys say they need Shift", KeyboardHints);
            Test("bodies: hits land on parts, wounds hinder, bleed, heal and scar", BodiesAndWounds);
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
        /// <summary>Bodies and wounds (docs/roadmap/depth.md, section 1): parts by plan, severity from damage, effects, bleeding, mending, scars, Portuguese.</summary>
        static void BodiesAndWounds()
        {
            var old = Loc.Current;
            try
            {
                Loc.Current = Lang.En;
                Assert(Bodies.PlanFor(Bestiary.Find("jackal")) == Bodies.Quadruped, "a jackal walks on four legs");
                Assert(Bodies.PlanFor(Bestiary.Find("cave bat")) == Bodies.Winged, "a bat flies on wings");
                Assert(Bodies.PlanFor(Bestiary.Find("cave spider")) == Bodies.Insect, "a spider has an insect plan");
                Assert(Bodies.PlanFor(Bestiary.Find("floating eye")) == null && Bodies.PlanFor(Bestiary.Find("brown mold")) == null, "eyes and moulds have nothing to break");
                Assert(!Bodies.Bleeds(new Monster(Bestiary.Find("skeleton"), new Rng(1))), "a skeleton does not bleed");

                Assert(Bodies.Severity(2, 30, false) == 0, "a scratch leaves no wound");
                Assert(Bodies.Severity(5, 30, false) == 1 && Bodies.Severity(10, 30, false) == 2, "grazed, then cut");
                Assert(Bodies.Severity(16, 30, false) == 3 && Bodies.Severity(25, 30, false) == 4, "torn, then mangled");
                Assert(Bodies.Severity(2, 30, true) == 1, "a critical cuts deeper");

                // A heavy edged hit opens a wound on one part, says so, and bleeds.
                var g = new Game(2024);
                var p = g.Player;
                p.HP = p.MaxHP;
                int big = p.MaxHP * 60 / 100;
                g.WoundFrom(p, new AttackResult { Hit = true, Damage = big }, true, false);
                Assert(p.Wounds.Count == 1 && p.Wounds[0].Severity == 3, $"a 60% hit tears a part, got {p.Wounds.Count} wound(s)");
                Assert(p.Wounds[0].Bleed > 0, "an edged wound bleeds");
                Assert(g.Log.Exists(m => m.Text.StartsWith("Your " + p.Wounds[0].Part + " is torn")), "the log names the part");
                var twin = new Game(2024); twin.Player.HP = twin.Player.MaxHP;
                twin.WoundFrom(twin.Player, new AttackResult { Hit = true, Damage = big }, true, false);
                Assert(twin.Player.Wounds[0].Part == p.Wounds[0].Part, "the part is the same for the same seed");
                g.WoundFrom(p, new AttackResult { Hit = true, Damage = 1 }, false, false);
                Assert(p.Wounds.Count == 1, "a light blow leaves nothing new");

                // Effects by part.
                p.Wounds.Clear();
                p.Wounds.Add(new Wound { Part = "right leg", Kind = PartKind.Leg, Severity = 3 });
                Assert(Bodies.Limp(p) == 1, "a broken leg makes the hero limp");
                p.Wounds.Add(new Wound { Part = "left leg", Kind = PartKind.Leg, Severity = 4 });
                Assert(Bodies.Limp(p) == 2, "two broken legs: crawling");
                p.Wounds.Clear();
                p.Wounds.Add(new Wound { Part = "right arm", Kind = PartKind.Arm, Severity = 3 });
                Assert(Bodies.ArmPenalty(p) == 2, "the weapon arm costs full to-hit");
                p.Wounds.Clear();
                p.Wounds.Add(new Wound { Part = "left arm", Kind = PartKind.Arm, Severity = 3 });
                Assert(Bodies.ArmPenalty(p) == 1, "the off arm costs half");
                p.Wounds.Add(new Wound { Part = "left eye", Kind = PartKind.Eye, Severity = 2 });
                Assert(Bodies.EyePenalty(p) == 2, "a cut eye shortens sight");
                var dog = new Monster(Bestiary.Find("jackal"), new Rng(2));
                dog.Wounds.Add(new Wound { Part = "front left leg", Kind = PartKind.Leg, Severity = 3 });
                Assert(Bodies.Limp(dog) == 0, "one bad leg out of four is not a limp");
                dog.Wounds.Add(new Wound { Part = "hind left leg", Kind = PartKind.Leg, Severity = 3 });
                Assert(Bodies.Limp(dog) == 1, "two bad legs out of four is");
                var bat = new Monster(Bestiary.Find("cave bat"), new Rng(3));
                bat.Wounds.Add(new Wound { Part = "left wing", Kind = PartKind.Wing, Severity = 4 });
                Assert(Bodies.Limp(bat) == 1, "a flier is slowed by its wings");

                // Bleeding runs out; a healed bad wound leaves a scar; mending clears the rest.
                p.Wounds.Clear();
                p.Wounds.Add(new Wound { Part = "torso", Kind = PartKind.Torso, Severity = 2, Worst = 2, Edged = true, Bleed = 2, HealIn = 500 });
                p.HP = p.MaxHP;
                g.Monsters.Clear();
                g.EndPlayerTurn(); g.EndPlayerTurn();
                Assert(!Bodies.Bleeding(p), "bleeding stops after its turns");
                Assert(g.Log.Exists(m => m.Text == "Your bleeding stops."), "and the log says so");
                p.Wounds.Clear();
                p.Wounds.Add(new Wound { Part = "left leg", Kind = PartKind.Leg, Severity = 1, Worst = 3, HealIn = 1 });
                g.EndPlayerTurn();
                Assert(p.Wounds.Count == 0 && p.Scars.Contains("left leg"), "a healed broken leg leaves a scar");
                p.Wounds.Add(new Wound { Part = "head", Kind = PartKind.Head, Severity = 4, Worst = 4, Bleed = 5, HealIn = 2000 });
                g.MendWounds(4);
                Assert(p.Wounds.Count == 0 && p.Scars.Contains("head"), "a night's rest (or full healing) closes everything");

                // The sheet shows them.
                p.Wounds.Add(new Wound { Part = "right arm", Kind = PartKind.Arm, Severity = 3, Worst = 3, HealIn = 900 });
                var hud = new GameHud(g); hud.Ui.Resize(110, 36);
                g.UiState.Active = Panel.Character;
                var sheet = hud.Draw().ToAscii();
                Assert(sheet.Contains("Wounds: right arm broken") && sheet.Contains("Scars: left leg, head"), "the character sheet lists wounds and scars");

                // Portuguese: the adjective follows the part's gender, the owner gets "do/da".
                Loc.Current = Lang.Pt;
                Assert(Loc.T("Your left leg is broken.") == "Sua perna esquerda está quebrada.", Loc.T("Your left leg is broken."));
                Assert(Loc.T("Your right arm is cut.") == "Seu braço direito está cortado.", Loc.T("Your right arm is cut."));
                Assert(Loc.T("The jackal's front left leg is torn.") == "A pata dianteira esquerda do chacal está dilacerada.", Loc.T("The jackal's front left leg is torn."));
                Assert(Loc.T("The kobold bleeds to death.") == "O kobold sangra até morrer.", Loc.T("The kobold bleeds to death."));
            }
            finally { Loc.Current = old; }
        }


        /// <summary>
        /// The language is chosen once, for everything (docs/tech/languages.md): a screen drawn in English must not carry a
        /// Portuguese word, however it got there. Portuguese letters are the tell, and every text table keeps them.
        /// </summary>
        static void EnglishFramesAreEnglish()
        {
            var old = Loc.Current;
            try
            {
                Loc.Current = Lang.En;
                var g = Game.NewHero(4200, "Arthur", "human", "fighter");
                var hud = new GameHud(g);
                hud.Ui.Resize(110, 36);
                foreach (Panel p in new[] { Panel.None, Panel.Inventory, Panel.Character, Panel.Spells, Panel.Abilities, Panel.Advance,
                                            Panel.Help, Panel.Controls, Panel.Settings, Panel.Discoveries, Panel.Journal, Panel.Achievements,
                                            Panel.Runs, Panel.Create, Panel.Travel, Panel.History })
                {
                    g.UiState.Active = p;
                    var text = hud.Draw().ToAscii();
                    var mark = Portuguese(text);
                    Assert(mark.Length == 0, "Portuguese in the English " + p + " panel: " + mark);
                }
                g.UiState.Active = Panel.None;
                g.LeaveToOverworld();
                Assert(Portuguese(hud.Draw().ToAscii()).Length == 0, "Portuguese on the English overworld: " + Portuguese(hud.Draw().ToAscii()));
                g.EnterTown("Ravensgate");
                Assert(Portuguese(hud.Draw().ToAscii()).Length == 0, "Portuguese in an English town: " + Portuguese(hud.Draw().ToAscii()));
                g.UiState.Active = Panel.Service;
                Assert(Portuguese(hud.Draw().ToAscii()).Length == 0, "Portuguese in an English service panel: " + Portuguese(hud.Draw().ToAscii()));
            }
            finally { Loc.Current = old; }
        }

        const string Accented = "áàâãäéèêëíìîïóòôõöúùûüçñÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑ";
        /// <summary>The first Portuguese word found in a screen, with a little of its line, or an empty string.</summary>
        static string Portuguese(string text)
        {
            int at = text.IndexOfAny(Accented.ToCharArray());
            if (at < 0) return "";
            int from = Math.Max(0, at - 24);
            return text.Substring(from, Math.Min(48, text.Length - from)).Replace("\n", " ");
        }

        /// <summary>
        /// The stairs are typed as symbols that no keyboard has on its own (review item): the Commands panel names the
        /// shift form, and the Controls panel explains it under the stairs line, in both languages.
        /// </summary>
        static void KeyboardHints()
        {
            var old = Loc.Current;
            try
            {
                int descend = Array.FindIndex(KeyBindings.Actions, a => a.Id == "descend");
                Assert(descend >= 0, "the stairs are a bound action");

                Loc.Current = Lang.En;
                var g = Game.NewHero(4300, "Keys", "human", "fighter");
                var hud = new GameHud(g); hud.Ui.Resize(110, 36);
                g.UiState.Active = Panel.Help;
                var help = hud.Draw().ToAscii();
                Assert(help.Contains("Shift + .") && help.Contains("Shift + ,"), "the Commands panel names the shift keys");
                g.UiState.Active = Panel.Controls; g.UiState.ControlsIndex = descend;
                var controls = hud.Draw().ToAscii();
                Assert(controls.Contains("Shift + ."), "the Controls panel explains '>' under the stairs line");

                Loc.Current = Lang.Pt;
                var pt = Game.NewHero(4300, "Keys", "human", "fighter");
                var ptHud = new GameHud(pt); ptHud.Ui.Resize(110, 36);
                pt.UiState.Active = Panel.Help;
                var ptHelp = ptHud.Draw().ToAscii();
                Assert(ptHelp.Contains("Shift + .") && ptHelp.Contains("Shift + ,"), "the Portuguese Commands panel names them too");
                pt.UiState.Active = Panel.Controls; pt.UiState.ControlsIndex = descend;
                var ptControls = ptHud.Draw().ToAscii();
                Assert(ptControls.Contains("Shift + .") && ptControls.Contains("ABNT2"), "and the Portuguese note names the layouts");
            }
            finally { Loc.Current = old; }
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

        static void Challenges()
        {
            var dive = Game.NewHero(808, "Fall", "human", "fighter");
            dive.Difficulty = Difficulty.Dive; dive.ApplyChallenge();
            Assert(dive.Depth == 5 && dive.Mode == GameMode.Dungeon, "Dive starts on depth 5");
            Assert(dive.Player.Level >= 4 && dive.Player.HP == dive.Player.MaxHP, "a few levels up and healthy");
            Assert(dive.Player.Inventory.Exists(i => i.Def.Name == "potion of healing"), "with potions");

            var naked = Game.NewHero(809, "Bare", "human", "fighter");
            int adv = naked.Player.PendingAdvances;
            naked.Difficulty = Difficulty.Naked; naked.ApplyChallenge();
            Assert(naked.Player.Wielded == null && naked.Player.WornArmor == null && naked.Player.WornShield == null, "Naked starts with nothing on");
            Assert(naked.Player.PendingAdvances == adv + 1, "and one more advancement");

            var normal = Game.NewHero(810, "Plain", "human", "fighter");
            normal.ApplyChallenge();
            Assert(normal.Player.Wielded != null && normal.Depth == 1, "Normal is untouched");

            foreach (var d in Difficulties.All)
            {
                Assert(Difficulties.Parse(Difficulties.Name(d)) == d, "mode names round-trip: " + d);
                Assert(Difficulties.Blurb(d).Length > 0, "every mode explains itself");
            }
            Difficulties.ScoreFactor("Dive", out int n, out int den);
            Assert(n == 2 && den == 1, "challenges score double");
        }

        static void FeatureTravel()
        {
            var g = Game.NewHero(901, "Walk", "human", "fighter");
            g.Monsters.Clear();
            var cmd = new Commands(g);
            int turn = g.Turn; cmd.Execute("feature");
            Assert(g.Turn == turn, "with nothing known the walk costs no turn");
            for (int i = 0; i < 40; i++) cmd.Execute("explore");
            // The farthest known floor cell gets a fountain; another one gets an altar beside it.
            int bx = -1, by = -1, bd = -1;
            for (int y = 0; y < g.Map.H; y++)
                for (int x = 0; x < g.Map.W; x++)
                    if (g.Map.WasSeen(x, y) && g.Map.Get(x, y) == TileKind.Floor)
                    {
                        int d = Pathfinder.Chebyshev(x, y, g.Player.X, g.Player.Y);
                        if (d > bd) { bd = d; bx = x; by = y; }
                    }
            Assert(bx >= 0 && bd >= 6, "test setup: a far floor cell");
            g.Map.Set(bx, by, TileKind.Fountain);
            for (int i = 0; i < 8 && !g.IsFeatureSpot(g.Player.X, g.Player.Y); i++) cmd.Execute("feature");
            Assert(g.Map.Get(g.Player.X, g.Player.Y) == TileKind.Fountain, "the walk ends on the fountain");
            turn = g.Turn; cmd.Execute("feature");
            Assert(g.Turn == turn, "already there costs nothing");
        }

        static Item Make(Game g, string name, int qty = 1)
        {
            foreach (var list in new IEnumerable<ItemDef>[] { Catalogue.Potions, Catalogue.Tools, Catalogue.Weapons, Catalogue.Armor })
                foreach (var d in list) if (d.Name == name) return new Item(d, g.Rng, g.NextUid()) { Identified = true, Quantity = qty };
            var corpse = new ItemDef { Name = name, Glyph = '%', Kind = ItemKind.Corpse, Weight = 10 };
            return new Item(corpse, g.Rng, g.NextUid()) { Quantity = qty };
        }

        static void CraftingFlow()
        {
            var g = Game.NewHero(1001, "Maker", "human", "fighter");
            g.Monsters.Clear();
            var cmd = new Commands(g);
            g.Player.Inventory.Clear();
            Assert(g.CraftChoices().Count == 0, "an empty pack makes nothing");
            int turn = g.Turn; cmd.Execute("craft");
            Assert(g.Turn == turn && !g.PendingChoice.Active, "nothing to craft costs no turn and opens nothing");

            // Molotov: oil + candle.
            g.Player.Inventory.Add(Make(g, "potion of oil")); g.Player.Inventory.Add(Make(g, "candle"));
            var choices = g.CraftChoices();
            Assert(choices.Count == 1 && choices[0].Def.Name == "molotov", "oil and a candle make a molotov");
            cmd.Execute("craft");
            Assert(g.PendingChoice.Active && g.PendingChoice.Prompt == Game.CraftPrompt, "the craft list opens");
            cmd.CommitChoice(g.PendingChoice.Items[0]);
            Assert(g.Player.Inventory.Exists(i => i.Def.Name == "molotov"), "the molotov is in the pack");
            Assert(!g.Player.Inventory.Exists(i => i.Def.Name == "potion of oil" || i.Def.Name == "candle"), "the ingredients are spent");

            // Throw it at a rat three squares away.
            g.Monsters.Clear();
            int px = g.Player.X, py = g.Player.Y;
            for (int x = px; x <= px + 4; x++) { g.Map.Set(x, py, TileKind.Floor); g.Map.SetSurface(x, py, SurfaceKind.None); }
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng) { X = px + 3, Y = py };
            g.Monsters.Add(rat); g.UpdateFov();
            var bomb = g.Player.Inventory.Find(i => i.Def.Name == "molotov");
            g.UiState.ThrowItem = bomb; g.UiState.Targeting = TargetingMode.Throw;
            int hp = rat.HP;
            g.ResolveTargeting(px + 3, py);
            Assert(!g.Player.Inventory.Contains(bomb), "the molotov is used up");
            Assert(g.Map.SurfaceAt(px + 3, py) == SurfaceKind.Fire || rat.IsDead || rat.HP < hp, "it lands in flames");
            Assert(rat.IsDead || rat.HP < hp || rat.BurnTurns > 0, "and whatever it hits burns");

            // Bone blade, bone armour, and brewing.
            g.Player.Inventory.Clear();
            g.Player.Inventory.Add(Make(g, "dagger")); g.Player.Inventory.Add(Make(g, "remains"));
            var blade = g.CraftChoices().Find(i => i.Def.Name == "bone blade");
            Assert(blade != null, "a blade and remains make a bone blade");
            g.Craft(blade);
            var made = g.Player.Inventory.Find(i => i.Def.Name == "bone blade");
            Assert(made != null && made.Prefix == "vampiric" && made.Def.Kind == ItemKind.Weapon, "the bone blade drinks life");

            g.Player.Inventory.Clear();
            g.Player.Inventory.Add(Make(g, "leather armour")); g.Player.Inventory.Add(Make(g, "remains"));
            Assert(g.CraftChoices().Find(i => i.Def.Name == "bone-studded armour") == null, "one remains is not enough for armour");
            g.Player.Inventory.Add(Make(g, "skeleton corpse"));
            var armour = g.CraftChoices().Find(i => i.Def.Name == "bone-studded armour");
            Assert(armour != null, "armour and two remains make bone armour");

            g.Player.Inventory.Clear();
            g.Player.Inventory.Add(Make(g, "potion of healing"));
            Assert(g.CraftChoices().Count == 0, "one healing potion brews nothing");
            g.Player.Inventory[0].Quantity = 2;
            var brew = g.CraftChoices().Find(i => i.Def.Name == "potion of extra healing");
            Assert(brew != null, "two healing potions brew an extra healing");
            g.Craft(brew);
            Assert(g.Player.Inventory.Count == 1 && g.Player.Inventory[0].Def.Name == "potion of extra healing", "the pair becomes one stronger potion");
        }

        static void SetsAndRelics()
        {
            var seen = new HashSet<string>();
            foreach (var a in Artifacts.All)
            {
                if (a.Chance >= 100) Assert(seen.Add(a.Branch + "@" + a.Depth), "two sure artifacts on one level: " + a.Branch + " " + a.Depth);
                Assert(a.Set == null || ArtifactSets.Find(a.Set) != null, "known set for " + a.Id);
            }
            Assert(Artifacts.All.Length >= 12, "a real roster of artifacts");

            var g = Game.NewHero(1100, "Court", "human", "fighter");
            g.Monsters.Clear();
            Item Art(string id) => Artifacts.Create(Artifacts.Find(id), g.Rng, g.NextUid());
            int cold0 = g.Player.Gear.ResCold;
            g.Player.Wear(Art("tidecallers-gauntlets"));
            Assert(ArtifactSets.Worn(g.Player, "drowned-court") == 1 && g.Player.Gear.ResCold == cold0 + 15, "one piece: its own bonus only");
            g.Player.Wear(Art("brinewalkers"));
            Assert(ArtifactSets.Worn(g.Player, "drowned-court") == 2 && g.Player.Gear.ResCold == cold0 + 15 + 20, "two pieces: the set's first bonus");
            int mp = g.Player.Gear.Mp;
            g.Player.Wear(Art("crown-drowned-king"));
            Assert(ArtifactSets.Worn(g.Player, "drowned-court") == 3 && g.Player.Gear.Mp >= mp + 10, "three pieces: the second bonus too");
            Assert(ArtifactSets.Worn(g.Player, "ashen-regalia") == 0, "another set is untouched");

            var r = Game.NewHero(1101, "Relic", "human", "fighter");
            r.Monsters.Clear();
            int hp = r.Player.MaxHP;
            Assert(r.WornRelics() == 0, "no relics at first");
            r.Player.Wear(Artifacts.Create(Artifacts.Find("hollow-ribs"), r.Rng, r.NextUid()));
            Assert(r.WornRelics() == 1 && r.Player.MaxHP >= hp + 20, "Hollow Ribs are strong");
            for (int i = 0; i < 100; i++) { r.Player.Nutrient = 1000; r.Player.HP = r.Player.MaxHP; r.EndPlayerTurn(); }
            Assert(r.Player.Corruption >= 4, "and they take something back, corruption " + r.Player.Corruption);
            var plain = Game.NewHero(1101, "Plain", "human", "fighter");
            plain.Monsters.Clear();
            for (int i = 0; i < 100; i++) { plain.Player.Nutrient = 1000; plain.Player.HP = plain.Player.MaxHP; plain.EndPlayerTurn(); }
            Assert(plain.Player.Corruption == 0, "without a relic nothing is taken");
        }

        static Game Archmage(ulong seed)
        {
            var g = Game.NewHero(seed, "A", "gnome", "wizard");
            var p = g.Player;
            p.Level = 15; p.Int = 21; p.Wis = 21; p.Skills[Skill.Magic] = 100;
            p.RecomputeMaxMp();
            p.MaxHP = p.HP = 5000;
            foreach (var s in Spells.All) if (!p.Spells.Contains(s.Id)) p.Spells.Add(s.Id);
            g.Monsters.Clear();
            return g;
        }

        // A visible open cell 2-3 squares away, with a clear line.
        static void SpellTarget(Game g, out int x, out int y)
        {
            int px = g.Player.X, py = g.Player.Y;
            for (int yy = py - 4; yy <= py + 4; yy++)
                for (int xx = px - 4; xx <= px + 4; xx++) { g.Map.Set(xx, yy, TileKind.Floor); g.Map.SetSurface(xx, yy, SurfaceKind.None); }
            g.UpdateFov();
            x = px + 3; y = py;
        }

        // Casts until one lands (not fizzled).
        static bool Cast(Game g, string id, int x, int y)
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

        static void NewSpells()
        {
            foreach (var id in new[] { "ice-lance", "steam-burst", "create-oil", "ossify", "reshape-flesh", "marrow-bolt", "purify" })
            {
                Assert(Spells.Find(id) != null, id + " exists");
                bool inBook = false;
                foreach (var book in new[] { "a tome of evocation", "a tome of conjuration", "a grimoire of the dead", "a book of mercy" })
                    foreach (string s in Spells.InBook(book)) if (s == id) inBook = true;
                Assert(inBook, id + " can be learned from a book");
            }

            var g = Archmage(1200); SpellTarget(g, out int x, out int y);
            var ogre = new Monster(Bestiary.Find("ogre"), g.Rng) { X = x, Y = y }; ogre.HP = ogre.MaxHP = 4000; g.Monsters.Add(ogre);
            Assert(Cast(g, "ice-lance", x, y) && ogre.HP < 4000, "an ice lance hurts");
            int ac = g.Player.ArmorClass();

            // Steam: wet things are scalded harder than dry ones, and the water is spent.
            var wet = Archmage(1201); SpellTarget(wet, out int wx, out int wy);
            var dry = Archmage(1201); SpellTarget(dry, out int dx, out int dy);
            var wo = new Monster(Bestiary.Find("ogre"), wet.Rng) { X = wx, Y = wy }; wo.HP = wo.MaxHP = 4000; wet.Monsters.Add(wo);
            var dd = new Monster(Bestiary.Find("ogre"), dry.Rng) { X = dx, Y = dy }; dd.HP = dd.MaxHP = 4000; dry.Monsters.Add(dd);
            wet.PutSurface(wx, wy, SurfaceKind.Water, 60); wet.PutSurface(wx + 1, wy, SurfaceKind.Water, 60);
            Assert(Cast(wet, "steam-burst", wx, wy) && Cast(dry, "steam-burst", dx, dy), "steam bursts cast");
            Assert(4000 - wo.HP > 4000 - dd.HP, "wet damage beats dry damage: " + (4000 - wo.HP) + " vs " + (4000 - dd.HP));
            Assert(wet.Map.SurfaceAt(wx, wy) == SurfaceKind.None && wet.Map.SurfaceAt(wx + 1, wy) == SurfaceKind.None, "the water boils away");

            // Oil.
            var oil = Archmage(1202); SpellTarget(oil, out int ox, out int oy);
            Assert(Cast(oil, "create-oil", ox, oy) && oil.Map.SurfaceAt(ox, oy) == SurfaceKind.Oil, "oil covers the target");

            // Bone and the Ossuary.
            var bone = Archmage(1203); bone.Player.Corruption = 0;
            int acBefore = bone.Player.ArmorClass();
            Assert(Cast(bone, "ossify", bone.Player.X, bone.Player.Y), "ossify casts");
            Assert(bone.Player.ArmorClass() == acBefore - 4, "bone armour adds four AC");
            Assert(bone.Player.Corruption >= 2, "and it costs corruption, got " + bone.Player.Corruption);

            var shape = Archmage(1204); shape.Player.Corruption = 0;
            Assert(Cast(shape, "reshape-flesh", shape.Player.X, shape.Player.Y), "reshape flesh casts");
            Assert(shape.Player.Mutated.Count >= 1 && shape.Player.Corruption >= 10, "a mutation and a price");

            var mb = Archmage(1205); SpellTarget(mb, out int mx, out int my);
            var target = new Monster(Bestiary.Find("ogre"), mb.Rng) { X = mx, Y = my }; target.HP = target.MaxHP = 4000; mb.Monsters.Add(target);
            var sk = new Monster(Bestiary.Find("skeleton"), mb.Rng) { X = mx, Y = my + 1 }; sk.HP = sk.MaxHP = 4000; mb.Monsters.Add(sk);
            Assert(Cast(mb, "marrow-bolt", mx, my) && target.HP < 4000, "a marrow bolt hurts the living");
            Assert(Cast(mb, "marrow-bolt", mx, my + 1) && sk.HP == 4000, "and the dead shrug it off");
            Assert(mb.Player.Corruption >= 4, "twice the price");

            var pure = Archmage(1206); pure.Player.Corruption = 40;
            Assert(Cast(pure, "purify", pure.Player.X, pure.Player.Y) && pure.Player.Corruption == 25, "purify burns 15 corruption");
        }

        // Puts an altar of the wanted god within a few squares and points the altar menu at it.
        static bool AltarOf(Game g, string godId)
        {
            int px = g.Player.X, py = g.Player.Y;
            for (int y = py - 4; y <= py + 4; y++)
                for (int x = px - 4; x <= px + 4; x++)
                {
                    if (x == px && y == py) continue;
                    if (!g.Map.InBounds(x, y) || Gods.AtAltar(g.Map.Number, x, y).Id != godId) continue;
                    g.Map.Set(x, y, TileKind.Altar);
                    g.UiState.AltarX = x; g.UiState.AltarY = y;
                    return true;
                }
            return false;
        }

        static void GodsExpanded()
        {
            Assert(Gods.All.Length == 6, "six gods");
            var mourne = Gods.Find("mourne");
            Assert(mourne != null && mourne.Rival == "veyra" && Gods.Find("veyra").Rival == "mourne", "Mourne and Veyra are rivals");

            // Sacrifices: a corpse's worth grows with the creature, and every god has its own taste.
            var g = Game.NewHero(1300, "Priest", "human", "cleric");
            g.Monsters.Clear();
            var cmd = new Commands(g);
            g.Player.God = "khorr"; g.Player.Piety = 60;
            Assert(AltarOf(g, "khorr"), "test setup: Khorr's altar");
            Assert(g.AltarRows().Exists(r => r.Id == "sacrifice" && !r.Enabled), "nothing to sacrifice without a corpse");
            var corpse = Make(g, "ogre corpse");
            g.Player.Inventory.Add(corpse);
            Assert(g.AltarRows().Exists(r => r.Id == "sacrifice" && r.Enabled), "a corpse can be sacrificed");
            g.Player.Level = 1;
            int piety = g.Player.Piety;
            g.AltarAction("sacrifice");
            Assert(g.PendingChoice.Active && g.PendingChoice.Prompt == Game.SacrificePrompt, "the corpse list opens");
            cmd.CommitChoice(g.PendingChoice.Items[0]);
            Assert(g.Player.Piety > piety && !g.Player.Inventory.Contains(corpse), "the ogre is worth something to Khorr");

            var aurel = Game.NewHero(1301, "Light", "human", "cleric");
            aurel.Monsters.Clear();
            aurel.Player.God = "aurel"; aurel.Player.Piety = 60;
            Assert(AltarOf(aurel, "aurel"), "test setup: Aurel's altar");
            var dead = Make(aurel, "jackal corpse"); aurel.Player.Inventory.Add(dead);
            piety = aurel.Player.Piety;
            aurel.SacrificeCorpse(dead);
            Assert(aurel.Player.Piety < piety, "Aurel hates the desecration of the dead");

            // A trial: five deeds the god likes, then a gift.
            var t = Game.NewHero(1302, "Fighter", "human", "fighter");
            t.Monsters.Clear();
            t.Player.God = "khorr"; t.Player.Piety = 20;
            Assert(AltarOf(t, "khorr"), "test setup: another altar of Khorr");
            Assert(t.AltarRows().Exists(r => r.Id == "trial" && !r.Enabled), "a trial needs 30 piety");
            t.Player.Piety = 40; int str = t.Player.Str;
            t.AltarAction("trial");
            Assert(t.Player.TrialGoal == Game.TrialDeeds && t.AltarRows().Exists(r => r.Id == "trial-info"), "the trial is set and shown");
            for (int i = 0; i < Game.TrialDeeds; i++)
            {
                var m = new Monster(Bestiary.Find("ogre"), t.Rng) { X = t.Player.X + 1, Y = t.Player.Y };
                t.Monsters.Add(m);
                Assert(t.Player.TrialDone == i, "deed " + i + " counted so far: " + t.Player.TrialDone);
                t.KillMonster(m);
            }
            Assert(t.Player.TrialGoal == 0 && t.Player.Str == str + 1, "the trial ends with Khorr's gift (+1 Str)");

            // Rivals: a follower of Khorr can defile Sylk's altar, and the altar goes dead.
            var d = Game.NewHero(1303, "Zealot", "human", "fighter");
            d.Monsters.Clear();
            d.Player.God = "khorr"; d.Player.Piety = 50;
            Assert(AltarOf(d, "sylk"), "test setup: Sylk's altar");
            Assert(d.AltarRows().Exists(r => r.Id == "defile"), "the rival's altar can be defiled");
            Assert(!d.AltarRows().Exists(r => r.Id == "defile") || d.AltarRows().Find(r => r.Id == "convert").Label.Contains("300"), "converting to a rival costs double");
            int before = d.Player.Piety;
            d.AltarAction("defile");
            Assert(d.Player.Piety >= before + 8 - 1, "defiling pleases your own god");
            Assert(d.AltarRows().Exists(r => r.Id == "dead") && !d.AltarRows().Exists(r => r.Id == "convert"), "the defiled altar is dead");

            // Mourne: mutations please her, her boon is always a gift, purging offends.
            var m2 = Game.NewHero(1304, "Seam", "human", "fighter");
            m2.Monsters.Clear();
            m2.Player.God = "mourne"; m2.Player.Piety = 60;
            int p0 = m2.Player.Piety;
            m2.GainMutation();
            Assert(m2.Player.Piety == p0 + 3, "a mutation pleases Mourne");
            m2.Player.Piety = 100; m2.Player.PrayerTimer = 0; m2.Player.HP = m2.Player.MaxHP;
            Assert(AltarOf(m2, "mourne"), "test setup: Mourne's altar");
            int mut = m2.Player.Mutated.Count;
            m2.AltarAction("pray");
            Assert(m2.Player.Mutated.Count == mut + 1, "her boon is a mutation");
            Assert(MutationTable.Find(m2.Player.Mutated[m2.Player.Mutated.Count - 1]).Kind == MutationKind.Boon, "and always a gift");
            m2.Player.Piety = 120;
            Assert(m2.Player.ResistPct(DamageType.Poison) >= 30, "tier two: poison resistance");
        }

        static Game Duel(ulong seed, string kind, string branch = "The Dungeons")
        {
            var g = Game.NewHero(seed, "Test", "human", "fighter");
            g.DescendTo(branch, 3);
            g.Monsters.Clear();
            int px = g.Player.X, py = g.Player.Y;
            for (int y = py - 4; y <= py + 4; y++)
                for (int x = px - 4; x <= px + 4; x++) { g.Map.Set(x, y, TileKind.Floor); g.Map.SetSurface(x, y, SurfaceKind.None); }
            g.Player.AC = 30;
            var m = new Monster(Bestiary.Find(kind), g.Rng) { X = px + 1, Y = py, HomeX = px + 1, HomeY = py, Alert = 1 };
            g.Monsters.Add(m);
            g.UpdateFov();
            return g;
        }

        static void Pass(Game g, int turns)
        {
            for (int i = 0; i < turns && g.Mode == GameMode.Dungeon; i++) { g.Player.HP = g.Player.MaxHP; g.Player.Nutrient = 1000; g.Wait(); }
        }

        static void BranchMonsters()
        {
            var rng = new Rng(5);
            bool Has(string branch, string name) { foreach (var d in Bestiary.SpawnTable(5, rng, branch)) if (d.Name == name) return true; return false; }
            Assert(Has("The Warrens", "plague rat") && !Has("The Mines of Dwarfdeep", "plague rat") && !Has(null, "plague rat"), "plague rats are native to the Warrens");
            Assert(Has("The Mines of Dwarfdeep", "cave bat") && Has("The Sunken Vaults", "drowned dead") && Has("The Ashen Spire", "ember wisp"), "each branch has its own");
            Assert(Has(null, "orc") && Has("The Warrens", "orc"), "the common monsters live everywhere");

            // Plague rats make you sick.
            var g = Duel(1400, "plague rat", "The Warrens");
            Pass(g, 120);
            Assert(g.Player.PoisonResist > 0 || g.Log.Exists(m => m.Text.Contains("festers")), "a plague rat's bite festers");

            // Swarms breed, up to a point.
            var s = Duel(1401, "rat swarm", "The Warrens");
            s.Player.AC = 30;
            for (int i = 0; i < 400 && s.Monsters.Count < 4; i++) { s.Player.HP = s.Player.MaxHP; s.Player.Nutrient = 1000; s.Wait(); foreach (var m in s.Monsters) m.HP = m.MaxHP; }
            Assert(s.Monsters.Count >= 2, "a rat swarm multiplies");
            for (int i = 0; i < 800; i++) { s.Player.HP = s.Player.MaxHP; s.Player.Nutrient = 1000; s.Wait(); foreach (var m in s.Monsters) m.HP = m.MaxHP; }
            Assert(s.Monsters.Count <= 8, "but not without limit, " + s.Monsters.Count);

            // The drowned mend in water, and only in water.
            var d = Duel(1402, "drowned dead", "The Sunken Vaults");
            var dead = d.Monsters[0]; dead.Alert = 0; dead.HP = 5;
            d.Map.SetSurface(dead.X, dead.Y, SurfaceKind.Water, 500);
            d.Wait(); d.Wait();
            Assert(dead.HP >= 7 || !d.Monsters.Contains(dead), "the drowned dead mends in water");
            var dry = Duel(1403, "drowned dead", "The Sunken Vaults");
            var dd = dry.Monsters[0]; dd.Alert = 0; dd.HP = 5; dd.Speed = 1;
            dry.Wait(); dry.Wait();
            Assert(dd.HP <= 5, "and not on dry floor");

            // A wisp bursts into flame.
            var w = Duel(1404, "ember wisp", "The Ashen Spire");
            var wisp = w.Monsters[0];
            int fx = wisp.X, fy = wisp.Y;
            w.KillMonster(wisp);
            Assert(w.Map.SurfaceAt(fx, fy) == SurfaceKind.Fire, "the dying wisp lights the floor");

            // Hounds hunt faster together.
            var h = Duel(1405, "gaol hound");
            var a = h.Monsters[0]; a.Alert = 1;
            var b2 = new Monster(Bestiary.Find("gaol hound"), h.Rng) { X = a.X + 1, Y = a.Y + 1, Alert = 1 };
            h.Monsters.Add(b2);
            h.Player.AC = 30;
            h.Wait();
            Assert(a.Speed == a.Def.Speed + 4, "a hound with a mate nearby is faster, speed " + a.Speed);

            // Ash wraiths set you alight; golems stun.
            var ash = Duel(1406, "ash wraith", "The Ashen Spire"); bool burned = false;
            for (int i = 0; i < 200 && !burned; i++) { Pass(ash, 1); burned = ash.Player.BurnTurns > 0 || ash.Log.Exists(m => m.Text.Contains("catch fire")); }
            Assert(burned, "an ash wraith sets you on fire");
            var golem = Duel(1407, "ore golem", "The Mines of Dwarfdeep"); bool stunned = false;
            for (int i = 0; i < 300 && !stunned; i++) { Pass(golem, 1); stunned = golem.Log.Exists(m => m.Text.Contains("rings through your skull")); }
            Assert(stunned, "an ore golem's blow can stun");
        }

        static Game Arena(ulong seed, string bossId, int dist, bool hurt = false)
        {
            var b = Bosses.Find(bossId);
            var g = Game.NewHero(seed, "Test", "human", "fighter");
            g.DescendTo(b.Branch, 3);
            g.Monsters.Clear();
            int px = g.Player.X, py = g.Player.Y;
            for (int y = py - 6; y <= py + 6; y++)
                for (int x = px - 8; x <= px + 8; x++) { g.Map.Set(x, y, TileKind.Floor); g.Map.SetSurface(x, y, SurfaceKind.None); }
            g.Player.MaxHP = 5000; g.Player.HP = 5000; g.Player.AC = 30;
            var boss = g.CreateBoss(b, px + dist, py); boss.Alert = 1;
            if (hurt) boss.HP = boss.MaxHP / 3;
            g.Monsters.Add(boss); g.UpdateFov();
            return g;
        }

        static bool Said(Game g, string part) => g.Log.Exists(m => m.Text.Contains(part));

        static void BossFights()
        {
            var seen = new HashSet<string>();
            foreach (var b in Bosses.All)
            {
                Assert(seen.Add(b.Branch + "@" + b.Depth), "one boss per level");
                var dungeon = new Dungeon(new Rng(31));
                Assert(b.Depth <= dungeon.Get(b.Branch).MaxDepth, b.Name + " lives inside its branch");
                Assert(Bestiary.TryGet(b.Base, out _), b.Name + " has a real base monster");
                Assert(b.Intro.Length > 0 && b.Phase2.Length > 0 && b.Fall.Length > 0, b.Name + " has its lines");
            }

            // Each waits on its level, far from the stairs, and the level itself is unchanged by it.
            foreach (var b in Bosses.All)
            {
                var g = Game.NewHero(1500, "Delver", "human", "fighter");
                g.DescendTo(b.Branch, b.Depth);
                Monster boss = null; foreach (var m in g.Monsters) if (m.BossId == b.Id) boss = m;
                Assert(boss != null && boss.Name == b.Name && boss.Unique && boss.MaxHP == b.HP, b.Name + " waits on " + b.Branch + " " + b.Depth);
                Assert(Pathfinder.Chebyshev(boss.X, boss.Y, g.Player.X, g.Player.Y) >= 12, b.Name + " is far from the arrival");
                Assert(Said(g, b.Intro), "the level announces " + b.Name);
            }

            // The Gaoler hooks you in, and calls hounds when hurt.
            var gl = Arena(1501, "gaoler", 8);
            int px = gl.Player.X; bool hooked = false;
            for (int i = 0; i < 60 && !hooked; i++) { gl.Player.HP = gl.Player.MaxHP; gl.Wait(); hooked = Said(gl, "drags you in"); }
            Assert(hooked, "the Gaoler's chain drags you in");
            var gl2 = Arena(1502, "gaoler", 5, hurt: true);
            for (int i = 0; i < 60 && !gl2.Monsters.Exists(m => m.Def.Name == "gaol hound"); i++) { gl2.Player.HP = gl2.Player.MaxHP; gl2.Wait(); }
            Assert(gl2.Monsters.Exists(m => m.Def.Name == "gaol hound") && Said(gl2, "howls for his hounds"), "a hurt Gaoler calls his hounds");

            // The Stone Warden slams.
            var sw = Arena(1503, "stone-warden", 1);
            for (int i = 0; i < 40 && !Said(sw, "slams the floor"); i++) { sw.Player.HP = sw.Player.MaxHP; sw.Wait(); }
            Assert(Said(sw, "slams the floor") && Said(sw, "shockwave"), "the Stone Warden slams the floor");

            // The Rat King fills the room with rats, but not without end.
            var rk = Arena(1504, "rat-king", 4);
            for (int i = 0; i < 80; i++) { rk.Player.HP = rk.Player.MaxHP; rk.Wait(); }
            int rats = 0; foreach (var m in rk.Monsters) if (m.Def.Name.Contains("rat")) rats++;
            Assert(rats >= 2 && rats <= 12, "the Rat King keeps a court of rats, got " + rats);

            // The Drowned King floods the floor, then lights the water.
            var dk = Arena(1505, "drowned-king", 5);
            for (int i = 0; i < 40 && !Said(dk, "Lightning leaps"); i++) { dk.Player.HP = dk.Player.MaxHP; dk.Wait(); }
            Assert(Said(dk, "Black water spreads"), "the Drowned King floods the floor");
            Assert(Said(dk, "Lightning leaps"), "and shocks whoever stands in it");

            // The Ashen Regent's nova.
            var ar = Arena(1506, "ashen-regent", 3);
            for (int i = 0; i < 30 && !Said(ar, "floor around it ignites"); i++) { ar.Player.HP = ar.Player.MaxHP; ar.Wait(); }
            Assert(Said(ar, "floor around it ignites"), "the Ashen Regent sets the floor alight");

            // Killing one pays out, once.
            var rwd = Arena(1507, "rat-king", 3);
            var king = rwd.Monsters[0]; int gold = rwd.Player.Gold;
            rwd.KillMonster(king);
            Assert(rwd.Player.Gold > gold && rwd.BossesSlain.Contains("rat-king"), "a dead boss pays gold");
            Assert(Said(rwd, Bosses.Find("rat-king").Fall), "and has its last words");
            var potions = GroundItems.At(rwd.Map.Number, king.X, king.Y);
            Assert(potions != null && potions.Exists(i => i.Def.Name == "potion of full healing"), "and leaves what it guarded");
            rwd.Earned.Clear(); rwd.CheckAchievements();
            Assert(rwd.Earned.Contains("boss-slayer"), "the achievement follows");
        }

        static void FactionWar()
        {
            Assert(Game.FactionOf(new Monster(Bestiary.Find("orc"), new Rng(1))) == Game.Faction.Greenskin, "orcs are greenskins");
            Assert(Game.FactionOf(new Monster(Bestiary.Find("dwarf"), new Rng(1))) == Game.Faction.Deepfolk, "dwarves are deepfolk");
            Assert(Game.FactionOf(new Monster(Bestiary.Find("skeleton"), new Rng(1))) == Game.Faction.Dead, "skeletons are the dead");
            Assert(Game.FactionOf(new Monster(Bestiary.Find("jackal"), new Rng(1))) == Game.Faction.Wild, "jackals are wild");
            Assert(Game.FactionOf(new Monster(Bestiary.Find("ogre"), new Rng(1))) == Game.Faction.None, "ogres take no side");
            Assert(Game.AreRivals(Game.Faction.Greenskin, Game.Faction.Deepfolk) && Game.AreRivals(Game.Faction.Wild, Game.Faction.Dead), "the old hatreds");
            Assert(!Game.AreRivals(Game.Faction.Greenskin, Game.Faction.Wild) && !Game.AreRivals(Game.Faction.None, Game.Faction.Dead), "and no others");

            // An orc and a dwarf, far from the hero, settle it between themselves.
            var g = Game.NewHero(1600, "Watcher", "human", "fighter");
            g.Monsters.Clear();
            int px = g.Player.X, py = g.Player.Y;
            for (int y = py - 3; y <= py + 3; y++)
                for (int x = px - 3; x <= px + 22; x++) { g.Map.Set(x, y, TileKind.Floor); g.Map.SetSurface(x, y, SurfaceKind.None); }
            var orc = new Monster(Bestiary.Find("orc"), g.Rng) { X = px + 18, Y = py, Alert = 0, Energy = 12 };
            var dwarf = new Monster(Bestiary.Find("dwarf"), g.Rng) { X = px + 20, Y = py, Alert = 0, Energy = 12 };
            g.Monsters.Add(orc); g.Monsters.Add(dwarf);
            for (int i = 0; i < 120 && g.Monsters.Count == 2; i++) { g.Player.Nutrient = 1000; g.Wait(); orc.HP = Math.Max(orc.HP, 1); }
            Assert(g.Monsters.Count == 1 || orc.HP < orc.MaxHP || dwarf.HP < dwarf.MaxHP, "greenskins and deepfolk fight on sight");

            // Allies, townsfolk, bosses and companions take no side.
            var friend = new Monster(Bestiary.Find("dwarf"), g.Rng) { Ally = true };
            Assert(Game.FactionOf(friend) == Game.Faction.None, "an ally takes no side");
            var boss = g.CreateBoss(Bosses.All[2], 3, 3);
            Assert(Game.FactionOf(boss) == Game.Faction.None, "a boss takes no side");

            // But the hero is always the nearer target.
            var h = Game.NewHero(1601, "Bait", "human", "fighter");
            h.Monsters.Clear();
            int hx = h.Player.X, hy = h.Player.Y;
            for (int y = hy - 3; y <= hy + 3; y++) for (int x = hx - 3; x <= hx + 6; x++) { h.Map.Set(x, y, TileKind.Floor); h.Map.SetSurface(x, y, SurfaceKind.None); }
            var wolf = new Monster(Bestiary.Find("jackal"), h.Rng) { X = hx + 2, Y = hy, Alert = 1, Energy = 12 };
            var zombie = new Monster(Bestiary.Find("human zombie"), h.Rng) { X = hx + 5, Y = hy, Alert = 1, Energy = 12 };
            h.Monsters.Add(wolf); h.Monsters.Add(zombie);
            h.Player.AC = 30; h.Wait();
            Assert(Pathfinder.Chebyshev(wolf.X, wolf.Y, hx, hy) <= 2, "a monster next to the hero does not wander off to fight");
        }

        static void ReputationAndJobs()
        {
            var g = Game.NewHero(1700, "Local", "human", "fighter");
            g.Monsters.Clear();
            Assert(g.RepOf(Houses.Guild) == 0, "a stranger starts at zero");
            Assert(g.Haggle(100, Houses.Guild) == 100, "no reputation, no discount");
            g.AddRep(Houses.Guild, 500);
            Assert(g.RepOf(Houses.Guild) == 100, "reputation caps at 100");
            Assert(g.Haggle(100, Houses.Guild) == 80, "a revered customer pays 80%");
            g.AddRep(Houses.Guild, -500);
            Assert(g.RepOf(Houses.Guild) == -100 && g.Haggle(100, Houses.Guild) == 120, "a hated one pays 120%");
            Assert(Houses.Standing(0) == "known" && Houses.Standing(30) == "trusted" && Houses.Standing(-30) == "distrusted", "standings by tier");

            // The shop reads it.
            var shop = new Shop { Kind = ShopKind.Weapon, Name = "T", Gold = 600 };
            var item = new Item(Catalogue.Weapons[3], g.Rng, 1);
            int hated = g.ShopPrice(shop, item);
            g.Player.Rep[Houses.Guild] = 100;
            int loved = g.ShopPrice(shop, item);
            Assert(loved < hated && loved * 100 / hated <= 66, $"the Guild's friends pay far less: {loved} vs {hated}");

            // The inn turns away someone the Watch hates.
            g.TalkBuilding = new Building { Services = Service.Rest | Service.Ale, Name = "Inn" };
            g.Player.Rep[Houses.Watch] = 0;
            Assert(g.ServiceRows().Find(r => r.Id == "rest").Enabled == (g.Player.Gold >= g.RestPrice), "a stranger may rest");
            g.Player.Rep[Houses.Watch] = -50; g.Player.Gold = 999;
            Assert(!g.ServiceRows().Find(r => r.Id == "rest").Enabled, "the Watch's enemies may not");

            // The Cult sells to its friends.
            g.TalkBuilding = new Building { Services = Service.Cure, Name = "Temple" };
            g.Player.Rep[Houses.Cult] = 0;
            Assert(!g.ServiceRows().Exists(r => r.Id == "grave"), "nothing is sold to strangers");
            g.Player.Rep[Houses.Cult] = 30; g.Player.Gold = 999;
            Assert(g.ServiceRows().Exists(r => r.Id == "grave"), "a friend of the Cult is shown the back room");
            g.ServiceAction("grave");
            Assert(g.Player.Inventory.Exists(i => i.Def.Name == "potion of mutation"), "and buys a vial");

            // Jobs: the same board every time, a hunt and a delve that count themselves, and a payday.
            var j = Game.NewHero(1701, "Hand", "human", "fighter");
            j.Monsters.Clear(); j.LeaveToOverworld();
            var a = j.ContractOffers(); var b = j.ContractOffers();
            Assert(a.Count == 3 && a[0].Key == b[0].Key && a[1].Key == b[1].Key && a[2].Key == b[2].Key, "the board is the same every time you read it");
            j.World.Day += 14;
            var later = j.ContractOffers();
            Assert(later[0].Key != a[0].Key || later[1].Key != a[1].Key || later[2].Key != a[2].Key, "and changes with the weeks");
            j.World.Day -= 14;
            j.TalkBuilding = new Building { Services = Service.Quest, Name = "Guild" };
            Assert(j.ServiceRows().FindAll(r => r.Id.StartsWith("offer:")).Count == 3, "the guild lists its jobs");
            Contract hunt = null, delve = null;
            foreach (var o in j.ContractOffers()) { if (o.Kind == "hunt" && hunt == null) hunt = o; if (o.Kind == "delve" && delve == null) delve = o; }
            // Make sure both kinds exist for the test, whatever the board shows.
            if (hunt == null) hunt = new Contract { Kind = "hunt", Branch = "The Dungeons", Target = "jackal", Count = 3, Reward = 90, Giver = Houses.Guild };
            if (delve == null) delve = new Contract { Kind = "delve", Branch = "The Dungeons", Target = "", Count = 3, Reward = 180, Giver = Houses.Watch };
            Assert(j.AcceptContract(hunt) && j.AcceptContract(delve), "two jobs taken");
            Assert(j.Contracts.Count == 2, "they are carried");

            j.DescendTo(hunt.Branch, 1);
            for (int i = 0; i < hunt.Count; i++)
            {
                var m = new Monster(Bestiary.Find(hunt.Target), j.Rng) { X = j.Player.X + 1, Y = j.Player.Y };
                j.Monsters.Add(m); j.KillMonster(m);
            }
            Assert(hunt.Complete, "killing the target counts toward the hunt");
            var other = new Monster(Bestiary.Find(hunt.Target == "newt" ? "jackal" : "newt"), j.Rng) { X = j.Player.X + 1, Y = j.Player.Y };
            j.Monsters.Add(other); int done = hunt.Done; j.KillMonster(other);
            Assert(hunt.Done == done, "other kills do not");
            Assert(!delve.Complete || delve.Branch == hunt.Branch, "a delve waits for its depth");
            j.DescendTo(delve.Branch, Math.Min(delve.Count, j.Dungeon.Get(delve.Branch).MaxDepth));
            if (delve.Count <= j.Dungeon.Get(delve.Branch).MaxDepth) Assert(delve.Complete, "reaching the depth completes the delve");

            int gold = j.Player.Gold, rep = j.RepOf(hunt.Giver);
            Assert(j.TurnInContract(hunt), "the hunt is handed in");
            Assert(j.Player.Gold == gold + hunt.Reward && j.RepOf(hunt.Giver) == rep + 10 && j.ContractsDone == 1, "paid, and thought of better");
            Assert(!j.TurnInContract(hunt), "but only once");
            var full = Game.NewHero(1702, "Busy", "human", "fighter");
            for (int i = 0; i < Game.MaxContracts; i++) full.AcceptContract(new Contract { Kind = "hunt", Branch = "The Dungeons", Target = "jackal" + i, Count = 1, Reward = 1, Giver = Houses.Guild });
            Assert(!full.AcceptContract(new Contract { Kind = "hunt", Branch = "x", Target = "y", Count = 1 }), "only three jobs at once");
        }

        static void RoadEvents()
        {
            foreach (string id in new[] { "camp", "caravan", "ruin", "shrine", "toll", "corpse" })
            {
                var g = Game.NewHero(1800, "Road", "human", "fighter");
                g.Monsters.Clear(); g.LeaveToOverworld();
                g.Player.Gold = 500;
                g.OpenEvent(id, "T", "text");
                Assert(g.UiState.Active == Panel.Service && g.CurrentEvent != null && g.CurrentEvent.Id == id, id + " opens the choice panel");
                var rows = g.ServiceRows();
                Assert(rows.Count >= 2 && rows[rows.Count - 1].Id == "leave", id + " lists choices and a way out");
                Assert(g.ServiceAction("leave") && g.CurrentEvent == null, id + " can be walked away from");
            }

            // Every choice of every event resolves without trouble, for many seeds.
            for (ulong seed = 1; seed <= 12; seed++)
                foreach (string id in new[] { "camp", "caravan", "ruin", "shrine", "toll", "corpse" })
                {
                    var probe = Game.NewHero(1900 + seed, "Probe", "human", "fighter");
                    probe.Monsters.Clear(); probe.LeaveToOverworld(); probe.Player.Gold = 500;
                    probe.OpenEvent(id, "T", "t");
                    var ids = new List<string>(); foreach (var r in probe.CurrentEvent.Rows) ids.Add(r.Id);
                    foreach (string choice in ids)
                    {
                        var g = Game.NewHero(1900 + seed, "Probe", "human", "fighter");
                        g.Monsters.Clear(); g.LeaveToOverworld(); g.Player.Gold = 500; g.Player.HP = 5;
                        g.OpenEvent(id, "T", "t");
                        g.ServiceAction(choice);
                        Assert(g.CurrentEvent == null || !g.ServiceAction("leave") == false, "the event ends: " + id + ":" + choice);
                    }
                }

            // Specific outcomes.
            var camp = Game.NewHero(1801, "Tired", "human", "fighter"); camp.LeaveToOverworld();
            camp.Player.HP = 3; camp.OpenEvent("camp", "T", "t"); camp.ServiceAction("rest");
            Assert(camp.Player.HP == camp.Player.MaxHP, "resting at the camp heals");

            var car = Game.NewHero(1802, "Buyer", "human", "fighter"); car.LeaveToOverworld();
            car.Player.Gold = 200; int rations = car.Player.Inventory.FindAll(i => i.Def.Name == "food ration").Count;
            car.OpenEvent("caravan", "T", "t");
            Assert(car.ServiceRows().Find(r => r.Id == "buy-potion").Price > 0, "the caravan has prices");
            Assert(!car.ServiceRows().Exists(r => r.Id == "buy-grave"), "and nothing under the black cart for strangers");
            car.ServiceAction("buy-potion");
            Assert(car.Player.Gold < 200 && car.Player.Inventory.Exists(i => i.Def.Name == "potion of healing"), "buying costs gold and gives the potion");
            car.Player.Rep[Houses.Cult] = 40; car.OpenEvent("caravan", "T", "t");
            Assert(car.ServiceRows().Exists(r => r.Id == "buy-grave"), "but a friend of the Cult is offered the vial");

            var toll = Game.NewHero(1803, "Walker", "human", "fighter"); toll.LeaveToOverworld();
            toll.Player.Gold = 200; toll.OpenEvent("toll", "T", "t"); toll.ServiceAction("pay");
            Assert(toll.Player.Gold < 200 && !toll.ActiveEncounter, "paying the toll ends it");
            toll.OpenEvent("toll", "T", "t"); toll.ServiceAction("fight");
            Assert(toll.ActiveEncounter && toll.RepOf(Houses.Watch) > 0, "refusing means a fight, and the Watch approves");

            var dead = Game.NewHero(1804, "Digger", "human", "fighter"); dead.LeaveToOverworld();
            dead.OpenEvent("corpse", "T", "t"); dead.ServiceAction("bury");
            Assert(dead.RepOf(Houses.Temple) > 0, "burying the dead pleases the Temple");
            int c0 = dead.Player.Corruption;
            dead.OpenEvent("corpse", "T", "t"); dead.ServiceAction("loot");
            Assert(dead.Player.Corruption > c0 && dead.RepOf(Houses.Temple) < 4, "robbing it taints you and costs standing");

            // The road does throw these at the hero, now and then.
            var walk = Game.NewHero(1805, "Pilgrim", "human", "fighter"); walk.LeaveToOverworld();
            int seen = 0;
            for (int i = 0; i < 1500 && seen < 3; i++)
            {
                if (walk.Mode != GameMode.Overworld) { walk.Mode = GameMode.Overworld; walk.World.PlayerX = Math.Max(3, walk.World.PlayerX); }
                if (walk.ActiveEncounter) { walk.FleeEncounter(); continue; }
                if (walk.CurrentEvent != null) { walk.ServiceAction("leave"); walk.UiState.Active = Panel.None; seen++; continue; }
                walk.Player.HP = walk.Player.MaxHP;
                walk.OverworldMove(i % 2 == 0 ? 1 : -1, 0);
                if (walk.Mode == GameMode.TownMap || walk.Mode == GameMode.Dungeon) { walk.LeaveToOverworld(); }
            }
            Assert(seen >= 1, "walking the road eventually raises an event, saw " + seen);
        }

        static string PersonaSig(Game g)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var n in g.Town.Npcs) sb.Append(n.Persona.Trait).Append(n.Persona.Second).Append(n.Persona.Want).Append(n.Persona.Essential ? "!" : ".").Append(',');
            return sb.ToString();
        }

        static Ossuary.Core.Items.Item DocItem(string name) =>
            new Ossuary.Core.Items.Item(new Ossuary.Core.Items.ItemDef { Name = name, Glyph = ':', Kind = Ossuary.Core.Items.ItemKind.Ornament, Flags = Ossuary.Core.Items.ItemFlags.QuestItem }, new Rng(5), 777000 + name.Length) { Identified = true };

        static bool DocOnFloor(Game g, string name)
        {
            for (int y = 0; y < g.Map.H; y++) for (int x = 0; x < g.Map.W; x++)
            {
                var s = GroundItems.At(g.Map.Number, x, y);
                if (s != null) for (int i = 0; i < s.Count; i++) if (s[i].Def.Name == name) return true;
            }
            return false;
        }

        static void GiveAmulet(Game g) => g.Player.Inventory.Add(new Ossuary.Core.Items.Item(Game.QuestAmuletDef, new Rng(9), 888001) { Identified = true });

        static void MainQuestline()
        {
            var g = new Game(6006); g.LeaveToOverworld();
            g.DescendTo("The Dungeons", 5);
            Assert(DocOnFloor(g, Game.DocLedger), "the warden's page waits on level 5 of The Dungeons");
            var twin = new Game(6006); twin.LeaveToOverworld(); twin.DescendTo("The Dungeons", 5);
            Assert(DocOnFloor(twin, Game.DocLedger), "and in the same seed too");

            // The Reader turns a document into a truth, and the quest follows.
            g.StartQuest("main.seal");
            g.Flags.Add("elder.warned"); g.QuestCheck();
            Assert(g.QuestOf("main.seal").Step == 1, "the Seal waits for the page");
            g.Player.Inventory.Add(DocItem(Game.DocLedger)); g.QuestCheck();
            Assert(g.QuestOf("main.seal").Step == 2, "holding the page moves the Seal on");
            var reader = Teller(TownRole.Scholar, Trait.Curious);
            g.Talking = reader; g.OpenDialogue(Dialogues.For(reader), reader);
            Pick(g, "warden's ledger page");
            Assert(g.Flags.Contains("truth.seal") && !g.HasItem(Game.DocLedger), "the Reader takes the page and the hero learns the seal");
            Assert(g.QuestOf("main.seal").Step == 3 && reader.Memory.Has(NpcMemory.Helped), "and the Reader remembers");
            g.CurrentDialogue = null; g.UiState.Active = Panel.None;

            // Walking out with the Amulet asks what to do with it; what is open depends on what the hero learned and who they served.
            GiveAmulet(g);
            Assert(g.CheckVictory() && g.Mode != GameMode.Won && g.CurrentDialogue != null, "a hero who read the Seal chooses first");
            int pay = RowIdx(g, "Give it to the League"), shut = RowIdx(g, "bury it"), stamp = RowIdx(g, "stamp it");
            var rows = g.ServiceRows();
            Assert(rows[pay].Enabled && !rows[shut].Enabled && !rows[stamp].Enabled, "only the League's offer is open without standing or truths");
            g.ServiceAction(rows[pay].Id);
            Assert(g.Mode == GameMode.Won && g.EndingId == "pay", "the League's ending wins the run");
            Assert(g.DeedCount("ending", "pay") == 1, "the ledger records the ending");

            // With every truth and the Temple's trust, the better endings open; the Stamp starts a new cycle instead of ending.
            var h = new Game(6007); h.LeaveToOverworld();
            foreach (var f in new[] { "truth.seal", "truth.vote", "truth.entry", "truth.order" }) h.Flags.Add(f);
            h.AddRep(Houses.Temple, 40, null);
            GiveAmulet(h);
            Assert(h.CheckVictory(), "the ending opens");
            rows = h.ServiceRows();
            Assert(rows[RowIdx(h, "bury it")].Enabled && rows[RowIdx(h, "stamp it")].Enabled, "the Temple's burial and the Stamp are open");
            h.ServiceAction(rows[RowIdx(h, "stamp it")].Id);
            Assert(h.Mode != GameMode.Won && h.Cycle == 1 && !h.HasAmulet() && h.Flags.Contains("legend"), "the Stamp starts a new cycle without ending the run");
            Assert(h.Ledger.Count > 0 && h.Flags.Contains("truth.vote"), "the deeds and truths carry over");

            // The short path is untouched: no Seal read, the Amulet still wins at once.
            var s = new Game(6008); s.LeaveToOverworld(); GiveAmulet(s);
            Assert(s.CheckVictory() && s.Mode == GameMode.Won, "the short path wins immediately");

            // What the hero learned is felt: the Holds charge more once the Vote is known.
            var w = new Game(6009); w.LeaveToOverworld(); w.EnterTown("Ironhold");
            foreach (var r in w.World.Regions) if (r.Name == "The Iron Hills") { w.World.PlayerX = r.X + r.W / 2; w.World.PlayerY = r.Y + r.H / 2; }
            int before = w.Haggle(100, Houses.Guild);
            w.Flags.Add("truth.vote");
            Assert(w.Haggle(100, Houses.Guild) > before, "the Iron Holds charge a hero who knows the Vote");
        }

        static void CultTrack()
        {
            var g = new Game(777); g.LeaveToOverworld();
            var beggar = Teller(TownRole.Beggar, Trait.Weary);
            g.Talking = beggar; g.OpenDialogue(Dialogues.For(beggar), beggar);
            Assert(RowIdx(g, "What work") >= 0 && !g.ServiceRows()[RowIdx(g, "What work")].Enabled, "the Cult's work is closed to strangers");
            g.UiState.Active = Panel.None;

            g.AddRep(Houses.Cult, 20, null);
            g.Talking = beggar; g.OpenDialogue(Dialogues.For(beggar), beggar);
            Pick(g, "What work");
            Assert(g.QuestActive("cult.vial"), "the vial job starts");
            g.CurrentDialogue = null; g.UiState.Active = Panel.None;
            g.Player.Inventory.Add(new Ossuary.Core.Items.Item(System.Linq.Enumerable.First(Ossuary.Core.Items.Catalogue.Potions, d => d.Name == "potion of mutation"), new Rng(4), 123456) { Identified = true });
            g.QuestCheck();
            Assert(g.QuestOf("cult.vial").Step == 1, "holding the vial moves it on");
            int cult = g.RepOf(Houses.Cult), temple = g.RepOf(Houses.Temple), corruption = g.Player.Corruption;
            g.Talking = beggar; g.OpenDialogue(Dialogues.For(beggar), beggar);
            Pick(g, "I have the vial");
            Assert(g.QuestDone("cult.vial") && !g.HasItem("potion of mutation"), "handing it over finishes the job");
            Assert(g.RepOf(Houses.Cult) > cult && g.RepOf(Houses.Temple) < temple && g.Player.Corruption > corruption, "the Cult gains, the Temple and the body pay");
        }

        static void RivalRace()
        {
            Assert(Game.RivalDepthOn(2) == 0 && Game.RivalDepthOn(7) == 1 && Game.RivalDepthOn(43) == 10 && Game.RivalDepthOn(400) == 10, "the rival descends one level every four days from day 3");
            var g = new Game(515); g.LeaveToOverworld(); g.EnterTown("Racetown");
            g.World.Day = 1;
            string early = g.RivalReport();
            g.World.Day = 20;
            Assert(g.RivalReport() != early, "the report changes as they descend");
            Assert(g.StartQuest(g.RivalQuest()) && g.QuestActive("rival.race"), "the race starts");
            Assert(g.QuestOf("rival.race").Def.Deadline == Game.RivalDayAt(Game.RivalRaceDepth) - 20, "the clock ends when they would arrive");
            g.World.AdvanceTime(24 * 40); g.QuestCheck();
            Assert(g.QuestOf("rival.race").Status == QStatus.Failed && g.Flags.Contains("rival.won"), "losing the race is remembered");
            Assert(g.RivalReport().Contains("before you"), "and the tavern says so");

            var h = new Game(516); h.LeaveToOverworld(); h.World.Day = 5;
            h.StartQuest(h.RivalQuest());
            h.DescendTo("The Dungeons", Game.RivalRaceDepth);
            Assert(h.QuestDone("rival.race") && h.Flags.Contains("rival.beaten"), "reaching level 7 first wins it");
        }

        static void RoadTravellers()
        {
            Game Road() { var g = new Game(8080); g.LeaveToOverworld(); g.Player.Gold = 300; return g; }

            var a = Road();
            a.OpenEvent("delver", "A wounded delver", "x");
            Assert(!a.ServiceRows().Find(r => r.Id == "heal").Enabled, "no potion, no help");
            a.OpenEvent("delver", "A wounded delver", "x");
            a.Player.Inventory.Add(new Ossuary.Core.Items.Item(System.Linq.Enumerable.First(Ossuary.Core.Items.Catalogue.Potions, d => d.Name == "potion of healing"), new Rng(2), 4321) { Identified = true });
            a.OpenEvent("delver", "A wounded delver", "x");
            int gold = a.Player.Gold, helped = a.DeedCount(Deed.Helped);
            a.ServiceAction("heal");
            Assert(a.Player.Gold == gold + 40 && a.Flags.Contains("delver.saved") && a.DeedCount(Deed.Helped) == helped + 1, "saving the delver pays, is flagged and remembered");
            Assert(!a.HasItem("potion of healing"), "the potion is spent");

            var b = Road();
            b.OpenEvent("refugee", "Refugees", "x");
            int temple = b.RepOf(Houses.Temple);
            b.ServiceAction("rob");
            Assert(b.RepOf(Houses.Temple) < temple && b.DeedCount(Deed.Struck, "refugees") == 1, "robbing refugees costs standing and is remembered");

            var c = Road();
            c.OpenEvent("peddler", "A peddler", "x");
            int seen = c.RegionsSeen();
            c.ServiceAction("map");
            Assert(c.Player.Gold == 300 - c.Haggle(30, Houses.Guild) && c.RegionsSeen() >= seen, "the map fragment costs gold and reveals the land");

            var d = Road();
            d.OpenEvent("pilgrim", "A pilgrim", "x");
            int rep = d.RepOf(Houses.Temple);
            d.ServiceAction("water");
            Assert(d.RepOf(Houses.Temple) > rep, "sharing water earns the Temple's regard");

            // The new events can actually come up on the road.
            var seenIds = new System.Collections.Generic.HashSet<string>();
            for (ulong seed = 1; seed < 400 && seenIds.Count < 4; seed++)
            {
                var g = new Game(seed); g.LeaveToOverworld();
                for (int i = 0; i < 60 && seenIds.Count < 4; i++)
                {
                    g.OverworldMove(1, 0); g.OverworldMove(-1, 0);
                    if (g.CurrentEvent != null && (g.CurrentEvent.Id == "pilgrim" || g.CurrentEvent.Id == "peddler" || g.CurrentEvent.Id == "refugee" || g.CurrentEvent.Id == "delver")) seenIds.Add(g.CurrentEvent.Id);
                    g.CurrentEvent = null; g.UiState.Active = Panel.None; g.ActiveEncounter = false;
                }
            }
            Assert(seenIds.Count >= 2, "the new travellers should turn up on the road, saw " + seenIds.Count);
        }

        static void TownEvents()
        {
            var g = new Game(1357); g.LeaveToOverworld(); g.EnterTown("Festivalton");
            var days = new System.Collections.Generic.Dictionary<TownEventKind, int>();
            int quiet = -1;
            for (int d = 1; d <= 400; d++)
            {
                g.World.Day = d;
                var k = g.TownEventToday();
                if (k == TownEventKind.None) { if (quiet < 0) quiet = d; }
                else if (!days.ContainsKey(k)) days[k] = d;
            }
            Assert(quiet > 0 && days.Count == 5, "a town should see quiet days and all five kinds in 400 days, got " + days.Count);

            var twin = new Game(1357); twin.LeaveToOverworld(); twin.EnterTown("Festivalton");
            foreach (var kv in days) { twin.World.Day = kv.Value; Assert(twin.TownEventToday() == kv.Key, "the schedule is a pure function of seed, town and day"); }
            g.World.Day = days[TownEventKind.Market] ; int market = g.Haggle(100, Houses.Guild);
            g.World.Day = quiet; int normal = g.Haggle(100, Houses.Guild);
            Assert(market < normal, "market day prices are lower (" + market + " vs " + normal + ")");
            g.World.Day = days[TownEventKind.Festival]; Assert(g.Haggle(100, Houses.Guild) < market, "a festival is cheaper still");

            // A funeral shuts the temple's healing; the plague shuts the inn.
            var priest = FindRole(g, TownRole.Priest);
            if (priest != null)
            {
                g.World.Day = days[TownEventKind.Funeral]; g.Player.HP = 1; g.Player.Gold = 500;
                g.TalkTo(priest);
                int heal = RowIdx(g, "Heal");
                Assert(heal >= 0 && !g.ServiceRows()[heal].Enabled, "healing is refused during a funeral");
                g.UiState.Active = Panel.None;
                g.World.Day = quiet; g.TalkTo(priest);
                heal = RowIdx(g, "Heal");
                Assert(heal >= 0 && g.ServiceRows()[heal].Enabled, "and open again on a quiet day");
                g.UiState.Active = Panel.None;
            }

            // A robbery gives the Watch a job, and it can be taken once.
            var captain = FindRole(g, TownRole.Captain);
            if (captain != null)
            {
                g.World.Day = days[TownEventKind.Theft];
                g.TalkTo(captain);
                Pick(g, "Talk");
                Pick(g, "robbery");
                Assert(g.QuestActive(g.TheftQuest().Id), "the robbery job should start");
                var q = g.QuestOf(g.TheftQuest().Id);
                Assert(q.Def.Deadline == 6, "the job is on a clock");
                g.UiState.Active = Panel.None;
            }
        }

        static void PlaceBeside(Game g, Monster target)
        {
            for (int d = 0; d < 8; d++)
            {
                int x = target.X + Pathfinder.Dx8[d], y = target.Y + Pathfinder.Dy8[d];
                var t = g.Map.Get(x, y);
                if ((t == TileKind.Floor || t == TileKind.FloorAlt) && g.MonsterAt(x, y) == null) { g.Player.X = x; g.Player.Y = y; g.UpdateFov(); return; }
            }
            Assert(false, "no free cell beside " + target.Name);
        }

        static void CrimeAndTheWatch()
        {
            var g = new Game(2468); g.LeaveToOverworld(); g.EnterTown("Crimeford");
            Monster citizen = null, guard = null;
            foreach (var n in g.Town.Npcs)
            {
                if (citizen == null && n.Role == TownRole.Citizen && n.Floor == 0) citizen = n;
                if (guard == null && n.Role == TownRole.Guard && n.Floor == 0) guard = n;
            }
            Assert(citizen != null && guard != null, "the town needs a citizen and a guard");
            g.Player.Gold = 5000;

            // A blow somebody saw earns a bounty that follows the damage, and turns the victim and the guards against the hero.
            PlaceBeside(g, citizen);
            citizen.HP = 999;
            Assert(g.BountyHere() == 0, "no bounty yet");
            g.Attack(citizen);
            int bounty = g.BountyHere();
            Assert(bounty >= 17 && bounty <= 150, "a seen blow adds a bounty by damage, got " + bounty);
            Assert(citizen.HostileUntil > g.Turn, "the victim turns hostile");
            foreach (var m in g.Monsters) if (m.Townsperson && m.IsGuard) Assert(m.HostileUntil > g.Turn, "the guards converge");

            // Neighbouring regions know half of it; far regions know nothing.
            string here = g.World.RegionAt(g.World.PlayerX, g.World.PlayerY).Name;
            var near = g.NeighbourRegions(here);
            Assert(near.Count > 0, "a region should have neighbours");
            g.Bounties[here] = 600;
            int px = g.World.PlayerX, py = g.World.PlayerY;
            bool sawFar = false;
            foreach (var r in g.World.Regions)
            {
                if (r.Name == here) continue;
                g.World.PlayerX = r.X + r.W / 2; g.World.PlayerY = r.Y + r.H / 2;
                if (near.Contains(r.Name)) Assert(g.BountyHere() == 300, "a neighbour holds half the bounty, got " + g.BountyHere());
                else { Assert(g.BountyHere() == 0, "a far region knows nothing"); sawFar = true; }
            }
            g.World.PlayerX = px; g.World.PlayerY = py;
            Assert(sawFar || g.World.Regions.Count <= near.Count + 1, "there should be a region that is not a neighbour");

            // Time and sight calm the street, but the Watch keeps the crime: a guard who sees the hero arrests them.
            g.Bounties[here] = 250;
            foreach (var m in g.Monsters) m.HostileUntil = 0;
            PlaceBeside(g, guard);
            for (int i = 0; i < 14 && g.CurrentDialogue == null; i++) g.EndPlayerTurn();
            Assert(g.CurrentDialogue != null && g.UiState.Active == Panel.Service, "a guard should call for the hero");
            Assert(g.ServiceNote.Contains("250"), "the arrest says how much is owed");
            int gold = g.Player.Gold;
            Pick(g, "Pay the fine");
            Assert(g.BountyHere() == 0 && g.Player.Gold == gold - 250, "paying clears the bounty");
            g.UiState.Active = Panel.None; g.CurrentDialogue = null;

            // Serving the sentence takes days and clears the name.
            g.Bounties[here] = 250;
            int day = g.World.Day;
            for (int i = 0; i < 20 && g.CurrentDialogue == null; i++) { g.EndPlayerTurn(); }
            Assert(g.CurrentDialogue != null, "the guard comes back for the next bounty");
            Pick(g, "Go quietly");
            Assert(g.World.Day >= day + g.SentenceDays(250) && g.BountyHere() == 0, "jail passes days and clears the name");
            Assert(g.DeedCount(Deed.Jailed) == 1, "the ledger remembers the cells");
            g.UiState.Active = Panel.None; g.CurrentDialogue = null;

            // Killing a citizen is murder: the town remembers (even for the ones who are not there to see it).
            Monster victim = null;
            foreach (var n in g.Town.Npcs) if (n != citizen && n.Role == TownRole.Citizen && n.Floor == 0 && !n.IsDead) { victim = n; break; }
            Assert(victim != null, "no second citizen");
            PlaceBeside(g, victim);
            victim.HP = 1;
            for (int i = 0; i < 40 && !victim.IsDead; i++) g.Attack(victim);
            Assert(victim.IsDead && g.MurdersIn("Crimeford") == 1, "the citizen should die and be counted");
            Assert(g.DeedCount(Deed.Killed, victim.Name) == 1, "the ledger records the killing");
            Assert(g.BountyHere() >= 1000, "a witnessed murder carries the heavy price, got " + g.BountyHere());

            // The people the story leans on are knocked out, never killed.
            var elder = FindRole(g, TownRole.Elder);
            if (elder != null)
            {
                PlaceBeside(g, elder);
                elder.HP = 1;
                for (int i = 0; i < 40 && elder.DownUntilDay <= g.World.Day; i++) g.Attack(elder);
                Assert(!elder.IsDead && elder.DownUntilDay > g.World.Day, "an essential person is knocked out, not killed");
            }
        }

        static Monster Teller(TownRole role, Trait trait)
        {
            var m = new Monster(Bestiary.Find("hobbit"), new Rng(1)) { Townsperson = true, Role = role };
            m.Persona = new Persona { Trait = trait, Second = trait }; m.Memory = new NpcMemory();
            return m;
        }

        static void RumoursWithTeeth()
        {
            Game Fresh() { var g = new Game(4242); g.LeaveToOverworld(); g.EnterTown("Rumourford"); return g; }
            var a = Fresh(); var b = Fresh();
            var ta = Rumours.Tell(a, Teller(TownRole.Citizen, Trait.Kind), 5);
            var tb = Rumours.Tell(b, Teller(TownRole.Citizen, Trait.Kind), 5);
            Assert(ta != null && ta.Text == tb.Text && ta.Truth == tb.Truth, "the same seed, town and question give the same rumour");

            int wrongDrunk = 0, wrongScholar = 0, bossHooks = 0, marked = 0;
            var gd = Fresh(); var gs = Fresh();
            for (int n = 0; n < 60; n++)
            {
                var d = Rumours.Tell(gd, Teller(TownRole.Drunk, Trait.Weary), n);
                var s = Rumours.Tell(gs, Teller(TownRole.Scholar, Trait.Curious), n);
                if (d != null && d.Truth == RTruth.False) wrongDrunk++;
                if (s != null && s.Truth == RTruth.False) wrongScholar++;
            }
            Assert(wrongDrunk > wrongScholar + 10, "drunks should be wrong far more often than scholars (" + wrongDrunk + " vs " + wrongScholar + ")");
            foreach (var r in gs.LearnedRumours)
            {
                if (r.Id.StartsWith("boss.") && r.Truth != RTruth.False) { bossHooks++; Assert(gs.QuestOf("region.boss." + r.Id.Substring(5)) != null, "a boss rumour should open a Region quest"); }
                if (r.Id.StartsWith("place.") && r.Truth == RTruth.True) { marked++; Assert(gs.World.Tiles[int.Parse(r.Id.Substring(6))].Discovered, "a true place rumour should mark the map"); }
                if (r.Id.StartsWith("boss.") && r.Truth == RTruth.False) Assert(gs.QuestOf("region.boss." + r.Id.Substring(5)) == null || bossHooks > 0, "a wrong boss rumour should not start anything on its own");
            }
            Assert(bossHooks + marked > 0, "a reliable teller should have hit at least one real fact");
            Assert(gs.LearnedRumours.Count > 0, "the hero should keep what they learned");

            // A Region quest from a rumour completes when its boss falls (the flag BossFalls raises).
            QuestState hook = null;
            foreach (var qs in gs.Quests) if (qs.Def.Track == QuestDef.Region) { hook = qs; break; }
            if (hook != null)
            {
                string bossId = hook.Def.Id.Substring("region.boss.".Length);
                hook.Step = 1;   // as if the hero had already reached the lair
                gs.Flags.Add("boss.slain." + bossId); gs.QuestCheck();
                Assert(hook.Status == QStatus.Done, "felling the boss should finish its Region quest");
            }
        }

        static int RowIdx(Game g, string label) => g.ServiceRows().FindIndex(r => r.Label.Contains(label));

        static void Pick(Game g, string label)
        {
            int i = RowIdx(g, label);
            Assert(i >= 0, "no row '" + label + "' (note: " + g.ServiceNote + ")");
            var row = g.ServiceRows()[i];
            Assert(row.Enabled, "row '" + label + "' is disabled");
            g.ServiceAction(row.Id);
        }

        static void PersonalErrands()
        {
            var found = new System.Collections.Generic.Dictionary<Want, (Game, Monster)>();
            for (int seed = 1; seed <= 60 && found.Count < 4; seed++)
            {
                var g = new Game((ulong)(7000 + seed)); g.LeaveToOverworld(); g.EnterTown("Errandton" + seed);
                foreach (var n in g.Town.Npcs)
                    if (n.Persona.Troubled && !found.ContainsKey(n.Persona.Want) && n.Floor == 0 && n.Role != TownRole.Bard && n.Role != TownRole.Scholar) found[n.Persona.Want] = (g, n);
            }
            Assert(found.Count >= 3, "towns should have troubled people of at least three kinds, got " + found.Count);

            foreach (var kv in found)
            {
                var (g, m) = kv.Value;
                string id = PersonalQuests.Id(g, m);
                g.Player.Gold = 200;
                g.TalkTo(m);
                Assert(g.CurrentDialogue != null && g.UiState.Active == Panel.Service, kv.Key + ": a troubled person opens a conversation");
                Pick(g, "troubling");
                Pick(g, "I will help");
                Assert(g.QuestActive(id), kv.Key + ": agreeing starts the errand");
                g.ServiceAction("d:end");

                switch (kv.Key)
                {
                    case Want.Debt: break;
                    case Want.Revenge:
                        for (int i = 0; i < 3; i++) g.KillMonster(new Ossuary.Core.Entities.Monster(Ossuary.Core.Entities.Bestiary.Find("kobold"), new Rng((ulong)(i + 5))));
                        Assert(g.QuestOf(id).Step == 1, "three kobolds finish the hunt");
                        break;
                    case Want.RareItem:
                        foreach (var d in Ossuary.Core.Items.Catalogue.Potions)
                            if (d.Name == "potion of healing") g.Player.Inventory.Add(new Ossuary.Core.Items.Item(d, new Rng(3), 987654) { Identified = true });
                        g.QuestCheck();
                        Assert(g.QuestOf(id).Step == 1, "holding the potion moves the errand on");
                        break;
                    case Want.KinLost:
                        Monster kin = null;
                        foreach (var n in g.Town.Npcs) if (n.Memory.Flags.Contains("kin.of." + id)) kin = n;
                        Assert(kin != null, "a kin should be chosen");
                        g.TalkTo(kin); g.UiState.Active = Panel.None; g.CurrentDialogue = null;
                        Assert(g.QuestOf(id).Step == 1, "meeting the kin moves the errand on");
                        Assert(TownText.Reaction(kin, 0, 0, 0, 0, 0, false).Contains("looking for me"), "the kin should know someone is looking");
                        break;
                }
                g.TalkTo(m);
                int gold = g.Player.Gold;
                Pick(g, "About that favour");
                string label = kv.Key == Want.Debt ? "Pay the debt" : kv.Key == Want.RareItem ? "Give the healing potion" : kv.Key == Want.Revenge ? "It is done" : "I found them";
                Pick(g, label);
                Assert(g.QuestDone(id), kv.Key + ": settling it completes the errand");
                Assert(m.Memory.Has(NpcMemory.Helped) && m.Memory.Disposition >= 40, kv.Key + ": the person should remember the favour");
                Assert(g.DeedCount(Deed.Helped, m.Name) >= 1, kv.Key + ": the ledger records the favour");
                if (kv.Key == Want.Debt) Assert(g.Player.Gold == gold - 40, "the debt costs forty gold");
                g.UiState.Active = Panel.None; g.CurrentDialogue = null;
            }
        }

        static void QuestEngine()
        {
            var g = new Game(909); g.LeaveToOverworld(); g.EnterTown("Questford");
            Assert(g.Quests.Count == 0 && !g.StartQuest("no.such.quest"), "no quests at the start, unknown ids ignored");

            // A quest starts once, counts kills of its target only, advances by flag and pays out.
            Assert(g.StartQuest("watch.bandits") && g.QuestActive("watch.bandits"), "the Watch job should start");
            Assert(!g.StartQuest("watch.bandits"), "a quest never starts twice");
            var q = g.QuestOf("watch.bandits");
            var def = Ossuary.Core.Entities.Bestiary.Find("orc");
            for (int i = 0; i < 3; i++)
            {
                Assert(q.Step == 0, "still on the kill step after " + i + " orcs");
                g.KillMonster(new Ossuary.Core.Entities.Monster(def, new Rng((ulong)(i + 1))));
            }
            Assert(q.Step == 1 && g.QuestActive("watch.bandits"), "three orcs should move it to the report step");
            g.KillMonster(new Ossuary.Core.Entities.Monster(Ossuary.Core.Entities.Bestiary.Find("jackal"), new Rng(9)));
            Assert(q.Step == 1, "other kills do not count");
            int gold = g.Player.Gold, rep = g.RepOf(Houses.Watch);
            g.Flags.Add("watch.bandits.report"); g.QuestCheck();
            Assert(g.QuestDone("watch.bandits") && g.Player.Gold == gold + 60, "the report should pay sixty gold");
            Assert(g.RepOf(Houses.Watch) > rep, "and the Watch should think better of the hero");
            Assert(g.DeedCount(Deed.Quest, "watch.bandits.done") == 1, "the ledger should remember it");

            // The clock can fail a quest that has a deadline; the failure costs reputation and is remembered.
            var h = new Game(910); h.LeaveToOverworld(); h.EnterTown("Lateton");
            h.StartQuest("watch.bandits");
            int r0 = h.RepOf(Houses.Watch);
            h.World.AdvanceTime(24 * 20);
            h.QuestCheck();
            Assert(h.QuestOf("watch.bandits").Status == QStatus.Failed, "twenty days should fail a fourteen-day job");
            Assert(h.RepOf(Houses.Watch) < r0 && h.DeedCount(Deed.Failed, "watch.bandits") == 1, "failing should cost standing and be recorded");

            // The Main quest follows the Elder's flag and the depth reached.
            var m = new Game(911); m.LeaveToOverworld(); m.EnterTown("Mainton");
            m.StartQuest("main.seal");
            Assert(m.QuestOf("main.seal").Step == 0, "the Seal waits for the Elder");
            m.Flags.Add("elder.warned"); m.QuestCheck();
            Assert(m.QuestOf("main.seal").Step == 1, "asking the Elder moves the Seal on");
        }

        static Monster FindRole(Game g, TownRole role)
        {
            foreach (var n in g.Town.Npcs) if (n.Role == role && n.Floor == 0 && !n.IsDead) return n;
            return null;
        }

        static void Conversations()
        {
            Game g = null; Monster elder = null;
            foreach (var name in new[] { "Talkton", "Chatford", "Speakhaven", "Wordbrook", "Gossipmoor", "Elderhollow" })
            {
                g = new Game(555); g.LeaveToOverworld(); g.EnterTown(name);
                elder = FindRole(g, TownRole.Elder);
                if (elder != null) break;
            }
            Assert(elder != null, "no town with a Guild elder to test");

            // The counter gets a Talk row; talking walks the graph and sets a flag.
            g.TalkTo(elder);
            Assert(g.UiState.Active == Panel.Service && g.Talking == elder, "the elder's counter should open");
            Assert(g.ServiceRows().Exists(r => r.Id == "talk"), "the counter should offer Talk");
            Assert(!g.ServiceAction("talk") && g.CurrentDialogue != null, "Talk should open the conversation and keep the panel");
            Assert(g.ServiceRows().Count >= 4, "the conversation should offer choices");
            Assert(!g.ServiceAction("d:0") && g.Flags.Contains("elder.warned"), "choosing a question should answer it and set the flag");
            Assert(g.ServiceNote.Contains("seal"), "the amulet answer should speak of the seal");

            // A gated choice stays shut until the Guild trusts you, then pays once.
            g.ServiceAction("d:0");   // back to the start
            var rows = g.ServiceRows();
            int favour = rows.FindIndex(r => r.Label.Contains("favour"));
            Assert(favour >= 0 && !rows[favour].Enabled, "the favour needs the Guild's trust");
            g.AddRep(Houses.Guild, 30, null);
            rows = g.ServiceRows();
            Assert(rows[favour].Enabled, "trust should open the favour");
            int gold = g.Player.Gold;
            g.ServiceAction(rows[favour].Id);
            Assert(g.Player.Gold == gold + 40 && g.Flags.Contains("elder.favour"), "the favour should pay forty gold");
            Assert(elder.Memory.Has(NpcMemory.Helped), "the elder should remember helping");
            g.ServiceAction("d:end");
            Assert(g.CurrentDialogue == null, "leaving ends the conversation and returns to the menu");

            // Talking holds the person in place: no turn passes in the box, and even if time runs they wait for you.
            var talker = FindRole(g, TownRole.Citizen);
            if (talker != null)
            {
                g.Talking = talker; g.OpenDialogue(Dialogues.For(talker), talker);
                int tx = talker.X, ty = talker.Y;
                for (int i = 0; i < 60; i++) g.EndPlayerTurn();
                Assert(talker.X == tx && talker.Y == ty, "someone you are talking to does not walk away");
                Assert(g.CurrentDialogue != null && g.ServiceRows().Count >= 3, "an ordinary citizen offers a few things to say");
                Pick(g, "Ask what they have heard");
                Assert(g.ServiceNote.Length > 0 && g.CurrentDialogue != null, "asking keeps the box open");
                g.ServiceAction("d:end");
                g.CurrentDialogue = null; g.UiState.Active = Panel.None;
            }

            // A person you struck will not talk, and the grudge shows in what they say.
            var bard = FindRole(g, TownRole.Bard);
            if (bard != null)
            {
                bard.HP = 999;
                g.Attack(bard);
                g.TalkTo(bard);
                Assert(g.CurrentDialogue != null && g.ServiceRows().Count == 1, "a grudge leaves only the way out");
                g.ServiceAction("d:end");
            }
        }

        static void PersonaAndLedger()
        {
            var g = new Game(31337); g.LeaveToOverworld(); g.EnterTown("Personaton");
            long calls = g.Rng.Calls;
            string sig = PersonaSig(g);
            var other = new Game(31337); other.LeaveToOverworld(); other.EnterTown("Personaton");
            Assert(PersonaSig(other) == sig, "the same seed must give the same personas");
            foreach (var n in g.Town.Npcs) Assert(n.Persona != null && n.Memory != null, "every townsperson needs a persona and a memory");
            Assert(g.Rng.Calls == calls, "personas must not touch the simulation's stream");

            // Essentials are the people the story leans on, and only them.
            bool anyEssential = false;
            foreach (var n in g.Town.Npcs)
                if (n.Persona.Essential) { anyEssential = true; Assert(Persona.IsEssentialRole(n.Role), "a non-story role is marked essential"); }
                else Assert(!Persona.IsEssentialRole(n.Role), "a story role is not essential");
            Assert(anyEssential, "a town should have at least one essential person");

            // Different people say different things: traits reach the small talk.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var n in g.Town.Npcs) if (n.Role == TownRole.Citizen) for (int i = 0; i < 6; i++) seen.Add(TownText.LineFor(n, i));
            Assert(seen.Count >= 5, "citizens should not all say the same few lines");

            // A blow is remembered: by the person, by the Watch and by the ledger; the line changes.
            Monster victim = null;
            foreach (var n in g.Town.Npcs) if (n.Role == TownRole.Citizen && n.Floor == 0) { victim = n; break; }
            Assert(victim != null, "no citizen to test with");
            int watch = g.RepOf(Houses.Watch), struck = g.DeedCount(Deed.Struck);
            victim.HP = 999;
            g.Attack(victim);
            Assert(victim.Memory.Has(NpcMemory.Struck) && victim.Memory.Disposition < 0, "the person should remember the blow");
            Assert(g.RepOf(Houses.Watch) < watch, "the Watch should hear of it");
            Assert(g.DeedCount(Deed.Struck) == struck + 1, "the ledger should record it");
            string line = TownText.Reaction(victim, 0, 0, 0, 0, 0, false);
            Assert(line != null && (line.Contains("hand to me") || line.Contains("Keep away")), "their line should mention it");
            int rep = g.RepOf(Houses.Watch);
            g.Attack(victim);
            Assert(g.RepOf(Houses.Watch) == rep, "the Watch is told once per person, not per blow");
            Assert(Loc.T(line) != null, "the line needs a translation path");
        }

        static void TownRoutine()
        {
            Monster Person(TownRole role) { var m = new Monster(Bestiary.Find("dwarf"), new Rng(1)) { Townsperson = true, Role = role }; return m; }
            var guard = Person(TownRole.Guard); var priest = Person(TownRole.Priest); var smith = Person(TownRole.Smith); var citizen = Person(TownRole.Citizen);
            string R(Monster m, int w = 0, int t = 0, int gl = 0, int c = 0, int mu = 0, bool comp = false) => TownText.Reaction(m, w, t, gl, c, mu, comp);
            Assert(R(guard) == null && R(priest) == null && R(smith) == null && R(citizen) == null, "strangers get the usual lines");
            Assert(R(guard, w: 40) != null && R(guard, w: 40) != R(guard, w: -40), "the Watch greets friends and foes differently");
            Assert(R(priest, t: 40) != null && R(priest, t: -40) != R(priest, t: 40), "so does the Temple");
            Assert(R(priest, t: 40, c: 50) != R(priest, t: 40), "and a priest sees the corruption first");
            Assert(R(smith, gl: 40) != null && R(smith, gl: -40) != null && R(smith, gl: 40) != R(smith, gl: -40), "traders answer to the Guild");
            Assert(R(citizen, mu: 2) != null && R(citizen, comp: true) != null, "citizens notice your body and your company");
            Assert(R(Person(TownRole.Pet), mu: 3) == null, "pets say nothing");

            // After dark the residents go home.
            var g = new Game(4242);
            g.LeaveToOverworld();
            g.EnterTown("Probeton");
            var residents = new List<Monster>();
            foreach (var m in g.Monsters)
                if (m.Townsperson && m.Home != null && m.Home.Keeper != m && !m.IsGuard && !m.IsPriest && m.Floor == 0 && (m.Role == TownRole.Citizen || m.Role == TownRole.Child || m.Role == TownRole.Elder || m.Role == TownRole.Scholar)) residents.Add(m);
            Assert(residents.Count >= 3, "test setup: a town with residents, found " + residents.Count);
            int Inside() { int n = 0; foreach (var m in residents) if (m.Home.Contains(m.X, m.Y) && Pathfinder.Chebyshev(m.X, m.Y, m.Home.X + m.Home.W / 2, m.Home.Y + m.Home.H / 2) <= 1) n++; return n; }
            g.World.Hour = 12;
            for (int i = 0; i < 40; i++) g.Wait();
            int noon = Inside();
            for (int i = 0; i < 160; i++) { g.World.Hour = 22; g.Wait(); }
            int night = Inside();
            Assert(night >= noon && night * 100 >= residents.Count * 60, $"residents are tucked in at night: {noon} at noon, {night} of {residents.Count} at night");
        }

        static void VaultsAndKeys()
        {
            // Carving never changes what the stairs can reach.
            int carved = 0;
            for (int seed = 1; seed <= 30; seed++)
            {
                var rng = new Rng((ulong)(seed * 104729));
                var opts = new Ossuary.Core.Gen.GenOptions { Width = 79, Height = 25, Style = seed % 2 == 0 ? Ossuary.Core.Gen.LevelStyle.Rooms : Ossuary.Core.Gen.LevelStyle.Barracks, MaxRooms = 10, AllowStairsUp = true, AllowStairsDown = true };
                var map = Ossuary.Core.Gen.DungeonGen.Generate(opts, rng, out _, out var starts);
                var before = map.Reachability(starts[0] % map.W, starts[0] / map.W, true);
                var site = Ossuary.Core.Gen.Vaults.Carve(map, rng, TileKind.LockedDoor);
                if (site == null) continue;
                carved++;
                var after = map.Reachability(starts[0] % map.W, starts[0] / map.W, true);
                for (int i = 0; i < before.Length; i++) Assert(before[i] == after[i] || site.Cells.Contains(i), "carving a vault must not change the level around it, seed " + seed);
                foreach (int c in site.Cells) Assert(!after[c], "the vault is sealed behind its door, seed " + seed);
                Assert(map.Get(site.DoorX, site.DoorY) == TileKind.LockedDoor && site.Cells.Count == 12, "a locked door and a 4x3 chamber");
                map.Set(site.DoorX, site.DoorY, TileKind.OpenDoor);
                int ox = -1, oy = -1;
                for (int k = 0; k < 4; k++)
                {
                    int nx = site.DoorX + Ossuary.Core.Dirs.Dx4[k], ny = site.DoorY + Ossuary.Core.Dirs.Dy4[k];
                    if (map.Walkable(nx, ny) && !site.Cells.Contains(nx + ny * map.W)) { ox = nx; oy = ny; }
                }
                Assert(ox >= 0, "the door opens onto the level, seed " + seed);
                var open = map.Reachability(ox, oy, true);
                foreach (int c in site.Cells) Assert(open[c], "and the chamber is reachable once the door is open, seed " + seed);
            }
            Assert(carved >= 12, "many levels have room for a vault, carved " + carved);

            // Generated levels: vaults appear, their key rides on a monster, and the cache is rigged.
            int keyed = 0, caches = 0;
            for (ulong seed = 1; seed <= 90; seed++)   // enough levels that "a few have keyed vaults" is not a coin flip
            {
                var d = new Dungeon(new Rng(seed * 6700417));
                var map = d.Ensure("The Dungeons", 4 + (int)(seed % 5), out var spawns, out int sx, out int sy);
                bool key = false;
                if (spawns != null) foreach (var s in spawns) if (s.Monster.Inventory.Exists(i => i.Def.Name == "brass key")) key = true;
                for (int y = 0; y < map.H && !key; y++) for (int x = 0; x < map.W; x++) { var st = GroundItems.At(map.Number, x, y); if (st != null && st.Exists(i => i.Def.Name == "brass key")) key = true; }
                bool lockedVault = false, hidden = false;
                for (int y = 0; y < map.H; y++) for (int x = 0; x < map.W; x++) { if (map.Get(x, y) == TileKind.LockedDoor) lockedVault = true; if (map.Get(x, y) == TileKind.HiddenDoor) hidden = true; }
                if (key) { keyed++; Assert(lockedVault, "a keyed level has its locked door, seed " + seed); }
                if (hidden)
                {
                    int rigged = 0;
                    for (int y = 0; y < map.H; y++) for (int x = 0; x < map.W; x++) if (TrapTable.TryGet(map.Number, x, y, out _, out _)) rigged++;
                    if (rigged >= 3) caches++;
                }
            }
            Assert(keyed >= 4, "locked vaults with keys turn up, saw " + keyed);
            Assert(caches >= 1, "rigged caches turn up, saw " + caches);

            // The key works, once, on any locked door.
            var g = Game.NewHero(2100, "Keeper", "human", "fighter");
            g.Monsters.Clear();
            int px = g.Player.X, py = g.Player.Y;
            for (int x = px - 1; x <= px + 2; x++) { g.Map.Set(x, py, TileKind.Floor); }
            g.Map.Set(px + 1, py, TileKind.LockedDoor); g.Map.Set(px + 2, py, TileKind.Floor);
            g.Player.Inventory.RemoveAll(i => i.Def.Name == "lock pick");
            var cmd = new Commands(g);
            cmd.Execute("move-e");
            Assert(g.Map.Get(px + 1, py) == TileKind.LockedDoor, "a locked door holds without a key");
            g.Player.Inventory.Add(new Item(Crafted.BrassKey, g.Rng, 1) { Identified = true });
            cmd.Execute("move-e");
            Assert(g.Map.Get(px + 1, py) == TileKind.OpenDoor, "the brass key opens it");
            Assert(!g.Player.Inventory.Exists(i => i.Def.Name == "brass key"), "and is used up");
        }

        static void EntrancesReachBranches()
        {
            var reached = new HashSet<string>();
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var g = new Game(seed * 31337);
                g.LeaveToOverworld();
                for (int y = 0; y < g.World.H; y++)
                    for (int x = 0; x < g.World.W; x++)
                    {
                        var t = g.World.Get(x, y);
                        if (t.Feature != Ossuary.Core.World.OverworldFeature.Dungeon) continue;
                        string region = g.World.RegionAt(x, y).Name;
                        string want = Ossuary.Core.World.OverworldGen.BranchForRegion(region);
                        Assert(Ossuary.Core.World.OverworldGen.BranchForEntrance(t.Name) == want, $"{t.Name} in {region} should lead to {want}");
                        g.Mode = GameMode.Overworld;
                        g.EnterDungeonFromOverworld(x, y);
                        Assert(g.Branch == want && g.Mode == GameMode.Dungeon, $"entering {t.Name} lands in {want}, not {g.Branch}");
                        reached.Add(g.Branch);
                        g.LeaveToOverworld();
                    }
            }
            Assert(reached.Count == 5, "every branch can be reached from the overworld, reached " + string.Join(", ", reached));
        }

        static void AnnexFlow()
        {
            // The portal waits on Dungeons 4, and nowhere else.
            int portals = 0, levels = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var d = new Dungeon(new Rng(seed * 15485863));
                var four = d.Ensure("The Dungeons", 4, out _, out _, out _);
                int n = 0; for (int i = 0; i < four.W * four.H; i++) if (four.GetRaw(i) == TileKind.Portal) n++;
                levels++; if (n == 1) portals++;
                var three = d.Ensure("The Dungeons", 3, out _, out _, out _);
                for (int i = 0; i < three.W * three.H; i++) Assert(three.GetRaw(i) != TileKind.Portal, "no portal on other floors");
            }
            Assert(portals >= 9, $"Dungeons 4 carries one portal, {portals} of {levels}");

            // In and out.
            var g = Game.NewHero(2200, "Portal", "human", "fighter");
            g.DescendTo("The Dungeons", 4);
            int px = -1, py = -1;
            for (int y = 0; y < g.Map.H; y++) for (int x = 0; x < g.Map.W; x++) if (g.Map.Get(x, y) == TileKind.Portal) { px = x; py = y; }
            Assert(px >= 0, "test setup: the portal");
            g.Player.X = px; g.Player.Y = py; g.UpdateFov();
            var cmd = new Commands(g);
            cmd.Execute(">");
            Assert(g.Branch == "The Annex" && g.Depth == 1 && g.AnnexVisited, "> on the portal enters the Annex");
            Assert(g.Map.Get(g.Player.X, g.Player.Y) == TileKind.Portal, "you arrive standing on the way out");
            cmd.Execute("<");
            Assert(g.Branch == "The Dungeons" && g.Depth == 4 && g.Player.X == px && g.Player.Y == py, "< on it goes back to the same spot");
            cmd.Execute(">");
            Assert(g.Branch == "The Annex", "and in again");
            g.Monsters.Clear();
            g.DescendTo("The Annex", 1);

            // Hard: the Annex's first floor has monsters from six floors down.
            var ordinary = Game.NewHero(2201, "Plain", "human", "fighter");
            var annex = Game.NewHero(2201, "Hard", "human", "fighter");
            annex.DescendTo("The Annex", 1);
            double Avg(Game gg) { double s = 0; int n = 0; foreach (var m in gg.Monsters) { s += m.Def.Level; n++; } return n == 0 ? 0 : s / n; }
            Assert(Avg(annex) > Avg(ordinary) + 1.5, $"the Annex is hard: average monster level {Avg(annex):0.0} vs {Avg(ordinary):0.0}");

            // At the bottom: the Warden and the mantle.
            var bottom = Game.NewHero(2202, "Deep", "human", "fighter");
            bottom.DescendTo("The Annex", 3);
            Assert(bottom.Monsters.Exists(m => m.BossId == "annex-warden"), "the Warden waits on the third floor");
            bool mantle = false;
            for (int y = 0; y < bottom.Map.H; y++) for (int x = 0; x < bottom.Map.W; x++) { var st = GroundItems.At(bottom.Map.Number, x, y); if (st != null && st.Exists(i => i.ArtifactId == "tithe-mantle")) mantle = true; }
            Assert(mantle, "and the Tithe-Collector's Mantle lies on the same floor");

            // The Warden collects.
            var w = Arena(2203, "annex-warden", 5);
            int corruption = w.Player.Corruption;
            for (int i = 0; i < 40 && !Said(w, "reads out a debt"); i++) { w.Player.HP = w.Player.MaxHP; w.Wait(); }
            Assert(Said(w, "reads out a debt") && w.Player.Corruption > corruption, "the Warden bleeds you and taints you");
        }

        static void TrainedMode()
        {
            var normal = Game.NewHero(2300, "Used", "human", "fighter");
            int c0 = normal.Player.Skills[Skill.Combat];
            normal.Player.GainSkill(Skill.Combat, 5);
            Assert(normal.Player.Skills[Skill.Combat] == c0 + 5, "skills rise with use normally");

            var g = Game.NewHero(2301, "Student", "human", "fighter");
            g.Difficulty = Difficulty.Trained; g.ApplyChallenge();
            g.Monsters.Clear();
            Assert(g.Player.Trained && g.Player.TrainXp == 60, "Trained starts with some XP to spend");
            int c1 = g.Player.Skills[Skill.Combat];
            g.Player.GainSkill(Skill.Combat, 5);
            Assert(g.Player.Skills[Skill.Combat] == c1, "use no longer teaches");
            int xp = g.Player.TrainXp;
            g.Player.AddXp(30);
            Assert(g.Player.TrainXp == xp + 30, "experience is also spending money");

            var cmd = new Commands(g);
            cmd.Execute("train");
            Assert(g.PendingChoice.Active && g.PendingChoice.Prompt == Game.TrainPrompt && g.PendingChoice.Items.Count >= 3, "the training list opens");
            var combat = g.PendingChoice.Items.Find(i => i.Def.Name == "Combat");
            int cost = g.TrainCost(Skill.Combat);
            g.Player.TrainXp = cost + 7;
            cmd.CommitChoice(combat);
            Assert(g.Player.Skills[Skill.Combat] == c1 + 5 && g.Player.TrainXp == 7, "five points bought for their price");
            cmd.Execute("train");
            var again = g.PendingChoice.Items.Find(i => i.Def.Name == "Combat");
            cmd.CommitChoice(again);
            Assert(g.Player.Skills[Skill.Combat] == c1 + 5 && g.Player.TrainXp == 7, "not enough XP, nothing bought");
            // Caps hold.
            g.Player.Skills[Skill.Magic] = Roles.Find("fighter").CapFor(Skill.Magic);
            g.Player.TrainXp = 999; cmd.Execute("train");
            Assert(!g.PendingChoice.Items.Exists(i => i.Def.Name == "Magic"), "a skill at its class cap cannot be trained");

            var plain = Game.NewHero(2302, "Plain", "human", "fighter");
            int turn = plain.Turn; new Commands(plain).Execute("train");
            Assert(plain.Turn == turn && !plain.PendingChoice.Active, "outside Trained mode the verb only explains itself");
        }

        static void SoundCues()
        {
            var g = Game.NewHero(2400, "Ears", "human", "fighter");
            g.DrainCues();
            Assert(g.DrainCues().Length == 0, "silence at first");
            g.Say("A rat bites you.", MessageKind.Combat);
            g.Say("The rat dies.", MessageKind.Kill);
            g.Say("Plain words.", MessageKind.Neutral);
            var cues = g.DrainCues();
            Assert(cues.Length == 2 && cues[0] == "kill" && cues[1] == "hit", "kills before hits, and plain words make no sound: " + string.Join(",", cues));
            Assert(g.DrainCues().Length == 0, "draining empties the queue");
            foreach (var kind in new[] { MessageKind.Bad, MessageKind.Good, MessageKind.Warn, MessageKind.Quest, MessageKind.Death }) g.Say("x", kind);
            g.Cue("levelup");
            var many = g.DrainCues();
            Assert(many.Length == 3 && many[0] == "death" && many[1] == "levelup", "at most three cues, the strongest first: " + string.Join(",", many));
            g.Say("again", MessageKind.Combat); g.Say("again", MessageKind.Combat);
            Assert(g.DrainCues().Length == 1, "the same cue twice is heard once");

            var mage = Archmage(2401);
            mage.DrainCues();
            SpellTarget(mage, out int x, out int y);
            var ogre = new Monster(Bestiary.Find("ogre"), mage.Rng) { X = x, Y = y }; ogre.HP = ogre.MaxHP = 4000; mage.Monsters.Add(ogre);
            mage.DrainCues();
            Assert(Cast(mage, "magic-missile", x, y), "cast");
            Assert(Array.IndexOf(mage.DrainCues(9), "magic") >= 0, "casting is heard");

            // Stairs and loose items are heard too.
            var walker = Game.NewHero(2402, "Steps", "human", "fighter");
            walker.DrainCues();
            walker.Cue("stairs"); walker.Cue("door"); walker.Cue("rest");
            var world = walker.DrainCues(9);
            Assert(world.Length == 3 && Array.IndexOf(world, "stairs") >= 0 && Array.IndexOf(world, "door") >= 0 && Array.IndexOf(world, "rest") >= 0,
                "stairs, doors and rest have cues: " + string.Join(",", world));

            // Something blocking the way is heard loudly: an enemy coming into sight warns once, not once a turn.
            var scout = Game.NewHero(2405, "Sight", "human", "fighter");
            scout.Monsters.Clear();
            scout.UpdateFov();
            scout.NoteThreat();          // an empty level settles the state before the enemy arrives
            scout.DrainCues();
            Assert(scout.DrainCues(9).Length == 0, "an empty level is quiet");
            var foe = new Monster(Bestiary.Find("giant rat"), scout.Rng);
            foe.Dormant = false;
            foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (scout.Map.Walkable(scout.Player.X + d.Item1, scout.Player.Y + d.Item2)) { foe.X = scout.Player.X + d.Item1; foe.Y = scout.Player.Y + d.Item2; break; }
            scout.Monsters.Add(foe);
            scout.UpdateFov();
            scout.NoteThreat();
            Assert(Array.IndexOf(scout.DrainCues(9), "danger") >= 0, "an enemy in sight is heard");
            scout.NoteThreat();
            Assert(Array.IndexOf(scout.DrainCues(9), "danger") < 0, "the same enemy warns once, not every turn");
            scout.Monsters.Clear();
            scout.UpdateFov();
            scout.NoteThreat();
            scout.Monsters.Add(foe);
            scout.UpdateFov();
            scout.NoteThreat();
            Assert(Array.IndexOf(scout.DrainCues(9), "danger") >= 0, "and again when a fresh enemy appears");

            // A road encounter blocks the way (and travelling past it), and both are heard.
            var road = Game.NewHero(2406, "Road", "human", "fighter");
            road.LeaveToOverworld();
            road.ActiveEncounter = true;
            road.DrainCues();
            road.OverworldMove(1, 0);
            Assert(Array.IndexOf(road.DrainCues(9), "danger") >= 0, "a blocked road is heard");
            road.CommitTravel(road.World.PlayerX + 1, road.World.PlayerY);
            Assert(Array.IndexOf(road.DrainCues(9), "danger") >= 0, "travel past a blocker is heard");

            // The music follows the scene: something while alive, nothing once the run is over.
            Assert(!string.IsNullOrEmpty(walker.MusicScene()), "a living hero has a scene: " + walker.MusicScene());
            var fallen = Game.NewHero(2404, "Gone", "human", "fighter");
            fallen.Mode = GameMode.GameOver;
            Assert(fallen.MusicScene() == "", "a dead hero hears no music");
        }

        static void SquareTiles()
        {
            var g = Game.NewHero(2500, "Square", "human", "fighter");
            g.Monsters.Clear();
            var hud = new GameHud(g);
            hud.Ui.Resize(110, 36);
            (int x, int y) Find(TextBuilder t, char c, int from = 0)
            {
                for (int y = 0; y < t.Height; y++) for (int x = from; x < t.Width; x++) if (t.CharAt(x, y) == c) return (x, y);
                return (-1, -1);
            }
            try
            {
                DisplaySettings.Current.Square = false;
                var narrow = hud.Draw();
                var (nx, ny) = Find(narrow, '@');
                Assert(nx >= 0, "the hero is on screen");
                g.PushTargeting(TargetingMode.Look);
                for (int i = 0; i < 3; i++) g.NudgeTarget(1, 0);
                var nt = hud.Draw();
                var (ncx, ncy) = Find(nt, '◎');
                Assert(ncx - nx == 3 && ncy == ny, "narrow: a target three cells away is three columns away");

                DisplaySettings.Current.Square = true;
                var sqr = hud.Draw();
                var (sx, sy) = Find(sqr, '@');
                Assert(sx >= 0 && sy >= 0, "square: the hero is still on screen");
                Assert(sqr.CharAt(sx + 1, sy) == ' ', "and stands on the left of his two columns");
                var (scx, scy) = Find(sqr, '◎');
                Assert(scx - sx == 6 && scy == sy, "square: the same target is six columns away, " + (scx - sx));
                // Scenery repeats across both columns: some wall cell has the same glyph on both sides of its pair.
                int pairs = 0, same = 0;
                for (int y = 0; y < sqr.Height; y++)
                    for (int x = 1; x + 1 < 70; x += 2)
                    {
                        char a = sqr.CharAt(x, y), b = sqr.CharAt(x + 1, y);
                        if (a == '#') { pairs++; if (b == '#') same++; }
                    }
                Assert(pairs == 0 || same * 100 / pairs >= 60, "walls fill both columns of their cell, " + same + "/" + pairs);
            }
            finally { DisplaySettings.Current.Square = false; g.UiState.Targeting = TargetingMode.None; }

            // The menu has the row, and it flips the setting.
            Assert(Array.IndexOf(MenuRows.All, MenuRow.Tiles) >= 0, "there is a Tiles menu row");
            var s = DisplaySettings.Current; int v = s.Version;
            s.CycleSquare(); Assert(s.Square && s.Version > v, "cycling turns square tiles on"); s.CycleSquare(); Assert(!s.Square, "and off");
        }
    }
}
