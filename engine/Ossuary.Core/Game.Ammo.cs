using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Ammunition and the pack. A bow, crossbow or sling (in hand or on the back) looses what it takes from the pack, one per
    /// shot; the piece lands where it was aimed and is picked up again, unless it snapped. With no launcher or nothing to
    /// loose, the hero hurls a stone, weakly. Also here: stacking what comes into the pack, the hero's named masterworks, and
    /// examining an item.
    /// </summary>
    public sealed partial class Game
    {
        public const string ExaminePrompt = "Examine what?";

        // ---------------------------------------------------------------- the pack

        /// <summary>Things that come by the stack and merge when they are the same thing.</summary>
        static bool Stacks(ItemKind k) => k == ItemKind.Ammo || k == ItemKind.Material || k == ItemKind.Rock || k == ItemKind.Food;

        static bool SameStack(Item a, Item b) =>
            a.Def.Name == b.Def.Name && a.Material == b.Material && a.Enchant == b.Enchant && a.Imbue == b.Imbue && a.Title == null && b.Title == null
            && a.Maker == b.Maker && a.ArtifactName == null && b.ArtifactName == null && a.Identified == b.Identified;

        /// <summary>Puts a thing in the pack, onto a stack of the same thing when there is one.</summary>
        public void Pack(Item it)
        {
            if (it == null) return;
            if (Stacks(it.Def.Kind))
                foreach (var have in Player.Inventory)
                    if (have != it && SameStack(have, it)) { have.Quantity += Math.Max(1, it.Quantity); return; }
            Player.Inventory.Add(it);
        }

        // ---------------------------------------------------------------- the quiver

        /// <summary>The launcher the hero would shoot with (the one in hand first) and the stack it would loose, either null.</summary>
        public (Item Launcher, Item Ammo) Quiver()
        {
            Item launcher = Ammo.IsLauncher(Player.Wielded) ? Player.Wielded : null;
            Item fed = launcher != null ? AmmoFor(launcher) : null;
            if (fed == null)
                foreach (var it in Player.Inventory)
                {
                    if (!Ammo.IsLauncher(it)) continue;
                    var a = AmmoFor(it);
                    if (a != null) { launcher = it; fed = a; break; }
                    if (launcher == null) launcher = it;
                }
            return (launcher, fed);
        }

        /// <summary>The best stack in the pack this launcher takes: enchanted first, then a metal that bites harder.</summary>
        Item AmmoFor(Item launcher)
        {
            string want = Ammo.AmmoFor(launcher.Def);
            Item best = null;
            foreach (var it in Player.Inventory)
            {
                if (it.Def.Kind != ItemKind.Ammo || it.Def.Name != want || it.Quantity <= 0) continue;
                if (best == null || Score(it) > Score(best)) best = it;
            }
            return best;
            int Score(Item i) => i.Enchant * 10 + (i.Mat?.Dmg ?? 0);
        }

        /// <summary>
        /// One shot at a monster: launcher and ammunition when there are both, a hurled stone otherwise. Spends the piece and
        /// leaves it at the target's feet unless it broke. The caller says the result and handles the kill.
        /// </summary>
        AttackResult Loose(Monster m, int dist, out bool crit, int mult, int hitBonus)
        {
            var (launcher, ammo) = Quiver();
            if (launcher == null || ammo == null)
            {
                if (launcher != null) Say($"You have nothing for the {launcher.Def.Name}. You hurl a stone.", MessageKind.Info);
                return Battles.PlayerRanged(Player, m, dist, Rng, out crit, mult, hitBonus);
            }
            var head = Materials.Find(ammo.Material);
            int hit = hitBonus + launcher.Enchant + ammo.Enchant + (head?.ToHit ?? 0) + launcher.Def.ToHit;
            int bonus = Ammo.Pull(launcher.Def) + launcher.Enchant + ammo.Enchant + (head?.Dmg ?? 0);
            var res = Battles.PlayerRanged(Player, m, dist, Rng, out crit, mult, hit, ammo.Def.Damage, ammo.Def.Sides, bonus, head);
            Spend(ammo, m.X, m.Y, res.Hit);
            return res;
        }

        /// <summary>Takes one piece off the stack; it lands at (x, y) unless it snapped.</summary>
        void Spend(Item stack, int x, int y, bool hit)
        {
            var one = new Item(stack.Def, Rng, NextUid()) { Identified = stack.Identified, Material = stack.Material, Enchant = stack.Enchant, Maker = stack.Maker, Quantity = 1 };
            if (--stack.Quantity <= 0) Player.Inventory.Remove(stack);
            AmmoSpent++;
            if (Rng.Range(0, 100) < Ammo.BreakChance(stack.Def, hit)) return;
            if (Map == null || !Map.InBounds(x, y) || !Tiles.Walkable(Map.Get(x, y))) return;
            var here = GroundItems.At(Map.Number, x, y);
            if (here != null) foreach (var g in here) if (SameStack(g, one)) { g.Quantity++; return; }
            GroundItems.Add(Map.Number, x, y, one);
        }

        /// <summary>Pieces loosed this run, for the tests and the morgue.</summary>
        public int AmmoSpent;

        // ---------------------------------------------------------------- named masterworks

        /// <summary>A crafted piece got a name: the log says so and the morgue keeps it.</summary>
        void NameMasterwork(Item it)
        {
            Say($"The work is too fine to be nameless. You call it {it.Title}.", MessageKind.Good);
            Player.Works.Add(it.Name);
        }

        // ---------------------------------------------------------------- examining

        /// <summary>What a careful look at a thing tells: what it is, its material, the hand that made it, its wear and worth.</summary>
        public List<string> DescribeItem(Item it)
        {
            var lines = new List<string>();
            if (it == null) return lines;
            lines.Add($"You look closely at {it.Name}.");
            if (it.Title != null) lines.Add($"A named work: {it.Title}.");
            var d = it.Def;
            if (d.Kind == ItemKind.Weapon && !Ammo.IsLauncher(it))
            {
                var m = it.Mods;
                lines.Add($"Damage {Math.Max(1, d.Damage)}d{d.Sides}{Signed(d.DmgBonus + m.Dmg)}, to hit {Signed(d.ToHit + m.ToHit)}.");
            }
            if (Ammo.IsLauncher(it)) lines.Add($"Looses {Ammo.AmmoFor(d)}, pull {Signed(Ammo.Pull(d) + it.Enchant)}.");
            if (d.Kind == ItemKind.Ammo) lines.Add($"Ammunition: {d.Damage}d{d.Sides}{Signed(it.Enchant + (it.Mat?.Dmg ?? 0))} a shot.");
            if (d.Kind.IsWearable()) lines.Add($"Armour {it.TotalAc}.");
            var mat = it.Mat;
            if (mat != null) lines.Add($"Made of {mat.Name}.");
            if (it.Maker != null) lines.Add($"Made by {it.Maker}, {Quality(it)}.");
            if (it.Engraving != null && !it.Engraving.StartsWith("made by ")) lines.Add($"Engraved: {it.Engraving}");
            if (mat != null && (d.Kind.IsGear()))
            {
                string word = it.ConditionWord;
                lines.Add(word == null ? $"Sound ({it.Wear} of {mat.Durability} blows)." : $"Worn: {word} ({it.Wear} blows).");
            }
            if (d.Kind != ItemKind.Gold) lines.Add($"Worth about {Math.Max(1, StackValue(it))} gold.");
            return lines;

            string Signed(int v) => v >= 0 ? "+" + v : v.ToString();
        }

        static string Quality(Item it) =>
            it.Title != null ? "a named masterwork" : it.Enchant >= 2 ? "masterwork" : it.Enchant == 1 ? "fine work" : it.Enchant < 0 ? "crude work" : "plain work";

        public void ExamineItem(Item it)
        {
            foreach (string line in DescribeItem(it)) Say(line, MessageKind.Info);
        }
    }
}
