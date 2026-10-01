using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.Entities
{
    public enum Skill
    {
        Combat, Dodging, Stealth, Magic, Survival, Search,
    }

    /// <summary>
    /// The player. Attributes follow NetHack's 3..20 scale with fractional strength,
    /// because that curve (the "18/xx" breakpoints) is what makes item-vs-attribute
    /// tradeoffs interesting rather than arithmetic.
    /// </summary>
    public sealed class Player : Actor
    {
        public int Str = 10, Dex = 10, Con = 10, Int = 11, Wis = 10, Cha = 10;
        public int StrFrac;   // 0..99, the "18/xx" part

        public int Gold;
        public int XpLevel;
        public int XpNext = 20;
        public int EnergyMax = 12;

        /// <summary>Satiety pool, NetHack style. Drains one per turn; food refills it.</summary>
        public int Nutrient = 1000;

        /// <summary>Turns spent below zero nutrient; drives the "starving" messages.</summary>
        public int Hunger;

        /// <summary>Legacy accessor kept for the HUD; mirrors <see cref="Nutrient"/>.</summary>
        public int FoodNutrient
        {
            get => Nutrient;
            set => Nutrient = value;
        }
        public int Thirst;
        public int Role;
        public string RoleId = "adventurer";
        public string Title = "Adventurer";

        /// <summary>Unspent level-up picks (see Progression). Base HP/XP is granted
        /// immediately on level; these are the player's bonus choices.</summary>
        public int PendingAdvances;
        public readonly List<string> AdvancesTaken = new List<string>();

        /// <summary>Flat max-HP from "tough" advances. Kept separate so role HP
        /// curves and ring bonuses stay recomputable.</summary>
        public int BonusMaxHP;
        public int Kills;
        public int Turns;
        public bool InsideDungeon;
        public string CurrentBranch = "";
        public int CurrentDepth;
        public int MaxDepth;
        public int Progress = 0;
        public bool NewGamePlus;

        public readonly Dictionary<Skill, int> Skills = new Dictionary<Skill, int>
        {
            { Skill.Combat, 0 }, { Skill.Dodging, 0 }, { Skill.Stealth, 0 },
            { Skill.Magic, 0 }, { Skill.Survival, 0 }, { Skill.Search, 0 },
        };

        public Player(Rng rng) : this(rng, "adventurer")
        {
        }

        public Player(Rng rng, string roleId)
        {
            IsPlayer = true;
            Glyph = '@';
            Name = "you";
            Align = Alignment.Neutral;
            Level = 1;
            Speed = 12;
            AC = 10;
            EnergyMax = 12;
            RoleId = string.IsNullOrEmpty(roleId) ? "adventurer" : roleId;
            var role = Roles.Find(RoleId);
            RoleId = role.Id;
            RollAttributes(rng, role);
            foreach (var kv in role.StartingSkills) Skills[kv.Key] = System.Math.Min(100, Skills[kv.Key] + kv.Value);
            Title = Roles.TitleFor(RoleId, Level);
            RecomputeMaxHP();
            HP = MaxHP;
            Energy = 12;
        }

        public int TotalStrength => Str;

        void RollAttributes(Rng rng, RoleDef role)
        {
            Str = rng.Roll(3, 6, 6);
            Dex = rng.Roll(3, 6, 6);
            Con = rng.Roll(3, 6, 7);
            Int = rng.Roll(3, 6, 7);
            Wis = rng.Roll(3, 6, 7);
            Cha = rng.Roll(3, 6, 7);
            if (role != null)
            {
                // Same seed, same base roll: only the role bias differs, so runs
                // stay comparable across roles and replays stay exact.
                Str = ClampAttr(Str + role.StrMod);
                Dex = ClampAttr(Dex + role.DexMod);
                Con = ClampAttr(Con + role.ConMod);
                Int = ClampAttr(Int + role.IntMod);
                Wis = ClampAttr(Wis + role.WisMod);
                Cha = ClampAttr(Cha + role.ChaMod);
            }
            StrFrac = Str == 18 ? rng.Range(0, 100) : 0;
        }

        static int ClampAttr(int v) => v < 3 ? 3 : v > 18 ? 18 : v;

        public int HpPerLevel()
        {
            var r = Roles.Find(RoleId);
            return r != null ? r.HpPerLevel : 4;
        }

        /// <summary>NetHack-style: level 10-ish baseline plus a constitution contribution.</summary>
        public void RecomputeMaxHP()
        {
            int prev = MaxHP;
            MaxHP = 20 + (Level - 1) * HpPerLevel() + Con + Str - 10 + RingBonus(Ability.Constitution) + BonusMaxHP;
            if (MaxHP < 1) MaxHP = 1;
            if (prev > 0 && MaxHP > prev) HP += MaxHP - prev;
            if (HP > MaxHP) HP = MaxHP;
            if (HP < 1) HP = 1;
        }

        public int RingBonus(Ability a)
        {
            int b = 0;
            for (int i = 0; i < 2; i++)
            {
                var r = Rings[i];
                if (r == null) continue;
                if (RingKnown[i] && r.Name == "ring of constitution") b += 2;
            }
            return b;
        }

        public int StrengthForDamage()
        {
            if (Str <= 18) return Str;
            if (Str == 18) return 18 + (StrFrac / 50);  // 18/00-49 -> 18, 18/50-99 -> 19
            return 19 + ((Str - 18) * 2) + (StrFrac / 50);
        }

        public int ArmorClass()
        {
            int ac = AC;
            if (WornArmor != null) ac -= WornArmor.Def.AC;
            if (WornShield != null) ac -= WornShield.Def.AC;
            if (Rings[0] != null && RingKnown[0] && Rings[0].Name == "ring of protection") ac -= 3;
            if (Rings[1] != null && RingKnown[1] && Rings[1].Name == "ring of protection") ac -= 3;
            int dexAdj = Dex >= 10 ? (Dex - 10) / 2 : -((10 - Dex) / 2);
            ac -= dexAdj;
            if (WornArmor != null && (WornArmor.Def.Flags & ItemFlags.Cursed) != 0) ac += 2;
            return ac;
        }

        public int Evasion()
        {
            int e = 2;
            e += (Dex - 10) / 2;
            if (WornArmor != null && (WornArmor.Def.Flags & ItemFlags.Cursed) != 0) e -= 3;
            if (Rings[0] != null && RingKnown[0] && Rings[0].Name == "ring of warning") e += 2;
            if (Rings[1] != null && RingKnown[1] && Rings[1].Name == "ring of warning") e += 2;
            if (Invisible) e += 3;
            if (Asleep) e -= 5;
            if (Confused) e -= 2;
            return Math.Max(0, e);
        }

        public bool AddXp(int amount)
        {
            Xp += amount;
            bool leveled = false;
            while (Xp >= XpNext)
            {
                Xp -= XpNext;
                Level++;
                XpNext = (int)(XpNext * 1.35) + 10;
                PendingAdvances++;
                Title = Roles.TitleFor(RoleId, Level);
                RecomputeMaxHP();
                HP = MaxHP;
                Energy = EnergyMax;
                leveled = true;
            }
            return leveled;
        }

        /// <summary>Spends one pending level-up pick. Returns false when there is
        /// nothing to spend or the id is unknown.</summary>
        public bool ApplyAdvance(string id)
        {
            if (PendingAdvances <= 0) return false;
            if (!Progression.Apply(this, id)) return false;
            PendingAdvances--;
            AdvancesTaken.Add(id);
            return true;
        }

        public int XpForNext() => Math.Max(1, XpNext - Xp);

        public string LevelString => $"Dlvl {Level:00}, XP {Xp:XpForNext():X00}";

        public override string LongDescription =>
            $"{Name}, the {Title} (level {Level}, {HP}/{MaxHP} HP, AC {ArmorClass()}, {XpForNext()} XP to level {Level + 1})";

        public string AttributeLine()
        {
            string sfrac = Str == 18 ? StrFrac.ToString("00") : "  ";
            return $"Str:{Str,2}/{sfrac} Dex:{Dex,2} Con:{Con,2} Int:{Int,2} Wis:{Wis,2} Cha:{Cha,2}";
        }

        public int CarryingCapacity() => 50 + Str * 15;

        public bool IsOverloaded() => WeightCarried() > CarryingCapacity();

        public string AlignmentString => Align.ToString();

        public int GainSkill(Skill s, int amount)
        {
            Skills[s] = Math.Min(100, Skills[s] + amount);
            return Skills[s];
        }
    }
}
