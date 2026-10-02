using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    public enum ShopKind { General, Weapon, Armor, potion, Scroll, Wand, Food, Tool, Temple, Jewel, Book }

    public enum TownRole
    {
        Citizen, Child, Guard, Captain, Shopkeeper, Smith, Innkeeper, Barkeep, Priest, Elder,
        Scholar, Bard, Drunk, Adventurer, Beggar, Prisoner, Pet,
    }

    public enum BuildingKind
    {
        Smithy, Armoury, Alchemist, Emporium, General, Tavern, Inn, Temple, Guild, Library,
        Barracks, Cottage, Townhouse, Watchtower, Stall,
    }

    /// <summary>What the person behind the counter will do for gold besides selling things.</summary>
    [Flags]
    public enum Service
    {
        None = 0, Rest = 1, Meal = 2, Ale = 4, Rumor = 8, Heal = 16, Cure = 32,
        Donate = 64, Appraise = 128, Hone = 256, Quest = 512,
    }

    /// <summary>
    /// One building. Its footprint is the same on every floor, so a staircase puts you at the same
    /// (x, y) one level up or down: floor 0 is the street, positive floors are upstairs, negative are cellars.
    /// </summary>
    public sealed class Building
    {
        public BuildingKind Kind;
        public string Name;
        public int X, Y, W, H;
        public int DoorX, DoorY;
        public int Up, Down;
        public Service Services;
        public Shop Shop;
        public Monster Keeper;
        public int CounterX = -1, CounterY = -1;

        public bool Contains(int x, int y) => x >= X && y >= Y && x < X + W && y < Y + H;
        public bool HasFloor(int z) => z >= -Down && z <= Up;
        /// <summary>A stall has no walls: you never stand "inside" it.</summary>
        public bool Roofed => Kind != BuildingKind.Stall;
    }

    public sealed class Shop
    {
        public ShopKind Kind;
        public string Name;
        public int X, Y;            // the counter cell you bump to trade
        public int Glyph;
        public readonly List<Item> Stock = new List<Item>();
        public int OwnerName;
        public Monster Keeper;
        public int Gold;
    }

    /// <summary>A walled town: streets, a plaza, buildings with several floors, and the people in them.</summary>
    public sealed class Town
    {
        public string Name;
        public string Size = "village";
        public int EntryX, EntryY;
        public readonly Dictionary<int, GameMap> Floors = new Dictionary<int, GameMap>();
        public readonly List<Shop> Shops = new List<Shop>();
        public readonly List<Monster> Npcs = new List<Monster>();
        public readonly List<Building> Buildings = new List<Building>();
        public int Population;
        public int Wealth;
        public int Defense;

        /// <summary>The street level.</summary>
        public GameMap Map => Floors[0];
        public GameMap FloorMap(int z) => Floors.TryGetValue(z, out var m) ? m : null;
        public int MinFloor { get { int lo = 0; foreach (var b in Buildings) lo = Math.Min(lo, -b.Down); return lo; } }
        public int MaxFloor { get { int hi = 0; foreach (var b in Buildings) hi = Math.Max(hi, b.Up); return hi; } }

        public Building BuildingAt(int x, int y, int z, bool roofedOnly = false)
        {
            foreach (var b in Buildings)
                if (b.Contains(x, y) && b.HasFloor(z) && (!roofedOnly || b.Roofed)) return b;
            return null;
        }

        public Shop ShopAt(int x, int y)
        {
            for (int i = 0; i < Shops.Count; i++) if (Shops[i].X == x && Shops[i].Y == y) return Shops[i];
            return null;
        }

        public bool IsGate(int x, int y)
        {
            var m = Map;
            return (x == 0 || y == 0 || x == m.W - 1 || y == m.H - 1) && m.Get(x, y) == TileKind.OpenDoor;
        }
    }

    /// <summary>
    /// Builds a town as a grid of lots on either side of a cross of streets, a plaza in the middle and a
    /// wall with four gates. Buildings are drawn in "door-relative" coordinates (u across, v from the
    /// door inward) so one template serves lots on both sides of the main street.
    /// </summary>
    public static class TownGen
    {
        const int LotW = 13, LotH = 11, StreetW = 5, StreetH = 5;

        sealed class Plan
        {
            public Town T; public Rng R; public int Depth;
            public int W, H;
            public Building B; public bool North;
            public int X(int u) => B.X + u;
            public int Y(int v) => North ? B.Y + B.H - 1 - v : B.Y + v;
            public GameMap Floor(int z)
            {
                if (!T.Floors.TryGetValue(z, out var m))
                {
                    m = new GameMap(W, H) { Number = R.Range(1000000, int.MaxValue / 2), LevelName = T.Name };
                    m.Fill(TileKind.Void);
                    T.Floors[z] = m;
                }
                return m;
            }
            public void Put(int z, int u, int v, TileKind k) => Floor(z).Set(X(u), Y(v), k);
            public TileKind At(int z, int u, int v) => Floor(z).Get(X(u), Y(v));
        }

        public static Town Generate(string name, Rng rng, int depth)
        {
            var t = new Town { Name = name };
            t.Population = rng.Range(40, 420);
            t.Wealth = rng.Range(1, 5);
            t.Defense = rng.Range(1, 5);
            t.Size = t.Population < 110 ? "hamlet" : t.Population < 220 ? "village" : t.Population < 330 ? "town" : "city";
            int cols = t.Size == "hamlet" ? 2 : 3;
            int w = 2 + cols * 2 * LotW + StreetW, h = 2 + LotH * 2 + StreetH;
            var p = new Plan { T = t, R = rng, Depth = depth, W = w, H = h };
            var map = p.Floor(0);
            map.Fill(TileKind.Floor);

            // Wall ring, streets, plaza, lamps.
            for (int x = 0; x < w; x++) { map.Set(x, 0, TileKind.WallAlt); map.Set(x, h - 1, TileKind.WallAlt); }
            for (int y = 0; y < h; y++) { map.Set(0, y, TileKind.WallAlt); map.Set(w - 1, y, TileKind.WallAlt); }
            int sx0 = 1 + cols * LotW, sy0 = 1 + LotH, cx = sx0 + StreetW / 2, cy = sy0 + StreetH / 2;
            for (int x = 1; x < w - 1; x++) for (int y = sy0; y < sy0 + StreetH; y++) map.Set(x, y, TileKind.FloorAlt);
            for (int y = 1; y < h - 1; y++) for (int x = sx0; x < sx0 + StreetW; x++) map.Set(x, y, TileKind.FloorAlt);
            for (int x = cx - 3; x <= cx + 3; x++) for (int y = cy - 2; y <= cy + 2; y++) map.Set(x, y, TileKind.Floor);
            map.Set(cx, cy, TileKind.Fountain);
            foreach (int[] o in new[] { new[] { -3, -2 }, new[] { 3, -2 }, new[] { -3, 2 }, new[] { 3, 2 } }) map.Set(cx + o[0], cy + o[1], TileKind.Pillar);
            for (int x = 4; x < w - 3; x += 8)
            {
                if (Math.Abs(x - cx) <= 4) continue;
                map.Set(x, sy0, TileKind.Pillar); map.Set(x, sy0 + StreetH - 1, TileKind.Pillar);
            }
            map.Set(cx, 0, TileKind.OpenDoor); map.Set(cx, h - 1, TileKind.OpenDoor);
            map.Set(0, cy, TileKind.OpenDoor); map.Set(w - 1, cy, TileKind.OpenDoor);
            t.EntryX = cx; t.EntryY = h - 2;

            // Which buildings this settlement has. The first few are always there; the rest is dice.
            var kinds = ChooseBuildings(t, rng, cols * 4);

            // Lots: north row then south row, west half then east half.
            var lots = new List<int[]>();   // x, y, north?
            for (int row = 0; row < 2; row++)
                for (int c = 0; c < cols * 2; c++)
                {
                    int lx = c < cols ? 1 + c * LotW : sx0 + StreetW + (c - cols) * LotW;
                    lots.Add(new[] { lx, row == 0 ? 1 : sy0 + StreetH, row == 0 ? 1 : 0 });
                }
            rng.Shuffle(lots);
            for (int i = 0; i < lots.Count; i++)
            {
                var lot = lots[i];
                p.North = lot[2] == 1;
                var kind = i < kinds.Count ? kinds[i] : (BuildingKind?)null;
                if (kind == null) { Yard(p, lot[0], lot[1], true); continue; }
                if (kind == BuildingKind.Cottage) { Cottages(p, lot[0], lot[1]); continue; }
                if (kind == BuildingKind.Stall) { Market(p, lot[0], lot[1]); continue; }
                Place(p, kind.Value, lot[0], lot[1]);
            }

            Populate(p, cx, cy, sy0);
            return t;
        }

        static List<BuildingKind> ChooseBuildings(Town t, Rng rng, int lots)
        {
            var must = new List<BuildingKind> { BuildingKind.Tavern, BuildingKind.General, BuildingKind.Smithy, BuildingKind.Temple };
            var pool = new List<BuildingKind>();
            if (lots >= 12)
            {
                must.AddRange(new[] { BuildingKind.Inn, BuildingKind.Alchemist, BuildingKind.Armoury, BuildingKind.Emporium, BuildingKind.Guild });
                pool.AddRange(new[] { BuildingKind.Library, BuildingKind.Barracks, BuildingKind.Stall, BuildingKind.Watchtower, BuildingKind.Townhouse, BuildingKind.Cottage });
            }
            else
            {
                must.Add(BuildingKind.Alchemist); must.Add(BuildingKind.Inn);
                pool.AddRange(new[] { BuildingKind.Armoury, BuildingKind.Cottage, BuildingKind.Stall, BuildingKind.Cottage });
            }
            rng.Shuffle(pool);
            var all = new List<BuildingKind>(must);
            for (int i = 0; all.Count < lots - 1 && i < pool.Count; i++) all.Add(pool[i]);
            while (all.Count < lots - 1) all.Add(BuildingKind.Cottage);
            return all;
        }

        // ---------------------------------------------------------------- lots

        /// <summary>Open ground: trees, the odd grave. Never touches the cells next to a building.</summary>
        static void Yard(Plan p, int lx, int ly, bool graves)
        {
            var map = p.Floor(0);
            bool cemetery = graves && p.R.Chance(45);
            for (int y = ly; y < ly + LotH; y++)
                for (int x = lx; x < lx + LotW; x++)
                {
                    if (map.Get(x, y) != TileKind.Floor) continue;
                    float h = Theme.Hash01(x * 5 + 3, y * 11 + 7);
                    bool edge = (p.North ? y >= ly + LotH - 2 : y <= ly + 1);   // keep the street frontage clear
                    if (edge) continue;
                    if (cemetery && x > lx && x < lx + LotW - 1 && (x % 2 == 0) && (y % 2 == 0) && y > ly + 1 && y < ly + LotH - 2) map.Set(x, y, TileKind.Grave);
                    else if (h > 0.80f) map.Set(x, y, TileKind.Tree);
                }
        }

        static void Cottages(Plan p, int lx, int ly)
        {
            for (int i = 0; i < 2; i++)
            {
                var b = Frame(p, BuildingKind.Cottage, 5, 5, lx + 1 + i * 6, ly);
                b.Name = NameFor(p.R, BuildingKind.Cottage);
                for (int z = 0; z <= b.Up; z++) Shell(p, b, z);
                Door(p, b);
                p.Put(0, 1, 3, TileKind.Bed); p.Put(0, 3, 3, TileKind.Table); p.Put(0, 2, 3, TileKind.Hearth);
                Person(p, b, TownRole.Citizen, 0, 2, 2, 1, null);
            }
            Yard(p, lx, ly, false);
        }

        static void Market(Plan p, int lx, int ly)
        {
            var kinds = new[] { ShopKind.Food, ShopKind.Jewel, ShopKind.General };
            for (int i = 0; i < 3; i++)
            {
                int bx = lx + 1 + i * 4;
                var b = new Building { Kind = BuildingKind.Stall, Name = StallName(p.R, kinds[i]), X = bx, Y = p.North ? ly + LotH - 2 : ly, W = 3, H = 2 };
                p.B = b;
                // The counter faces the street (v = 0); the vendor stands behind it.
                p.Put(0, 0, 0, TileKind.Counter); p.Put(0, 1, 0, TileKind.Counter); p.Put(0, 2, 0, TileKind.Counter);
                b.CounterX = p.X(1); b.CounterY = p.Y(0);
                p.T.Buildings.Add(b);
                Vendor(p, b, kinds[i], 1, 1, TownRole.Shopkeeper);
            }
            Yard(p, lx, ly, false);
        }

        // ------------------------------------------------------------ buildings

        static Building Frame(Plan p, BuildingKind kind, int bw, int bh, int bx, int lotY)
        {
            int by = p.North ? lotY + LotH - bh : lotY;
            var b = new Building { Kind = kind, W = bw, H = bh, X = bx, Y = by };
            b.DoorX = bx + bw / 2; b.DoorY = p.North ? by + bh - 1 : by;
            p.B = b;
            p.T.Buildings.Add(b);
            return b;
        }

        /// <summary>Walls and a wooden floor for floor z, on that floor's own map.</summary>
        static void Shell(Plan p, Building b, int z)
        {
            p.B = b;
            var m = p.Floor(z);
            for (int y = b.Y; y < b.Y + b.H; y++)
                for (int x = b.X; x < b.X + b.W; x++)
                {
                    bool wall = x == b.X || y == b.Y || x == b.X + b.W - 1 || y == b.Y + b.H - 1;
                    m.Set(x, y, wall ? TileKind.WallAlt : TileKind.FloorAlt);
                }
        }

        static void Door(Plan p, Building b) { p.B = b; p.Floor(0).Set(b.DoorX, b.DoorY, TileKind.OpenDoor); }

        static void Place(Plan p, BuildingKind kind, int lx, int ly)
        {
            int bw, bh, up = 0, down = 0;
            switch (kind)
            {
                case BuildingKind.Smithy: bw = 9; bh = 6; break;
                case BuildingKind.Armoury: bw = 9; bh = 6; up = 1; break;
                case BuildingKind.Alchemist: bw = 9; bh = 6; down = 1; break;
                case BuildingKind.Emporium: bw = 9; bh = 9; up = 3; break;
                case BuildingKind.General: bw = 9; bh = 6; down = 1; break;
                case BuildingKind.Tavern: bw = 11; bh = 8; down = 1; break;
                case BuildingKind.Inn: bw = 11; bh = 8; up = 2; break;
                case BuildingKind.Temple: bw = 11; bh = 9; up = 1; down = 1; break;
                case BuildingKind.Guild: bw = 11; bh = 8; up = 1; break;
                case BuildingKind.Library: bw = 11; bh = 8; up = 1; break;
                case BuildingKind.Barracks: bw = 11; bh = 8; up = 1; down = 1; break;
                case BuildingKind.Townhouse: bw = 7; bh = 7; up = 1; break;
                default: bw = 5; bh = 5; up = 3; break;   // watchtower
            }
            var b = Frame(p, kind, bw, bh, lx + (LotW - bw) / 2, ly);
            b.Up = up; b.Down = down;
            b.Name = NameFor(p.R, kind);
            for (int z = -down; z <= up; z++) Shell(p, b, z);
            Door(p, b);
            Yard(p, lx, ly, kind != BuildingKind.Barracks);
            Stairs(p, b);

            switch (kind)
            {
                case BuildingKind.Smithy: Shopfront(p, b, ShopKind.Weapon, TownRole.Smith, Service.Hone); break;
                case BuildingKind.Armoury: Shopfront(p, b, ShopKind.Armor, TownRole.Shopkeeper, Service.Hone); break;
                case BuildingKind.Alchemist: Shopfront(p, b, ShopKind.potion, TownRole.Shopkeeper, Service.None); break;
                case BuildingKind.Emporium: Shopfront(p, b, ShopKind.Wand, TownRole.Scholar, Service.Appraise); break;
                case BuildingKind.General: Shopfront(p, b, ShopKind.General, TownRole.Shopkeeper, Service.None); break;
                case BuildingKind.Library: Shopfront(p, b, ShopKind.Book, TownRole.Scholar, Service.Appraise); break;
                case BuildingKind.Tavern: Tavern(p, b); break;
                case BuildingKind.Inn: Inn(p, b); break;
                case BuildingKind.Temple: Temple(p, b); break;
                case BuildingKind.Guild: Guild(p, b); break;
                case BuildingKind.Barracks: Barracks(p, b); break;
                case BuildingKind.Townhouse: Townhouse(p, b); break;
                default: Watchtower(p, b); break;
            }
            Upstairs(p, b);
        }

        /// <summary>
        /// Staircases alternate between two corner cells so the one you arrive on is never the one
        /// that leads onward: floor z connects to z+1 at corner A when z is even and at corner B when odd.
        /// </summary>
        static void Stairs(Plan p, Building b)
        {
            p.B = b;
            int ua = 1, ub = b.W - 2;
            for (int z = 0; z < b.Up; z++)
            {
                int u = z % 2 == 0 ? ua : ub;
                p.Put(z, u, 1, TileKind.StairsUp);
                p.Put(z + 1, u, 1, TileKind.StairsDown);
            }
            if (b.Down > 0)
            {
                p.Put(0, ub, 1, TileKind.StairsDown);
                p.Put(-1, ub, 1, TileKind.StairsUp);
            }
        }

        static void Shopfront(Plan p, Building b, ShopKind kind, TownRole role, Service services)
        {
            p.B = b;
            int vc = b.H - 3, vb = b.H - 2, ku = b.W / 2;
            for (int u = 1; u <= b.W - 2; u++)
            {
                p.Put(0, u, vc, TileKind.Counter);
                bool end = u == 1 || u == b.W - 2;
                TileKind back = b.Kind == BuildingKind.Smithy && end ? TileKind.Forge : end ? TileKind.Barrel : TileKind.Shelf;
                if (u != ku) p.Put(0, u, vb, back);
            }
            if (b.Kind == BuildingKind.Library || b.Kind == BuildingKind.Emporium)
                for (int u = 2; u <= b.W - 3; u++)
                    for (int v = 2; v <= vc - 2; v++)
                        if (Math.Abs(u - ku) >= 2) p.Put(0, u, v, TileKind.Shelf);
            b.CounterX = p.X(ku); b.CounterY = p.Y(vc);
            b.Services = services;
            var shop = MakeShop(p, b, kind);
            var keeper = Vendor(p, b, kind, ku, vb, role);
            b.Keeper = keeper;
            shop.Keeper = keeper;
            // A second pair of hands: someone browsing the stock.
            if (p.R.Chance(70)) Person(p, b, TownRole.Citizen, 0, ku + 1, 1, 1, null);
        }

        static Shop MakeShop(Plan p, Building b, ShopKind kind)
        {
            var shop = new Shop
            {
                Kind = kind, Name = b.Name, X = b.CounterX, Y = b.CounterY,
                OwnerName = p.R.Range(0, 1000), Gold = p.R.Range(400, 2500) + p.Depth * 200
            };
            StockShop(shop, p.R, p.Depth);
            b.Shop = shop;
            p.T.Shops.Add(shop);
            return shop;
        }

        /// <summary>A shopkeeper standing behind the counter cell of a building (or stall).</summary>
        static Monster Vendor(Plan p, Building b, ShopKind kind, int u, int v, TownRole role)
        {
            p.B = b;
            if (b.Kind == BuildingKind.Stall)
            {
                var shop = MakeShop(p, b, kind);
                shop.Name = b.Name;
                var m = Person(p, b, role, 0, u, v, 0, shop);
                b.Keeper = m; shop.Keeper = m;
                return m;
            }
            return Person(p, b, role, 0, u, v, 0, b.Shop);
        }

        static void Tavern(Plan p, Building b)
        {
            p.B = b;
            b.Services = Service.Ale | Service.Meal | Service.Rumor;
            for (int u = 1; u <= 5; u++) p.Put(0, u, 5, TileKind.Counter);
            p.Put(0, 1, 6, TileKind.Barrel); p.Put(0, 2, 6, TileKind.Barrel); p.Put(0, 4, 6, TileKind.Shelf); p.Put(0, 5, 6, TileKind.Shelf);
            p.Put(0, 9, 6, TileKind.Hearth);
            foreach (int[] t in new[] { new[] { 3, 2 }, new[] { 7, 2 }, new[] { 7, 4 }, new[] { 8, 4 } }) p.Put(0, t[0], t[1], TileKind.Table);
            b.CounterX = p.X(3); b.CounterY = p.Y(5);
            b.Keeper = Person(p, b, TownRole.Barkeep, 0, 3, 6, 0, null);
            Person(p, b, TownRole.Bard, 0, 8, 2, 1, null);
            Person(p, b, TownRole.Drunk, 0, 6, 3, 1, null);
            if (p.R.Chance(70)) Person(p, b, TownRole.Adventurer, 0, 2, 3, 1, null);
            // Cellar: casks, a cat, nobody who is supposed to be there.
            Storeroom(p, b, -1, 0.7f);
            if (p.R.Chance(80)) Pet(p, b, -1, 4, 3);
        }

        static void Inn(Plan p, Building b)
        {
            p.B = b;
            b.Services = Service.Rest | Service.Meal | Service.Rumor;
            for (int u = 1; u <= 4; u++) p.Put(0, u, 5, TileKind.Counter);
            p.Put(0, 1, 6, TileKind.Shelf); p.Put(0, 3, 6, TileKind.Shelf); p.Put(0, 4, 6, TileKind.Barrel);
            p.Put(0, 9, 6, TileKind.Hearth);
            p.Put(0, 7, 3, TileKind.Table); p.Put(0, 8, 3, TileKind.Table);
            b.CounterX = p.X(2); b.CounterY = p.Y(5);
            b.Keeper = Person(p, b, TownRole.Innkeeper, 0, 2, 6, 0, null);
            if (p.R.Chance(60)) Person(p, b, TownRole.Citizen, 0, 7, 4, 1, null);
            // Guest rooms: a partition with two doors, beds along the back wall.
            for (int z = 1; z <= b.Up; z++)
            {
                for (int u = 1; u <= b.W - 2; u++) p.Put(z, u, 3, TileKind.WallAlt);
                p.Put(z, 3, 3, TileKind.OpenDoor); p.Put(z, 7, 3, TileKind.OpenDoor);
                for (int u = 2; u <= 8; u += 2) p.Put(z, u, 6, TileKind.Bed);
                for (int u = 2; u <= 8; u += 3) p.Put(z, u, 4, TileKind.Table);
                if (p.R.Chance(65)) Person(p, b, TownRole.Citizen, z, 5 + p.R.Range(-1, 2), 5, 1, null);
            }
        }

        static void Temple(Plan p, Building b)
        {
            p.B = b;
            b.Services = Service.Heal | Service.Cure | Service.Donate | Service.Rumor;
            p.Put(0, 5, 7, TileKind.Altar);
            for (int v = 3; v <= 5; v += 2)
                for (int u = 2; u <= 8; u++) if (u != 5) p.Put(0, u, v, TileKind.Table);   // pews, with an aisle
            b.CounterX = p.X(5); b.CounterY = p.Y(7);
            b.Keeper = Person(p, b, TownRole.Priest, 0, 5, 6, 0, null);
            if (p.R.Chance(60)) Person(p, b, TownRole.Citizen, 0, 3, 2, 1, null);
            // Choir loft above, crypt below.
            for (int u = 2; u <= 8; u += 2) p.Put(1, u, 5, TileKind.Table);
            Person(p, b, TownRole.Scholar, 1, 5, 3, 1, null);
            for (int v = 2; v <= 6; v += 2)
                for (int u = 2; u <= 8; u += 2) if (!(u == b.W - 2 && v == 1)) p.Put(-1, u, v, TileKind.Grave);
            for (int v = 3; v <= 5; v += 2) p.Put(-1, 1, v, TileKind.Pillar);
            Person(p, b, TownRole.Citizen, -1, 5, 3, 1, null).Name = "the gravekeeper";
        }

        static void Guild(Plan p, Building b)
        {
            p.B = b;
            b.Services = Service.Quest | Service.Rumor;
            for (int u = 2; u <= 8; u += 3) p.Put(0, u, 6, TileKind.Board);
            p.Put(0, 5, 3, TileKind.Table); p.Put(0, 4, 3, TileKind.Table);
            b.CounterX = p.X(2); b.CounterY = p.Y(6);   // the notice boards
            b.Keeper = Person(p, b, TownRole.Elder, 0, 6, 5, 0, null);
            Person(p, b, TownRole.Adventurer, 0, 8, 3, 1, null);
            if (p.R.Chance(60)) Person(p, b, TownRole.Adventurer, 0, 3, 4, 1, null);
            // Training hall upstairs: racks and a sparring veteran.
            for (int u = 2; u <= 8; u++) p.Put(1, u, 6, TileKind.Shelf);
            p.Put(1, 5, 3, TileKind.Table);
            Person(p, b, TownRole.Adventurer, 1, 4, 4, 2, null);
        }

        static void Barracks(Plan p, Building b)
        {
            p.B = b;
            b.Services = Service.Rumor;
            for (int u = 2; u <= 8; u += 3) p.Put(0, u, 6, TileKind.Shelf);
            p.Put(0, 5, 3, TileKind.Table);
            b.Keeper = Person(p, b, TownRole.Captain, 0, 5, 5, 1, null);
            Person(p, b, TownRole.Guard, 0, 8, 4, 1, null);
            for (int u = 2; u <= 8; u += 2) { p.Put(1, u, 6, TileKind.Bed); p.Put(1, u, 5, TileKind.Table); }
            Person(p, b, TownRole.Guard, 1, 4, 3, 1, null);
            // Jail: three cells behind bars, a prisoner in each of two.
            for (int v = 2; v <= 6; v++) { p.Put(-1, 4, v, TileKind.WallAlt); p.Put(-1, 7, v, TileKind.WallAlt); }
            p.Put(-1, 4, 4, TileKind.OpenDoor); p.Put(-1, 7, 4, TileKind.OpenDoor);
            Person(p, b, TownRole.Prisoner, -1, 2, 4, 0, null);
            Person(p, b, TownRole.Prisoner, -1, 9, 5, 0, null);
            Person(p, b, TownRole.Guard, -1, 5, 3, 1, null);
        }

        static void Townhouse(Plan p, Building b)
        {
            p.B = b;
            p.Put(0, 3, 5, TileKind.Table); p.Put(0, 5, 5, TileKind.Hearth);
            Person(p, b, TownRole.Citizen, 0, 3, 3, 1, null);
            p.Put(1, 1, 5, TileKind.Bed); p.Put(1, 5, 5, TileKind.Bed); p.Put(1, 3, 3, TileKind.Table);
            if (p.R.Chance(60)) Person(p, b, TownRole.Child, 1, 3, 4, 1, null);
        }

        static void Watchtower(Plan p, Building b)
        {
            p.B = b;
            for (int z = 0; z <= b.Up; z++)
            {
                if (z > 0) p.Put(z, 2, 3, TileKind.Table);
                Person(p, b, TownRole.Guard, z, 2, 2, z == 0 ? 0 : 1, null);
            }
        }

        /// <summary>Barrels and shelves around the edges of a floor that is only storage.</summary>
        static void Storeroom(Plan p, Building b, int z, float density)
        {
            p.B = b;
            for (int v = 1; v <= b.H - 2; v++)
                for (int u = 1; u <= b.W - 2; u++)
                {
                    bool edge = u == 1 || u == b.W - 2 || v == b.H - 2;
                    if (!edge || p.At(z, u, v) != TileKind.FloorAlt) continue;
                    if (u == b.W - 2 && v == 1) continue;
                    if (u == 1 && v == 1) continue;
                    if (Theme.Hash01(p.X(u) * 3 + z, p.Y(v) * 5) < density) p.Put(z, u, v, Theme.Hash01(p.X(u), p.Y(v) + 9) < 0.5f ? TileKind.Barrel : TileKind.Shelf);
                }
        }

        /// <summary>Upper floors and cellars that no template furnished: storage below, bedrooms above.</summary>
        static void Upstairs(Plan p, Building b)
        {
            p.B = b;
            for (int z = -b.Down; z <= b.Up; z++)
            {
                if (z == 0 || FloorFurnished(p, b, z)) continue;
                if (z < 0) { Storeroom(p, b, z, 0.8f); if (p.R.Chance(50)) Pet(p, b, z, b.W / 2, 3); continue; }
                if (b.Kind == BuildingKind.Emporium)
                {
                    // Laboratory, library, observatory: a wizard's tower, bottom to top.
                    for (int u = 2; u <= b.W - 3; u += 2) p.Put(z, u, b.H - 2, z == 2 ? TileKind.Shelf : TileKind.Table);
                    if (z == 1) { p.Put(z, 4, 4, TileKind.Hearth); p.Put(z, 3, 3, TileKind.Barrel); }
                    Person(p, b, TownRole.Scholar, z, 4, 3, 1, null);
                }
                else Storeroom(p, b, z, 0.6f);
            }
        }

        static bool FloorFurnished(Plan p, Building b, int z)
        {
            var m = p.Floor(z);
            for (int v = 1; v <= b.H - 2; v++)
                for (int u = 1; u <= b.W - 2; u++)
                {
                    var k = p.At(z, u, v);
                    if (k != TileKind.FloorAlt && k != TileKind.StairsUp && k != TileKind.StairsDown) return true;
                }
            return false;
        }

        // --------------------------------------------------------------- people

        static readonly int[] CivilColours = { 0xB89A78, 0xA8B0C8, 0xC8A0A0, 0x98B898, 0xC0B070, 0xA090C0, 0xD0A060 };

        static Monster Person(Plan p, Building b, TownRole role, int z, int u, int v, int leash, Shop shop)
        {
            p.B = b;
            return MakePerson(p, role, z, p.X(u), p.Y(v), leash, shop, b);
        }

        static Monster MakePerson(Plan p, TownRole role, int z, int x, int y, int leash, Shop shop, Building home)
        {
            bool guard = role == TownRole.Guard || role == TownRole.Captain;
            var def = Bestiary.Find(guard ? "dwarf" : "hobbit");
            def.Level = 0;
            def.Glyph = '@';
            switch (role)
            {
                case TownRole.Shopkeeper: case TownRole.Smith: case TownRole.Innkeeper: case TownRole.Barkeep: def.Color = 0xE8C060; break;
                case TownRole.Priest: def.Color = 0xF4F0FF; break;
                case TownRole.Elder: def.Color = 0xC8A0F0; break;
                case TownRole.Scholar: def.Color = 0x7FD0F0; break;
                case TownRole.Bard: def.Color = 0xFF8AD0; break;
                case TownRole.Drunk: def.Color = 0xD08060; break;
                case TownRole.Adventurer: def.Color = 0xF0A050; break;
                case TownRole.Beggar: case TownRole.Prisoner: def.Color = 0x8A8478; break;
                case TownRole.Guard: case TownRole.Captain: def.Color = 0x8FB0E8; break;
                case TownRole.Child: def.Color = 0xE0D890; def.Glyph = 'i'; break;
                case TownRole.Pet: def.Color = 0xC8B090; def.Glyph = 'd'; def.Name = "stray dog"; break;
                default: def.Color = CivilColours[p.R.Range(0, CivilColours.Length)]; break;
            }
            var m = new Monster(def, p.R)
            {
                Townsperson = true, Role = role, Floor = z, Leash = leash, Voice = p.R.Range(0, 1000),
                Shop = shop, Home = home, Dormant = true, Alert = 0, IsGuard = guard, IsPriest = role == TownRole.Priest,
                X = x, Y = y, HomeX = x, HomeY = y,
            };
            m.Name = role == TownRole.Pet ? def.Name : TownText.GivenName(p.R);
            p.T.Npcs.Add(m);
            return m;
        }

        static void Pet(Plan p, Building b, int z, int u, int v)
        {
            p.B = b;
            var m = MakePerson(p, TownRole.Pet, z, p.X(u), p.Y(v), 2, null, b);
            if (p.R.Chance(55)) { m.Def.Glyph = 'f'; m.Glyph = 'f'; m.Def.Name = "cat"; m.Name = "cat"; m.Def.Color = 0xD8B070; }
        }

        /// <summary>Everyone who lives out on the streets: guards at the gates, citizens, children, a bard, a beggar, a dog.</summary>
        static void Populate(Plan p, int cx, int cy, int sy0)
        {
            var t = p.T; var map = t.Map;
            int w = map.W, h = map.H;
            // One guard beside each gate, never in the way of it.
            foreach (int[] g in new[] { new[] { cx + 1, 1 }, new[] { cx + 1, h - 2 }, new[] { 1, cy + 1 }, new[] { w - 2, cy + 1 } })
            {
                var at = Spot(p, g[0], g[1]);
                if (at != null) MakePerson(p, TownRole.Guard, 0, at[0], at[1], 0, null, null);
            }
            for (int i = 0; i < t.Defense; i++)
            {
                int[] c = StreetCell(p, sy0, cx, cy);
                if (c != null) MakePerson(p, TownRole.Guard, 0, c[0], c[1], 10, null, null);
            }
            int citizens = t.Size == "hamlet" ? 4 : t.Size == "village" ? 7 : t.Size == "town" ? 10 : 14;
            for (int i = 0; i < citizens; i++)
            {
                int[] c = StreetCell(p, sy0, cx, cy);
                if (c != null) MakePerson(p, i % 4 == 3 ? TownRole.Child : TownRole.Citizen, 0, c[0], c[1], 8, null, null);
            }
            var bard = Spot(p, cx + 2, cy + 1); if (bard != null) MakePerson(p, TownRole.Bard, 0, bard[0], bard[1], 2, null, null);
            var beggar = Spot(p, cx - 2, cy + 2); if (beggar != null) MakePerson(p, TownRole.Beggar, 0, beggar[0], beggar[1], 1, null, null);
            var hero = Spot(p, cx + 3, cy - 1);
            if (hero != null && p.R.Chance(70)) MakePerson(p, TownRole.Adventurer, 0, hero[0], hero[1], 3, null, null);
            for (int i = 0; i < 1 + p.R.Range(0, 3); i++)
            {
                int[] c = StreetCell(p, sy0, cx, cy);
                if (c == null) continue;
                var d = MakePerson(p, TownRole.Pet, 0, c[0], c[1], 9, null, null);
                if (p.R.Chance(45)) { d.Def.Glyph = 'f'; d.Glyph = 'f'; d.Def.Name = "cat"; d.Name = "cat"; d.Def.Color = 0xD8B070; }
            }
        }

        /// <summary>The nearest free street-level cell to (x, y): open ground, nobody on it, not the cell you arrive on.</summary>
        static int[] Spot(Plan p, int x, int y)
        {
            var map = p.T.Map;
            for (int r = 0; r <= 4; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int nx = x + dx, ny = y + dy;
                        var k = map.Get(nx, ny);
                        if (k != TileKind.Floor && k != TileKind.FloorAlt) continue;
                        if (nx == p.T.EntryX && ny == p.T.EntryY) continue;
                        bool taken = false;
                        foreach (var n in p.T.Npcs) if (n.Floor == 0 && n.X == nx && n.Y == ny) { taken = true; break; }
                        if (!taken) return new[] { nx, ny };
                    }
            return null;
        }

        static int[] StreetCell(Plan p, int sy0, int cx, int cy)
        {
            var map = p.T.Map;
            for (int tries = 0; tries < 60; tries++)
            {
                bool eastWest = p.R.Chance(55);
                int x = eastWest ? p.R.Range(2, map.W - 2) : cx + p.R.Range(-2, 3);
                int y = eastWest ? sy0 + p.R.Range(0, StreetH) : p.R.Range(2, map.H - 2);
                if (map.Get(x, y) != TileKind.FloorAlt && map.Get(x, y) != TileKind.Floor) continue;
                if (Math.Abs(x - cx) <= 1 && Math.Abs(y - cy) <= 1) continue;
                bool taken = false;
                foreach (var n in p.T.Npcs) if (n.Floor == 0 && n.X == x && n.Y == y) { taken = true; break; }
                if (!taken && !(x == p.T.EntryX && y == p.T.EntryY)) return new[] { x, y };
            }
            return null;
        }

        // ---------------------------------------------------------------- stock

        public static int FindFloorNear(GameMap map, int x, int y, int radius)
        {
            for (int r = 0; r <= radius; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int nx = x + dx, ny = y + dy;
                        if (!map.InBounds(nx, ny)) continue;
                        if (map.Get(nx, ny) == TileKind.Floor) return nx + ny * map.W;
                    }
            return -1;
        }

        public static void StockShop(Shop shop, Rng rng, int depth)
        {
            int n = 5 + rng.Range(0, 4);
            for (int i = 0, tries = 0; i < n && tries < 400; tries++)
            {
                var item = LevelBuilder.RollLoot(rng, Math.Max(1, depth - 2));
                if (!Matches(shop.Kind, item.Def)) continue;
                item.Identified = true;
                shop.Stock.Add(item); i++;
            }
            // Staples a shop of that kind never runs out of, so no town leaves you without a lamp or a meal.
            switch (shop.Kind)
            {
                case ShopKind.Food: Basics(shop, rng, Catalogue.Food, 4); break;
                case ShopKind.potion: Basics(shop, rng, Catalogue.Potions, 4); break;
                case ShopKind.Weapon: Basics(shop, rng, Catalogue.Weapons, 3); break;
                case ShopKind.Armor: Basics(shop, rng, Catalogue.Armor, 2); Basics(shop, rng, Catalogue.Shields, 1); break;
                case ShopKind.Wand: Basics(shop, rng, Catalogue.Scrolls, 3); Basics(shop, rng, Catalogue.Wands, 2); break;
                case ShopKind.Book: Basics(shop, rng, Catalogue.Books, 3); break;
                case ShopKind.General: Basics(shop, rng, Catalogue.Tools, 3); Basics(shop, rng, Catalogue.Food, 2); break;
                case ShopKind.Jewel: Basics(shop, rng, Catalogue.Rings, 2); Basics(shop, rng, Catalogue.Ornaments, 2); break;
            }
        }

        static void Basics(Shop shop, Rng rng, IReadOnlyList<ItemDef> list, int count)
        {
            if (list.Count == 0) return;
            for (int i = 0; i < count; i++)
                shop.Stock.Add(new Item(list[rng.Range(0, list.Count)], rng, GroundItems.NextUid()) { Identified = true });
        }

        static bool Matches(ShopKind kind, ItemDef d)
        {
            switch (kind)
            {
                case ShopKind.Weapon: return d.Kind == ItemKind.Weapon;
                case ShopKind.Armor: return d.Kind.IsWearable();
                case ShopKind.potion: return d.Kind == ItemKind.Potion;
                case ShopKind.Scroll: return d.Kind == ItemKind.Scroll;
                case ShopKind.Wand: return d.Kind == ItemKind.Wand || d.Kind == ItemKind.Scroll;
                case ShopKind.Food: return d.Kind == ItemKind.Food;
                case ShopKind.Tool: return d.Kind == ItemKind.Tool;
                case ShopKind.Jewel: return d.Kind == ItemKind.Ring || d.Kind == ItemKind.Amulet || d.Kind == ItemKind.Gem || d.Kind == ItemKind.Ornament;
                case ShopKind.Book: return d.Kind == ItemKind.Book || d.Kind == ItemKind.Scroll;
                case ShopKind.General: return d.Kind == ItemKind.Tool || d.Kind == ItemKind.Ornament || d.Kind == ItemKind.Gem || d.Kind == ItemKind.Rock || d.Kind == ItemKind.Food;
                case ShopKind.Temple: return d.Kind == ItemKind.Scroll || d.Kind == ItemKind.Book;
                default: return false;
            }
        }

        public static string NameFor(Rng rng, BuildingKind kind) => TownText.BuildingName(rng, kind);
        static string StallName(Rng rng, ShopKind kind) => TownText.StallName(rng, kind);
    }
}
