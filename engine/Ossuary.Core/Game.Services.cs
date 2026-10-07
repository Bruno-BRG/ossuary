using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Shop services for items: a sage recharges a spent wand (each recharge after the first may blow it apart), a priest lifts a
    /// curse, and the cursed amulets that will not come off take their price every so often.
    /// </summary>
    public sealed partial class Game
    {
        public const string RechargePrompt = "Recharge which wand?";

        // ---------------------------------------------------------------- recharging

        List<Item> SpentWands() => Player.Inventory.FindAll(it => it.Def.Kind == ItemKind.Wand && it.ChargesUsed > 0);

        public int RechargePrice(Item w) => Haggle(40 + 15 * w.ChargesUsed + 40 * w.Recharged, Houses.Guild);

        /// <summary>Chance in a hundred that a recharge blows the wand apart: none the first time, a third more each time after.</summary>
        public static int RechargeRisk(Item w) => Math.Min(90, 30 * w.Recharged);

        public bool RechargeWand(Item w)
        {
            if (w == null || w.Def.Kind != ItemKind.Wand || w.ChargesUsed <= 0) return false;
            if (!Pay(RechargePrice(w))) return false;
            if (Rng.Range(0, 100) < RechargeRisk(w))
            {
                Player.Inventory.Remove(w);
                int dmg = Rng.Range(2, 9);
                Player.HP = Math.Max(1, Player.HP - dmg);
                Tell($"The {w.Name} cannot hold any more. It splits with a crack and a flash. (-{dmg})", MessageKind.Bad);
                return true;
            }
            w.Recharged++;
            w.ChargesUsed = 0;
            if (w.Recharged > 1) w.Charges = Math.Max(1, w.Charges - 1);
            Learn(w, false);
            Tell($"The sage hums over the {w.Name} until it is warm. ({w.RemainingCharges} charges)", MessageKind.Good);
            return true;
        }

        // ---------------------------------------------------------------- curses

        static bool IsCursed(Item it) => it != null && (it.Def.Flags & ItemFlags.Cursed) != 0;

        /// <summary>The cursed things the hero has on: they are what a priest can free.</summary>
        List<Item> CursedWorn()
        {
            var list = new List<Item>();
            if (IsCursed(Player.Amulet)) list.Add(Player.Amulet);
            if (IsCursed(Player.Wielded)) list.Add(Player.Wielded);
            foreach (var piece in Player.WornPieces()) if (IsCursed(piece)) list.Add(piece);
            for (int i = 0; i < 2; i++) if (IsCursed(Player.Rings[i])) list.Add(Player.Rings[i]);
            return list;
        }

        public int LiftCursePrice => Haggle(80 + 10 * Player.Level, Houses.Temple);

        public void LiftCurses()
        {
            var cursed = CursedWorn();
            foreach (var it in cursed)
            {
                it.Def.Flags = (it.Def.Flags & ~ItemFlags.Cursed) | ItemFlags.Uncursed;
                it.Identified = true;
            }
            AddRep(Houses.Temple, 1, null);
            Tell(cursed.Count > 0 ? "The priest says the old words, and something lets go of you." : "There is no curse on you.", MessageKind.Good);
        }

        /// <summary>A cursed amulet will not leave the neck. True (and said) when it refuses.</summary>
        public bool AmuletStuck()
        {
            if (!IsCursed(Player.Amulet)) return false;
            Player.Amulet.Identified = true;
            Say("The amulet will not come off. It is cursed: a priest can lift it.", MessageKind.Bad);
            return true;
        }

        /// <summary>What the cursed amulets take, every few turns.</summary>
        void CursedAmuletTick()
        {
            var a = Player.Amulet;
            if (a == null) return;
            switch (a.Def.Name)
            {
                case "amulet of the leech":
                    if (Turn % 20 == 0 && Player.HP > 1) { Player.HP--; Say("The amulet drinks a little of you.", MessageKind.Bad); }
                    break;
                case "amulet of restless sleep":
                    if (Turn % 80 == 40 && !HostileInView() && Rng.Chance(35)) { Player.SleepTurns = Math.Max(Player.SleepTurns, 3); Say("Your eyes close by themselves.", MessageKind.Bad); }
                    break;
            }
        }

        /// <summary>Hunger the hero's gear adds per turn.</summary>
        int GearHunger() => Player.Amulet != null && Player.Amulet.Def.Name == "amulet of the hungry dead" ? 1 : 0;

        // ---------------------------------------------------------------- service rows

        void AddItemServiceRows(Action<string, string, int, bool> add)
        {
            var s = TalkBuilding?.Services ?? Service.None;
            if ((s & Service.Appraise) != 0)
            {
                var wands = SpentWands();
                add("recharge", "Recharge a wand", wands.Count > 0 ? RechargePrice(wands[0]) : 0, wands.Count > 0);
            }
            if ((s & Service.Cure) != 0) add("lift", "Lift a curse", LiftCursePrice, CursedWorn().Count > 0);
            if (TalkBuilding?.Kind == BuildingKind.Library) add("chronicle", "Read the chronicles", 0, true);
        }

        bool ItemServiceAction(string id)
        {
            switch (id)
            {
                case "recharge":
                    PushChoice(RechargePrompt, SpentWands());
                    UiState.ChoiceIndex = 0;
                    return true;
                case "lift":
                    if (CursedWorn().Count == 0 || !Pay(LiftCursePrice)) return true;
                    LiftCurses();
                    return true;
                case "chronicle":
                    {
                        var (en, pt, key) = Ossuary.Core.World.History.Entry(Rng.Seed, _chronicleRead++);
                        LearnLegend(key);
                        Tell(TownText.L(en, pt), MessageKind.Narrative);
                        return true;
                    }
            }
            return false;
        }

        /// <summary>How far into the library's chronicles the hero has read.</summary>
        public int _chronicleRead;
    }
}
