using System.Collections.Generic;
using UnityEngine;

namespace Ossuary.Gameplay
{
    /// <summary>
    /// Turns a physical key into one of the command strings understood by
    /// <see cref="Ossuary.Core.Commands.Execute(string)"/>.
    ///
    /// The router is deliberately dumb and stateless: no game state, no
    /// allocation, one call in, one string (or null) out. Everything that needs
    /// to know about panels, targets or choices lives in GameApp.
    ///
    /// IMPORTANT: the returned strings are the *real* case labels from
    /// Commands.Execute, which are single characters ("g" for pickup, "f" for
    /// fire, "k" for kick, "l" for look, "u" for use-key, "D" for open, "s" for
    /// search, "c" for character, "D2" for discoveries, "O" for travel,
    /// "m" for minimap, "H" for history, "?" for help, "save", "quit"), plus the
    /// "move-nw" style direction verbs. They are *not* long names like "pickup"
    /// or "search-area"; Commands has no such cases.
    ///
    /// Modifier handling: Unity reports KeyCode.K for both "k" and "K", so the
    /// shift-sensitive bindings need the modifier state. <see cref="Translate(KeyCode)"/>
    /// is the single entry point and assumes no modifiers; the overload
    /// <see cref="Translate(KeyCode, bool, bool)"/> is what GameApp actually
    /// calls, and it is what resolves the vi-vs-verb conflicts documented below.
    /// </summary>
    public static class InputRouter
    {
        /// <summary>Every key the router reacts to, including the keys GameApp handles itself.</summary>
        public static readonly KeyCode[] Keys =
        {
            // movement
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.Keypad8, KeyCode.Keypad2, KeyCode.Keypad4, KeyCode.Keypad6,
            KeyCode.Keypad7, KeyCode.Keypad9, KeyCode.Keypad1, KeyCode.Keypad3,
            KeyCode.Keypad5, KeyCode.Keypad0,
            KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Y, KeyCode.U, KeyCode.B, KeyCode.N,

            // verbs
            KeyCode.Period, KeyCode.Comma, KeyCode.Greater, KeyCode.Less, KeyCode.Slash,
            KeyCode.I, KeyCode.G, KeyCode.D, KeyCode.A, KeyCode.F, KeyCode.Q, KeyCode.E,
            KeyCode.W, KeyCode.T, KeyCode.P, KeyCode.R, KeyCode.Z, KeyCode.S,
            KeyCode.O, KeyCode.M, KeyCode.C, KeyCode.X, KeyCode.V,
            KeyCode.F5, KeyCode.F6,

            // handled directly by GameApp (still polled so they are never swallowed)
            KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space, KeyCode.Escape,
        };

        /// <summary>Every key the router listens for. Use this to poll, so no mapped key is left unread.</summary>
        public static IEnumerable<KeyCode> AllKeys() => Keys;

        /// <summary>Single entry point, no modifiers assumed.</summary>
        public static string Translate(KeyCode key) => Translate(key, false, false);

