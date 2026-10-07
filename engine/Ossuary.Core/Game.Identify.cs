using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Identify by use. Potions, scrolls and wands start as what they look like (Items/Appearances.cs); drinking, reading or zapping
    /// one, buying it, having it appraised or reading identify teaches the kind for the rest of the run, and every one of that kind
    /// the hero holds or picks up afterwards is known.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>The kinds (item names) the hero has learned this run.</summary>
        public readonly HashSet<string> KnownKinds = new HashSet<string>();

        public bool Knows(ItemDef d) => !Appearances.Hidden(d) || KnownKinds.Contains(d.Name);

        /// <summary>Learns the kind of a thing that was just used, bought or appraised. Says what it was when it had been a mystery.</summary>
        public void Learn(Item it, bool announce = true)
        {
            if (it == null) return;
            bool mystery = !it.Identified;
            string look = mystery ? it.Name : null;
            it.Identified = true;
            if (!Appearances.Hidden(it.Def) || !KnownKinds.Add(it.Def.Name)) return;
            foreach (var other in Player.Inventory) if (other.Def.Name == it.Def.Name) other.Identified = true;
            if (announce && mystery && look != null) Say($"You learn what the {look} is: {it.Name}.", MessageKind.Info);
        }

        /// <summary>Something enters the pack: a known kind is recognised at once, and a thing that arrives known teaches its kind.</summary>
        void Recognise(Item it)
        {
            if (it == null || !Appearances.Hidden(it.Def)) return;
            if (it.Identified) KnownKinds.Add(it.Def.Name);
            else if (KnownKinds.Contains(it.Def.Name) || Player.Inventory.Exists(o => o != it && o.Identified && o.Def.Name == it.Def.Name)) { it.Identified = true; KnownKinds.Add(it.Def.Name); }
        }

        /// <summary>Kinds learned so far, by group, for the discoveries panel.</summary>
        public int KnownCount(ItemKind k)
        {
            int n = 0;
            foreach (string name in KnownKinds) if (Trades.TryDef(name, out var d) && d.Kind == k) n++;
            return n;
        }
    }
}
