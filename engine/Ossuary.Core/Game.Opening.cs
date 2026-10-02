using System;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>Where a new expedition begins: on the overworld, a step from the first dungeon mouth.</summary>
    public sealed partial class Game
    {
        /// <summary>
        /// Moves a freshly created hero out of the dungeon and onto the overworld, beside the
        /// dungeon entrance closest to the first town. Consumes no RNG, so it is replay-safe.
        /// </summary>
        public void BeginAtOverworld()
        {
            int tx = World.PlayerX, ty = World.PlayerY;
            int best = int.MaxValue, bx = -1, by = -1;
            for (int y = 0; y < World.H; y++)
                for (int x = 0; x < World.W; x++)
                {
                    if (World.Tiles[x + y * World.W].Feature != OverworldFeature.Dungeon) continue;
                    // The gentlest region first (the Verdant Reach), then the closest to the first town.
                    int d = World.RegionAt(x, y).Danger * 1000 + Math.Abs(x - tx) + Math.Abs(y - ty);
                    if (d < best) { best = d; bx = x; by = y; }
                }

            int px = tx, py = ty;
            if (bx >= 0)
            {
                // Walk the eight neighbours, nearest to the town first, for dry ground with no feature on it.
                int bestScore = int.MaxValue;
                for (int pass = 0; pass < 2 && bestScore == int.MaxValue; pass++)   // straight neighbours first, so one step enters
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = bx + dx, y = by + dy;
                        if ((dx == 0 && dy == 0) || !World.InBounds(x, y)) continue;
                        if (pass == 0 && dx != 0 && dy != 0) continue;
                        var t = World.Get(x, y);
                        if (t.Feature != OverworldFeature.None) continue;
                        if (t.Terrain == OverworldTerrain.DeepWater || t.Terrain == OverworldTerrain.Water || t.Terrain == OverworldTerrain.Mountain) continue;
                        int score = Math.Abs(x - tx) + Math.Abs(y - ty);
                        if (score < bestScore) { bestScore = score; px = x; py = y; }
                    }
            }

            World.PlayerX = px; World.PlayerY = py;
            World.CurrentRegionName = World.RegionAt(px, py).Name;
            World.Day = 1; World.Hour = 8;
            World.Discover(8);

            Mode = GameMode.Overworld;
            Player.InsideDungeon = false;
            Map = null; Town = null;
            Monsters.Clear();
            Log.Clear(); Transcript.Clear();
            foreach (var line in Story.Opening(World.CurrentRegionName)) Say(line, MessageKind.Narrative);
        }
    }
}
