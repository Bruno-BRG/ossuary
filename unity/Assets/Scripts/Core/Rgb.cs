using System;
using System.Globalization;

namespace Ossuary.Core
{
    /// <summary>
    /// Engine-agnostic 24-bit colour. Core stays free of UnityEngine so the whole
    /// simulation can be exercised headlessly from the CLI.
    /// </summary>
    public readonly struct Rgb : IEquatable<Rgb>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public Rgb(byte r, byte g, byte b) { R = r; G = g; B = b; }

        public static Rgb operator +(Rgb a, Rgb b) => new Rgb(Clamp(a.R + b.R), Clamp(a.G + b.G), Clamp(a.B + b.B));
        public static Rgb operator -(Rgb a, Rgb b) => new Rgb(Clamp(a.R - b.R), Clamp(a.G - b.G), Clamp(a.B - b.B));
        public static Rgb operator *(Rgb a, float k) => new Rgb(Clamp(a.R * k), Clamp(a.G * k), Clamp(a.B * k));

        public static Rgb Lerp(Rgb a, Rgb b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return new Rgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        /// <summary>Multiplies toward white; used to render "bold" text.</summary>
        public Rgb Brighten(float amount) => Lerp(this, new Rgb(255, 255, 255), Math.Max(0f, Math.Min(1f, amount)));

        public Rgb Dim(float amount) => this * (1f - Math.Max(0f, Math.Min(1f, amount)));

        public Rgb WithValue(float v) => new Rgb(Clamp(v * 255f), Clamp(v * 255f), Clamp(v * 255f));

        public uint Packed => 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | B;

        public static Rgb FromHex(int rgb) => new Rgb((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));

        public static Rgb FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return new Rgb(255, 0, 255);
            string s = hex.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);
            if (s.Length == 3) s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
            if (s.Length != 6) return new Rgb(255, 0, 255);
            if (!int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v)) return new Rgb(255, 0, 255);
            return FromHex(v);
        }

        public static Rgb FromBytes(int r, int g, int b) => new Rgb(Clamp(r), Clamp(g), Clamp(b));

        public static readonly Rgb Black = new Rgb(0, 0, 0);
        public static readonly Rgb White = new Rgb(255, 255, 255);

        static byte Clamp(double v) => (byte)(v <= 0 ? 0 : v >= 255 ? 255 : v);
        static byte Clamp(float v) => (byte)(v <= 0f ? 0f : v >= 255f ? 255f : v);

        public bool Equals(Rgb other) => R == other.R && G == other.G && B == other.B;
        public override bool Equals(object obj) => obj is Rgb o && Equals(o);
        public override int GetHashCode() => (R << 16) | (G << 8) | B;
        public override string ToString() => string.Format("#{0:X2}{1:X2}{2:X2}", R, G, B);
    }
}
