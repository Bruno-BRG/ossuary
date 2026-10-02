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
            if (ResFire != 0) l.Add($"fire {ResFire}%");
            if (ResCold != 0) l.Add($"cold {ResCold}%");
            if (ResLightning != 0) l.Add($"lightning {ResLightning}%");
            if (ResPoison != 0) l.Add($"poison {ResPoison}%");
            if (ResNecrotic != 0) l.Add($"necrotic {ResNecrotic}%");
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
    }

    public static class Artifacts
    {
        public static readonly ArtifactDef[] All = {
            new ArtifactDef { Id = "veil-first-cell", Name = "Veil of the First Cell", Base = "cloak", Branch = "The Dungeons", Depth = 8, Enchant = 2,
                Lore = "Woven by the first prisoner, from the shirts of the ones who never came back.", Mods = new ItemMods { Evasion = 3, Dex = 1 } },
            new ArtifactDef { Id = "dwarfdeep-cleaver", Name = "Dwarfdeep Cleaver", Base = "battle axe", Branch = "The Mines of Dwarfdeep", Depth = 6, Enchant = 3,
                Lore = "Left behind when the dwarves abandoned the black water. It still remembers the stone.", Mods = new ItemMods { Str = 2, Dmg = 2 } },
            new ArtifactDef { Id = "rat-kings-tooth", Name = "Rat King's Tooth", Base = "dagger", Branch = "The Warrens", Depth = 7, Enchant = 2,
                Lore = "Gnawed, not forged. It drinks.", Mods = new ItemMods { Dex = 2, ExtraType = DamageType.Poison, ExtraSides = 4, LifeSteal = 20 } },
            new ArtifactDef { Id = "crown-drowned-king", Name = "Crown of the Drowned King", Base = "dwarvish helm", Branch = "The Sunken Vaults", Depth = 10, Enchant = 3,
                Lore = "Cold as the water that took him. It still wants a head to wear it.", Mods = new ItemMods { Wis = 3, Mp = 8, ResCold = 40 } },
            new ArtifactDef { Id = "ashfall", Name = "Ashfall", Base = "long sword", Branch = "The Ashen Spire", Depth = 13, Enchant = 2,
                Lore = "Forged to burn the dead. It could not tell the difference.", Mods = new ItemMods { ResFire = 40, ExtraType = DamageType.Fire, ExtraSides = 6 } },
        };

        public static ArtifactDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static ArtifactDef ForLevel(string branch, int depth)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Branch == branch && All[i].Depth == depth) return All[i];
            return null;
        }

        public static Item Create(ArtifactDef def, Rng rng, long uid)
        {
            ItemDef baseDef = default; bool found = false;
            foreach (var list in new[] { Catalogue.Weapons, Catalogue.Armor, Catalogue.Helms, Catalogue.Gloves, Catalogue.Boots, Catalogue.Cloaks, Catalogue.Shields })
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
