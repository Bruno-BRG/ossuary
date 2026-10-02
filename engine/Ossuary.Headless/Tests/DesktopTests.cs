using System;
using Ossuary.Core;
using Ossuary.Core.Entities;

namespace Ossuary.Desktop
{
    public static class DesktopTests
    {
        static void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("Desktop test failed: " + label);
        }
        static void Menus()
        {
            var s = new Session(); s.New(31337); s.Resize(110, 36); s.Draw();
            // Options are reachable from the title, before any run exists, and that is not "started".
            s.SetTitle(true); s.Key("Escape"); var title = s.Draw();
            Check(s.Game.UiState.Active == Panel.Settings && !s.Started, "the menu opens over the title without starting a run");
            Check(title.Glyphs[0] == 32 && title.Bg[0] == title.Void, "the title frame leaves the backdrop empty for the title scene");
            s.Key("Escape"); s.Draw();
            Check(!s.Game.UiState.IsOpen, "Escape closes the menu at the title");
            s.SetTitle(false);
            s.Key("Escape"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Settings, "Escape opens the pause menu");
            s.Key("Escape"); s.Draw();
            Check(s.Game.UiState.Active == Panel.None, "Escape closes the pause menu");
            // Volume rows clamp and persist as data.
            s.Key("F2"); s.Draw();
            for (int i = 0; i < 6; i++) s.Key("ArrowDown");
            int master = AudioSettings.Current.Master;
            for (int i = 0; i < 4; i++) s.Key("ArrowRight");
            Check(AudioSettings.Current.Master == Math.Min(AudioSettings.Max, master + 4), "volume changes and clamps");
            for (int i = 0; i < 20; i++) s.Key("ArrowLeft");
            Check(AudioSettings.Current.Master == 0, "volume floors at zero");
            AudioSettings.Current.Load("8,6,8");
            s.Key("Escape"); s.Key("Period", "."); s.Draw();   // a real turn, so there is a run to save
            s.Key("F2"); s.Draw();
            for (int i = 0; i < 10; i++) s.Key("ArrowDown");
            s.Key("ArrowUp"); s.Key("ArrowUp"); s.Key("ArrowUp"); s.Key("ArrowUp");
            // Master -> Music -> Effects -> Controls -> Achievements -> Past runs -> Main menu
            for (int i = 0; i < 6; i++) s.Key("ArrowDown");
            s.Draw();
            s.Key("Enter"); var f = s.Draw();
            Check(f.ToTitle && s.Game != null, "main menu returns to the title");
            Check(s.HasSave, "main menu writes the save");
            s.Key("KeyI"); f = s.Draw();
            Check(!f.ToTitle, "title flag lasts one frame");
            SaveStore.DeleteSave();
        }

        static void Bindings()
        {
            var b = KeyBindings.Current; b.ResetAll();
            int inv = Array.FindIndex(KeyBindings.Actions, a => a.Id == "inventory");
            string c = "KeyB", k = "b"; bool sh = false, ct = false;
            b.Resolve(ref c, ref k, ref sh, ref ct);
            Check(c == "Numpad1", "KeyB starts as south-west");
            // '?' on a layout where it is not on Slash (ABNT2 puts it on IntlRo, others behind AltGr).
            foreach (var layout in new[] { new[] { "IntlRo", "?", "1" }, new[] { "KeyW", "?", "0" }, new[] { "Slash", "?", "1" }, new[] { "NumpadDivide", "/", "0" } })
            {
                string hc = layout[0], hk = layout[1]; bool hs = layout[2] == "1", hctl = false;
                b.Resolve(ref hc, ref hk, ref hs, ref hctl);
                Check(hc == "Slash" && !hs, "'" + layout[1] + "' on " + layout[0] + " opens help");
            }
            b.Rebind(inv, "KeyB", false, false, out bool ok);
            Check(ok, "rebind accepted");
            c = "KeyB"; k = "b"; sh = false; ct = false; b.Resolve(ref c, ref k, ref sh, ref ct);
            Check(c == "KeyI", "KeyB now opens the inventory");
            c = "KeyI"; k = "i"; sh = false; ct = false; b.Resolve(ref c, ref k, ref sh, ref ct);
            Check(c == "None", "the old key is released, not kept as an alias");
            int sw = Array.FindIndex(KeyBindings.Actions, a => a.Id == "move-sw");
            Check(b.KeysOf(sw).Contains("Numpad1") && !b.KeysOf(sw).Contains("KeyB"), "the displaced action keeps its other keys");
            b.Rebind(inv, "ArrowUp", false, false, out ok);
            Check(!ok, "arrows are reserved");
            b.Rebind(inv, "Escape", false, false, out ok);
            Check(!ok, "escape is reserved");
            string saved = b.Serialize();
            var other = new KeyBindings(); other.Load(saved);
            Check(other.Serialize() == saved && other.KeysLabel(inv) == b.KeysLabel(inv), "bindings round-trip through text");
            // Through the session: the rebound key opens the inventory and moves nothing.
            var s = new Session(); s.New(31337); s.Resize(110, 36); s.Draw();
            int x = s.Game.Player.X, y = s.Game.Player.Y;
            s.Key("KeyB", "b"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Inventory && s.Game.Player.X == x && s.Game.Player.Y == y, "rebound key does its new job");
            s.Key("Escape"); s.Draw();
            b.ResetAll();
            // Rebinding through the Controls panel itself.
            s.Key("F2"); s.Draw();
            for (int i = 0; i < 9; i++) s.Key("ArrowDown");
            s.Key("Enter"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Controls, "menu opens the controls panel");
            s.Key("Enter"); s.Draw();
            Check(s.Game.UiState.Rebinding, "enter waits for a key");
            s.Key("KeyZ"); s.Draw();
            Check(!s.Game.UiState.Rebinding && b.KeysOf(0).Contains("KeyZ"), "the pressed key is captured");
            s.Key("KeyR"); s.Draw();
            Check(b.KeysOf(0).Contains("KeyK") && !b.KeysOf(0).Contains("KeyZ"), "R resets the row");
            b.ResetAll();
        }

