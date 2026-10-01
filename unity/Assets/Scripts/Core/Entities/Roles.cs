using System.Collections.Generic;

namespace Ossuary.Core.Entities
{
    /// <summary>
    /// A playable role: attribute bias, starting skills, kit and title track.
    /// Modifiers are applied on top of the shared 3d6-style roll so every role
    /// stays on the same NetHack 3..20 scale and the same seed still replays.
    /// </summary>
    public sealed class RoleDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int StrMod, DexMod, ConMod, IntMod, WisMod, ChaMod;
        public int HpPerLevel;
        public int Gold;
        public int Rations = 2;
        public string Weapon;
        public string Armor;
        public string Shield;
        public string Book;
        public string Tool;
        public Dictionary<Skill, int> StartingSkills = new Dictionary<Skill, int>();
        public string[] Titles = new string[0];
    }

    public static class Roles
    {
        public static readonly RoleDef[] All = {
            new RoleDef {
                Id = "adventurer", Name = "Adventurer",
                Description = "No destiny yet. Average at everything, owed nothing.",
                HpPerLevel = 4, Gold = 30, Rations = 3,
                Weapon = "dagger", Armor = "leather armour", Tool = "lock pick",
                Titles = new[] { "Adventurer", "Explorer", "Veteran", "Legend" },
            },
            new RoleDef {
                Id = "fighter", Name = "Fighter",
                Description = "Holds the line. Strong and hardy, slow to learn magic.",
                StrMod = 2, ConMod = 2, DexMod = 1, IntMod = -1,
                HpPerLevel = 6, Gold = 20, Rations = 2,
                Weapon = "short sword", Armor = "ring mail",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Combat, 10 }, { Skill.Survival, 5 } },
                Titles = new[] { "Squire", "Warrior", "Knight", "Champion" },
            },
            new RoleDef {
                Id = "rogue", Name = "Rogue",
                Description = "Strikes from the dark. Quick hands, light armour, keen eyes.",
                DexMod = 3, IntMod = 1, StrMod = -1,
                HpPerLevel = 4, Gold = 40, Rations = 2,
                Weapon = "dagger", Armor = "leather armour", Tool = "lock pick",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Stealth, 10 }, { Skill.Search, 10 }, { Skill.Dodging, 5 } },
                Titles = new[] { "Footpad", "Thief", "Shadow", "Master Thief" },
            },
            new RoleDef {
                Id = "cleric", Name = "Cleric",
                Description = "Anointed to endure. Wise, sturdy, and hard to poison with prayer.",
                WisMod = 3, ConMod = 1, DexMod = -1,
                HpPerLevel = 5, Gold = 25, Rations = 2,
                Weapon = "mace", Armor = "leather armour", Shield = "small shield", Book = "a book of prayers",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Survival, 10 }, { Skill.Magic, 5 } },
                Titles = new[] { "Acolyte", "Priest", "Vicar", "High Priest" },
            },
            new RoleDef {
                Id = "wizard", Name = "Wizard",
                Description = "Traded muscle for study. Frail, brilliant, already half-attuned.",
                IntMod = 3, WisMod = 1, StrMod = -2, ConMod = -1,
                HpPerLevel = 3, Gold = 25, Rations = 2,
                Weapon = "quarterstaff", Armor = "leather armour", Book = "a spellbook",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Magic, 15 }, { Skill.Search, 5 } },
                Titles = new[] { "Apprentice", "Conjurer", "Sorcerer", "Archmage" },
            },
        };

        public static RoleDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return All[0];
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return All[0];
        }

        /// <summary>Title tier by level: 1-3, 4-7, 8-11, 12+.</summary>
        public static string TitleFor(string roleId, int level)
        {
            var r = Find(roleId);
            if (r.Titles == null || r.Titles.Length == 0) return r.Name;
            int tier = level >= 12 ? 3 : level >= 8 ? 2 : level >= 4 ? 1 : 0;
            if (tier >= r.Titles.Length) tier = r.Titles.Length - 1;
            return r.Titles[tier];
        }
    }
}
