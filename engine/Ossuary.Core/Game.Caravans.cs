using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Caravans on the road. The caravan met on the road is the week's caravan of the nearest town: ride with it as a guard and it
    /// arrives (that town's goods are cheap this week, and it pays you), rob it and the road is raided (that town goes short, and
    /// the Watch wants you). What the hero decided is stored per town and week; everything else stays a function of the seed.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>What the hero made of a town's caravan this week ("Town|week").</summary>
        public readonly Dictionary<string, CaravanNews> CaravanFate = new Dictionary<string, CaravanNews>();

        /// <summary>The town whose caravan is on this stretch of road: the nearest one.</summary>
        public string CaravanTown()
        {
            if (World == null) return null;
            string best = null; int bestD = int.MaxValue;
            for (int i = 0; i < World.Tiles.Length; i++)
            {
                var t = World.Tiles[i];
                if (t.Feature != OverworldFeature.Town || string.IsNullOrEmpty(t.Name)) continue;
                int d = Math.Max(Math.Abs(i % World.W - World.PlayerX), Math.Abs(i / World.W - World.PlayerY));
                if (d < bestD) { bestD = d; best = t.Name; }
            }
            return best;
        }

        string CaravanKey(string town) => town + "|" + (World.Day / 7);

        public int EscortPay => 40 + Player.Level * 5;

        void CaravanEscort()
        {
            string town = CaravanTown();
            if (town == null) return;
            CaravanFate[CaravanKey(town)] = CaravanNews.Arrived;
            PassHours(6);
            Player.Gold += EscortPay;
            AddRep(Houses.Guild, 3, "guarding a caravan");
            RecordDeed(Deed.Helped, "a caravan", 2);
            Tell($"Six hours at the tailboard with a spear across your knees. The carts reach {town}, and the drivers pay {EscortPay} gold.", MessageKind.Good);
            if (Rng.Chance(40))
            {
                var m = new Monster(Bestiary.Find(Player.Level < 4 ? "kobold" : "orc"), Rng);
                BeginRoadFight(m, "Bandits come out of the ditch at dusk");
            }
        }

        void CaravanRob()
        {
            string town = CaravanTown();
            if (town == null) return;
            CaravanFate[CaravanKey(town)] = CaravanNews.Raided;
            int gold = Rng.Range(60, 141) + Player.Level * 5;
            Player.Gold += gold;
            GiveItem("food ration", 2);
            AddRep(Houses.Guild, -6, "robbing a caravan");
            AddRep(Houses.Watch, -5, null);
            RecordDeed(Deed.Struck, "a caravan", 3);
            AddBounty(150, "You robbed a caravan.");
            Tell($"The drivers run. You take {gold} gold and what you can carry. {town} will go short this week.", MessageKind.Warn);
        }
    }
}
