using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>One answer the hero can give. Gated by a condition, may cost gold, runs an effect, then goes to another node (null = end).</summary>
    public sealed class DChoice
    {
        public string Label, Goto;
        public Func<Game, Monster, bool> If;
        /// <summary>When set and false, the choice is not listed at all (If only greys it out).</summary>
        public Func<Game, Monster, bool> Show;
        public Action<Game, Monster> Do;
        public int Price;
    }

    /// <summary>What the person says, and what the hero may answer. The text can depend on the game (flags, reputation, memory).</summary>
    public sealed class DNode
    {
        public Func<Game, Monster, string> Text;
        public readonly List<DChoice> Choices = new List<DChoice>();
    }

    /// <summary>A short branching conversation, written as data. The PT of every string goes beside its EN through TownText.L.</summary>
    public sealed class Dialogue
    {
        public const string Start = "start";
        /// <summary>Runs when the conversation opens (start a quest, note a meeting).</summary>
        public Action<Game, Monster> OnOpen;
        public readonly Dictionary<string, DNode> Nodes = new Dictionary<string, DNode>();

        public Dialogue Node(string id, string text, params DChoice[] choices) => Node(id, (g, m) => text, choices);

        public Dialogue Node(string id, Func<Game, Monster, string> text, params DChoice[] choices)
        {
            var n = new DNode { Text = text };
            n.Choices.AddRange(choices);
            Nodes[id] = n;
            return this;
        }

        public static DChoice Go(string label, string to, Action<Game, Monster> doIt = null, Func<Game, Monster, bool> cond = null, int price = 0) =>
            new DChoice { Label = label, Goto = to, Do = doIt, If = cond, Price = price };
    }
}
