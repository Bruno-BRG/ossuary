using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.Entities
{
    public enum Alignment
    {
        ChaoticGood = -7, NeutralGood = -3, LawfulGood = 3, Neutral = 0,
        ChaoticNeutral = -1, LawfulNeutral = 1, ChaoticEvil = -9, NeutralEvil = -5, LawfulEvil = 9
    }

    [Flags]
    public enum Ability
    {
        None = 0,
        Strength = 1 << 0,
        Dexterity = 1 << 1,
        Constitution = 1 << 2,
        Intelligence = 1 << 3,
        Wisdom = 1 << 4,
        Charisma = 1 << 5,
        All = Strength | Dexterity | Constitution | Intelligence | Wisdom | Charisma
    }

    /// <summary>Base for anything that occupies a cell and can be hit.</summary>
    public abstract class Actor
    {
        public string Name = "something";
        public char Glyph = 'x';
        public Alignment Align = Alignment.Neutral;

        public int X, Y;
        public int HP = 1;
        public int MaxHP = 1;
        public int Energy;          // action points
        public int Speed = 12;
        public int AC = 10;
        public int Level = 1;
        public int Xp = 0;
        public int Depth;

        public bool IsPlayer;
        public bool Asleep;
        public bool Confused;
        public bool Blinded;
        public bool Hallucinating;
        public bool WoundedLegs;
        public bool Invisible;
        public bool Stunned;
        public int SleepTurns;
        public int StunTurns;
        public int ConfusionTurns;
        public int BlindTurns;
        public int HallucinationTurns;
        public int PoisonResist;
        /// <summary>Burning (fire damage each turn), and soaked (cold and lightning hurt more, fire cannot catch).</summary>
        public int BurnTurns, WetTurns;
        public int SleepResist;
        public int Stealth;
        public bool Swallowed;
        public int SwallowedBy = -1;

        public readonly List<Item> Inventory = new List<Item>();
        /// <summary>Injuries on body parts (see <see cref="Bodies"/> and Game.Wounds.cs).</summary>
        public readonly List<Wound> Wounds = new List<Wound>();
        public Item Wielded;
        public Item WornArmor;
        public Item WornShield;
        public Item WornHelm;
        public Item WornGloves;
        public Item WornBoots;
        public Item WornCloak;
        public Item[] Rings = new Item[2];
        public Item Amulet;
        public bool[] RingKnown = new bool[2];

        public int XpKill;
        public bool IsDead => HP <= 0;
        public int NextEnergyValue => Energy <= 0 ? 1 : Energy;

        public virtual int VisionRadius => 8;

        /// <summary>The bare name, without an article: callers write "the {TheName}" themselves. Use <see cref="Obj"/>/<see cref="Subj"/> for whole phrases.</summary>
        public string TheName => IsPlayer ? Name : (Name.StartsWith("the ", StringComparison.Ordinal) ? Name.Substring(4) : Name);

        /// <summary>Named people (townsfolk) take no article.</summary>
        bool TakesArticle => !IsPlayer && !(this is Monster tm && tm.Townsperson);

        /// <summary>This actor as the object of a sentence: "you", "the jackal", "Dagny".</summary>
        public string Obj => IsPlayer ? "you" : TakesArticle ? "the " + TheName : TheName;

        /// <summary>This actor as the subject of a sentence: "You", "The jackal", "Dagny".</summary>
        public string Subj => IsPlayer ? "You" : TakesArticle ? "The " + TheName : TheName;

        public virtual string LongDescription
        {
            get
            {
                string hp = "";
                if (MaxHP > 0) hp = $", {HP}/{MaxHP} HP";
                return $"{Subj} (level {Level}{hp})";
            }
        }

        public int WeightCarried()
        {
            int w = 0;
            for (int i = 0; i < Inventory.Count; i++) w += Inventory[i].Weight * Inventory[i].Quantity;
            return w;
        }

        public int CountItemKind(ItemKind kind)
        {
            int n = 0;
            for (int i = 0; i < Inventory.Count; i++)
                if (Inventory[i].Def.Kind == kind) n += Inventory[i].Quantity;
            return n;
        }

        public Item FindFirst(string nameFragment)
        {
            for (int i = 0; i < Inventory.Count; i++)
                if (Inventory[i].Name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0) return Inventory[i];
            return null;
        }

        public Item RemoveFirst(string nameFragment)
        {
            Item it = FindFirst(nameFragment);
            if (it != null) Inventory.Remove(it);
            return it;
        }
    }
}
