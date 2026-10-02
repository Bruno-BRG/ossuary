using System;
using System.Collections.Generic;
using System.Text;

namespace Ossuary.Core
{
    /// <summary>One rebindable verb: its label, the key the game logic understands, and the default keys.</summary>
    public sealed class KeyAction
    {
        public readonly string Id, Label, Group;
        /// <summary>The canonical physical key (code, shift, ctrl) the rest of the input pipeline expects.</summary>
        public readonly string Code, Char;
        public readonly bool Shift, Ctrl;
        public readonly string[] Defaults;

        public KeyAction(string id, string label, string group, string canonical, string[] defaults, string ch = "")
        {
            Id = id; Label = label; Group = group; Defaults = defaults; Char = ch;
            string c = canonical;
            if (c.StartsWith("Ctrl+")) { Ctrl = true; c = c.Substring(5); }
            if (c.StartsWith("Shift+")) { Shift = true; c = c.Substring(6); }
            Code = c;
        }
    }

    /// <summary>
    /// Physical key to action mapping, as data. A rebind never changes what the game does for
    /// a key: <see cref="Resolve"/> rewrites the physical key into the action's canonical key,
    /// so saved run logs (which store canonical keys) replay under any bindings.
    /// Arrows, Enter, Escape, Space and Tab are reserved: they always work and cannot be bound.
    /// </summary>
    public sealed class KeyBindings
    {
        static readonly string[] Reserved =
            { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Enter", "NumpadEnter", "Escape", "Space", "Tab", "F11" };

        public static readonly KeyAction[] Actions =
        {
            new KeyAction("move-n", "Move north", "Movement", "ArrowUp", new[] { "ArrowUp", "Numpad8", "KeyK" }),
            new KeyAction("move-s", "Move south", "Movement", "ArrowDown", new[] { "ArrowDown", "Numpad2", "KeyJ" }),
            new KeyAction("move-w", "Move west", "Movement", "ArrowLeft", new[] { "ArrowLeft", "Numpad4", "KeyH" }),
            new KeyAction("move-e", "Move east", "Movement", "ArrowRight", new[] { "ArrowRight", "Numpad6", "KeyL" }),
            new KeyAction("move-nw", "Move north-west", "Movement", "Numpad7", new[] { "Numpad7", "KeyY" }),
            new KeyAction("move-ne", "Move north-east", "Movement", "Numpad9", new[] { "Numpad9", "KeyU" }),
            new KeyAction("move-sw", "Move south-west", "Movement", "Numpad1", new[] { "Numpad1", "KeyB" }),
            new KeyAction("move-se", "Move south-east", "Movement", "Numpad3", new[] { "Numpad3", "KeyN" }),
            new KeyAction("wait", "Wait a turn", "Movement", "Period", new[] { "Period", "Numpad5", "Numpad0" }),
            new KeyAction("descend", "Descend stairs", "Movement", "Shift+Period", new[] { "Shift+Period" }, ">"),
            new KeyAction("climb", "Climb stairs", "Movement", "Shift+Comma", new[] { "Shift+Comma" }, "<"),
            new KeyAction("search", "Search", "Movement", "KeyS", new[] { "KeyS" }),
            new KeyAction("travel", "Travel (overworld)", "Movement", "KeyO", new[] { "KeyO" }),

            new KeyAction("get", "Pick up", "Items", "KeyG", new[] { "KeyG", "Comma", "KeyP" }),
            new KeyAction("drop", "Drop", "Items", "KeyD", new[] { "KeyD" }),
            new KeyAction("inventory", "Inventory", "Items", "KeyI", new[] { "KeyI" }),
            new KeyAction("apply", "Apply a tool", "Items", "KeyA", new[] { "KeyA" }),
            new KeyAction("wield", "Wield weapon", "Items", "KeyW", new[] { "KeyW" }),
            new KeyAction("wear", "Wear armour", "Items", "Shift+KeyW", new[] { "Shift+KeyW" }),
            new KeyAction("takeoff", "Take off armour", "Items", "Shift+KeyT", new[] { "Shift+KeyT" }),
            new KeyAction("ring-on", "Put on ring", "Items", "Shift+KeyP", new[] { "Shift+KeyP" }),
            new KeyAction("ring-off", "Remove ring", "Items", "Shift+KeyR", new[] { "Shift+KeyR" }),
            new KeyAction("read", "Read scroll", "Items", "KeyR", new[] { "KeyR" }),
            new KeyAction("zap", "Zap wand", "Items", "KeyZ", new[] { "KeyZ" }),
            new KeyAction("cast", "Cast a spell", "Items", "Shift+KeyZ", new[] { "Shift+KeyZ" }),
            new KeyAction("ability", "Use an ability", "Combat", "Shift+KeyV", new[] { "Shift+KeyV" }),
            new KeyAction("quaff", "Drink a potion", "Items", "Shift+KeyQ", new[] { "Shift+KeyQ" }),
            new KeyAction("eat", "Eat", "Items", "KeyE", new[] { "KeyE" }),

            new KeyAction("fire", "Fire at target", "Combat", "KeyF", new[] { "KeyF", "KeyQ" }),
            new KeyAction("kick", "Kick / attack ahead", "Combat", "Shift+KeyK", new[] { "Shift+KeyK" }),
            new KeyAction("use", "Use key", "Combat", "Shift+KeyU", new[] { "Shift+KeyU" }),
            new KeyAction("door", "Open door", "Combat", "Shift+KeyD", new[] { "Shift+KeyD" }),
            new KeyAction("look", "Look", "Combat", "Shift+KeyL", new[] { "Shift+KeyL", "KeyV" }),
            new KeyAction("inspect", "Inspect", "Combat", "KeyX", new[] { "KeyX" }),
            new KeyAction("swap", "Swap with", "Combat", "Shift+KeyX", new[] { "Shift+KeyX" }),

            new KeyAction("character", "Character sheet", "Windows", "KeyC", new[] { "KeyC" }),
            new KeyAction("advance", "Spend advancements", "Windows", "Shift+KeyC", new[] { "Shift+KeyC" }),
            new KeyAction("discoveries", "Discoveries", "Windows", "F6", new[] { "F6" }),
            new KeyAction("history", "Message history", "Windows", "Shift+KeyH", new[] { "Shift+KeyH" }),
            new KeyAction("minimap", "Toggle minimap", "Windows", "KeyM", new[] { "KeyM" }),
            new KeyAction("help", "Help", "Windows", "Slash", new[] { "Slash", "Shift+Slash", "IntlRo", "Shift+IntlRo" }),
            new KeyAction("menu", "Menu / options", "Windows", "F2", new[] { "F2" }),
            new KeyAction("crt", "Cycle CRT", "Windows", "F3", new[] { "F3" }),
            new KeyAction("theme", "Cycle colour theme", "Windows", "F4", new[] { "F4" }),
            new KeyAction("quicksave", "Quick save", "Windows", "F5", new[] { "F5" }),
            new KeyAction("quit", "Abandon run", "Windows", "Ctrl+KeyQ", new[] { "Ctrl+KeyQ" }),
        };

        // Declared last among the statics: the constructor needs Actions and DefaultKeys.
        public static readonly KeyBindings Current = new KeyBindings();

        readonly List<string>[] _keys = new List<string>[Actions.Length];
        Dictionary<string, int> _table;

        /// <summary>Bumped on every change, so the host knows to persist.</summary>
        public int Version;

        public KeyBindings() { ResetAll(); Version = 0; }

        public static string Phys(string code, bool shift, bool ctrl) => (ctrl ? "Ctrl+" : "") + (shift ? "Shift+" : "") + code;
        public static bool IsReserved(string phys) => Array.IndexOf(Reserved, phys) >= 0;
        static bool IsModifier(string code) => code.StartsWith("Shift") || code.StartsWith("Control") || code.StartsWith("Alt") || code.StartsWith("Meta");

        public IList<string> KeysOf(int action) => _keys[action];

        public void ResetAll()
        {
            for (int i = 0; i < Actions.Length; i++) _keys[i] = new List<string>(Actions[i].Defaults);
            _table = null; Version++;
        }

        public void Reset(int action) { _keys[action] = new List<string>(Actions[action].Defaults); _table = null; Version++; }

        /// <summary>Clears an action's own keys; reserved keys (arrows) stay so menus remain reachable.</summary>
        public void Unbind(int action)
        {
            _keys[action].RemoveAll(k => !IsReserved(k));
            _table = null; Version++;
        }

        /// <summary>
        /// Binds a physical key to an action, replacing the action's other keys (reserved ones stay)
        /// and taking the key from whichever action had it. Returns a short note, or null if refused.
        /// </summary>
        public string Rebind(int action, string code, bool shift, bool ctrl, out bool ok)
        {
            ok = false;
            if (string.IsNullOrEmpty(code) || IsModifier(code)) return null;
            string phys = Phys(code, shift, ctrl);
            if (IsReserved(phys)) return KeyName(phys) + " is reserved for menus.";
            string note = null;
            for (int i = 0; i < Actions.Length; i++)
                if (i != action && _keys[i].Remove(phys)) note = "Taken from \"" + Actions[i].Label + "\".";
            _keys[action].RemoveAll(k => !IsReserved(k));
            _keys[action].Add(phys);
            _table = null; Version++; ok = true;
            return note ?? "Bound to " + KeyName(phys) + ".";
        }

        void Build()
        {
            _table = new Dictionary<string, int>();
            for (int i = 0; i < Actions.Length; i++) foreach (var k in _keys[i]) _table[k] = i;
        }

        static readonly HashSet<string> DefaultKeys = BuildDefaults();
        static HashSet<string> BuildDefaults()
        {
            var set = new HashSet<string>();
            foreach (var a in Actions) foreach (var k in a.Defaults) if (!IsReserved(k)) set.Add(k);
            return set;
        }

        /// <summary>
        /// Rewrites a physical key into the canonical key of the action it is bound to. A default key
        /// that was rebound away resolves to "None" (it must not keep doing its old job); any other key
        /// (Enter, Escape, panel keys) passes through untouched.
        /// </summary>
        public void Resolve(ref string code, ref string key, ref bool shift, ref bool ctrl)
        {
            if (_table == null) Build();
            string phys = Phys(code ?? "", shift, ctrl);
            // '?' and '/' sit behind AltGr or on other physical keys on many layouts (ABNT2 has
            // them on IntlRo and AltGr+W/Q): honour the character while help is on its defaults.
            if ((key == "?" || key == "/") && code != "Slash" && code != "IntlRo")
            {
                int help = Array.FindIndex(Actions, x => x.Id == "help");
                if (_keys[help].Contains("Slash")) { code = "Slash"; shift = false; ctrl = false; return; }
                code = "None"; key = ""; return;
            }
            if (_table.TryGetValue(phys, out int a))
            {
                var act = Actions[a];
                code = act.Code; shift = act.Shift; ctrl = act.Ctrl;
                key = act.Char.Length > 0 ? act.Char : (key ?? "");
                return;
            }
            // Layouts where '>' and '<' sit on other physical keys: honour the character while
            // the stairs are still on their defaults.
            if (key == ">" || key == "<")
            {
                string ch = key;
                int idx = Array.FindIndex(Actions, x => x.Char == ch);
                if (_keys[idx].Contains(Actions[idx].Defaults[0])) return;
                code = "None"; key = ""; return;
            }
            if (DefaultKeys.Contains(phys)) { code = "None"; key = ""; shift = false; ctrl = false; }
        }

        // ------------------------------------------------------------------ names

        public static string KeyName(string phys)
        {
            string p = phys, prefix = "";
            if (p.StartsWith("Ctrl+")) { prefix += "Ctrl+"; p = p.Substring(5); }
            if (p.StartsWith("Shift+")) { prefix += "⇧"; p = p.Substring(6); }
            string n;
            if (p.StartsWith("Key") && p.Length == 4) n = p.Substring(3);
            else if (p.StartsWith("Digit") && p.Length == 6) n = p.Substring(5);
            else if (p.StartsWith("Numpad") && p.Length == 7) n = "Num" + p[6];
            else switch (p)
            {
                case "ArrowUp": n = "↑"; break;
                case "ArrowDown": n = "↓"; break;
                case "ArrowLeft": n = "←"; break;
                case "ArrowRight": n = "→"; break;
                case "Period": n = "."; break;
                case "Comma": n = ","; break;
                case "Slash": n = "/"; break;
                case "Semicolon": n = ";"; break;
                case "Quote": n = "'"; break;
                case "Minus": n = "-"; break;
                case "Equal": n = "="; break;
                case "BracketLeft": n = "["; break;
                case "BracketRight": n = "]"; break;
                case "Backslash": n = "\\"; break;
                case "Backquote": n = "`"; break;
                case "Enter": n = "Enter"; break;
                case "Space": n = "Space"; break;
                default: n = p; break;
            }
            return prefix + n;
        }

        public string KeysLabel(int action)
        {
            var sb = new StringBuilder();
            foreach (var k in _keys[action])
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(KeyName(k));
            }
            return sb.Length == 0 ? "-" : sb.ToString();
        }

