using System;
using System.Collections.Generic;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

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

        /// <summary>Mana. Spent by spells (a later part); pool and regen are derived from role, race, attributes and Magic.</summary>
        public int Mp, MpMax;
        public int MpTimer, HpTimer;
        /// <summary>Spell ids the hero has learned, in learning order.</summary>
        public readonly List<string> Spells = new List<string>();
        /// <summary>Timed spell effects by id (ward, haste, ...): turns left.</summary>
        public readonly Dictionary<string, int> Buffs = new Dictionary<string, int>();
        public int BuffTurns(string id) => Buffs.TryGetValue(id, out int t) ? t : 0;
        public void SetBuff(string id, int turns)
        {
            if (turns > 0) Buffs[id] = turns; else Buffs.Remove(id);
            if (SpellBuffs.Find(id) != null) RefreshGear();
        }

        /// <summary>What the rings and the amulet add up to.</summary>
        public ItemMods AccessoryMods
        {
            get
            {
                var m = new ItemMods();
                for (int i = 0; i < Rings.Length; i++) if (Rings[i] != null) m.Add(Rings[i].Mods);
                if (Amulet != null) m.Add(Amulet.Mods);
                return m;
            }
        }

        /// <summary>What the spells on you add up to (the same numbers an item would give), and the part of it that is not stats.</summary>
        public ItemMods BuffMods
        {
            get
            {
                var m = new ItemMods();
                if (Buffs.Count == 0) return m;
                foreach (var kv in Buffs) { var b = SpellBuffs.Find(kv.Key); if (b != null) m.Add(b.Mods); }
                return m;
            }
        }
        public int WardTurns { get => BuffTurns("ward"); set => SetBuff("ward", value); }

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
        public string RaceId = "human";
        /// <summary>The hero's chosen name. <see cref="Actor.Name"/> stays "you" for the message log.</summary>
        public string CharName = "Wanderer";
        public string Title = "Adventurer";

        /// <summary>Unspent level-up picks (see Progression). Base HP/XP is granted
        /// immediately on level; these are the player's bonus choices.</summary>
        public int PendingAdvances;
        public readonly List<string> AdvancesTaken = new List<string>();

        /// <summary>Perk id to rank.</summary>
        public readonly Dictionary<string, int> Perks = new Dictionary<string, int>();
        public int PerkRank(string id) => Perks.TryGetValue(id, out int r) ? r : 0;
        /// <summary>Ability ids available to use (verb V), granted by perks.</summary>
        public readonly List<string> Abilities = new List<string>();
        public void LearnAbility(string id) { if (!Abilities.Contains(id)) Abilities.Add(id); }

        /// <summary>Stamina for abilities, separate from Mp: every hero has it, it comes back quickly.</summary>
        public int Vigor, VigorMax, VigorTimer;

        /// <summary>The god followed (id), piety 0..200, turns until the next safe prayer, and oaths broken.</summary>
        public string God;
        public int Piety, PrayerTimer, Renounced;
        /// <summary>A god's trial: deeds the god likes still to do (0 = none) and how many are done.</summary>
        public int TrialGoal, TrialDone;
        /// <summary>Reputation per house (see Houses), -100..100.</summary>
        public readonly Dictionary<string, int> Rep = new Dictionary<string, int>();
        /// <summary>0 none, 1 from piety 50, 2 from piety 100.</summary>
        public int GodTier => God == null ? 0 : Piety >= Gods.Tier2At ? 2 : Piety >= Gods.Tier1At ? 1 : 0;
        public int GodMeleeHit => God == "khorr" && GodTier >= 1 ? 1 : 0;
        public int GodMeleeDmg => God == "khorr" && GodTier >= 2 ? 2 : 0;

        /// <summary>Flat max-HP from "tough" advances. Kept separate so role HP
        /// curves and ring bonuses stay recomputable.</summary>
        public int BonusMaxHP;
        public int Kills;
        /// <summary>How far the Ossuary has got into the hero, 0..100. Every 20 points a mutation takes hold.</summary>
        public int Corruption;
        /// <summary>Mutation ids, in the order they took hold.</summary>
        public readonly List<string> Mutated = new List<string>();
        /// <summary>Body parts that carry a scar from a wound that healed after being torn, broken or worse.</summary>
        public readonly List<string> Scars = new List<string>();
        public int Turns;
        public bool InsideDungeon;
        public string CurrentBranch = "";
        public int CurrentDepth;
        public int MaxDepth;
        public int Progress = 0;
        public bool NewGamePlus;

        /// <summary>Experience in each trade (see Items/Trades.cs), by trade id. Absent means none.</summary>
        public readonly Dictionary<string, int> TradeXp = new Dictionary<string, int>();
        /// <summary>The last day the hero played for coin, so a town pays once a day.</summary>
        public int LastBuskDay = -1;

        public readonly Dictionary<Skill, int> Skills = new Dictionary<Skill, int>
        {
            { Skill.Combat, 0 }, { Skill.Dodging, 0 }, { Skill.Stealth, 0 },
            { Skill.Magic, 0 }, { Skill.Survival, 0 }, { Skill.Search, 0 },
        };

        public Player(Rng rng) : this(rng, "adventurer")
        {
        }

        public Player(Rng rng, string roleId) : this(rng, roleId, "human")
        {
        }

        public Player(Rng rng, string roleId, string raceId)
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
            var race = Races.Find(raceId);
            RaceId = race.Id;
            Align = race.Align;
            RollAttributes(rng, role, race);
            foreach (var kv in role.StartingSkills) GainSkill(kv.Key, kv.Value);
            foreach (var kv in race.StartingSkills) GainSkill(kv.Key, kv.Value);
            PendingAdvances = race.StartAdvances;
            Spells.AddRange(role.StartSpells);
            if (role.StartGod != null)
            {
                God = role.StartGod; Piety = 30;
                var god = Gods.Find(God); if (god != null) Align = god.Align;
            }
            foreach (string perk in role.StartPerks)
            {
                Perks[perk] = 1;
                var granted = Progression.Find(perk);
                if (granted != null && granted.GrantsAbility != null) LearnAbility(granted.GrantsAbility);
            }
            Title = Roles.TitleFor(RoleId, Level);
            RecomputeMaxHP();
            HP = MaxHP;
            RecomputeMaxMp();
            Mp = MpMax;
            RecomputeMaxVigor();
            Vigor = VigorMax;
            Energy = 12;
        }

        public int TotalStrength => Str;

        void RollAttributes(Rng rng, RoleDef role, RaceDef race)
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
            if (race != null)
            {
                Str = ClampAttr(Str + race.StrMod);
                Dex = ClampAttr(Dex + race.DexMod);
                Con = ClampAttr(Con + race.ConMod);
                Int = ClampAttr(Int + race.IntMod);
                Wis = ClampAttr(Wis + race.WisMod);
                Cha = ClampAttr(Cha + race.ChaMod);
            }
            StrFrac = Str == 18 ? rng.Range(0, 100) : 0;
        }

        static int ClampAttr(int v) => v < 3 ? 3 : v > 18 ? 18 : v;

        public int HpPerLevel()
        {
            var r = Roles.Find(RoleId);
            int hp = r != null ? r.HpPerLevel : 4;
            return Math.Max(2, hp + Races.Find(RaceId).HpPerLevelMod);
        }

        /// <summary>NetHack-style: level 10-ish baseline plus a constitution contribution.</summary>
        public void RecomputeMaxHP()
        {
            int prev = MaxHP;
            MaxHP = 20 + (Level - 1) * HpPerLevel() + Con + Str - 10 + RingBonus(Ability.Constitution) + BonusMaxHP + Gear.Hp;
            if (MaxHP < 1) MaxHP = 1;
            if (prev > 0 && MaxHP > prev) HP += MaxHP - prev;
            if (HP > MaxHP) HP = MaxHP;
            if (HP < 1) HP = 1;
        }

        /// <summary>
        /// Pool = role base + per level, plus the casting attribute above 10, plus Magic/10,
        /// scaled by the race. Roles with neither base nor growth never get mana.
        /// </summary>
        public void RecomputeMaxMp()
        {
            var role = Roles.Find(RoleId);
            var race = Races.Find(RaceId);
            if (role.MpBase == 0 && role.MpPerLevel == 0) { MpMax = 0; Mp = 0; return; }
            int prev = MpMax;
            int stat = role.MpStat == 'W' ? Wis : Int;
            int raw = role.MpBase + (Level - 1) * role.MpPerLevel + Math.Max(0, stat - 10) + Skills[Skill.Magic] / 10 + 6 * PerkRank("mind-expansion") + Gear.Mp;
            MpMax = Math.Max(1, raw * (100 + race.MpPct) / 100);
            if (prev > 0 && MpMax > prev) Mp += MpMax - prev;
            if (Mp > MpMax) Mp = MpMax;
        }

        public void RecomputeMaxVigor()
        {
            VigorMax = Math.Max(4, 8 + Level * 2 + (Con - 10) / 2 + 8 * PerkRank("vigorous") + Gear.Vigor);
            if (Vigor > VigorMax) Vigor = VigorMax;
        }

        /// <summary>Turns per point of Vigor.</summary>
        public int VigorRegenInterval() => Math.Max(2, 5 - PerkRank("vigorous"));

        /// <summary>Turns per point of Mp.</summary>
        public int MpRegenInterval()
        {
            var role = Roles.Find(RoleId);
            int stat = role.MpStat == 'W' ? Wis : Int;
            return Math.Max(3, Math.Min(20, 14 - (stat - 10) - Skills[Skill.Magic] / 15 - 2 * PerkRank("quick-recovery")));
        }

        /// <summary>Turns per point of HP: faster with level and Con, scaled by race.</summary>
        public int HpRegenInterval()
        {
            int turns = Math.Max(10, Math.Min(30, 30 - Level - (Con - 10) / 2));
            if (God == "aurel" && GodTier >= 1) turns = Math.Max(4, turns * 3 / 4);
            return Math.Max(2, turns * 100 / Math.Max(25, Races.Find(RaceId).RegenPct));
        }

        /// <summary>Resistance in percent (-100..90) from race; items join in later parts.</summary>
        public int ResistPct(DamageType type)
        {
            int pct = 0;
            if (Races.Find(RaceId).Resist.TryGetValue(type, out int r)) pct += r;
            if (type == DamageType.Poison) pct += 20 * PerkRank("iron-will");
            pct += Gear.Resist(type);
            if (type == DamageType.Fire && God == "veyra") pct += GodTier >= 2 ? 60 : GodTier == 1 ? 30 : 0;
            if (type == DamageType.Poison && God == "mourne" && GodTier >= 2) pct += 30;
            if (type == DamageType.Necrotic && ((God == "nhal" && GodTier >= 1) || (God == "aurel" && GodTier >= 2))) pct += 30;
            return Math.Max(-100, Math.Min(90, pct));
        }

        /// <summary>Damage after resistance; a resisted hit still does at least 1.</summary>
        public int ResistDamage(int amount, DamageType type)
        {
            if (amount <= 0) return 0;
            int pct = ResistPct(type);
            if (pct == 0) return amount;
            return Math.Max(1, amount * (100 - pct) / 100);
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

        // ------------------------------------------------------------ equipment

        /// <summary>Every armour piece currently worn: body, shield, helm, gloves, boots, cloak.</summary>
        public IEnumerable<Item> WornPieces()
        {
            if (WornArmor != null) yield return WornArmor;
            if (WornShield != null) yield return WornShield;
            if (WornHelm != null) yield return WornHelm;
            if (WornGloves != null) yield return WornGloves;
            if (WornBoots != null) yield return WornBoots;
            if (WornCloak != null) yield return WornCloak;
        }

        public Item PieceIn(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Armor: return WornArmor;
                case ItemKind.Shield: return WornShield;
                case ItemKind.Helm: return WornHelm;
                case ItemKind.Gloves: return WornGloves;
                case ItemKind.Boots: return WornBoots;
                case ItemKind.Cloak: return WornCloak;
                default: return null;
            }
        }

        public void SetPiece(ItemKind kind, Item it)
        {
            switch (kind)
            {
                case ItemKind.Armor: WornArmor = it; break;
                case ItemKind.Shield: WornShield = it; break;
                case ItemKind.Helm: WornHelm = it; break;
                case ItemKind.Gloves: WornGloves = it; break;
                case ItemKind.Boots: WornBoots = it; break;
                case ItemKind.Cloak: WornCloak = it; break;
            }
        }

        /// <summary>Wears a piece in its slot, returning what it replaced to the pack. Reveals magic.</summary>
        public void Wear(Item it)
        {
            Inventory.Remove(it);
            var old = PieceIn(it.Def.Kind);
            if (old != null) Inventory.Add(old);
            SetPiece(it.Def.Kind, it);
            RefreshGear();
        }

        public void TakeOff(Item it)
        {
            if (!ReferenceEquals(PieceIn(it.Def.Kind), it)) return;
            SetPiece(it.Def.Kind, null);
            Inventory.Add(it);
            RefreshGear();
        }

        public bool IsWorn(Item it) => it != null && ReferenceEquals(PieceIn(it.Def.Kind), it);

        /// <summary>Sum of everything the wielded weapon and worn armour grant.</summary>
        public ItemMods Gear
        {
            get
            {
                var g = new ItemMods();
                if (Wielded != null) g.Add(Wielded.Mods);
                foreach (var it in WornPieces()) g.Add(it.Mods);
                foreach (string id in Mutated) { var mu = MutationTable.Find(id); if (mu != null) g.Add(mu.Mods); }
                g.Add(ArtifactSets.Bonus(this));
                g.Add(BuffMods);
                g.Add(AccessoryMods);
                return g;
            }
        }

        ItemMods _gearApplied;

        /// <summary>
        /// Call after any equipment change. Attribute bonuses are applied to the attribute fields as
        /// a difference from what was applied last time, so training and gear never overwrite each other.
        /// </summary>
        public void RefreshGear()
        {
            var g = Gear;
            Str += g.Str - _gearApplied.Str; Dex += g.Dex - _gearApplied.Dex; Con += g.Con - _gearApplied.Con;
            Int += g.Int - _gearApplied.Int; Wis += g.Wis - _gearApplied.Wis;
            _gearApplied = g;
            RecomputeMaxHP();
            RecomputeMaxMp();
            RecomputeMaxVigor();
        }

        public int ArmorClass()
        {
            int ac = AC;
            foreach (var piece in WornPieces()) ac -= piece.TotalAc;
            foreach (string id in Mutated) ac -= MutationTable.Find(id)?.Mods.Ac ?? 0;
            if (Rings[0] != null && RingKnown[0] && Rings[0].Name == "ring of protection") ac -= 3;
            if (Rings[1] != null && RingKnown[1] && Rings[1].Name == "ring of protection") ac -= 3;
            if (WardTurns > 0) ac -= 3;
            if (WornShield != null) ac -= 2 * PerkRank("shield-wall");
            if (WornArmor != null) ac -= PerkRank("aura");
            if (BuffTurns("stone-skin") > 0) ac -= 6;
            if (BuffTurns("ossify") > 0) ac -= 4;
            if (Buffs.Count > 0) ac -= BuffMods.Ac;
            ac -= AccessoryMods.Ac;
            int dexAdj = Dex >= 10 ? (Dex - 10) / 2 : -((10 - Dex) / 2);
            ac -= dexAdj;
            if (WornArmor != null && (WornArmor.Def.Flags & ItemFlags.Cursed) != 0) ac += 2;
            return ac;
        }

        public int Evasion()
        {
            int e = 2;
            e += (Dex - 10) / 2;
            e += Gear.Evasion;
            if (God == "sylk" && GodTier >= 1) e += 2;
            e += Races.Find(RaceId).EvasionBonus + SkillRanks.Rank(Skills[Skill.Dodging]) + 2 * PerkRank("lucky");
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
            amount += amount * 15 * PerkRank("quick-learner") / 100;
            Xp += amount;
            if (Trained) TrainXp += amount;
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
                RecomputeMaxMp();
                Mp = MpMax;
                RecomputeMaxVigor();
                Vigor = VigorMax;
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
            RecomputeMaxMp();
            return true;
        }

        public int XpForNext() => Math.Max(1, XpNext - Xp);

        public string LevelString => $"Dlvl {Level:00}, XP {Xp:XpForNext():X00}";

        public override string LongDescription =>
            $"{CharName}, the {Title} (level {Level}, {HP}/{MaxHP} HP, AC {ArmorClass()}, {XpForNext()} XP to level {Level + 1})";

        public string AttributeLine()
        {
            string sfrac = Str == 18 ? StrFrac.ToString("00") : "  ";
            return $"Str:{Str,2}/{sfrac} Dex:{Dex,2} Con:{Con,2} Int:{Int,2} Wis:{Wis,2} Cha:{Cha,2}";
        }

        public int CarryingCapacity() => 50 + Str * 15;

        public bool IsOverloaded() => WeightCarried() > CarryingCapacity();

        public string AlignmentString => Align.ToString();

        /// <summary>Trained mode: skills are bought, not earned by use. TrainXp is what has not been spent yet.</summary>
        public bool Trained;
        public int TrainXp;

        public int GainSkill(Skill s, int amount, bool bought = false)
        {
            if (Trained && !bought) return Skills[s];
            Skills[s] = Math.Min(Roles.Find(RoleId).CapFor(s), Skills[s] + amount);
            return Skills[s];
        }
    }
}
