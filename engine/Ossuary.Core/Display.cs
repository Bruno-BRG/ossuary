using System;

namespace Ossuary.Core
{
    public enum ThemePreset { Ossuary, Amber, Phosphor, Cga }

    public enum CrtLevel { Off, Subtle, Strong }

    /// <summary>
    /// User-facing display options: colour theme and CRT strength. Plain data, no
    /// rendering framework, so the settings panel can be drawn and tested headlessly. The runtime
    /// desktop frontend persists it and pushes <see cref="Crt"/> into the renderer.
    /// </summary>
    public sealed class DisplaySettings
    {
        /// <summary>
        /// The one live instance. Display options belong to the player, not to a run, so a
        /// new Game must not reset them.
        /// </summary>
        public static readonly DisplaySettings Current = new DisplaySettings();

        public ThemePreset Preset = ThemePreset.Ossuary;
        public CrtLevel Crt = CrtLevel.Subtle;

        /// <summary>Interface and story language. Mirrors <see cref="Loc.Current"/>.</summary>
        public Lang Language = Lang.En;

        /// <summary>Text size: 0 picks the largest that fits, otherwise a fixed 1x, 2x or 3x.</summary>
        public int Scale;

        /// <summary>Bumped on every change so the runtime knows to re-apply.</summary>
        public int Version;

        public void CycleTheme(int dir)
        {
            int n = Enum.GetValues(typeof(ThemePreset)).Length;
            Preset = (ThemePreset)(((int)Preset + dir + n) % n);
            Theme.Use(Preset);
            Version++;
        }

        public void CycleCrt(int dir)
        {
            int n = Enum.GetValues(typeof(CrtLevel)).Length;
            Crt = (CrtLevel)(((int)Crt + dir + n) % n);
            Version++;
        }

        public void CycleLanguage()
        {
            SetLanguage(Language == Lang.Pt ? Lang.En : Lang.Pt);
        }

        public void SetLanguage(Lang l)
        {
            if (Language == l && Loc.Current == l) return;
            Language = l; Loc.Current = l; Version++;
        }

        public void CycleScale(int dir)
        {
            Scale = (Scale + dir + 4) % 4;   // auto, 1x, 2x, 3x
            Version++;
        }

        public void Apply(ThemePreset p, CrtLevel c)
        {
            Preset = p; Crt = c;
            Theme.Use(p);
            Version++;
        }

        public static string ScaleName(int scale) => scale == 0 ? Loc.T("Auto") : scale + "x";

        public static string Name(ThemePreset p)
        {
            switch (p)
            {
                case ThemePreset.Amber: return "Amber";
                case ThemePreset.Phosphor: return "Phosphor";
                case ThemePreset.Cga: return "CGA";
                default: return "Ossuary";
            }
        }

        public static string Describe(ThemePreset p)
        {
            switch (p)
            {
                case ThemePreset.Amber: return "IBM 5151 amber phosphor";
                case ThemePreset.Phosphor: return "P1 green phosphor";
                case ThemePreset.Cga: return "the sixteen colours of a PC";
                default: return "indigo dark, bone text, full colour";
            }
        }

        public static string Name(CrtLevel c)
        {
            switch (c)
            {
                case CrtLevel.Off: return "Off";
                case CrtLevel.Strong: return "Strong";
                default: return "Subtle";
            }
        }

        /// <summary>Scanline depth, vignette and phosphor glow for a level.</summary>
        public static void CrtParams(CrtLevel c, out float scanline, out float vignette, out float glow)
        {
            switch (c)
            {
                case CrtLevel.Off: scanline = 0f; vignette = 0f; glow = 0f; break;
                case CrtLevel.Strong: scanline = 0.28f; vignette = 0.32f; glow = 0.55f; break;
                default: scanline = 0.12f; vignette = 0.16f; glow = 0.30f; break;
            }
        }
    }
}
