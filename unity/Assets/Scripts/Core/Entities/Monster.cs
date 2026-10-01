using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.Entities
{
    public enum AiKind
    {
        Walk,       // wanders
        Hunt,       // actively chases when it sees you
        Guard,      // stays near its home tile
        Coward,     // flees when hurt
        Ambush,     // waits until you are close, then strikes
        Sniper,     // keeps distance, throws
        Territorial,// hunts only inside its territory
        Predator,   // fast, relentless hunter
    }

    public enum AttackKind
    {
        Bite, Claw, ClawOrBite, Pierce, Hit, Kick, Butt, PierceOrHit, HitOrClaw,
        Explode, Grab, Strangle, PierceOrClaw, Touch, Stun, Drain
    }

    public struct MonsterDef
    {
        public string Name;
        public char Glyph;
        public int Color;            // hex
        public int Level;            // difficulty rating
        public int HP;
        public int AC;
        public int Speed;
        public int Weight;
        public int Nutrient;
        public int DepthMin, DepthMax;
        public Alignment Align;
        public AiKind Ai;
        public AttackKind[] Attacks;
        public int[] DmgDice;
        public int[] DmgSides;
        public int[] ToHit;
        public int Vision;
        public int ArmorClassMod;
        public int CorpseValue;
        public ItemDef[] Carries;
        public int[] CarryWeights;   // parallel to Carries
        public bool Regenerates;
        public bool Mindless;
        public bool Skeleton;
        public bool Undead;
        public bool Explodes;
        public bool Flys;
        public int Difficulty;       // for spawn tables
    }

    /// <summary>A living (or once-living) thing in the dungeon.</summary>
    public sealed class Monster : Actor
    {
        public MonsterDef Def;
        public int HomeX, HomeY;
        public int Alert;
        public bool Dormant;
        public long Uid;
        public bool IsPriest;
        public bool IsGuard;
        public bool Unique;

        public Monster(MonsterDef def, Rng rng)
        {
            Def = def;
            Name = def.Name;
            Glyph = def.Glyph;
            Align = def.Align;
            Level = def.Level;
            AC = def.AC;
            Speed = def.Speed;
            Energy = rng.Range(0, 12);
            XpKill = Math.Max(1, def.Level * def.Level + def.Difficulty);
            RollStats(rng);
        }

        void RollStats(Rng rng)
        {
            // Difficulty rating drives a physical stat budget, NetHack style:
            // AC eats points first, then HP, so armoured monsters are not also tanks.
            int budget = Def.Level * 4 + Def.Difficulty;
            AC = Math.Max(0, Def.AC);
            int acSpend = AC > 10 ? (AC - 10) : 0;
            budget -= acSpend;
            MaxHP = Math.Max(1, Def.HP + budget / 2);
            HP = MaxHP;
            Align = Def.Align;
        }

        public int Nutrient => Def.Nutrient;
        public int XpValue => XpKill;

        public override int VisionRadius => Def.Vision;

        public override string LongDescription
        {
            get
            {
                string s = TheName;
                if (Def.Undead) s += " of the dead";
                s += $", level {Level} ({Def.HP} HP, AC {AC})";
                return s;
            }
        }

        public bool IsThreat => !Dormant;

        public string ThreatLabel()
        {
            if (Def.Level == 0) return "peaceful";
            if (Def.Level <= 2) return "weak";
            if (Def.Level <= 5) return "somewhat dangerous";
            if (Def.Level <= 9) return "dangerous";
            if (Def.Level <= 13) return "very dangerous";
            return "extremely dangerous";
        }

        public bool HasAttack(AttackKind a)
        {
            if (Def.Attacks == null) return false;
            for (int i = 0; i < Def.Attacks.Length; i++) if (Def.Attacks[i] == a) return true;
            return false;
        }
    }
}
