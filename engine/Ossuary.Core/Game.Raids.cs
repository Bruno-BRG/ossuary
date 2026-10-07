using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Raids on towns. Some weeks a monster band (kobolds near the coast roads, orcs further in, the dead in the worst country)
    /// comes for a town on one day of the week. If the hero is there that day, the raiders come over the wall and the hero can
    /// fight them in the streets: kill them all and the town remembers. If the hero is away (or leaves them to it), the Watch
    /// holds or it does not: an undefended town loses a shop to fire and some of its people, and the burnt shop stays burnt.
    /// When a raid happens is a pure function of seed, town and week; what came of it is stored on the town.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Per town: the weeks whose raid has been settled, and whether the hero beat the raid off.</summary>
        readonly Dictionary<string, HashSet<int>> _raidsSettled = new Dictionary<string, HashSet<int>>();
        /// <summary>What raids did, for the legends: "raided Ashford on day 12".</summary>
        public readonly List<string> RaidChronicle = new List<string>();

        public struct Raid
        {
            public int Week, Day, Strength;
            public string Band;
        }

        /// <summary>This town's raid in a given week, or null: one week in six or so, more in dangerous country.</summary>
        public Raid? RaidIn(string town, int week)
        {
            if (World == null || town == null) return null;
            uint h = Rumours.Hash(Rng.Seed ^ 0x7A1D5UL, town, week);
            int depth = World.RegionAt(World.PlayerX, World.PlayerY).Depth;
            if (week < 1 || h % 100 >= (uint)Math.Min(35, 12 + depth * 2)) return null;
            string band = depth < 4 ? "kobold" : depth < 9 ? "orc" : "human zombie";
            if (!Bestiary.TryGet(band, out _)) band = "orc";
            return new Raid { Week = week, Day = week * 7 + (int)((h >> 8) % 7), Strength = 3 + (int)((h >> 12) % 3) + depth / 4, Band = band };
        }

        HashSet<int> Settled(string town) { if (!_raidsSettled.TryGetValue(town, out var s)) { s = new HashSet<int>(); _raidsSettled[town] = s; } return s; }

        /// <summary>Raiders still in the streets.</summary>
        public List<Monster> Raiders => Town == null ? new List<Monster>() : Town.Npcs.FindAll(n => n.Raider && !n.IsDead);

        /// <summary>Arriving: raids that came while the hero was away are settled; a raid due today comes over the wall now.</summary>
        void RaidsOnArrival()
        {
            if (Town == null || World == null) return;
            int week = MarketDay / 7;
            var settled = Settled(Town.Name);
            for (int w = Math.Max(1, week - 4); w <= week; w++)
            {
                if (settled.Contains(w)) continue;
                var raid = RaidIn(Town.Name, w);
                if (raid == null) { if (w < week) settled.Add(w); continue; }
                var r = raid.Value;
                if (r.Day < MarketDay) { settled.Add(w); RaidWhileAway(r); }
                else if (r.Day == MarketDay) { settled.Add(w); RaidNow(r); }
            }
        }

        /// <summary>The raiders come over the wall by the gate.</summary>
        void RaidNow(Raid r)
        {
            var def = Bestiary.Find(r.Band);
            int placed = 0;
            for (int i = 0; i < 60 && placed < r.Strength; i++)
            {
                int x = Town.EntryX + (i % 7) - 3, y = Town.EntryY - 1 - i / 7;
                if (!Map.InBounds(x, y) || !Map.Walkable(x, y) || MonsterAt(x, y) != null || (x == Player.X && y == Player.Y)) continue;
                var m = new Monster(def, Rng) { X = x, Y = y, HomeX = x, HomeY = y, Floor = 0, Alert = 1, Raider = true, Uid = NextUid() };
                Town.Npcs.Add(m);
                if (TownZ == 0) Monsters.Add(m);
                placed++;
            }
            Say($"Raiders at the gate! {placed} {r.Band}s come over the wall.", MessageKind.Bad);
            Danger();
        }

        /// <summary>A raider falls; when the last one does, the town is saved and says so.</summary>
        void RaiderKilled(Monster m)
        {
            if (!m.Raider || Town == null) return;
            Town.Npcs.Remove(m);
            if (Raiders.Count > 0) return;
            int reward = 60 + 10 * Player.Level;
            Player.Gold += reward;
            AddRep(Houses.Watch, 10, "beating off a raid");
            AddRep(Houses.Guild, 3, null);
            RecordDeed(Deed.Helped, Town.Name, 4);
            RaidChronicle.Add(TownText.L($"{Player.CharName} beat off a raid on {Town.Name} on day {MarketDay}.", $"{Player.CharName} rechaçou um ataque a {Town.Name} no dia {MarketDay}."));
            Say($"The last raider falls. The elder of {Town.Name} presses {reward} gold on you, and the street cheers your name.", MessageKind.Good);
        }

        /// <summary>The hero walks out on a raid in progress: the town fends for itself.</summary>
        void RaidAbandoned()
        {
            if (Town == null) return;
            var left = Raiders;
            if (left.Count == 0) return;
            foreach (var m in left) Town.Npcs.Remove(m);
            AddRep(Houses.Watch, -5, "leaving a town to its raiders");
            RaidLosses(left[0].Def.Name, MarketDay);
        }

        /// <summary>A raid that came while the hero was elsewhere: the Watch holds, or the town pays.</summary>
        void RaidWhileAway(Raid r)
        {
            uint h = Rumours.Hash(Rng.Seed ^ 0xDEF3UL, Town.Name, r.Week);
            int hold = Town.Defense * 20 + (int)(h % 40);
            if (hold >= r.Strength * 15)
            {
                Say($"On day {r.Day} a band of {r.Band}s raided {Town.Name}, and the Watch beat them off.", MessageKind.Info);
                RaidChronicle.Add(TownText.L($"The Watch of {Town.Name} beat off a raid of {r.Band}s on day {r.Day}.", $"A Guarda de {Town.Name} rechaçou um ataque de {Loc.PtOf(r.Band)}s no dia {r.Day}."));
                return;
            }
            RaidLosses(r.Band, r.Day);
        }

        /// <summary>An undefended town loses a shop to fire and some of its people. The shop stays burnt.</summary>
        void RaidLosses(string band, int day)
        {
            uint h = Rumours.Hash(Rng.Seed ^ 0xB0E7UL, Town.Name, day);
            var shops = Town.Buildings.FindAll(b => b.Shop != null && !b.Burned && b.Kind != BuildingKind.Temple && b.Kind != BuildingKind.Guild && b.Kind != BuildingKind.Stall);
            string burnt = null;
            if (shops.Count > 0)
            {
                var b = shops[(int)(h % (uint)shops.Count)];
                Burn(b);
                burnt = b.Name;
            }
            var people = Town.Npcs.FindAll(n => !n.Raider && !n.IsDead && !n.IsGuard && n.Role != TownRole.Pet && !Persona.IsEssentialRole(n.Role) && n.Floor == 0 && (n.Persona == null || !n.Persona.Troubled));
            int dead = Math.Min(people.Count, 1 + (int)((h >> 8) % 3));
            for (int i = 0; i < dead; i++)
            {
                var victim = people[(int)((h >> (4 * i)) % (uint)people.Count)];
                people.Remove(victim);
                victim.HP = 0;
                Town.Npcs.Remove(victim);
                Monsters.Remove(victim);
            }
            RecordDeed(Deed.Failed, Town.Name, 2);
            string en = burnt != null ? $"On day {day} {band}s raided {Town.Name}: {burnt} burned, and {dead} died." : $"On day {day} {band}s raided {Town.Name}, and {dead} died.";
            string pt = burnt != null ? $"No dia {day}, {Loc.PtOf(band)}s atacaram {Town.Name}: {Loc.PtOf(burnt)} queimou, e {dead} morreram." : $"No dia {day}, {Loc.PtOf(band)}s atacaram {Town.Name}, e {dead} morreram.";
            RaidChronicle.Add(TownText.L(en, pt));
            Say(en, MessageKind.Bad);
        }

        /// <summary>A building burns: its shop is emptied and closed, its keeper gone, its floor black and broken.</summary>
        void Burn(Building b)
        {
            b.Burned = true;
            b.Services = Service.None;
            if (b.Shop != null) { b.Shop.Stock.Clear(); b.Shop.Gold = 0; }
            if (b.Keeper != null) { b.Keeper.HP = 0; Town.Npcs.Remove(b.Keeper); }
            if (!Town.Floors.TryGetValue(0, out var map)) return;
            for (int y = b.Y + 1; y < b.Y + b.H - 1; y++)
                for (int x = b.X + 1; x < b.X + b.W - 1; x++)
                {
                    var t = map.Get(x, y);
                    if (t == TileKind.Shelf || t == TileKind.Barrel || t == TileKind.Table || t == TileKind.Bed) map.Set(x, y, (x + y) % 3 == 0 ? TileKind.Rubble : TileKind.Floor);
                    if (map.Get(x, y) == TileKind.Floor || map.Get(x, y) == TileKind.FloorAlt)
                        map.Stains[y * map.W + x] = new Stain { Kind = StainKind.Soot, Turn = int.MaxValue / 2, Source = "fire" };
                }
            map.Version++;
        }
    }
}
