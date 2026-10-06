using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// What gear is made of, in play (the table is <see cref="Materials"/>): weapons and armour wear with use and lose
    /// a point at each step, brittle or cheap blades can shatter on a critical, and the smith repairs gear or pours it
    /// anew in a metal you bring as ore. Wear counts blows, so it draws no RNG; only a possible shatter rolls.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Ore it takes to pour a weapon or a piece of armour anew.</summary>
        public const int OreForWeapon = 2, OreForArmour = 3;

        /// <summary>After one of the hero's melee blows: the bane line, then the weapon wears and maybe breaks.</summary>
        void AfterBlow(AttackResult res, bool crit)
        {
            var w = Player.Wielded;
            if (!res.Hit || w == null) return;
            if (res.Bane)
            {
                var m = Materials.Find(w.Material);
                if (m != null) Say("The " + m.Name + " bites deep!", MessageKind.Combat);
            }
            if (w.Rarity == Rarity.Artifact || w.Mat == null) return;
            int before = w.Condition;
            w.Wear++;
            if (crit && Shatters(w))
            {
                Player.Wielded = null;
                Player.RefreshGear();
                Say($"Your {w.Name} shatters!", MessageKind.Warn);
                return;
            }
            if (w.Condition > before) Say($"Your {w.Name} shows wear.", MessageKind.Warn);
        }

        /// <summary>A brittle weapon that is already worn, or a cheap one worn to the end, may give way on a critical.</summary>
        bool Shatters(Item w)
        {
            var m = w.Mat; int c = w.Condition;
            if (m.Brittle && c >= 1) return Rng.OneIn(4);
            if (c >= 2 && m.Durability <= 200) return Rng.OneIn(6);
            return false;
        }

        /// <summary>A blow that lands on the hero wears one worn piece (which one follows the damage, so no RNG is drawn).</summary>
        void ArmourTakesBlow(AttackResult res)
        {
            if (!res.Hit || res.Killed) return;
            var pieces = new List<Item>();
            foreach (var it in Player.WornPieces()) if (it.Mat != null && it.Rarity != Rarity.Artifact) pieces.Add(it);
            if (pieces.Count == 0) return;
            var piece = pieces[Math.Max(0, res.Damage) % pieces.Count];
            int before = piece.Condition;
            piece.Wear++;
            if (piece.Condition > before) Say($"Your {piece.Name} shows wear.", MessageKind.Warn);
        }

        // ---------------------------------------------------------------- the smithy

        IEnumerable<Item> HeldGear()
        {
            if (Player.Wielded != null) yield return Player.Wielded;
            foreach (var it in Player.WornPieces()) yield return it;
        }

        /// <summary>What the smith asks to make every worn and wielded piece sound again.</summary>
        public int RepairPrice()
        {
            int price = 0;
            foreach (var it in HeldGear()) if (it.Wear > 0) price += 10 + 40 * it.Condition;
            return price;
        }

        public int ReforgePrice(MaterialDef m) => 40 + m.Value / 2;

        int OreCount(string ore) => Count(Player, i => i.Def.Name == ore);

        /// <summary>The metals the hero can have this piece poured in: it must take the metal, not be it already, and the ore must be in the pack.</summary>
        public List<MaterialDef> ReforgeOptions(Item it)
        {
            var list = new List<MaterialDef>();
            if (it == null || it.Rarity == Rarity.Artifact) return list;
            int need = it.Def.Kind == ItemKind.Weapon ? OreForWeapon : OreForArmour;
            var now = it.Mat;
            foreach (var m in Materials.All)
            {
                if (!m.Metal || !Materials.Takes(it.Def, m) || (now != null && now.Id == m.Id)) continue;
                string ore = Materials.OreFor(m);
                if (ore != null && OreCount(ore) >= need) list.Add(m);
            }
            return list;
        }

        /// <summary>The smithy's rows: repair, and one per metal the pack can pay for, for the weapon and the body armour.</summary>
        void AddSmithyRows(Action<string, string, int, bool> add)
        {
            int repair = RepairPrice();
            add("repair", "Repair my gear", repair, repair > 0);
            foreach (var (slot, it, what) in new[] { ("w", Player.Wielded, "weapon"), ("a", Player.WornArmor, "armour") })
            {
                int need = slot == "w" ? OreForWeapon : OreForArmour;
                foreach (var m in ReforgeOptions(it))
                    add("reforge:" + slot + ":" + m.Id, $"Pour my {what} in {m.Name} ({need} x {Materials.OreFor(m)})", ReforgePrice(m), true);
            }
        }

        /// <summary>Runs a smithy row. Returns false when the id is not one of them; the panel stays open either way.</summary>
        public bool SmithyAction(string id)
        {
            if (id == "repair")
            {
                int price = RepairPrice();
                if (price <= 0 || !Pay(price)) return true;
                foreach (var it in HeldGear()) it.Wear = 0;
                Player.RefreshGear();
                Tell("The smith hammers out the dents and grinds a fresh edge.", MessageKind.Good);
                return true;
            }
            if (!id.StartsWith("reforge:")) return false;
            var parts = id.Split(':');
            if (parts.Length != 3) return true;
            var item = parts[1] == "w" ? Player.Wielded : Player.WornArmor;
            var mat = Materials.Find(parts[2]);
            if (item == null || mat == null || !ReforgeOptions(item).Contains(mat)) { Tell("The smith shakes their head: not with what you brought.", MessageKind.Warn); return true; }
            if (!Pay(ReforgePrice(mat))) return true;
            string ore = Materials.OreFor(mat);
            int need = item.Def.Kind == ItemKind.Weapon ? OreForWeapon : OreForArmour;
            for (int i = Player.Inventory.Count - 1; i >= 0 && need > 0; i--)
            {
                var o = Player.Inventory[i];
                if (o.Def.Name != ore) continue;
                int take = Math.Min(need, Math.Max(1, o.Quantity));
                o.Quantity -= take; need -= take;
                if (o.Quantity <= 0) Player.Inventory.RemoveAt(i);
            }
            Materials.Set(item, mat);
            Player.RefreshGear();
            Tell($"The smith melts your ore and pours it. You get back {item.Name}.", MessageKind.Good);
            return true;
        }
    }
}
