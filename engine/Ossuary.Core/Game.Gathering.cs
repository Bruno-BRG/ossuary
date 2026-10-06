using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Gathering and music: where the raw goods of crafting come from, and the musician's trade. Butcher a carcass
    /// underfoot for meat and hide; on the road, spend two hours taking what the land gives (wood with an axe, ore with
    /// a pick, fish with a rod, herbs, flax, barley and honey by hand); dig in the Mines and a vein may break loose.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Puts goods in the pack, on an existing stack when there is one, and says so.</summary>
        public void Receive(string name, int qty)
        {
            if (qty <= 0 || !Trades.TryDef(name, out var def)) return;
            Item stack = null;
            if (def.Kind == ItemKind.Material || def.Kind == ItemKind.Food || def.Kind == ItemKind.Rock)
                foreach (var it in Player.Inventory) if (it.Def.Name == name) { stack = it; break; }
            if (stack != null) stack.Quantity += qty;
            else Player.Inventory.Add(new Item(def, Rng, NextUid()) { Identified = true, Quantity = qty });
            Say(qty > 1 ? $"You gather {name} (x{qty})." : $"You gather {name}.", MessageKind.Good);
        }

        bool Carries(Func<Item, bool> match) => Find(Player, match) != null || (Player.Wielded != null && match(Player.Wielded));

        /// <summary>The gather verb.</summary>
        public void Gather()
        {
            if (Mode == GameMode.Overworld) { GatherOutdoors(); return; }
            if (Map == null) return;
            var pile = GroundItems.At(Map.Number, Player.X, Player.Y);
            int idx = -1; bool bones = false;
            if (pile != null)
                for (int i = 0; i < pile.Count; i++)
                {
                    if (pile[i].Def.Kind != ItemKind.Corpse) continue;
                    if (IsRemains(pile[i])) { bones = true; continue; }
                    idx = i; break;
                }
            if (idx < 0)
            {
                if (bones) Say("Only bones are left here. They serve as they are.", MessageKind.Info);
                else if (Mode == GameMode.TownMap) Say("There is nothing to gather in the streets. The shops sell raw goods.", MessageKind.Info);
                else Say("There is nothing here to gather. Stand on a carcass, or gather on the road.", MessageKind.Info);
                return;
            }
            var carcass = GroundItems.Take(Map.Number, Player.X, Player.Y, idx);
            bool small = carcass.Def.Name == "small corpse" || carcass.Def.Weight < 30;
            Say("You butcher the carcass.", MessageKind.Neutral);
            Receive("raw meat", small ? 1 : 1 + Rng.Range(0, 2) + TradeRank("cook") / 2);
            if (!small) Receive("raw hide", 1 + (TradeRank("leatherworker") >= 2 ? 1 : 0));
            GainTrade("cook", 2);
            if (!small) GainTrade("leatherworker", 2);
            Map.Version++;
            EndPlayerTurn();
        }

        void GatherOutdoors()
        {
            if (World == null) return;
            if (ActiveEncounter) { Say("Not with a foe in front of you.", MessageKind.Warn); return; }
            var t = World.Get(World.PlayerX, World.PlayerY);
            int rank = TradeRank("forager");
            var got = new List<(string, int)>();
            bool ore = false;
            switch (t.Terrain)
            {
                case OverworldTerrain.Forest:
                    if (Carries(i => i.Def.Kind == ItemKind.Weapon && i.Def.Class == ItemClass.Axe)) got.Add(("log", 1 + Rng.Range(0, 2) + rank / 2));
                    else Say("You need an axe to fell trees. You take what lies on the ground.", MessageKind.Info);
                    if (Rng.Chance(50)) got.Add(("healing herb", 1 + rank / 2));
                    if (Rng.Chance(20 + 5 * rank)) got.Add(("honey", 1));
                    break;
                case OverworldTerrain.Grass: case OverworldTerrain.Road:
                    got.Add(("flax", 1 + Rng.Range(0, 2) + rank / 2));
                    if (Rng.Chance(45)) got.Add(("barley", 1 + Rng.Range(0, 2)));
                    if (Rng.Chance(25)) got.Add(("healing herb", 1));
                    if (Rng.Chance(8 + 4 * rank)) got.Add(("swiftroot", 1));
                    break;
                case OverworldTerrain.Hills: case OverworldTerrain.Mountain:
                    if (Carries(i => i.Def.Name == "pick-axe"))
                    {
                        got.Add((Materials.OreAt(t.Terrain == OverworldTerrain.Mountain ? 5 : 2, Rng.Range(0, 100)).Name, 1 + TradeRank("miner") / 2));
                        ore = true;
                    }
                    else Say("You need a pick-axe to break rock.", MessageKind.Info);
                    if (Rng.Chance(15 + 3 * rank)) got.Add(("swiftroot", 1));
                    break;
                case OverworldTerrain.Swamp:
                    got.Add(("nightshade", 1 + Rng.Range(0, 2)));
                    if (Rng.Chance(30)) got.Add(("healing herb", 1));
                    break;
                case OverworldTerrain.Water: case OverworldTerrain.Shallow: case OverworldTerrain.DeepWater: case OverworldTerrain.Sand:
                    if (Carries(i => i.Def.Name == "fishing rod")) got.Add(("raw fish", 1 + Rng.Range(0, 2) + rank / 2));
                    else Say("You need a fishing rod to fish.", MessageKind.Info);
                    break;
                case OverworldTerrain.Ruins:
                    if (Rng.Chance(35)) got.Add(("copper ore", 1));
                    break;
                default:
                    Say("Little grows here.", MessageKind.Info);
                    break;
            }
            World.AdvanceTime(2);
            if (got.Count == 0) Say("You spend two hours searching and find nothing of use.", MessageKind.Info);
            else Say("You spend two hours gathering.", MessageKind.Neutral);
            foreach (var (name, n) in got) Receive(name, n);
            GainTrade("forager", got.Count > 0 ? 2 : 1);
            if (ore) GainTrade("miner", 3);
            CheckOverworldEncounter();
        }

        /// <summary>After digging through rock: a vein may break loose, likely in the Mines, rarely anywhere else.</summary>
        void MineVein()
        {
            if (Map == null) return;
            bool mines = Map.BranchName == "The Mines of Dwarfdeep";
            if (!Rng.Chance((mines ? 30 : 6) + 5 * TradeRank("miner"))) { GainTrade("miner", 1); return; }
            Say("A vein of ore breaks loose.", MessageKind.Good);
            Receive(Materials.OreAt(mines ? Math.Max(1, Depth) : 1, Rng.Range(0, 100)).Name, 1);
            GainTrade("miner", 3);
        }

        // ---------------------------------------------------------------- music

        /// <summary>
        /// Plays an instrument. In a town the street pays once a day (more for a better player, a better instrument and a
        /// winning face); below, a tune can lull what listens to sleep (never the mindless or the dead); on the road it is practice.
        /// </summary>
        public void PlayInstrument(Item it)
        {
            if (it == null) return;
            int rank = TradeRank("musician");
            string name = it.Def.Name;
            MakeNoise(4);
            if (Mode == GameMode.TownMap)
            {
                if (Player.LastBuskDay == Today)
                {
                    Say("You play for a while. The street has given what it will today.", MessageKind.Info);
                    GainTrade("musician", 1);
                }
                else
                {
                    Player.LastBuskDay = Today;
                    int coins = 2 + rank * 4 + Math.Max(0, Player.Cha - 10) + it.Def.Cost / 40 + Rng.Range(0, 6);
                    Player.Gold += coins;
                    Say($"You play the {name}. Passers-by drop {coins} gold.", MessageKind.Good);
                    GainTrade("musician", 4);
                }
                EndPlayerTurn();
                return;
            }
            if (Mode == GameMode.Dungeon)
            {
                int calmed = 0;
                foreach (var m in Monsters)
                {
                    if (m.Ally || m.Def.Mindless || m.Def.Undead || m.Asleep) continue;
                    if (Pathfinder.Chebyshev(Player.X, Player.Y, m.X, m.Y) > 6 || !Map.IsVisible(m.X, m.Y)) continue;
                    if (!Rng.Chance(20 + 15 * rank)) continue;
                    m.Asleep = true; m.Alert = 0; calmed++;
                }
                if (calmed > 0) Say($"You play the {name}. {calmed} creature(s) sink into sleep.", MessageKind.Good);
                else Say($"You play the {name}. Nothing listens.", MessageKind.Info);
                GainTrade("musician", 3);
                EndPlayerTurn();
                return;
            }
            Say($"You play the {name} by the road.", MessageKind.Neutral);
            GainTrade("musician", 2);
            World?.AdvanceTime(1);
        }
    }
}

