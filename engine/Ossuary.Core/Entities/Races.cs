using System.Collections.Generic;

namespace Ossuary.Core.Entities
{
    /// <summary>
    /// A playable people. Like roles, races only bias the shared base roll (same seed,
    /// same dice), so runs stay comparable and replays stay exact. Traits are plain data
    /// (resistances, Mp, regeneration, evasion) read by Player.
    /// </summary>
    public sealed class RaceDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int StrMod, DexMod, ConMod, IntMod, WisMod, ChaMod;
        /// <summary>Added to the role's HP per level (never below 2 overall).</summary>
        public int HpPerLevelMod;
        public int GoldMod;
        /// <summary>Percent change to maximum Mp (+25 = a quarter more).</summary>
        public int MpPct;
        /// <summary>HP regeneration speed in percent (100 = normal).</summary>
        public int RegenPct = 100;
        public int EvasionBonus;
        /// <summary>Free level-up picks at creation.</summary>
        public int StartAdvances;
        /// <summary>Resistance in percent per damage type (negative = vulnerable).</summary>
        public Dictionary<DamageType, int> Resist = new Dictionary<DamageType, int>();

        /// <summary>Short lines for the creation and character screens.</summary>
        public List<string> TraitLines()
        {
            var t = new List<string>();
            if (StartAdvances > 0) t.Add($"{StartAdvances} free advancement at start");
            foreach (var kv in Resist) t.Add($"{kv.Key} {(kv.Value > 0 ? "+" : "")}{kv.Value}%");
            if (MpPct != 0) t.Add($"Mana {(MpPct > 0 ? "+" : "")}{MpPct}%");
            if (RegenPct != 100) t.Add($"Regeneration {RegenPct}%");
            if (EvasionBonus != 0) t.Add($"Evasion {(EvasionBonus > 0 ? "+" : "")}{EvasionBonus}");
            return t;
        }
        public Alignment Align = Alignment.Neutral;
        public Dictionary<Skill, int> StartingSkills = new Dictionary<Skill, int>();
    }

    public static class Races
    {
        public static readonly RaceDef[] All = {
            new RaceDef {
                Id = "human", Name = "Human",
                Description = "Short-lived and stubborn. No gift, no curse; the dungeon's oldest tenants.",
                Align = Alignment.Neutral, StartAdvances = 1,
            },
            new RaceDef {
                Id = "dwarf", Name = "Dwarf",
                Description = "Deepwrought and thick-skinned. Hardy, grim, bad at small talk.",
                ConMod = 2, StrMod = 1, ChaMod = -1, DexMod = -1, HpPerLevelMod = 1, GoldMod = 15,
                Align = Alignment.LawfulNeutral,
                Resist = new Dictionary<DamageType, int> { { DamageType.Poison, 50 } },
                StartingSkills = new Dictionary<Skill, int> { { Skill.Survival, 5 }, { Skill.Search, 3 } },
            },
            new RaceDef {
                Id = "elf", Name = "Elf",
                Description = "Quick, keen and fragile. Remembers the Age of Cities as yesterday.",
                DexMod = 2, IntMod = 1, ConMod = -2, HpPerLevelMod = -1,
                Align = Alignment.ChaoticGood, MpPct = 25, EvasionBonus = 1,
                StartingSkills = new Dictionary<Skill, int> { { Skill.Magic, 5 }, { Skill.Search, 5 } },
            },
            new RaceDef {
                Id = "halfling", Name = "Halfling",
                Description = "Small, lucky and underestimated. Walks quietly, eats well.",
                DexMod = 2, ChaMod = 1, StrMod = -2, HpPerLevelMod = -1,
                Align = Alignment.LawfulGood, EvasionBonus = 2,
                StartingSkills = new Dictionary<Skill, int> { { Skill.Stealth, 10 }, { Skill.Dodging, 5 } },
            },
            new RaceDef {
                Id = "orc", Name = "Orc",
                Description = "Barracks-born. Heavy hands, short temper, hard to put down.",
                StrMod = 2, ConMod = 1, IntMod = -1, ChaMod = -2, HpPerLevelMod = 1, GoldMod = -10,
                Align = Alignment.ChaoticNeutral, RegenPct = 200,
                Resist = new Dictionary<DamageType, int> { { DamageType.Poison, 25 } },
                StartingSkills = new Dictionary<Skill, int> { { Skill.Combat, 5 } },
            },
            new RaceDef {
                Id = "gnome", Name = "Gnome",
                Description = "Tinkers and scholars of the sunken archives. Clever, slight, curious.",
                IntMod = 2, ConMod = 1, StrMod = -2, HpPerLevelMod = -1,
                Align = Alignment.NeutralGood, MpPct = 15,
                StartingSkills = new Dictionary<Skill, int> { { Skill.Magic, 8 }, { Skill.Search, 3 } },
            },
            new RaceDef {
                Id = "ashen", Name = "Ashen",
                Description = "Touched by the Spire's fire. Grey skin, warm blood, cold company.",
                WisMod = 1, ConMod = 1, ChaMod = -2,
                Align = Alignment.NeutralEvil,
                Resist = new Dictionary<DamageType, int> { { DamageType.Fire, 50 }, { DamageType.Cold, -25 } },
                StartingSkills = new Dictionary<Skill, int> { { Skill.Survival, 5 }, { Skill.Magic, 3 } },
            },
        };

        public static RaceDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return All[0];
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return All[0];
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return i;
            return 0;
        }
    }
}

namespace Ossuary.Core
{
    /// <summary>Hero naming rules shared by the creation screen and the constructors.</summary>
    public static class Heroes
    {
        public const int MaxName = 16;
        public const string DefaultName = "Wanderer";

        /// <summary>Printable ASCII only (the font and the save file stay safe), trimmed, bounded.</summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return DefaultName;
            var sb = new System.Text.StringBuilder();
            foreach (char c in name.Trim())
            {
                if (c < 32 || c > 126) continue;
                if (c == ' ' && (sb.Length == 0 || sb[sb.Length - 1] == ' ')) continue;
                sb.Append(c);
                if (sb.Length >= MaxName) break;
            }
            string s = sb.ToString().TrimEnd();
            return s.Length == 0 ? DefaultName : s;
        }
    }
}