        // ------------------------------------------------------------ persistence

        /// <summary>Only what differs from the defaults: "id=Key1,Key2;id=".</summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Actions.Length; i++)
            {
                if (SameAsDefault(i)) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(Actions[i].Id).Append('=').Append(string.Join(",", _keys[i]));
            }
            return sb.ToString();
        }

        bool SameAsDefault(int i)
        {
            var d = Actions[i].Defaults;
            if (d.Length != _keys[i].Count) return false;
            for (int k = 0; k < d.Length; k++) if (d[k] != _keys[i][k]) return false;
            return true;
        }

        public void Load(string data)
        {
            ResetAll();
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var part in data.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    int idx = Array.FindIndex(Actions, a => a.Id == part.Substring(0, eq));
                    if (idx < 0) continue;
                    var list = new List<string>();
                    foreach (var k in part.Substring(eq + 1).Split(','))
                        if (k.Length > 0 && k.Length < 32 && !list.Contains(k)) list.Add(k);
                    _keys[idx] = list;
                }
                // A key may belong to one action only; the first claim in table order wins.
                var seen = new HashSet<string>();
                for (int i = 0; i < Actions.Length; i++) _keys[i].RemoveAll(k => !seen.Add(k));
            }
            _table = null; Version++;
        }
    }

    /// <summary>Volume sliders, 0-10. There is no audio yet; the settings exist so the menu and saved
    /// preferences are ready when sound lands.</summary>
    public sealed class AudioSettings
    {
        public static readonly AudioSettings Current = new AudioSettings();
        public int Master = 8, Music = 6, Effects = 8, Version;
        public const int Max = 10;

        public int Get(int channel) => channel == 0 ? Master : channel == 1 ? Music : Effects;
        public void Change(int channel, int delta)
        {
            int v = Math.Max(0, Math.Min(Max, Get(channel) + delta));
            if (channel == 0) Master = v; else if (channel == 1) Music = v; else Effects = v;
            Version++;
        }
        public static string Name(int channel) => channel == 0 ? "Master" : channel == 1 ? "Music" : "Effects";
        public string Serialize() => Master + "," + Music + "," + Effects;
        public void Load(string data)
        {
            var p = (data ?? "").Split(',');
            if (p.Length == 3 && int.TryParse(p[0], out int a) && int.TryParse(p[1], out int b) && int.TryParse(p[2], out int c))
            { Master = Math.Max(0, Math.Min(Max, a)); Music = Math.Max(0, Math.Min(Max, b)); Effects = Math.Max(0, Math.Min(Max, c)); }
            Version++;
        }
    }
}
