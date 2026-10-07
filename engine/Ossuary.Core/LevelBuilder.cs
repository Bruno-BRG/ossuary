using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Populates a freshly generated map with monsters, loot, features and the
    /// stairwell. Kept apart from DungeonGen so the same generator can serve
    /// the main dungeon, a branch, a town interior, or a hand-authored test map.
    /// </summary>
    public static class LevelBuilder
    {
        public sealed class SpawnPoint
        {
            public int X, Y;
            public Monster Monster;
        }

        public static List<SpawnPoint> Populate(
            GameMap map, Rng rng, int depth, string branchName,
            List<SpecialRoom> specials, List<int> startCells, out int startX, out int startY)
        {
            var points = new List<SpawnPoint>();
            // Traps and floor items live in tables keyed by map number. A new game that draws the same number must not
            // inherit what an earlier game left there, or two runs of one seed would not be the same run.
            TrapTable.Clear(map.Number);
            GroundItems.Clear(map.Number);

            startX = startCells.Count > 0 ? startCells[0] % map.W : 1;
            startY = startCells.Count > 0 ? startCells[0] / map.W : 1;
            if (!map.Walkable(startX, startY))
            {
                int f = DungeonGen.FindAnyFloor(map);
                startX = f % map.W; startY = f / map.W;
            }

            map.BranchName = branchName;
            map.Depth = depth;
            map.LevelName = NameFor(branchName, depth);

            // The Annex is only three floors deep and meant to be hard: its monsters and loot are those of six floors lower.
            int eff = branchName == "The Annex" ? depth + 6 : depth;
            SpawnMonsters(map, rng, eff, startX, startY, points, branchName);
            PlaceLoot(map, rng, eff, depth);
            PlaceTraps(map, rng, depth);
            PlaceSurfaces(map, rng, depth, branchName);
            PlaceVaults(map, rng, eff, points);
            if (branchName == "The Dungeons" && depth == 4 && TryFindOpenFloor(map, rng, out int px, out int py) && map.Get(px, py) != TileKind.StairsUp)
                map.Set(px, py, TileKind.Portal);
            return points;
        }

        public static string NameFor(string branch, int depth)
        {
            string b = string.IsNullOrEmpty(branch) ? "The Dungeons" : branch;
            return $"{b} : level {depth}";
        }

        static void SpawnMonsters(GameMap map, Rng rng, int depth, int startX, int startY, List<SpawnPoint> points, string branchName = null)
        {
            var table = Bestiary.SpawnTable(depth, rng, branchName);
            if (table.Count == 0) return;

            int area = map.CountWalkable();
            int budget = Math.Max(3, area / 70 + depth * 2 / 3);
            var weights = new int[table.Count];
            for (int i = 0; i < table.Count; i++) weights[i] = 100;

            for (int i = 0; i < budget; i++)
            {
                int pick = rng.WeightedIndex(weights);
                if (pick < 0) break;
                TryPlace(map, rng, table[pick], startX, startY, depth, points);
            }

            // A couple of guaranteed early threats so the first levels are never empty.
            int guard = 0;
            while (points.Count < 2 && guard++ < 60)
            {
                var def = table[rng.Range(0, table.Count)];
                TryPlace(map, rng, def, startX, startY, depth, points);
            }
        }

        static bool TryPlace(GameMap map, Rng rng, MonsterDef def, int startX, int startY, int depth, List<SpawnPoint> points)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                int x = rng.Range(1, map.W - 1);
                int y = rng.Range(1, map.H - 1);
                if (!map.Walkable(x, y)) continue;
                TileKind t = map.Get(x, y);
                if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                if (Tiles.IsStairs(t)) continue;
                if (Pathfinder.Chebyshev(x, y, startX, startY) < 4) continue;  // never spawn on the player
                if (def.Vision == 0 && rng.Chance(50)) continue;                // stationary clutter is sparser

                var m = new Monster(def, rng) { X = x, Y = y, HomeX = x, HomeY = y, Depth = map.Depth };
                m.Uid = GroundItems.NextUid();

                if (def.Carries != null && def.CarryWeights != null && def.Carries.Length > 0)
                {
                    int idx = rng.WeightedIndex(def.CarryWeights);
                    if (idx >= 0 && idx < def.Carries.Length)
                    {
                        var carried = new Item(def.Carries[idx], rng, GroundItems.NextUid());
                        Materials.Assign(carried, rng.Seed, depth, map.BranchName);
                        m.Inventory.Add(carried);
                    }
                }
                if (rng.Chance(15))
                {
                    int gold = rng.Range(5, 20 + depth * 8);
                    m.Inventory.Add(new Item(GoldDef, rng, GroundItems.NextUid())
                    { Quantity = gold, Identified = true });
                }

                points.Add(new SpawnPoint { X = x, Y = y, Monster = m });
                return true;
            }
            return false;
        }

        /// <summary>
        /// Loose items live in a side table keyed by cell rather than in the tile array,
        /// so terrain stays exactly one byte per cell and the renderer can layer
        /// "floor with something on it" without a second grid.
        /// </summary>
        static void PlaceLoot(GameMap map, Rng rng, int depth, int levelDepth)
        {
            int floor = map.CountWalkable();
            int stacks = Math.Max(2, floor / 140);
            for (int i = 0; i < stacks; i++)
            {
                int x, y;
                if (!TryFindOpenFloor(map, rng, out x, out y)) continue;
                var item = RollLoot(rng, depth, map.BranchName);
                if (item != null) GroundItems.Add(map.Number, x, y, item);
            }

            // The Mines give up ore: two to four lumps a level, copper and iron near the top, silver, mithril and adamantine deeper.
            if (map.BranchName == "The Mines of Dwarfdeep")
            {
                int veins = 2 + rng.Range(0, 3);
                for (int i = 0; i < veins; i++)
                    if (TryFindOpenFloor(map, rng, out int ox, out int oy))
                        GroundItems.Add(map.Number, ox, oy, new Item(Materials.OreAt(levelDepth, rng.Range(0, 100)), rng, GroundItems.NextUid()) { Identified = true });
            }

            // Each branch hides one named artifact on a fixed level.
            foreach (var art in Artifacts.RollForLevel(map.BranchName, levelDepth, rng))
                if (TryFindOpenFloor(map, rng, out int ax, out int ay))
                    GroundItems.Add(map.Number, ax, ay, Artifacts.Create(art, rng, GroundItems.NextUid()));

            // A treasure cache in roughly one level in five, as a fallback reward
            // for exploring the parts the stairs do not lead to.
            if (rng.Chance(20))
            {
                int x, y;
                if (TryFindOpenFloor(map, rng, out x, out y))
                {
                    var loot = RollLoot(rng, depth + 4, map.BranchName);
                    if (loot != null)
                    {
                        loot.Identified = true;
                        GroundItems.Add(map.Number, x, y, loot);
                    }
                }
            }
        }

        /// <summary>
        /// Two kinds of sealed chamber, built last and out of unused rock: a locked vault whose key a monster carries, and a
        /// hidden cache whose floor is rigged with traps. Both pay far better than a floor item.
        /// </summary>
        static void PlaceVaults(GameMap map, Rng rng, int depth, List<SpawnPoint> points)
        {
            if (depth >= 2 && rng.Chance(30))
            {
                var site = Vaults.Carve(map, rng, TileKind.LockedDoor);
                if (site != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var item = RollLoot(rng, depth + 2, map.BranchName);
                        if (item != null) GroundItems.Add(map.Number, site.Cells[site.Cells.Count - 1 - i * 2] % map.W, site.Cells[site.Cells.Count - 1 - i * 2] / map.W, item);
                    }
                    var gold = new Item(GoldDef, rng, GroundItems.NextUid()) { Quantity = 60 + depth * 25 };
                    int gc = site.Cells[site.Cells.Count / 2];
                    GroundItems.Add(map.Number, gc % map.W, gc / map.W, gold);
                    var key = new Item(Crafted.BrassKey, rng, GroundItems.NextUid()) { Identified = true };
                    if (points.Count > 0) points[rng.Range(0, points.Count)].Monster.Inventory.Add(key);
                    else if (TryFindOpenFloor(map, rng, out int kx, out int ky)) GroundItems.Add(map.Number, kx, ky, key);
                }
            }
            if (depth >= 3 && rng.Chance(22))
            {
                var site = Vaults.Carve(map, rng, TileKind.HiddenDoor);
                if (site != null)
                {
                    // Every cell but the far end is rigged; the loot waits at the far end.
                    for (int i = 0; i < site.Cells.Count - 3; i++)
                        if (rng.Chance(45))
                        {
                            Traps kind = rng.Pick(new[] { Traps.Spike, Traps.Dart, Traps.Fire, Traps.Alarm, Traps.Web });
                            TrapTable.Put(map.Number, site.Cells[i] % map.W, site.Cells[i] / map.W, kind, Math.Max(2, depth / 3));
                        }
                    for (int i = 0; i < 3; i++)
                    {
                        var item = RollLoot(rng, depth + 3, map.BranchName);
                        int c = site.Cells[site.Cells.Count - 1 - i];
                        if (item != null) GroundItems.Add(map.Number, c % map.W, c / map.W, item);
                    }
                }
            }
        }

        static bool TryFindOpenFloor(GameMap map, Rng rng, out int x, out int y)
        {
            x = y = 0;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int tx = rng.Range(1, map.W - 1);
                int ty = rng.Range(1, map.H - 1);
                TileKind t = map.Get(tx, ty);
                if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                x = tx; y = ty;
                return true;
            }
            return false;
        }

        /// <summary>Depth-weighted loot table: more wands and scrolls deeper, weapons and armour shallow.</summary>
        public static Item RollLoot(Rng rng, int depth, string branch = null)
        {
            int roll = rng.Range(0, 100);
            ItemDef def;
            if (roll < 18) def = PickDeep(Catalogue.Weapons, rng, depth);
            else if (roll < 32)
            {
                int piece = rng.Range(0, 100);
                def = piece < 50 ? PickDeep(Catalogue.Armor, rng, depth)
                    : piece < 62 ? PickDeep(Catalogue.Helms, rng, depth)
                    : piece < 74 ? PickDeep(Catalogue.Gloves, rng, depth)
                    : piece < 86 ? PickDeep(Catalogue.Boots, rng, depth)
                    : PickDeep(Catalogue.Cloaks, rng, depth);
            }
            else if (roll < 38) def = PickDeep(Catalogue.Shields, rng, depth);
            else if (roll < 48) def = PickDeep(Catalogue.Wands, rng, depth);
            else if (roll < 58) def = PickDeep(Catalogue.Scrolls, rng, depth);
            else if (roll < 68) def = PickDeep(Catalogue.Potions, rng, depth);
            else if (roll < 74) def = PickDeep(Catalogue.Rings, rng, depth);
            else if (roll < 78) def = PickDeep(Catalogue.Amulets, rng, depth);
            else if (roll < 81) def = Pick(Catalogue.Tools, rng);
            else if (roll < 84) def = Ammo.All[rng.Range(0, Ammo.All.Length)];
            else if (roll < 89) def = Pick(Catalogue.Food, rng);
            else if (roll < 93) def = Pick(Catalogue.Ornaments, rng);
            else if (roll < 97) def = PickBook(rng, depth);
            else def = GoldDef;

            var item = new Item(def, rng, GroundItems.NextUid());
            Materials.Assign(item, rng.Seed, depth, branch);
            ItemRoller.Roll(item, rng, depth);
            if (def.Kind == ItemKind.Gold)
            {
                item.Quantity = Math.Max(1, rng.Range(4 + depth * 2, 30 + depth * 22));
                item.Identified = true;
            }
            // Ammunition lies in bundles: a dropped quiver, a pouch of stones.
            if (def.Kind == ItemKind.Ammo) { item.Quantity = rng.Range(5, 16); item.Identified = true; }
            return item;
        }

        public static readonly ItemDef GoldDef = new ItemDef
        { Name = "gold piece", Glyph = '$', Kind = ItemKind.Gold, Cost = 1, Weight = 1 };

        static ItemDef Pick(IReadOnlyList<ItemDef> list, Rng rng)
        {
            return list[rng.Range(0, list.Count)];
        }

        /// <summary>Anything with a tier: the deeper the level, the higher the tier it may be (2 at the top, 5 from depth ~9).</summary>
        public static ItemDef PickDeep(IReadOnlyList<ItemDef> list, Rng rng, int depth)
        {
            int maxTier = Math.Min(5, 2 + depth / 3);
            var pool = new List<ItemDef>();
            foreach (var d in list) if (d.Tier <= maxTier) pool.Add(d);
            return pool.Count == 0 ? list[rng.Range(0, list.Count)] : pool[rng.Range(0, pool.Count)];
        }

        /// <summary>A book found on a level: the deeper you are, the higher the tier it may be (tier 1 at the top, 5 from depth ~12).</summary>
        public static ItemDef PickBook(Rng rng, int depth)
        {
            int maxTier = Math.Max(1, Math.Min(5, 1 + depth / 3));
            var pool = new List<ItemDef>();
            foreach (var b in Catalogue.Books) if (b.Tier <= maxTier) pool.Add(b);
            return pool[rng.Range(0, pool.Count)];
        }

        /// <summary>Puddles in the drowned vaults, dry brush in the warrens, spilled oil in the old forts and the Spire.</summary>
        static void PlaceSurfaces(GameMap map, Rng rng, int depth, string branch)
        {
            SurfaceKind kind = SurfaceKind.None; int patches = 0, radius = 2;
            switch (branch)
            {
                case "The Sunken Vaults": kind = SurfaceKind.Water; patches = 3 + depth / 4; break;
                case "The Warrens": kind = SurfaceKind.Grass; patches = 4; break;
                case "The Mines of Dwarfdeep": kind = SurfaceKind.Water; patches = 1; radius = 1; break;
                case "The Ashen Spire": kind = SurfaceKind.Oil; patches = 2; break;
                default:
                    if (depth >= 3 && rng.Chance(35)) { kind = SurfaceKind.Oil; patches = 1; radius = 1; }
                    else if (rng.Chance(30)) { kind = SurfaceKind.Grass; patches = 1; }
                    break;
            }
            for (int i = 0; i < patches; i++)
            {
                if (!TryFindOpenFloor(map, rng, out int cx, out int cy)) continue;
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int x = cx + dx, y = cy + dy;
                        var t = map.Get(x, y);
                        if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                        int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        if (rng.Chance(85 - dist * 20)) map.SetSurface(x, y, kind, 0);
                    }
            }
        }

        static void PlaceTraps(GameMap map, Rng rng, int depth)
        {
            int traps = Math.Max(1, map.CountWalkable() / 400);
            for (int i = 0; i < traps; i++)
            {
                int x = rng.Range(2, map.W - 2), y = rng.Range(2, map.H - 2);
                if (!map.Walkable(x, y)) continue;
                Traps kind = rng.Pick(new[] { Traps.Spike, Traps.Hole, Traps.Dart, Traps.Alarm, Traps.Web, Traps.Teleport });
                TrapTable.Put(map.Number, x, y, kind, Math.Max(2, depth / 3));
            }
        }
    }

    public enum Traps { Spike, Hole, Dart, Teleport, Alarm, Fire, Web }

    /// <summary>Sparse per-level trap layout, kept out of the tile array so terrain stays one byte per cell.</summary>
    public static class TrapTable
    {
        static readonly Dictionary<int, Dictionary<int, KeyValuePair<Traps, int>>> _traps
            = new Dictionary<int, Dictionary<int, KeyValuePair<Traps, int>>>();

        public static void Clear(int mapNumber) { _traps.Remove(mapNumber); _revealed.Remove(mapNumber); }

        public static void Put(int mapNumber, int x, int y, Traps kind, int level)
        {
            if (!_traps.TryGetValue(mapNumber, out var d))
            {
                d = new Dictionary<int, KeyValuePair<Traps, int>>();
                _traps[mapNumber] = d;
            }
            d[x + y * 4096] = new KeyValuePair<Traps, int>(kind, level);
        }

        public static bool TryGet(int mapNumber, int x, int y, out Traps kind, out int level)
        {
            kind = Traps.Spike; level = 0;
            if (!_traps.TryGetValue(mapNumber, out var d)) return false;
            if (!d.TryGetValue(x + y * 4096, out var v)) return false;
            kind = v.Key; level = v.Value;
            return true;
        }

        public static bool Remove(int mapNumber, int x, int y)
        {
            if (_revealed.TryGetValue(mapNumber, out var r)) r.Remove(x + y * 4096);
            if (!_traps.TryGetValue(mapNumber, out var d)) return false;
            return d.Remove(x + y * 4096);
        }

        // Traps the player has found. A found trap is drawn, avoided by auto-walk and can be disarmed.
        static readonly Dictionary<int, HashSet<int>> _revealed = new Dictionary<int, HashSet<int>>();

        public static bool Reveal(int mapNumber, int x, int y)
        {
            if (!TryGet(mapNumber, x, y, out _, out _)) return false;
            if (!_revealed.TryGetValue(mapNumber, out var r)) { r = new HashSet<int>(); _revealed[mapNumber] = r; }
            return r.Add(x + y * 4096);
        }

        public static bool IsRevealed(int mapNumber, int x, int y) =>
            _revealed.TryGetValue(mapNumber, out var r) && r.Contains(x + y * 4096);

        public static string Name(Traps kind)
        {
            switch (kind)
            {
                case Traps.Spike: return "spike trap";
                case Traps.Hole: return "hole";
                case Traps.Dart: return "dart trap";
                case Traps.Teleport: return "teleport trap";
                case Traps.Alarm: return "alarm trap";
                case Traps.Fire: return "fire trap";
                default: return "web";
            }
        }
    }

    /// <summary>Ground item stacks, keyed by (map, cell). Same rationale as TrapTable.</summary>
    public static class GroundItems
    {
        static readonly Dictionary<int, Dictionary<int, List<Items.Item>>> _items
            = new Dictionary<int, Dictionary<int, List<Items.Item>>>();
        static long _uid = 1;

        public static long NextUid() => _uid++;

        public static void Clear(int mapNumber) => _items.Remove(mapNumber);

        public static void Add(int mapNumber, int x, int y, Items.Item item)
        {
            if (!_items.TryGetValue(mapNumber, out var d))
            {
                d = new Dictionary<int, List<Items.Item>>();
                _items[mapNumber] = d;
            }
            int key = x + y * 4096;
            if (!d.TryGetValue(key, out var list))
            {
                list = new List<Items.Item>();
                d[key] = list;
            }
            list.Add(item);
        }

        public static List<Items.Item> At(int mapNumber, int x, int y)
        {
            if (!_items.TryGetValue(mapNumber, out var d)) return null;
            return d.TryGetValue(x + y * 4096, out var list) ? list : null;
        }

        public static Items.Item Take(int mapNumber, int x, int y, int index)
        {
            var list = At(mapNumber, x, y);
            if (list == null || index < 0 || index >= list.Count) return null;
            var item = list[index];
            list.RemoveAt(index);
            if (list.Count == 0) RemoveCell(mapNumber, x, y);
            return item;
        }

        public static void RemoveCell(int mapNumber, int x, int y)
        {
            if (_items.TryGetValue(mapNumber, out var d)) d.Remove(x + y * 4096);
        }
    }
}
