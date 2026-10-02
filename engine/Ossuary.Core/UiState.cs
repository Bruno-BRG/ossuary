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
        Settings,
        Controls,
        Create,
        Spells,
        Abilities,
        Advance,
        Altar,
        Service,
        Runs,
        Achievements,
    }

    /// <summary>
    /// UI state as data. The renderer reads these fields and draws panels; the input
    /// layer mutates them. This layer is plain data, so the whole interface
    /// is testable headlessly.
    /// </summary>
    public enum TargetingMode { None, Look, Shoot, Dig, PickLock, Swap, Inspect, Cast, Ability }

    public sealed class UiState
    {
        public Panel Active = Panel.None;

        public TargetingMode Targeting = TargetingMode.None;
        public int TargetX, TargetY;
        /// <summary>The spell waiting on a target (TargetingMode.Cast).</summary>
        public string CastSpell;
        public string PendingAbility;
        /// <summary>Selected row in the spell list.</summary>
        public int SpellIndex;
        public int AbilityIndex;
        public int AltarIndex, AltarX, AltarY;
        public bool AltarConfirm;
        public int AdvanceIndex;

        public readonly ChoiceRequest Choice = new ChoiceRequest();
        public int ChoiceIndex;

        public bool TravelMode;
        public int TravelX, TravelY;

        public int ScrollOffset;
        public bool ShowMinimap = true;

        /// <summary>Cursor row inside the shop's stock list.</summary>
        public int ShopIndex;
        /// <summary>Cursor row inside a service menu (inn, temple, smithy, guild...).</summary>
        public int ServiceIndex;

        /// <summary>Selected row in the menu (an index into <see cref="MenuRows.All"/>).</summary>
        public int SettingsIndex;

        /// <summary>Selected action in the controls panel, and whether it is waiting for a key.</summary>
        public int ControlsIndex;
        public bool Rebinding;
        public string BindNote = "";

        /// <summary>Finished runs, newest first, loaded by the host when the panel opens (the Core reads no files).</summary>
        public System.Collections.Generic.List<RunRecord> Runs = new System.Collections.Generic.List<RunRecord>();
        public int RunsIndex;
        /// <summary>Achievement id to the date it was unlocked, loaded by the host when the panel opens.</summary>
        public System.Collections.Generic.Dictionary<string, string> Unlocked = new System.Collections.Generic.Dictionary<string, string>();
        public int AchIndex;
        /// <summary>Show only daily-challenge runs, best score first.</summary>
        public bool RunsDaily;

        /// <summary>One-line result of the last menu action ("Game saved.").</summary>
        public string MenuNote = "";

        /// <summary>Which pane the world map is showing while travelling.</summary>
        public int TravelPage;

        /// <summary>Character-creation form (Panel.Create).</summary>
        public readonly CreationState Create = new CreationState();

        public bool IsOpen => Active != Panel.None;
        public bool IsTargeting => Targeting != TargetingMode.None;
    }

    public enum CreateStep { Name, Race, Role, Confirm }

    /// <summary>Plain data behind the creation screen; the host mutates it, the panel reads it.</summary>
    public sealed class CreationState
    {
        public CreateStep Step = CreateStep.Name;
        public string Name = "";
        public int RaceIndex, RoleIndex;
        public Difficulty Difficulty;
        public string RaceId => Races.All[RaceIndex].Id;
        public string RoleId => Roles.All[RoleIndex].Id;
        public void Reset() { Step = CreateStep.Name; Name = ""; RaceIndex = 0; RoleIndex = 0; Difficulty = Difficulty.Normal; }
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
        public bool Settings;
        public bool Spells;
        public bool Abilities;
        public bool Advance;
        public bool Altar;

        public void Clear()
        {
            Inventory = false; Help = false; HelpLong = false;
            History = false; Discoveries = false; Character = false; Travel = false;
            Settings = false; Spells = false; Abilities = false; Advance = false; Altar = false;
        }
    }
}

namespace Ossuary.Core
{
    public enum MenuRow { Resume, Save, Theme, Crt, Scale, Language, Master, Music, Effects, Controls, Achievements, PastRuns, MainMenu, Quit }

    /// <summary>Row order of the F2 menu, shared by the panel that draws it and the host that drives it.</summary>
    public static class MenuRows
    {
        public static readonly MenuRow[] All = (MenuRow[])Enum.GetValues(typeof(MenuRow));
    }
}
