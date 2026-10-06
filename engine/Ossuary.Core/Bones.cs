using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>One piece of gear a dead hero left behind.</summary>
    public sealed class BonesItem
    {
        public string Def { get; set; } = "";
        public int Enchant { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public int Rarity { get; set; }
        /// <summary>Absent in older bones files, which read as the default material.</summary>
        public string Material { get; set; }
    }

    /// <summary>
    /// A dead hero, as data: where they fell and what they carried. A later run may meet them again as
    /// a shade on that level (NetHack "bones"). The Core only builds and raises these; the host keeps the file.
    /// </summary>
    public sealed class Bones
    {
        public string Name { get; set; } = "";
        public string Race { get; set; } = "";
        public string Role { get; set; } = "";
        public int Level { get; set; }
        public string Cause { get; set; } = "";
        public string Branch { get; set; } = "";
        public int Depth { get; set; }
        public List<BonesItem> Gear { get; set; } = new List<BonesItem>();

        public string Key => Branch + "@" + Depth;
    }

    public sealed partial class Game
    {
        /// <summary>
        /// Dead heroes available to this run, fixed when it starts (a save carries its own copy, so a replay
        /// meets the same shades). Empty by default, which keeps the simulation exactly as before.
        /// </summary>
        public List<Bones> Graveyard = new List<Bones>();
        /// <summary>Keys of shades the player has destroyed this run; the host lays those bones to rest.</summary>
        public readonly List<string> LaidToRest = new List<string>();

        public const int MinBonesDepth = 2;

        /// <summary>The bones a dying player leaves, or null (not in a dungeon, too shallow, abandoned).</summary>
        public Bones LeaveBones()
        {
            if (Abandoned || Mode == GameMode.Won || !Player.InsideDungeon || Depth < MinBonesDepth) return null;
            var r = Races.Find(Player.RaceId); var role = Roles.Find(Player.RoleId);
            var b = new Bones
            {
                Name = Player.CharName, Race = r?.Name ?? Player.RaceId, Role = role?.Name ?? Player.RoleId,
                Level = Math.Max(1, Player.XpLevel > 0 ? Player.XpLevel : Player.Level),
                Cause = DeathCause ?? "unknown causes", Branch = Branch, Depth = Depth,
            };
            void Add(Item it) { if (it != null && b.Gear.Count < 8) b.Gear.Add(new BonesItem { Def = it.Def.Name, Enchant = it.Enchant, Prefix = it.Prefix, Suffix = it.Suffix, Rarity = (int)it.Rarity, Material = it.Material }); }
            Add(Player.Wielded);
            foreach (var piece in Player.WornPieces()) Add(piece);
            return b;
        }

        /// <summary>Called when a level is built for the first time: a shade may walk it.</summary>
        void RaiseBones(List<LevelBuilder.SpawnPoint> spawns, int startX, int startY)
        {
            if (Graveyard == null || Graveyard.Count == 0) return;
            Bones b = null;
            foreach (var candidate in Graveyard) if (candidate.Branch == Branch && candidate.Depth == Depth) { b = candidate; break; }
            if (b == null) return;
            // A private stream: the world's own Rng is never touched, so a level is the same with or without a shade.
            var rng = new Rng(Rng.Seed ^ (ulong)(Branch.GetHashCode() * 31 + Depth * 7919 + 12345) ^ 0x5348414445UL);
            if (rng.Chance(40)) return;
            var monster = RaiseShade(b, rng, startX, startY);
            if (monster == null) return;
            spawns?.Add(new LevelBuilder.SpawnPoint { X = monster.X, Y = monster.Y, Monster = monster });
            Monsters.Add(monster);
            Say($"A cold draught. Someone died here: {b.Name} the {b.Role}.", MessageKind.Narrative);
        }

        Monster RaiseShade(Bones b, Rng rng, int startX, int startY)
        {
            int cell = -1;
            for (int tries = 0; tries < 200; tries++)
            {
                int x = rng.Range(1, Map.W - 1), y = rng.Range(1, Map.H - 1);
                if (!Map.Walkable(x, y) || Map.Get(x, y) != TileKind.Floor && Map.Get(x, y) != TileKind.FloorAlt) continue;
                if (Pathfinder.Chebyshev(x, y, startX, startY) < 12 || MonsterAt(x, y) != null) continue;
                cell = x + y * Map.W; break;
            }
            if (cell < 0) return null;
            var def = Bestiary.Find("wandering wraith");
            int lv = Math.Max(1, b.Level);
            def.Name = "shade of " + b.Name;
            def.Glyph = '@'; def.Color = 0xC8C8F0;
            def.Level = lv; def.HP = 12 + lv * 7; def.AC = Math.Max(2, 9 - lv / 2);
            def.DepthMin = def.DepthMax = b.Depth;
            def.Ai = AiKind.Guard; def.Undead = true; def.Mindless = false; def.Regenerates = false;
            def.Attacks = new[] { AttackKind.Hit, AttackKind.Touch }; def.DmgDice = new[] { 1, 1 };
            def.DmgSides = new[] { 4 + lv / 2, 3 + lv / 3 }; def.ToHit = new[] { 3 + lv / 2, 3 + lv / 3 };
            def.Difficulty = lv + 4; def.CorpseValue = 0;
            var m = new Monster(def, rng) { X = cell % Map.W, Y = cell / Map.W, Depth = Map.Depth, Unique = true, BonesKey = b.Key };
            m.HomeX = m.X; m.HomeY = m.Y;
            m.HP = m.MaxHP = def.HP;
            foreach (var g in b.Gear)
            {
                if (!Catalogue.TryFindGear(g.Def, out var gd)) continue;
                m.Inventory.Add(new Item(gd, rng, NextUid()) { Enchant = g.Enchant, Prefix = g.Prefix, Suffix = g.Suffix, Rarity = (Rarity)g.Rarity, Material = Materials.Find(g.Material) != null ? g.Material : null });
            }
            return m;
        }
    }
}