        static void LanguageAndOpening()
        {
            try
            {
                DisplaySettings.Current.SetLanguage(Lang.Pt);
                Check(Loc.T("You break away and run.") == "Você se solta e corre.", "static messages translate");
                Check(Loc.T("You buy a dagger for 12 gold.") == "Você compra a dagger por 12 de ouro.", "dynamic messages translate by pattern");
                Check(Loc.T("some string nobody translated") == "some string nobody translated", "untranslated text falls back to English");
                var pt = Story.Intro(); DisplaySettings.Current.SetLanguage(Lang.En); var en = Story.Intro();
                Check(pt.Length == en.Length && pt.Length >= 4, "the intro has the same pages in both languages");
                Check(pt[0][0] != en[0][0], "the intro differs by language");

                // Language is a menu row and takes effect immediately.
                var s = new Session(); s.New(31337); s.Resize(110, 36); s.Draw(); s.SetTitle(true); s.Key("Escape"); s.Draw();
                for (int i = 0; i < Array.IndexOf(MenuRows.All, MenuRow.Language); i++) s.Key("ArrowDown");
                s.Key("ArrowRight"); var f = s.Draw();
                Check(DisplaySettings.Current.Language == Lang.Pt && f.Lang == "pt", "the menu switches to Portuguese");
                s.Key("ArrowRight"); f = s.Draw();
                Check(DisplaySettings.Current.Language == Lang.En && f.Lang == "en", "and back to English");
                s.Key("Escape");

                // A created hero plays the intro, then starts on the overworld beside a dungeon mouth.
                var c = new Session(); c.New(2024, true); c.Resize(110, 36); c.Draw();
                foreach (var key in new[] { "Enter", "Enter", "Enter", "Enter" }) c.Key(key);
                var frame = c.Draw();
                Check(frame.Intro != null && frame.Intro.Length >= 4, "creation is followed by the intro");
                Check(c.Game.Mode == GameMode.Overworld && c.Game.Map == null, "the run begins on the overworld");
                bool nearDungeon = false;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    nearDungeon |= c.Game.World.Get(c.Game.World.PlayerX + dx, c.Game.World.PlayerY + dy).Feature == Ossuary.Core.World.OverworldFeature.Dungeon;
                Check(nearDungeon, "the hero starts one step from a dungeon entrance");
                c.Key("Enter"); frame = c.Draw();
                Check(frame.Intro == null && c.Started, "a key ends the intro and starts the run");
                SaveStore.DeleteSave();
            }
            finally { DisplaySettings.Current.SetLanguage(Lang.En); }
        }

        static void SaveAndLoad()
        {
            var s = new Session(); s.New(4242); s.Resize(110, 36); s.Draw();
            foreach (var code in new[] { "ArrowRight", "ArrowDown", "Period", "KeyS", "ArrowLeft", "KeyI", "Escape", "ArrowUp", "Period" })
            { s.Key(code, ""); s.Draw(); }
            Check(s.Save() == null && s.HasSave, "save writes");
            int turn = s.Game.Turn, x = s.Game.Player.X, y = s.Game.Player.Y, hp = s.Game.Player.HP;
            var loaded = new Session(); loaded.New(); loaded.Resize(110, 36); loaded.Load(); loaded.Draw();
            Check(loaded.Game.Turn == turn && loaded.Game.Player.X == x && loaded.Game.Player.Y == y && loaded.Game.Player.HP == hp, "load rebuilds the run exactly");
            Check(loaded.Started, "a loaded run counts as started");
            // The menu is excluded from the log, so a replay never reopens it or touches settings.
            s.Key("F2"); s.Draw(); s.Key("Escape"); s.Draw();
            s.Save();
            var again = new Session(); again.New(); again.Resize(110, 36); again.Load();
            Check(again.Game.UiState.Active == Panel.None && again.Game.Turn == turn, "menu keys are not replayed");
            // Dying ends the run and its save; another run's death leaves it alone.
            var doomed = new Session(); doomed.New(7); doomed.Resize(110, 36); doomed.Key("Period", "."); doomed.Game.Mode = GameMode.GameOver; doomed.Key("Enter");
            Check(new Session().HasSave, "a different run's death keeps the save");
            s.Game.Mode = GameMode.GameOver; s.Key("Enter");
            Check(!new Session().HasSave, "death deletes the save of that run");
        }

