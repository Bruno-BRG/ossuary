using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Draws the whole interface into a TextBuilder: header, map, sidebar, journal,
    /// status line and every modal panel ("Fósforo &amp; Osso", docs/tech/visual.md section 3.4).
    /// Pure C# so the layout can be asserted headlessly.
    ///
    /// Every colour comes from <see cref="Theme"/>; map cells go through Theme.Shade /
    /// Theme.Terrain so torch light, memory and day/night apply before a preset remap.
    /// </summary>
    public sealed class Ui
    {
        readonly Game _g;
        readonly TextBuilder _t;
        public int FacingX = 1, FacingY;
        public bool FirstDraw = true;

        /// <summary>Draw only the modal panels, undimmed, so a title screen can sit behind them.</summary>
        public bool TitleBackdrop;
        public string CenterTitle = "";
        public int CameraX, CameraY;
        /// <summary>Where the dungeon map sits on screen this frame (origin, size in columns/rows, columns per cell); lets effects be placed on it.</summary>
        public int MapOx, MapOy, MapViewW, MapViewH, MapSq = 1;
        public int MapX, MapY, MapW, MapH;

        /// <summary>Journal height including its rule row.</summary>
        const int LogRows = 5;

        /// <summary>Distance at which the torch no longer lights a cell.</summary>
        const float TorchRadius = 10f;

        public Ui(Game g)
        {
            _g = g;
            _t = new TextBuilder(120, 40);
        }

        public UiState State => _g.UiState;
        public UiRequests Requests => _g.UiRequests;
        public ChoiceRequest PendingChoice => _g.PendingChoice;
        public Commands Cmd { get; set; }

        public int Width => _t.Width;
        public int Height => _t.Height;

        public void Resize(int w, int h) => _t.Resize(w, h);

        bool OnOverworld => _g.Mode == GameMode.Overworld;

        public TextBuilder Draw()
        {
            DrainRequests();

            var theme = Theme.Current;
            int w = _t.Width, h = _t.Height;
            _t.Clear(theme.Void);
            if (TitleBackdrop) { DrawOverlays(false); FirstDraw = false; return _t; }

            int statusY = h - 2;
            int logTop = statusY - LogRows;
            int boxTop = 1;
            int boxH = Math.Max(4, logTop - boxTop);

            CenterTitle = TitleFor();
            DrawHeader(w, theme);

            int sidebarW = w >= 100 ? 32 : (w >= 84 ? 28 : 0);
            int frameW = w - sidebarW;
            _t.FillRect(0, boxTop, frameW, boxH, ' ', theme.Text, theme.Background);
            _t.RoundBox(0, boxTop, frameW, boxH, theme.Rule, false, theme.Background);
            EmbedTitle(0, boxTop, frameW, CenterTitle, theme, theme.Background, theme.Rule);
            DrawFrameCoords(frameW, boxTop + boxH - 1, theme);
            MapX = 1; MapY = boxTop + 1; MapW = Math.Max(1, frameW - 2); MapH = Math.Max(1, boxH - 2);

            if (OnOverworld) DrawOverworld(MapX, MapY, MapW, MapH);
            else DrawMap(MapX, MapY, MapW, MapH);
            if (sidebarW > 0) DrawSidebar(frameW, boxTop, sidebarW, boxH);

            DrawMessageWindow(0, logTop, w, LogRows);
            DrawStatusLine(0, statusY, w, theme);
            DrawShortcuts(0, h - 1, w, theme);
            DrawOverlays();

            FirstDraw = false;
            return _t;
        }

        // --------------------------------------------------------------- header

        void DrawHeader(int w, Theme theme)
        {
            _t.FillRect(0, 0, w, 1, ' ', theme.Text, theme.Panel);
            _t.Write(1, 0, " ♦ OSSUARY ", theme.Void, true, theme.Title);
            int x = 13;

            string place; string mood = null;
            switch (_g.Mode)
            {
                case GameMode.Overworld: place = "OVERWORLD"; break;
                case GameMode.TownMap: place = "TOWN" + (_g.TownZ != 0 ? " " + _g.TownFloorLabel() : ""); break;
                default:
                    place = "DEPTH " + _g.Player.CurrentDepth;
                    mood = Theme.DepthMood(_g.Player.CurrentDepth);
                    break;
            }
            _t.Write(x, 0, " " + place + " ", theme.Label, true, theme.PanelHi);
            x += place.Length + 3;
            if (mood != null) { _t.Write(x, 0, mood, theme.Dim, false, theme.Panel); x += mood.Length + 1; }

            string clock; Rgb sun = theme.Gold; char icon = '●';
            if (_g.World != null)
            {
                int hr = _g.World.Hour;
                if (hr >= 20 || hr < 5) { icon = '◐'; sun = theme.Info; }
                else if (hr >= 17 || hr < 7) { icon = '◑'; sun = theme.Warn; }
                clock = $"Day {_g.World.Day}  {hr:00}:00";
            }
            else clock = "";
            string turn = $"T {_g.Turn}";
            int rx = w - 2;
            rx -= turn.Length; _t.Write(rx, 0, turn, theme.Dim, false, theme.Panel);
            if (clock.Length > 0)
            {
                rx -= clock.Length + 3;
                _t.Put(rx, 0, icon, sun, true, theme.Panel);
                _t.Write(rx + 2, 0, clock, theme.Text, false, theme.Panel);
            }
        }

        // Row/column tag on the map frame's bottom edge: where the player stands.
        void DrawFrameCoords(int frameW, int y, Theme theme)
        {
            int px = OnOverworld ? _g.World.PlayerX : _g.Player.X, py = OnOverworld ? _g.World.PlayerY : _g.Player.Y;
            string c = $"{px},{py}";
            int x = frameW - c.Length - 6;
            if (x < 4) return;
            _t.Put(x, y, '╡', theme.Rule, false, theme.Background);
            _t.Put(x + 1, y, ' ', theme.Text, false, theme.Background);
            _t.Write(x + 2, y, c, theme.Dim, false, theme.Background);
            _t.Put(x + 2 + c.Length, y, ' ', theme.Text, false, theme.Background);
            _t.Put(x + 3 + c.Length, y, '╞', theme.Rule, false, theme.Background);
        }

        // A keycap: the key on a raised chip, then what it does.
        int KeyCap(int x, int y, string key, string label, Theme theme)
        {
            _t.Write(x, y, " " + key + " ", theme.Title, true, theme.Rule);
            x += key.Length + 3;
            _t.Write(x, y, " " + label, theme.Text, false, theme.PanelHi);
            return x + label.Length + 3;
        }

        void DrawShortcuts(int ox, int oy, int w, Theme theme)
        {
            _t.FillRect(ox, oy, w, 1, ' ', theme.Dim, theme.PanelHi);
            string[,] keys = OnOverworld && _g.ActiveEncounter
                ? new[,] { { "Enter", "attack" }, { "R", "flee" }, { "i", "pack" }, { "c", "character" }, { "?", "help" } }
                : _g.Mode == GameMode.TownMap
                ? new[,] { { "arrows", "move" }, { "bump", "talk" }, { ">", "descend" }, { "<", "climb" }, { "i", "pack" }, { "c", "character" }, { "?", "help" }, { "F2", "options" } }
                : OnOverworld
                ? new[,] { { "arrows", "move" }, { "O", "travel" }, { "i", "pack" }, { "c", "character" }, { "H", "log" }, { "?", "help" }, { "F2", "options" } }
                : new[,] { { "arrows", "move" }, { "g", "get" }, { "i", "pack" }, { "c", "character" }, { ">", "descend" }, { "<", "climb" }, { "s", "search" }, { "?", "help" }, { "F2", "options" } };
            int x = ox + 1;
            for (int i = 0; i < keys.GetLength(0); i++)
            {
                string key = Loc.T(keys[i, 0]), label = Loc.T(keys[i, 1]);
                if (x + key.Length + label.Length + 5 > ox + w - 1) break;
                x = KeyCap(x, oy, key, label, theme);
            }
        }

        void DrainRequests()
        {
            if (Requests.Inventory) { State.Active = Panel.Inventory; State.ScrollOffset = 0; }
            if (Requests.Help) { State.Active = Panel.Help; State.ScrollOffset = 0; }
            if (Requests.HelpLong) { State.Active = Panel.Help; State.ScrollOffset = 8; }
            if (Requests.History) { State.Active = Panel.History; State.ScrollOffset = 0; }
            if (Requests.Discoveries) { State.Active = Panel.Discoveries; State.ScrollOffset = 0; }
            if (Requests.Journal) { State.Active = Panel.Journal; State.ScrollOffset = 0; }
            if (Requests.Character) { State.Active = Panel.Character; State.ScrollOffset = 0; }
            if (Requests.Travel) { State.Active = Panel.Travel; State.ScrollOffset = 0; }
            if (Requests.Altar) { State.Active = Panel.Altar; State.AltarIndex = 0; }
            if (Requests.Abilities) { State.Active = Panel.Abilities; State.AbilityIndex = 0; }
            if (Requests.Advance) { State.Active = Panel.Advance; State.AdvanceIndex = 0; }
            if (Requests.Spells) { State.Active = Panel.Spells; State.SpellIndex = 0; }
            if (Requests.Settings) { State.Active = Panel.Settings; State.SettingsIndex = 0; State.MenuNote = ""; }
            Requests.Clear();
        }

        // ----------------------------------------------------------------- map

        void DrawMap(int ox, int oy, int w, int h)
        {
            var map = _g.Map;
            if (map == null) return;
            var theme = Theme.Current;
            var p = _g.Player;

            // Square tiles use two screen columns per map cell, so the window holds half as many cells across.
            int sq = DisplaySettings.Current.Square ? 2 : 1;
            int cellsW = Math.Max(1, w / sq);
            // A map smaller than the window sits centred (negative camera offset) instead of
            // hugging the top-left corner; cells outside the map are simply skipped.
            int cx = map.W <= cellsW ? -((cellsW - map.W) / 2) : Math.Max(0, Math.Min(p.X - cellsW / 2, map.W - cellsW));
            int cy = map.H <= h ? -((h - map.H) / 2) : Math.Max(0, Math.Min(p.Y - h / 2, map.H - h));
            CameraX = cx; CameraY = cy;
            MapOx = ox; MapOy = oy; MapViewW = w; MapViewH = h; MapSq = sq;

            // Towns are daylit outside the night hours; everything else is torchlit.
            bool daylight = _g.Mode == GameMode.TownMap && _g.World != null && !_g.World.IsNight;
            bool inDungeon = _g.Mode == GameMode.Dungeon;
            bool sense = p.BuffTurns("sense") > 0 || (p.Amulet != null && p.Amulet.Def.Name == "amulet of ESP");
            Theme.DepthTint(p.CurrentDepth, out Rgb tintFg, out Rgb tintBg, out float tintAmt);

            for (int y = 0; y < h; y++)
            {
                int my = cy + y;
                for (int x = 0; x < cellsW; x++)
                {
                    int mx = cx + x;
                    if (!map.InBounds(mx, my)) continue;
                    bool vis = map.IsVisible(mx, my);
                    bool seen = map.WasSeen(mx, my);
                    if (!vis && !seen)
                    {
                        // Unexplored rock is not dead black: a sparse, faint grain keeps the
                        // screen alive and shows that the dark is solid, not missing.
                        float fog = Theme.Hash01(mx * 3 + 1, my * 7 + 2);
                        if (fog > 0.88f)
                            PutTile(sq, ox + x * sq, oy + y, fog > 0.96f ? '▒' : '·', Rgb.Lerp(theme.Background, theme.Rule, fog > 0.96f ? 0.16f : 0.30f), false, theme.Background, false);
                        continue;
                    }

                    TileKind t = vis ? map.Get(mx, my) : map.Remembered(mx, my);
                    char g = Tiles.Get(t).Glyph;
                    theme.TileColors(t, out Rgb fg, out Rgb bg);
                    bool bold = vis && Tiles.IsStairs(t);

                    // Texture: variants and tone jitter come from hash(x, y) only, so the
                    // dungeon never shimmers and the simulation's RNG is not touched.
                    float hv = Theme.Hash01(mx, my);
                    // A secret door is drawn as the plain wall it hides in, texture included.
                    TileKind vt = t == TileKind.HiddenDoor ? TileKind.Wall : t;
                    bool stone = vt == TileKind.Floor || vt == TileKind.FloorAlt || vt == TileKind.Wall
                              || vt == TileKind.WallAlt || vt == TileKind.WallDark || vt == TileKind.Rubble;
                    if (stone)
                    {
                        if (inDungeon) { fg = Rgb.Lerp(fg, tintFg, tintAmt); bg = Rgb.Lerp(bg, tintBg, tintAmt * 1.4f); }
                        fg = fg * (0.88f + 0.24f * hv);
                        bg = bg * (0.92f + 0.16f * Theme.Hash01(my, mx + 17));
                        if (vt == TileKind.Floor) g = hv < 0.58f ? '·' : hv < 0.80f ? '.' : hv < 0.93f ? '·' : ',';
                        else if (vt == TileKind.FloorAlt) g = hv < 0.7f ? '·' : hv < 0.88f ? ',' : '`';
                        else if (vt == TileKind.Wall && hv > 0.90f) g = '▒';
                        else if (vt == TileKind.WallDark) g = hv < 0.7f ? '#' : '▓';
                    }

                    bool water = false;
                    if (t == TileKind.Floor || t == TileKind.FloorAlt)
                    {
                        var sk = map.SurfaceAt(mx, my);
                        water = sk == SurfaceKind.Water && vis;
                        if (sk != SurfaceKind.None && (vis || sk != SurfaceKind.Fire))
                        {
                            theme.SurfaceStyle(sk, mx, my, _g.Turn, out char sg, out Rgb sfg, out Rgb sbg);
                            g = sg; fg = sfg; bg = sbg; bold = sk == SurfaceKind.Fire;
                        }
                    }

                    if ((t == TileKind.Floor || t == TileKind.FloorAlt) && TrapTable.IsRevealed(map.Number, mx, my))
                    {
                        g = '^'; fg = theme.Warn; bold = true;
                    }

                    bool entity = false;
                    if (vis)
                    {
                        var item = TopItemAt(mx, my);
                        if (item != null)
                        {
                            Rgb ic = theme.ItemRaw(item);
                            g = item.Def.Glyph; fg = ic; bold = true; entity = true;
                            bg = Rgb.Lerp(bg, ic * 0.35f, 0.45f);
                        }

                        var m = _g.MonsterAt(mx, my);
                        if (m != null)
                        {
                            Rgb mc = Theme.Mon(m.Def.Color);
                            g = m.Glyph; fg = mc; bold = true; entity = true;
                            bg = Rgb.Lerp(bg, mc * 0.32f, 0.60f);
                        }
                    }
                    else if (sense && Math.Abs(mx - p.X) <= 12 && Math.Abs(my - p.Y) <= 12)
                    {
                        // Sense Life and the amulet of ESP: living things nearby show through the walls, dimly.
                        var sm = _g.MonsterAt(mx, my);
                        if (sm != null && !sm.Dormant)
                        {
                            Rgb mc = Theme.Mon(sm.Def.Color);
                            g = sm.Glyph; fg = mc; bold = true; entity = true;
                            bg = Rgb.Lerp(bg, mc * 0.2f, 0.4f);
                        }
                    }

                    float light = 1f;
                    if (vis && !daylight)
                    {
                        float dx = mx - p.X, dy = my - p.Y;
                        float fall = 1f - Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) / TorchRadius);
                        light = (float)Math.Pow(fall, 1.35);
                    }
                    else if (vis) light = 0.75f;
                    theme.Shade(fg, bg, light, !vis, out Rgb ofg, out Rgb obg);
                    PutTile(sq, ox + x * sq, oy + y, g, ofg, bold, obg, entity);
                    if (water && g != '@' && _g.MonsterAt(mx, my) == null) { _t.Shimmer(ox + x * sq, oy + y); if (sq == 2) _t.Shimmer(ox + x * sq + 1, oy + y); }
                }
            }

            int px = ox + (p.X - cx) * sq, py = oy + (p.Y - cy);
            if (px >= ox && py >= oy && px < ox + w && py < oy + h)
            {
                Rgb pbg = theme.Remap(Rgb.FromHex(0x6A4220), true);
                _t.Put(px, py, '@', theme.Accent, true, pbg);
                if (sq == 2) _t.Put(px + 1, py, ' ', theme.Accent, false, pbg);
            }

            DrawCursors(ox, oy, w, h, cx, cy, theme, sq);
        }

        void DrawCursors(int ox, int oy, int w, int h, int cx, int cy, Theme theme, int sq = 1)
        {
            if (State.IsTargeting) PutCursor(ox + (State.TargetX - cx) * sq, oy + (State.TargetY - cy), ox, oy, w, h, '◎', theme);
            if (State.Active == Panel.Travel || State.TravelMode)
                PutCursor(ox + (State.TravelX - cx) * sq, oy + (State.TravelY - cy), ox, oy, w, h, '◊', theme);
        }

        /// <summary>One map cell: one screen column, or two when tiles are square. Scenery repeats across both, things stand on the left.</summary>
        void PutTile(int sq, int sx, int sy, char g, Rgb fg, bool bold, Rgb bg, bool entity)
        {
            _t.Put(sx, sy, g, fg, bold, bg);
            if (sq == 2) _t.Put(sx + 1, sy, entity ? ' ' : g, fg, bold && !entity, bg);
        }

        // The cursor keeps the cell's own background so the terrain stays readable under it.
        void PutCursor(int sx, int sy, int ox, int oy, int w, int h, char glyph, Theme theme)
        {
            if (sx < ox || sy < oy || sx >= ox + w || sy >= oy + h) return;
            _t.Put(sx, sy, glyph, theme.Cursor, true, _t.BgAt(sx, sy));
        }

        Item TopItemAt(int x, int y)
        {
            var stack = GroundItems.At(_g.Map.Number, x, y);
            if (stack == null || stack.Count == 0) return null;
            return stack[stack.Count - 1];
        }

        // ------------------------------------------------------------- overworld

        void DrawOverworld(int ox, int oy, int w, int h)
        {
            var world = _g.World;
            var theme = Theme.Current;
            int sq = DisplaySettings.Current.Square ? 2 : 1;
            int cellsW = Math.Max(1, w / sq);
            int cx = world.W <= cellsW ? -((cellsW - world.W) / 2) : Math.Max(0, Math.Min(world.PlayerX - cellsW / 2, world.W - cellsW));
            int cy = world.H <= h ? -((h - world.H) / 2) : Math.Max(0, Math.Min(world.PlayerY - h / 2, world.H - h));
            CameraX = cx; CameraY = cy;

            for (int y = 0; y < h; y++)
            {
                int my = cy + y;
                for (int x = 0; x < cellsW; x++)
                {
                    int mx = cx + x;
                    if (!world.InBounds(mx, my)) continue;
                    var t = world.Get(mx, my);
                    if (!t.Discovered)
                    {
                        float fog = Theme.Hash01(mx * 3 + 1, my * 7 + 2);
                        if (fog > 0.9f) PutTile(sq, ox + x * sq, oy + y, '·', Rgb.Lerp(theme.Background, theme.Rule, 0.30f), false, theme.Background, false);
                        continue;
                    }
                    theme.Terrain(t.Terrain, t.Feature, mx, my, world.Hour, out char g, out Rgb fg, out Rgb bg);
                    PutTile(sq, ox + x * sq, oy + y, g, fg, t.Feature != OverworldFeature.None, bg, t.Feature != OverworldFeature.None);
                }
            }

            int px = ox + (world.PlayerX - cx) * sq, py = oy + (world.PlayerY - cy);
            _t.Put(px, py, '@', theme.Accent, true, _t.BgAt(px, py));
            if (sq == 2) _t.Put(px + 1, py, ' ', theme.Accent, false, _t.BgAt(px, py));

            if (_g.ActiveEncounter && _g.EncounterMonster != null)
            {
                int ex = ox + (_g.EncounterX - cx) * sq, ey = oy + (_g.EncounterY - cy);
                if (ex >= ox && ey >= oy && ex < ox + w && ey < oy + h)
                {
                    var mon = _g.EncounterMonster;
                    _t.Put(ex, ey, mon.Glyph, theme.Remap(Theme.Mon(mon.Def.Color), false), true, _t.BgAt(ex, ey));
                    if (sq == 2) _t.Put(ex + 1, ey, ' ', theme.Text, false, _t.BgAt(ex, ey));
                    if (ey - 1 >= oy) _t.Put(ex, ey - 1, '▼', theme.Warn, true, _t.BgAt(ex, ey - 1));
                }
            }

            DrawCursors(ox, oy, w, h, cx, cy, theme, sq);
        }

        // -------------------------------------------------------------- sidebar

        void DrawSidebar(int ox, int oy, int w, int h)
        {
            var theme = Theme.Current;
            var p = _g.Player;
            _t.FillRect(ox, oy, w, h, ' ', theme.Text, theme.Panel);
            _t.DoubleBox(ox, oy, w, h, theme.Frame, false, theme.Panel);

            string title = OnOverworld ? _g.World.CurrentRegionName.ToUpperInvariant()
                         : Roles.Find(p.RoleId).Name.ToUpperInvariant();
            EmbedTitle(ox, oy, w, title, theme);

            int x = ox + 2;
            int iw = w - 4;
            int y = oy + 2;
            int last = oy + h - 2;

            string lv = $"Lv {p.Level}";
            _t.Write(x, y, lv, theme.Void, true, theme.Label);
            string xp = $"{p.Xp}/{p.XpNext}";
            _t.Write(x + iw - xp.Length, y, xp, theme.Dim, false, theme.Panel);
            int xbarX = x + lv.Length + 1, xbarW = iw - lv.Length - 1 - xp.Length - 1;
            if (xbarW >= 3) Bar(xbarX, y, xbarW, p.XpNext > 0 ? (double)p.Xp / p.XpNext : 0, theme.Magic, theme);
            y += 2;

            double hpFrac = p.MaxHP > 0 ? (double)p.HP / p.MaxHP : 0;
            StatBar(x, y++, iw, "HP", $"{p.HP}/{p.MaxHP}", hpFrac, theme.Fraction(hpFrac), theme);
            double enFrac = p.EnergyMax > 0 ? (double)p.Energy / p.EnergyMax : 0;
            StatBar(x, y++, iw, "EN", $"{p.Energy}/{p.EnergyMax}", enFrac, theme.Info, theme);
            if (p.Abilities.Count > 0) StatBar(x, y++, iw, "VG", $"{p.Vigor}/{p.VigorMax}", (double)p.Vigor / p.VigorMax, theme.Gold, theme);
            if (p.MpMax > 0) StatBar(x, y++, iw, "MP", $"{p.Mp}/{p.MpMax}", (double)p.Mp / p.MpMax, theme.Magic, theme);

            // Sections, in priority order; each one drops out when the column is full.
            // A section's own rule line is its separator, so there are no blank rows
            // between them: the column is short and the minimap needs the room.
            if (OnOverworld) y = SidebarEncounter(x, y, iw, last, theme);
            else y = SidebarNearby(x, y, iw, last, theme);

            y = SidebarWorn(x, y, iw, last, theme);
            y = SidebarVitals(x, y, iw, last, theme);
            if (OnOverworld) y = SidebarLandmarks(x, y, iw, last, theme);

            if (!OnOverworld) y = SidebarInView(x, y, iw, last, theme);

            if (!OnOverworld && State.ShowMinimap && _g.Map != null && last - y >= 6)
            {
                y = Section(x, y, iw, "Map", theme);
                DrawMinimap(x, y, iw, Math.Min(last - y + 1, 12), theme);
            }
        }

        // "╡ TITLE ╞" cut into a frame's top edge.
        void EmbedTitle(int ox, int oy, int w, string title, Theme theme, Rgb? bgOpt = null, Rgb? edgeOpt = null)
        {
            Rgb bg = bgOpt ?? theme.Panel, edge = edgeOpt ?? theme.Frame;
            int max = Math.Max(0, w - 8);
            if (title.Length > max) title = title.Substring(0, max);
            _t.Put(ox + 2, oy, '╡', edge, false, bg);
            _t.Put(ox + 3, oy, ' ', theme.Text, false, bg);
            _t.Write(ox + 4, oy, title, theme.Title, true, bg);
            _t.Put(ox + 4 + title.Length, oy, ' ', theme.Text, false, bg);
            _t.Put(ox + 5 + title.Length, oy, '╞', edge, false, bg);
        }

        // "─ NAME ──────────": returns the next free row.
        int Section(int x, int y, int iw, string name, Theme theme)
        {
            _t.HLine(x, y, iw, theme.Rule);
            _t.Write(x + 2, y, " " + name.ToUpperInvariant() + " ", theme.Label, false, theme.Panel);
            return y + 1;
        }

        void StatBar(int x, int y, int iw, string label, string nums, double frac, Rgb color, Theme theme)
        {
            bool low = label == "HP" && frac < 0.30;
            _t.Write(x, y, label, low ? theme.Danger : theme.Label, low, theme.Panel);
            _t.Write(x + iw - nums.Length, y, nums, low ? theme.Danger : theme.Text, low, theme.Panel);
            int barW = Math.Max(1, iw - 3 - nums.Length - 1);
            Bar(x + 3, y, barW, frac, color, theme);
        }

        // Solid bar in half-cell steps: full cells █, the boundary cell ▌ on a dark trough.
        void Bar(int x, int y, int w, double frac, Rgb color, Theme theme)
        {
            frac = Math.Max(0, Math.Min(1, frac));
            int half = (int)Math.Round(frac * w * 2);
            if (frac > 0 && half == 0) half = 1;
            Rgb trough = Rgb.Lerp(theme.Panel, theme.BarEmpty, 0.55f);
            for (int i = 0; i < w; i++)
            {
                int cell = half - i * 2;
                if (cell >= 2) _t.Put(x + i, y, '█', color, false, theme.Panel);
                else if (cell == 1) _t.Put(x + i, y, '▌', color, false, trough);
                else _t.Put(x + i, y, ' ', trough, false, trough);
            }
        }

        int SidebarNearby(int x, int y, int iw, int last, Theme theme)
        {
            var near = new List<Monster>();
            var p = _g.Player;
            for (int i = 0; i < _g.Monsters.Count; i++)
            {
                var m = _g.Monsters[i];
                if (_g.Map != null && _g.Map.IsVisible(m.X, m.Y)) near.Add(m);
            }
            if (near.Count == 0 || y + 3 > last) return y;

            near.Sort((a, b) => Cheb(a.X - p.X, a.Y - p.Y).CompareTo(Cheb(b.X - p.X, b.Y - p.Y)));
            y = Section(x, y, iw, "Nearby", theme);
            int rows = Math.Min(Math.Min(near.Count, 5), last - y - 2);
            for (int i = 0; i < rows; i++, y++)
            {
                var m = near[i];
                Rgb mc = Theme.Mon(m.Def.Color);
                _t.Put(x, y, m.Glyph, theme.Remap(mc, false), true, theme.Remap(mc * 0.3f, true));
                double f = m.MaxHP > 0 ? (double)m.HP / m.MaxHP : 0;
                bool adjacent = Cheb(m.X - p.X, m.Y - p.Y) <= 1;
                if (m.Townsperson)
                {
                    _t.WriteClipped(x + 2, y, m.Name + (m.Role == TownRole.Pet ? "" : " · " + Loc.U(TownText.RoleTitle(m.Role))), adjacent ? theme.Info : theme.Dim, iw - 2, adjacent, theme.Panel);
                    continue;
                }
                _t.WriteClipped(x + 2, y, m.Name, adjacent ? theme.Danger : theme.Text, iw - 2 - 7, adjacent, theme.Panel);
                Bar(x + iw - 6, y, 6, f, theme.Fraction(f), theme);
            }
            if (near.Count > rows && y <= last) { _t.Write(x + 2, y, Loc.T($"+{near.Count - rows} more"), theme.Dim, false, theme.Panel); y++; }
            return y;
        }

        int SidebarEncounter(int x, int y, int iw, int last, Theme theme)
        {
            if (y + 3 > last) return y;
            y = Section(x, y, iw, "Ahead", theme);
            if (_g.ActiveEncounter && _g.EncounterMonster != null)
            {
                var m = _g.EncounterMonster;
                Rgb mc = Theme.Mon(m.Def.Color);
                _t.Put(x, y, m.Glyph, theme.Remap(mc, false), true, theme.Remap(mc * 0.3f, true));
                _t.WriteClipped(x + 2, y, m.Name, theme.Warn, iw - 2, true, theme.Panel);
                y++;
            }
            else
            {
                _t.Write(x, y++, "The road is quiet.", theme.Dim, false, theme.Panel);
            }
            return y;
        }

        int SidebarWorn(int x, int y, int iw, int last, Theme theme)
        {
            var p = _g.Player;
            int pieces = 0; foreach (var _ in p.WornPieces()) pieces++;
            int need = 3 + (pieces > 0 ? pieces - 1 : 0) + (p.Rings[0] != null ? 1 : 0) + (p.Rings[1] != null ? 1 : 0) + (p.Amulet != null ? 1 : 0);
            if (y + need > last) return y;
            y = Section(x, y, iw, "Worn", theme);
            if (p.Wielded != null)
            {
                _t.Put(x, y, p.Wielded.Def.Glyph, theme.ItemColor(p.Wielded), true, theme.Panel);
                _t.WriteClipped(x + 2, y++, p.Wielded.Name, theme.Text, iw - 2, false, theme.Panel);
            }
            else _t.Write(x, y++, "  bare hands", theme.Dim, false, theme.Panel);

            bool anyPiece = false;
            foreach (var piece in p.WornPieces())
            {
                anyPiece = true;
                _t.Put(x, y, '[', theme.ItemColor(piece), true, theme.Panel);
                _t.WriteClipped(x + 2, y++, piece.Name, theme.Text, iw - 2, false, theme.Panel);
            }
            if (!anyPiece) _t.Write(x, y++, "  no armour", theme.Dim, false, theme.Panel);

            for (int i = 0; i < 2; i++)
            {
                if (p.Rings[i] == null) continue;
                _t.Put(x, y, '=', theme.ItemColor(p.Rings[i]), true, theme.Panel);
                _t.WriteClipped(x + 2, y++, p.Rings[i].Name, theme.Text, iw - 2, false, theme.Panel);
            }
            if (p.Amulet != null)
            {
                _t.Put(x, y, '"', theme.ItemColor(p.Amulet), true, theme.Panel);
                _t.WriteClipped(x + 2, y++, p.Amulet.Name, theme.Text, iw - 2, false, theme.Panel);
            }
            return y;
        }

        int SidebarVitals(int x, int y, int iw, int last, Theme theme)
        {
            var p = _g.Player;
            if (y + 4 > last) return y;
            y = Section(x, y, iw, "Vitals", theme);
            _t.Write(x, y, "AC", theme.Label, false, theme.Panel);
            _t.Write(x + 3, y, p.ArmorClass().ToString(), theme.Text, true, theme.Panel);
            _t.Write(x + 9, y, "Evade", theme.Label, false, theme.Panel);
            _t.Write(x + 15, y++, p.Evasion().ToString(), theme.Text, true, theme.Panel);

            if (y + 3 <= last)
            {
                int ax = x;
                void Attr(string n, int v)
                {
                    _t.Write(ax, y, n, theme.Dim, false, theme.Panel);
                    string vs = v.ToString();
                    _t.Write(ax + 4, y, vs, v >= 16 ? theme.Good : v <= 8 ? theme.Bad : theme.Text, false, theme.Panel);
                    ax += 4 + Math.Max(3, vs.Length + 1);
                }
                Attr("Str", p.Str); Attr("Dex", p.Dex); Attr("Con", p.Con);
                y++;
                ax = x;
                Attr("Int", p.Int); Attr("Wis", p.Wis); Attr("Cha", p.Cha);
                y++;
            }

            _t.Write(x, y, "$", theme.Gold, true, theme.Panel);
            _t.Write(x + 2, y, _g.CarryingGold().ToString(), theme.Gold, false, theme.Panel);
            string hunger = FoodWord(p.FoodNutrient, theme, out Rgb hc);
            _t.Write(x + iw - hunger.Length, y++, hunger, hc, false, theme.Panel);

            if (y <= last && (p.Confused || p.Blinded || p.Stunned || p.Hallucinating || p.BurnTurns > 0 || p.WetTurns > 0 || p.Buffs.Count > 0 || p.Wounds.Count > 0))
            {
                int sx = x;
                void Pill(string label, Rgb c) { if (sx + label.Length + 2 > x + iw) return; _t.Write(sx, y, " " + label + " ", theme.Void, true, c); sx += label.Length + 3; }
                if (p.Confused) Pill("CONFUSED", theme.Warn);
                if (p.Blinded) Pill("BLIND", theme.Warn);
                if (p.Stunned) Pill("STUNNED", theme.Warn);
                if (p.Hallucinating) Pill("TRIPPING", theme.Magic);
                if (p.BurnTurns > 0) Pill("BURNING", theme.Danger);
                if (p.WetTurns > 0) Pill("WET", theme.Info);
                if (Bodies.Bleeding(p)) Pill("BLEEDING", theme.Danger);
                int limp = Bodies.Limp(p);
                if (limp > 0) Pill(limp == 1 ? "LIMPING" : "CRAWLING", theme.Warn);
                else if (p.Wounds.Count > 0 && !Bodies.Bleeding(p)) Pill("WOUNDED", theme.Warn);
                if (p.Aim != null) Pill(Loc.T("AIM") + " " + Loc.T(p.Aim.Value.ToString().ToUpperInvariant()), theme.Info);
                foreach (var kv in p.Buffs) Pill(Spells.BuffLabel(kv.Key).ToUpperInvariant(), theme.Info);
                y++;
            }
            return y;
        }

        // A living legend: the notable things in sight (features and items), nearest first,
        // each with the glyph and colour the map uses for it.
        int SidebarInView(int x, int y, int iw, int last, Theme theme)
        {
            var map = _g.Map;
            if (map == null || y + 3 > last) return y;
            var p = _g.Player;
            var found = new List<KeyValuePair<int, string>>();
            var seen = new HashSet<string>();
            const int reach = 12;
            var glyphs = new Dictionary<string, KeyValuePair<char, Rgb>>();
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int mx = p.X + dx, my = p.Y + dy;
                    if (!map.InBounds(mx, my) || !map.IsVisible(mx, my) || (dx == 0 && dy == 0)) continue;
                    var item = TopItemAt(mx, my);
                    string key; char g; Rgb c;
                    if (item != null) { key = item.Name; g = item.Def.Glyph; c = theme.ItemColor(item); }
                    else
                    {
                        var t = map.Get(mx, my);
                        bool notable = Tiles.IsStairs(t) || Tiles.IsDoor(t) || t == TileKind.Fountain
                                    || t == TileKind.Altar || t == TileKind.Portal || t == TileKind.Pillar;
                        if (!notable || Tiles.IsSecret(t)) continue;
                        theme.TileColors(t, out Rgb fg, out Rgb _);
                        key = Tiles.Get(t).Name; g = Tiles.Get(t).Glyph; c = theme.Remap(fg, false);
                    }
                    if (!seen.Add(key)) continue;
                    glyphs[key] = new KeyValuePair<char, Rgb>(g == ' ' ? '?' : g, c);
                    found.Add(new KeyValuePair<int, string>(Cheb(dx, dy), key));
                }
            if (found.Count == 0) return y;
            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            y = Section(x, y, iw, "In view", theme);
            int rows = Math.Min(Math.Min(found.Count, 5), last - y - 1);
            for (int i = 0; i < rows; i++, y++)
            {
                var gc = glyphs[found[i].Value];
                _t.Put(x, y, gc.Key, gc.Value, true, theme.Panel);
                _t.WriteClipped(x + 2, y, found[i].Value, theme.Text, iw - 2 - 3, false, theme.Panel);
                string d = found[i].Key + "";
                _t.Write(x + iw - d.Length, y, d, theme.Dim, false, theme.Panel);
            }
            return y;
        }

        static string FoodWord(int nutrient, Theme theme, out Rgb color)
        {
            if (nutrient < 20) { color = theme.Danger; return "Starving"; }
            if (nutrient < 150) { color = theme.Warn; return "Hungry"; }
            color = theme.Good; return "Fed";
        }

        int SidebarLandmarks(int x, int y, int iw, int last, Theme theme)
        {
            var world = _g.World;
            if (y + 3 > last) return y;

            var found = new List<KeyValuePair<int, OverworldTile>>();
            const int reach = 16;
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int tx = world.PlayerX + dx, ty = world.PlayerY + dy;
                    if (!world.InBounds(tx, ty) || (dx == 0 && dy == 0)) continue;
                    var t = world.Get(tx, ty);
                    if (!t.Discovered || t.Feature == OverworldFeature.None || t.Feature == OverworldFeature.Bridge) continue;
                    found.Add(new KeyValuePair<int, OverworldTile>(Cheb(dx, dy), t));
                }
            if (found.Count == 0) return y;

            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            y = Section(x, y, iw, "Landmarks", theme);
            int rows = Math.Min(found.Count, last - y + 1);
            for (int i = 0; i < rows; i++, y++)
            {
                var t = found[i].Value;
                theme.Terrain(t.Terrain, t.Feature, 0, 0, 12, out char g, out Rgb fg, out Rgb _);
                _t.Put(x, y, g, fg, true, theme.Panel);
                string name = string.IsNullOrEmpty(t.Name) ? t.Feature.ToString() : t.Name;
                string dist = found[i].Key + "";
                _t.WriteClipped(x + 2, y, name, theme.Text, iw - 2 - dist.Length - 1, false, theme.Panel);
                _t.Write(x + iw - dist.Length, y, dist, theme.Dim, false, theme.Panel);
            }
            return y;
        }

        static int Cheb(int dx, int dy) => Math.Max(Math.Abs(dx), Math.Abs(dy));

        void DrawMinimap(int ox, int oy, int w, int h, Theme theme)
        {
            var map = _g.Map;
            if (map == null || h < 3) return;
            // Frame first: the interior is the only drawable area.
            _t.RoundBox(ox, oy, w, h, theme.Rule, false, theme.Background);
            int iw = w - 2, ih = h - 2;
            if (iw <= 0 || ih <= 0) return;
            int stepX = Math.Max(1, (map.W + iw - 1) / iw);
            int stepY = Math.Max(1, (map.H + ih - 1) / ih);
            Rgb wall = theme.Rule;
            Rgb floor = theme.Dim;
            for (int y = 0; y < ih; y++)
            {
                for (int x = 0; x < iw; x++)
                {
                    char g = ' '; Rgb c = floor;
                    // A minimap cell stands for a block of tiles: show the most telling one.
                    for (int sy = 0; sy < stepY; sy++)
                        for (int sx = 0; sx < stepX; sx++)
                        {
                            int mx = x * stepX + sx, my = y * stepY + sy;
                            if (!map.InBounds(mx, my) || !map.WasSeen(mx, my)) continue;
                            var t = map.Get(mx, my);
                            char tg; Rgb tc;
                            if (Tiles.IsStairs(t)) { tg = '>'; tc = theme.Accent; }
                            else if (Tiles.IsDoor(t)) { tg = '+'; tc = theme.Warn; }
                            else if (Tiles.Walkable(t)) { tg = '·'; tc = floor; }
                            else { tg = '▒'; tc = Rgb.Lerp(theme.Rule, theme.Dim, 0.35f); }
                            if (Rank(tg) >= Rank(g)) { g = tg; c = tc; }
                        }
                    foreach (var mon in _g.Monsters)
                        if (_g.Map.IsVisible(mon.X, mon.Y) && mon.X / stepX == x && mon.Y / stepY == y) { g = '•'; c = mon.Ally ? theme.Good : theme.Danger; }
                    int px = _g.Player.X / stepX, py = _g.Player.Y / stepY;
                    if (x == px && y == py) { g = '■'; c = theme.Accent; }
                    if (g != ' ') _t.Put(ox + 1 + x, oy + 1 + y, g, c, false, theme.Background);
                }
            }
        }

        static int Rank(char g) => g == '>' ? 4 : g == '+' ? 3 : g == '·' ? 2 : g == '▒' ? 1 : 0;

        // ----------------------------------------------------------- status bar

        void DrawStatusLine(int ox, int oy, int w, Theme theme)
        {
            _t.FillRect(ox, oy, w, 1, ' ', theme.Text, theme.PanelHi);
            Rgb bg = theme.PanelHi;
            int x = ox + 1;
            int limit = ox + w;

            void Field(string label, string value, Rgb c, bool bold = false)
            {
                if (x >= limit - 4) return;
                if (label != null) x = _t.WriteClipped(x, oy, label + " ", theme.Dim, limit - x, false, bg);
                x = _t.WriteClipped(x, oy, value, c, limit - x, bold, bg);
                if (x < limit - 3) { _t.Put(x + 1, oy, '│', theme.Rule, false, bg); x += 3; }
            }

            var p = _g.Player;
            Field("Here", HereLabel(out Rgb hc), hc, true);
            int cap = Math.Max(1, p.CarryingCapacity()), load = p.WeightCarried();
            Field("Load", $"{load}/{cap}", p.IsOverloaded() ? theme.Bad : load * 10 > cap * 7 ? theme.Warn : theme.Text);
            Field("Kills", p.Kills.ToString(), theme.Text);

            string danger = DangerLabel();
            if (danger.Length > 0 && ox + w - 3 - danger.Length > x)
                _t.Write(ox + w - 2 - danger.Length, oy, " " + danger + " ", theme.Void, true, theme.Danger);
        }

        // What is underfoot, as a short phrase for the status bar.
        string HereLabel(out Rgb color)
        {
            var theme = Theme.Current;
            color = theme.Text;
            if (OnOverworld)
            {
                var tile = _g.World.Get(_g.World.PlayerX, _g.World.PlayerY);
                string name = tile.Feature != OverworldFeature.None
                    ? (string.IsNullOrEmpty(tile.Name) ? tile.Feature.ToString() : tile.Name)
                    : tile.Terrain.ToString();
                if (tile.Feature != OverworldFeature.None) color = theme.Accent;
                return name;
            }
            if (_g.Map == null) return "-";
            var item = TopItemAt(_g.Player.X, _g.Player.Y);
            if (item != null) { color = theme.ItemColor(item); return item.Name; }
            var t = _g.Map.Get(_g.Player.X, _g.Player.Y);
            if (Tiles.IsStairs(t)) color = theme.Accent;
            else if (t == TileKind.Fountain || t == TileKind.Altar) color = theme.Quest;
            else if (t == TileKind.Portal) color = theme.Magic;
            return Tiles.Get(t).Name;
        }

        string DangerLabel()
        {
            int worst = 0;
            for (int i = 0; i < _g.Monsters.Count; i++)
            {
                var m = _g.Monsters[i];
                if (_g.Map == null || !_g.Map.IsVisible(m.X, m.Y)) continue;
                if (!m.Ally) worst = Math.Max(worst, m.Def.Level);
            }
            if (worst >= 12) return "[DEADLY]";
            if (worst >= 8) return "[DANGEROUS]";
            if (worst >= 4) return "[WARY]";
            return "";
        }

        // ------------------------------------------------------- message window

        struct LogLine
        {
            public string Text; public MessageKind Kind; public int Turn; public int Count;
        }

        static char MarkerFor(MessageKind k)
        {
            switch (k)
            {
                case MessageKind.Good: return '+';
                case MessageKind.Bad: case MessageKind.Warn: return '!';
                case MessageKind.Combat: return '»';
                case MessageKind.Kill: return '×';
                case MessageKind.Death: return '☻';
                case MessageKind.Narrative: return '◇';
                case MessageKind.Quest: return '◆';
                case MessageKind.Info: return '•';
                default: return '·';
            }
        }

        void DrawMessageWindow(int ox, int oy, int w, int h)
        {
            var theme = Theme.Current;
            _t.FillRect(ox, oy, w, h, ' ', theme.Text, theme.Panel);
            _t.HLine(ox, oy, w, theme.Rule);
            _t.Put(ox + 2, oy, '╡', theme.Rule, false, theme.Panel);
            _t.Write(ox + 4, oy, "JOURNAL", theme.Title, true, theme.Panel);
            _t.Put(ox + 12, oy, '╞', theme.Rule, false, theme.Panel);
            const string hint = " H history ";
            if (w > 40) _t.Write(ox + w - hint.Length - 2, oy, hint, theme.Dim, false, theme.Panel);

            int count = h - 1;
            // Collapse runs of the same message into "text (x3)", newest last.
            var lines = new List<LogLine>(count);
            for (int i = _g.Log.Count - 1; i >= 0 && lines.Count < count; i--)
            {
                var m = _g.Log[i];
                int rep = 1;
                while (i - 1 >= 0 && _g.Log[i - 1].Text == m.Text) { rep++; i--; }
                lines.Add(new LogLine { Text = m.Text, Kind = m.Kind, Turn = m.Turn, Count = rep });
            }
            lines.Reverse();

            var names = VisibleMonsterColours(theme);
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                bool fresh = l.Turn >= _g.Turn - 1;
                Rgb baseC = theme.MessageColor(l.Kind);
                // Older lines fade, so the eye lands on the newest.
                int age = lines.Count - 1 - i;
                if (!fresh) baseC = Rgb.Lerp(baseC, theme.Dim, Math.Min(0.8f, 0.45f + 0.12f * age));
                int x = ox + 1, y = oy + 1 + i;
                _t.Put(x, y, MarkerFor(l.Kind), fresh ? theme.MessageColor(l.Kind) : theme.Rule, fresh, theme.Panel);
                x = WriteRich(x + 2, y, l.Text, baseC, w - 4, names, fresh, theme);
                if (l.Count > 1) _t.Write(x + 1, y, $"(x{l.Count})", theme.Dim, false, theme.Panel);
            }
        }

        // Visible monster names mapped to their colour, so "the kobold" in the log reads
        // in the kobold's own colour.
        List<KeyValuePair<string, Rgb>> VisibleMonsterColours(Theme theme)
        {
            var list = new List<KeyValuePair<string, Rgb>>();
            if (_g.Map == null) return list;
            for (int i = 0; i < _g.Monsters.Count; i++)
            {
                var m = _g.Monsters[i];
                if (!_g.Map.IsVisible(m.X, m.Y) || string.IsNullOrEmpty(m.Name)) continue;
                bool dup = false;
                for (int k = 0; k < list.Count; k++) if (list[k].Key == m.Name) { dup = true; break; }
                if (!dup) list.Add(new KeyValuePair<string, Rgb>(m.Name, theme.Remap(Theme.Mon(m.Def.Color), false)));
            }
            return list;
        }

        // Writes text with monster names and numbers picked out. Returns the next free x.
        int WriteRich(int x, int y, string text, Rgb baseC, int maxW, List<KeyValuePair<string, Rgb>> names, bool bright, Theme theme)
        {
            int n = Math.Min(text.Length, maxW);
            var cols = new Rgb[n];
            for (int i = 0; i < n; i++) cols[i] = baseC;

            for (int k = 0; k < names.Count; k++)
            {
                int at = 0;
                while ((at = text.IndexOf(names[k].Key, at, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    for (int i = at; i < at + names[k].Key.Length && i < n; i++) cols[i] = bright ? names[k].Value : Rgb.Lerp(names[k].Value, theme.Dim, 0.5f);
                    at += names[k].Key.Length;
                }
            }

            if (bright)
            {
                for (int i = 0; i < n; i++)
                {
                    bool digit = text[i] >= '0' && text[i] <= '9';
                    bool word = i > 0 && char.IsLetter(text[i - 1]);
                    if (digit && !word) cols[i] = theme.Accent;
                }
            }

            for (int i = 0; i < n; i++) _t.Put(x + i, y, text[i], cols[i], false, theme.Panel);
            return x + n;
        }

        // --------------------------------------------------------------- panels

        void DrawOverlays(bool dim = true)
        {
            bool modal = State.Active != Panel.None || PendingChoice.Active
                         || _g.Mode == GameMode.GameOver || _g.Mode == GameMode.Won;
            // Travel is a thin banner over a live map; everything else pauses the screen.
            if (dim && modal && State.Active != Panel.Travel)
            {
                var theme = Theme.Current;
                _t.DimAll(0.55f, theme.Remap);
            }

            switch (State.Active)
            {
                case Panel.Inventory: DrawInventoryPanel(); break;
                case Panel.Help: DrawHelpPanel(); break;
                case Panel.History: DrawHistoryPanel(); break;
                case Panel.Discoveries: DrawDiscoveriesPanel(); break;
                case Panel.Journal: DrawJournalPanel(); break;
                case Panel.Character: DrawCharacterPanel(); break;
                case Panel.Travel: DrawTravelPanel(); break;
                case Panel.Shop: DrawShopPanel(); break;
                case Panel.Settings: DrawSettingsPanel(); break;
                case Panel.Controls: DrawControlsPanel(); break;
                case Panel.Runs: DrawRunsPanel(); break;
                case Panel.Achievements: DrawAchievementsPanel(); break;
                case Panel.Create: DrawCreatePanel(); break;
                case Panel.Spells: DrawSpellsPanel(); break;
                case Panel.Abilities: DrawAbilitiesPanel(); break;
                case Panel.Altar: DrawAltarPanel(); break;
                case Panel.Service: DrawServicePanel(); break;
                case Panel.Advance: DrawAdvancePanel(); break;
            }
            if (PendingChoice.Active) DrawChoicePanel();
            if (_g.Mode == GameMode.GameOver) DrawDeathPanel();
            if (_g.Mode == GameMode.Won) DrawWinPanel();
        }

        /// <summary>
        /// A centred modal: double frame with the title cut into the top edge and an
        /// optional hint in the bottom edge. Content starts two rows below py.
        /// </summary>
        void PanelRect(out int px, out int py, out int pw, out int ph, int wantW, int wantH, string title, string hint = null)
        {
            var theme = Theme.Current;
            title = Loc.U(title);
            if (hint != null) hint = Loc.U(hint);
            pw = Math.Min(wantW, _t.Width - 2);
            ph = Math.Min(wantH, _t.Height - 2);
            px = (_t.Width - pw) / 2;
            py = (_t.Height - ph) / 2;
            // Drop shadow: one cell right and one below, darkened in place.
            for (int i = 1; i <= ph; i++) _t.Darken(px + pw, py + i, 0.65f);
            for (int i = 1; i <= pw; i++) _t.Darken(px + i, py + ph, 0.65f);
            _t.FillRect(px, py, pw, ph, ' ', theme.Text, theme.Panel);
            _t.DoubleBox(px, py, pw, ph, theme.Frame, false, theme.Panel);
            EmbedTitle(px, py, pw, "◆ " + title.ToUpperInvariant() + " ◆", theme);
            if (hint != null && hint.Length + 6 < pw)
            {
                int hx = px + pw - hint.Length - 5;
                _t.Put(hx, py + ph - 1, '╡', theme.Frame, false, theme.Panel);
                _t.Put(hx + 1, py + ph - 1, ' ', theme.Text, false, theme.Panel);
                _t.Write(hx + 2, py + ph - 1, hint, theme.Dim, false, theme.Panel);
                _t.Put(hx + 2 + hint.Length, py + ph - 1, ' ', theme.Text, false, theme.Panel);
                _t.Put(hx + 3 + hint.Length, py + ph - 1, '╞', theme.Frame, false, theme.Panel);
            }
        }

        // Selected-row highlight used by every list panel.
        void RowBar(int x, int y, int w, Theme theme) => _t.FillRect(x, y, w, 1, ' ', theme.Text, theme.PanelHi);

        static readonly ItemKind[] KindOrder =
        {
            ItemKind.Weapon, ItemKind.Armor, ItemKind.Shield, ItemKind.Helm, ItemKind.Gloves, ItemKind.Boots, ItemKind.Cloak, ItemKind.Ring, ItemKind.Amulet,
            ItemKind.Ammo, ItemKind.Wand, ItemKind.Scroll, ItemKind.Potion, ItemKind.Food, ItemKind.Tool,
            ItemKind.Material, ItemKind.Rock, ItemKind.Book, ItemKind.Gem, ItemKind.Ornament, ItemKind.Statuette, ItemKind.Container, ItemKind.Corpse, ItemKind.Gold,
        };

        static string KindName(ItemKind k)
        {
            switch (k)
            {
                case ItemKind.Weapon: return "Weapons";
                case ItemKind.Armor: case ItemKind.Shield: case ItemKind.Helm: case ItemKind.Gloves: case ItemKind.Boots: case ItemKind.Cloak: return "Armour";
                case ItemKind.Ring: case ItemKind.Amulet: return "Jewellery";
                case ItemKind.Wand: return "Wands";
                case ItemKind.Scroll: return "Scrolls";
                case ItemKind.Potion: return "Potions";
                case ItemKind.Food: return "Food";
                case ItemKind.Tool: return "Tools";
                case ItemKind.Ammo: return "Ammunition";
                case ItemKind.Material: case ItemKind.Rock: return "Goods";
                case ItemKind.Book: return "Books";
                default: return "Other";
            }
        }

        void DrawInventoryPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            PanelRect(out int px, out int py, out int pw, out int ph, 70, 24, "Inventory", "any key closes");

            int x = px + 3, y0 = py + 2;
            int rows = ph - 4;
            int leftW = Math.Min(40, pw - 30);
            int rx = x + leftW + 3, rw = pw - 6 - leftW - 3;

            // Left column: items grouped by kind, kinds in a fixed order.
            int y = y0, shown = 0, listed = 0;
            string lastGroup = null;
            foreach (var kind in KindOrder)
            {
                for (int i = 0; i < p.Inventory.Count; i++)
                {
                    var it = p.Inventory[i];
                    if (it.Def.Kind != kind) continue;
                    string group = KindName(kind);
                    if (group != lastGroup)
                    {
                        if (y - y0 + 2 > rows) goto listDone;
                        if (lastGroup != null) y++;
                        _t.Write(x, y++, group, theme.Label, false, theme.Panel);
                        lastGroup = group;
                    }
                    if (y - y0 + 1 > rows) goto listDone;

                    string name = it.Name;
                    if (it.Quantity > 1) name += $" (x{it.Quantity})";
                    if (it.RemainingCharges > 0 && kind == ItemKind.Wand)
                        name += $" [{it.RemainingCharges}]";
                    Rgb c = (it.Def.Flags & ItemFlags.Cursed) != 0 ? theme.Bad
                          : (it.Def.Flags & ItemFlags.Blessed) != 0 ? theme.Good
                          : theme.ItemColor(it);
                    _t.Put(x + 1, y, it.Def.Glyph == ' ' ? '?' : it.Def.Glyph, c, true, theme.Panel);
                    _t.WriteClipped(x + 3, y, name, theme.Text, leftW - 3, false, theme.Panel);
                    y++; shown++;
                }
            }
        listDone:
            listed = shown;
            if (p.Inventory.Count == 0) _t.Write(x, y0, "You are carrying nothing.", theme.Dim, false, theme.Panel);
            else if (listed < p.Inventory.Count) _t.Write(x, py + ph - 2, Loc.T($"...and {p.Inventory.Count - listed} more"), theme.Dim, false, theme.Panel);

            // Divider and right column: what is worn, and how heavy the pack is.
            for (int yy = y0; yy < py + ph - 2; yy++) _t.Put(rx - 2, yy, '│', theme.Rule, false, theme.Panel);
            int ry = y0;
            _t.Write(rx, ry++, "Equipped", theme.Label, false, theme.Panel);
            ry = EquipLine(rx, ry, rw, p.Wielded, "bare hands", theme);
            bool anyWorn = false;
            foreach (var piece in p.WornPieces()) { anyWorn = true; ry = EquipLine(rx, ry, rw, piece, "", theme); }
            if (!anyWorn) ry = EquipLine(rx, ry, rw, null, "no armour", theme);
            for (int i = 0; i < 2; i++) if (p.Rings[i] != null) ry = EquipLine(rx, ry, rw, p.Rings[i], "", theme);
            if (p.Amulet != null) ry = EquipLine(rx, ry, rw, p.Amulet, "", theme);
            ry++;
            _t.Write(rx, ry++, "Carrying", theme.Label, false, theme.Panel);
            int wt = p.WeightCarried(), cap = Math.Max(1, p.CarryingCapacity());
            double wf = Math.Min(1.0, (double)wt / cap);
            _t.Write(rx, ry, $"{wt}/{cap}", p.IsOverloaded() ? theme.Bad : theme.Text, false, theme.Panel);
            Bar(rx + 8, ry++, Math.Max(4, rw - 8), wf, wf > 0.85 ? theme.Danger : wf > 0.6 ? theme.Warn : theme.Good, theme);
            ry++;
            _t.Write(rx, ry, "$", theme.Gold, true, theme.Panel);
            _t.Write(rx + 2, ry++, _g.CarryingGold().ToString(), theme.Gold, false, theme.Panel);
            string hunger = FoodWord(p.FoodNutrient, theme, out Rgb hc);
            _t.Write(rx, ry, hunger, hc, false, theme.Panel);
        }

        int EquipLine(int x, int y, int w, Item it, string empty, Theme theme)
        {
            if (it == null)
            {
                if (empty.Length > 0) _t.Write(x, y++, empty, theme.Dim, false, theme.Panel);
                return y;
            }
            _t.Put(x, y, it.Def.Glyph, theme.ItemColor(it), true, theme.Panel);
            _t.WriteClipped(x + 2, y, it.Name, theme.Text, w - 2, false, theme.Panel);
            return y + 1;
        }

        void DrawChoicePanel()
        {
            var theme = Theme.Current;
            int n = PendingChoice.Items.Count;
            PanelRect(out int px, out int py, out int pw, out int ph, 56, n + 5, PendingChoice.Prompt ?? "Choose", "enter picks");

            int visRows = ph - 4;
            int start = Math.Max(0, State.ChoiceIndex - visRows + 1);
            for (int i = 0; i < visRows; i++)
            {
                int idx = start + i;
                if (idx >= n) break;
                var it = PendingChoice.Items[idx];
                bool sel = idx == State.ChoiceIndex;
                int y = py + 2 + i;
                if (sel) RowBar(px + 1, y, pw - 2, theme);
                Rgb bg = sel ? theme.PanelHi : theme.Panel;
                _t.Put(px + 2, y, sel ? '▶' : ' ', theme.Accent, true, bg);
                _t.Put(px + 4, y, it.Def.Glyph == ' ' ? '?' : it.Def.Glyph, theme.ItemColor(it), true, bg);
                _t.WriteClipped(px + 6, y, it.Name, sel ? theme.Accent : theme.Text, pw - 8, sel, bg);
            }
        }

        void DrawHelpPanel()
        {
            var theme = Theme.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 76, 28, "Commands", "any key closes");

            string[,] rows = {
                { "hjklyubn", "move (vi keys, arrows, numpad)" },
                { "y u", "diagonals" },
                { ".", "wait one turn" },
                { ">  <", "descend / climb (stairs in towns too)" },
                { ">  <", "Shift + . and Shift + , on any keyboard" },
                { "bump", "talk to a person, trade at a counter (town)" },
                { "Enter K  R", "road: fight / flee a monster in your way" },
                { "g  d", "pick up / drop" },
                { "i", "inventory" },
                { "a", "apply a tool (pick-axe, lock pick, an instrument)" },
                { "w  W  T", "wield / wear / take off armour" },
                { "P  R", "put on / remove a ring" },
                { "r  z  Z", "read a scroll or book / zap a wand / cast a spell" },
                { "V  C", "use an ability / spend advancements (Shift)" },
                { "e  Shift+Q", "eat / drink a potion" },
                { "f", "fire at a target" },
                { "k", "kick or attack ahead" },
                { "u  D", "use a key / open a door" },
                { "s  Shift+S", "search for traps and doors / rest until healed" },
                { "Shift+A", "disarm a trap you have found" },
                { "Shift+E", "drink at a fountain (it may be tainted)" },
                { "Shift+B", "craft: make what your trades, pack and place allow" },
                { "Shift+I", "examine an item: material, maker, wear and worth" },
                { "Shift+J", "every recipe, by trade and rank" },
                { "Shift+G", "gather: butcher a carcass, or forage on the road" },
                { "Shift+N", "train a skill with XP (Trained mode)" },
                { "t  `  ~", "auto-explore / travel to the stairs / to an altar or fountain" },
                { "l  x  X", "look / inspect / swap with" },
                { "O", "travel on the overworld" },
                { "m", "toggle the minimap" },
                { "c  F6", "character sheet / discoveries" },
                { "H", "message history" },
                { "F2 F3 F4", "options / CRT strength / colour theme" },
                { "?", "this help (on ABNT2: AltGr + W)" },
            };

            int x = px + 3, y = py + 2;
            int maxRows = ph - 4;
            for (int i = 0; i < maxRows; i++)
            {
                int line = i + State.ScrollOffset;
                if (line >= rows.GetLength(0)) break;
                _t.WriteClipped(x, y + i, rows[line, 0], theme.Title, 11, false, theme.Panel);
                _t.WriteClipped(x + 12, y + i, rows[line, 1], theme.Text, pw - 18, false, theme.Panel);
            }
        }

        void DrawHistoryPanel()
        {
            var theme = Theme.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 80, 24, "Message history", "any key closes");
            int y = py + 2, rows = ph - 4;
            int start = Math.Max(0, _g.Transcript.Count - rows - State.ScrollOffset);
            for (int i = 0; i < rows; i++)
            {
                int idx = start + i;
                if (idx >= _g.Transcript.Count) break;
                _t.WriteClipped(px + 3, y + i, _g.Transcript[idx].Text, theme.MessageColor(_g.Transcript[idx].Kind), pw - 6, false, theme.Panel);
            }
        }

        void KeyValue(int x, int y, string key, string value, Rgb valueColor, Theme theme)
        {
            _t.Write(x, y, key, theme.Label, false, theme.Panel);
            _t.Write(x + 18, y, value, valueColor, false, theme.Panel);
        }

        void DrawDiscoveriesPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            PanelRect(out int px, out int py, out int pw, out int ph, 54, 14, "Discoveries", "any key closes");
            int y = py + 2, x = px + 3;
            KeyValue(x, y++, "Deepest level", p.MaxDepth.ToString(), theme.Text, theme);
            KeyValue(x, y++, "Kills", p.Kills.ToString(), theme.Text, theme);
            KeyValue(x, y++, "Gold carried", _g.CarryingGold().ToString(), theme.Gold, theme);
            KeyValue(x, y++, "Levels mapped", _g.Dungeon.LevelCount.ToString(), theme.Text, theme);
            KeyValue(x, y++, "Regions seen", _g.RegionsSeen().ToString(), theme.Text, theme);
            KeyValue(x, y++, "Seed", _g.Rng.Seed.ToString(), theme.Dim, theme);
        }

        /// <summary>Every quest by track: where you stand in it, where to go, how long you have. Guild jobs sit under their track too.</summary>
        void DrawJournalPanel()
        {
            var theme = Theme.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 70, 26, "Journal", "any key closes");
            int x = px + 3, iw = pw - 6, y = py + 2, bottom = py + ph - 2;
            int today = _g.World != null ? _g.World.Day : 0;
            bool any = false;
            int wanted = _g.World != null ? _g.BountyHere() : 0;
            if (wanted > 0) { any = true; _t.WriteClipped(x, y++, Loc.T($"Wanted: {wanted} gold"), theme.Bad, iw, true, theme.Panel); y++; }
            var today_ = _g.Mode == GameMode.TownMap ? _g.TownEventToday() : TownEventKind.None;
            if (today_ != TownEventKind.None) { any = true; _t.Write(x, y, "Today", theme.Label, true, theme.Panel); _t.WriteClipped(x + 8, y++, Game.EventTitle(today_), theme.Quest, iw - 8, true, theme.Panel); y++; }
            foreach (string track in new[] { QuestDef.Main, QuestDef.Guild, QuestDef.Watch, QuestDef.Temple, QuestDef.Cult, QuestDef.Personal, QuestDef.Rival, QuestDef.Region })
            {
                var open = new System.Collections.Generic.List<QuestState>();
                foreach (var q in _g.Quests) if (q.Def.Track == track && q.Status == QStatus.Active) open.Add(q);
                bool jobs = track == QuestDef.Guild && _g.Contracts.Count > 0;
                if (open.Count == 0 && !jobs) continue;
                any = true;
                if (y >= bottom) break;
                _t.Write(x, y++, track, theme.Label, true, theme.Panel);
                foreach (var q in open)
                {
                    if (y >= bottom) break;
                    int left = q.DaysLeft(today);
                    _t.WriteClipped(x + 2, y++, q.Def.Title + (left >= 0 ? $"  ({left}d)" : ""), theme.Title, iw - 2, true, theme.Panel);
                    var s = q.Current;
                    if (s != null && y < bottom)
                    {
                        string prog = s.Kind == ObjKind.Kill && s.Count > 1 ? $" {q.Progress}/{s.Count}" : "";
                        _t.WriteClipped(x + 4, y++, s.Text + prog, theme.Text, iw - 4, false, theme.Panel);
                        if (s.Hint != null && y < bottom) _t.WriteClipped(x + 4, y++, s.Hint, theme.Dim, iw - 4, false, theme.Panel);
                    }
                }
                if (jobs)
                    foreach (var c in _g.Contracts)
                        if (y < bottom) _t.WriteClipped(x + 2, y++, $"{c.Describe()}  {System.Math.Min(c.Done, c.Count)}/{c.Count}", c.Complete ? theme.Good : theme.Text, iw - 2, false, theme.Panel);
                y++;
            }
            if (_g.LearnedRumours.Count > 0 && y < bottom - 1)
            {
                any = true;
                _t.Write(x, y++, "Rumours", theme.Label, true, theme.Panel);
                for (int i = _g.LearnedRumours.Count - 1, shown = 0; i >= 0 && shown < 4 && y < bottom; i--, shown++)
                    _t.WriteClipped(x + 2, y++, _g.LearnedRumours[i].Text, theme.Dim, iw - 2, false, theme.Panel);
            }
            if (_g.PriceHistory.Count > 0 && y < bottom - 1)
            {
                any = true;
                _t.Write(x, y++, "Markets", theme.Label, true, theme.Panel);
                for (int i = _g.PriceHistory.Count - 1, shown = 0; i >= 0 && shown < 5 && y < bottom; i--, shown++)
                {
                    var n = _g.PriceHistory[i];
                    var col = n.Pct >= 110 ? theme.Good : n.Pct <= 85 ? theme.Bad : theme.Text;
                    _t.WriteClipped(x + 2, y++, Loc.T($"{n.Town}: {n.Class} sell at {n.Pct}% (day {n.Day})"), col, iw - 2, false, theme.Panel);
                }
            }
            if (!any) _t.WriteClipped(x, y, "No open quests. Speak to the people of the town.", theme.Dim, iw, false, theme.Panel);
            int done = 0, failed = 0;
            foreach (var q in _g.Quests) { if (q.Status == QStatus.Done) done++; else if (q.Status == QStatus.Failed) failed++; }
            if (done + failed > 0) _t.WriteClipped(x, py + ph - 2, Loc.T($"Done {done}   Failed {failed}"), theme.Dim, iw, false, theme.Panel);
        }

        void DrawCharacterPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            var role = Roles.Find(p.RoleId);
            PanelRect(out int px, out int py, out int pw, out int ph, 62, 28, "Character", "any key closes");
            int y = py + 2, x = px + 3, iw = pw - 6;
            _t.WriteClipped(x, y++, $"{p.CharName}, the {Races.Find(p.RaceId).Name} {role.Name}", theme.Title, iw, true, theme.Panel);
            string mp = p.MpMax > 0 ? $"   MP {p.Mp}/{p.MpMax}" : "";
            _t.WriteClipped(x, y++, $"Level {p.Level}   HP {p.HP}/{p.MaxHP}{mp}   AC {p.ArmorClass()}   XP {p.XpForNext()}", theme.Dim, iw, false, theme.Panel); y++;
            _t.WriteClipped(x, y++, role.Description, theme.Text, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, p.AttributeLine(), theme.Text, iw, false, theme.Panel); y++;
            _t.WriteClipped(x, y++, $"Alignment {p.AlignmentString}", theme.Dim, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, $"Title {p.Title}", theme.Text, iw, false, theme.Panel);
            if (p.PendingAdvances > 0)
                _t.WriteClipped(x, y++, $"{p.PendingAdvances} advancement(s) unspent", theme.Good, iw, true, theme.Panel);
            if (p.Perks.Count > 0)
            {
                var taken = new System.Collections.Generic.List<string>();
                foreach (var kv in p.Perks) { var d = Progression.Find(kv.Key); if (d != null) taken.Add(d.MaxRank > 1 ? d.Name + " " + kv.Value : d.Name); }
                _t.WriteClipped(x, y++, "Perks: " + string.Join(", ", taken.ToArray()), theme.Dim, iw, false, theme.Panel);
            }
            y++;
            _t.Write(x, y++, "Skills", theme.Label, false, theme.Panel);
            if (p.Trained) _t.WriteClipped(x + 20, y - 1, $"XP to spend: {p.TrainXp}", theme.Gold, iw - 20, true, theme.Panel);
            foreach (var kv in p.Skills)
            {
                int cap = role.CapFor(kv.Key);
                string capText = cap < 100 ? $"  (max {cap})" : "";
                _t.Write(x + 2, y++, $"{Loc.T(kv.Key.ToString()),-10}{kv.Value,3}  {Loc.T(SkillRanks.Name(kv.Value)),-8}{Loc.T(capText)}", theme.Text, false, theme.Panel);
            }
            {
                var trades = new System.Collections.Generic.List<string>();
                foreach (var t in Items.Trades.All)
                    if (p.TradeXp.TryGetValue(t.Id, out int xp) && xp > 0) trades.Add($"{Loc.T(t.Title)} {xp} ({Loc.T(Items.Trades.RankNames[Items.Trades.Rank(xp)])})");
                if (trades.Count > 0) _t.WriteClipped(x, y++, Loc.T("Trades") + ": " + string.Join(", ", trades.ToArray()), theme.Info, iw, false, theme.Panel);
            }
            {
                var houses = new System.Collections.Generic.List<string>();
                foreach (string house in Houses.All) { int r = _g.RepOf(house); if (r != 0) houses.Add($"{Loc.T(Houses.Name(house))} {r:+#;-#;0}"); }
                if (houses.Count > 0) _t.WriteClipped(x, y++, Loc.T("Standing") + ": " + string.Join(", ", houses.ToArray()), theme.Info, iw, false, theme.Panel);
                foreach (var c in _g.Contracts)
                    _t.WriteClipped(x, y++, Loc.T($"{Loc.T("Job")}: {c.Describe()} ({System.Math.Min(c.Done, c.Count)}/{c.Count})" + (c.Complete ? " ✓" : "")), c.Complete ? theme.Good : theme.Dim, iw, false, theme.Panel);
            }
            foreach (var comp in _g.Companions)
                _t.WriteClipped(x, y++, $"Companion: {comp.Name}, level {comp.Level}, {System.Math.Max(0, comp.HP)}/{comp.MaxHP} HP", theme.Good, iw, false, theme.Panel);
            if (p.Corruption > 0 || p.Mutated.Count > 0)
            {
                _t.WriteClipped(x, y++, $"Corruption {p.Corruption}/{Game.CorruptionMax}", p.Corruption >= 60 ? theme.Bad : theme.Warn, iw, true, theme.Panel);
                var muts = new System.Collections.Generic.List<string>();
                foreach (string id in p.Mutated) { var mu = MutationTable.Find(id); if (mu != null) muts.Add(Loc.T(mu.Name)); }
                if (muts.Count > 0) _t.WriteClipped(x, y++, Loc.T("Mutations") + ": " + string.Join(", ", muts.ToArray()), theme.Info, iw, false, theme.Panel);
            }
            if (_g.StealthReduction() > 0 || _g.ArmourClatter() > 0)
                _t.WriteClipped(x, y++, _g.ArmourClatter() > 0 ? $"Stealth: notice -{_g.StealthReduction()}, armour rattles +{_g.ArmourClatter()}" : $"Stealth: monsters notice you {_g.StealthReduction()} square(s) later", theme.Info, iw, false, theme.Panel);
            var gearLines = p.Gear.Lines();
            // One pattern per part: the joined line ("+2 Dex, fire 30%") is not a sentence any dictionary can hold.
            if (gearLines.Count > 0) _t.WriteClipped(x, y++, "Gear: " + string.Join(", ", gearLines.ConvertAll(l => Loc.T(l)).ToArray()), theme.Info, iw, false, theme.Panel);
            foreach (var set in ArtifactSets.All)
            {
                int pieces = ArtifactSets.Worn(p, set.Id);
                if (pieces > 0) _t.WriteClipped(x, y++, $"Set: {Loc.T(set.Name)} {pieces}/3", pieces >= 2 ? theme.Gold : theme.Dim, iw, false, theme.Panel);
            }
            if (_g.WornRelics() > 0) _t.WriteClipped(x, y++, $"Relics worn: {_g.WornRelics()} (they corrupt)", theme.Warn, iw, false, theme.Panel);
            var faith = Gods.Find(p.God);
            if (faith != null) _t.WriteClipped(x, y++, Loc.T($"Faith: {faith.Name}, {faith.Title} - piety {p.Piety}/{Gods.MaxPiety}" + (p.GodTier > 0 ? $" (tier {p.GodTier})" : "")), theme.Gold, iw, false, theme.Panel);
            var traits = Races.Find(p.RaceId).TraitLines();
            if (traits.Count > 0) _t.WriteClipped(x, y++, "Traits: " + string.Join(", ", traits.ToArray()), theme.Info, iw, false, theme.Panel);
            var wounds = Game.WoundLines(p);
            if (wounds.Count > 0) _t.WriteClipped(x, y++, Loc.T("Wounds") + ": " + string.Join(", ", wounds.ToArray()), Bodies.Bleeding(p) ? theme.Bad : theme.Warn, iw, false, theme.Panel);
            if (p.Scars.Count > 0) _t.WriteClipped(x, y++, Loc.T("Scars") + ": " + string.Join(", ", Game.ScarLines(p).ToArray()), theme.Dim, iw, false, theme.Panel);
            y++;
            _t.WriteClipped(x, y++, $"Now: {(_g.Map != null ? _g.Map.LevelName : _g.World.CurrentRegionName)}", theme.Text, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, $"Time: {_g.World.TimeString}", theme.Dim, iw, false, theme.Panel);
        }

        void DrawCreatePanel()
        {
            var theme = Theme.Current;
            var c = State.Create;
            string hint = c.Step == CreateStep.Name ? "Enter next  Esc title"
                : c.Step == CreateStep.Confirm ? "◄► mode  Enter begin  Esc back" : "Up/Down choose  Enter next  Esc back";
            PanelRect(out int px, out int py, out int pw, out int ph, 80, 25, "New character", hint);
            int x = px + 3, iw = pw - 6;
            var race = Races.All[c.RaceIndex];
            var role = Roles.All[c.RoleIndex];

            // Name field: a block cursor trails the text while it is being edited.
            bool editing = c.Step == CreateStep.Name;
            _t.Write(x, py + 2, "Name", editing ? theme.Accent : theme.Label, editing, theme.Panel);
            _t.FillRect(x + 6, py + 2, Heroes.MaxName + 2, 1, ' ', theme.Text, editing ? theme.PanelHi : theme.Panel);
            string shown = c.Name.Length == 0 && !editing ? Heroes.DefaultName : c.Name;
            _t.Write(x + 7, py + 2, shown, c.Name.Length == 0 && !editing ? theme.Dim : theme.Text, true, editing ? theme.PanelHi : theme.Panel);
            if (editing) _t.Put(x + 7 + c.Name.Length, py + 2, '█', theme.Accent, false, theme.PanelHi);

            void Column(int cx, int top, string head, int count, Func<int, string> label, int index, bool active)
            {
                _t.Write(cx, top, head, active ? theme.Accent : theme.Label, active, theme.Panel);
                for (int i = 0; i < count; i++)
                {
                    bool on = i == index;
                    Rgb bg = on && active ? theme.PanelHi : theme.Panel;
                    if (on && active) _t.FillRect(cx - 1, top + 1 + i, 17, 1, ' ', theme.Text, bg);
                    _t.Put(cx - 1, top + 1 + i, on ? '▶' : ' ', active ? theme.Accent : theme.Dim, true, bg);
                    _t.Write(cx + 1, top + 1 + i, label(i), on ? (active ? theme.Accent : theme.Text) : theme.Dim, on, bg);
                }
            }
            bool raceActive = c.Step == CreateStep.Race, roleActive = c.Step == CreateStep.Role;
            Column(x + 1, py + 4, "Race", Races.All.Length, i => Races.All[i].Name, c.RaceIndex, raceActive);
            Column(x + 19, py + 4, "Class", Roles.All.Length, i => Roles.All[i].Name, c.RoleIndex, roleActive);

            // Detail pane for whatever is being picked, plus the combined bonuses.
            int dx = x + 38, dw = px + pw - 3 - dx, y = py + 4;
            _t.WriteClipped(dx, y++, race.Name + " " + role.Name, theme.Title, dw, true, theme.Panel); y++;
            foreach (string line in Wrap(Loc.T(race.Description), dw)) _t.Write(dx, y++, line, theme.Text, false, theme.Panel);
            y++;
            foreach (string line in Wrap(Loc.T(role.Description), dw)) _t.Write(dx, y++, line, theme.Dim, false, theme.Panel);
            y++;
            _t.Write(dx, y++, "Attributes " + Mods(race, role), theme.Info, false, theme.Panel);
            int hp = System.Math.Max(2, role.HpPerLevel + race.HpPerLevelMod);
            _t.Write(dx, y++, $"HP/level {hp}   Gold {System.Math.Max(0, role.Gold + race.GoldMod)}", theme.Text, false, theme.Panel);
            var traitLines = race.TraitLines();
            if (traitLines.Count > 0) foreach (string line in Wrap("Traits: " + string.Join(", ", traitLines.ToArray()), dw)) _t.Write(dx, y++, line, theme.Info, false, theme.Panel);
            foreach (string line in Wrap(Loc.T("Kit: " + Kit(role)), dw)) _t.Write(dx, y++, line, theme.Text, false, theme.Panel);
            var skills = new System.Collections.Generic.List<string>();
            var total = new System.Collections.Generic.Dictionary<Skill, int>(role.StartingSkills);
            foreach (var kv in race.StartingSkills) total[kv.Key] = (total.ContainsKey(kv.Key) ? total[kv.Key] : 0) + kv.Value;
            foreach (var kv in total) skills.Add(kv.Key + " +" + kv.Value);
            foreach (string line in Wrap(Loc.T("Skills: " + (skills.Count == 0 ? "none" : string.Join(", ", skills.ToArray()))), dw)) _t.Write(dx, y++, line, theme.Text, false, theme.Panel);

            if (c.Step == CreateStep.Confirm)
            {
                string who = $"{Heroes.CleanName(c.Name)} the {race.Name} {role.Name}";
                _t.WriteClipped(x, py + ph - 5, "Mode", theme.Label, 6, false, theme.Panel);
                string dn = Loc.T(Difficulties.Name(c.Difficulty));
                _t.Write(x + 6, py + ph - 5, "◄ " + dn + " ►", theme.Accent, true, theme.Panel);
                _t.WriteClipped(x + 8 + dn.Length + 5, py + ph - 5, Loc.T(Difficulties.Blurb(c.Difficulty)), theme.Dim, iw - dn.Length - 14, false, theme.Panel);
                _t.WriteClipped(x, py + ph - 3, Loc.T($"Begin as {who}?  (Enter)"), theme.Good, iw, true, theme.Panel);
            }
        }

        static string Mods(RaceDef r, RoleDef o)
        {
            var parts = new System.Collections.Generic.List<string>();
            void Add(string n, int v) { if (v != 0) parts.Add(n + (v > 0 ? "+" : "") + v); }
            Add("Str", r.StrMod + o.StrMod); Add("Dex", r.DexMod + o.DexMod); Add("Con", r.ConMod + o.ConMod);
            Add("Int", r.IntMod + o.IntMod); Add("Wis", r.WisMod + o.WisMod); Add("Cha", r.ChaMod + o.ChaMod);
            return parts.Count == 0 ? "average" : string.Join(" ", parts.ToArray());
        }

        static string Kit(RoleDef r)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(r.Weapon)) parts.Add(r.Weapon);
            if (!string.IsNullOrEmpty(r.Armor)) parts.Add(r.Armor);
            if (!string.IsNullOrEmpty(r.Shield)) parts.Add(r.Shield);
            if (!string.IsNullOrEmpty(r.Book)) parts.Add(r.Book);
            if (!string.IsNullOrEmpty(r.Tool)) parts.Add(r.Tool);
            return string.Join(", ", parts.ToArray());
        }

        static System.Collections.Generic.IEnumerable<string> Wrap(string text, int width)
        {
            var line = new System.Text.StringBuilder();
            foreach (string word in text.Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width) { yield return line.ToString(); line.Clear(); }
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0) yield return line.ToString();
        }

        static readonly string[] SchoolTabs = { "Evo", "Con", "Alt", "Ill", "Nec", "Sac", "Nat", "Sha" };

        void DrawSpellsPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            PanelRect(out int px, out int py, out int pw, out int ph, 72, 26, "Spells", "Up/Down  Left/Right school  Enter casts  Esc closes");
            int x = px + 3, iw = pw - 6, y = py + 2;
            _t.Write(x, y, $"Mana {p.Mp}/{p.MpMax}", theme.Magic, true, theme.Panel);
            _t.Write(x + 16, y, "Lv  School        Cost  Fail", theme.Label, false, theme.Panel);
            y++;
            // School tabs: the one shown is lit, schools you know nothing of are dim.
            int tx = x;
            for (int t = -1; t < SchoolTabs.Length; t++)
            {
                string name = Loc.T(t < 0 ? "All" : SchoolTabs[t]);
                int n = _g.SpellCount(t);
                bool on = State.SpellSchool == t;
                Rgb fg = on ? theme.Accent : n > 0 ? theme.Text : theme.Dim;
                string label = name + (n > 0 ? n.ToString() : "");
                _t.Write(tx, y, on ? "[" + label + "]" : " " + label + " ", fg, on, theme.Panel);
                tx += label.Length + 2;
            }
            y += 2;
            if (_g.CastableSpells().Count == 0)
            {
                _t.WriteClipped(x, y++, "You know no spells.", theme.Dim, iw, false, theme.Panel);
                _t.WriteClipped(x, y++, "Read a spellbook (r) away from enemies to learn from it.", theme.Dim, iw, false, theme.Panel);
                return;
            }
            var list = _g.SpellsShown();
            if (list.Count == 0) { _t.WriteClipped(x, y, Loc.T("Nothing known in this school."), theme.Dim, iw, false, theme.Panel); return; }
            int sel = System.Math.Max(0, System.Math.Min(State.SpellIndex, list.Count - 1));
            int rows = System.Math.Max(1, ph - 12);
            int first = System.Math.Max(0, System.Math.Min(sel - rows / 2, list.Count - rows));
            if (first > 0) _t.Put(px + pw - 4, y - 1, '▲', theme.Dim, false, theme.Panel);
            if (first + rows < list.Count) _t.Put(px + pw - 4, y + rows, '▼', theme.Dim, false, theme.Panel);
            for (int i = first; i < list.Count && i < first + rows; i++, y++)
            {
                var s = Spells.Find(list[i]);
                if (s == null) continue;
                bool on = i == sel, afford = p.Mp >= s.Cost;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                Rgb fg = !afford ? theme.Dim : on ? theme.Accent : theme.Text;
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x, y, i < 26 ? ((char)('a' + i)).ToString() : " ", theme.Title, true, bg);
                if (!p.Spells.Contains(s.Id)) _t.Put(x + 1, y, '◆', theme.Magic, true, bg);   // lent by what you wear or wield
                _t.Write(x + 3, y, s.Name, fg, on, bg);
                _t.Write(x + 25, y, s.Level.ToString(), fg, false, bg);
                _t.Write(x + 29, y, s.School.ToString(), fg, false, bg);
                _t.Write(x + 41, y, s.Cost.ToString(), afford ? theme.Magic : theme.Bad, false, bg);
                _t.Write(x + 47, y, Spells.FailPct(p, s) + "%", fg, false, bg);
            }
            var cur = Spells.Find(list[sel]);
            if (cur != null)
            {
                int by = py + ph - 6;
                _t.HLine(px + 2, by - 1, pw - 4, theme.Rule);
                var blurb = new System.Collections.Generic.List<string>(Wrap(Loc.U(cur.Blurb), iw));
                for (int bl = 0; bl < 2; bl++) if (bl < blurb.Count) _t.Write(x, by + bl, blurb[bl], theme.Text, false, theme.Panel);
                by++;
                string R = Loc.T("Range");
                string tgt = cur.Target == SpellTarget.Self ? (cur.Radius > 0 && cur.Shape == Shape.Nova ? $"{Loc.T("Around you")}, {cur.Radius} {Loc.T("cells")}" : Loc.T("Self")) : cur.Target == SpellTarget.Cone ? $"Cone {cur.Radius}, {Loc.T("aim within")} {cur.Range}" : cur.Shape == Shape.Ball ? $"{R} {cur.Range}, {Loc.T("radius")} {cur.Radius}" : $"{R} {cur.Range}";
                _t.WriteClipped(x, by + 1, $"{tgt}.  {Loc.T("Casting stat")} {Loc.T(Roles.Find(p.RoleId).MpStat == 'W' ? "Wis" : "Int")}; {Loc.T("armour and shield raise failure.")}", theme.Dim, iw, false, theme.Panel);
                string dmg = cur.Dice > 0 ? $"{Loc.T("Damage")} {cur.Dice}d{cur.Sides}+{Loc.T("level")}/{System.Math.Max(1, cur.Div)} {Loc.T("dice")}, {Loc.T(cur.Type.ToString().ToLowerInvariant())}." : "";
                if (cur.Rider != Rider.None) dmg += $" {Loc.T("Rider")}: {Loc.T(cur.Rider.ToString().ToLowerInvariant())} ({cur.RiderPct}%).";
                if (dmg.Length > 0) _t.WriteClipped(x, by + 2, dmg.Trim(), theme.Dim, iw, false, theme.Panel);
            }
        }

        void DrawAltarPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            var god = _g.AltarGod();
            PanelRect(out int px, out int py, out int pw, out int ph, 76, 24, "Altar", "Up/Down  Enter chooses  Esc leaves");
            int x = px + 3, iw = pw - 6, y = py + 2;
            if (god == null) return;
            _t.WriteClipped(x, y++, Loc.T($"{god.Name}, {god.Title}"), theme.Title, iw, true, theme.Panel);
            _t.WriteClipped(x, y++, Loc.T($"God of {god.Domain}."), theme.Dim, iw, false, theme.Panel);
            y++;
            _t.WriteClipped(x, y++, Loc.T("Likes:    " + god.Likes), theme.Good, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, Loc.T("Dislikes: " + god.Dislikes), theme.Bad, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, Loc.T($"Boon ({god.BoonCost} piety): " + god.Boon), theme.Text, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, Loc.T($"Piety {Gods.Tier1At}: " + god.Tier1), theme.Info, iw, false, theme.Panel);
            _t.WriteClipped(x, y++, Loc.T($"Piety {Gods.Tier2At}: " + god.Tier2), theme.Info, iw, false, theme.Panel);
            var rival = Gods.Find(god.Rival);
            if (rival != null) _t.WriteClipped(x, y++, "Rival: " + rival.Name, theme.Dim, iw, false, theme.Panel);
            y++;
            if (p.God == god.Id)
            {
                _t.Write(x, y, Loc.T($"Your piety {p.Piety}/{Gods.MaxPiety}"), theme.Gold, true, theme.Panel);
                Bar(x + 22, y, Math.Max(6, iw - 22), (double)p.Piety / Gods.MaxPiety, theme.Gold, theme);
                y++;
                _t.WriteClipped(x, y++, p.PrayerTimer > 0 ? Loc.T($"The last prayer is still fresh ({p.PrayerTimer} turns).") : Loc.T("You may pray."), theme.Dim, iw, false, theme.Panel);
            }
            else if (p.God != null) _t.WriteClipped(x, y++, Loc.T($"You follow {_g.God.Name}."), theme.Warn, iw, false, theme.Panel);
            else _t.WriteClipped(x, y++, "You follow no god.", theme.Dim, iw, false, theme.Panel);
            y++;
            var rows = _g.AltarRows();
            int sel = Math.Max(0, Math.Min(State.AltarIndex, rows.Count - 1));
            for (int i = 0; i < rows.Count && y < py + ph - 1; i++, y++)
            {
                bool on = i == sel;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                Rgb fg = !rows[i].Enabled ? theme.Dim : on ? theme.Accent : theme.Text;
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x, y, ((char)('a' + i)).ToString(), theme.Title, true, bg);
                _t.WriteClipped(x + 3, y, rows[i].Label, fg, iw - 3, on, bg);
            }
        }

        /// <summary>The menu of whoever you bumped at a counter: browse, rest, heal, appraise, hear the news.</summary>
        /// <summary>A conversation: who is speaking, what they say (several lines, wrapped), and the choices numbered below.</summary>
        void DrawDialoguePanel()
        {
            var theme = Theme.Current;
            var who = _g.Talking;
            var rows = _g.ServiceRows();
            var note = new System.Collections.Generic.List<string>(Wrap(Loc.U(_g.ServiceNote ?? ""), 70));
            if (note.Count > 10) note.RemoveRange(10, note.Count - 10);
            int want = 9 + rows.Count + note.Count;
            PanelRect(out int px, out int py, out int pw, out int ph, 78, want, who != null ? who.Name : "", "Up/Down  Enter chooses  Esc leaves");
            int x = px + 3, iw = pw - 6, y = py + 2;
            if (who != null) _t.WriteClipped(x, y, Loc.U(TownText.RoleTitle(who.Role)), theme.Label, iw, false, theme.Panel);
            y++;
            _t.HLine(px + 1, y, pw - 2, theme.Rule);
            y += 2;
            foreach (string line in note) _t.WriteClipped(x, y++, line, theme.Narrative, iw, false, theme.Panel);
            y++;

            int sel = Math.Max(0, Math.Min(State.ServiceIndex, rows.Count - 1));
            for (int i = 0; i < rows.Count && y < py + ph - 1; i++, y++)
            {
                bool on = i == sel;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                Rgb fg = !rows[i].Enabled ? theme.Dim : on ? theme.Accent : theme.Text;
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x, y, ((char)('a' + i)).ToString(), theme.Title, true, bg);
                string price = rows[i].Price > 0 ? rows[i].Price + "g" : "";
                int priceX = px + pw - 3 - price.Length;
                _t.WriteClipped(x + 3, y, rows[i].Label, fg, Math.Max(1, priceX - (x + 3) - 1), on, bg);
                if (price.Length > 0) _t.Write(priceX, y, price, _g.CarryingGold() >= rows[i].Price ? theme.Gold : theme.Dim, on, bg);
            }
        }

        void DrawServicePanel()
        {
            if (_g.CurrentDialogue != null && _g.Talking != null) { DrawDialoguePanel(); return; }
            var theme = Theme.Current;
            var b = _g.TalkBuilding;
            if (b == null) { PanelRect(out _, out _, out _, out _, 60, 8, "Town", "Esc leaves"); return; }
            var rows = _g.ServiceRows();
            var note = new System.Collections.Generic.List<string>(Wrap(Loc.U(_g.ServiceNote ?? ""), 66));
            if (note.Count > 4) note.RemoveRange(4, note.Count - 4);
            int want = 11 + rows.Count + note.Count;
            PanelRect(out int px, out int py, out int pw, out int ph, 74, want, b.Name ?? "Town", "Up/Down  Enter chooses  Esc leaves");
            int x = px + 3, iw = pw - 6, y = py + 2;

            var who = _g.Talking;
            if (who != null) _t.WriteClipped(x, y, who.Name + ", " + Loc.U(TownText.RoleTitle(who.Role)), theme.Title, iw, true, theme.Panel);
            y++;
            _t.Write(x, y, "Your gold", theme.Label, false, theme.Panel);
            _t.Write(x + 10, y, _g.CarryingGold().ToString(), theme.Gold, true, theme.Panel);
            y++;
            _t.HLine(px + 1, y, pw - 2, theme.Rule);
            y += 2;
            foreach (string line in note) _t.WriteClipped(x, y++, line, theme.Narrative, iw, false, theme.Panel);
            y++;

            int sel = Math.Max(0, Math.Min(State.ServiceIndex, rows.Count - 1));
            for (int i = 0; i < rows.Count && y < py + ph - 2; i++, y++)
            {
                bool on = i == sel;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                Rgb fg = !rows[i].Enabled ? theme.Dim : on ? theme.Accent : theme.Text;
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x, y, ((char)('a' + i)).ToString(), theme.Title, true, bg);
                string price = rows[i].Price > 0 ? rows[i].Price + "g" : "";
                int priceX = px + pw - 3 - price.Length;
                _t.WriteClipped(x + 3, y, rows[i].Label, fg, Math.Max(1, priceX - (x + 3) - 1), on, bg);
                if (price.Length > 0) _t.Write(priceX, y, price, _g.CarryingGold() >= rows[i].Price ? theme.Gold : theme.Dim, on, bg);
            }
        }

        void DrawAbilitiesPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            PanelRect(out int px, out int py, out int pw, out int ph, 72, 18, "Abilities", "Up/Down  Enter uses  Esc closes");
            int x = px + 3, iw = pw - 6, y = py + 2;
            _t.Write(x, y, $"Vigor {p.Vigor}/{p.VigorMax}", theme.Gold, true, theme.Panel);
            _t.Write(x + 20, y, "Cost  Reach", theme.Label, false, theme.Panel);
            y += 2;
            if (p.Abilities.Count == 0)
            {
                _t.WriteClipped(x, y++, "You know no abilities.", theme.Dim, iw, false, theme.Panel);
                _t.WriteClipped(x, y++, "Class perks (Shift+C) grant them.", theme.Dim, iw, false, theme.Panel);
                return;
            }
            int sel = System.Math.Max(0, System.Math.Min(State.AbilityIndex, p.Abilities.Count - 1));
            for (int i = 0; i < p.Abilities.Count && y < py + ph - 5; i++, y++)
            {
                var a = Abilities.Find(p.Abilities[i]);
                if (a == null) continue;
                bool on = i == sel, afford = p.Vigor >= a.Cost;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                Rgb fg = !afford ? theme.Dim : on ? theme.Accent : theme.Text;
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x, y, ((char)('a' + i)).ToString(), theme.Title, true, bg);
                _t.Write(x + 3, y, a.Name, fg, on, bg);
                _t.Write(x + 20, y, a.Cost.ToString(), afford ? theme.Gold : theme.Bad, false, bg);
                _t.Write(x + 26, y, a.Target == AbilityTarget.Self ? "self" : a.Target == AbilityTarget.Adjacent ? "adjacent" : "range " + a.Range, fg, false, bg);
            }
            var cur = Abilities.Find(p.Abilities[sel]);
            if (cur != null)
            {
                int by = py + ph - 4;
                _t.HLine(px + 2, by - 1, pw - 4, theme.Rule);
                _t.WriteClipped(x, by, cur.Blurb, theme.Text, iw, false, theme.Panel);
            }
        }

        void DrawAdvancePanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            PanelRect(out int px, out int py, out int pw, out int ph, 78, 26, "Advancement", "Up/Down  Enter takes  Esc closes");
            int x = px + 3, iw = pw - 6, y = py + 2;
            _t.Write(x, y, $"{p.PendingAdvances} pick(s) to spend", p.PendingAdvances > 0 ? theme.Good : theme.Dim, true, theme.Panel);
            y += 2;
            var list = Progression.Available(p);
            if (list.Count == 0) { _t.WriteClipped(x, y, "Nothing is available to you right now.", theme.Dim, iw, false, theme.Panel); return; }
            int sel = System.Math.Max(0, System.Math.Min(State.AdvanceIndex, list.Count - 1));
            int rows = System.Math.Max(1, ph - 9);
            int first = System.Math.Max(0, System.Math.Min(sel - rows / 2, list.Count - rows));
            if (first > 0) _t.Put(px + pw - 4, y - 1, '▲', theme.Dim, false, theme.Panel);
            if (first + rows < list.Count) _t.Put(px + pw - 4, y + rows, '▼', theme.Dim, false, theme.Panel);
            for (int i = first; i < list.Count && i < first + rows; i++, y++)
            {
                var d = list[i];
                bool on = i == sel;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                Rgb fg = p.PendingAdvances <= 0 ? theme.Dim : on ? theme.Accent : theme.Text;
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x, y, i < 26 ? ((char)('a' + i)).ToString() : " ", theme.Title, true, bg);
                int rank = p.PerkRank(d.Id);
                string name = d.MaxRank > 1 ? $"{d.Name} {rank + 1}/{d.MaxRank}" : d.Name;
                _t.Write(x + 3, y, name, fg, on, bg);
                if (d.GrantsAbility != null) _t.Write(x + 28, y, "ability", theme.Gold, false, bg);
            }
            var cur = list[sel];
            int by = py + ph - 4;
            _t.HLine(px + 2, by - 1, pw - 4, theme.Rule);
            _t.WriteClipped(x, by, cur.Blurb, theme.Text, iw, false, theme.Panel);
            string need = cur.MinLevel > 1 ? Loc.T($"Level {cur.MinLevel}+") : "";
            if (cur.Roles != null) need += (need.Length > 0 ? "   " : "") + "For: " + string.Join("/", cur.Roles);
            _t.WriteClipped(x, by + 1, need, theme.Dim, iw, false, theme.Panel);
        }

        void DrawTravelPanel()
        {
            var theme = Theme.Current;
            int w = Math.Min(48, _t.Width - 4), h = 8;
            int px = (_t.Width - w) / 2, py = 2;
            _t.FillRect(px, py, w, h, ' ', theme.Text, theme.Panel);
            _t.DoubleBox(px, py, w, h, theme.Frame, false, theme.Panel);
            EmbedTitle(px, py, w, "TRAVEL", theme);
            _t.Write(px + 3, py + 2, "Arrow keys choose a destination.", theme.Text, false, theme.Panel);
            _t.Write(px + 3, py + 3, "Enter confirms.  Esc cancels.", theme.Dim, false, theme.Panel);
            _t.Write(px + 3, py + 5, $"You are in {_g.World.CurrentRegionName}", theme.Warn, false, theme.Panel);
            _t.Write(px + 3, py + 6, $"Cursor {_g.UiState.TravelX},{_g.UiState.TravelY}", theme.Info, false, theme.Panel);
        }

        void DrawShopPanel()
        {
            var theme = Theme.Current;
            var shop = _g.CurrentShop;
            string title = !string.IsNullOrEmpty(_g.ShopName) ? _g.ShopName
                : (shop != null && !string.IsNullOrEmpty(shop.Name) ? shop.Name : "Shop");
            PanelRect(out int px, out int py, out int pw, out int ph, 70, 24, title, "Esc leaves");

            // Defensive: the panel flag can outlive the shop (a stale save, a test
            // that only sets Active). Never throw here; say the keeper is gone.
            if (shop == null || !_g.InShop)
            {
                _t.Write(px + 3, py + 2, "The shopkeeper is gone.", theme.Dim, false, theme.Panel);
                return;
            }

            _t.Write(px + 3, py + 2, "Your gold", theme.Label, false, theme.Panel);
            _t.Write(px + 13, py + 2, _g.CarryingGold().ToString(), theme.Gold, true, theme.Panel);
            _t.Write(px + 28, py + 2, "Shop gold", theme.Label, false, theme.Panel);
            _t.Write(px + 38, py + 2, shop.Gold.ToString(), theme.Dim, false, theme.Panel);
            int mood = _g.TraderMood(shop);
            _t.Write(px + 48, py + 2, "Mood", theme.Label, false, theme.Panel);
            _t.WriteClipped(px + 53, py + 2, Game.MoodWord(mood), mood >= 5 ? theme.Good : mood <= -5 ? theme.Bad : theme.Dim, Math.Max(1, pw - 56), false, theme.Panel);
            _t.HLine(px + 1, py + 3, pw - 2, theme.Rule);

            int y0 = py + 4;
            int maxRows = Math.Max(1, ph - 7);
            int count = shop.Stock.Count;
            if (count == 0)
            {
                _t.Write(px + 3, y0, "Sold out.", theme.Dim, false, theme.Panel);
            }
            else
            {
                // Clamp the view only; the commands own the real cursor.
                int sel = Math.Max(0, Math.Min(State.ShopIndex, count - 1));
                int start = Math.Max(0, sel - maxRows + 1);
                if (start + maxRows > count) start = Math.Max(0, count - maxRows);
                for (int i = 0; i < maxRows; i++)
                {
                    int idx = start + i;
                    if (idx >= count) break;
                    var it = shop.Stock[idx];
                    bool cur = idx == sel;
                    int price = _g.ShopPrice(shop, it);
                    int y = y0 + i;
                    Rgb bg = cur ? theme.PanelHi : theme.Panel;
                    if (cur) RowBar(px + 1, y, pw - 2, theme);
                    _t.Put(px + 2, y, cur ? '▶' : ' ', theme.Accent, true, bg);
                    _t.Put(px + 4, y, it.Def.Glyph == ' ' ? '?' : it.Def.Glyph, theme.ItemColor(it), true, bg);
                    string left = it.Name + (it.Quantity > 1 ? $" (x{it.Quantity})" : "");
                    string right = price + "g";
                    int priceX = px + pw - 3 - right.Length;
                    _t.WriteClipped(px + 6, y, left, cur ? theme.Accent : theme.Text, Math.Max(1, priceX - (px + 6) - 1), cur, bg);
                    Rgb pc = _g.CarryingGold() >= price ? theme.Gold : theme.Dim;
                    _t.Write(priceX, y, right, pc, cur, bg);
                }
            }

            _t.WriteClipped(px + 3, py + ph - 2, "Enter/b buy   s sell   hjkl move   Esc leave", theme.Dim, pw - 6, false, theme.Panel);
        }

        // ------------------------------------------------------------------ menu

        static string MenuNote(MenuRow row)
        {
            switch (row)
            {
                case MenuRow.Resume: return Loc.T("Back.");
                case MenuRow.Save: return Loc.T("Writes this run to disk. Dying deletes the save.");
                case MenuRow.Theme: return Loc.T(DisplaySettings.Describe(DisplaySettings.Current.Preset));
                case MenuRow.Crt: return DisplaySettings.Current.Crt == CrtLevel.Off ? Loc.T("Flat pixels, no scanlines.") : Loc.T("Curvature, scanlines, phosphor glow.");
                case MenuRow.Scale: return DisplaySettings.Current.Scale == 0 ? Loc.T("The largest that fits the window.") : (Loc.Current == Lang.Pt ? "Fixo: cada pixel da fonte vale " : "Fixed: each font pixel is ") + DisplaySettings.Current.Scale + "x" + DisplaySettings.Current.Scale + (Loc.Current == Lang.Pt ? " pixels de tela." : " screen pixels.");
                case MenuRow.Tiles: return Loc.T(DisplaySettings.Current.Square ? "Each map cell is two columns wide: the world looks square." : "One column per map cell: the world looks tall and narrow.");
                case MenuRow.Language: return Loc.T("Portuguese (Brazil) or English.");
                case MenuRow.Master: case MenuRow.Effects: return Loc.T("Short square-wave bleeps: hits, kills, wounds, warnings.");
                case MenuRow.Music: return Loc.T("Music is not written yet.");
                case MenuRow.Controls: return Loc.T("Rebind any key.");
                case MenuRow.Achievements: return Loc.T("What you have done across all your runs.");
                case MenuRow.PastRuns: return Loc.T("Your finished expeditions, newest first.");
                case MenuRow.MainMenu: return Loc.T("Saves the run and returns to the title.");
                default: return Loc.T("Saves the run and closes the game.");
            }
        }

        void DrawSettingsPanel()
        {
            var theme = Theme.Current;
            var set = DisplaySettings.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 60, 27, Loc.T("Menu"), Loc.T("Esc resumes"));
            int x = px + 3, iw = pw - 6;
            int sel = State.SettingsIndex;
            int y = py + 2;

            void Header(string name)
            {
                _t.HLine(px + 2, y, pw - 4, theme.Rule);
                _t.Write(px + 4, y, " " + Loc.T(name) + " ", theme.Label, false, theme.Panel);
                y++;
            }

            void Row(MenuRow row, string label, string value, bool arrows, int volume = -1, Rgb? labelColor = null)
            {
                int index = Array.IndexOf(MenuRows.All, row);
                bool on = sel == index;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Write(x + 1, y, Loc.T(label), on ? theme.Accent : (labelColor ?? theme.Text), on, bg);
                int vx = x + 19;
                if (volume >= 0)
                {
                    _t.Put(vx, y, '◄', on ? theme.Text : theme.Dim, false, bg);
                    for (int i = 0; i < AudioSettings.Max; i++)
                        _t.Put(vx + 2 + i, y, i < volume ? '■' : '□', i < volume ? theme.Gold : theme.BarEmpty, false, bg);
                    _t.Put(vx + 13, y, '►', on ? theme.Text : theme.Dim, false, bg);
                    _t.Write(vx + 15, y, volume.ToString(), theme.Text, on, bg);
                }
                else if (value != null)
                {
                    if (arrows) _t.Put(vx, y, '◄', on ? theme.Text : theme.Dim, false, bg);
                    _t.Write(vx + 2, y, Loc.T(value), theme.Text, true, bg);
                    if (arrows) _t.Put(vx + 3 + Loc.T(value).Length, y, '►', on ? theme.Text : theme.Dim, false, bg);
                }
                y++;
            }

            Rgb? off = TitleBackdrop ? theme.Dim : (Rgb?)null;   // no run yet: these two do nothing
            Row(MenuRow.Resume, TitleBackdrop ? "Back" : "Resume", null, false);
            Row(MenuRow.Save, "Save game", null, false, -1, off);
            Header("Display");
            Row(MenuRow.Theme, "Theme", DisplaySettings.Name(set.Preset), true);
            Row(MenuRow.Crt, "CRT", DisplaySettings.Name(set.Crt), true);
            Row(MenuRow.Scale, "Text size", DisplaySettings.ScaleName(set.Scale), true);
            Row(MenuRow.Tiles, "Tiles", set.Square ? "Square" : "Narrow", true);
            Row(MenuRow.Language, "Language", Loc.Name(set.Language), true);
            Header("Audio");
            var audio = AudioSettings.Current;
            Row(MenuRow.Master, "Master volume", null, true, audio.Master);
            Row(MenuRow.Music, "Music", null, true, audio.Music);
            Row(MenuRow.Effects, "Effects", null, true, audio.Effects);
            Header("Game");
            Row(MenuRow.Controls, "Controls", "rebind keys", false);
            Row(MenuRow.Achievements, "Achievements", "local", false);
            Row(MenuRow.PastRuns, "Past runs", "history", false);
            Row(MenuRow.MainMenu, "Main menu", null, false, -1, off);
            Row(MenuRow.Quit, "Quit game", null, false, -1, theme.Bad);

            int note = py + ph - 4;
            if (State.MenuNote.Length > 0) _t.WriteClipped(x, note, State.MenuNote, theme.Good, iw, true, theme.Panel);
            _t.WriteClipped(x, py + ph - 3, MenuNote(MenuRows.All[Math.Max(0, Math.Min(sel, MenuRows.All.Length - 1))]), theme.Dim, iw, false, theme.Panel);
            _t.WriteClipped(x, py + ph - 2, Loc.T("↑↓ select   ←→ change   Enter choose"), theme.Dim, iw, false, theme.Panel);
        }

        // -------------------------------------------------------------- controls

        void DrawControlsPanel()
        {
            var theme = Theme.Current;
            var binds = KeyBindings.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 66, 28, "Controls", "Esc back");
            int x = px + 3, iw = pw - 6;
            int count = KeyBindings.Actions.Length;
            int sel = Math.Max(0, Math.Min(State.ControlsIndex, count - 1));

            // Flatten into display rows: a header whenever the group changes.
            var rows = new List<int>();             // -1 - groupIndex for headers, else action index
            var groups = new List<string>();
            string last = null;
            for (int i = 0; i < count; i++)
            {
                if (KeyBindings.Actions[i].Group != last)
                {
                    last = KeyBindings.Actions[i].Group; groups.Add(last); rows.Add(-groups.Count);
                }
                rows.Add(i);
            }
            int visible = Math.Max(3, ph - 7);
            int selRow = rows.IndexOf(sel);
            int start = Math.Max(0, Math.Min(selRow - visible / 2, rows.Count - visible));

            for (int r = 0; r < visible && start + r < rows.Count; r++)
            {
                int y = py + 2 + r, item = rows[start + r];
                if (item < 0)
                {
                    _t.HLine(px + 2, y, pw - 4, theme.Rule);
                    _t.Write(px + 4, y, " " + groups[-item - 1] + " ", theme.Label, false, theme.Panel);
                    continue;
                }
                bool on = item == sel;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.WriteClipped(x + 1, y, KeyBindings.Actions[item].Label, on ? theme.Accent : theme.Text, 26, on, bg);
                if (on && State.Rebinding) _t.Write(x + 29, y, "press a key...", theme.Accent, true, bg);
                else
                {
                    bool changed = !SameKeys(binds, item);
                    _t.WriteClipped(x + 29, y, binds.KeysLabel(item), changed ? theme.Good : theme.Title, iw - 30, on, bg);
                }
            }
            if (rows.Count > visible)
            {
                int bar = Math.Max(1, visible * visible / rows.Count);
                int top = (rows.Count - visible) == 0 ? 0 : start * (visible - bar) / (rows.Count - visible);
                for (int i = 0; i < visible; i++) _t.Put(px + pw - 2, py + 2 + i, i >= top && i < top + bar ? '█' : '│', i >= top && i < top + bar ? theme.Dim : theme.Rule, false, theme.Panel);
            }

            if (State.BindNote.Length > 0) _t.WriteClipped(x, py + ph - 3, State.BindNote, theme.Good, iw, true, theme.Panel);
            else
            {
                string layout = KeyLayoutNote(sel);
                if (layout.Length > 0) _t.WriteClipped(x, py + ph - 3, layout, theme.Dim, iw, false, theme.Panel);
            }
            string hint = State.Rebinding ? "Press the new key.  Esc cancels." : "Enter rebind   Del clear   R reset   ⇧R reset all";
            _t.WriteClipped(x, py + ph - 2, hint, theme.Dim, iw, false, theme.Panel);
        }

        /// <summary>
        /// Descending and climbing are typed as symbols, and ">" has no key of its own on any layout (US or ABNT2): the line under
        /// the cursor says so, so nobody goes looking for a key that is not on the keyboard. Empty for every other action.
        /// </summary>
        static string KeyLayoutNote(int action)
        {
            if (action < 0 || action >= KeyBindings.Actions.Length) return "";
            switch (KeyBindings.Actions[action].Char)
            {
                case ">": return Loc.T("> is Shift + . on every keyboard, US or ABNT2.");
                case "<": return Loc.T("< is Shift + , on every keyboard, US or ABNT2.");
                default: return "";
            }
        }

        // ------------------------------------------------------------------ achievements

        void DrawAchievementsPanel()
        {
            var theme = Theme.Current;
            var all = Achievements.All;
            int got = 0;
            foreach (var a in all) if (State.Unlocked.ContainsKey(a.Id)) got++;
            PanelRect(out int px, out int py, out int pw, out int ph, 76, 24, "Achievements", "Esc back");
            int x = px + 3, iw = pw - 6;
            _t.Write(x, py + 2, $"{got}/{all.Length}", got == all.Length ? theme.Gold : theme.Label, true, theme.Panel);
            int sel = Math.Max(0, Math.Min(State.AchIndex, all.Length - 1));
            int visible = Math.Max(3, ph - 8);
            int start = Math.Max(0, Math.Min(sel - visible / 2, all.Length - visible));
            for (int r = 0; r < visible && start + r < all.Length; r++)
            {
                var a = all[start + r];
                bool has = State.Unlocked.TryGetValue(a.Id, out string when);
                bool on = start + r == sel;
                int y = py + 4 + r;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                _t.Put(x + 1, y, has ? '♦' : '·', has ? theme.Gold : theme.Dim, true, bg);
                _t.WriteClipped(x + 3, y, Loc.T(a.Name), has ? (on ? theme.Accent : theme.Text) : theme.Dim, 22, on, bg);
                _t.WriteClipped(x + 27, y, has ? when : "", theme.Dim, iw - 27, false, bg);
            }
            var s = all[sel];
            _t.HLine(px + 2, py + ph - 4, pw - 4, theme.Rule);
            _t.WriteClipped(x, py + ph - 3, Loc.T(s.Blurb), theme.Text, iw, false, theme.Panel);
        }

        // ------------------------------------------------------------------ past runs

        void DrawRunsPanel()
        {
            var theme = Theme.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 84, 24, State.RunsDaily ? "Daily board" : "Past runs", "D daily board   Esc back");
            int x = px + 3, iw = pw - 6;
            var list = State.Runs;
            if (list.Count == 0)
            {
                _t.WriteClipped(x, py + 3, Loc.T(State.RunsDaily ? "No daily runs yet." : "No finished runs yet."), theme.Dim, iw, false, theme.Panel);
                return;
            }
            int sel = Math.Max(0, Math.Min(State.RunsIndex, list.Count - 1));
            int visible = Math.Max(3, ph - 8);
            int start = Math.Max(0, Math.Min(sel - visible / 2, list.Count - visible));
            _t.WriteClipped(x + 2, py + 2, Loc.T("Hero") + new string(' ', 0), theme.Label, 18, false, theme.Panel);
            _t.Write(x + 22, py + 2, Loc.T("Class"), theme.Label, false, theme.Panel);
            _t.Write(x + 40, py + 2, Loc.T("Dlvl"), theme.Label, false, theme.Panel);
            _t.Write(x + 46, py + 2, Loc.T("End"), theme.Label, false, theme.Panel);
            _t.Write(x + 62, py + 2, Loc.T("Score"), theme.Label, false, theme.Panel);
            for (int r = 0; r < visible && start + r < list.Count; r++)
            {
                var run = list[start + r];
                int y = py + 3 + r;
                bool on = start + r == sel;
                Rgb bg = on ? theme.PanelHi : theme.Panel;
                if (on) RowBar(px + 1, y, pw - 2, theme);
                _t.Put(px + 2, y, on ? '▶' : ' ', theme.Accent, true, bg);
                Rgb end = run.Outcome == "won" ? theme.Good : run.Outcome == "abandoned" ? theme.Dim : theme.Danger;
                _t.WriteClipped(x + 2, y, (run.Daily.Length > 0 ? "◆ " : "") + run.Name, on ? theme.Accent : theme.Text, 18, on, bg);
                _t.WriteClipped(x + 22, y, Loc.T(run.Role) + " " + run.Level, theme.Text, 17, false, bg);
                _t.Write(x + 40, y, run.MaxDepth.ToString(), theme.Text, false, bg);
                _t.WriteClipped(x + 46, y, Loc.T(run.Outcome), end, 15, false, bg);
                _t.Write(x + 62, y, run.Score.ToString(), theme.Gold, false, bg);
            }
            var sr = list[sel];
            _t.HLine(px + 2, py + ph - 5, pw - 4, theme.Rule);
            string how = sr.Outcome == "won" ? Loc.T("Escaped with the Amulet of Yendor.") : sr.Outcome == "abandoned" ? Loc.T("Abandoned the run.") : Loc.T("Killed by") + " " + Loc.T(sr.Cause);
            _t.WriteClipped(x, py + ph - 4, how, theme.Text, iw, true, theme.Panel);
            _t.WriteClipped(x, py + ph - 3, $"{Loc.T(sr.Race)} {Loc.T(sr.Role)}, {Loc.T(sr.Title)}   {Loc.T(sr.Branch)} {sr.Depth}   {sr.Turns} {Loc.T("Turns")}   {sr.Kills} {Loc.T("Kills")}", theme.Dim, iw, false, theme.Panel);
            _t.WriteClipped(x, py + ph - 2, $"{sr.Date}   {Loc.T("Seed")} {sr.Seed}" + (sr.Daily.Length > 0 ? $"   {Loc.T("Daily")} {sr.Daily}" : "") + (sr.Mode != "Normal" ? $"   {Loc.T(sr.Mode)}" : ""), theme.Dim, iw, false, theme.Panel);
        }

        static bool SameKeys(KeyBindings binds, int action)
        {
            var d = KeyBindings.Actions[action].Defaults; var k = binds.KeysOf(action);
            if (d.Length != k.Count) return false;
            for (int i = 0; i < d.Length; i++) if (d[i] != k[i]) return false;
            return true;
        }

        void DrawDeathPanel()
        {
            var theme = Theme.Current;
            var p = _g.Player;
            PanelRect(out int px, out int py, out int pw, out int ph, 46, 19, "Game over", null);
            string[] stone = {
                "      _______________",
                "     /               \\",
                "    /      R I P      \\",
                "   |                   |",
                "   |                   |",
                "   |                   |",
                "   |                   |",
                "  *|___________________|*",
            };
            int sx = px + (pw - 25) / 2;
            for (int i = 0; i < stone.Length; i++)
                _t.Write(sx, py + 2 + i, stone[i], theme.Dim, false, theme.Panel);
            CentreOn(px, pw, py + 6, p.Name ?? "Adventurer", theme.Text, true);
            CentreOn(px, pw, py + 7, Loc.T($"Dlvl {p.MaxDepth}   {p.Kills} kills"), theme.Dim, false);

            string cause = _g.Abandoned ? Loc.T("Abandoned the run.") : _g.DeathCause != null ? Loc.T("Killed by") + " " + Loc.T(_g.DeathCause) : "";
            if (cause.Length > 0) CentreOn(px, pw, py + 9, cause.Length > pw - 4 ? cause.Substring(0, pw - 4) : cause, theme.Warn, false);
            CentreOn(px, pw, py + 11, "You have died.", theme.Danger, true);
            CentreOn(px, pw, py + 13, "Any key begins again.", theme.Text, false);
            CentreOn(px, pw, py + 14, "Esc quits.", theme.Dim, false);
        }

        void DrawWinPanel()
        {
            var theme = Theme.Current;
            PanelRect(out int px, out int py, out int pw, out int ph, 50, 13, "Victory", null);
            CentreOn(px, pw, py + 3, "♦", theme.Gold, true);
            CentreOn(px, pw, py + 4, "You escape with your life.", theme.Good, true);
            CentreOn(px, pw, py + 6, $"Deepest level  {_g.Player.MaxDepth}", theme.Text, false);
            CentreOn(px, pw, py + 7, $"Kills          {_g.Player.Kills}", theme.Text, false);
            CentreOn(px, pw, py + 9, "Any key begins another run.", theme.Dim, false);
        }

        void CentreOn(int px, int pw, int y, string s, Rgb c, bool bold)
        {
            int x = px + Math.Max(1, (pw - s.Length) / 2);
            _t.Write(x, y, s, c, bold, Theme.Current.Panel);
        }

        string TitleFor()
        {
            switch (_g.Mode)
            {
                case GameMode.Overworld: return _g.World.CurrentRegionName;
                case GameMode.TownMap:
                    {
                        var inside = _g.InsideBuilding();
                        return inside != null && inside.Name != null ? inside.Name : (_g.Town != null ? _g.Town.Name : "Town");
                    }
                case GameMode.Dungeon: return _g.Map != null ? _g.Map.LevelName : "";
                default: return "OSSUARY";
            }
        }
    }
}
