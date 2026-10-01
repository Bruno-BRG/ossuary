using System;
using UnityEngine;
using Ossuary.Core;

namespace Ossuary.Gameplay
{
    /// <summary>
    /// The whole runtime: owns the simulation, the HUD, the camera and the
    /// terminal renderer, and routes keyboard input through InputRouter.
    ///
    /// The renderer is reached through exactly one method, <see cref="Present"/>,
    /// so if Ossuary.Render.TerminalRenderer ever changes shape this class needs
    /// one edit, not twenty.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        // ---------------------------------------------------------------- config

        /// <summary>Zero means "pick a random seed". Set it for reproducible tests.</summary>
        public ulong Seed;

        /// <summary>Target pixel height of one character cell. Smaller = more text.</summary>
        public int CellPixels = 18;

        /// <summary>Width/height ratio of one character cell: glyphs are about twice as tall as wide.</summary>
        public float CellAspect = 0.5f;

        public int MinCols = 40, MaxCols = 200;
        public int MinRows = 25, MaxRows = 120;

        // CRT ships OFF: scanlines + vignette blur an already dense glyph grid.
        // Opt in from the inspector once the text is sharp on your display.
        public bool CrtEnabled = false;
        public float ScanlineStrength = 0.35f;
        public float VignetteStrength = 0.55f;

        // ----------------------------------------------------------------- state

        public Ossuary.Core.Game Game { get; private set; }
        public GameHud Hud { get; private set; }

        Camera _cam;
        Ossuary.Render.TerminalRenderer _renderer;
        TextBuilder _screen;

        int _cols, _rows;
        bool _sizeApplied;
        bool _loggedCommandBug;

        // Quit confirmation: Ctrl-Q arms it, a second Ctrl-Q inside the window quits.
        bool _quitArmed;
        float _quitArmedAt;

        const float QuitConfirmSeconds = 3f;

        // --------------------------------------------------------------- bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindObjectOfType<GameApp>() != null) return;
            var go = new GameObject("Ossuary");
            go.AddComponent<GameApp>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;

            EnsureCamera();

            var child = new GameObject("Terminal");
            child.transform.SetParent(transform, false);
            _renderer = child.AddComponent<Ossuary.Render.TerminalRenderer>();

            BuildGame();

