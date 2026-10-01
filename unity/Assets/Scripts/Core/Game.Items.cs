using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>Item-use verbs. Split from the main Game partial so combat and movement stay readable.</summary>
    public sealed partial class Game
    {
        public void UseScroll(Item scroll)
        {
            if (scroll.RemainingCharges <= 0) { Say("The scroll is blank."); return; }
            scroll.ChargesUsed++;
            scroll.Identified = true;
            var p = Player;

            if (scroll.Name.Contains("identify"))
            {
                Say("You feel a surge of insight.", MessageKind.Good);
                foreach (var it in p.Inventory) it.Identified = true;
                if (p.Wielded != null) p.Wielded.Identified = true;
                if (p.WornArmor != null) p.WornArmor.Identified = true;
                for (int i = 0; i < 2; i++) if (p.Rings[i] != null) { p.Rings[i].Identified = true; p.RingKnown[i] = true; }
                p.GainSkill(Skill.Magic, 4);
            }
            else if (scroll.Name.Contains("mapping"))
            {
                Say("The shape of this level floods your mind.", MessageKind.Good);
                for (int y = 0; y < Map.H; y++)
                    for (int x = 0; x < Map.W; x++)
                        Map.Remember(x, y);
                p.GainSkill(Skill.Magic, 3);
            }
            else if (scroll.Name.Contains("destroy armor"))
            {
                if (p.WornArmor != null)
                {
                    Say($"Your {p.WornArmor.Name} crumbles to nothing!", MessageKind.Bad);
                    p.Inventory.Add(p.WornArmor);
                    p.WornArmor = null;
                }
                else Say("You are not wearing armour. Nothing happens.");
            }
            else if (scroll.Name.Contains("confuse"))
            {
                var m = FacingMonster();
                if (m != null)
                {
                    m.Confused = true;
                    m.ConfusionTurns = Rng.Range(8, 20);
                    Say($"The {m.TheName} reels in confusion.", MessageKind.Good);
                }
                else Say("You find nothing to confuse.");
            }
            else if (scroll.Name.Contains("fire"))
            {
                var m = FacingMonster();
                if (m != null) Say($"A column of fire engulfs the {m.TheName}!", MessageKind.Combat);
                else Say("Fire erupts around you, and passes harmlessly overhead.");
                Map.Version++;
            }
            else if (scroll.Name.Contains("earth"))
            {
                Say("The ground trembles and dust falls from the ceiling.", MessageKind.Narrative);
                Map.Version++;
            }
            else if (scroll.Name.Contains("punishment"))
            {
                Say("You feel the weight of a thousand eyes turn toward you.", MessageKind.Warn);
                Down(Rng.Range(3, 9));
            }
            else if (scroll.Name.Contains("charging"))
            {
                var target = FindUncharged();
                if (target != null)
                {
                    target.Charges += Rng.Range(3, 8);
                    Say($"The {target.Name} glows briefly and seems fuller.", MessageKind.Good);
                }
                else Say("You feel strangely refreshed.");
            }
            else if (scroll.Name.Contains("teleportation"))
            {
                Say("The world folds around you.", MessageKind.Narrative);
                TeleportPlayerAway();
            }
            else if (scroll.Name.Contains("genocide"))
            {
                Say("Something vast shifts its attention elsewhere.", MessageKind.Good);
                p.GainSkill(Skill.Magic, 8);
            }

            p.GainSkill(Skill.Magic, 1);
            EndPlayerTurn();
        }

        Item FindUncharged()
        {
            var p = Player;
            for (int i = 0; i < p.Inventory.Count; i++)
            {
                var k = p.Inventory[i].Def.Kind;
                if ((k == ItemKind.Wand || k == ItemKind.Potion) && p.Inventory[i].RemainingCharges < 2)
                    return p.Inventory[i];
            }
            return null;
        }

        public void UseWand(Item wand)
        {
            if (wand.RemainingCharges <= 0) { Say("The wand is spent."); return; }
            wand.ChargesUsed++;
            wand.Identified = true;
            var p = Player;

            if (wand.Name.Contains("light"))
            {
                Say("The wand flares with light!", MessageKind.Good);
                for (int dy = -6; dy <= 6; dy++)
                    for (int dx = -6; dx <= 6; dx++)
                        if (Map.InBounds(p.X + dx, p.Y + dy)) Map.Remember(p.X + dx, p.Y + dy);
                UpdateFov();
            }
            else if (wand.Name.Contains("create monster"))
            {
                var def = Bestiary.RandomForDepth(Depth, Rng, out _);
                if (def.HasValue)
                {
                    var m = new Monster(def.Value, Rng);
                    int spot = FindOpenCellNear(p.X, p.Y, 3);
                    if (spot >= 0)
                    {
                        m.X = spot % Map.W; m.Y = spot / Map.W;
                        Monsters.Add(m);
                        Say($"A {m.Name} appears in a puff of smoke!", MessageKind.Bad);
                        m.Alert = 1;
                    }
                }
            }
            else if (wand.Name.Contains("digging"))
            {
                int fx = p.X + FacingX, fy = p.Y + FacingY;
                if (Map.InBounds(fx, fy) && Map.BlocksMove(fx, fy))
                {
                    Map.Set(fx, fy, TileKind.Floor);
                    Say("The wall crumbles to rubble!", MessageKind.Good);
                    Map.Version++;
                }
                else Say("There is nothing to dig there.");
            }
            else if (wand.Name.Contains("striking") || wand.Name.Contains("cold") ||
                     wand.Name.Contains("fire") || wand.Name.Contains("lightning"))
            {
                var m = FacingMonster();
                string what;
                if (wand.Name.Contains("cold")) what = "A frost ray";
                else if (wand.Name.Contains("lightning")) what = "A bolt of lightning";
                else if (wand.Name.Contains("fire")) what = "A gout of flame";
                else what = "A silver bolt";
                if (m != null)
                {
                    int dmg = Rng.Range(4, 10);
                    m.HP -= dmg;
                    Say($"{what} strikes the {m.TheName} for {dmg} damage.", MessageKind.Combat);
                    if (m.HP <= 0) KillMonster(m);
                }
                else Say($"{what} flashes past and fades.");
            }
            else if (wand.Name.Contains("teleportation"))
            {
                Say("You are yanked across the level!", MessageKind.Narrative);
                TeleportPlayerAway();
            }
            else if (wand.Name.Contains("opening") || wand.Name.Contains("locking"))
            {
                int fx = p.X + FacingX, fy = p.Y + FacingY;
                bool open = wand.Name.Contains("opening");
                Map.Set(fx, fy, open ? TileKind.OpenDoor : TileKind.ClosedDoor);
                Say(open ? "The door swings open." : "The door slams shut.", MessageKind.Neutral);
                Map.Version++;
            }
            else if (wand.Name.Contains("nothing"))
            {
                Say("Nothing happens.", MessageKind.Info);
            }

            p.GainSkill(Skill.Magic, 2);
            EndPlayerTurn();
        }

        void TeleportPlayerAway()
        {
            if (Map == null) return;
            for (int attempt = 0; attempt < 300; attempt++)
            {
                int x = Rng.Range(1, Map.W - 1), y = Rng.Range(1, Map.H - 1);
                if (!Map.Walkable(x, y)) continue;
                if (MonsterAt(x, y) != null) continue;
                Player.X = x; Player.Y = y;
                UpdateFov();
                return;
            }
            Say("The pull fades before it takes hold.", MessageKind.Neutral);
        }

        public Monster FacingMonster() => MonsterAt(Player.X + FacingX, Player.Y + FacingY);

        public int FindOpenCellNear(int x, int y, int radius)
        {
            if (Map == null) return -1;
            for (int r = 1; r <= radius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        int nx = x + dx, ny = y + dy;
                        if (!Map.Walkable(nx, ny)) continue;
                        if (MonsterAt(nx, ny) != null) continue;
                        return nx + ny * Map.W;
                    }
                }
            }
            return -1;
        }
    }
}