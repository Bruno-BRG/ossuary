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
            Recognise(it);
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
                if (Player.QuiverUid != 0 && it.Uid == Player.QuiverUid) return it;
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
            if (res.Hit) Splatter(m, res.Damage);
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
        // ---------------------------------------------------------------- choosing what to loose

        public const string QuiverPrompt = "Loose which first?";

        /// <summary>The hero picks the stack the quiver reaches for first (silver for the dead, plain for the rest).</summary>
        public void ChooseQuiver(Item stack)
        {
            if (stack == null || stack.Def.Kind != ItemKind.Ammo) return;
            Player.QuiverUid = stack.Uid;
            Say($"You will loose {stack.Name} first.", MessageKind.Info);
        }

        // ---------------------------------------------------------------- monster archers

        /// <summary>
        /// A monster with a bow, crossbow or sling and something to loose shoots from range instead of closing in: a hit, a miss or
        /// a dodge, and the piece lands at the hero's feet unless it snapped. True when it shot.
        /// </summary>
        public bool MonsterShoots(Monster m, int dist)
        {
            if (dist < 2 || dist > 7 || m.Ally || m.Townsperson || Map == null) return false;
            Item launcher = null, ammo = null;
            foreach (var it in m.Inventory) if (Ammo.IsLauncher(it)) { launcher = it; break; }
            if (launcher == null) return false;
            string want = Ammo.AmmoFor(launcher.Def);
            foreach (var it in m.Inventory) if (it.Def.Kind == ItemKind.Ammo && it.Def.Name == want && it.Quantity > 0) { ammo = it; break; }
            if (ammo == null || !Map.IsVisible(m.X, m.Y) || !Fov.HasLine(Map, m.X, m.Y, Player.X, Player.Y)) return false;
            if (!Rng.Chance(dist <= 2 ? 40 : 75)) return false;

            int fromX = m.X, fromY = m.Y, toX = Player.X, toY = Player.Y;
            Fx((tl, s) => FxLib.Bolt(tl, s, fromX, fromY, toX, toY, Elem.Wind, '\0', 3));
            MakeNoise(1);
            if (--ammo.Quantity <= 0) m.Inventory.Remove(ammo);
            int toHit = (m.Def.ToHit != null && m.Def.ToHit.Length > 0 ? m.Def.ToHit[0] : 2) + launcher.Enchant;
            int roll = toHit + Rng.Dice(20);
            bool hit = roll >= 20 - Player.ArmorClass() + 1 + dist / 2 && Rng.Dice(100) >= Player.Evasion() * 3;
            string what = ammo.Def.Name;
            if (hit)
            {
                int dmg = Math.Max(1, Rng.Roll(ammo.Def.Damage, ammo.Def.Sides, Ammo.Pull(launcher.Def) - 1 + ammo.Enchant));
                Player.HP -= dmg;
                HurtBy(Article(m));
                Splatter(Player, dmg);
                Say($"The {m.Name} shoots: the {what} hits you for {dmg} damage.", MessageKind.Combat);
                CheckDeath();
            }
            else Say($"The {m.Name} shoots: the {what} misses you.", MessageKind.Combat);
            // The piece lands where the hero stands, unless it snapped: it is the hero's to pick up.
            if (Rng.Range(0, 100) >= Ammo.BreakChance(ammo.Def, hit))
                GroundItems.Add(Map.Number, toX, toY, new Item(ammo.Def, Rng, NextUid()) { Identified = true, Material = ammo.Material, Quantity = 1 });
            Map.Version++;
            return true;
        }


        /// <summary>
        /// A companion archer looses a shot from its own bow at the nearest visible foe two to six cells away. The shot lands
        /// where the foe stands, as a hero's arrow would, and is the hero's to pick up.
        /// </summary>
        bool AllyShoots(Monster a)
        {
            Item launcher = null, ammo = null;
            foreach (var it in a.Inventory) if (Ammo.IsLauncher(it)) { launcher = it; break; }
            if (launcher == null) return false;
            string want = Ammo.AmmoFor(launcher.Def);
            foreach (var it in a.Inventory) if (it.Def.Kind == ItemKind.Ammo && it.Def.Name == want && it.Quantity > 0) { ammo = it; break; }
            if (ammo == null) return false;
            Monster foe = null; int best = int.MaxValue;
            foreach (var m in Monsters)
            {
                if (m.IsDead || m.Ally || m.Townsperson || m.Def.Level == 0) continue;
                int d = Pathfinder.Chebyshev(a.X, a.Y, m.X, m.Y);
                if (d < 2 || d > 6 || !Map.IsVisible(m.X, m.Y) || !Fov.HasLine(Map, a.X, a.Y, m.X, m.Y)) continue;
                if (d < best) { foe = m; best = d; }
            }
            if (foe == null || !Rng.Chance(best <= 2 ? 55 : 75)) return false;

            int fromX = a.X, fromY = a.Y, toX = foe.X, toY = foe.Y;
            Fx((tl, s) => FxLib.Bolt(tl, s, fromX, fromY, toX, toY, Elem.Wind, '\0', 3));
            if (--ammo.Quantity <= 0) a.Inventory.Remove(ammo);
            bool hit = Rng.Chance(70 - 5 * best);
            if (hit)
            {
                int dmg = Math.Max(1, Rng.Roll(ammo.Def.Damage, ammo.Def.Sides, Ammo.Pull(launcher.Def) - 1 + ammo.Enchant));
                foe.HP -= dmg;
                foe.Alert = 1; foe.Dormant = false;
                Say($"The {a.Name} shoots the {foe.TheName} for {dmg} damage.", MessageKind.Combat);
                if (foe.IsDead) { _killByAlly = true; KillMonster(foe); }
            }
            else Say($"The {a.Name}'s arrow misses the {foe.TheName}.", MessageKind.Combat);
            if (Rng.Range(0, 100) >= Ammo.BreakChance(ammo.Def, hit))
                GroundItems.Add(Map.Number, toX, toY, new Item(ammo.Def, Rng, NextUid()) { Identified = true, Material = ammo.Material, Quantity = 1 });
            Map.Version++;
            return true;
        }

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
            foreach (string line in RelicStory(it)) lines.Add(line);
            return lines;

            string Signed(int v) => v >= 0 ? "+" + v : v.ToString();
        }

        static string Quality(Item it) =>
            it.Title != null ? "a named masterwork" : it.Enchant >= 2 ? "masterwork" : it.Enchant == 1 ? "fine work" : it.Enchant < 0 ? "crude work" : "plain work";

        /// <summary>
        /// What the world remembers of a thing: a unique's biography from the chronicle (once it is known for what it is), the
        /// people who had it before the hero, and the deeds done with it. English lines, their Portuguese registered beside them.
        /// </summary>
        public List<string> RelicStory(Item it)
        {
            var lines = new List<string>();
            if (it == null) return lines;
            var art = Artifacts.Find(it.ArtifactId);
            if (art != null && it.Identified)
            {
                var keys = new List<string>();
                foreach (var (en, pt) in Ossuary.Core.World.History.Biography(Rng.Seed, art.Id, art.Name, it.Mat?.Name, keys))
                    lines.Add(TownText.L(en, pt));
                foreach (string k in keys) LearnLegend(k);
            }
            if (it.Owners != null) foreach (string o in it.Owners) lines.Add($"Once carried by {o}.");
            if (it.Deeds != null) foreach (string d in it.Deeds) lines.Add(d);
            return lines;
        }

        /// <summary>A gear item changes hands: it remembers who had it, in both languages.</summary>
        void HandedDown(Item it, string en, string pt)
        {
            if (it == null || (!it.Def.Kind.IsGear() && it.Def.Kind != ItemKind.Ring && it.Def.Kind != ItemKind.Amulet)) return;
            it.AddOwner(TownText.L(en, pt));
        }

        /// <summary>A boss or unique falls: every relic the hero has in hand or on writes the deed into itself.</summary>
        void RelicsRemember(Monster m)
        {
            if (m.BossId == null && !m.Unique) return;
            int day = World != null ? World.Day : 0;
            string where = Branch + " " + Depth;
            var relics = new List<Item>();
            if (Player.Wielded != null && Player.Wielded.IsRelic) relics.Add(Player.Wielded);
            foreach (var piece in Player.WornPieces()) if (piece.IsRelic) relics.Add(piece);
            if (Player.Amulet != null && Player.Amulet.IsRelic) relics.Add(Player.Amulet);
            for (int i = 0; i < 2; i++) if (Player.Rings[i] != null && Player.Rings[i].IsRelic) relics.Add(Player.Rings[i]);
            foreach (var r in relics)
            {
                bool hand = r == Player.Wielded;
                string en = hand ? $"Slew the {m.Name} on {where}, day {day}, in the hand of {Player.CharName}."
                                 : $"Was worn by {Player.CharName} when the {m.Name} fell on {where}, day {day}.";
                r.AddDeed(en);
            }
            if (relics.Count > 0) Say($"Your relics will remember the {m.Name}.", MessageKind.Good);
        }

        public void ExamineItem(Item it)
        {
            foreach (string line in DescribeItem(it)) Say(line, MessageKind.Info);
        }
    }
}
