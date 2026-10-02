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

            SpawnMonsters(map, rng, depth, startX, startY, points);
            PlaceLoot(map, rng, depth);
            PlaceTraps(map, rng, depth);
            PlaceSurfaces(map, rng, depth, branchName);
            return points;
        }

        public static string NameFor(string branch, int depth)
        {
            string b = string.IsNullOrEmpty(branch) ? "The Dungeons" : branch;
            return $"{b} : level {depth}";
        }

        static void SpawnMonsters(GameMap map, Rng rng, int depth, int startX, int startY, List<SpawnPoint> points)
        {
            var table = Bestiary.SpawnTable(depth, rng);
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
                        m.Inventory.Add(new Item(def.Carries[idx], rng, GroundItems.NextUid()));
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
        static void PlaceLoot(GameMap map, Rng rng, int depth)
        {
            int floor = map.CountWalkable();
            int stacks = Math.Max(2, floor / 140);
            for (int i = 0; i < stacks; i++)
            {
                int x, y;
                if (!TryFindOpenFloor(map, rng, out x, out y)) continue;
                var item = RollLoot(rng, depth);
                if (item != null) GroundItems.Add(map.Number, x, y, item);
            }

            // Each branch hides one named artifact on a fixed level.
            var art = Artifacts.ForLevel(map.BranchName, depth);
            if (art != null && TryFindOpenFloor(map, rng, out int ax, out int ay))
                GroundItems.Add(map.Number, ax, ay, Artifacts.Create(art, rng, GroundItems.NextUid()));

            // A treasure cache in roughly one level in five, as a fallback reward
            // for exploring the parts the stairs do not lead to.
            if (rng.Chance(20))
            {
                int x, y;
                if (TryFindOpenFloor(map, rng, out x, out y))
                {
                    var loot = RollLoot(rng, depth + 4);
                    if (loot != null)
                    {
                        loot.Identified = true;
                        GroundItems.Add(map.Number, x, y, loot);
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
        public static Item RollLoot(Rng rng, int depth)
        {
            int roll = rng.Range(0, 100);
            ItemDef def;
            if (roll < 18) def = Pick(Catalogue.Weapons, rng);
            else if (roll < 32)
            {
                int piece = rng.Range(0, 100);
                def = piece < 50 ? Pick(Catalogue.Armor, rng)
                    : piece < 62 ? Pick(Catalogue.Helms, rng)
                    : piece < 74 ? Pick(Catalogue.Gloves, rng)
                    : piece < 86 ? Pick(Catalogue.Boots, rng)
                    : Pick(Catalogue.Cloaks, rng);
            }
            else if (roll < 38) def = Pick(Catalogue.Shields, rng);
            else if (roll < 48) def = Pick(Catalogue.Wands, rng);
            else if (roll < 58) def = Pick(Catalogue.Scrolls, rng);
            else if (roll < 68) def = Pick(Catalogue.Potions, rng);
            else if (roll < 74) def = Pick(Catalogue.Rings, rng);
            else if (roll < 78) def = Pick(Catalogue.Amulets, rng);
            else if (roll < 84) def = Pick(Catalogue.Tools, rng);
            else if (roll < 89) def = Pick(Catalogue.Food, rng);
            else if (roll < 93) def = Pick(Catalogue.Ornaments, rng);
            else if (roll < 97) def = Pick(Catalogue.Books, rng);
            else def = GoldDef;

            var item = new Item(def, rng, GroundItems.NextUid());
            ItemRoller.Roll(item, rng, depth);
            if (def.Kind == ItemKind.Gold)
            {
                item.Quantity = Math.Max(1, rng.Range(4 + depth * 2, 30 + depth * 22));
                item.Identified = true;
            }
            return item;
        }

        public static readonly ItemDef GoldDef = new ItemDef
        { Name = "gold piece", Glyph = '$', Kind = ItemKind.Gold, Cost = 1, Weight = 1 };

        static ItemDef Pick(IReadOnlyList<ItemDef> list, Rng rng)
        {
            return list[rng.Range(0, list.Count)];
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