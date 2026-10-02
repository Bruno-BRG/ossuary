using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>UI-facing entry points and simple state flags. Deliberately thin.</summary>
    public sealed partial class Game
    {
        public void Wait() { BeQuiet(); EndPlayerTurn(); }

        public void ToggleMinimap() => UiState.ShowMinimap = !UiState.ShowMinimap;

        public void PushInventory() => UiRequests.Inventory = true;
        public void PushHelp() => UiRequests.Help = true;
        public void PushHistory() => UiRequests.History = true;
        public void PushDiscoveries() => UiRequests.Discoveries = true;
        public void PushCharacter() => UiRequests.Character = true;
        public void PushTravelMode() => UiRequests.Travel = true;
        public void PushSettings() => UiRequests.Settings = true;

        public void PushChoice(string prompt, List<Item> items)
        {
            PendingChoice.Clear();
            PendingChoice.Prompt = prompt;
            PendingChoice.Items.AddRange(items);
        }

        public void PushTargeting(TargetingMode mode)
        {
            if (Map == null) return;
            UiState.Targeting = mode;
            UiState.TargetX = Player.X;
            UiState.TargetY = Player.Y;
        }

        /// <summary>
        /// Gold the player can actually spend. The Player.Gold field tracks the purse;
        /// this is the accessor the HUD uses so the arithmetic lives in one place.
        /// </summary>
        public int CarryingGold() => Player.Gold;

        public int RegionsSeen()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < World.W * World.H; i++)
            {
                if (World.Tiles[i].Discovered) seen.Add(World.RegionAt(i % World.W, i / World.W).Name);
            }
            return seen.Count;
        }

        /// <summary>Moves the targeting cursor, clamped to the visible area.</summary>
        public void NudgeTarget(int dx, int dy)
        {
            if (Map == null) return;
            UiState.TargetX = Math.Max(0, Math.Min(Map.W - 1, UiState.TargetX + dx));
            UiState.TargetY = Math.Max(0, Math.Min(Map.H - 1, UiState.TargetY + dy));
        }

        public void ResetTargetToPlayer()
        {
            if (Map == null) return;
            UiState.TargetX = Player.X;
            UiState.TargetY = Player.Y;
        }
    }
}