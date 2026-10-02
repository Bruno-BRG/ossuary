using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.Entities
{
    /// <summary>The static bestiary. Depth ranges are what drive the spawn tables.</summary>
    public static class Bestiary
    {
        static MonsterDef[] _defs;
        public static IReadOnlyList<MonsterDef> All => _defs ?? (_defs = Build());

        static MonsterDef[] Build()
        {
            var d = new List<MonsterDef>();

            void M(string name, char g, int color, int level, int hp, int ac, int speed, int weight,
                  int depthMin, int depthMax, AiKind ai, Alignment align, AttackKind[] atk,
                  int[] dice, int[] sides, int[] tohit, int vision = 8, int diff = 2, int nut = 3,
                  bool undead = false, bool regen = false, bool mindless = false, bool skel = false,
                  int corpse = 0, ItemDef[] carries = null, int[] carryW = null, bool flys = false,
                  string branch = null, string trait = null)
            {
                d.Add(new MonsterDef
                {
                    Name = name, Glyph = g, Color = color, Level = level, HP = hp, AC = ac, Speed = speed,
                    Weight = weight, DepthMin = depthMin, DepthMax = depthMax, Ai = ai, Align = align,
                    Attacks = atk, DmgDice = dice, DmgSides = sides, ToHit = tohit, Vision = vision,
                    Difficulty = diff, Nutrient = nut, Undead = undead, Regenerates = regen,
                    Mindless = mindless, Skeleton = skel, CorpseValue = corpse, Carries = carries,
                    CarryWeights = carryW, Flys = flys, Branch = branch, Trait = trait
                });
            }

            // --- depths 1-4: the soft opening ---------------------------------
            M("grid bug", 'x', 0xB07C4F, 0, 1, 9, 12, 5, 1, 4, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 1 }, new[] { 0 }, 3, 1, 2, mindless: true, corpse: 0);
            M("newt", 'x', 0x5C8A3C, 0, 3, 8, 12, 10, 1, 4, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 1 }, new[] { 0 }, 3, 1, 3, mindless: true);
            M("jackal", 'd', 0xB08A4A, 1, 5, 7, 16, 300, 1, 5, AiKind.Hunt, Alignment.ChaoticNeutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 1 }, new[] { 4 }, 8, 3, 4);
            M("giant rat", 'r', 0x8A6A4A, 1, 5, 7, 14, 30, 1, 6, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.Bite, AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 1, 1 }, new[] { 3, 3 }, 6, 3, 4);
            M("kobold", 'k', 0x8A6A3C, 1, 6, 8, 12, 400, 1, 7, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.ClawOrBite, AttackKind.PierceOrHit }, new[] { 1, 1 }, new[] { 4, 3 }, new[] { 2, 3 }, 8, 3, 5,
              carries: new[] { ItemDefs.Club }, carryW: new[] { 40 });
            M("dwarf", 'h', 0xC09050, 4, 18, 10, 10, 900, 2, 14, AiKind.Hunt, Alignment.LawfulGood,
              new[] { AttackKind.Hit }, new[] { 1, 1 }, new[] { 6, 3 }, new[] { 3, 2 }, 8, 5, 7,
              carries: new[] { ItemDefs.Axe, ItemDefs.Dagger, ItemDefs.PickAxe }, carryW: new[] { 40, 40, 20 });
            M("gnome", 'G', 0x70B0C0, 3, 12, 10, 10, 650, 3, 20, AiKind.Hunt, Alignment.LawfulNeutral,
              new[] { AttackKind.Hit }, new[] { 1 }, new[] { 6 }, new[] { 2 }, 8, 4, 6,
              carries: new[] { ItemDefs.Axe, ItemDefs.Sling }, carryW: new[] { 60, 20 });
            M("hobbit", 'h', 0xA08060, 1, 10, 9, 12, 500, 1, 10, AiKind.Walk, Alignment.LawfulNeutral,
              new[] { AttackKind.Hit }, new[] { 1 }, new[] { 4 }, new[] { 2 }, 8, 3, 5);
            M("gnome lord", 'G', 0x40A0E0, 7, 32, 8, 10, 750, 5, 20, AiKind.Hunt, Alignment.LawfulNeutral,
              new[] { AttackKind.Hit }, new[] { 1, 1 }, new[] { 6, 3 }, new[] { 4, 3 }, 9, 6, 7);
            M("orc", 'o', 0x60A050, 4, 16, 8, 12, 1200, 3, 20, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.PierceOrHit }, new[] { 1, 1 }, new[] { 4, 3 }, new[] { 2, 3 }, 8, 5, 7,
              carries: new[] { ItemDefs.Axe, ItemDefs.Mace, ItemDefs.Dagger }, carryW: new[] { 30, 40, 30 });
            M("orc shaman", 'o', 0x60D050, 7, 30, 8, 12, 1100, 5, 20, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.ClawOrBite }, new[] { 1 }, new[] { 2 }, new[] { 2 }, 8, 7, 7,
              carries: new[] { ItemDefs.Club }, carryW: new[] { 100 });
            M("orc chieftain", 'o', 0xFF8030, 12, 60, 8, 10, 1350, 10, 30, AiKind.Predator, Alignment.ChaoticEvil,
              new[] { AttackKind.Hit, AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 6, 4 }, new[] { 5, 4 }, 9, 9, 9,
              carries: new[] { ItemDefs.BattleAxe, ItemDefs.ArmorPlate }, carryW: new[] { 50, 25 });
            M("brown mold", 'F', 0x40C040, 0, 4, 8, 4, 50, 1, 8, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Explode }, new[] { 1 }, new[] { 4 }, new[] { 0 }, 0, 2, 1, mindless: true, flys: true);
            M("yellow mold", 'F', 0xE0D040, 0, 4, 8, 4, 50, 1, 10, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Explode }, new[] { 1 }, new[] { 4 }, new[] { 0 }, 0, 2, 1, mindless: true, flys: true);
            M("gray mold", 'F', 0x9090A0, 0, 8, 8, 4, 50, 3, 14, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Explode }, new[] { 1 }, new[] { 6 }, new[] { 0 }, 0, 4, 1, mindless: true, flys: true);
            M("gas spore", 'F', 0xC0E080, 0, 1, 8, 4, 10, 1, 8, AiKind.Ambush, Alignment.Neutral,
              new[] { AttackKind.Explode }, new[] { 1 }, new[] { 2 }, new[] { 0 }, 0, 1, 1, mindless: true, flys: true);
            M("little lizard", 'x', 0x60C0A0, 1, 5, 8, 14, 60, 1, 8, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 1 }, new[] { 0 }, 6, 2, 3);
            M("floating eye", 'e', 0xC0A0F0, 2, 10, 9, 10, 10, 2, 14, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.Bite, AttackKind.Kick }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 0, 0 }, 8, 3, 4, flys: true);
            M("cave spider", 'S', 0x705030, 3, 12, 6, 12, 50, 2, 16, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 2 }, new[] { 0 }, 6, 4, 4, flys: true);
            M("centipede", 'S', 0xC0A040, 3, 14, 6, 12, 50, 3, 18, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 3 }, new[] { 0 }, 6, 4, 4);
            M("human zombie", 'Z', 0xA0C090, 4, 16, 8, 6, 1450, 4, 25, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.ClawOrBite, AttackKind.Grab }, new[] { 1, 1 }, new[] { 3, 3 }, new[] { 2, 2 }, 8, 6, 8,
              undead: true, mindless: true, corpse: 10);
            M("skeleton", 'Z', 0xE0E0C0, 5, 18, 6, 10, 1000, 4, 25, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 3, 2 }, 8, 6, 8,
              undead: true, mindless: true, skel: true, corpse: 8,
              carries: new[] { ItemDefs.Dagger, ItemDefs.Axe, ItemDefs.LongSword }, carryW: new[] { 30, 30, 40 });
            M("gnome mummy", 'M', 0xC0C090, 7, 26, 8, 6, 1200, 6, 30, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 2, 2 }, 8, 7, 8,
              undead: true, mindless: true, corpse: 20);

            // --- mid depths --------------------------------------------------
            M("jackal warden", 'd', 0xC0A060, 8, 32, 8, 16, 400, 8, 30, AiKind.Predator, Alignment.ChaoticNeutral,
              new[] { AttackKind.Bite, AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 2, 2 }, new[] { 4, 4 }, 9, 8, 6);
            M("stalker", 'T', 0x506050, 10, 40, 7, 16, 800, 9, 32, AiKind.Ambush, Alignment.Neutral,
              new[] { AttackKind.Grab, AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 4, 4 }, new[] { 4, 4 }, 9, 9, 8);
            M("troll", 'T', 0x60B080, 12, 90, 7, 8, 3000, 12, 40, AiKind.Hunt, Alignment.ChaoticNeutral,
              new[] { AttackKind.ClawOrBite, AttackKind.Grab }, new[] { 1, 1, 1 }, new[] { 4, 4, 3 }, new[] { 3, 3, 2 }, 9, 11, 12,
              regen: true, corpse: 60, carries: new[] { ItemDefs.ArmorRing, ItemDefs.BattleAxe }, carryW: new[] { 30, 40 });
            M("ogre", 'O', 0xC08060, 9, 50, 6, 10, 1600, 8, 30, AiKind.Hunt, Alignment.ChaoticNeutral,
              new[] { AttackKind.Hit, AttackKind.Kick }, new[] { 1, 1 }, new[] { 6, 3 }, new[] { 3, 2 }, 8, 9, 10,
              corpse: 30);
            M("ogre lord", 'O', 0xFF9060, 15, 110, 6, 10, 1800, 14, 40, AiKind.Predator, Alignment.ChaoticNeutral,
              new[] { AttackKind.Hit, AttackKind.Kick }, new[] { 1, 1 }, new[] { 6, 4 }, new[] { 4, 3 }, 9, 12, 12,
              corpse: 60, carries: new[] { ItemDefs.ArmorPlate }, carryW: new[] { 30 });
            M("hill giant", 'H', 0xC0A080, 18, 200, 6, 10, 4000, 16, 45, AiKind.Hunt, Alignment.ChaoticNeutral,
              new[] { AttackKind.Hit, AttackKind.Kick, AttackKind.Butt }, new[] { 1, 1, 1 }, new[] { 6, 4, 3 }, new[] { 4, 3, 3 }, 9, 14, 20,
              corpse: 150);
            M("werenothing", 'W', 0x6060A0, 16, 80, 8, 12, 1500, 14, 40, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.ClawOrBite, AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 4, 4 }, new[] { 4, 4 }, 9, 12, 12,
              regen: true);
            M("wood nymph", '@', 0x70E090, 5, 20, 7, 12, 500, 4, 22, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.ClawOrBite, AttackKind.Strangle }, new[] { 1, 1 }, new[] { 2, 1 }, new[] { 2, 2 }, 9, 6, 6);
            M("dwarf lord", 'h', 0xFFB060, 14, 70, 10, 8, 1000, 12, 40, AiKind.Hunt, Alignment.LawfulGood,
              new[] { AttackKind.Hit }, new[] { 1, 1 }, new[] { 6, 4 }, new[] { 5, 3 }, 9, 11, 8,
              carries: new[] { ItemDefs.ArmorChain, ItemDefs.WarHammer }, carryW: new[] { 30, 40 });
            M("ice troll", 'T', 0x90D0F0, 14, 80, 8, 8, 3000, 12, 40, AiKind.Hunt, Alignment.ChaoticNeutral,
              new[] { AttackKind.ClawOrBite, AttackKind.Grab }, new[] { 1, 1 }, new[] { 4, 3 }, new[] { 3, 3 }, 9, 12, 12, regen: true);

            // --- the deep ----------------------------------------------------
            M("wandering wraith", 'W', 0x7080C0, 15, 60, 8, 12, 700, 14, 45, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.Explode, AttackKind.ClawOrBite }, new[] { 1, 1 }, new[] { 4, 3 }, new[] { 4, 3 }, 9, 13, 10,
              undead: true, regen: true);
            M("lich", 'E', 0xE0E0F0, 22, 90, 8, 8, 1100, 20, 50, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.Touch }, new[] { 1, 1 }, new[] { 6, 4 }, new[] { 5, 4 }, 10, 16, 10, undead: true);
            M("dread knight", 'K', 0xC0C0E0, 18, 100, 6, 12, 2000, 16, 45, AiKind.Predator, Alignment.LawfulEvil,
              new[] { AttackKind.Hit, AttackKind.Kick }, new[] { 1, 1 }, new[] { 6, 4 }, new[] { 5, 3 }, 10, 15, 14, undead: true);
            M("fire giant", 'H', 0xFF6030, 24, 240, 6, 12, 4500, 22, 55, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.Hit, AttackKind.Kick, AttackKind.Butt }, new[] { 1, 1, 1 }, new[] { 6, 4, 3 }, new[] { 5, 4, 3 }, 10, 18, 22,
              corpse: 200, carries: new[] { ItemDefs.ArmorPlate }, carryW: new[] { 30 });
            M("tiamat", 'D', 0xFF3030, 30, 300, 4, 14, 5000, 28, 60, AiKind.Predator, Alignment.NeutralEvil,
              new[] { AttackKind.Bite, AttackKind.ClawOrBite, AttackKind.Bite }, new[] { 1, 1, 1 }, new[] { 6, 6, 4 }, new[] { 6, 5, 5 }, 12, 24, 26,
              regen: true, flys: true);
            M("guardian of the deep", '@', 0x40E0D0, 20, 150, 6, 12, 2000, 18, 50, AiKind.Guard, Alignment.Neutral,
              new[] { AttackKind.Hit, AttackKind.Kick }, new[] { 1, 1 }, new[] { 6, 4 }, new[] { 6, 4 }, 10, 16, 14);

            // --- native to one branch, each with a habit of its own ------------------------------
            const string Dun = "The Dungeons", Min = "The Mines of Dwarfdeep", War = "The Warrens", Vau = "The Sunken Vaults", Spi = "The Ashen Spire";
            M("gaol hound", 'd', 0x9A9A80, 4, 16, 7, 14, 400, 3, 10, AiKind.Hunt, Alignment.NeutralEvil,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 6 }, new[] { 4 }, 9, 4, 5, branch: Dun, trait: "pack");
            M("cave bat", 'B', 0x8C6A8C, 2, 6, 8, 18, 40, 1, 8, AiKind.Walk, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 4 }, new[] { 3 }, 7, 2, 3, flys: true, branch: Min, trait: "erratic");
            M("ore golem", 'g', 0x9C8F7A, 6, 44, 4, 8, 2500, 4, 8, AiKind.Guard, Alignment.Neutral,
              new[] { AttackKind.Hit }, new[] { 1 }, new[] { 10 }, new[] { 5 }, 6, 6, 6, mindless: true, branch: Min, trait: "slam");
            M("plague rat", 'r', 0x7A9A4A, 3, 9, 7, 14, 40, 1, 9, AiKind.Hunt, Alignment.NeutralEvil,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 4 }, new[] { 4 }, 7, 3, 4, branch: War, trait: "plague");
            M("rat swarm", 'r', 0xA08A6A, 2, 6, 8, 14, 30, 2, 9, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.Bite }, new[] { 1 }, new[] { 3 }, new[] { 3 }, 6, 3, 3, branch: War, trait: "swarm");
            M("drowned dead", 'Z', 0x5A8AA0, 6, 36, 9, 8, 1400, 1, 12, AiKind.Hunt, Alignment.NeutralEvil,
              new[] { AttackKind.Hit }, new[] { 1 }, new[] { 8 }, new[] { 4 }, 7, 6, 6, undead: true, mindless: true, branch: Vau, trait: "drowned");
            M("tide wraith", 'W', 0x6AB0C0, 9, 40, 6, 12, 600, 4, 12, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.Touch }, new[] { 1 }, new[] { 6 }, new[] { 5 }, 9, 8, 8, undead: true, branch: Vau, trait: "drowned");
            M("ember wisp", '*', 0xFF8A30, 5, 8, 6, 14, 10, 1, 15, AiKind.Hunt, Alignment.Neutral,
              new[] { AttackKind.Touch }, new[] { 1 }, new[] { 4 }, new[] { 4 }, 8, 5, 3, mindless: true, flys: true, branch: Spi, trait: "wisp");
            M("ash wraith", 'W', 0xB0A090, 11, 50, 5, 12, 700, 6, 15, AiKind.Hunt, Alignment.ChaoticEvil,
              new[] { AttackKind.Touch }, new[] { 1 }, new[] { 8 }, new[] { 6 }, 9, 10, 9, undead: true, branch: Spi, trait: "ashen");

            return d.ToArray();
        }

        /// <summary>Monsters that can appear at a given depth, weighted by how far from their comfort zone they are.</summary>
        public static List<MonsterDef> SpawnTable(int depth, Rng rng, string branch = null)
        {
            var table = new List<MonsterDef>();
            var weights = new List<int>();
            for (int i = 0; i < All.Count; i++)
            {
                var def = All[i];
                if (depth < def.DepthMin || depth > def.DepthMax) continue;
                if (def.Branch != null && def.Branch != branch) continue;
                int w = 100;
                if (depth == def.DepthMin || depth == def.DepthMax) w = 45;
                table.Add(def);
                weights.Add(w);
            }
            return table;
        }

        public static MonsterDef? RandomForDepth(int depth, Rng rng, out int index)
        {
            index = -1;
            var table = SpawnTable(depth, rng);
            var weights = new int[table.Count];
            for (int i = 0; i < table.Count; i++)
            {
                var d = table[i];
                weights[i] = (depth == d.DepthMin || depth == d.DepthMax) ? 45 : 100;
            }
            int pick = rng.WeightedIndex(weights);
            if (pick < 0) return null;
            index = pick;
            return table[pick];
        }

        /// <summary>Exact-name lookup. Check the boolean before relying on the value.</summary>
        public static bool TryGet(string name, out MonsterDef def)
        {
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i].Name != name) continue;
                def = All[i];
                return true;
            }
            def = default;
            return false;
        }

        /// <summary>
        /// Exact-name lookup with a fallback to the first entry. Only for stat shells
        /// (town guards, shopkeepers) where the body matters, not the identity.
        /// </summary>
        public static MonsterDef Find(string name)
        {
            return TryGet(name, out var def) ? def : All[0];
        }
    }

    /// <summary>Named item defs the bestiary references, resolved once to avoid duplication.</summary>
    public static class ItemDefs
    {
        static ItemDef Find(string name)
        {
            foreach (var w in Catalogue.Weapons) if (w.Name == name) return w;
            foreach (var a in Catalogue.Armor) if (a.Name == name) return a;
            foreach (var s in Catalogue.Shields) if (s.Name == name) return s;
            foreach (var r in Catalogue.Rings) if (r.Name == name) return r;
            foreach (var am in Catalogue.Amulets) if (am.Name == name) return am;
            foreach (var f in Catalogue.Food) if (f.Name == name) return f;
            foreach (var t in Catalogue.Tools) if (t.Name == name) return t;
            return new ItemDef { Name = name, Glyph = ')', Kind = ItemKind.Weapon, Cost = 10, Weight = 10, Tier = 1 };
        }

        static ItemDef? _club, _axe, _dagger, _pickAxe, _sling, _mace, _warHammer, _battleAxe, _longSword;
        static ItemDef? _armorRing, _armorChain, _armorPlate;
        static ItemDef? _shortSword, _scimitar, _warAxe, _trident, _quarterstaff, _flail, _whip, _spear, _crossbow, _shortBow, _elvenBow;

        static ItemDef Cache(ref ItemDef? slot, string name)
        {
            if (!slot.HasValue) slot = Find(name);
            return slot.Value;
        }

        public static ItemDef Club => Cache(ref _club, "mace");
        public static ItemDef Mace => Cache(ref _mace, "mace");
        public static ItemDef Axe => Cache(ref _axe, "axe");
        public static ItemDef Dagger => Cache(ref _dagger, "dagger");
        public static ItemDef PickAxe => Cache(ref _pickAxe, "pick-axe");
        public static ItemDef Sling => Cache(ref _sling, "sling");
        public static ItemDef WarHammer => Cache(ref _warHammer, "war hammer");
        public static ItemDef BattleAxe => Cache(ref _battleAxe, "battle axe");
        public static ItemDef LongSword => Cache(ref _longSword, "long sword");
        public static ItemDef ShortSword => Cache(ref _shortSword, "short sword");
        public static ItemDef Scimitar => Cache(ref _scimitar, "scimitar");
        public static ItemDef WarAxe => Cache(ref _warAxe, "war axe");
        public static ItemDef Trident => Cache(ref _trident, "trident");
        public static ItemDef Quarterstaff => Cache(ref _quarterstaff, "quarterstaff");
        public static ItemDef Flail => Cache(ref _flail, "flail");
        public static ItemDef Whip => Cache(ref _whip, "whip");
        public static ItemDef Spear => Cache(ref _spear, "spear");
        public static ItemDef Crossbow => Cache(ref _crossbow, "crossbow");
        public static ItemDef ShortBow => Cache(ref _shortBow, "short bow");
        public static ItemDef ElvenBow => Cache(ref _elvenBow, "elven bow");
        public static ItemDef ArmorRing => Cache(ref _armorRing, "ring mail");
        public static ItemDef ArmorChain => Cache(ref _armorChain, "chain mail");
        public static ItemDef ArmorPlate => Cache(ref _armorPlate, "plate mail");
    }
}
