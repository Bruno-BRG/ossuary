using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Draws the whole interface into a TextBuilder: the map, the status line,
    /// the message window, the right-hand sidebar, and every modal panel.
    /// Pure C# so the layout can be asserted headlessly.
    /// </summary>
    public sealed class Ui
    {
        readonly Game _g;
        readonly TextBuilder _t;
        public int FacingX = 1, FacingY;
        public bool FirstDraw = true;
        public string CenterTitle = "";
        public int CameraX, CameraY;
        public int MapX, MapY, MapW, MapH;

        const int MsgHeight = 3;

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

        public TextBuilder Draw()
        {
            DrainRequests();

            int w = _t.Width, h = _t.Height;
            _t.Clear();

            int mapTop = 0;
            int statusY = h - 1;
            int mapBottom = statusY - 1;
            // The message window owns its rows: the map ends where it begins,
            // so log text never paints over dungeon tiles.
            int msgTop = mapBottom - MsgHeight + 1;
            int mapH = Math.Max(1, msgTop - mapTop);
            var theme = Theme.Default;

            CenterTitle = TitleFor();

            if (_g.Mode == GameMode.Overworld || _g.Mode == GameMode.TownMap)
            {
                MapX = 0; MapY = mapTop; MapW = w; MapH = mapH;
                DrawOverworld(0, mapTop, w, mapH);
            }
            else
            {
                int sidebarW = w >= 96 ? 30 : 0;
                MapX = 0; MapY = mapTop; MapW = w - sidebarW; MapH = mapH;
                DrawMap(0, mapTop, MapW, MapH);
                if (sidebarW > 0) DrawSidebar(MapW, mapTop, sidebarW, mapH);
            }

            DrawMessageWindow(0, msgTop, w, MsgHeight);
            DrawStatusLine(0, statusY, w, theme);
            DrawOverlays();

            FirstDraw = false;
            return _t;
        }

        void DrainRequests()
        {
            if (Requests.Inventory) { State.Active = Panel.Inventory; State.ScrollOffset = 0; }
            if (Requests.Help) { State.Active = Panel.Help; State.ScrollOffset = 0; }
            if (Requests.HelpLong) { State.Active = Panel.Help; State.ScrollOffset = 8; }
            if (Requests.History) { State.Active = Panel.History; State.ScrollOffset = 0; }
            if (Requests.Discoveries) { State.Active = Panel.Discoveries; State.ScrollOffset = 0; }
            if (Requests.Character) { State.Active = Panel.Character; State.ScrollOffset = 0; }
            if (Requests.Travel) { State.Active = Panel.Travel; State.ScrollOffset = 0; }
            Requests.Clear();
        }

        // ----------------------------------------------------------------- map

        void DrawMap(int ox, int oy, int w, int h)
        {
            var map = _g.Map;
            if (map == null) return;
            var theme = Theme.Default;
            var p = _g.Player;

            int cx = p.X - w / 2;
            int cy = p.Y - h / 2;
            cx = Math.Max(0, Math.Min(cx, Math.Max(0, map.W - w)));
            cy = Math.Max(0, Math.Min(cy, Math.Max(0, map.H - h)));
            CameraX = cx; CameraY = cy;

            for (int y = 0; y < h; y++)
            {
                int my = cy + y;
                for (int x = 0; x < w; x++)
                {
                    int mx = cx + x;
                    if (!map.InBounds(mx, my)) continue;
                    bool vis = map.IsVisible(mx, my);
                    bool seen = map.WasSeen(mx, my);
                    if (!vis && !seen) continue;

                    TileKind t = vis ? map.Get(mx, my) : map.Remembered(mx, my);
                    char g = Tiles.Get(t).Glyph;
                    Rgb fg = theme.TileColor(t);
                    if (!vis) fg = fg.Dim(0.45f);
                    Rgb bg = vis ? theme.Background : theme.BackgroundDim;

                    if (vis)
                    {
                        var item = TopItemAt(mx, my);
                        if (item != null) { g = item.Def.Glyph; fg = theme.ItemColor(item); }

                        var m = _g.MonsterAt(mx, my);
                        if (m != null) { g = m.Glyph; fg = Rgb.FromHex(m.Def.Color); }
                    }

                    _t.Put(ox + x, oy + y, g, fg, vis && Tiles.IsAlt(t), bg);
                }
            }

            int px = ox + (p.X - cx), py = oy + (p.Y - cy);
            if (px >= ox && py >= oy && px < ox + w && py < oy + h)
                _t.Put(px, py, '@', theme.Player, true, theme.Background);

            if (State.IsTargeting)
            {
                int sx = ox + (State.TargetX - cx), sy = oy + (State.TargetY - cy);
                if (sx >= ox && sy >= oy && sx < ox + w && sy < oy + h)
                    _t.Put(sx, sy, '✚', theme.Cursor, true, theme.Background);
            }

            if (State.Active == Panel.Travel || _g.UiState.TravelMode)
            {
                int sx = ox + (_g.UiState.TravelX - cx), sy = oy + (_g.UiState.TravelY - cy);
                if (sx >= ox && sy >= oy && sx < ox + w && sy < oy + h)
                    _t.Put(sx, sy, '✦', theme.Cursor, true, theme.Background);
            }
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
            var theme = Theme.Default;
            int cx = Math.Max(0, Math.Min(world.PlayerX - w / 2, Math.Max(0, world.W - w)));
            int cy = Math.Max(0, Math.Min(world.PlayerY - h / 2, Math.Max(0, world.H - h)));
            CameraX = cx; CameraY = cy;

            for (int y = 0; y < h; y++)
            {
                int my = cy + y;
                for (int x = 0; x < w; x++)
                {
                    int mx = cx + x;
                    if (!world.InBounds(mx, my)) continue;
                    var t = world.Get(mx, my);
                    if (!t.Discovered) continue;
                    _t.Put(ox + x, oy + y, t.Glyph,
                           theme.OverworldColor(t.Terrain, t.Feature),
                           t.Feature != OverworldFeature.None, theme.Background);
                }
            }

            _t.Put(ox + (world.PlayerX - cx), oy + (world.PlayerY - cy), '@', theme.PlayerOver, true, theme.Background);

            if (_g.ActiveEncounter && _g.EncounterMonster != null)
            {
                int ex = ox + (_g.EncounterX - cx), ey = oy + (_g.EncounterY - cy);
                if (ex >= ox && ey >= oy && ex < ox + w && ey < oy + h)
                {
                    _t.Put(ex, ey, _g.EncounterMonster.Glyph, Rgb.FromHex(_g.EncounterMonster.Def.Color), true, theme.Background);
                    _t.Put(ex, ey - 1, '▼', theme.Cursor, true, theme.Background);
                }
            }
        }

        // -------------------------------------------------------------- sidebar

        void DrawSidebar(int ox, int oy, int w, int h)
        {
            var theme = Theme.Default;
            var p = _g.Player;
            _t.FillRect(ox, oy, w, h, ' ', theme.SidebarText, theme.Sidebar);

            int x = ox + 2;
            int y = oy + 1;
            int maxText = w - 4;

            _t.WriteClipped(x, y++, TitleBar(maxText), theme.Title, maxText, true);
            _t.HLine(ox + 1, y++, w - 2, theme.Rule);
            y++;

            double hpFrac = p.MaxHP > 0 ? (double)p.HP / p.MaxHP : 0;
            Rgb hpColor = hpFrac > 0.6f ? theme.Good : hpFrac > 0.3f ? theme.Warn : theme.Bad;
            _t.Write(x, y++, "Health", theme.Label);
            Bar(ox + 2, y++, w - 4, hpFrac, hpColor, theme);

            double enFrac = (double)p.Energy / p.EnergyMax;
            _t.Write(x, y++, "Energy", theme.Label);
            Bar(ox + 2, y++, w - 4, enFrac, theme.Energy, theme);
            y++;

            _t.Write(x, y++, $"Depth    {p.CurrentDepth}", theme.Text);
            _t.Write(x, y++, $"Deepest  {p.MaxDepth}", theme.Dim);
            _t.Write(x, y++, $"Turn     {_g.Turn}", theme.Dim);
            y++;

            _t.HLine(ox + 1, y++, w - 2, theme.Rule);
            _t.Write(x, y++, "Equipment", theme.Label, true);
            string wg = p.Wielded != null ? p.Wielded.Def.Glyph.ToString() : ")";
            string wname = p.Wielded != null ? p.Wielded.Name : "(bare hands)";
            string aname = p.WornArmor != null ? p.WornArmor.Name : "(nothing)";
            _t.WriteClipped(x, y++, $"{wg} {wname}", p.Wielded != null ? theme.Text : theme.Dim, maxText);
            _t.WriteClipped(x, y++, $"[ {aname}", p.WornArmor != null ? theme.Text : theme.Dim, maxText);
            for (int i = 0; i < 2; i++)
                if (p.Rings[i] != null) _t.WriteClipped(x, y++, $"= {p.Rings[i].Name}", theme.Text, maxText);
            y++;

            _t.HLine(ox + 1, y++, w - 2, theme.Rule);
            _t.Write(x, y++, "Vitals", theme.Label, true);
            _t.Write(x, y++, $"AC  {p.ArmorClass(),-4} Evade {p.Evasion()}", theme.Text);
            _t.Write(x, y++, $"Gold {_g.CarryingGold(),-6}", theme.Gold);
            _t.Write(x, y++, $"Food {p.FoodNutrient,-6}", p.FoodNutrient < 20 ? theme.Warn : theme.Text);
            if (p.PoisonResist > 0) _t.Write(x, y++, "Poisoned", theme.Bad);
            if (p.Confused) _t.Write(x, y++, "Confused", theme.Warn);
            if (p.Blinded) _t.Write(x, y++, "Blind", theme.Warn);
            y++;

            if (y + 14 < oy + h)
            {
                _t.HLine(ox + 1, y++, w - 2, theme.Rule);
                _t.Write(x, y++, "Skills", theme.Label, true);
                foreach (var kv in p.Skills)
                {
                    if (kv.Value <= 0) continue;
                    _t.WriteClipped(x, y++, $"{kv.Key} {kv.Value}", theme.Dim, maxText);
                }
            }

            if (State.ShowMinimap && _g.Map != null && y + 14 < oy + h)
            {
                y++;
                _t.Write(x, y++, "Map", theme.Label, true);
                int mmH = Math.Min(14, oy + h - y - 2);
                if (mmH > 2) DrawMinimap(ox + 2, y, w - 4, mmH, theme);
            }
        }

        void Bar(int ox, int oy, int w, double frac, Rgb color, Theme theme)
        {
            frac = Math.Max(0, Math.Min(1, frac));
            int filled = (int)Math.Round(frac * w);
            for (int i = 0; i < w; i++)
            {
                char c = i < filled ? '█' : '░';
                _t.Put(ox + i, oy, c, i < filled ? color : theme.BarEmpty, false, theme.Background);
            }
        }

        void DrawMinimap(int ox, int oy, int w, int h, Theme theme)
        {
            var map = _g.Map;
            if (map == null) return;
            // Frame first: the interior (ox+1..ox+w-2, oy+1..oy+h-2) is the only
            // drawable area. The old loop wrote at ox+x+1 with x < w, painting
            // over the right and bottom borders.
            _t.Box(ox, oy, w, h, theme.Rule, false, theme.Background);
            int iw = w - 2, ih = h - 2;
            if (iw <= 0 || ih <= 0) return;
            int stepX = Math.Max(1, map.W / iw);
            int stepY = Math.Max(1, map.H / ih);
            for (int y = 0; y < ih; y++)
            {
                int my = y * stepY;
                for (int x = 0; x < iw; x++)
                {
                    int mx = x * stepX;
                    if (!map.InBounds(mx, my)) continue;
                    if (!map.WasSeen(mx, my)) { _t.Put(ox + 1 + x, oy + 1 + y, ' ', theme.Fog, false, theme.Background); continue; }
                    var t = map.Get(mx, my);
                    Rgb c = (mx == _g.Player.X && my == _g.Player.Y) ? theme.Player : theme.TileColor(t).Dim(0.45f);
                    _t.Put(ox + 1 + x, oy + 1 + y, Tiles.Get(t).Glyph, c, false, theme.Background);
                }
            }
        }

        string TitleBar(int maxText)
        {
            string t = CenterTitle;
            if (t.Length > maxText) t = t.Substring(0, maxText);
            return t.PadLeft((maxText + t.Length) / 2).PadRight(maxText);
        }

        // ----------------------------------------------------------- status bar

        void DrawStatusLine(int ox, int oy, int w, Theme theme)
        {
            var p = _g.Player;
            _t.FillRect(ox, oy, w, 1, ' ', theme.Text, theme.StatusBar);

            // Every field is written through Field(), which keeps a single space
            // between fields and stops as soon as the line is full. Writing fields
            // back-to-back made "HP:42/42" run straight into "Str:13".
            int x = ox + 1;
            int limit = ox + w;

            int Field(string s, Rgb c)
            {
                if (s == null || x >= limit) return 0;
                if (x > ox + 1) { _t.Put(x, oy, ' ', theme.Dim); x++; }
                x = _t.WriteClipped(x, oy, s, c, limit - x);
                return x;
            }

            Field($"Dlvl {p.Level,-2}", theme.Label);
            Field($"HP {p.HP}/{p.MaxHP}", p.HP > p.MaxHP / 3 ? theme.Good : theme.Bad);
            Field($"Str {p.Str,-2}", theme.Text);
            Field($"Dex {p.Dex,-2}", theme.Text);
            Field($"Con {p.Con,-2}", theme.Text);
            Field($"AC {p.ArmorClass(),-3}", theme.Text);
            Field($"Gold {_g.CarryingGold(),-5}", theme.Gold);
            Field($"XP {p.XpForNext()}", theme.Dim);
            Field(_g.World != null ? _g.World.TimeString : $"Turn {_g.Turn}", theme.Dim);

            string loc = _g.Mode == GameMode.Overworld || _g.Mode == GameMode.TownMap
                ? _g.World.CurrentRegionName
                : (_g.Map != null ? _g.Map.LevelName : "");
            string danger = DangerLabel();

            // Right-aligned, laid out from the right edge so nothing can run past it.
            int rx = ox + w - 1;
            if (danger.Length > 0 && rx - danger.Length > x + 1)
            {
                rx -= danger.Length;
                _t.Write(rx, oy, danger, theme.Danger);
            }

            if (loc.Length > 0)
            {
                int lx = rx - loc.Length - 1;
                if (lx > x + 1)
                {
                    _t.Put(rx - 1, oy, ' ', theme.Dim);
                    _t.Write(lx, oy, loc, theme.StatusLocation);
                }
            }
        }

        string DangerLabel()
        {
            int worst = 0;
            for (int i = 0; i < _g.Monsters.Count; i++)
            {
                var m = _g.Monsters[i];
                if (_g.Map == null || !_g.Map.IsVisible(m.X, m.Y)) continue;
                worst = Math.Max(worst, m.Def.Level);
            }
            if (worst >= 12) return "[DEADLY]";
            if (worst >= 8) return "[DANGEROUS]";
            if (worst >= 4) return "[WARY]";
            return "";
        }

        // ------------------------------------------------------- message window

        void DrawMessageWindow(int ox, int oy, int w, int h)
        {
            var theme = Theme.Default;
            _t.FillRect(ox, oy, w, h, ' ', theme.Text, theme.Background);
            _t.HLine(ox, oy, w, theme.Rule);

            int count = h - 1;
            int start = Math.Max(0, _g.Log.Count - count);
            for (int i = 0; i < count; i++)
            {
                int idx = start + i;
                if (idx >= _g.Log.Count) break;
                _t.WriteClipped(ox + 1, oy + 1 + i, _g.Log[idx].Text, theme.MessageColor(_g.Log[idx].Kind), w - 2);
            }
        }

        // --------------------------------------------------------------- panels

        void DrawOverlays()
        {
            switch (State.Active)
            {
                case Panel.Inventory: DrawInventoryPanel(); break;
                case Panel.Help: DrawHelpPanel(); break;
                case Panel.History: DrawHistoryPanel(); break;
                case Panel.Discoveries: DrawDiscoveriesPanel(); break;
                case Panel.Character: DrawCharacterPanel(); break;
                case Panel.Travel: DrawTravelPanel(); break;
                case Panel.Shop: DrawShopPanel(); break;
            }
            if (PendingChoice.Active) DrawChoicePanel();
            if (_g.Mode == GameMode.GameOver) DrawDeathPanel();
            if (_g.Mode == GameMode.Won) DrawWinPanel();
        }

        void PanelRect(out int px, out int py, out int pw, out int ph, int wantW, int wantH)
        {
            var theme = Theme.Default;
            pw = Math.Min(wantW, _t.Width - 4);
            ph = Math.Min(wantH, _t.Height - 4);
            px = (_t.Width - pw) / 2;
            py = (_t.Height - ph) / 2;
            _t.FillRect(px, py, pw, ph, ' ', theme.Text, theme.Panel);
            _t.Box(px, py, pw, ph, theme.PanelFrame, true, theme.Panel);
        }

        void DrawInventoryPanel()
        {
            PanelRect(out int px, out int py, out int pw, out int ph, 66, 24);
            var theme = Theme.Default;
            var p = _g.Player;
            _t.Write(px + 2, py + 1, "Inventory", theme.Title, true);
            _t.Write(px + pw - 14, py + 1, "any key closes", theme.Dim);

            int x = px + 2, y = py + 3;
            int maxText = pw - 4;
            int maxRows = ph - 6;
            int shown = 0;

            for (int i = 0; i < p.Inventory.Count && shown < maxRows; i++)
            {
                var it = p.Inventory[i];
                string label = $"{(it.Def.Glyph == ' ' ? '?' : it.Def.Glyph)} {it.Name}";
                if (it.Quantity > 1) label += $" (x{it.Quantity})";
                if (it.RemainingCharges > 0 && (it.Def.Kind == ItemKind.Wand || it.Def.Kind == ItemKind.Potion || it.Def.Kind == ItemKind.Scroll))
                    label += $" [{it.RemainingCharges}]";
                Rgb c = (it.Def.Flags & ItemFlags.Cursed) != 0 ? theme.Bad
                      : (it.Def.Flags & ItemFlags.Blessed) != 0 ? theme.Good
                      : theme.ItemColor(it);
                _t.WriteClipped(x, y + shown, label, c, maxText);
                shown++;
            }
            if (shown < p.Inventory.Count) _t.Write(x, y + shown, $"...and {p.Inventory.Count - shown} more", theme.Dim);
            if (p.Inventory.Count == 0) _t.Write(x, y, "You are carrying nothing.", theme.Dim);

            int goldY = py + ph - 2;
            _t.Write(x, goldY, $"Gold {_g.CarryingGold()}", theme.Gold, true);
            _t.Write(x + 16, goldY, $"Weight {p.WeightCarried()}/{p.CarryingCapacity()}", p.IsOverloaded() ? theme.Bad : theme.Dim);
        }

        void DrawChoicePanel()
        {
            var theme = Theme.Default;
            int n = PendingChoice.Items.Count;
            int w = Math.Min(52, _t.Width - 4);
            int h = Math.Min(n + 4, _t.Height - 4);
            int px = (_t.Width - w) / 2, py = (_t.Height - h) / 2;
            _t.FillRect(px, py, w, h, ' ', theme.Text, theme.Panel);
            _t.Box(px, py, w, h, theme.PanelFrame, true, theme.Panel);
            _t.Write(px + 2, py + 1, PendingChoice.Prompt ?? "Choose", theme.Title, true);

            int visRows = h - 3;
            int start = Math.Max(0, State.ChoiceIndex - visRows + 1);
            for (int i = 0; i < visRows; i++)
            {
                int idx = start + i;
                if (idx >= n) break;
                var it = PendingChoice.Items[idx];
                bool sel = idx == State.ChoiceIndex;
                _t.WriteClipped(px + 2, py + 2 + i,
                    (sel ? "▶ " : "  ") + $"{(it.Def.Glyph == ' ' ? '?' : it.Def.Glyph)} {it.Name}",
                    sel ? theme.Cursor : theme.ItemColor(it), w - 4, sel);
            }
        }

        void DrawHelpPanel()
        {
            PanelRect(out int px, out int py, out int pw, out int ph, 74, 26);
            var theme = Theme.Default;
            _t.Write(px + 2, py + 1, "Commands", theme.Title, true);
            _t.Write(px + pw - 14, py + 1, "any key closes", theme.Dim);

            string[,] rows = {
                { "hjklyubn", "move (vi keys, arrows, numpad)" },
                { "y u", "diagonals" },
                { ".", "wait one turn" },
                { ">  <", "descend / climb" },
                { "g  d", "pick up / drop" },
                { "i", "inventory" },
                { "a", "apply a tool (pick-axe, lock pick)" },
                { "w  W  T", "wield / wear / take off armour" },
                { "P  R", "put on / remove a ring" },
                { "r  z", "read a scroll / zap a wand" },
                { "e", "eat" },
                { "f", "fire at a target" },
                { "k", "kick or attack ahead" },
                { "u  D", "use a key / open a door" },
                { "s", "search for traps and doors" },
                { "l  x  X", "look / inspect / swap with" },
                { "O", "travel on the overworld" },
                { "m", "toggle the minimap" },
                { "c  D", "character sheet / discoveries" },
                { "H", "message history" },
                { "?", "this help" },
            };

            int x = px + 2, y = py + 3;
            int maxRows = ph - 4;
            for (int i = 0; i < maxRows; i++)
            {
                int line = i + State.ScrollOffset;
                if (line >= rows.GetLength(0)) break;
                int c = _t.WriteClipped(x, y + i, rows[line, 0].PadRight(14), theme.Label, 14);
                _t.WriteClipped(c, y + i, rows[line, 1], theme.Text, pw - 18);
            }
        }

        void DrawHistoryPanel()
        {
            PanelRect(out int px, out int py, out int pw, out int ph, 78, 24);
            var theme = Theme.Default;
            _t.Write(px + 2, py + 1, "Message history", theme.Title, true);
            int y = py + 3, rows = ph - 4;
            int start = Math.Max(0, _g.Transcript.Count - rows - State.ScrollOffset);
            for (int i = 0; i < rows; i++)
            {
                int idx = start + i;
                if (idx >= _g.Transcript.Count) break;
                _t.WriteClipped(px + 2, y + i, _g.Transcript[idx].Text, theme.MessageColor(_g.Transcript[idx].Kind), pw - 4);
            }
        }

        void DrawDiscoveriesPanel()
        {
            PanelRect(out int px, out int py, out int pw, out int ph, 60, 18);
            var theme = Theme.Default;
            var p = _g.Player;
            _t.Write(px + 2, py + 1, "Discoveries", theme.Title, true);
            int y = py + 3, x = px + 2;
            _t.Write(x, y++, $"Deepest level   {p.MaxDepth}", theme.Text);
            _t.Write(x, y++, $"Kills           {p.Kills}", theme.Text);
            _t.Write(x, y++, $"Gold carried    {_g.CarryingGold()}", theme.Gold);
            _t.Write(x, y++, $"Levels mapped   {_g.Dungeon.LevelCount}", theme.Text);
            _t.Write(x, y++, $"Regions seen    {_g.RegionsSeen()}", theme.Text);
            _t.Write(x, y++, $"Seed            {_g.Rng.Seed}", theme.Dim);
        }

        void DrawCharacterPanel()
        {
            PanelRect(out int px, out int py, out int pw, out int ph, 56, 26);
            var theme = Theme.Default;
            var p = _g.Player;
            var role = Roles.Find(p.RoleId);
            _t.Write(px + 2, py + 1, "Character", theme.Title, true);
            int y = py + 3, x = px + 2;
            _t.WriteClipped(x, y++, p.LongDescription, theme.Text, pw - 4, true); y++;
            _t.WriteClipped(x, y++, $"Role {role.Name} — {role.Description}", theme.Text, pw - 4);
            _t.WriteClipped(x, y++, p.AttributeLine(), theme.Text, pw - 4); y++;
            _t.WriteClipped(x, y++, $"Alignment {p.AlignmentString}", theme.Dim, pw - 4);
            _t.WriteClipped(x, y++, $"Title {p.Title}", theme.Text, pw - 4);
            if (p.PendingAdvances > 0)
                _t.WriteClipped(x, y++, $"{p.PendingAdvances} advancement(s) unspent", theme.Good, pw - 4);
            if (p.AdvancesTaken.Count > 0)
                _t.WriteClipped(x, y++, "Taken: " + string.Join(", ", p.AdvancesTaken.ToArray()), theme.Dim, pw - 4);
            y++;
            _t.Write(x, y++, "Skills", theme.Label);
            foreach (var kv in p.Skills) _t.Write(x + 12, y++, $"{kv.Key,-9}{kv.Value}", theme.Text);
            y++;
            _t.WriteClipped(x, y++, $"Now: {(_g.Map != null ? _g.Map.LevelName : _g.World.CurrentRegionName)}", theme.Text, pw - 4);
            _t.WriteClipped(x, y++, $"Time: {_g.World.TimeString}", theme.Dim, pw - 4);
        }

        void DrawTravelPanel()
        {
            var theme = Theme.Default;
            int w = Math.Min(46, _t.Width - 4), h = 9;
            int px = (_t.Width - w) / 2, py = 1;
            _t.FillRect(px, py, w, h, ' ', theme.Text, theme.Panel);
            _t.Box(px, py, w, h, theme.PanelFrame, true, theme.Panel);
            _t.Write(px + 2, py + 1, "Travel", theme.Title, true);
            _t.Write(px + 2, py + 3, "Arrow keys choose a destination.", theme.Text);
            _t.Write(px + 2, py + 4, "Enter confirms.  Esc cancels.", theme.Dim);
            _t.Write(px + 2, py + 5, $"You are in {_g.World.CurrentRegionName}", theme.Warn);
            _t.Write(px + 2, py + 6, $"Cursor {_g.UiState.TravelX},{_g.UiState.TravelY}", theme.Info);
        }

        void DrawShopPanel()
        {
            var theme = Theme.Default;
            var shop = _g.CurrentShop;
            PanelRect(out int px, out int py, out int pw, out int ph, 66, 24);
            string title = !string.IsNullOrEmpty(_g.ShopName) ? _g.ShopName
                : (shop != null && !string.IsNullOrEmpty(shop.Name) ? shop.Name : "Shop");
            _t.Write(px + 2, py + 1, title, theme.Title, true);
            _t.Write(px + pw - 14, py + 1, "Esc leaves", theme.Dim);

            // Defensive: the panel flag can outlive the shop (a stale save, a test
            // that only sets Active). Never throw here; say the keeper is gone.
            if (shop == null || !_g.InShop)
            {
                _t.Write(px + 2, py + 3, "The shopkeeper is gone.", theme.Dim);
                return;
            }

            int goldY = py + 2;
            _t.Write(px + 2, goldY, $"Your gold {_g.CarryingGold()}", theme.Gold, true);
            _t.Write(px + 24, goldY, $"Shop gold {shop.Gold}", theme.Dim);

            int y0 = py + 4;
            int maxRows = ph - 7;
            if (maxRows < 1) maxRows = 1;
            int count = shop.Stock.Count;
            if (count == 0)
            {
                _t.Write(px + 2, y0, "Sold out.", theme.Dim);
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
                    char glyph = it.Def.Glyph == ' ' ? '?' : it.Def.Glyph;
                    string left = (cur ? "▶ " : "  ") + glyph + " " + it.Name;
                    if (it.Quantity > 1) left += $" (x{it.Quantity})";
                    string right = price + "g";
                    int priceX = px + pw - 2 - right.Length;
                    int leftMax = Math.Max(1, priceX - (px + 2) - 1);
                    _t.WriteClipped(px + 2, y0 + i, left, cur ? theme.Cursor : theme.ItemColor(it), leftMax, cur);
                    Rgb priceColor = cur ? theme.Cursor
                        : (_g.CarryingGold() >= price ? theme.Gold : theme.Dim);
                    _t.Write(priceX, y0 + i, right, priceColor, cur);
                }
            }

            _t.WriteClipped(px + 2, py + ph - 2, "Enter/b buy - s sell - hjkl move - Esc leave", theme.Dim, pw - 4);
        }

        void DrawDeathPanel()
        {
            var theme = Theme.Default;
            PanelRect(out int px, out int py, out int pw, out int ph, 46, 12);
            string[] lines = {
                "You have died.",
                "",
                "No bones will be found here.",
                "Press R to begin again.",
                "Press Q to quit."
            };
            for (int i = 0; i < lines.Length && i < ph - 2; i++)
            {
                int pad = Math.Max(1, (pw - lines[i].Length) / 2);
                _t.Write(px + pad, py + 2 + i, lines[i], i == 0 ? theme.Bad : theme.Text, i == 0);
            }
        }

        void DrawWinPanel()
        {
            var theme = Theme.Default;
            PanelRect(out int px, out int py, out int pw, out int ph, 50, 12);
            string[] lines = {
                "You escape with your life.",
                "",
                $"Deepest level  {_g.Player.MaxDepth}",
                $"Kills          {_g.Player.Kills}",
                "Press R for another run."
            };
            for (int i = 0; i < lines.Length && i < ph - 2; i++)
            {
                int pad = Math.Max(1, (pw - lines[i].Length) / 2);
                _t.Write(px + pad, py + 2 + i, lines[i], i == 0 ? theme.Good : theme.Text, i == 0);
            }
        }

        string TitleFor()
        {
            switch (_g.Mode)
            {
                case GameMode.Overworld: return _g.World.CurrentRegionName;
                case GameMode.TownMap: return (_g.Town != null ? _g.Town.Name : "Town");
                case GameMode.Dungeon: return _g.Map != null ? _g.Map.LevelName : "";
                default: return "OSSUARY";
            }
        }
    }
}