        static void Creation()
        {
            var s = new Session(); s.New(2024, true); s.Resize(110, 36); s.Draw();
            var ui = s.Game.UiState;
            Check(ui.Active == Panel.Create, "a new expedition opens creation");
            int turn = s.Game.Turn;
            s.Key("KeyM", "M"); s.Key("KeyA", "a"); s.Key("Backspace"); s.Key("KeyB", "b"); s.Key("KeyL", "l"); s.Key("Digit1", "é");
            Check(ui.Create.Name == "Mb" + "l", "typing builds the name, backspace edits, non-ASCII is refused");
            s.Key("Enter");
            Check(ui.Create.Step == CreateStep.Race, "Enter leaves the name");
            s.Key("ArrowDown"); s.Key("ArrowDown");
            Check(ui.Create.RaceIndex == 2, "race list moves");
            s.Key("Escape"); Check(ui.Create.Step == CreateStep.Name, "Escape steps back");
            s.Key("Enter"); s.Key("Enter");
            s.Key("ArrowUp");
            Check(ui.Create.RoleIndex == Roles.All.Length - 1, "role list wraps");
            s.Key("Enter"); Check(ui.Create.Step == CreateStep.Confirm, "confirm step");
            s.Draw();
            s.Key("Enter"); var f = s.Draw();
            var p = s.Game.Player;
            Check(s.Game.UiState.Active == Panel.None && s.Game.Turn == turn, "confirming starts the run at turn 0");
            Check(p.CharName == "Mbl" && p.RaceId == Races.All[2].Id && p.RoleId == Roles.All[Roles.All.Length - 1].Id, "the chosen hero is built");
            Check(s.Game.Rng.Seed == 2024, "the seed survives creation");
            Check(f.Cols == 110, "viewport survives creation");

            s.Key("Period", "."); s.Key("Period", ".");
            Check(s.Save() == null, "hero saves");
            var loaded = new Session(); loaded.New(); loaded.Resize(110, 36); loaded.Load();
            Check(loaded.Game.Player.CharName == "Mbl" && loaded.Game.Player.RaceId == p.RaceId && loaded.Game.Player.RoleId == p.RoleId, "load restores the hero");
            Check(loaded.Game.Turn == s.Game.Turn && loaded.Game.Player.HP == p.HP, "load replays onto the same hero");

            // Escape on the first step returns to the title.
            var t = new Session(); t.New(1, true); t.Resize(110, 36); t.Draw();
            t.Key("Escape"); var tf = t.Draw();
            Check(tf.ToTitle && t.AtTitle && t.Game.UiState.Active == Panel.None, "Escape at the name returns to the title");
            SaveStore.DeleteSave();
        }

        /// <summary>Creation now ends on the overworld behind the intro; these flows need a dungeon floor.</summary>
        static void Dive(Session s)
        {
            if (s.Intro) s.Key("Enter");
            if (s.Game.Mode == GameMode.Overworld)
            {
                var w = s.Game.World;
                var dirs = new[] { ("ArrowLeft", -1, 0), ("ArrowRight", 1, 0), ("ArrowUp", 0, -1), ("ArrowDown", 0, 1), ("Numpad7", -1, -1), ("Numpad9", 1, -1), ("Numpad1", -1, 1), ("Numpad3", 1, 1) };
                foreach (var (code, dx, dy) in dirs)
                    if (w.Get(w.PlayerX + dx, w.PlayerY + dy).Feature == Ossuary.Core.World.OverworldFeature.Dungeon) { s.Key(code); break; }
            }
            s.Draw();
        }

        static void CastingFlow()
        {
            var s = new Session(); s.New(606, true); s.Resize(110, 36); s.Draw();
            s.Key("Enter"); s.Key("Enter");
            for (int i = 0; i < 4; i++) s.Key("ArrowDown");   // Wizard
            s.Key("Enter"); s.Key("Enter"); s.Draw();
            Dive(s); var g = s.Game;
            Check(g.Player.RoleId == "wizard", "created a wizard");
            Check(Ossuary.Tests.TestRunner.Arena(g, out int x, out int y), "arena");
            var jackal = new Ossuary.Core.Entities.Monster(Ossuary.Core.Entities.Bestiary.Find("jackal"), g.Rng) { X = x, Y = y };
            jackal.HP = jackal.MaxHP = 60; g.Monsters.Add(jackal);
            int turn = g.Turn, hp = jackal.HP;
            s.Key("KeyZ", "Z", true); s.Draw();
            Check(g.UiState.Active == Panel.Spells, "Z opens the spell list");
            s.Key("ArrowDown"); s.Key("ArrowUp"); s.Draw();
            Check(g.UiState.SpellIndex == 0 && g.Turn == turn, "the list owns the arrows");
            s.Key("KeyA", "a"); s.Draw();
            Check(g.UiState.IsTargeting && g.UiState.Targeting == TargetingMode.Cast, "choosing a spell starts targeting");
            Check(g.UiState.TargetX == x && g.UiState.TargetY == y, "the cursor starts on the nearest monster");
            s.Key("Enter"); s.Draw();
            Check(g.Turn == turn + 1 && !g.UiState.IsTargeting, "the cast resolves and takes a turn");
            Check(g.Player.Mp < g.Player.MpMax, "the cast spent mana");
            Check(jackal.HP <= hp, "the target was not healed");
            s.Key("KeyZ", "Z", true); s.Draw(); s.Key("Escape"); s.Draw();
            Check(g.UiState.Active == Panel.None, "Escape closes the spell list");

            // A self spell replays exactly: the save is only seed, hero and keys.
            var r = new Session(); r.New(606, true); r.Resize(110, 36); r.Draw();
            r.Key("Enter"); r.Key("Enter");
            for (int i = 0; i < 4; i++) r.Key("ArrowDown");
            r.Key("Enter"); r.Key("Enter"); Dive(r);
            for (int i = 0; i < 6; i++) { r.Key("KeyZ", "Z", true); r.Draw(); r.Key("KeyB", "b"); r.Draw(); }
            Check(r.Game.Player.WardTurns > 0 || r.Game.Player.Mp < r.Game.Player.MpMax, "ward cast through the panel");
            Check(r.Save() == null, "save");
            var again = new Session(); again.New(); again.Resize(110, 36); again.Load();
            Check(again.Game.Turn == r.Game.Turn && again.Game.Player.Mp == r.Game.Player.Mp && again.Game.Player.WardTurns == r.Game.Player.WardTurns, "casting replays exactly");
            SaveStore.DeleteSave();
        }

