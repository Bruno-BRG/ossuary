using System.Collections.Generic;

namespace Ossuary.Core.Entities
{
    /// <summary>
    /// One perk: what a level-up pick buys. Level-ups still grant their base HP/XP curve
    /// immediately; each level also queues one pending pick the player spends here. Perks have
    /// ranks, may be limited to roles, and can require a level, a skill or another perk.
    /// A perk may also grant an active ability (see <see cref="Abilities"/>). No RNG here, so a
    /// (seed, decisions) replay stays exact.
    /// </summary>
    public sealed class AdvanceDef
    {
        public string Id;
        public string Name;
        public string Blurb;
        public int MaxRank = 1;
        /// <summary>Role ids that may take it; null means any.</summary>
        public string[] Roles;
        public int MinLevel = 1;
        public Skill ReqSkill; public int ReqSkillMin;
        public string Requires;       // another perk id
        public bool NeedsMagic;       // only for characters with a mana pool
        public string GrantsAbility;
    }

    public static class Progression
    {
        static AdvanceDef P(string id, string name, string blurb, int max = 1, string[] roles = null, int minLevel = 1,
            Skill skill = Skill.Combat, int skillMin = 0, string requires = null, bool magic = false, string ability = null)
            => new AdvanceDef { Id = id, Name = name, Blurb = blurb, MaxRank = max, Roles = roles, MinLevel = minLevel,
                ReqSkill = skill, ReqSkillMin = skillMin, Requires = requires, NeedsMagic = magic, GrantsAbility = ability };

        static readonly string[] Martial = { "fighter", "paladin", "ranger", "rogue", "adventurer" };
        static readonly string[] Heavy = { "fighter", "paladin", "adventurer", "ranger" };
        static readonly string[] Shielded = { "fighter", "paladin", "cleric" };

        public static readonly AdvanceDef[] All = {
            // ---- Attributes (anyone)
            P("tough",   "Tough",   "+6 max HP, and heal 6 now", 5),
            P("mighty",  "Mighty",  "+1 Strength", 4),
            P("agile",   "Agile",   "+1 Dexterity, +4 Dodging", 4),
            P("hale",    "Hale",    "+1 Constitution", 4),
            P("learned", "Learned", "+1 Intelligence, +5 Magic", 4),
            P("devout",  "Devout",  "+1 Wisdom, +5 Survival", 4),
            P("focused", "Focused", "+6 Magic, +4 Search", 3),
            // ---- General
            P("lucky",         "Lucky",         "+2 Evasion per rank", 2, null, 2),
            P("iron-will",     "Iron Will",     "+20% poison resistance per rank", 2, null, 3, Skill.Combat, 0, "hale"),
            P("gourmand",      "Gourmand",      "You get hungry half as fast", 1, null, 2, Skill.Survival, 10),
            P("quick-learner", "Quick Learner", "+15% experience per rank", 2, null, 2),
            P("vigorous",      "Vigorous",      "+8 Vigor and faster Vigor recovery per rank", 2, null, 2),
            // ---- Martial
            P("weapon-master", "Weapon Master", "+1 to hit and +1 damage per rank", 3, Martial, 2, Skill.Combat, 15),
            P("power-strike",  "Power Strike",  "Ability: a blow for double damage (4 Vigor)", 1, Heavy, 1, Skill.Combat, 0, null, false, "power-strike"),
            P("second-wind",   "Second Wind",   "Ability: recover a quarter of your HP (8 Vigor)", 1, Heavy, 3, Skill.Combat, 0, null, false, "second-wind"),
            P("cleave",        "Cleave",        "Ability: strike every adjacent foe (6 Vigor)", 1, new[] { "fighter", "paladin" }, 5, Skill.Combat, 0, "weapon-master", false, "cleave"),
            P("shield-bash",   "Shield Bash",   "Ability: stagger a foe; needs a shield (4 Vigor)", 1, Shielded, 2, Skill.Combat, 0, null, false, "shield-bash"),
            P("war-cry",       "War Cry",       "Ability: frighten the living around you (8 Vigor)", 1, new[] { "fighter", "paladin" }, 4, Skill.Combat, 0, null, false, "war-cry"),
            P("shield-wall",   "Shield Wall",   "+2 AC per rank while carrying a shield", 2, Shielded, 3),
            // ---- Holy
            P("lay-on-hands",  "Lay on Hands",  "Ability: heal half your HP and cleanse poison (10 Vigor)", 1, new[] { "paladin", "cleric" }, 1, Skill.Combat, 0, null, false, "lay-on-hands"),
            P("holy-strike",   "Holy Strike",   "Ability: a blow that burns evil; the dead take double (6 Vigor)", 1, new[] { "paladin", "cleric" }, 3, Skill.Combat, 0, null, false, "holy-strike"),
            P("aura",          "Aura of Protection", "+1 AC per rank while armoured", 2, new[] { "paladin" }, 3),
            // ---- Stealth and archery
            P("backstab",      "Backstab",      "Ability: triple damage on a sleeping, fleeing or unaware foe (4 Vigor)", 1, new[] { "rogue", "ranger", "adventurer" }, 1, Skill.Combat, 0, null, false, "backstab"),
            P("vanish",        "Vanish",        "Ability: turn invisible for a few turns (8 Vigor)", 1, new[] { "rogue", "adventurer" }, 3, Skill.Combat, 0, null, false, "vanish"),
            P("light-feet",    "Light Feet",    "Foes notice you from 2 cells less per rank", 3, new[] { "rogue", "ranger" }, 2),
            P("aimed-shot",    "Aimed Shot",    "Ability: a careful shot, +4 to hit, double damage (5 Vigor)", 1, new[] { "ranger", "rogue", "adventurer" }, 1, Skill.Combat, 0, null, false, "aimed-shot"),
            P("keen-eye",      "Keen Eye",      "+2 to hit with missiles per rank", 2, new[] { "ranger", "rogue" }, 2),
            // ---- Casting
            P("mind-expansion","Mind Expansion","+6 maximum Mp per rank", 3, null, 2, Skill.Combat, 0, null, true),
            P("focus",         "Focus",         "-5% spell failure per rank", 3, null, 2, Skill.Combat, 0, null, true),
            P("spell-power",   "Spell Power",   "+2 damage to every damaging spell per rank", 3, null, 3, Skill.Combat, 0, null, true),
            P("quick-recovery","Quick Recovery","Mana returns 2 turns sooner per point, per rank", 2, null, 3, Skill.Combat, 0, null, true),
        };

