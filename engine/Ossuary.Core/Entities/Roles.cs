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
        /// <summary>A bow, crossbow or sling carried in the pack, and how much it starts with to loose.</summary>
        public string Launcher;
        public int AmmoCount;
        public string Tool;
        public Dictionary<Skill, int> StartingSkills = new Dictionary<Skill, int>();
        public string[] Titles = new string[0];
        /// <summary>Mp pool: base, per level after the first, and the attribute that feeds it ('I' Int, 'W' Wis).</summary>
        public int MpBase, MpPerLevel;
        public char MpStat = 'I';
        public string[] StartSpells = new string[0];
        /// <summary>Perk ids the hero begins with (usually the class's signature ability).</summary>
        public string[] StartPerks = new string[0];
        /// <summary>God the hero already follows at the start (Cleric and Paladin).</summary>
        public string StartGod;
        /// <summary>Ceiling per skill (default 100). Keeps a class from mastering everything.</summary>
        public Dictionary<Skill, int> SkillCaps = new Dictionary<Skill, int>();
        public int CapFor(Skill s) => SkillCaps.TryGetValue(s, out int c) ? c : 100;
    }

    public static class Roles
    {
        public static readonly RoleDef[] All = {
            new RoleDef {
                Id = "adventurer", Name = "Adventurer",
                Description = "No destiny yet. Average at everything, owed nothing.",
                MpBase = 2, MpPerLevel = 1, StartPerks = new[] { "power-strike" }, HpPerLevel = 5, Gold = 30, Rations = 3,
                Weapon = "dagger", Armor = "leather armour", Tool = "lock pick",
                Titles = new[] { "Adventurer", "Explorer", "Veteran", "Legend" },
            },
            new RoleDef {
                Id = "fighter", Name = "Fighter",
                Description = "Holds the line. Strong and hardy, slow to learn magic.",
                StrMod = 2, ConMod = 2, DexMod = 1, IntMod = -1,
                StartPerks = new[] { "power-strike" }, MpBase = 0, MpPerLevel = 0, SkillCaps = new Dictionary<Skill, int> { { Skill.Magic, 40 } }, HpPerLevel = 5, Gold = 20, Rations = 2,
                Weapon = "short sword", Armor = "ring mail", Book = Abilities.StrikesBook,
                StartingSkills = new Dictionary<Skill, int> { { Skill.Combat, 10 }, { Skill.Survival, 5 } },
                Titles = new[] { "Squire", "Warrior", "Knight", "Champion" },
            },
            new RoleDef {
                Id = "rogue", Name = "Rogue",
                Description = "Strikes from the dark. Quick hands, light armour, keen eyes.",
                DexMod = 3, IntMod = 1, StrMod = -1,
                StartPerks = new[] { "backstab" }, MpBase = 0, MpPerLevel = 1, SkillCaps = new Dictionary<Skill, int> { { Skill.Magic, 50 } }, HpPerLevel = 4, Gold = 40, Rations = 2,
                Weapon = "dagger", Armor = "leather armour", Tool = "lock pick", Book = "a cutpurse's primer", StartSpells = new[] { "throwing-knives", "hex" },
                Launcher = "sling", AmmoCount = 15,
                StartingSkills = new Dictionary<Skill, int> { { Skill.Stealth, 10 }, { Skill.Search, 10 }, { Skill.Dodging, 5 }, { Skill.Magic, 4 } },
                Titles = new[] { "Footpad", "Thief", "Shadow", "Master Thief" },
            },
            new RoleDef {
                Id = "cleric", Name = "Cleric",
                Description = "Anointed to endure. Wise, sturdy, and hard to poison with prayer.",
                WisMod = 3, ConMod = 1, DexMod = -1,
                StartSpells = new[] { "cure-wounds" }, StartGod = "aurel", MpBase = 4, MpPerLevel = 2, MpStat = 'W', SkillCaps = new Dictionary<Skill, int> { { Skill.Stealth, 50 } }, HpPerLevel = 5, Gold = 25, Rations = 2,
                Weapon = "mace", Armor = "leather armour", Shield = "small shield", Book = "a book of prayers",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Survival, 10 }, { Skill.Magic, 5 } },
                Titles = new[] { "Acolyte", "Priest", "Vicar", "High Priest" },
            },
            new RoleDef {
                Id = "wizard", Name = "Wizard",
                Description = "Traded muscle for study. Frail, brilliant, already half-attuned.",
                IntMod = 3, WisMod = 1, StrMod = -2, ConMod = -1,
                StartSpells = new[] { "magic-missile", "ward" }, MpBase = 8, MpPerLevel = 3, SkillCaps = new Dictionary<Skill, int> { { Skill.Combat, 60 } }, HpPerLevel = 4, Gold = 25, Rations = 2,
                Weapon = "quarterstaff", Armor = "leather armour", Book = "a spellbook",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Magic, 15 }, { Skill.Search, 5 } },
                Titles = new[] { "Apprentice", "Conjurer", "Sorcerer", "Archmage" },
            },
            new RoleDef {
                Id = "ranger", Name = "Ranger",
                Description = "Hunts what hunts others. Patient, sure-handed, at home outdoors.",
                DexMod = 2, WisMod = 1, ConMod = 1, IntMod = -1,
                StartPerks = new[] { "aimed-shot" }, MpBase = 0, MpPerLevel = 1, MpStat = 'W', SkillCaps = new Dictionary<Skill, int> { { Skill.Magic, 50 } }, HpPerLevel = 5, Gold = 25, Rations = 3,
                Weapon = "spear", Armor = "leather armour", Book = "a druid's handbook", StartSpells = new[] { "thorn-dart", "barkskin" },
                Launcher = "short bow", AmmoCount = 30,
                StartingSkills = new Dictionary<Skill, int> { { Skill.Survival, 10 }, { Skill.Combat, 5 }, { Skill.Stealth, 5 }, { Skill.Magic, 4 } },
                Titles = new[] { "Tracker", "Hunter", "Warden", "Beastmaster" },
            },
            new RoleDef {
                Id = "paladin", Name = "Paladin",
                Description = "Sworn to a god that may be dead. Armoured, devout, uncompromising.",
                StrMod = 2, WisMod = 1, ConMod = 1, DexMod = -2,
                StartSpells = new[] { "cure-wounds" }, StartPerks = new[] { "lay-on-hands" }, StartGod = "aurel", MpBase = 2, MpPerLevel = 1, MpStat = 'W', SkillCaps = new Dictionary<Skill, int> { { Skill.Stealth, 40 } }, HpPerLevel = 6, Gold = 20, Rations = 2,
                Weapon = "long sword", Armor = "ring mail", Shield = "small shield", Book = "a book of prayers",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Combat, 8 }, { Skill.Magic, 4 } },
                Titles = new[] { "Gallant", "Crusader", "Templar", "Lord Paladin" },
            },
            new RoleDef {
                Id = "necromancer", Name = "Necromancer",
                Description = "Studies the dead because the dead keep their secrets. Pale, patient.",
                IntMod = 2, WisMod = 1, ConMod = -1, ChaMod = -2, StrMod = -1,
                StartSpells = new[] { "drain-life", "raise-skeleton" }, MpBase = 9, MpPerLevel = 3, SkillCaps = new Dictionary<Skill, int> { { Skill.Combat, 60 } }, HpPerLevel = 4, Gold = 20, Rations = 1,
                Weapon = "dagger", Armor = "leather armour", Book = "a book of shadows",
                StartingSkills = new Dictionary<Skill, int> { { Skill.Magic, 12 }, { Skill.Stealth, 4 }, { Skill.Survival, 3 } },
                Titles = new[] { "Gravewalker", "Bonecaller", "Deathbinder", "Lord of Ossuary" },
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
