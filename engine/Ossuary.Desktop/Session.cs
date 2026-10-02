using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Ossuary.Core;
using Ossuary.Core.Entities;

namespace Ossuary.Desktop
{
    // Desktop application flow, separated from both the simulation and its renderer.
    public sealed class Session
    {
        public Game Game { get; private set; }
        public GameHud Hud { get; private set; }
        public bool ExitRequested { get; private set; }

        /// <summary>True once the player has acted in this run (the title offers "continue").</summary>
        public bool Started { get; private set; }
        /// <summary>Set for the frame that follows "Main menu": the frontend shows the title.</summary>
        public bool ToTitle { get; private set; }

        /// <summary>The opening story is playing: the frame carries its pages and the next key starts the run.</summary>
        public bool Intro { get; private set; }
        public bool HasSave { get; private set; }
        /// <summary>The title screen is up: the frame carries only the menu panels, over the title.</summary>
        public bool AtTitle { get; private set; }
        public string SaveInfo { get; private set; } = "";

        long _quitAt;
        ulong _seed;
        int _generation;
        bool _replaying, _quitRan;
        List<string> _log = new List<string>();

        public Session() { RefreshSave(); }

        /// <summary>Starts a fresh run. With <paramref name="create"/> the creation screen opens first.</summary>
        public void New(ulong? seed = null, bool create = false) => Start(seed, null, null, null, create, false);

