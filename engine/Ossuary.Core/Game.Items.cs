using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>Item-use verbs. Split from the main Game partial so combat and movement stay readable.</summary>
    public sealed partial class Game
    {
        /// <summary>A wand, scroll or potion backed by a spell: self spells go off at once, the rest ask where. Nothing is spent until the spell lands.</summary>
        void UseSpellItem(Item it, MagicItemDef md)
        {
            var sp = Spells.Find(md.Spell);
            if (sp == null || Map == null) return;
            if (it.Def.Kind == ItemKind.Wand && it.RemainingCharges <= 0) { Say("The wand is spent."); return; }
            if (Player.Asleep || Player.Stunned) { Say("You cannot focus."); return; }
            if (sp.Target == SpellTarget.Self) { CastFromItem(it, md.Spell, Player.X, Player.Y, md.Power); return; }
            UiState.CastSpell = md.Spell; UiState.CastItem = it; UiState.CastPower = md.Power;
            PushTargeting(TargetingMode.Cast);
            var near = NearestVisibleHostile(sp.Range);
            if (near != null) { UiState.TargetX = near.X; UiState.TargetY = near.Y; }
        }

        public void UseScroll(Item scroll)
        {
            if (scroll.RemainingCharges > 0 && ItemSpells.TryGet(scroll.Def.Name, out var scrollSpell)) { UseSpellItem(scroll, scrollSpell); return; }
            if (scroll.RemainingCharges <= 0) { Say("The scroll is blank."); return; }
            scroll.ChargesUsed++;
            Learn(scroll);
            if (scroll.RemainingCharges <= 0) Player.Inventory.Remove(scroll);
            var p = Player;

            if (scroll.Def.Name.Contains("enchant"))
            {
                bool weapon = scroll.Def.Name.Contains("weapon");
                Item target = weapon ? p.Wielded : null;
                if (!weapon)
                    foreach (var piece in p.WornPieces()) if (target == null || piece.Enchant < target.Enchant) target = piece;
                if (target == null) Say(weapon ? "Your empty hands tingle." : "Your skin prickles, but you wear nothing to enchant.");
                else if (target.Enchant >= 5) Say($"Your {target.Name} shudders, but can hold no more.", MessageKind.Warn);
                else
                {
                    target.Enchant++;
                    p.RefreshGear();
                    Say($"Your {target.Name} glows with a soft blue light.", MessageKind.Good);
                }
                p.GainSkill(Skill.Magic, 1);
                EndPlayerTurn();
                return;
            }

            if (scroll.Def.Name.Contains("identify"))
            {
                Say("You feel a surge of insight.", MessageKind.Good);
                foreach (var it in p.Inventory.ToArray()) Learn(it, false);
                if (p.Wielded != null) p.Wielded.Identified = true;
                foreach (var piece in p.WornPieces()) piece.Identified = true;
                for (int i = 0; i < 2; i++) if (p.Rings[i] != null) { p.Rings[i].Identified = true; p.RingKnown[i] = true; }
                p.GainSkill(Skill.Magic, 4);
            }
            else if (scroll.Def.Name.Contains("mapping"))
            {
                Say("The shape of this level floods your mind.", MessageKind.Good);
                for (int y = 0; y < Map.H; y++)
                    for (int x = 0; x < Map.W; x++)
                        Map.Remember(x, y);
                p.GainSkill(Skill.Magic, 3);
            }
            else if (scroll.Def.Name.Contains("destroy armor"))
            {
                if (p.WornArmor != null)
                {
                    Say($"Your {p.WornArmor.Name} crumbles to nothing!", MessageKind.Bad);
                    p.Inventory.Add(p.WornArmor);
                    p.WornArmor = null;
                    p.RefreshGear();
                }
                else Say("You are not wearing armour. Nothing happens.");
            }
            else if (scroll.Def.Name.Contains("confuse"))
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
            else if (scroll.Def.Name.Contains("fire"))
            {
                var m = FacingMonster();
                if (m != null) Say($"A column of fire engulfs the {m.TheName}!", MessageKind.Combat);
                else Say("Fire erupts around you, and passes harmlessly overhead.");
                Map.Version++;
            }
            else if (scroll.Def.Name.Contains("earth"))
            {
                Say("The ground trembles and dust falls from the ceiling.", MessageKind.Narrative);
                Map.Version++;
            }
            else if (scroll.Def.Name.Contains("punishment"))
            {
                Say("You feel the weight of a thousand eyes turn toward you.", MessageKind.Warn);
                Down(Rng.Range(3, 9));
            }
            else if (scroll.Def.Name.Contains("charging"))
            {
                var target = FindUncharged();
                if (target != null)
                {
                    target.Charges += Rng.Range(3, 8);
                    Say($"The {target.Name} glows briefly and seems fuller.", MessageKind.Good);
                }
                else Say("You feel strangely refreshed.");
            }
            else if (scroll.Def.Name.Contains("teleportation"))
            {
                Say("The world folds around you.", MessageKind.Narrative);
                TeleportPlayerAway();
            }
            else if (scroll.Def.Name.Contains("genocide"))
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
                if (k == ItemKind.Wand && p.Inventory[i].RemainingCharges < 2)
                    return p.Inventory[i];
            }
            return null;
        }

        public void UseWand(Item wand)
        {
            if (wand.RemainingCharges <= 0) { Say("The wand is spent."); return; }
            if (ItemSpells.TryGet(wand.Def.Name, out var wandSpell)) { UseSpellItem(wand, wandSpell); return; }
            wand.ChargesUsed++;
            Learn(wand);
            var p = Player;

            if (wand.Def.Name.Contains("light"))
            {
                Say("The wand flares with light!", MessageKind.Good);
                for (int dy = -6; dy <= 6; dy++)
                    for (int dx = -6; dx <= 6; dx++)
                        if (Map.InBounds(p.X + dx, p.Y + dy)) Map.Remember(p.X + dx, p.Y + dy);
                UpdateFov();
            }
            else if (wand.Def.Name.Contains("create monster"))
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
            else if (wand.Def.Name.Contains("digging"))
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
            else if (wand.Def.Name.Contains("striking") || wand.Def.Name.Contains("cold") ||
                     wand.Def.Name.Contains("fire") || wand.Def.Name.Contains("lightning"))
            {
                var m = FacingMonster();
                string what;
                if (wand.Def.Name.Contains("cold")) what = "A frost ray";
                else if (wand.Def.Name.Contains("lightning")) what = "A bolt of lightning";
                else if (wand.Def.Name.Contains("fire")) what = "A gout of flame";
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
            else if (wand.Def.Name.Contains("teleportation"))
            {
                Say("You are yanked across the level!", MessageKind.Narrative);
                TeleportPlayerAway();
            }
            else if (wand.Def.Name.Contains("opening") || wand.Def.Name.Contains("locking"))
            {
                int fx = p.X + FacingX, fy = p.Y + FacingY;
                bool open = wand.Def.Name.Contains("opening");
                Map.Set(fx, fy, open ? TileKind.OpenDoor : TileKind.ClosedDoor);
                Say(open ? "The door swings open." : "The door slams shut.", MessageKind.Neutral);
                Map.Version++;
            }
            else if (wand.Def.Name.Contains("nothing"))
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
                int fromX = Player.X, fromY = Player.Y;
                Player.X = x; Player.Y = y;
                Fx((tl, s) => FxLib.Teleport(tl, s, fromX, fromY, x, y, Elem.Arcane));
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
