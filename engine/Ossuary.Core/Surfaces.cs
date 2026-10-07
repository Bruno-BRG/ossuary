namespace Ossuary.Core
{
    /// <summary>
    /// What lies on a floor cell besides the floor itself. A sparse layer on the map (see GameMap.Surfaces)
    /// so terrain stays one byte per cell. Surfaces interact: fire eats grass and oil, frost freezes water,
    /// water carries lightning.
    /// </summary>
    public enum SurfaceKind : byte { None, Water, Ice, Fire, Oil, Grass }

    public struct Surface
    {
        public SurfaceKind Kind;
        /// <summary>Turns left; 0 means it stays until something changes it.</summary>
        public int Turns;
    }

    /// <summary>What creatures and fire leave on a floor: a second sparse layer under the surfaces, fading with time.</summary>
    public enum StainKind : byte { None, Blood, Ichor, Slime, Mud, Soot, Footprints, Drag }

    public struct Stain
    {
        public StainKind Kind;
        /// <summary>The turn it was left, which is how fresh it is.</summary>
        public int Turn;
        /// <summary>Whose it is ("kobold", "you"), for the hero reading a trail.</summary>
        public string Source;
        /// <summary>Which way the one who left it was going.</summary>
        public sbyte Dx, Dy;
    }

    public static class StainInfo
    {
        /// <summary>Turns before a stain is gone.</summary>
        public static int Life(StainKind k)
        {
            switch (k)
            {
                case StainKind.Blood: case StainKind.Ichor: return 500;
                case StainKind.Slime: return 400;
                case StainKind.Mud: return 250;
                case StainKind.Soot: return 900;
                case StainKind.Footprints: return 120;
                case StainKind.Drag: return 300;
                default: return 0;
            }
        }

        public static string Name(StainKind k)
        {
            switch (k)
            {
                case StainKind.Blood: return "blood";
                case StainKind.Ichor: return "ichor";
                case StainKind.Slime: return "slime";
                case StainKind.Mud: return "mud";
                case StainKind.Soot: return "soot";
                case StainKind.Footprints: return "footprints";
                case StainKind.Drag: return "drag marks";
                default: return "";
            }
        }

        /// <summary>Stains a trail is read from: what something wounded leaves behind it.</summary>
        public static bool Trail(StainKind k) => k == StainKind.Blood || k == StainKind.Ichor || k == StainKind.Slime || k == StainKind.Drag;
    }

    public static class SurfaceInfo
    {
        public static bool Flammable(SurfaceKind k) => k == SurfaceKind.Grass || k == SurfaceKind.Oil;

        public static string Name(SurfaceKind k)
        {
            switch (k)
            {
                case SurfaceKind.Water: return "shallow water";
                case SurfaceKind.Ice: return "ice";
                case SurfaceKind.Fire: return "flames";
                case SurfaceKind.Oil: return "spilled oil";
                case SurfaceKind.Grass: return "dry brush";
                default: return "";
            }
        }
    }
}
