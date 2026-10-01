using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>Overworld travel, encounters and towns.</summary>
    public sealed partial class Game
    {
        public void OverworldMove(int dx, int dy)
        {
            if (ActiveEncounter)
            {
                Say("You cannot walk away from it. Attack (k) or flee (<).", MessageKind.Warn);
                return;
            }
            int nx = World.PlayerX + dx, ny = World.PlayerY + dy;
            if (!World.InBounds(nx, ny)) return;

            World.PlayerX = nx;
            World.PlayerY = ny;
            World.CurrentRegionName = World.RegionAt(nx, ny).Name;
            World.AdvanceTime(1);
            World.Discover(5);

            var t = World.Get(nx, ny);
            if (t.Feature == OverworldFeature.Town) EnterTown(t.Name ?? "a town");
            else if (t.Feature == OverworldFeature.Dungeon) EnterDungeonFromOverworld(nx, ny);
            else CheckOverworldEncounter();
        }

        void CheckOverworldEncounter()
        {
            if (ActiveEncounter) return;
            var region = World.RegionAt(World.PlayerX, World.PlayerY);
            int chance = region.Danger / 5 + (World.IsNight ? 6 : 0);
            if (chance <= 0) return;
            if (!Rng.Chance(chance)) return;

            int depth = Math.Max(1, region.Depth / 3);
            var def = Bestiary.RandomForDepth(depth, Rng, out _);
            if (!def.HasValue) return;

            var m = new Monster(def.Value, Rng);
            EncounterMonster = m;
            EncounterX = World.PlayerX;
            EncounterY = World.PlayerY;
            ActiveEncounter = true;
            Say($"A {m.Name} blocks your path!", MessageKind.Bad);
            Say($"Attack it with k, or flee with <. It is {m.ThreatLabel()}.", MessageKind.Warn);
        }

        public void FleeEncounter()
        {
            if (!ActiveEncounter) return;
            EncounterMonster = null;
            ActiveEncounter = false;
            World.AdvanceTime(1);
            Say("You break away and run.", MessageKind.Neutral);
        }

        public void AttackEncounter()
        {
            if (!ActiveEncounter || EncounterMonster == null) return;
            var m = EncounterMonster;

            bool crit;
            var res = Battles.PlayerMelee(Player, m, Rng, out crit);
            Say(res.Message, res.Killed ? MessageKind.Kill : MessageKind.Combat);

            if (res.Killed)
            {
                foreach (var it in m.Inventory)
                {
                    if (it.Def.Kind == ItemKind.Gold) Player.Gold += it.Quantity;
                    else Player.Inventory.Add(it);
                }
                Player.Kills++;
                Player.AddXp(m.XpKill);
                Player.GainSkill(Skill.Combat, 3);
                ActiveEncounter = false;
                EncounterMonster = null;
                World.AdvanceTime(1);
                return;
            }

            var back = Battles.MeleeAttack(m, Player, Rng);
            Say(back.Message, back.Killed ? MessageKind.Death : MessageKind.Combat);
            World.AdvanceTime(1);
            CheckDeath();
        }

        public void TravelCursorTo(int x, int y)
        {
            if (!World.InBounds(x, y)) return;
            UiState.TravelX = x;
            UiState.TravelY = y;
        }

        public void CommitTravel(int x, int y)
        {
            if (!World.InBounds(x, y)) return;
            var from = World.RegionAt(World.PlayerX, World.PlayerY);
            var to = World.RegionAt(x, y);
            int dist = Pathfinder.Manhattan(World.PlayerX, World.PlayerY, x, y);
            int hours = Math.Max(1, dist / 6);

            World.PlayerX = x;
            World.PlayerY = y;
            World.CurrentRegionName = to.Name;
            World.AdvanceTime(hours);
            World.Discover(Math.Max(5, dist / 3));

            if (from.Name != to.Name) Say($"You travel {hours} hours into {to.Name}.", MessageKind.Narrative);
            else Say($"You walk for {hours} hours.", MessageKind.Neutral);

            var t = World.Get(x, y);
            if (t.Feature == OverworldFeature.Town) EnterTown(t.Name ?? "a town");
            else if (t.Feature == OverworldFeature.Dungeon) EnterDungeonFromOverworld(x, y);
            else CheckOverworldEncounter();
        }

        public void EnterTown(string name)
        {
            Town = TownGen.Generate(name, Rng, 1);
            TownMap = Town.Map;
            Mode = GameMode.TownMap;
            Map = TownMap;
            Player.X = Town.EntryX;
            Player.Y = Town.EntryY;
            Map.ClearVisibility();
            UpdateFov();
            Say($"You arrive in {name}. Some {Town.Population} people live here.", MessageKind.Narrative);
            foreach (var b in Town.Buildings) Say("  " + b, MessageKind.Info);
        }

        public void LeaveTown()
        {
            Mode = GameMode.Overworld;
            Town = null;
            TownMap = null;
            Map = null;
            Say("You leave town and return to the road.", MessageKind.Neutral);
        }

        // ------------------------------------------------------------- shops

        public void OpenShop(Shop shop)
        {
            CurrentShop = shop;
            InShop = true;
            ShopName = shop.Name;
            Say($"You step into {shop.Name}.", MessageKind.Neutral);
            foreach (var it in shop.Stock) Say($"  {it.Name} - {ShopPrice(shop, it)} gold", MessageKind.Info);
        }

        public void CloseShop()
        {
            InShop = false;
            CurrentShop = null;
            Say("You step back outside.", MessageKind.Neutral);
        }

        public int ShopPrice(Shop shop, Item item)
        {
            int basePrice = item.Def.Cost;
            if (basePrice <= 0) basePrice = 5;
            int markup = 100 + shop.Gold / 60;
            int price = basePrice * markup / 100;
            if (item.Def.Kind == ItemKind.Gold) price = 1;
            return Math.Max(1, price);
        }

        public bool BuyFromShop(Shop shop, Item item)
        {
            int price = ShopPrice(shop, item);
            if (item.Def.Kind == ItemKind.Gold)
            {
                shop.Gold -= item.Quantity;
                if (shop.Gold < 0) { Say("The shopkeeper cannot afford that much gold."); return false; }
                Player.Gold += item.Quantity;
                Say($"You buy {item.Quantity} gold pieces for {price} gold.");
                shop.Stock.Remove(item);
                return true;
            }
            if (Player.Gold < price) { Say("You cannot afford that."); return false; }
            Player.Gold -= price;
            shop.Gold += price;
            Player.Inventory.Add(item);
            Say($"You buy {item.Name} for {price} gold.", MessageKind.Good);
            shop.Stock.Remove(item);
            return true;
        }

        public bool SellToShop(Shop shop, Item item)
        {
            if (item.Def.Kind == ItemKind.Gold)
            {
                Player.Gold += item.Quantity;
                Say($"You hand over {item.Quantity} gold pieces.");
                return true;
            }
            int value = Math.Max(1, item.Def.Cost / 2);
            if (shop.Gold < value) { Say("The shopkeeper cannot afford that."); return false; }
            Player.Inventory.Remove(item);
            shop.Gold += value;
            Player.Gold += value;
            shop.Stock.Add(item);
            Say($"You sell {item.Name} for {value} gold.", MessageKind.Good);
            return true;
        }
    }
}