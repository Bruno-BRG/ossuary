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
