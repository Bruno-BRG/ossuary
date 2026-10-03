using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Conversations ride on the service panel (like road events): the node's text is the note, its choices are the rows.
    /// Opened from a counter ("Talk") it returns to the menu when it ends; opened by bumping a person it closes the panel.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Story flags set by dialogue and quests ("elder.hired", later the truths). Pure function of seed and keys.</summary>
        public readonly HashSet<string> Flags = new HashSet<string>();

        public Dialogue CurrentDialogue;
        DNode _node;
        bool _dialogueFromCounter;
        static readonly Building DialogueBuilding = new Building { Name = "Conversation", Services = Service.None };

        internal int NextTalk() => _talkCount++;

        public bool CanTalk(Monster m) => m != null && m.Townsperson && Dialogues.For(m) != null;

        public void StartDialogue(Monster m)
        {
            var d = m != null && m.Memory != null && m.Memory.Has(NpcMemory.Struck) ? Dialogues.Cold(m) : Dialogues.For(m);
            if (d == null) return;
            OpenDialogue(d, m);
        }

        public void OpenDialogue(Dialogue d, Monster m)
        {
            _dialogueFromCounter = TalkBuilding != null && Talking == m && CurrentEvent == null;
            CurrentEvent = null;
            CurrentDialogue = d;
            Talking = m;
            if (!_dialogueFromCounter) TalkBuilding = DialogueBuilding;
            UiState.Active = Panel.Service;
            d.OnOpen?.Invoke(this, m);
            Goto(Dialogue.Start);
        }

        void Goto(string id)
        {
            _node = CurrentDialogue.Nodes[id];
            ServiceNote = Loc.T(_node.Text(this, Talking));
            UiState.ServiceIndex = 0;
        }

        List<ServiceRow> DialogueRows()
        {
            var rows = new List<ServiceRow>();
            if (_node == null) return rows;
            for (int i = 0; i < _node.Choices.Count; i++)
            {
                var c = _node.Choices[i];
                if (c.Show != null && !c.Show(this, Talking)) continue;
                bool ok = (c.If == null || c.If(this, Talking)) && Player.Gold >= c.Price;
                rows.Add(new ServiceRow { Id = "d:" + i, Label = c.Label, Price = c.Price, Enabled = ok });
            }
            rows.Add(new ServiceRow { Id = "d:end", Label = _dialogueFromCounter ? "Back" : "Take my leave", Price = 0, Enabled = true });
            return rows;
        }

        /// <summary>Runs a conversation choice. Returns true when the panel should close.</summary>
        bool DialogueAction(string id)
        {
            if (id != "d:end" && int.TryParse(id.Substring(2), out int i) && _node != null && i >= 0 && i < _node.Choices.Count)
            {
                var c = _node.Choices[i];
                if ((c.Show != null && !c.Show(this, Talking)) || (c.If != null && !c.If(this, Talking))) return false;
                if (Player.Gold < c.Price) { Tell("You cannot afford that.", MessageKind.Warn); return false; }
                Player.Gold -= c.Price;
                c.Do?.Invoke(this, Talking);
                QuestCheck();
                if (c.Goto != null && CurrentDialogue.Nodes.ContainsKey(c.Goto)) { Goto(c.Goto); return false; }
            }
            return EndDialogue();
        }

        bool EndDialogue()
        {
            CurrentDialogue = null; _node = null;
            if (Mode == GameMode.Won) return false;   // the ending took over the screen
            if (_dialogueFromCounter)
            {
                ServiceNote = Loc.T("You step back from the counter.");
                UiState.ServiceIndex = 0;
                return false;
            }
            return true;
        }
    }
}