        void Start(ulong? seed, string name, string race, string role, bool create, bool overworld, List<Bones> bones = null, Difficulty difficulty = Difficulty.Normal)
        {
            ulong s = seed ?? BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8), 0);
            Game = role == null ? new Game(s) : Game.NewHero(s, name, race, role);
            Game.Difficulty = difficulty; _difficulty = difficulty;
            _seed = Game.Rng.Seed;
            _graveyard = bones ?? SaveStore.ReadBones();
            Game.Graveyard = _graveyard;
            _name = Game.Player.CharName; _race = Game.Player.RaceId; _role = Game.Player.RoleId;
            _overworld = overworld;
            if (overworld) Game.BeginAtOverworld();
            Hud = new GameHud(Game);
            Hud.Ui.Cmd = Hud.Cmd;
            _quitAt = 0;
            ExitRequested = false; ToTitle = false; Started = false; AtTitle = false; Intro = false;
            _log = new List<string>();
            _generation++;
            if (create) { Game.UiState.Create.Reset(); Game.UiState.Active = Panel.Create; }
        }

        bool _overworld;
        Difficulty _difficulty;
        List<Bones> _graveyard = new List<Bones>();
        string _name = Heroes.DefaultName, _race = "human", _role = "adventurer";
        // Hero (name, race, role) is a save field, not a logged key, so the creation form needs no replay.

        public void SetTitle(bool on)
        {
            AtTitle = on;
            var ui = Game.UiState;
            if (on && (ui.Active == Panel.Settings || ui.Active == Panel.Controls || ui.Active == Panel.Runs)) ui.Active = Panel.None;
            ui.Rebinding = false;
        }

        public void Resize(int cols, int rows) => Hud.Ui.Resize(Math.Clamp(cols, 84, 240), Math.Clamp(rows, 26, 120));
        static int Wrap(int v, int n) => n <= 0 ? 0 : ((v % n) + n) % n;
        static bool Accept(string code) => code == "Enter" || code == "NumpadEnter" || code == "Space";

        // ------------------------------------------------------------------ saves

        void RefreshSave()
        {
            var data = SaveStore.ReadSave();
            HasSave = data != null;
            SaveInfo = data?.Info ?? "";
        }

        string Describe()
        {
            var p = Game.Player;
            string where = Game.Map != null ? Game.Map.LevelName : Game.World.CurrentRegionName;
            return $"Lv {p.Level}  {where}  T {Game.Turn}";
        }

        /// <summary>Writes the run to disk. Returns null on success, otherwise a short error.</summary>
        public string Save(bool forQuit = false)
        {
            if (Game.Mode == GameMode.GameOver || Game.Mode == GameMode.Won || !Started) return Loc.T("Nothing to save yet.");
            if (_difficulty == Difficulty.Hardcore && !forQuit) return Loc.T("Hardcore: the run is saved only when you quit.");
            try
            {
                SaveStore.WriteSave(new SaveData { Seed = _seed.ToString(), Name = _name, Race = _race, Role = _role, Overworld = _overworld, Bones = _graveyard, Difficulty = _difficulty.ToString(), Keys = new List<string>(_log), Info = Describe() });
                RefreshSave();
                return null;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return ex.Message; }
        }

        /// <summary>Rebuilds the saved run by replaying its keys. Throws if there is no usable save.</summary>
        public void Load()
        {
            var data = SaveStore.ReadSave() ?? throw new InvalidOperationException("No saved run.");
            int cols = Hud?.Ui.Width ?? 110, rows = Hud?.Ui.Height ?? 36;
            Start(ulong.Parse(data.Seed), data.Name, data.Race, data.Role, false, data.Overworld, data.Bones ?? new List<Bones>(), Difficulties.Parse(data.Difficulty)); Resize(cols, rows);
            _replaying = true;
            try
            {
                foreach (string entry in data.Keys)
                {
                    var p = entry.Split('|');
                    if (p.Length != 4) throw new FormatException("Corrupt save entry.");
                    KeyCore(p[0], p[1], p[2] == "1", p[3] == "1");
                    Hud.Draw();   // drains panel requests exactly as live play does after every key
                }
            }
            catch
            {
                New(); Resize(cols, rows);
                throw;
            }
            finally { _replaying = false; }
            if (Game.UiState.Active == Panel.Settings) Game.UiState.Active = Panel.None;
            _log = new List<string>(data.Keys);
            Game.LaidToRest.Clear();
            // Hardcore keeps a single save: resuming spends it.
            if (_difficulty == Difficulty.Hardcore) SaveStore.DeleteSave();
            Started = true; RefreshSave();
        }

        // A run that ends (death, victory or abandoning it) leaves a morgue file and a history entry, once.
        Game _recorded;

        void RecordRun()
        {
            if (_replaying || Game == null || ReferenceEquals(_recorded, Game)) return;
            if (Game.Mode != GameMode.GameOver && Game.Mode != GameMode.Won) return;
            _recorded = Game;
            if (!Started && Game.Turn == 0) return;   // nothing was played
            var record = Morgue.Summarize(Game);
            record.Date = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            string path = SaveStore.WriteRun(record, Morgue.Text(Game, record));
            if (path != null) LastMorgue = path;
            var bones = Game.LeaveBones();
            if (bones != null) SaveStore.WriteBones(bones);
        }

        /// <summary>Where the last finished run's morgue file went (for the death screen); null if none.</summary>
        public string LastMorgue;

        void DeleteSaveIfThisRun()
        {
            var data = SaveStore.ReadSave();
            if (data != null && data.Seed == _seed.ToString()) { SaveStore.DeleteSave(); RefreshSave(); }
        }

        // ------------------------------------------------------------------ input

        // Held-key walking: a repeat is honoured only after a step that was calm (see Game.CanKeepWalking).
        bool _walkCalm;

        /// <summary>Autorepeat of a held key. Anything but a calm walking step is ignored, so holding a key never queues turns.</summary>
        public void KeyRepeat(string code, string key = "", bool shift = false, bool ctrl = false)
        {
            if (!_walkCalm || Intro || Game == null) return;
            var ui = Game.UiState;
            if (ui.Active != Panel.None || ui.Rebinding || ui.IsTargeting || ui.TravelMode || Game.PendingChoice.Active) return;
            KeyBindings.Current.Resolve(ref code, ref key, ref shift, ref ctrl);
            if (!IsWalk(code, key, shift, ctrl)) return;
            Key(code, key, shift, ctrl, bindingsApplied: true);
        }

        static bool IsWalk(string code, string key, bool shift, bool ctrl)
        {
            string command = Input.Translate(code, key, shift, ctrl);
            return command != null && command.StartsWith("move-");
        }

        public void Key(string code, string key = "", bool shift = false, bool ctrl = false, bool bindingsApplied = false)
        {
            _walkCalm = false;
            var ui = Game.UiState;
            ToTitle = false;
            if (Intro) { if (!_replaying) Started = true; Intro = false; return; }
            if (ui.Active == Panel.Create) { CreateKey(code, key); return; }
            if (ui.Active == Panel.Spells || ui.Active == Panel.Abilities || ui.Active == Panel.Advance || ui.Active == Panel.Altar || ui.Active == Panel.Service)
            {
                // List letters (a..z) must not pass through the key bindings, which would turn them into movement.
                KeyCore(code, key, shift, ctrl);
                _log.Add($"{code}|{key}|{(shift ? 1 : 0)}|{(ctrl ? 1 : 0)}");
                Started = true;
                return;
            }
            if (ui.Rebinding) { RebindKey(code, shift, ctrl); return; }
            if (ui.Active == Panel.Controls) { ControlsKey(code, shift); return; }
            if (ui.Active == Panel.Runs) { RunsKey(code); return; }

            if (!bindingsApplied) KeyBindings.Current.Resolve(ref code, ref key, ref shift, ref ctrl);
            if (code == "None") return;

            bool walk = IsWalk(code, key, shift, ctrl);
            int hp = Game.Player.HP; long said = Game.Said; var from = Game.WalkPosition();
            bool log = ShouldLog(code, ctrl, ui);
            int generation = _generation;
            _quitRan = false;
            // A key on the death screen starts a new run; the finished one must not keep its save.
            if (Game.Mode == GameMode.GameOver || Game.Mode == GameMode.Won) DeleteSaveIfThisRun();
            KeyCore(code, key, shift, ctrl);

            bool dead = Game.Mode == GameMode.GameOver || Game.Mode == GameMode.Won;
            if (dead) { RecordRun(); DeleteSaveIfThisRun(); return; }
            if (generation != _generation) return;
            if ((log || _quitRan) && !ExitRequested)
            {
                _log.Add($"{code}|{key}|{(shift ? 1 : 0)}|{(ctrl ? 1 : 0)}");
                Started = true;
            }
            if (Game.LaidToRest.Count > 0)
            {
                foreach (string grave in Game.LaidToRest) SaveStore.RemoveBones(grave);
                Game.LaidToRest.Clear();
            }
            // The next repeat is allowed only if this step moved us, said nothing, cost no HP and the way is still calm.
            _walkCalm = walk && Game.Said == said && Game.Player.HP >= hp
                && Game.WalkPosition() != from && Game.CanKeepWalking();
        }

        // Keys that only drive the menu, the display or the host never enter the run log, so a
        // replay can't change the player's settings or reopen a menu.
        bool ShouldLog(string code, bool ctrl, UiState ui)
        {
            if (ui.Active == Panel.Settings) return false;
            if (code == "F2" || code == "F3" || code == "F4" || code == "F5") return false;
            if (ctrl && code == "KeyQ") return false;   // logged only if it actually abandons the run
            if (code == "Escape" && OpensMenu(ui)) return false;
            return true;
        }

        bool OpensMenu(UiState ui) =>
            ui.Active == Panel.None && !Game.PendingChoice.Active && !ui.TravelMode && !ui.IsTargeting
            && Game.Mode != GameMode.GameOver && Game.Mode != GameMode.Won;

        void KeyCore(string code, string key, bool shift, bool ctrl)
        {
            var ui = Game.UiState;
            string command = Input.Translate(code, key, shift, ctrl);
            if (code == "F3" || code == "F4") { Hud.Cmd.Execute(command); return; }
            if (Game.Mode == GameMode.GameOver || Game.Mode == GameMode.Won || ui.Active == Panel.Death || ui.Active == Panel.Win)
            {
                if (code == "Escape") { ExitRequested = true; return; }
                int cols = Hud.Ui.Width, rows = Hud.Ui.Height;
                New(null, true); Resize(cols, rows); return;
            }
            if (Game.PendingChoice.Active && Game.PendingChoice.Items.Count > 0)
            {
                int count = Game.PendingChoice.Items.Count;
                if (Input.Step(code, out int dx, out int dy)) ui.ChoiceIndex = Wrap(ui.ChoiceIndex + dx + dy, count);
                else if (Accept(code))
                {
                    var chosen = Game.PendingChoice.Items[Math.Clamp(ui.ChoiceIndex, 0, count - 1)];
                    ui.ChoiceIndex = 0;
                    if (Game.InShop && Game.PendingChoice.Prompt == Commands.SellPrompt) Hud.Cmd.CommitSellChoice(chosen);
                    else Hud.Cmd.CommitChoice(chosen);
                }
                else if (code == "Escape") { Game.PendingChoice.Clear(); ui.ChoiceIndex = 0; }
                return;
            }
            if (ui.Active == Panel.Settings) { MenuKey(code); return; }
            if (ui.Active == Panel.Spells) { SpellsKey(code); return; }
            if (ui.Active == Panel.Abilities) { AbilitiesKey(code); return; }
            if (ui.Active == Panel.Advance) { AdvanceKey(code); return; }
            if (ui.Active == Panel.Altar) { AltarKey(code); return; }
            if (ui.Active == Panel.Service) { ServiceKey(code); return; }
            if (ui.Active == Panel.Travel || ui.TravelMode)
            {
                ui.TravelMode = ui.Active == Panel.Travel;
                if (Input.Step(code, out int dx, out int dy)) Game.TravelCursorTo(ui.TravelX + dx, ui.TravelY + dy);
                else if (code == "Enter" || code == "NumpadEnter")
                {
                    ui.TravelMode = false; ui.Active = Panel.None;
                    Game.CommitTravel(ui.TravelX, ui.TravelY);
                }
                else if (code == "Escape") { ui.TravelMode = false; ui.Active = Panel.None; }
                return;
            }
            if (ui.IsTargeting)
            {
                if (Input.Step(code, out int dx, out int dy)) Game.NudgeTarget(dx, dy);
                else if (Accept(code)) Game.ResolveTargeting(ui.TargetX, ui.TargetY);
                else if (code == "Escape") ui.Targeting = TargetingMode.None;
                return;
            }
            if (ui.Active == Panel.Shop)
            {
                if (code == "Escape")
                {
                    ui.Active = Panel.None; ui.ShopIndex = 0;
                    if (Game.InShop) Game.CloseShop();
                    if (Game.ShopReturnsToServices) ui.Active = Panel.Service;   // back to the menu it was opened from
                }
                else if (code == "Enter" || code == "NumpadEnter" || code == "KeyB")
                {
                    Hud.Cmd.Execute("shop-buy"); if (!Game.InShop) ui.Active = Panel.None;
                }
                else if (code == "KeyS") Hud.Cmd.Execute("shop-sell");
                else if (Input.Step(code, out int dx, out int dy))
                {
                    int n = Game.CurrentShop?.Stock.Count ?? 0;
                    int next = dy != 0 ? ui.ShopIndex + dy : dx > 0 ? n - 1 : 0;
                    ui.ShopIndex = n <= 0 ? 0 : Math.Clamp(next, 0, n - 1);
                }
                return;
            }
            if (ui.IsOpen)
            {
                if (code == "Escape" || Accept(code) || command != null) ui.Active = Panel.None;
                return;
            }
            // A monster blocking the road is fought or fled from: Enter/Space/K/F attack, R or < flee. Plain K is
            // "walk north" everywhere else, which is why attacking used to do nothing. The typed character decides,
            // not the code: key bindings have already rewritten the code (KeyK arrives here as ArrowUp).
            if (Game.Mode == GameMode.Overworld && Game.ActiveEncounter && !ctrl)
            {
                bool plain = !shift;
                if (code == "Enter" || code == "NumpadEnter" || code == "Space" || key == " " || (plain && (key == "k" || key == "f"))) command = "k";
                else if ((plain && key == "r") || key == "<") command = "<";
            }
            if (command == null)
            {
                // Escape with nothing open is the pause menu.
                if (code == "Escape") Hud.Cmd.Execute("settings");
                return;
            }
            if (command == "save") { QuickSave(); return; }
            if (command == "quit")
            {
                long now = Stopwatch.GetTimestamp();
                if (!_replaying && (_quitAt == 0 || (now - _quitAt) / (double)Stopwatch.Frequency > 3))
                {
                    _quitAt = now;
                    Game.Say("Press Ctrl-Q again within 3s to abandon the run.", MessageKind.Warn);
                    return;
                }
                _quitAt = 0; _quitRan = true;
            }
            Hud.Cmd.Execute(command);
        }

        void QuickSave()
        {
            string error = Save();
            Game.Say(error == null ? "Game saved." : "Could not save: " + error, error == null ? MessageKind.Good : MessageKind.Bad);
        }

        // ----------------------------------------------------------------- spells

        void SpellsKey(string code)
        {
            var ui = Game.UiState;
            var known = Game.Player.Spells;
            if (known.Count == 0 || code == "Escape" || code == "KeyZ") { ui.Active = Panel.None; return; }
            ui.SpellIndex = Math.Clamp(ui.SpellIndex, 0, known.Count - 1);
            int pick = -1;
            if (code.Length == 4 && code.StartsWith("Key") && code[3] >= 'A' && code[3] <= 'Z') pick = code[3] - 'A';
            if (Input.Step(code, out int dx, out int dy) && pick < 0)
            {
                if (code.StartsWith("Arrow") || code.StartsWith("Numpad")) ui.SpellIndex = Wrap(ui.SpellIndex + dx + dy, known.Count);
                return;
            }
            if (pick >= 0 && pick < known.Count) ui.SpellIndex = pick;
            else if (!Accept(code)) return;
            string id = known[ui.SpellIndex];
            ui.Active = Panel.None;
            Game.BeginCast(id);
        }

        // Shared by the three list panels: arrows move, a letter or Enter picks. Returns the chosen
        // index, or -1 when nothing was chosen (the panel stays open) and -2 when it should close.
        static int ListKey(string code, ref int index, int count, bool closeOnLetterZ)
        {
            if (count == 0 || code == "Escape") return -2;
            index = Math.Clamp(index, 0, count - 1);
            int pick = -1;
            if (code.Length == 4 && code.StartsWith("Key") && code[3] >= 'A' && code[3] <= 'Z') pick = code[3] - 'A';
            if (closeOnLetterZ && code == "KeyZ") return -2;
            if (pick < 0 && Input.Step(code, out int dx, out int dy))
            {
                if (code.StartsWith("Arrow") || code.StartsWith("Numpad")) index = Wrap(index + dx + dy, count);
                return -1;
            }
            if (pick >= 0 && pick < count) { index = pick; return pick; }
            return Accept(code) ? index : -1;
        }

        void AbilitiesKey(string code)
        {
            var ui = Game.UiState; var list = Game.Player.Abilities;
            int r = ListKey(code, ref ui.AbilityIndex, list.Count, false);
            if (r == -2) ui.Active = Panel.None;
            else if (r >= 0) { string id = list[r]; ui.Active = Panel.None; Game.BeginAbility(id); }
        }

        void ServiceKey(string code)
        {
            var ui = Game.UiState;
            var rows = Game.ServiceRows();
            int r = ListKey(code, ref ui.ServiceIndex, rows.Count, false);
            if (r == -2) { ui.Active = Panel.None; return; }
            if (r < 0) return;
            var row = rows[r];
            if (!row.Enabled)
            {
                Game.Tell(Game.Player.Gold < row.Price ? "You cannot afford that." : "You cannot do that now.", MessageKind.Warn);
                return;
            }
            if (Game.ServiceAction(row.Id)) ui.Active = Panel.None;
        }

        void AltarKey(string code)
        {
            var ui = Game.UiState;
            var rows = Game.AltarRows();
            int before = ui.AltarIndex;
            int r = ListKey(code, ref ui.AltarIndex, rows.Count, false);
            if (r == -2) { ui.Active = Panel.None; return; }
            if (r < 0) { if (ui.AltarIndex != before) ui.AltarConfirm = false; return; }
            var row = rows[r];
            if (!row.Enabled) { Game.Say("You cannot do that now."); return; }
            if (row.Id == "leave") { ui.Active = Panel.None; return; }
            if (Game.AltarAction(row.Id)) ui.Active = Panel.None;
        }

        void AdvanceKey(string code)
        {
            var ui = Game.UiState;
            var list = Progression.Available(Game.Player);
            int r = ListKey(code, ref ui.AdvanceIndex, list.Count, false);
            if (r == -2) ui.Active = Panel.None;
            else if (r >= 0)
            {
                Game.ApplyLevelAdvance(list[r].Id);
                if (Game.Player.PendingAdvances <= 0) ui.Active = Panel.None;
            }
        }

        // --------------------------------------------------------------- creation

        void CreateKey(string code, string key)
        {
            var c = Game.UiState.Create;
            bool enter = code == "Enter" || code == "NumpadEnter";
            bool up = code == "ArrowUp" || code == "Numpad8", down = code == "ArrowDown" || code == "Numpad2";
            switch (c.Step)
            {
                case CreateStep.Name:
                    if (code == "Escape") { Game.UiState.Active = Panel.None; ToTitle = true; AtTitle = true; }
                    else if (enter || code == "Tab") c.Step = CreateStep.Race;
                    else if (code == "Backspace") { if (c.Name.Length > 0) c.Name = c.Name.Substring(0, c.Name.Length - 1); }
                    else if (!string.IsNullOrEmpty(key) && key.Length == 1 && key[0] >= 32 && key[0] <= 126 && c.Name.Length < Heroes.MaxName)
                        c.Name += key;
                    break;
                case CreateStep.Race:
                    if (up || down) c.RaceIndex = Wrap(c.RaceIndex + (down ? 1 : -1), Races.All.Length);
                    else if (enter || code == "ArrowRight") c.Step = CreateStep.Role;
                    else if (code == "Escape" || code == "ArrowLeft") c.Step = CreateStep.Name;
                    break;
                case CreateStep.Role:
                    if (up || down) c.RoleIndex = Wrap(c.RoleIndex + (down ? 1 : -1), Roles.All.Length);
                    else if (enter) c.Step = CreateStep.Confirm;
                    else if (code == "Escape" || code == "ArrowLeft") c.Step = CreateStep.Race;
                    break;
                case CreateStep.Confirm:
                    if (enter)
                    {
                        int cols = Hud.Ui.Width, rows = Hud.Ui.Height;
                        Start(_seed, c.Name, c.RaceId, c.RoleId, false, true, null, c.Difficulty); Resize(cols, rows); Intro = true;
                    }
                    else if (code == "ArrowLeft" || code == "ArrowRight" || up || down)
                        c.Difficulty = Difficulties.All[Wrap(Array.IndexOf(Difficulties.All, c.Difficulty) + (code == "ArrowLeft" || up ? -1 : 1), Difficulties.All.Length)];
                    else if (code == "Escape") c.Step = CreateStep.Role;
                    break;
            }
        }

        // ------------------------------------------------------------------- menu

        void MenuKey(string code)
        {
            var ui = Game.UiState;
            var rows = MenuRows.All;
            if (code == "Escape" || code == "F2") { ui.Active = Panel.None; return; }
            ui.MenuNote = "";
            ui.SettingsIndex = Math.Clamp(ui.SettingsIndex, 0, rows.Length - 1);
            if (Input.Step(code, out int dx, out int dy))
            {
                if (dy != 0) ui.SettingsIndex = Wrap(ui.SettingsIndex + dy, rows.Length);
                else ChangeMenu(rows[ui.SettingsIndex], dx);
            }
            else if (Accept(code)) ActivateMenu(rows[ui.SettingsIndex]);
        }

        static void ChangeMenu(MenuRow row, int dir)
        {
            var settings = DisplaySettings.Current;
            switch (row)
            {
                case MenuRow.Theme: settings.CycleTheme(dir); break;
                case MenuRow.Crt: settings.CycleCrt(dir); break;
                case MenuRow.Scale: settings.CycleScale(dir); break;
                case MenuRow.Language: settings.CycleLanguage(); break;
                case MenuRow.Master: AudioSettings.Current.Change(0, dir); break;
                case MenuRow.Music: AudioSettings.Current.Change(1, dir); break;
                case MenuRow.Effects: AudioSettings.Current.Change(2, dir); break;
            }
        }

        void ActivateMenu(MenuRow row)
        {
            var ui = Game.UiState;
            switch (row)
            {
                case MenuRow.Resume: ui.Active = Panel.None; break;
                case MenuRow.Save:
                    { string error = Save(); ui.MenuNote = error == null ? Loc.T("Game saved.") : Loc.T("Could not save: " + error); break; }
                case MenuRow.MainMenu when AtTitle: ui.MenuNote = Loc.T("You are already here."); break;
                case MenuRow.Controls: ui.Active = Panel.Controls; ui.ControlsIndex = 0; ui.BindNote = ""; break;
                case MenuRow.PastRuns:
                    ui.Runs = SaveStore.ReadHistory(); ui.Runs.Reverse(); ui.RunsIndex = 0; ui.Active = Panel.Runs; break;
                case MenuRow.MainMenu: Save(true); ui.Active = Panel.None; ToTitle = true; AtTitle = true; break;
                case MenuRow.Quit: Save(true); ExitRequested = true; break;
                default: ChangeMenu(row, 1); break;
            }
        }

        // --------------------------------------------------------------- controls

        void RunsKey(string code)
        {
            var ui = Game.UiState;
            int n = ui.Runs.Count;
            if (code == "Escape" || code == "F2") { ui.Active = Panel.Settings; return; }
            if (n == 0) return;
            if (code == "PageDown") ui.RunsIndex = Math.Min(n - 1, ui.RunsIndex + 8);
            else if (code == "PageUp") ui.RunsIndex = Math.Max(0, ui.RunsIndex - 8);
            else if (code == "Home") ui.RunsIndex = 0;
            else if (code == "End") ui.RunsIndex = n - 1;
            else if (code == "ArrowUp" || code == "ArrowDown") ui.RunsIndex = Wrap(ui.RunsIndex + (code == "ArrowUp" ? -1 : 1), n);
        }

        void ControlsKey(string code, bool shift)
        {
            var ui = Game.UiState;
            int n = KeyBindings.Actions.Length;
            ui.ControlsIndex = Math.Clamp(ui.ControlsIndex, 0, n - 1);
            ui.BindNote = "";
            if (code == "Escape" || code == "F2") { ui.Active = Panel.Settings; return; }
            if (code == "PageDown") ui.ControlsIndex = Math.Min(n - 1, ui.ControlsIndex + 8);
            else if (code == "PageUp") ui.ControlsIndex = Math.Max(0, ui.ControlsIndex - 8);
            else if (code == "Home") ui.ControlsIndex = 0;
            else if (code == "End") ui.ControlsIndex = n - 1;
            else if (code == "ArrowUp" || code == "ArrowDown")
                ui.ControlsIndex = Wrap(ui.ControlsIndex + (code == "ArrowUp" ? -1 : 1), n);
            else if (Accept(code)) { ui.Rebinding = true; }
            else if (code == "Delete" || code == "Backspace")
            { KeyBindings.Current.Unbind(ui.ControlsIndex); ui.BindNote = "Cleared."; }
            else if (code == "KeyR")
            {
                if (shift) { KeyBindings.Current.ResetAll(); ui.BindNote = "All keys reset."; }
                else { KeyBindings.Current.Reset(ui.ControlsIndex); ui.BindNote = "Reset to default."; }
            }
        }

        void RebindKey(string code, bool shift, bool ctrl)
        {
            var ui = Game.UiState;
            if (code == "Escape") { ui.Rebinding = false; ui.BindNote = "Cancelled."; return; }
            string note = KeyBindings.Current.Rebind(ui.ControlsIndex, code, shift, ctrl, out bool ok);
            if (note == null) return;   // a lone modifier: keep waiting
            ui.Rebinding = false; ui.BindNote = note;
        }

        // ------------------------------------------------------------------ frame

        public Frame Draw()
        {
            RecordRun();
            Hud.Ui.TitleBackdrop = AtTitle;
            TextBuilder screen = Hud.Draw();
            int n = screen.Width * screen.Height;
            var glyphs = new int[n]; var fg = new int[n]; var bg = new int[n]; var bold = new bool[n];
            for (int y = 0, i = 0; y < screen.Height; y++)
                for (int x = 0; x < screen.Width; x++, i++)
                {
                    glyphs[i] = screen.CharAt(x, y); fg[i] = Pack(screen.ColorAt(x, y));
                    bg[i] = Pack(screen.BgAt(x, y)); bold[i] = screen.BoldAt(x, y);
                }
            var s = DisplaySettings.Current; var t = Theme.Current; var a = AudioSettings.Current;
            DisplaySettings.CrtParams(s.Crt, out float scanline, out float vignette, out float glow);
            return new Frame
            {
                Cols = screen.Width, Rows = screen.Height, Glyphs = glyphs, Fg = fg, Bg = bg, Bold = bold,
                Seed = Game.Rng.Seed.ToString(), Turn = Game.Turn, Mode = Game.Mode.ToString(),
                Panel = Game.UiState.Active.ToString(), Theme = (int)s.Preset, Crt = (int)s.Crt, Scale = s.Scale,
                Void = Pack(t.Void), Text = Pack(t.Text), Dim = Pack(t.Dim), Title = Pack(t.Title),
                Rule = Pack(t.Rule), PanelColor = Pack(t.Panel), Bad = Pack(t.Bad), Exit = ExitRequested,
                Scanline = scanline, Vignette = vignette, Glow = glow,
                Master = a.Master, Music = a.Music, Effects = a.Effects,
                Started = Started, ToTitle = ToTitle, HasSave = HasSave, SaveInfo = SaveInfo,
                Lang = Loc.Code(s.Language), Intro = Intro ? Story.Intro() : null,
            };
        }
        static int Pack(Rgb c) => (c.R << 16) | (c.G << 8) | c.B;
    }

    public sealed class Frame
    {
        public int Cols { get; set; }
        public int Rows { get; set; }
        public int[] Glyphs { get; set; }
        public int[] Fg { get; set; }
        public int[] Bg { get; set; }
        public bool[] Bold { get; set; }
        public string Seed { get; set; }
        public int Turn { get; set; }
        public string Mode { get; set; }
        public string Panel { get; set; }
        public int Theme { get; set; }
        public int Crt { get; set; }
        public int Scale { get; set; }
        public int Void { get; set; }
        public int Text { get; set; }
        public int Dim { get; set; }
        public int Title { get; set; }
        public int Rule { get; set; }
        public int PanelColor { get; set; }
        public int Bad { get; set; }
        public bool Exit { get; set; }
        public float Scanline { get; set; }
        public float Vignette { get; set; }
        public float Glow { get; set; }
        public int Master { get; set; }
        public int Music { get; set; }
        public int Effects { get; set; }
        public bool Started { get; set; }
        public bool ToTitle { get; set; }
        public bool HasSave { get; set; }
        public string SaveInfo { get; set; }
        public string Lang { get; set; }
        public string[][] Intro { get; set; }
    }
}