        static void AdvanceFlow()
        {
            var s = new Session(); s.New(808, true); s.Resize(110, 36); s.Draw();
            s.Key("Enter"); s.Key("Enter"); s.Key("Enter"); s.Key("Enter"); s.Draw();   // default hero: Adventurer, human
            Dive(s); var g = s.Game; var p = g.Player;
            Check(p.PendingAdvances == 1, "humans start with a free advancement");
            s.Key("KeyC", "C", true); s.Draw();
            Check(g.UiState.Active == Panel.Advance, "Shift+C opens advancement");
            int turn = g.Turn;
            s.Key("ArrowDown"); s.Key("ArrowUp"); s.Draw();
            Check(g.UiState.AdvanceIndex == 0 && g.Turn == turn, "the list owns the arrows");
            s.Key("KeyA", "a"); s.Draw();   // first available perk: tough
            Check(p.PendingAdvances == 0 && p.PerkRank("tough") == 1, "a letter takes the perk");
            Check(g.UiState.Active == Panel.None, "the panel closes when the picks run out");

            // Levelling up opens the panel by itself.
            g.Monsters.Clear();
            var rat = new Ossuary.Core.Entities.Monster(Ossuary.Core.Entities.Bestiary.Find("giant rat"), g.Rng) { X = p.X, Y = p.Y };
            rat.XpKill = 5000; rat.HP = 0;
            g.KillMonster(rat); s.Draw();
            Check(g.UiState.Active == Panel.Advance && p.PendingAdvances > 0, "level-up opens the advancement panel");
            s.Key("Escape"); s.Draw();
            Check(g.UiState.Active == Panel.None, "Esc closes it");

            // Abilities panel (the fighter knows power strike).
            var f = new Session(); f.New(809, true); f.Resize(110, 36); f.Draw();
            f.Key("Enter"); f.Key("Enter"); f.Key("ArrowDown"); f.Key("Enter"); f.Key("Enter"); Dive(f);   // human fighter
            Check(f.Game.Player.RoleId == "fighter", "fighter created");
            f.Key("KeyV", "V", true); f.Draw();
            Check(f.Game.UiState.Active == Panel.Abilities, "Shift+V opens abilities");
            f.Key("Escape"); f.Draw();
            Check(f.Game.UiState.Active == Panel.None, "Esc closes abilities");
        }

        /// <summary>A finished run leaves one history entry and one morgue file, however many frames are drawn.</summary>
        static void RunRecorded()
        {
            var s = new Session(); s.New(1357); s.Resize(110, 36); s.Draw();
            s.Key("Period", "."); s.Draw();
            int before = SaveStore.ReadHistory().Count;
            s.Game.Player.HP = 0; s.Game.CheckDeath();
            s.Draw(); s.Draw();
            var history = SaveStore.ReadHistory();
            Check(history.Count == before + 1, "a death is recorded exactly once");
            Check(s.LastMorgue != null && System.IO.File.Exists(s.LastMorgue), "the morgue file exists");
            Check(System.IO.File.ReadAllText(s.LastMorgue).Contains("Last words"), "the morgue file has content");
            Check(history[history.Count - 1].Outcome == "died", "outcome is died");

            // The menu lists past runs, newest first, and Esc returns to the menu.
            s = new Session(); s.New(2468); s.Resize(110, 36); s.Draw();
            s.Key("F2"); s.Draw();
            for (int i = 0; i < 20 && Ossuary.Core.MenuRows.All[s.Game.UiState.SettingsIndex] != Ossuary.Core.MenuRow.PastRuns; i++) s.Key("ArrowDown");
            Check(Ossuary.Core.MenuRows.All[s.Game.UiState.SettingsIndex] == Ossuary.Core.MenuRow.PastRuns, "test setup: Past runs row selected");
            s.Key("Enter"); var f = s.Draw();
            Check(s.Game.UiState.Active == Panel.Runs && s.Game.UiState.Runs.Count >= 1, "Past runs opens with the recorded runs");
            s.Key("Escape"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Settings, "Esc returns from Past runs to the menu");
        }

        /// <summary>Achievements persist across runs and the menu lists them.</summary>
        static void AchievementsFlow()
        {
            var s = new Session(); s.New(5151); s.Resize(110, 36); s.Draw();
            s.Game.Monsters.Clear(); s.Game.Player.Kills = 1;
            s.Key("Period", "."); s.Draw();
            Check(SaveStore.ReadAchievements().ContainsKey("first-blood"), "an earned achievement is written to disk");
            var s2 = new Session(); s2.New(5252); s2.Resize(110, 36); s2.Draw();
            Check(s2.Game.AlreadyUnlocked.Contains("first-blood"), "the next run knows what is unlocked");
            s2.Key("F2"); s2.Draw();
            for (int i = 0; i < 20 && Ossuary.Core.MenuRows.All[s2.Game.UiState.SettingsIndex] != Ossuary.Core.MenuRow.Achievements; i++) s2.Key("ArrowDown");
            s2.Key("Enter"); s2.Draw();
            Check(s2.Game.UiState.Active == Panel.Achievements && s2.Game.UiState.Unlocked.ContainsKey("first-blood"), "the achievements panel opens with the unlocked ones");
            s2.Key("ArrowDown"); s2.Key("End"); s2.Draw();
            s2.Key("Escape"); s2.Draw();
            Check(s2.Game.UiState.Active == Panel.Settings, "Esc returns to the menu");
        }

