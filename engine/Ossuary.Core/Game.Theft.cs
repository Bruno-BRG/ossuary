using System;
using System.Collections.Generic;
using System.Linq;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Property crimes at the counters of a town: a good taken from a shop's stock unpaid (steal, F9) and a molotov thrown
    /// into a shop (burn, F10). The people who can see the hero witness it: a witnessed crime is a bounty by its value, and the
    /// Watch remembers. An unwitnessed one is only in the ledger. A burnt shop stays burnt (Burn, Game.Raids.cs).
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>The shop whose counter the hero stands beside on the street, with goods on it; null when there is none.</summary>
        Shop CounterBeside()
        {
            if (Mode != GameMode.TownMap || Town == null || TownZ != 0) return null;
            foreach (var s in Town.Shops)
                if (Math.Max(Math.Abs(s.X - Player.X), Math.Abs(s.Y - Player.Y)) <= 1 && s.Stock.Exists(i => i.Def.Kind != ItemKind.Gold))
                    return s;
            return null;
        }

        /// <summary>The building whose counter the hero stands beside on the street, and which is still standing; null when there is none.</summary>
        Building BuildingBeside()
        {
            if (Mode != GameMode.TownMap || Town == null || TownZ != 0) return null;
            foreach (var b in Town.Buildings)
                if (!b.Burned && b.CounterX >= 0 && Math.Max(Math.Abs(b.CounterX - Player.X), Math.Abs(b.CounterY - Player.Y)) <= 1) return b;
            return null;
        }

        /// <summary>F9 beside a counter: the first good on it goes into the pack, unpaid. A witness makes it a bounty by its value.</summary>
        public bool StealFromCounter()
        {
            var shop = CounterBeside();
            if (shop == null)
            {
                Say(TownText.L("Stand beside a counter to steal from it.", "Fique ao lado de um balcão para roubar dele."), MessageKind.Info);
                return false;
            }
            var item = shop.Stock.Find(i => i.Def.Kind != ItemKind.Gold);
            int value = Math.Max(1, StackValue(item));
            var seen = shop.Keeper != null ? WitnessesOf(shop.Keeper, true) : new List<Monster>();
            shop.Stock.Remove(item);
            Player.Inventory.Add(item);
            if (seen.Count > 0)
            {
                AddBounty(Math.Max(50, value * 2), TownText.L("You stole from a counter in front of people.", "Você roubou de um balcão na frente de gente."));
                RaiseAlarm(seen);
                AddRep(Houses.Watch, -6, "theft in view of the town");
                RecordDeed(Deed.Stole, shop.Name, Math.Max(2, Math.Min(5, value / 40)));
                Say(TownText.L($"{seen.Count} saw it. The Watch will hear of it.", $"{seen.Count} viram. A Guarda vai ficar sabendo."), MessageKind.Warn);
            }
            else
            {
                RecordDeed(Deed.Stole, shop.Name, 1);
                Say(TownText.L("No one saw it. The goods are yours, and the counter will not forget a missing thing.",
                    "Ninguém viu. A mercadoria é sua, e o balcão não esquece uma peça que sumiu."), MessageKind.Neutral);
            }
            EndPlayerTurn();
            return true;
        }

        /// <summary>F10 beside a counter, with a molotov: the building burns for good. A witnessed fire is a bounty by what burned.</summary>
        public bool BurnCounter()
        {
            var b = BuildingBeside();
            if (b == null)
            {
                Say(TownText.L("Stand beside a counter to set it alight.", "Fique ao lado de um balcão para pôr fogo nele."), MessageKind.Info);
                return false;
            }
            if (b.Kind == BuildingKind.Temple || b.Kind == BuildingKind.Guild)
            {
                Say(TownText.L("The stone does not burn, and the priests and the Guild will say so.", "A pedra não queima, e os padres e a Guilda vão dizer isso."), MessageKind.Warn);
                return false;
            }
            if (Player.FindFirst("molotov") == null)
            {
                Say(TownText.L("You need a molotov to set a counter alight.", "Você precisa de um coquetel molotov para pôr fogo num balcão."), MessageKind.Info);
                return false;
            }
            TakeItem("molotov");
            int stock = b.Shop != null ? b.Shop.Stock.Sum(i => StackValue(i)) : 0;
            var seen = b.Keeper != null ? WitnessesOf(b.Keeper, true) : new List<Monster>();
            Burn(b);
            if (seen.Count > 0)
            {
                AddBounty(500 + stock, TownText.L("You set a shop alight in front of people.", "Você pôs uma loja em chamas na frente de gente."));
                RaiseAlarm(seen);
                AddRep(Houses.Watch, -15, "arson in the town");
            }
            RecordDeed(Deed.Burnt, b.Name, seen.Count > 0 ? 5 : 3);
            Say(TownText.L("The counter goes up in a sheet of flame. Only ash and a blackened counter are left.",
                "O balcão vira uma folha de fogo. Só restam cinzas e um balcão enegrecido."), MessageKind.Warn);
            EndPlayerTurn();
            return true;
        }

        /// <summary>A lock pick opens the cell door while the Watch is taking the hero in: out, the bounty doubles, and the Watch remembers the lock.</summary>
        public void LockpickOut()
        {
            // Twice what the Watch here holds, echo included: an echo-only bounty is topped up in this region's own book.
            int bounty = BountyHere(), own = Bounties.TryGetValue(RegionHere(), out int here) ? here : 0;
            if (bounty > 0) AddBounty(2 * bounty - own, TownText.L("You broke out of the cells.", "Você fugiu das celas."));
            AddRep(Houses.Watch, -10, "a broken cell");
            RecordDeed(Deed.Escaped, RegionHere(), 3);
            Say(TownText.L("The lock gives under the pick. You are out in the alley, and the Watch will remember the lock.",
                "A fechadura cede à gazua. Você está na viela, e a Guarda vai lembrar da fechadura."), MessageKind.Warn);
        }
    }
}
