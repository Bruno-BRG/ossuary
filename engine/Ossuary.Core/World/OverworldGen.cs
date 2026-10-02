using System;
using System.Collections.Generic;

namespace Ossuary.Core.World
{
    /// <summary>
    /// Value-noise over the overworld: rivers carved by elevation minima, forests
    /// clustered on moisture, roads stitched between settlements so travel reads as
    /// "a road" rather than "a line on a map".
    /// </summary>
    public static class OverworldGen
    {
        public static Overworld Generate(int w, int h, ulong seed)
        {
            var rng = new Rng(seed);
            var world = new Overworld(w, h);

            // --- elevation + moisture fields --------------------------------
            var elev = new float[w * h];
            var moist = new float[w * h];
            var fElev = Fbm(w, h, rng, 0.035f, 5);
            var fMoist = Fbm(w, h, rng, 0.05f, 4);
            for (int i = 0; i < w * h; i++)
            {
                elev[i] = fElev[i];
                moist[i] = fMoist[i];
            }

            // Edge falloff: the map should feel like a landmass, not a torus.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float ex = Math.Min(1f, Math.Min(x, w - 1 - x) / (w * 0.18f));
                    float ey = Math.Min(1f, Math.Min(y, h - 1 - y) / (h * 0.18f));
                    float edge = Math.Min(ex, ey);
                    elev[x + y * w] = elev[x + y * w] * 0.75f + (1f - edge) * 0.55f;
                }
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = x + y * w;
                    float e = elev[i];
                    float m = moist[i];
                    OverworldTerrain t;
                    if (e > 0.78f) t = OverworldTerrain.Mountain;
                    else if (e > 0.68f) t = OverworldTerrain.Hills;
                    else if (e > 0.60f) t = OverworldTerrain.Forest;
                    else if (e < 0.22f) t = OverworldTerrain.DeepWater;
                    else if (e < 0.30f) t = OverworldTerrain.Water;
                    else if (e < 0.36f) t = OverworldTerrain.Shallow;
                    else if (m < 0.30f) t = OverworldTerrain.Sand;
                    else if (m > 0.72f) t = OverworldTerrain.Swamp;
                    else t = OverworldTerrain.Grass;
                    world.Set(x, y, new OverworldTile { Terrain = t, Glyph = GlyphFor(t), Index = -1 });
                }
            }

            CarveRivers(world, elev, rng);
            DefineRegions(world, rng);
            PlaceSettlements(world, rng);
            ConnectRoads(world, rng);
            PlaceWildernessFeatures(world, rng);
            return world;
        }

        static float[] Fbm(int w, int h, Rng rng, float scale, int octaves)
        {
            var outp = new float[w * h];
            float amp = 1f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                int gw = Math.Max(2, (int)(w * scale * (1 << o)));
                int gh = Math.Max(2, (int)(h * scale * (1 << o)));
                var grid = new float[gw * gh];
                for (int i = 0; i < grid.Length; i++) grid[i] = (float)rng.NextDouble();

                for (int y = 0; y < h; y++)
                {
                    float gy = y / (float)h * (gh - 1);
                    int y0 = (int)gy, y1 = Math.Min(gh - 1, y0 + 1);
                    float ty = Smooth(gy - y0);
                    for (int x = 0; x < w; x++)
                    {
                        float gx = x / (float)w * (gw - 1);
                        int x0 = (int)gx, x1 = Math.Min(gw - 1, x0 + 1);
                        float tx = Smooth(gx - x0);
                        float a = Lerp(grid[y0 * gw + x0], grid[y0 * gw + x1], tx);
                        float b = Lerp(grid[y1 * gw + x0], grid[y1 * gw + x1], tx);
                        outp[x + y * w] += Lerp(a, b, ty) * amp;
                    }
                }
                total += amp;
                amp *= 0.5f;
            }
            for (int i = 0; i < outp.Length; i++) outp[i] /= total;
            return outp;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);
        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static void CarveRivers(Overworld world, float[] elev, Rng rng)
        {
            int rivers = Math.Max(2, world.W / 22);
            for (int r = 0; r < rivers; r++)
            {
                int x = rng.Range(2, world.W - 3);
                int y = rng.Range(0, world.H - 1);
                float downhill = 2f;
                for (int step = 0; step < world.W * 2; step++)
                {
                    if (x < 0 || y < 0 || x >= world.W || y >= world.H) break;
                    if (rng.Chance(10)) x += rng.Chance(50) ? 1 : -1;
                    if (rng.Chance(20)) y += rng.Chance(50) ? 1 : -1;
                    if (x <= 0 || x >= world.W - 1 || y <= 0 || y >= world.H - 1) break;

                    var t = world.Get(x, y);
                    if (t.Terrain == OverworldTerrain.DeepWater) break;
                    float e = elev[x + y * world.W];
                    if (e < downhill && rng.Chance(70)) downhill = e;

                    t.Terrain = OverworldTerrain.Water;
                    t.Glyph = '~';
                    world.Set(x, y, t);
                    // Widen slightly downstream.
                    if (rng.Chance(45)) { var s = world.Get(x, y + 1); if (s.Terrain != OverworldTerrain.DeepWater) { s.Terrain = OverworldTerrain.Water; s.Glyph = '~'; world.Set(x, y + 1, s); } }
                }
            }
        }

        static void DefineRegions(Overworld world, Rng rng)
        {
            int cols = 4, rows = 3;
            string[] names = {
                "The Verdant Reach", "Ashen Marches", "The Sunken Vale", "Gallowmoor",
                "The Iron Hills", "Whisperfen", "The Craglands", "Emberdown", "The Hollow Wastes"
            };
            var used = new HashSet<string>();
            for (int ry = 0; ry < rows; ry++)
            {
                for (int rx = 0; rx < cols; rx++)
                {
                    int x = rx * world.W / cols, y = ry * world.H / rows;
                    int w = world.W / cols, h = world.H / rows;
                    int d = (rx + ry * cols);
                    string name = d < names.Length ? names[d] : "The Wilds";
                    world.Regions.Add(new Region
                    {
                        X = x, Y = y, W = w, H = h, Name = name,
                        Danger = Math.Min(100, 15 + d * 9),
                        Depth = Math.Min(30, 4 + d * 3)
                    });
                }
            }
        }

        static void PlaceSettlements(Overworld world, Rng rng)
        {
            int townCount = Math.Max(3, world.Regions.Count / 2);
            var placed = new List<(int x, int y)>();

            for (int i = 0; i < townCount; i++)
            {
                var r = world.Regions[i % world.Regions.Count];
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    int x = rng.Range(r.X + 3, r.X + r.W - 4);
                    int y = rng.Range(r.Y + 3, r.Y + r.H - 4);
                    if (world.Get(x, y).Terrain == OverworldTerrain.DeepWater) continue;
                    if (world.Get(x, y).Terrain == OverworldTerrain.Water) continue;
                    if (TooClose(placed, x, y, 6)) continue;
                    placed.Add((x, y));
                    var t = world.Get(x, y);
                    t.Feature = OverworldFeature.Town;
                    t.Glyph = '+';
                    t.Name = TownName(rng, i);
                    t.Index = -1;
                    world.Set(x, y, t);
                    break;
                }
            }

            // Dungeon entrances: one per region, biased toward nastier terrain.
            for (int i = 0; i < world.Regions.Count; i++)
            {
                var r = world.Regions[i];
                for (int attempt = 0; attempt < 120; attempt++)
                {
                    int x = rng.Range(r.X + 2, r.X + r.W - 3);
                    int y = rng.Range(r.Y + 2, r.Y + r.H - 3);
                    var t = world.Get(x, y);
                    if (t.Feature != OverworldFeature.None) continue;
                    if (t.Terrain == OverworldTerrain.DeepWater || t.Terrain == OverworldTerrain.Water) continue;

                    t.Feature = OverworldFeature.Dungeon;
                    t.Glyph = '^';
                    t.Name = DungeonName(rng, r.Name);
                    t.Index = 0;
                    world.Set(x, y, t);
                    break;
                }
            }
        }

        static void ConnectRoads(Overworld world, Rng rng)
        {
            var towns = new List<int>();
            for (int i = 0; i < world.W * world.H; i++)
                if (world.Tiles[i].Feature == OverworldFeature.Town) towns.Add(i);

            if (towns.Count < 2) return;

            // Nearest-neighbour chain: keeps the road network plausible without MST cost.
            var used = new bool[towns.Count];
            used[0] = true;
            for (int step = 1; step < towns.Count; step++)
            {
                int bestA = -1, bestB = -1;
                int bestD = int.MaxValue;
                for (int i = 0; i < towns.Count; i++)
                {
                    if (!used[i]) continue;
                    for (int j = 0; j < towns.Count; j++)
                    {
                        if (used[j]) continue;
                        int d = Math.Abs(towns[i] % world.W - towns[j] % world.W) + Math.Abs(towns[i] / world.W - towns[j] / world.W);
                        if (d < bestD) { bestD = d; bestA = i; bestB = j; }
                    }
                }
                if (bestB < 0) break;
                used[bestB] = true;
                RoadBetween(world, towns[bestA] % world.W, towns[bestA] / world.W, towns[bestB] % world.W, towns[bestB] / world.W, rng);
            }

            // Spurs to the nearest dungeon, so the road network reaches the interesting parts.
            for (int i = 0; i < world.W * world.H; i++)
            {
                if (world.Tiles[i].Feature != OverworldFeature.Dungeon) continue;
                int dx = i % world.W, dy = i / world.W;
                int best = -1, bestD = int.MaxValue;
                foreach (int t in towns)
                {
                    int d = Math.Abs(t % world.W - dx) + Math.Abs(t / world.W - dy);
                    if (d < bestD) { bestD = d; best = t; }
                }
                if (best >= 0) RoadBetween(world, dx, dy, best % world.W, best / world.W, rng);
            }
        }

        static void RoadBetween(Overworld world, int x0, int y0, int x1, int y1, Rng rng)
        {
            // Simple A* over land so roads avoid water, then stamp the terrain.
            int w = world.W, h = world.H;
            var cost = new int[w * h];
            var came = new int[w * h];
            for (int i = 0; i < cost.Length; i++) { cost[i] = int.MaxValue; came[i] = -1; }
            var open = new List<int>();
            int s = x0 + y0 * w;
            cost[s] = 0;
            open.Add(s);
            while (open.Count > 0)
            {
                int best = 0, bestF = int.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int c = open[i];
                    int f = cost[c] + Math.Abs(c % w - x1) + Math.Abs(c / w - y1);
                    if (f < bestF) { bestF = f; best = i; }
                }
                int cur = open[best];
                open.RemoveAt(best);
                if (cur == x1 + y1 * w) break;
                int cx = cur % w, cy = cur / w;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Pathfinder.Dx8[k], ny = cy + Pathfinder.Dy8[k];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    int ni = nx + ny * w;
                    var t = world.Tiles[ni];
                    int stepCost = (t.Terrain == OverworldTerrain.DeepWater || t.Terrain == OverworldTerrain.Water) ? 40 : 1;
                    if (t.Terrain == OverworldTerrain.Mountain) stepCost = 12;
                    int nc = cost[cur] + stepCost;
                    if (nc >= cost[ni]) continue;
                    cost[ni] = nc;
                    came[ni] = cur;
                    open.Add(ni);
                }
            }

            int walk = x1 + y1 * w;
            if (cost[walk] == int.MaxValue) return;
            while (walk >= 0)
            {
                int x = walk % w, y = walk / w;
                var t = world.Tiles[walk];
                if (t.Feature == OverworldFeature.None)
                {
                    t.Terrain = OverworldTerrain.Road;
                    t.Glyph = '.';
                    world.Set(x, y, t);
                }
                if (walk == s) break;
                walk = came[walk];
            }
        }

        static void PlaceWildernessFeatures(Overworld world, Rng rng)
        {
            int total = world.W * world.H;
            int tries = total / 30;
            for (int i = 0; i < tries; i++)
            {
                int x = rng.Range(0, world.W), y = rng.Range(0, world.H);
                var t = world.Get(x, y);
                if (t.Feature != OverworldFeature.None) continue;
                if (t.Terrain == OverworldTerrain.DeepWater || t.Terrain == OverworldTerrain.Water) continue;

                int roll = rng.Range(0, 100);
                OverworldFeature f;
                if (roll < 12) f = OverworldFeature.Ruin;
                else if (roll < 18) f = OverworldFeature.Cave;
                else if (roll < 22) f = OverworldFeature.Mine;
                else if (roll < 27) f = OverworldFeature.Keep;
                else if (roll < 32) f = OverworldFeature.Shrine;
                else if (roll < 36) f = OverworldFeature.Bridge;
                else continue;

                t.Feature = f;
                t.Glyph = GlyphForFeature(f);
                t.Name = FeatureName(f, rng);
                t.Index = f == OverworldFeature.Cave || f == OverworldFeature.Mine || f == OverworldFeature.Keep ? 0 : -1;
                world.Set(x, y, t);
            }
        }

        static bool TooClose(List<(int x, int y)> list, int x, int y, int min)
        {
            foreach (var p in list)
            {
                if (Math.Abs(p.x - x) < min && Math.Abs(p.y - y) < min) return true;
            }
            return false;
        }

        static string TownName(Rng rng, int i)
        {
            string[] a = { "Ash", "Gloom", "Hollow", "Ember", "Vex", "Dun", "Grim", "Roke", "Bell", "Thorn" };
            string[] b = { "brook", "ford", "haven", "gate", "march", "watch", "hold", "crest", "barrow", "rest" };
            return a[rng.Range(0, a.Length)] + b[rng.Range(0, b.Length)];
        }

        /// <summary>
        /// The branch a region's entrance leads into: the Spire in the ash country, the Vaults under the drowned vale, the Mines
        /// in the hills, the Warrens in the fen, and the plain Dungeons everywhere else (so the Amulet is always within reach).
        /// </summary>
        public static string BranchForRegion(string region)
        {
            switch (region)
            {
                case "Ashen Marches": case "Emberdown": return "The Ashen Spire";
                case "The Sunken Vale": return "The Sunken Vaults";
                case "The Iron Hills": case "The Craglands": return "The Mines of Dwarfdeep";
                case "Whisperfen": return "The Warrens";
                default: return "The Dungeons";
            }
        }

        /// <summary>The branch an entrance name belongs to, from the word the name ends in.</summary>
        public static string BranchForEntrance(string name)
        {
            if (name == null) return "The Dungeons";
            if (name.EndsWith("Spire")) return "The Ashen Spire";
            if (name.EndsWith("Vaults")) return "The Sunken Vaults";
            if (name.EndsWith("Delve")) return "The Mines of Dwarfdeep";
            if (name.EndsWith("Warren")) return "The Warrens";
            return "The Dungeons";
        }

        static string DungeonName(Rng rng, string region)
        {
            string[] names;
            switch (BranchForRegion(region))
            {
                case "The Ashen Spire": names = new[] { "the Ashen Spire", "the Cinder Spire" }; break;
                case "The Sunken Vaults": names = new[] { "the Sunless Vaults", "the Drowned Vaults" }; break;
                case "The Mines of Dwarfdeep": names = new[] { "the Iron Delve", "the Deep Delve" }; break;
                case "The Warrens": names = new[] { "the Weeping Warren", "the Gnawed Warren" }; break;
                default: names = new[] { "the Hollow Deep", "the Bleak Catacombs", "the Gilded Galleries", "the Gloomhold Dungeons" }; break;
            }
            string pick = names[rng.Range(0, names.Length)];
            rng.Range(0, 8);          // the old two-part name drew twice: keep the stream, and so the rest of the world, as it was
            return pick;
        }

        static string FeatureName(OverworldFeature f, Rng rng)
        {
            switch (f)
            {
                case OverworldFeature.Ruin: return "an ancient ruin";
                case OverworldFeature.Cave: return "a cave mouth";
                case OverworldFeature.Mine: return "an abandoned mine";
                case OverworldFeature.Keep: return "a ruined keep";
                case OverworldFeature.Shrine: return "a wayshrine";
                case OverworldFeature.Bridge: return "a stone bridge";
                default: return "a landmark";
            }
        }

        /// <summary>
        /// CP437 vocabulary (see Theme.FeatureGlyph / GlyphSet).
        /// Single source of truth for the generator and the coverage test.
        /// </summary>
        public static char GlyphForFeature(OverworldFeature f) => Theme.FeatureGlyph(f);

        public static char GlyphFor(OverworldTerrain t) => Theme.TerrainGlyph(t);
    }
}