        /// <summary>The daily challenge is the same dungeon and hero for a date, its runs are flagged, and the board lists them best first.</summary>
        static void DailyFlow()
        {
            var day = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
            var a = new Session(); a.NewDaily(day); a.Resize(110, 36); a.Draw();
            var b = new Session(); b.NewDaily(day.AddHours(5)); b.Resize(110, 36); b.Draw();
            var other = new Session(); other.NewDaily(day.AddDays(1)); other.Resize(110, 36); other.Draw();
            Check(a.Game.Rng.Seed == b.Game.Rng.Seed && a.Game.Player.RaceId == b.Game.Player.RaceId && a.Game.Player.RoleId == b.Game.Player.RoleId, "one day, one seed and one hero");
            Check(a.Game.Rng.Seed != other.Game.Rng.Seed, "another day, another seed");
            Check(a.Intro && a.Game.Player.CharName == "Daily", "the daily opens on the story");

            a.Key("Enter"); a.Draw();
            a.Key("Period", "."); a.Game.Player.HP = 0; a.Game.CheckDeath(); a.Draw();
            var runs = SaveStore.ReadHistory();
            Check(runs[runs.Count - 1].Daily == "2026-10-02", "the run is flagged with its date");

            var s = new Session(); s.New(99); s.Resize(110, 36); s.Draw();
            s.Key("F2"); s.Draw();
            for (int i = 0; i < 20 && Ossuary.Core.MenuRows.All[s.Game.UiState.SettingsIndex] != Ossuary.Core.MenuRow.PastRuns; i++) s.Key("ArrowDown");
            s.Key("Enter"); s.Draw();
            int all = s.Game.UiState.Runs.Count;
            s.Key("KeyD"); s.Draw();
            Check(s.Game.UiState.RunsDaily && s.Game.UiState.Runs.Count >= 1 && s.Game.UiState.Runs.Count <= all, "D filters to daily runs");
            Check(s.Game.UiState.Runs.TrueForAll(r => r.Daily.Length > 0), "the daily board has only daily runs");
            s.Key("KeyD"); s.Draw();
            Check(!s.Game.UiState.RunsDaily && s.Game.UiState.Runs.Count == all, "D again shows everything");
        }

        /// <summary>Modes are picked on the confirm step, survive a save, and Hardcore keeps one save that resuming spends.</summary>
        static void DifficultyFlow()
        {
            SaveStore.DeleteSave();
            var s = new Session(); s.New(6006, true); s.Resize(110, 36); s.Draw();
            var c = s.Game.UiState.Create;
            s.Key("Enter"); s.Key("Enter"); s.Key("Enter");
            Check(c.Step == CreateStep.Confirm && c.Difficulty == Difficulty.Normal, "confirm starts on Normal");
            s.Key("ArrowRight"); s.Key("ArrowRight"); s.Draw();
            Check(c.Difficulty == Difficulty.Hardcore, "arrows pick the mode");
            for (int i = 0; i < Difficulties.All.Length - 2; i++) s.Key("ArrowRight");
            Check(c.Difficulty == Difficulty.Normal, "the mode wraps");
            s.Key("ArrowLeft"); Check(c.Difficulty == Difficulty.Trained, "and goes back");
            for (int i = 0; i < Difficulties.All.Length - 3; i++) s.Key("ArrowLeft");
            Check(c.Difficulty == Difficulty.Hardcore, "to Hardcore");
            s.Key("Enter"); s.Draw(); s.Key("Enter"); s.Draw();   // begin, then skip the opening story
            Check(s.Game.Difficulty == Difficulty.Hardcore, "the run is Hardcore");
            s.Key("Period", "."); s.Draw();
            Check(s.Save() != null && !s.HasSave, "no free save in Hardcore");
            s.Key("F5"); s.Draw();
            Check(!s.HasSave, "quicksave is refused in Hardcore");
            Check(s.Save(true) == null && s.HasSave, "quitting writes the one save");
            var back = new Session(); back.New(); back.Resize(110, 36); back.Load();
            Check(back.Game.Difficulty == Difficulty.Hardcore && back.Game.Turn == s.Game.Turn, "the loaded run is still Hardcore");
            Check(!back.HasSave && SaveStore.ReadSave() == null, "resuming a Hardcore run spends its save");

            // Classic: nothing is eaten.
            var g = new Game(77) { Difficulty = Difficulty.Classic };
            int food = g.Player.Nutrient;
            for (int i = 0; i < 300; i++) g.Wait();
            Check(g.Player.Nutrient == food && g.Player.Hunger == 0, "Classic has no hunger");
            var n = new Game(77);
            for (int i = 0; i < 300; i++) n.Wait();
            Check(n.Player.Nutrient < food, "Normal still gets hungry");
        }

        /// <summary>A death below the first level leaves bones; a replay keeps the snapshot; laying the shade to rest removes them.</summary>
        static void BonesFlow()
        {
            var s = new Session(); s.New(8484); s.Resize(110, 36); s.Draw();
            s.Key("Period", "."); s.Game.DescendTo("The Dungeons", 2); s.Draw();
            s.Game.Player.HP = 0; s.Game.CheckDeath(); s.Draw();
            var bones = SaveStore.ReadBones();
            Check(bones.Exists(b => b.Key == "The Dungeons@2"), "the death left bones on its level");

            var s2 = new Session(); s2.New(8585); s2.Resize(110, 36); s2.Draw();
            Check(s2.Game.Graveyard.Exists(b => b.Key == "The Dungeons@2"), "a new run starts with the graveyard");
            s2.Key("Period", "."); s2.Draw();
            Check(s2.Save() == null, "the run saves");
            SaveStore.RemoveBones("The Dungeons@2");
            var s3 = new Session(); s3.Load(); s3.Draw();
            Check(s3.Game.Graveyard.Exists(b => b.Key == "The Dungeons@2"), "a loaded run keeps the graveyard it began with");
            s3.Game.LaidToRest.Add("The Dungeons@2");
            SaveStore.WriteBones(bones.Find(b => b.Key == "The Dungeons@2"));
            s3.Key("Period", "."); s3.Draw();
            Check(!SaveStore.ReadBones().Exists(b => b.Key == "The Dungeons@2"), "a shade laid to rest takes its bones off disk");
            SaveStore.DeleteSave();
        }