        public static AdvanceDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>True when the player may take (another rank of) this perk now.</summary>
        public static bool IsAvailable(Player p, AdvanceDef d)
        {
            if (d == null) return false;
            if (p.PerkRank(d.Id) >= d.MaxRank) return false;
            if (d.Roles != null && System.Array.IndexOf(d.Roles, p.RoleId) < 0) return false;
            if (p.Level < d.MinLevel) return false;
            if (d.ReqSkillMin > 0 && p.Skills[d.ReqSkill] < d.ReqSkillMin) return false;
            if (d.Requires != null && p.PerkRank(d.Requires) == 0) return false;
            if (d.NeedsMagic && p.MpMax <= 0) return false;
            return true;
        }

        public static List<AdvanceDef> Available(Player p)
        {
            var list = new List<AdvanceDef>();
            foreach (var d in All) if (IsAvailable(p, d)) list.Add(d);
            return list;
        }

        /// <summary>Applies the perk to the player. Returns false on unknown or unavailable ids.</summary>
        public static bool Apply(Player p, string id)
        {
            var def = Find(id);
            if (!IsAvailable(p, def)) return false;
            p.Perks[id] = p.PerkRank(id) + 1;
            if (def.GrantsAbility != null) p.LearnAbility(def.GrantsAbility);
            switch (id)
            {
                case "tough":
                    p.BonusMaxHP += 6;
                    p.RecomputeMaxHP();
                    p.HP = System.Math.Min(p.MaxHP, p.HP + 6);
                    break;
                case "mighty":
                    p.Str = System.Math.Min(21, p.Str + 1);
                    if (p.Str == 18 && p.StrFrac == 0) p.StrFrac = 50;
                    p.RecomputeMaxHP();
                    break;
                case "agile":
                    p.Dex = System.Math.Min(21, p.Dex + 1);
                    p.GainSkill(Skill.Dodging, 4);
                    break;
                case "hale":
                    p.Con = System.Math.Min(21, p.Con + 1);
                    p.RecomputeMaxHP();
                    break;
                case "learned":
                    p.Int = System.Math.Min(21, p.Int + 1);
                    p.GainSkill(Skill.Magic, 5);
                    break;
                case "devout":
                    p.Wis = System.Math.Min(21, p.Wis + 1);
                    p.GainSkill(Skill.Survival, 5);
                    break;
                case "focused":
                    p.GainSkill(Skill.Magic, 6);
                    p.GainSkill(Skill.Search, 4);
                    break;
                case "vigorous":
                    p.RecomputeMaxVigor();
                    p.Vigor = p.VigorMax;
                    break;
            }
            return true;
        }
    }
}