            ApplyTerminalSize(force: true);
            ApplyCrt();
        }

        void BuildGame()
        {
            ulong seed = Seed;
            if (seed == 0) seed = unchecked((ulong)(uint)Environment.TickCount);

            Game = new Ossuary.Core.Game(seed);
            Hud = new GameHud(Game);

            // The UI raises requests and Commands resolves them; Commands talks to
            // the UI through this back-reference, so the link has to be closed here.
            Hud.Ui.Cmd = Hud.Cmd;

            _screen = null;
            _sizeApplied = false;
            _quitArmed = false;
        }

        void EnsureCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("OssuaryCamera") { tag = "MainCamera" };
                _cam = go.AddComponent<Camera>();
                go.transform.SetParent(transform, false);
            }

            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _cam.orthographic = true;
            _cam.orthographicSize = 5f;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.transform.rotation = Quaternion.identity;
            _cam.nearClipPlane = 0.01f;
            _cam.farClipPlane = 100f;
            _cam.enabled = true;
        }

        void Start()
        {
            Debug.Log($"+----------------------------------------+\n" +
                      $"|  OSSUARY  seed {Game.Rng.Seed,-20} |\n" +
                      $"+----------------------------------------+");
            Present();
        }

        // ----------------------------------------------------------------- update

        void Update()
        {
            ApplyTerminalSize(force: false);
            HandleInput();
        }

        /// <summary>
        /// Cell sizing. A character cell is roughly CellPixels tall and
        /// CellPixels * CellAspect wide, so the grid that fits the window is
        ///
        ///     rows = Screen.height / CellPixels
        ///     cols = Screen.width  / (CellPixels * CellAspect)
        ///
        /// Integer division just leaves a few unused pixels at the right/bottom
        /// edge, which the renderer letterboxes away. Both are clamped so the UI
        /// never degenerates to a 3-column strip during a window resize, and so a
        /// huge 4K window does not ask the renderer for a 400-column atlas.
        /// </summary>
        void ApplyTerminalSize(bool force)
        {
            if (Hud == null) return;

            int cellH = Mathf.Max(4, CellPixels);
            int cellW = Mathf.Max(2, Mathf.RoundToInt(cellH * CellAspect));

            int cols = Mathf.Clamp(Screen.width / cellW, MinCols, MaxCols);
            int rows = Mathf.Clamp(Screen.height / cellH, MinRows, MaxRows);

            if (!force && _sizeApplied && cols == _cols && rows == _rows) return;

            _cols = cols;
            _rows = rows;
            _sizeApplied = true;

            Hud.Ui.Resize(cols, rows);
            _renderer.FitCamera(_cam, cols, rows);
        }

        void ApplyCrt()
        {
            _renderer.SetCrt(CrtEnabled ? ScanlineStrength : 0f, CrtEnabled ? VignetteStrength : 0f);
        }

        /// <summary>
        /// The only place the renderer is touched. Everything else in the app
        /// works on Ui state, so swapping the renderer means editing this method.
        /// </summary>
        void Present()
        {
            _screen = Hud.Draw();
            _renderer.Present(_screen);
        }

        /// <summary>The last drawn screen as plain text, for headless verification.</summary>
        public string ScreenAscii()
        {
            if (_screen == null) return "";
            return _screen.ToAscii();
        }

        // ------------------------------------------------------------------ input

        void HandleInput()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // Consume exactly one key per frame: read the whole mapped set, act on
            // the first press found, then stop. That keeps a held key from queuing
            // a burst of commands on the frame it repeats. Iterated as an array so
            // the steady state allocates nothing.
            var keys = InputRouter.Keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (!Input.GetKeyDown(keys[i])) continue;
                Dispatch(keys[i], shift, ctrl);
                return;
            }
        }

        void Dispatch(KeyCode key, bool shift, bool ctrl)
        {
            var ui = Game.UiState;

            // Priority 1: death or victory. Any key starts a fresh run.
            if (Game.Mode == GameMode.GameOver || Game.Mode == GameMode.Won ||
                ui.Active == Panel.Death || ui.Active == Panel.Win)
            {
                if (key == KeyCode.Escape)
                {
                    // Escape backs out to the editor instead of looping forever.
                    QuitApplication();
                    return;
                }
                BuildGame();
                ApplyTerminalSize(force: true);
                Present();
                return;
            }

            // Priority 2: a modal item choice blocks everything else.
            if (Game.PendingChoice.Active && Game.PendingChoice.Items.Count > 0)
            {
                HandleChoice(key);
                return;
            }

            // Priority 3: travel cursor, which swallows arrows so the player can
            // pick a destination without moving on the map.
            if (ui.Active == Panel.Travel || ui.TravelMode)
            {
                HandleTravel(key);
                return;
            }

            // Priority 4: targeting. The cursor eats movement keys so hjkl aims
            // rather than walks.
            if (ui.IsTargeting)
            {
                HandleTargeting(key);
                return;
            }

            // Priority 5: the shop list is modal too. A pending sell choice sits
            // on top of it and keeps priority (handled above), so reaching here
            // means no choice is open.
            if (ui.Active == Panel.Shop)
            {
                HandleShop(key);
                return;
            }

            // Priority 6: any other open panel. Escape, Enter or the same key
            // closes it; the panels themselves say "any key closes".
            if (ui.IsOpen)
            {
                string cmd = InputRouter.Translate(key, shift, ctrl);
                bool closes = key == KeyCode.Escape || key == KeyCode.Return ||
                              key == KeyCode.KeypadEnter || key == KeyCode.Space || cmd != null;
                if (closes)
                {
                    ui.Active = Panel.None;
                    Present();
                }
                return;
            }

            // Priority 7: ordinary play.
            string command = InputRouter.Translate(key, shift, ctrl);
            if (command == null) { Present(); return; }

            if (command == "quit")
            {
                if (!_quitArmed || Time.unscaledTime - _quitArmedAt > QuitConfirmSeconds)
                {
                    _quitArmed = true;
                    _quitArmedAt = Time.unscaledTime;
                    Game.Say($"Press Ctrl-Q again within {QuitConfirmSeconds:0}s to abandon the run.", MessageKind.Warn);
                    Present();
                    return;
                }
                _quitArmed = false;
            }

            RunCommand(command);
            Present();
        }

        void HandleChoice(KeyCode key)
        {
            var ui = Game.UiState;
            int n = Game.PendingChoice.Items.Count;

            if (InputRouter.TryStep(key, out int dx, out int dy))
            {
                int next = ui.ChoiceIndex;
                if (dx != 0) next = Wrap(next + dx, n);
                if (dy != 0) next = Wrap(next + dy, n);
                ui.ChoiceIndex = Mathf.Clamp(next, 0, n - 1);
                Present();
                return;
            }

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space)
            {
                int index = Mathf.Clamp(ui.ChoiceIndex, 0, n - 1);
                ui.ChoiceIndex = 0;
                var chosen = Game.PendingChoice.Items[index];
                // A sell picker raised inside a shop resolves as a sale, not as
                // wield/wear/eat. Everything else keeps the old behaviour.
                if (Game.InShop && Game.PendingChoice.Prompt == Ossuary.Core.Commands.SellPrompt)
                    TryRun(() => Hud.Cmd.CommitSellChoice(chosen), "sell");
                else
                    TryRun(() => Hud.Cmd.CommitChoice(chosen), "choose");
                Present();
                return;
            }

            if (key == KeyCode.Escape)
            {
                Game.PendingChoice.Clear();
                ui.ChoiceIndex = 0;
                Present();
            }
        }

        void HandleTravel(KeyCode key)
        {
            var ui = Game.UiState;
            ui.TravelMode = ui.Active == Panel.Travel;

            if (InputRouter.TryStep(key, out int dx, out int dy))
            {
                Game.TravelCursorTo(ui.TravelX + dx, ui.TravelY + dy);
                Present();
                return;
            }

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                ui.TravelMode = false;
                ui.Active = Panel.None;
                TryRun(() => Game.CommitTravel(ui.TravelX, ui.TravelY), "travel");
                Present();
                return;
            }

            if (key == KeyCode.Escape)
            {
                ui.TravelMode = false;
                ui.Active = Panel.None;
                Present();
            }
        }

        /// <summary>
        /// Modal shop input: hjkl/arrows move the stock cursor (via TryStep, like
        /// the other cursors), Enter or B buys the row, S opens the sell picker,
        /// Esc closes the shop. Every other key is swallowed so the modal holds.
        /// B is checked before TryStep because unshifted B is also "move-sw".
        /// </summary>
        void HandleShop(KeyCode key)
        {
            var ui = Game.UiState;

            if (key == KeyCode.Escape)
            {
                ui.Active = Panel.None;
                ui.ShopIndex = 0;
                if (Game.InShop) TryRun(() => Game.CloseShop(), "close shop");
                Present();
                return;
            }

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.B)
            {
                TryRun(() => Hud.Cmd.Execute("shop-buy"), "buy");
                if (!Game.InShop) ui.Active = Panel.None;
                Present();
                return;
            }

            if (key == KeyCode.S)
            {
                TryRun(() => Hud.Cmd.Execute("shop-sell"), "sell");
                Present();
                return;
            }

            if (InputRouter.TryStep(key, out int dx, out int dy))
            {
                int n = Game.CurrentShop != null ? Game.CurrentShop.Stock.Count : 0;
                // Vertical motion steps one row; horizontal jumps to the ends so
                // a long stock list stays navigable without paging keys.
                int next = ui.ShopIndex;
                if (dy != 0) next += dy;
                else if (dx > 0) next = n - 1;
                else if (dx < 0) next = 0;
                ui.ShopIndex = n <= 0 ? 0 : Mathf.Clamp(next, 0, n - 1);
                Present();
                return;
            }
        }

        void HandleTargeting(KeyCode key)
        {
            var ui = Game.UiState;

            if (InputRouter.TryStep(key, out int dx, out int dy))
            {
                Game.NudgeTarget(dx, dy);
                Present();
                return;
            }

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space)
            {
                int tx = ui.TargetX, ty = ui.TargetY;
                TryRun(() => Game.ResolveTargeting(tx, ty), "targeting");
                Present();
                return;
            }

            if (key == KeyCode.Escape)
            {
                ui.Targeting = TargetingMode.None;
                Present();
            }
        }

        static int Wrap(int value, int n) => n <= 0 ? 0 : ((value % n) + n) % n;

        // --------------------------------------------------------------- command

        /// <summary>
        /// Runs one command, converting a simulation bug into a message instead of
        /// a dead player. The exception is logged once so the real stack shows up
        /// in the console without spamming it every keystroke.
        /// </summary>
        void RunCommand(string command)
        {
            TryRun(() => Hud.Cmd.Execute(command), command);
        }

        void TryRun(Action action, string what)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (!_loggedCommandBug)
                {
                    _loggedCommandBug = true;
                    Debug.LogException(ex);
                }
                Game.Say($"Something goes wrong ({what}): {ex.Message}", MessageKind.Bad);
            }
        }

        void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ----------------------------------------------------------------- debug

        [ContextMenu("Log Keymap")]
        void LogKeymap()
        {
            foreach (string line in InputRouter.Describe()) Debug.Log(line);
        }
    }
}