        /// <summary>Holding a direction keeps walking, but only while the way is calm, and never queues turns.</summary>
        static void HeldKeyWalking()
        {
            var s = new Session(); s.New(4242); s.Resize(110, 36); s.Draw();
            var g = s.Game;
            g.Monsters.Clear();
            int y = g.Player.Y, x0 = g.Player.X;
            // A straight, empty corridor of twelve floor cells with a wall at the end.
            for (int x = x0; x <= x0 + 12; x++)
            {
                g.Map.Set(x, y, TileKind.Floor); g.Map.Set(x, y - 1, TileKind.Wall); g.Map.Set(x, y + 1, TileKind.Wall);
                GroundItems.RemoveCell(g.Map.Number, x, y);
            }
            g.Map.Set(x0 + 13, y, TileKind.Wall);
            g.UpdateFov(); s.Draw();

            int turn = g.Turn;
            s.KeyRepeat("ArrowRight");
            Check(g.Player.X == x0, "a repeat does nothing before a first real step");
            s.Key("ArrowRight");
            for (int i = 0; i < 4; i++) s.KeyRepeat("ArrowRight");
            Check(g.Player.X == x0 + 5, "held direction keeps walking");
            Check(g.Turn - turn == 5, "each repeat is exactly one turn");

            s.KeyRepeat("KeyI", "i");
            Check(g.UiState.Active == Panel.None, "only movement repeats");

            // Something hostile steps into view: the repeat stops by itself.
            var rat = new Monster(Bestiary.Find("giant rat"), g.Rng) { X = g.Player.X + 4, Y = y };
            g.Monsters.Add(rat); g.UpdateFov(); s.Key("ArrowRight"); int at = g.Player.X;
            Check(g.Map.IsCurrentlyVisible(rat.X, rat.Y), "test setup: the rat is in sight");
            s.KeyRepeat("ArrowRight"); s.KeyRepeat("ArrowRight");
            Check(g.Player.X == at, "a hostile in view stops the held walk");
            g.Monsters.Clear(); g.UpdateFov();

            // A fresh press rearms it.
            s.Key("ArrowLeft"); int x1 = g.Player.X; s.KeyRepeat("ArrowLeft");
            Check(g.Player.X == x1 - 1, "a new press rearms the walk");
            g.Player.HP = g.Player.MaxHP;
            s.Key("ArrowRight"); int x2 = g.Player.X;
            GroundItems.Add(g.Map.Number, x2 + 1, y, new Ossuary.Core.Items.Item(Ossuary.Core.Items.Catalogue.Weapons[0], g.Rng, g.NextUid()));
            s.KeyRepeat("ArrowRight"); s.KeyRepeat("ArrowRight");
            Check(g.Player.X == x2 + 1, "an item underfoot stops the held walk");

            // The auto-walk keys reach the engine: T explores, Shift+S rests, ` heads for the stairs.
            var s2 = new Session(); s2.New(777); s2.Resize(110, 36); s2.Draw();
            s2.Game.Monsters.Clear();
            int t0 = s2.Game.Turn; s2.Key("KeyT", "t"); s2.Draw();
            Check(s2.Game.Turn > t0, "T explores");
            s2.Game.Player.HP = 1; s2.Game.Player.Nutrient = 5000; s2.Key("KeyS", "S", true); s2.Draw();
            Check(s2.Game.Player.HP > 1, "Shift+S rests");
            t0 = s2.Game.Turn; s2.Key("Backquote", "`"); s2.Draw();
            Check(s2.Game.Turn >= t0, "` travels to the stairs");
        }

