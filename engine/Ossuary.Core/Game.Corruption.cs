using System;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// The Ossuary gets into the body. Corruption rises from tainted water, the Amulet, necromancy and the
    /// wrong potion; each 20 points a mutation takes hold. It can be bled off at a temple. Everything here is
    /// plain arithmetic over the game's own Rng, so a replay grows the same extra ribs.
    /// </summary>
    public sealed partial class Game
    {
        public const int CorruptionStep = 20, CorruptionMax = 100;

        /// <summary>Raises corruption and lets any mutations that were crossed take hold. Returns how many did.</summary>
        public int AddCorruption(int amount, string cause = null)
        {
            if (amount <= 0) return 0;
            var p = Player;
            int before = p.Corruption;
            p.Corruption = Math.Min(CorruptionMax, p.Corruption + amount);
            if (cause != null && p.Corruption > before) Say(cause, MessageKind.Warn);
            int gained = 0;
            for (int level = before / CorruptionStep + 1; level <= p.Corruption / CorruptionStep; level++)
                if (GainMutation()) gained++;
            return gained;
        }

        /// <summary>One new mutation, chosen at random. False when the hero already has them all.</summary>
        public bool GainMutation()
        {
            var m = MutationTable.Pick(Rng, Player.Mutated);
            if (m == null) return false;
            Player.Mutated.Add(m.Id);
            Player.RefreshGear();
            if (m.Sight > 0) UpdateFov();
            Say($"Your body twists: {m.Name}. {m.Blurb}", m.Kind == MutationKind.Bane ? MessageKind.Bad : m.Kind == MutationKind.Boon ? MessageKind.Good : MessageKind.Warn);
            return true;
        }

        public int MutationSight() { int s = 0; foreach (string id in Player.Mutated) s += MutationTable.Find(id)?.Sight ?? 0; return s; }
        public int MutationHunger() { int s = 0; foreach (string id in Player.Mutated) s += MutationTable.Find(id)?.Hunger ?? 0; return s; }
        public int MutationNoise() { int s = 0; foreach (string id in Player.Mutated) s += MutationTable.Find(id)?.Noise ?? 0; return s; }

        /// <summary>The Amulet gnaws at whoever carries it (a point every 40 turns); relics do the same, every 25.</summary>
        void AmuletCorrupts()
        {
            if (Turn % 40 == 0 && HasAmulet()) AddCorruption(1, null);
            if (Turn % 25 == 0)
            {
                int relics = WornRelics();
                if (relics > 0) AddCorruption(relics, null);
            }
        }

        /// <summary>Corrupting artifacts the hero has on (wielded or worn).</summary>
        public int WornRelics()
        {
            int n = 0;
            if (Player.Wielded != null && Artifacts.Find(Player.Wielded.ArtifactId)?.Corrupts == true) n++;
            foreach (var piece in Player.WornPieces()) if (Artifacts.Find(piece.ArtifactId)?.Corrupts == true) n++;
            return n;
        }

        // ---------------------------------------------------------------- fountains

        /// <summary>Drinking at a dungeon fountain (Shift+E while standing on it). The water may be clean, bitter or tainted.</summary>
        public void DrinkFromFountain()
        {
            int roll = Rng.Range(0, 10);
            if (roll < 4)
            {
                Player.HP = Math.Min(Player.MaxHP, Player.HP + 3);
                Say("The fountain water is cold and clean.");
            }
            else if (roll < 7) Say("The water tastes of rust and nothing else.", MessageKind.Info);
            else if (roll < 9) AddCorruption(10, "The water is dark with something that was not water. You swallow it anyway.");
            else AddCorruption(20, "The water is thick and black. It burns going down, and it does not stop.");
            EndPlayerTurn();
        }

        // ---------------------------------------------------------------- the temple

        public int PurgePrice => 60 + Player.Corruption * 3;

        /// <summary>A priest bleeds the Ossuary out of you: 30 corruption, and the newest unwelcome mutation.</summary>
        public void PurgeCorruption()
        {
            var p = Player;
            p.Corruption = Math.Max(0, p.Corruption - 30);
            for (int i = p.Mutated.Count - 1; i >= 0; i--)
            {
                var m = MutationTable.Find(p.Mutated[i]);
                if (m == null || m.Kind == MutationKind.Boon) continue;
                p.Mutated.RemoveAt(i);
                p.RefreshGear();
                Tell($"The priest draws out {m.Name} like a splinter.", MessageKind.Good);
                return;
            }
            Tell("The priest drains the worst of it. Nothing unwelcome is left in you.", MessageKind.Good);
        }
    }
}
