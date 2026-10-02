using System;
using System.Collections.Generic;

namespace Ossuary.Desktop
{
    // DOM physical codes: independent of keyboard layout and NumLock.
    public static class Input
    {
        static readonly Dictionary<string, string> Movement = new Dictionary<string, string>
        {
            ["ArrowUp"] = "move-n", ["ArrowDown"] = "move-s",
            ["ArrowLeft"] = "move-w", ["ArrowRight"] = "move-e",
            ["Numpad8"] = "move-n", ["Numpad2"] = "move-s",
            ["Numpad4"] = "move-w", ["Numpad6"] = "move-e",
            ["Numpad7"] = "move-nw", ["Numpad9"] = "move-ne",
            ["Numpad1"] = "move-sw", ["Numpad3"] = "move-se",
            ["KeyH"] = "move-w", ["KeyJ"] = "move-s", ["KeyK"] = "move-n",
            ["KeyL"] = "move-e", ["KeyY"] = "move-nw", ["KeyU"] = "move-ne",
            ["KeyB"] = "move-sw", ["KeyN"] = "move-se",
        };

        public static string Translate(string code, string key, bool shift, bool ctrl)
        {
            if (code == "KeyQ" && ctrl) return "quit";
            if (key == ">" || key == "<") return key;
            if (Movement.TryGetValue(code ?? "", out var move))
            {
                if (shift && code == "KeyH") return "H";
                if (shift && code == "KeyK") return "k";
                if (shift && code == "KeyL") return "l";
                if (shift && code == "KeyU") return "u";
                return move;
            }
            switch (code)
            {
                case "Period": return shift ? ">" : ".";
                case "Comma": return shift ? "<" : "g";
                case "Numpad5": case "Numpad0": return ".";
                case "Slash": return "?";
                case "KeyI": return "i";
                case "KeyG": return "g";
                case "KeyD": return shift ? "D" : "d";
                case "KeyA": return shift ? "disarm" : "a";
                case "KeyF": return "f";
                case "KeyQ": return shift ? "q" : "f";
                case "KeyE": return shift ? "drink" : "e";
                case "KeyS": return shift ? "rest" : "s";
                case "KeyW": return shift ? "W" : "w";
                case "KeyT": return shift ? "T" : "explore";
                case "Backquote": return "stairs";
                case "KeyP": return shift ? "P" : "g";
                case "KeyR": return shift ? "R" : "r";
                case "KeyZ": return shift ? "Z" : "z";
                case "KeyX": return shift ? "X" : "x";
                case "KeyV": return shift ? "V" : "l";
                case "KeyO": return "O";
                case "KeyM": return "m";
                case "KeyC": return shift ? "C" : "c";
                case "F2": return "settings";
                case "F3": return "crt";
                case "F4": return "theme";
                case "F5": return "save";
                case "F6": return "D2";
                default: return null;
            }
        }

        public static bool Step(string code, out int dx, out int dy)
        {
            dx = dy = 0;
            if (!Movement.TryGetValue(code ?? "", out var move)) return false;
            switch (move)
            {
                case "move-n": dy = -1; break;
                case "move-s": dy = 1; break;
                case "move-w": dx = -1; break;
                case "move-e": dx = 1; break;
                case "move-nw": dx = dy = -1; break;
                case "move-ne": dx = 1; dy = -1; break;
                case "move-sw": dx = -1; dy = 1; break;
                case "move-se": dx = dy = 1; break;
            }
            return true;
        }
    }
}