        /// <summary>
        /// A monster in the road is fought with Enter, Space or K and fled from with R or Shift+Comma.
        /// Plain K used to be "walk north" and was refused, so the player could only ever run.
        /// </summary>
        static void RoadEncounter()
        {
            var s = new Session(); s.New(9090); s.Resize(110, 36); s.Draw();
            s.Game.LeaveToOverworld(); s.Draw();
            var g = s.Game;
            void Block()
            {
                g.EncounterMonster = new Monster(Bestiary.Find("giant rat"), g.Rng);
                g.EncounterX = g.World.PlayerX; g.EncounterY = g.World.PlayerY;
                g.ActiveEncounter = true;
                s.Draw();
            }
            int Clock() => g.World.Day * 24 + g.World.Hour;

            foreach (var (code, key, shift, label) in new[] { ("KeyK", "k", false, "K"), ("Enter", "Enter", false, "Enter"), ("Space", " ", false, "Space"), ("KeyF", "f", false, "F") })
            {
                g.World.Hour = 8; g.Player.HP = g.Player.MaxHP;
                Block(); int before = Clock(), x = g.World.PlayerX, y = g.World.PlayerY;
                s.Key(code, key, shift); s.Draw();
                Check(Clock() > before, label + " attacks the monster in the road");
                Check(g.World.PlayerX == x && g.World.PlayerY == y, label + " does not walk anywhere");
                g.ActiveEncounter = false; g.EncounterMonster = null; s.Draw();
            }

            foreach (var (code, key, shift, label) in new[] { ("KeyR", "r", false, "R"), ("Comma", "<", true, "<") })
            {
                Block();
                s.Key(code, key, shift); s.Draw();
                Check(!g.ActiveEncounter && g.EncounterMonster == null, label + " flees the encounter");
            }

            // Walking and travelling are refused while something blocks the way, and say how to get out.
            Block(); int px = g.World.PlayerX, py = g.World.PlayerY;
            s.Key("ArrowRight"); s.Draw();
            Check(g.World.PlayerX == px && g.ActiveEncounter, "arrows cannot walk past a monster");
            s.Key("KeyO"); s.Draw(); s.Key("ArrowRight"); s.Key("ArrowRight"); s.Key("Enter"); s.Draw();
            Check(g.World.PlayerX == px && g.World.PlayerY == py && g.ActiveEncounter, "travel is refused while a monster blocks the road");
            Check(g.UiState.Active == Panel.None || g.UiState.Active == Panel.Travel, "travel leaves the panel in a sane state");
            g.UiState.Active = Panel.None; g.UiState.TravelMode = false;
            g.ActiveEncounter = false; g.EncounterMonster = null; s.Draw();

            // Killing it on the road awards experience once, and the road is free afterwards.
            Block(); g.EncounterMonster.HP = 1; g.Player.Level = Math.Max(1, g.Player.Level);
            int kills = g.Player.Kills;
            for (int i = 0; i < 40 && g.ActiveEncounter; i++) { s.Key("KeyK", "k"); s.Draw(); if (g.Mode == GameMode.GameOver) break; }
            Check(!g.ActiveEncounter && g.Player.Kills == kills + 1, "the monster dies and counts as a kill");

            // Outside an encounter K is still "walk north".
            s.Draw();
            Check(Input.Translate("KeyK", "k", false, false) == "move-n", "K is north when nothing blocks the road");
        }

        /// <summary>Town through the real key path: bump to talk, stairs, services, and Esc back out of a shop.</summary>
        static void TownFlow()
        {
            var s = new Session(); s.New(1717); s.Resize(120, 40); s.Draw();
            var g = s.Game;
            g.LeaveToOverworld(); g.EnterTown("Keytown"); s.Draw();
            Check(g.Mode == GameMode.TownMap, "in town");

            // A service counter opens the menu; letters pick rows; Esc leaves.
            Building smith = null;
            foreach (var b in g.Town.Buildings) if (b.Kind == BuildingKind.Smithy) smith = b;
            int sx = smith.CounterX, sy = smith.CounterY + (smith.DoorY > smith.CounterY ? 1 : -1);
            g.Player.X = sx; g.Player.Y = sy;
            s.Key(smith.DoorY > smith.CounterY ? "ArrowUp" : "ArrowDown"); var f = s.Draw();
            Check(g.UiState.Active == Panel.Service && f.Panel == "Service", "bumping the counter opens the service panel");
            int turn = g.Turn;
            s.Key("KeyA"); s.Draw();    // row a: browse the wares
            Check(g.UiState.Active == Panel.Shop && g.InShop, "letter a browses the wares");
            s.Key("Escape"); s.Draw();
            Check(g.UiState.Active == Panel.Service && !g.InShop, "Esc from the shop returns to the service menu");
            s.Key("Escape"); s.Draw();
            Check(g.UiState.Active == Panel.None && g.Turn == turn, "Esc closes the menu without spending a turn");

            // Stairs through the real keys: > goes down, < goes up. The hint on arrival names the right key.
            var tavern = default(Building);
            foreach (var b in g.Town.Buildings) if (b.Kind == BuildingKind.Tavern) tavern = b;
            for (int y = tavern.Y; y < tavern.Y + tavern.H; y++)
                for (int x = tavern.X; x < tavern.X + tavern.W; x++)
                    if (g.Town.Map.Get(x, y) == TileKind.StairsDown) { g.Player.X = x; g.Player.Y = y; }
            s.Key("Period", ">", true); f = s.Draw();
            Check(g.TownZ == -1 && f.Mode.Length > 0, "shift+. descends into the cellar");
            s.Key("Comma", "<", true); s.Draw();
            Check(g.TownZ == 0, "shift+, climbs back out");
        }

        static void AltarFlow()
        {
            var s = new Session(); s.New(909, true); s.Resize(110, 36); s.Draw();
            s.Key("Enter"); s.Key("Enter");
            for (int i = 0; i < 3; i++) s.Key("ArrowDown");   // Cleric
            s.Key("Enter"); s.Key("Enter"); s.Draw();
            Dive(s); var g = s.Game; var p = g.Player;
            Check(p.RoleId == "cleric", "cleric created");
            // Put an altar to the right (or left) of the player, and make it the player's own god.
            int dir = Tiles.Walkable(g.Map.Get(p.X + 1, p.Y)) ? 1 : -1;
            int ax = p.X + dir, ay = p.Y;
            Check(Tiles.Walkable(g.Map.Get(ax, ay)), "room for an altar");
            g.Map.Set(ax, ay, TileKind.Altar);
            p.God = Gods.AtAltar(g.Map.Number, ax, ay).Id; p.Piety = 100; p.PrayerTimer = 0;
            p.HP = Math.Max(1, p.MaxHP / 2);
            int turn = g.Turn;
            s.Key(dir > 0 ? "ArrowRight" : "ArrowLeft"); s.Draw();
            Check(g.UiState.Active == Panel.Altar && g.Turn == turn, "walking into an altar opens its menu");
            s.Key("ArrowDown"); s.Key("ArrowUp"); s.Draw();
            Check(g.UiState.AltarIndex == 0 && g.Turn == turn, "the menu owns the arrows");
            s.Key("KeyA", "a"); s.Draw();   // first row: Pray
            Check(p.PrayerTimer > 0 && g.UiState.Active == Panel.None, "a letter prays and closes the menu");
            s.Key(dir > 0 ? "ArrowRight" : "ArrowLeft"); s.Draw();
            s.Key("Escape"); s.Draw();
            Check(g.UiState.Active == Panel.None, "Esc leaves the altar");
            s.Key(dir > 0 ? "ArrowRight" : "ArrowLeft"); s.Draw();
            s.Key("KeyF", "f"); s.Draw();   // sixth row: renounce, which only asks
            Check(g.UiState.Active == Panel.Altar && g.UiState.AltarConfirm && p.God != null, "renouncing asks twice");
            s.Key("KeyF", "f"); s.Draw();
            Check(p.God == null && g.UiState.Active == Panel.None, "the second press renounces");
        }

