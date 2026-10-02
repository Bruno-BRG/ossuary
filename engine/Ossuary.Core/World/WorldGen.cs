using System;
using System.Collections.Generic;

namespace Ossuary.Core.World
{
    public enum OverworldTerrain
    {
        DeepWater, Water, Shallow, Sand, Grass, Forest, Hills, Mountain, Swamp, Snow, Ash, Ruins, Road
    }

    public enum OverworldFeature
    {
        None = 0,
        Town,
        Dungeon,
        Ruin,
        Shrine,
        Cave,
        Mine,
        Keep,
        Bridge,
        Signpost,
    }

    public struct OverworldTile
    {
        public OverworldTerrain Terrain;
        public OverworldFeature Feature;
        public char Glyph;
        public bool Discovered;
        public int Index;      // which dungeon branch lives here, or -1
        public string Name;
        public bool Visited;
    }

    public struct Region
    {
        public int X, Y, W, H;
        public string Name;
        public OverworldTerrain Dominant;
        public int Danger;      // 0..100, scales the overworld encounter rate
        public int Depth;       // suggested dungeon depth range
    }

    /// <summary>
    /// The map above the map. Fallout-style travel: you pick a destination, time
    /// passes, and the wilds in between are full of things that want to eat you.
    /// </summary>
    public sealed class Overworld
    {
        public readonly int W;
        public readonly int H;
        public readonly OverworldTile[] Tiles;
        public readonly List<Region> Regions = new List<Region>();
        public int PlayerX, PlayerY;
        public string CurrentRegionName = "";
        public int Day = 1;
        public int Hour = 8;
        public long NextUid = 1;

        public Overworld(int w, int h)
        {
            W = w; H = h;
            Tiles = new OverworldTile[w * h];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public OverworldTile Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return new OverworldTile { Terrain = OverworldTerrain.DeepWater, Glyph = '~' };
            return Tiles[x + y * W];
        }

        public void Set(int x, int y, OverworldTile t)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Tiles[x + y * W] = t;
        }

        public void Discover(int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = PlayerX + dx, y = PlayerY + dy;
                    if (x < 0 || y < 0 || x >= W || y >= H) continue;
                    if (dx * dx + dy * dy > radius * radius) continue;
                    ref var t = ref TilesRef(x, y);
                    t.Discovered = true;
                }
            }
        }

        ref OverworldTile TilesRef(int x, int y) => ref Tiles[x + y * W];

        public void AdvanceTime(int hours)
        {
            Hour += hours;
            while (Hour >= 24) { Hour -= 24; Day++; }
        }

        public bool IsNight => Hour < 6 || Hour >= 20;
        public string TimeString => $"{Day,3} {Hour:00}:00" + (IsNight ? " (night)" : "");

        public Region RegionAt(int x, int y)
        {
            for (int i = 0; i < Regions.Count; i++)
            {
                var r = Regions[i];
                if (x >= r.X && y >= r.Y && x < r.X + r.W && y < r.Y + r.H) return r;
            }
            return Regions.Count > 0 ? Regions[0] : default;
        }
    }
}
