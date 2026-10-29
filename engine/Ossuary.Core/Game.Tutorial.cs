using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>One first-time hint: the card's text and the action that closes it.</summary>
    public sealed class Hint
    {
        public readonly string Id;
        /// <summary>English; the card shows it through <see cref="Loc.T"/>.</summary>
        public readonly string Text;
        /// <summary>The event that closes the card, raised by the code that performs the action (<see cref="Game.HintDone"/>).</summary>
        public readonly string DoneEvent;

        public Hint(string id, string text, string doneEvent) { Id = id; Text = text; DoneEvent = doneEvent; }
    }

    public static class Tutorials
    {
        public static readonly Hint[] All =
        {
            new Hint("dungeon", "Underground. Press t to explore on its own.", "explore"),
            new Hint("wounded", "Wounded. Bind your wounds with a bandage (a) to stop the bleeding.", "bandage"),
            new Hint("levelup", "A new level: pick a perk. Arrows and Enter take one.", "perk"),
            new Hint("shop", "Bought something? Shift+I shows its numbers.", "examine"),
            new Hint("crafting", "You made something. Shift+J lists every recipe you know.", "recipes"),
            new Hint("casting", "You cast a spell. Shift+Z lists every spell you know.", "spells"),
            new Hint("town", "A town. Bump into a person to talk; step into a counter to trade.", "talk"),
            new Hint("gathering", "You take meat and hide from a carcass. Shift+B crafts with them.", "craft"),
            new Hint("altar", "An altar. Offer something, or swear to the god who listens.", "sacrifice"),
            new Hint("jobs", "You have taken a job. F7 opens the Journal.", "journal"),
        };

        public static Hint Find(string id)
        {
            foreach (var h in All) if (h.Id == id) return h;
            return null;
        }
    }

    /// <summary>
    /// First-time hints: each system explains itself once per install. The shown set and the
    /// on/off switch are data the host reads and writes (like achievements); the Core never
    /// touches a file. A card takes no keys and spends no turn, so a hint changes neither the
    /// run nor its replay.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Hints are on unless the player said they already know the game.</summary>
        public bool TutorialsOn = true;

        /// <summary>No tutorials file existed yet: the first-run question is due.</summary>
        public bool FirstLaunch;

        /// <summary>Hint ids shown on this install so far, set by the host at start.</summary>
        public HashSet<string> SeenHints = new HashSet<string>();

        /// <summary>Hint ids first shown in this run, which the host writes to disk.</summary>
        public readonly List<string> NewlySeen = new List<string>();

        /// <summary>
        /// Shows a hint's card if tutorials are on, it has not been shown on this install and no
        /// other card is open. Marked as seen when shown, so an interrupted session never repeats it.
        /// </summary>
        public void ShowHint(string id)
        {
            var ui = UiState;
            if (!TutorialsOn || SeenHints.Contains(id) || ui.Hint != null || Tutorials.Find(id) == null) return;
            ui.Hint = id;
            SeenHints.Add(id);
            NewlySeen.Add(id);
        }

        /// <summary>Closes the open card when the action it was about has just happened.</summary>
        public void HintDone(string doneEvent)
        {
            var ui = UiState;
            var hint = ui.Hint == null ? null : Tutorials.Find(ui.Hint);
            if (hint != null && hint.DoneEvent == doneEvent) ui.Hint = null;
        }
    }
}