        public static void Run()
        {
            string data = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ossuary-test-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("OSSUARY_DATA", data);
            Check(Input.Translate("KeyK", "k", false, false) == "move-n", "vi north");
            Check(Input.Translate("KeyK", "K", true, false) == "k", "shift kick");
            Check(Input.Translate("Period", ">", true, false) == ">", "descending");
            Check(Input.Translate("KeyQ", "q", false, true) == "quit", "quit");
            var s = new Session(); s.New(31337); s.Resize(110, 36);
            var f = s.Draw();
            Check(f.Glyphs.Length == 3960 && f.Fg.Length == 3960 && f.Bg.Length == 3960, "cell protocol");
            Check(f.Sounds != null, "every frame says what to play, even if it is nothing");
            int turn = s.Game.Turn;
            s.Key("KeyI"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Inventory, "inventory request drained");
            s.Key("ArrowUp"); s.Draw();
            Check(s.Game.Turn == turn && s.Game.UiState.Active == Panel.None, "panel consumes movement");
            s.Key("KeyV"); s.Draw();
            Check(s.Game.UiState.IsTargeting, "target starts");
            int x = s.Game.Player.X;
            s.Key("ArrowRight"); s.Draw();
            Check(s.Game.Player.X == x && s.Game.UiState.TargetX == x + 1 && s.Game.Turn == turn, "target moves independently");
            s.Key("Escape"); s.Draw();
            s.Key("F2"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Settings, "F2 opens the menu");
            s.Key("ArrowDown"); s.Key("ArrowDown"); s.Key("ArrowDown"); s.Key("Enter"); s.Draw();
            Check(DisplaySettings.Current.Crt == CrtLevel.Strong, "settings own arrows");
            Check(s.Game.Turn == turn, "menu keys never advance a turn");
            s.Key("Escape"); s.Draw();
            s.Key("Period", "."); s.Draw();
            Check(s.Game.Turn == turn + 1, "wait advances exactly one turn");
            s.Resize(1, 9999); f = s.Draw();
            Check(f.Cols == 84 && f.Rows == 120, "resize is bounded");
            s.New(777); s.Resize(110, 36); s.Draw();
            s.Game.LeaveToOverworld(); s.Draw(); s.Key("KeyO"); s.Draw();
            Check(s.Game.UiState.Active == Panel.Travel, "travel opens");
            turn = s.Game.Turn;
            s.Key("ArrowRight"); s.Draw();
            Check(s.Game.Turn == turn, "travel cursor does not move player");
            s.Key("Escape"); s.Draw();
            Check(!s.Game.UiState.TravelMode && s.Game.UiState.Active == Panel.None, "travel cancels");
            s.Game.EnterTown("Ravensgate"); s.Game.OpenShop(s.Game.Town.Shops[0]);
            s.Game.UiState.Active = Panel.Shop; s.Draw();
            turn = s.Game.Turn;
            s.Key("ArrowDown"); s.Draw();
            Check(s.Game.UiState.ShopIndex == 1 && s.Game.Turn == turn, "shop owns cursor");
            s.Key("KeyS"); s.Draw();
            Check(s.Game.PendingChoice.Active, "sell raises item choice");
            s.Key("ArrowDown"); s.Draw();
            Check(s.Game.UiState.ChoiceIndex == 1, "choice owns cursor over shop");
            s.Key("Escape"); s.Draw();
            Check(!s.Game.PendingChoice.Active && s.Game.InShop, "choice cancellation keeps shop");
            s.Key("Escape"); s.Draw();
            Check(!s.Game.InShop, "shop closes");
            s.Game.Mode = GameMode.GameOver; s.Draw(); s.Key("Enter"); s.Draw();
            Check(s.Game.Mode == GameMode.Dungeon && s.Game.Turn == 0 && s.Hud.Ui.Width == 110, "death restarts and keeps viewport");
            DisplaySettings.Current.Apply(ThemePreset.Ossuary, CrtLevel.Subtle);
            Menus(); LanguageAndOpening(); Bindings(); SaveAndLoad(); Creation(); CastingFlow(); AdvanceFlow(); AltarFlow(); RoadEncounter(); HeldKeyWalking(); RunRecorded(); BonesFlow(); DifficultyFlow(); DailyFlow(); AchievementsFlow(); TownFlow();
            try { System.IO.Directory.Delete(data, true); } catch { /* temp dir only */ }
            Environment.SetEnvironmentVariable("OSSUARY_DATA", null);
            Console.WriteLine("==== desktop: input, choices, targeting, travel, shop, settings, restart and frame protocol PASS ====");
        }
    }
}
