using System;
using System.Collections.Generic;
using System.Linq;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    public enum ShopKind { General, Weapon, Armor, potion, Scroll, Wand, Food, Tool, Temple }

    /// <summary>A town: a small map plus the shops, NPCs and services inside it.</summary>
    public sealed class Town
    {
        public string Name;
        public GameMap Map;
        public int EntryX, EntryY;
        public readonly List<Shop> Shops = new List<Shop>();
        public readonly List<Monster> Npcs = new List<Monster>();
        public readonly List<string> Buildings = new List<string>();
        public int Population;
        public int Wealth;
        public int Defense;

        public Shop ShopAt(int x, int y)
        {
            for (int i = 0; i < Shops.Count; i++) if (Shops[i].X == x && Shops[i].Y == y) return Shops[i];
            return null;
        }
    }

    public sealed class Shop
    {
        public ShopKind Kind;
        public string Name;
        public int X, Y;
        public int Glyph;
        public readonly List<Item> Stock = new List<Item>();
        public int OwnerName;
        public Monster Keeper;
        public int Gold;
    }

    /// <summary>
    /// Builds towns as a small walled settlement: a plaza, streets, a wall with
    /// gates, and one building per service. Fallout-style hub, NetHack-style detail.
    /// </summary>
    public static class TownGen
    {
        public static Town Generate(string name, Rng rng, int depth)
        {
            var t = new Town { Name = name };
            int w = rng.Range(31, 45), h = rng.Range(21, 29);
            if (w % 2 == 0) w++;
            if (h % 2 == 0) h++;
            var map = new GameMap(w, h) { Number = rng.Range(1, int.MaxValue / 2) };
            map.Fill(TileKind.Wall);

            int px0 = 2, py0 = 2, px1 = w - 3, py1 = h - 3;

            // Plaza in the middle, streets radiating out.
            int cx = w / 2, cy = h / 2;
            int pw = rng.Range(7, 11), ph = rng.Range(5, 7);
            map.Stamp(cx - pw / 2, cy - ph / 2, pw, ph, TileKind.Floor, true);
            map.Set(cx, cy, TileKind.Fountain);

            for (int x = cx - pw / 2; x <= cx + pw / 2; x++)
            {
                map.Set(x, cy, TileKind.Floor);
                map.Set(x, cy - ph / 2, TileKind.Floor);
                map.Set(x, cy + ph / 2, TileKind.Floor);
            }
            for (int y = cy - ph / 2; y <= cy + ph / 2; y++)
            {
                map.Set(cx, y, TileKind.Floor);
                map.Set(cx - pw / 2, y, TileKind.Floor);
                map.Set(cx + pw / 2, y, TileKind.Floor);
            }

            // Buildings around the plaza.
            var kinds = new[]
            {
                ShopKind.Weapon, ShopKind.Armor, ShopKind.potion, ShopKind.Scroll,
                ShopKind.Wand, ShopKind.Food, ShopKind.Tool, ShopKind.General
            };
            rng.Shuffle(kinds);

            var slots = new List<(int x, int y)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    slots.Add((cx + dx * (pw / 2 + 3), cy + dy * (ph / 2 + 3)));
                }
            }
            rng.Shuffle(slots);

            for (int i = 0; i < kinds.Length && i < slots.Count; i++)
            {
                int bx = slots[i].x, by = slots[i].y;
                if (!map.InBounds(bx - 2, by - 2) || bx + 2 >= w || by + 2 >= h) continue;
                // Building shell with a doorway facing the plaza.
                for (int y = by - 2; y <= by + 2; y++)
                    for (int x = bx - 2; x <= bx + 2; x++)
                        if (map.Get(x, y) != TileKind.Floor) map.Set(x, y, TileKind.Wall);

                int dx = bx - cx, dy = by - cy;
                int doorX = bx, doorY = by;
                if (Math.Abs(dx) > Math.Abs(dy)) doorX = dx > 0 ? bx + 2 : bx - 2;
                else doorY = dy > 0 ? by + 2 : by - 2;

                map.Stamp(bx - 1, by - 1, 3, 3, TileKind.Floor, true);
                map.Set(doorX, doorY, TileKind.OpenDoor);
                map.Set(bx, by, TileKind.Altar);   // a counter to stand behind

                var shop = new Shop
                {
                    Kind = kinds[i],
                    Name = ShopNameFor(kinds[i], rng),
                    X = doorX, Y = doorY,
                    OwnerName = rng.Range(0, 1000),
                    Gold = rng.Range(400, 2500) + depth * 200
                };
                StockShop(shop, rng, depth);
                t.Shops.Add(shop);
                t.Buildings.Add($"{ShopNameFor(kinds[i], rng)} ({shop.Name})");
            }

            // Outlying streets so the town is not just one plaza.
            for (int i = 0; i < 6; i++)
            {
                int x = rng.Range(px0 + 1, px1 - 1), y = rng.Range(py0 + 1, py1 - 1);
                if (map.Get(x, y) != TileKind.Wall) continue;
                map.Set(x, y, TileKind.Floor);
                if (rng.Chance(60)) map.Set(x + 1 < w ? x + 1 : x, y, TileKind.Floor);
                if (rng.Chance(60)) map.Set(x, y + 1 < h ? y + 1 : y, TileKind.Floor);
            }

            // Gates.
            map.Set(cx, py0, TileKind.OpenDoor);
            map.Set(cx, py1, TileKind.OpenDoor);
            map.Set(px0, cy, TileKind.OpenDoor);
            map.Set(px1, cy, TileKind.OpenDoor);

            t.EntryX = cx; t.EntryY = py1;
            if (!map.Walkable(t.EntryX, t.EntryY)) { t.EntryX = cx; t.EntryY = cy + 1; }
            t.Map = map;
            t.Population = rng.Range(40, 400);
            t.Wealth = rng.Range(1, 5);
            t.Defense = rng.Range(1, 5);

            // Townsfolk: guards on the gates, citizens in the plaza.
            SpawnTownsfolk(t, rng, depth);
            return t;
        }

        static void SpawnTownsfolk(Town t, Rng rng, int depth)
        {
            var map = t.Map;

            // Townsfolk reuse bestiary bodies purely as stat shells; a shopkeeper is a
            // "dwarf" only in the sense that it has a body and an inventory.
            MonsterDef KeeperShell() => Bestiary.Find("hobbit");
            MonsterDef GuardShell() => Bestiary.Find("dwarf");

            foreach (var s in t.Shops)
            {
                var keeper = new Monster(KeeperShell(), rng);
                keeper.Name = s.Name;
                keeper.Glyph = '@';
                keeper.IsGuard = false;
                keeper.Dormant = true;   // townsfolk never start a fight on their own
                int spot = FindFloorNear(map, s.X, s.Y, 3);
                if (spot >= 0) { keeper.X = spot % map.W; keeper.Y = spot / map.W; }
                else { keeper.X = s.X; keeper.Y = s.Y; }
                s.Keeper = keeper;
                t.Npcs.Add(keeper);
            }

            int guards = 2 + t.Defense;
            for (int i = 0; i < guards; i++)
            {
                var g = new Monster(GuardShell(), rng);
                g.Name = "town guard";
                g.Glyph = '@';
                g.IsGuard = true;
                g.Alert = 0;
                g.Dormant = true;
                int spot = FindFloorNear(map, map.W / 2, map.H / 2, 12);
                if (spot < 0) continue;
                g.X = spot % map.W; g.Y = spot / map.W;
                t.Npcs.Add(g);
            }
        }



        public static int FindFloorNear(GameMap map, int x, int y, int radius)
        {
            for (int r = 0; r <= radius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int nx = x + dx, ny = y + dy;
                        if (!map.InBounds(nx, ny)) continue;
                        if (map.Get(nx, ny) == TileKind.Floor) return nx + ny * map.W;
                    }
                }
            }
            return -1;
        }

        public static void StockShop(Shop shop, Rng rng, int depth)
        {
            int n = 4 + rng.Range(0, 4);
            for (int i = 0; i < n; i++)
            {
                var item = LevelBuilder.RollLoot(rng, Math.Max(1, depth - 2));
                if (!Matches(shop.Kind, item.Def)) i--;
                else shop.Stock.Add(item);
            }
            // Shops always keep a few basics.
            if (shop.Kind == ShopKind.Food)
                for (int i = 0; i < 3; i++) shop.Stock.Add(new Item(Catalogue.Food[rng.Range(0, Catalogue.Food.Count)], rng, GroundItems.NextUid()));
            if (shop.Kind == ShopKind.potion)
                for (int i = 0; i < 3; i++) shop.Stock.Add(new Item(Catalogue.Potions[rng.Range(0, Catalogue.Potions.Count)], rng, GroundItems.NextUid()));
        }

        static bool Matches(ShopKind kind, ItemDef d)
        {
            switch (kind)
            {
                case ShopKind.Weapon: return d.Kind == ItemKind.Weapon;
                case ShopKind.Armor: return d.Kind == ItemKind.Armor || d.Kind == ItemKind.Shield;
                case ShopKind.potion: return d.Kind == ItemKind.Potion;
                case ShopKind.Scroll: return d.Kind == ItemKind.Scroll;
                case ShopKind.Wand: return d.Kind == ItemKind.Wand;
                case ShopKind.Food: return d.Kind == ItemKind.Food;
                case ShopKind.Tool: return d.Kind == ItemKind.Tool || d.Kind == ItemKind.Ring || d.Kind == ItemKind.Amulet;
                case ShopKind.General: return d.Kind == ItemKind.Book || d.Kind == ItemKind.Ornament || d.Kind == ItemKind.Gem || d.Kind == ItemKind.Rock;
                case ShopKind.Temple: return d.Kind == ItemKind.Scroll || d.Kind == ItemKind.Book;
                default: return false;
            }
        }

        public static string ShopNameFor(ShopKind kind, Rng rng)
        {
            string[] a = { "the Iron", "the Gilded", "the Wandering", "the Old", "the Quiet", "the Bright", "the Rusty", "the Silver" };
            string[] b = { "Hand", "Trades", "Vault", "Supply", "Emporium", "Exchange", "Goods", "Depot" };
            return a[rng.Range(0, a.Length)] + " " + b[rng.Range(0, b.Length)];
        }
    }
}