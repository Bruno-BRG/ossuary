using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    public enum Panel
    {
        None,
        Inventory,
        Help,
        History,
        Discoveries,
        Choice,
        Travel,
        Character,
        Shop,
        Death,
        Win,
    }

    /// <summary>
    /// UI state as data. The renderer reads these fields and draws panels; the input
    /// layer mutates them. Nothing here touches UnityEngine, so the whole interface
    /// is testable headlessly.
    /// </summary>
    public enum TargetingMode { None, Look, Shoot, Dig, PickLock, Swap, Inspect }

    public sealed class UiState
    {
        public Panel Active = Panel.None;

        public TargetingMode Targeting = TargetingMode.None;
        public int TargetX, TargetY;

        public readonly ChoiceRequest Choice = new ChoiceRequest();
        public int ChoiceIndex;

        public bool TravelMode;
        public int TravelX, TravelY;

        public int ScrollOffset;
        public bool ShowMinimap = true;

        /// <summary>Cursor row inside the shop's stock list.</summary>
        public int ShopIndex;

        /// <summary>Which pane the world map is showing while travelling.</summary>
        public int TravelPage;

        public bool IsOpen => Active != Panel.None;
        public bool IsTargeting => Targeting != TargetingMode.None;
    }

    /// <summary>A "which item?" prompt. Modal: it blocks the map until answered or cancelled.</summary>
    public sealed class ChoiceRequest
    {
        public string Prompt;
        public readonly List<Item> Items = new List<Item>();
        public void Clear() { Prompt = null; Items.Clear(); }
        public bool Active => Prompt != null;
    }

    /// <summary>
    /// One-shot requests raised by Commands and drained by the HUD each frame.
    /// Using a queue instead of direct state means a command can fire "open this
    /// panel" without the simulation needing to know panels exist.
    /// </summary>
    public sealed class UiRequests
    {
        public bool Inventory;
        public bool Help;
        public bool HelpLong;
        public bool History;
        public bool Discoveries;
        public bool Character;
        public bool Travel;

        public void Clear()
        {
            Inventory = false; Help = false; HelpLong = false;
            History = false; Discoveries = false; Character = false; Travel = false;
        }
    }
}