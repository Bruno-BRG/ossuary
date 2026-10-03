using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core.Items
{
    public enum Rarity { Common, Magic, Rare, Artifact }

    /// <summary>
    /// Everything a magical item adds, as plain numbers. Enchantment, prefix, suffix and artifact
    /// all reduce to one of these, and the player's gear is the sum of its worn pieces.
    /// </summary>
    public struct ItemMods
    {
        public int ToHit, Dmg, Ac, Evasion;
        public int Str, Dex, Con, Int, Wis;
        public int Mp, Hp, Vigor;
        /// <summary>Spell damage added to every spell, percent off failure (positive = fewer fizzles), and squares of Vision taken off what monsters notice.</summary>
        public int SpellPower, SpellFocus, Stealth;
        /// <summary>Resistance to holy light (percent); the rest of the elements are above.</summary>
        public int ResHoly;
        public int ResFire, ResCold, ResLightning, ResPoison, ResNecrotic;
        /// <summary>One extra die of this type and size added to every melee hit (0 = none).</summary>
        public DamageType ExtraType;
        public int ExtraSides;
        /// <summary>Percent of damage dealt returned as HP.</summary>
        public int LifeSteal;

        public void Add(ItemMods o)
        {
            ToHit += o.ToHit; Dmg += o.Dmg; Ac += o.Ac; Evasion += o.Evasion;
            Str += o.Str; Dex += o.Dex; Con += o.Con; Int += o.Int; Wis += o.Wis;
            Mp += o.Mp; Hp += o.Hp; Vigor += o.Vigor;
            SpellPower += o.SpellPower; SpellFocus += o.SpellFocus; Stealth += o.Stealth; ResHoly += o.ResHoly;
            ResFire += o.ResFire; ResCold += o.ResCold; ResLightning += o.ResLightning; ResPoison += o.ResPoison; ResNecrotic += o.ResNecrotic;
            if (o.ExtraSides > ExtraSides) { ExtraSides = o.ExtraSides; ExtraType = o.ExtraType; }
            LifeSteal += o.LifeSteal;
        }

        public int Resist(DamageType t)
        {
            switch (t)
            {
                case DamageType.Fire: return ResFire;
                case DamageType.Cold: return ResCold;
                case DamageType.Lightning: return ResLightning;
                case DamageType.Poison: return ResPoison;
                case DamageType.Necrotic: return ResNecrotic;
                case DamageType.Holy: return ResHoly;
                default: return 0;
            }
        }

        /// <summary>Short text for tooltips: "+2 Dex, fire 30%, 1d4 fire".</summary>
        public List<string> Lines()
        {
            var l = new List<string>();
            void A(string n, int v) { if (v != 0) l.Add((v > 0 ? "+" : "") + v + " " + n); }
            A("to hit", ToHit); A("damage", Dmg); A("AC", Ac); A("evasion", Evasion);
            A("Str", Str); A("Dex", Dex); A("Con", Con); A("Int", Int); A("Wis", Wis);
            A("Mp", Mp); A("HP", Hp); A("Vigor", Vigor);
            A("spell power", SpellPower); A("spell focus", SpellFocus); A("stealth", Stealth);
            if (ResFire != 0) l.Add($"fire {ResFire}%");
            if (ResCold != 0) l.Add($"cold {ResCold}%");
            if (ResLightning != 0) l.Add($"lightning {ResLightning}%");
            if (ResPoison != 0) l.Add($"poison {ResPoison}%");
            if (ResNecrotic != 0) l.Add($"necrotic {ResNecrotic}%");
            if (ResHoly != 0) l.Add($"holy {ResHoly}%");
            if (ExtraSides > 0) l.Add($"+1d{ExtraSides} {ExtraType.ToString().ToLowerInvariant()}");
            if (LifeSteal != 0) l.Add($"{LifeSteal}% life steal");
            return l;
        }
    }

    public sealed class AffixDef
    {
        public string Id, Name;
        public bool Prefix, ForWeapon, ForArmor;
        public int Weight = 10;
        public ItemMods Mods;
    }

    public static class Affixes
    {
        static AffixDef W(string id, string name, bool prefix, int weight, ItemMods m) => new AffixDef { Id = id, Name = name, Prefix = prefix, ForWeapon = true, Weight = weight, Mods = m };
        static AffixDef A(string id, string name, bool prefix, int weight, ItemMods m) => new AffixDef { Id = id, Name = name, Prefix = prefix, ForArmor = true, Weight = weight, Mods = m };
        static AffixDef B(string id, string name, bool prefix, int weight, ItemMods m) => new AffixDef { Id = id, Name = name, Prefix = prefix, ForWeapon = true, ForArmor = true, Weight = weight, Mods = m };

        public static readonly AffixDef[] All = {
            // Weapon prefixes
            W("keen", "keen", true, 12, new ItemMods { ToHit = 2 }),
            W("brutal", "brutal", true, 12, new ItemMods { Dmg = 2 }),
            W("flaming", "flaming", true, 5, new ItemMods { ExtraType = DamageType.Fire, ExtraSides = 4 }),
            W("frozen", "frozen", true, 5, new ItemMods { ExtraType = DamageType.Cold, ExtraSides = 4 }),
            W("venomous", "venomous", true, 5, new ItemMods { ExtraType = DamageType.Poison, ExtraSides = 3 }),
            W("vampiric", "vampiric", true, 3, new ItemMods { LifeSteal = 25 }),
            // Armour prefixes
            A("sturdy", "sturdy", true, 12, new ItemMods { Ac = 1 }),
            A("fire-warded", "fire-warded", true, 6, new ItemMods { ResFire = 30 }),
            A("frost-warded", "frost-warded", true, 6, new ItemMods { ResCold = 30 }),
            A("venom-proof", "venom-proof", true, 6, new ItemMods { ResPoison = 40 }),
            A("storm-warded", "storm-warded", true, 4, new ItemMods { ResLightning = 30 }),
            // Suffixes
            B("of-the-fox", "of the fox", false, 10, new ItemMods { Dex = 2 }),
            B("of-the-bear", "of the bear", false, 10, new ItemMods { Str = 2 }),
            B("of-the-owl", "of the owl", false, 8, new ItemMods { Int = 2 }),
            B("of-the-sage", "of the sage", false, 8, new ItemMods { Wis = 2 }),
            B("of-the-mage", "of the mage", false, 7, new ItemMods { Mp = 6 }),
            A("of-life", "of life", false, 8, new ItemMods { Hp = 10 }),
            A("of-vigor", "of vigor", false, 8, new ItemMods { Vigor = 8 }),
            A("of-shadows", "of shadows", false, 7, new ItemMods { Evasion = 2 }),
            W("of-ruin", "of ruin", false, 6, new ItemMods { Dmg = 3, ToHit = -1 }),

            // More weapon prefixes: other elements, and weapons for casters.
            W("shocking", "shocking", true, 5, new ItemMods { ExtraType = DamageType.Lightning, ExtraSides = 4 }),
            W("radiant", "radiant", true, 4, new ItemMods { ExtraType = DamageType.Holy, ExtraSides = 4 }),
            W("rotting", "rotting", true, 4, new ItemMods { ExtraType = DamageType.Necrotic, ExtraSides = 4 }),
            W("thundering", "thundering", true, 3, new ItemMods { Dmg = 1, ExtraType = DamageType.Lightning, ExtraSides = 3 }),
            W("searing", "searing", true, 2, new ItemMods { ExtraType = DamageType.Fire, ExtraSides = 6 }),
            W("holy", "holy", true, 2, new ItemMods { ExtraType = DamageType.Holy, ExtraSides = 6 }),
            W("draining", "draining", true, 3, new ItemMods { LifeSteal = 12, ExtraType = DamageType.Necrotic, ExtraSides = 3 }),
            W("masterwork", "masterwork", true, 4, new ItemMods { ToHit = 2, Dmg = 1 }),
            W("razor-edged", "razor-edged", true, 6, new ItemMods { ToHit = 1, Dmg = 1 }),
            W("arcane", "arcane", true, 5, new ItemMods { SpellPower = 2, Mp = 3 }),
            // More armour prefixes.
            A("mage-woven", "mage-woven", true, 6, new ItemMods { Mp = 5, SpellFocus = 5 }),
            A("shadowed", "shadowed", true, 6, new ItemMods { Stealth = 1, Evasion = 1 }),
            A("grave-warded", "grave-warded", true, 6, new ItemMods { ResNecrotic = 30 }),
            A("mithril-lined", "mithril-lined", true, 3, new ItemMods { Ac = 2 }),
            A("rune-etched", "rune-etched", true, 5, new ItemMods { SpellPower = 1, SpellFocus = 5 }),
            A("troll-hide", "troll-hide", true, 5, new ItemMods { Hp = 8 }),
            A("dragon-warded", "dragon-warded", true, 3, new ItemMods { ResFire = 20, ResCold = 20 }),
            A("fortified", "fortified", true, 5, new ItemMods { Ac = 2, Evasion = -1 }),
            // More suffixes.
            B("of-the-wolf", "of the wolf", false, 8, new ItemMods { Str = 1, Dex = 1 }),
            B("of-the-lion", "of the lion", false, 5, new ItemMods { Str = 2, Hp = 6 }),
            B("of-the-sphinx", "of the sphinx", false, 6, new ItemMods { Int = 1, Wis = 1, Mp = 4 }),
            B("of-stealth", "of stealth", false, 7, new ItemMods { Stealth = 2 }),
            B("of-brilliance", "of brilliance", false, 6, new ItemMods { SpellFocus = 12 }),
            B("of-the-archmage", "of the archmage", false, 2, new ItemMods { SpellPower = 2, Mp = 6 }),
            A("of-fire", "of fire", false, 6, new ItemMods { ResFire = 35 }),
            A("of-frost", "of frost", false, 6, new ItemMods { ResCold = 35 }),
            A("of-storms", "of storms", false, 5, new ItemMods { ResLightning = 35 }),
            A("of-the-grave", "of the grave", false, 5, new ItemMods { ResNecrotic = 40 }),
            A("of-vitality", "of vitality", false, 7, new ItemMods { Hp = 14 }),
            W("of-precision", "of precision", false, 7, new ItemMods { ToHit = 2 }),
            W("of-might", "of might", false, 7, new ItemMods { Dmg = 2 }),
            W("of-the-hunter", "of the hunter", false, 5, new ItemMods { ToHit = 1, Stealth = 1 }),
            W("of-slaying", "of slaying", false, 2, new ItemMods { ToHit = 2, Dmg = 3 }),
        };

        public static AffixDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static AffixDef Pick(Rng rng, bool prefix, bool weapon)
        {
            int total = 0;
            foreach (var a in All) if (a.Prefix == prefix && (weapon ? a.ForWeapon : a.ForArmor)) total += a.Weight;
            int roll = rng.Range(0, total);
            foreach (var a in All)
            {
                if (a.Prefix != prefix || !(weapon ? a.ForWeapon : a.ForArmor)) continue;
                if (roll < a.Weight) return a;
                roll -= a.Weight;
            }
            return null;
        }
    }

    /// <summary>A named, fixed item. Lore is in the description; each lives in one branch.</summary>
    public sealed class ArtifactDef
    {
        public string Id, Name, Base, Branch, Lore;
        public int Depth, Enchant;
        public ItemMods Mods;
        /// <summary>The set this piece belongs to (see <see cref="ArtifactSets"/>), or null.</summary>
        public string Set;
        /// <summary>A relic: strong, but every 25 turns it is carried it presses a point of corruption into you.</summary>
        public bool Corrupts;
        /// <summary>Spells the item lets you cast while you wear or wield it (ids from <see cref="Magic.Spells"/>).</summary>
        public string[] Grants;
        /// <summary>Percent chance it is on its level (default: always). Several uniques may share a level; the first that rolls is placed.</summary>
        public int Chance = 100;
    }

    /// <summary>A named set of artifacts. Wearing two pieces grants the first bonus, three the second as well.</summary>
    public sealed class ArtifactSetDef
    {
        public string Id, Name;
        public ItemMods Two, Three;
    }

    public static class ArtifactSets
    {
        public static readonly ArtifactSetDef[] All =
        {
            new ArtifactSetDef { Id = "drowned-court", Name = "The Drowned Court", Two = new ItemMods { ResCold = 20, Wis = 1 }, Three = new ItemMods { Mp = 10, ResLightning = 30 } },
            new ArtifactSetDef { Id = "ashen-regalia", Name = "The Ashen Regalia", Two = new ItemMods { ResFire = 20, Ac = 1 }, Three = new ItemMods { Dmg = 2, ResFire = 20 } },
            new ArtifactSetDef { Id = "lich-queen", Name = "The Lich-Queen's Panoply", Two = new ItemMods { SpellPower = 2, ResNecrotic = 30 }, Three = new ItemMods { Mp = 20, Int = 2, SpellPower = 1 } },
            new ArtifactSetDef { Id = "stormcaller", Name = "The Stormcaller's Regalia", Two = new ItemMods { ResLightning = 40, SpellPower = 1 }, Three = new ItemMods { Mp = 15, SpellPower = 2 } },
            new ArtifactSetDef { Id = "verdant-court", Name = "The Verdant Court", Two = new ItemMods { Wis = 1, Stealth = 1, ResPoison = 30 }, Three = new ItemMods { Hp = 20, Mp = 10 } },
            new ArtifactSetDef { Id = "midnight-cabal", Name = "The Midnight Cabal", Two = new ItemMods { Stealth = 2, Evasion = 2 }, Three = new ItemMods { Dex = 2, ExtraType = DamageType.Necrotic, ExtraSides = 4 } },
        };

        public static ArtifactSetDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in All) if (s.Id == id) return s;
            return null;
        }

        /// <summary>How many pieces of a set the hero has on (the wielded weapon counts).</summary>
        public static int Worn(Entities.Player p, string setId)
        {
            int n = 0;
            if (p.Wielded != null && Artifacts.Find(p.Wielded.ArtifactId)?.Set == setId) n++;
            foreach (var piece in p.WornPieces()) if (Artifacts.Find(piece.ArtifactId)?.Set == setId) n++;
            for (int i = 0; i < p.Rings.Length; i++) if (p.Rings[i] != null && Artifacts.Find(p.Rings[i].ArtifactId)?.Set == setId) n++;
            if (p.Amulet != null && Artifacts.Find(p.Amulet.ArtifactId)?.Set == setId) n++;
            return n;
        }

        /// <summary>The bonus from every set the hero is partly wearing.</summary>
        public static ItemMods Bonus(Entities.Player p)
        {
            var m = new ItemMods();
            foreach (var s in All)
            {
                int n = Worn(p, s.Id);
                if (n >= 2) m.Add(s.Two);
                if (n >= 3) m.Add(s.Three);
            }
            return m;
        }
    }

    public static partial class Artifacts
    {
        static readonly ArtifactDef[] Original = {
            new ArtifactDef { Id = "veil-first-cell", Name = "Veil of the First Cell", Base = "cloak", Branch = "The Dungeons", Depth = 8, Enchant = 2,
                Lore = "Woven by the first prisoner, from the shirts of the ones who never came back.", Mods = new ItemMods { Evasion = 3, Dex = 1 } },
            new ArtifactDef { Id = "dwarfdeep-cleaver", Name = "Dwarfdeep Cleaver", Base = "battle axe", Branch = "The Mines of Dwarfdeep", Depth = 6, Enchant = 3,
                Lore = "Left behind when the dwarves abandoned the black water. It still remembers the stone.", Mods = new ItemMods { Str = 2, Dmg = 2 } },
            new ArtifactDef { Id = "rat-kings-tooth", Name = "Rat King's Tooth", Base = "dagger", Branch = "The Warrens", Depth = 7, Enchant = 2,
                Lore = "Gnawed, not forged. It drinks.", Mods = new ItemMods { Dex = 2, ExtraType = DamageType.Poison, ExtraSides = 4, LifeSteal = 20 } },
            new ArtifactDef { Id = "crown-drowned-king", Name = "Crown of the Drowned King", Base = "dwarvish helm", Branch = "The Sunken Vaults", Depth = 10, Enchant = 3,
                Lore = "Cold as the water that took him. It still wants a head to wear it.", Mods = new ItemMods { Wis = 3, Mp = 8, ResCold = 40 }, Set = "drowned-court" },
            new ArtifactDef { Id = "ashfall", Name = "Ashfall", Base = "long sword", Branch = "The Ashen Spire", Depth = 13, Enchant = 2,
                Lore = "Forged to burn the dead. It could not tell the difference.", Mods = new ItemMods { ResFire = 40, ExtraType = DamageType.Fire, ExtraSides = 6 }, Set = "ashen-regalia" },

            // The Drowned Court: three pieces of a sunken king's regalia.
            new ArtifactDef { Id = "tidecallers-gauntlets", Name = "Tidecaller's Gauntlets", Base = "gauntlets", Branch = "The Sunken Vaults", Depth = 5, Enchant = 2, Set = "drowned-court",
                Lore = "They close like a tide. Whoever wore them once held the water back.", Mods = new ItemMods { Str = 1, ResCold = 15 } },
            new ArtifactDef { Id = "brinewalkers", Name = "Brinewalkers", Base = "iron boots", Branch = "The Sunken Vaults", Depth = 8, Enchant = 2, Set = "drowned-court",
                Lore = "They never dry. They never slip, either.", Mods = new ItemMods { Evasion = 2, ResLightning = 20 } },

            // The Ashen Regalia: what the Spire's masters wore.
            new ArtifactDef { Id = "mantle-of-ash", Name = "Mantle of Ash", Base = "cloak", Branch = "The Ashen Spire", Depth = 8, Enchant = 2, Set = "ashen-regalia",
                Lore = "Grey flakes fall from it as you walk, and every flake is still warm.", Mods = new ItemMods { ResFire = 25, Evasion = 1 } },
            new ArtifactDef { Id = "cinder-plate", Name = "Cinder Plate", Base = "plate mail", Branch = "The Ashen Spire", Depth = 11, Enchant = 3, Set = "ashen-regalia",
                Lore = "Black plate, glowing at the seams. The last owner is still inside, in a way.", Mods = new ItemMods { Con = 2, ResFire = 20 } },

            // The Annex's prize: a cloak for those who paid every debt.
            new ArtifactDef { Id = "tithe-mantle", Name = "Tithe-Collector's Mantle", Base = "cloak of elvenkind", Branch = "The Annex", Depth = 3, Enchant = 3,
                Lore = "Woven from the receipts of everything the Annex ever took. It has no weight. It has a great deal of memory.", Mods = new ItemMods { Evasion = 3, ResNecrotic = 40, Hp = 12, Wis = 1 } },

            // Relics: strong, and they take something back.
            new ArtifactDef { Id = "hollow-ribs", Name = "Hollow Ribs", Base = "splint mail", Branch = "The Dungeons", Depth = 9, Enchant = 4, Corrupts = true,
                Lore = "A cage of ribs, hollowed out to be worn. It holds you like it held something else.", Mods = new ItemMods { Hp = 20, Con = 2 } },
            new ArtifactDef { Id = "gravediggers-spade", Name = "Gravedigger's Spade", Base = "war hammer", Branch = "The Mines of Dwarfdeep", Depth = 7, Enchant = 3, Corrupts = true,
                Lore = "It has dug more graves than anything alive. It wants to keep digging.", Mods = new ItemMods { Dmg = 3, ExtraType = DamageType.Necrotic, ExtraSides = 6 } },
            new ArtifactDef { Id = "gnawed-cowl", Name = "Gnawed Cowl", Base = "orcish helm", Branch = "The Warrens", Depth = 8, Enchant = 3, Corrupts = true,
                Lore = "The Rat King's thoughts still crawl in the lining. They are clever. They are hungry.", Mods = new ItemMods { Int = 3, Mp = 10, Evasion = 1 } },
        };

        static ArtifactDef[] _all;
        static Dictionary<string, ArtifactDef> _byId;

        /// <summary>Every unique item: the first thirteen, and the many that came after (Artifacts.More.cs).</summary>
        public static ArtifactDef[] All
        {
            get
            {
                if (_all == null)
                {
                    var l = new List<ArtifactDef>(Original); l.AddRange(More());
                    var d = new Dictionary<string, ArtifactDef>();
                    foreach (var a in l) d[a.Id] = a;
                    _all = l.ToArray(); _byId = d;
                }
                return _all;
            }
        }

        public static ArtifactDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var _ = All;
            return _byId.TryGetValue(id, out var a) ? a : null;
        }

        /// <summary>The first unique placed on a level by rule (always-there ones only); kept for callers that do not roll.</summary>
        public static ArtifactDef ForLevel(string branch, int depth)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Branch == branch && All[i].Depth == depth && All[i].Chance >= 100) return All[i];
            return null;
        }

        /// <summary>The uniques this level holds: each one that belongs here rolls its own chance (the first thirteen always succeed).</summary>
        public static List<ArtifactDef> RollForLevel(string branch, int depth, Rng rng)
        {
            var here = new List<ArtifactDef>();
            for (int i = 0; i < All.Length; i++)
            {
                var a = All[i];
                if (a.Branch != branch || a.Depth != depth) continue;
                if (a.Chance >= 100 || rng.Chance(a.Chance)) here.Add(a);
            }
            return here;
        }

        public static Item Create(ArtifactDef def, Rng rng, long uid)
        {
            ItemDef baseDef = default; bool found = false;
            foreach (var list in new[] { Catalogue.Weapons, Catalogue.Armor, Catalogue.Helms, Catalogue.Gloves, Catalogue.Boots, Catalogue.Cloaks, Catalogue.Shields, Catalogue.Rings, Catalogue.Amulets })
            {
                foreach (var d in list) if (d.Name == def.Base) { baseDef = d; found = true; break; }
                if (found) break;
            }
            if (!found) throw new InvalidOperationException("Artifact base missing: " + def.Base);
            return new Item(baseDef, rng, uid) { Rarity = Rarity.Artifact, ArtifactId = def.Id, ArtifactName = def.Name, Enchant = def.Enchant, Identified = false };
        }
    }

    /// <summary>Decides whether loot is magical and what it carries. Deeper levels roll better.</summary>
    public static class ItemRoller
    {
        public static void Roll(Item item, Rng rng, int depth)
        {
            if (!item.Def.Kind.IsGear()) return;
            bool weapon = item.Def.Kind == ItemKind.Weapon;
            int rareChance = Math.Min(15, 2 + depth);
            int magicChance = Math.Min(45, 10 + depth * 3);
            int roll = rng.Range(0, 100);
            if (roll < rareChance)
            {
                item.Rarity = Rarity.Rare;
                item.Enchant = rng.Range(1, 3) + (depth >= 10 ? 1 : 0);
                item.Prefix = Affixes.Pick(rng, true, weapon)?.Id;
                item.Suffix = Affixes.Pick(rng, false, weapon)?.Id;
            }
            else if (roll < rareChance + magicChance)
            {
                item.Rarity = Rarity.Magic;
                item.Enchant = rng.Range(0, 2);
                if (rng.Chance(50)) item.Prefix = Affixes.Pick(rng, true, weapon)?.Id;
                else item.Suffix = Affixes.Pick(rng, false, weapon)?.Id;
                if (item.Prefix == null && item.Suffix == null && item.Enchant == 0) item.Enchant = 1;
            }
            else return;
            item.Identified = false;
            item.Value = item.TradeValue;
        }
    }
}
