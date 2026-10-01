namespace Ossuary.Core.Entities
{
    /// <summary>
    /// One spendable level-up pick. Level-ups still grant their base HP/XP curve
    /// immediately (so old saves and the soak bot keep working); each level also
    /// queues one pending advance the player spends when ready. No RNG here, so a
    /// (seed, decisions) replay stays exact.
    /// </summary>
    public sealed class AdvanceDef
    {
        public string Id;
        public string Name;
        public string Blurb;
    }

    public static class Progression
    {
        public static readonly AdvanceDef[] All = {
            new AdvanceDef { Id = "tough",   Name = "Tough",   Blurb = "+6 max HP, and heal 6 now" },
            new AdvanceDef { Id = "mighty",  Name = "Mighty",  Blurb = "+1 Strength" },
            new AdvanceDef { Id = "agile",   Name = "Agile",   Blurb = "+1 Dexterity, +4 Dodging" },
            new AdvanceDef { Id = "hale",    Name = "Hale",    Blurb = "+1 Constitution" },
            new AdvanceDef { Id = "learned", Name = "Learned", Blurb = "+1 Intelligence, +5 Magic" },
            new AdvanceDef { Id = "devout",  Name = "Devout",  Blurb = "+1 Wisdom, +5 Survival" },
            new AdvanceDef { Id = "focused", Name = "Focused", Blurb = "+6 Magic, +4 Search" },
        };

        public static AdvanceDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }

        /// <summary>Applies the advance to the player. Returns false on unknown id.</summary>
        public static bool Apply(Player p, string id)
        {
            switch (id)
            {
                case "tough":
                    p.BonusMaxHP += 6;
                    p.RecomputeMaxHP();
                    p.HP = System.Math.Min(p.MaxHP, p.HP + 6);
                    return true;
                case "mighty":
                    p.Str = System.Math.Min(21, p.Str + 1);
                    if (p.Str == 18 && p.StrFrac == 0) p.StrFrac = 50;
                    p.RecomputeMaxHP();
                    return true;
                case "agile":
                    p.Dex = System.Math.Min(21, p.Dex + 1);
                    p.GainSkill(Skill.Dodging, 4);
                    return true;
                case "hale":
                    p.Con = System.Math.Min(21, p.Con + 1);
                    p.RecomputeMaxHP();
                    return true;
                case "learned":
                    p.Int = System.Math.Min(21, p.Int + 1);
                    p.GainSkill(Skill.Magic, 5);
                    return true;
                case "devout":
                    p.Wis = System.Math.Min(21, p.Wis + 1);
                    p.GainSkill(Skill.Survival, 5);
                    return true;
                case "focused":
                    p.GainSkill(Skill.Magic, 6);
                    p.GainSkill(Skill.Search, 4);
                    return true;
                default:
                    return false;
            }
        }
    }
}
