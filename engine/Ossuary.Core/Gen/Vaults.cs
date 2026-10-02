using System.Collections.Generic;

namespace Ossuary.Core.Gen
{
    /// <summary>A sealed chamber carved into solid rock next to the level's own passages.</summary>
    public sealed class VaultSite
    {
        public int DoorX, DoorY;
        /// <summary>Interior floor cells, nearest the door first.</summary>
        public readonly List<int> Cells = new List<int>();
    }

    /// <summary>
    /// Vaults are built after a level is finished, out of rock the level did not use, so they can never cut a route or hide
    /// the stairs: the layout and the stairs-reachable guarantee are exactly what the generator already proved.
    /// </summary>
    public static class Vaults
    {
        const int Depth = 4, Height = 3;

        static bool Rock(GameMap map, int x, int y) =>
            map.InBounds(x, y) && x > 0 && y > 0 && x < map.W - 1 && y < map.H - 1 && (map.Get(x, y) == TileKind.Wall || map.Get(x, y) == TileKind.WallAlt || map.Get(x, y) == TileKind.WallDark);

        /// <summary>
        /// Finds a spot, carves the chamber and puts <paramref name="door"/> in its mouth. Returns null when the level has no
        /// room for one (a cave full of passages, say).
        /// </summary>
        public static VaultSite Carve(GameMap map, Rng rng, TileKind door)
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                int fx = rng.Range(2, map.W - 2), fy = rng.Range(2, map.H - 2);
                TileKind t = map.Get(fx, fy);
                if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                int k = rng.Range(0, 4);
                int dx = Dirs.Dx4[k], dy = Dirs.Dy4[k];
                int doorX = fx + dx, doorY = fy + dy;
                if (!Rock(map, doorX, doorY)) continue;

                // The chamber runs away from the door: Depth cells long, Height wide, centred on the door's line.
                int px = -dy, py = dx;
                int half = Height / 2;
                bool clear = true;
                // Everything in the chamber and a one-cell collar around it (except the door's own cell) must be rock.
                for (int along = 1; along <= Depth + 1 && clear; along++)
                    for (int across = -half - 1; across <= half + 1 && clear; across++)
                    {
                        int x = doorX + dx * along + px * across, y = doorY + dy * along + py * across;
                        if (!Rock(map, x, y)) clear = false;
                    }
                if (!clear) continue;
                // The door cell's own flanks must be rock too, or the mouth would open sideways.
                if (!Rock(map, doorX + px, doorY + py) || !Rock(map, doorX - px, doorY - py)) continue;

                var site = new VaultSite { DoorX = doorX, DoorY = doorY };
                for (int along = 1; along <= Depth; along++)
                    for (int across = -half; across <= half; across++)
                    {
                        int x = doorX + dx * along + px * across, y = doorY + dy * along + py * across;
                        map.SetRaw(x + y * map.W, TileKind.Floor);
                        site.Cells.Add(x + y * map.W);
                    }
                map.SetRaw(doorX + doorY * map.W, door);
                map.Version++;
                return site;
            }
            return null;
        }
    }
}