        /// <summary>
        /// Modifier-aware translation. GameApp passes the live shift/ctrl state,
        /// which is the only way to tell "k" (move north) from "K" (kick) since
        /// Unity reports the same KeyCode for both.
        /// </summary>
        public static string Translate(KeyCode key, bool shift, bool ctrl)
        {
            switch (key)
            {
                // -------------------------------------------------- arrows / numpad
                case KeyCode.UpArrow: return "move-n";
                case KeyCode.DownArrow: return "move-s";
                case KeyCode.LeftArrow: return "move-w";
                case KeyCode.RightArrow: return "move-e";

                case KeyCode.Keypad8: return "move-n";
                case KeyCode.Keypad2: return "move-s";
                case KeyCode.Keypad4: return "move-w";
                case KeyCode.Keypad6: return "move-e";
                case KeyCode.Keypad7: return "move-nw";
                case KeyCode.Keypad9: return "move-ne";
                case KeyCode.Keypad1: return "move-sw";
                case KeyCode.Keypad3: return "move-se";
                case KeyCode.Keypad5: return ".";
                case KeyCode.Keypad0: return ".";

                // ---------------------------------------------------------------- vi
                // Vi movement wins over AD&D letter verbs on the unshifted key,
                // because a roguelike player reaches for hjkl first:
                //   h j k l   west south north east
                //   y u b n   north-west north-east south-west south-east
                // The displaced verbs live on shift: K kick, L look, U use-key.
                case KeyCode.H: return shift ? "H" : "move-w";
                case KeyCode.J: return "move-s";
                case KeyCode.K: return shift ? "k" : "move-n";
                case KeyCode.L: return shift ? "l" : "move-e";
                case KeyCode.Y: return "move-nw";
                case KeyCode.U: return shift ? "u" : "move-ne";
                case KeyCode.B: return "move-sw";
                case KeyCode.N: return "move-se";

                // ------------------------------------------------------------- verbs
                case KeyCode.Period: return ".";          // wait
                case KeyCode.Comma: return "g";            // pickup alias (Commands uses "g")
                case KeyCode.Greater: return ">";          // descend
                case KeyCode.Less: return "<";             // ascend / flee an encounter
                case KeyCode.Slash: return "?";            // help ("?" needs shift on most layouts)

                case KeyCode.I: return "i";                // inventory
                case KeyCode.G: return "g";                // pickup
                case KeyCode.D: return shift ? "D" : "d";  // open door / drop
                case KeyCode.A: return "a";                // apply tool
                case KeyCode.F: return "f";                // fire at a target
                case KeyCode.Q: return ctrl ? "quit" : "f"; // Ctrl-Q quits, Q is a fire alias
                case KeyCode.E: return "e";                // eat
                case KeyCode.S: return "s";                // search for traps and doors

                case KeyCode.W: return shift ? "W" : "w";  // wear / wield
                case KeyCode.T: return shift ? "T" : null; // take off armour (shift)
                case KeyCode.P: return shift ? "P" : "g";   // put on ring (shift) / pickup alias
                case KeyCode.R: return shift ? "R" : "r";  // remove ring / read
                case KeyCode.Z: return "z";                // zap
                case KeyCode.X: return shift ? "X" : "x";  // swap with monster / inspect
                case KeyCode.V: return "l";                // look (Commands spells this "l")
                case KeyCode.F5: return "save";            // prints the seed, which is the save
                case KeyCode.F6: return "D2";              // discoveries

                case KeyCode.O: return "O";                // travel on the overworld
                case KeyCode.M: return "m";                // toggle minimap
                case KeyCode.C: return "c";                // character sheet

                // Keys GameApp interprets directly; null here means "not a command".
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                case KeyCode.Escape:
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Grid step for the keys that move something (player, target cursor,
        /// travel cursor, list selection). Returns false for everything else.
        /// Shifted vi keys fall back to their unshifted direction so that moving
        /// a cursor still works if a player holds shift by accident.
        /// </summary>
        public static bool TryStep(KeyCode key, out int dx, out int dy)
        {
            dx = 0; dy = 0;
            string cmd = Translate(key);
            if (cmd == null) return false;

            switch (cmd)
            {
                case "move-n": dy = -1; return true;
                case "move-s": dy = 1; return true;
                case "move-w": dx = -1; return true;
                case "move-e": dx = 1; return true;
                case "move-nw": dx = -1; dy = -1; return true;
                case "move-ne": dx = 1; dy = -1; return true;
                case "move-sw": dx = -1; dy = 1; return true;
                case "move-se": dx = 1; dy = 1; return true;
                default: return false;
            }
        }

        /// <summary>Human-readable key list, for a help/debug screen.</summary>
        public static IEnumerable<string> Describe()
        {
            yield return "h j k l / arrows / numpad  move (shift-K kick, shift-L look, shift-U use key)";
            yield return "y u b n  diagonals";
            yield return ".  wait      > <  descend / ascend";
            yield return "g  pick up   d  drop      s  search";
            yield return "i  inventory a  apply    e  eat";
            yield return "w  wield     W  wear     T  take off armour";
            yield return "P  ring on   R  ring off  r  read   z  zap";
            yield return "f  fire      Q  fire     shift-D  open door";
            yield return "x  inspect   X  swap with monster";
            yield return "v  look (also shift-L)";
            yield return "O  travel    m  minimap  c  character  F6  discoveries";
            yield return "H  history   ?  help     F5  seed  Ctrl-Q  quit";
            yield return "Enter commit  Esc cancel/close";
        }
    }
}