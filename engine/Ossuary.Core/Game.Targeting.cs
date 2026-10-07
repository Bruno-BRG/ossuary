using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>Targeting modes: looking, digging, shooting, swapping, inspecting.</summary>
    public sealed partial class Game
    {
        public void ResolveTargeting(int tx, int ty)
        {
            var mode = UiState.Targeting;
            UiState.Targeting = TargetingMode.None;

            switch (mode)
            {
                case TargetingMode.Look: DescribeCell(tx, ty); break;
                case TargetingMode.Inspect: InspectCell(tx, ty); break;
                case TargetingMode.Dig: DigAt(tx, ty); break;
                case TargetingMode.PickLock: PickLockAt(tx, ty); break;
                case TargetingMode.Shoot: ShootAt(tx, ty); break;
                case TargetingMode.Throw: ThrowAt(UiState.ThrowItem, tx, ty); UiState.ThrowItem = null; break;
                case TargetingMode.Swap: SwapWith(tx, ty); break;
                case TargetingMode.Cast:
                    {
                        var src = UiState.CastItem; UiState.CastItem = null;
                        if (src != null) CastFromItem(src, UiState.CastSpell, tx, ty, UiState.CastPower);
                        else CastSpell(UiState.CastSpell, tx, ty);
                        break;
                    }
                case TargetingMode.Ability: UseAbility(UiState.PendingAbility, tx, ty); break;
            }
        }

        void InspectCell(int x, int y)
        {
            if (Map == null || !Map.InBounds(x, y)) { Say("Nothing there."); return; }
            if (!Map.WasSeen(x, y)) { Say("You have never seen that place."); return; }
            Say($"({x},{y}) {TerrainName(Map.Get(x, y))}", MessageKind.Info);
        }

        void PickLockAt(int x, int y)
        {
            if (Map == null || !Map.InBounds(x, y)) { Say("Nothing there."); EndPlayerTurn(); return; }
            TileKind t = Map.Get(x, y);
            if (t == TileKind.LockedDoor || t == TileKind.HiddenDoor)
            {
                Map.Set(x, y, TileKind.OpenDoor);
                Say("You pick the lock.", MessageKind.Good);
                Map.Version++;
            }
            else Say("There is no lock there.");
            EndPlayerTurn();
        }

        void DigAt(int x, int y)
        {
            if (Map == null || !Map.InBounds(x, y)) { Say("You cannot dig there."); EndPlayerTurn(); return; }
            TileKind t = Map.Get(x, y);
            if (Tiles.Walkable(t)) { Say("There is nothing to dig here."); EndPlayerTurn(); return; }
            if ((Tiles.Get(t).Flags & TileFlags.Dig) == 0) { Say("That cannot be dug through."); EndPlayerTurn(); return; }
            if (Player.CountItemKind(ItemKind.Tool) == 0) { Say("You have nothing to dig with."); EndPlayerTurn(); return; }

            int effort = 1 + Player.StrengthForDamage() / 2 + Player.Skills[Skill.Survival] / 20;
            Map.Set(x, y, TileKind.Floor);
            Say($"You dig through the {Tiles.Get(t).Name} (effort {effort}).", MessageKind.Neutral);
            Map.Version++;
            Player.GainSkill(Skill.Survival, 1);
            MineVein();
            EndPlayerTurn();
        }

        void ShootAt(int x, int y)
        {
            if (x == Player.X && y == Player.Y) { Say("You cannot shoot yourself."); return; }
            var m = MonsterAt(x, y);
            if (m != null && m.Ally) { Say("You would hit your ally."); return; }
            if (!Fov.HasLine(Map, Player.X, Player.Y, x, y)) { Say("You do not have a clear shot."); return; }
            if (m == null)
            {
                var (launcher, ammo) = Quiver();
                if (launcher != null && ammo != null) { Say("You loose a shot into empty air."); Spend(ammo, x, y, false); }
                else Say("You hurl a stone into empty air.");
                Map.Version++;
                EndPlayerTurn();
                return;
            }

            int dist = Pathfinder.Chebyshev(Player.X, Player.Y, x, y);
            bool crit;
            int fromX = Player.X, fromY = Player.Y;
            Fx((tl, s) => FxLib.Bolt(tl, s, fromX, fromY, x, y, Elem.Wind, '\0', 3));
            var res = Loose(m, dist, out crit, 1, -Bodies.AimPenalty(Player.Aim));
            if (res.Hit) m.Asleep = false;
            Say(res.Message, res.Killed ? MessageKind.Kill : MessageKind.Combat);
            WoundFrom(m, res, true, crit, Player.Aim);
            Player.GainSkill(Skill.Combat, 2);
            if (res.Killed) KillMonster(m);
            Map.Version++;
            EndPlayerTurn();
        }

        void SwapWith(int x, int y)
        {
            var m = MonsterAt(x, y);
            if (m == null) { Say("There is nothing there."); return; }
            if (m.Inventory.Count == 0) { Say($"The {m.TheName} has nothing worth taking."); return; }
            Say($"The {m.TheName} is carrying:", MessageKind.Info);
            foreach (var it in m.Inventory) Say("  " + it.Name, MessageKind.Info);
            var pick = m.Inventory[Rng.Range(0, m.Inventory.Count)];
            if (pick.Def.Kind == ItemKind.Gold) Player.Gold += pick.Quantity;
            else Player.Inventory.Add(pick);
            m.Inventory.Remove(pick);
            Say($"You take {pick.Name}.", MessageKind.Good);
            Player.GainSkill(Skill.Stealth, 3);
            EndPlayerTurn();
        }

        public void DescribeCell(int x, int y)
        {
            if (Map == null || !Map.InBounds(x, y)) { Say("Nothing there."); return; }
            if (!Map.WasSeen(x, y)) { Say("You have never been there."); return; }

            var m = MonsterAt(x, y);
            if (m != null && Map.IsVisible(x, y))
            {
                Say(m.LongDescription, MessageKind.Info);
                Say($"It is {m.ThreatLabel()}.", MessageKind.Info);
                string work = CarriedWork(m);
                if (work != null) Say(work, MessageKind.Info);
                DescribeWounds(m);
                return;
            }

            var t = Map.Get(x, y);
            var items = GroundItems.At(Map.Number, x, y);
            var notes = new List<string>();
            if (items != null) foreach (var it in items) notes.Add(it.Name);
            if (TrapTable.TryGet(Map.Number, x, y, out var kind, out var lvl)) notes.Add($"{TrapName(kind)} trap (level {lvl})");
            if (Map.SurfaceAt(x, y) != SurfaceKind.None) notes.Add(SurfaceInfo.Name(Map.SurfaceAt(x, y)));
            var stain = Map.StainAt(x, y);
            if (stain.Kind != StainKind.None && Map.IsVisible(x, y)) notes.Add(StainLine(stain));
            string carved = Map.EngravingAt(x, y);
            if (carved != null) notes.Add($"an engraving: \"{carved}\"");
            string room = Mode == GameMode.Dungeon ? RoomAt(x, y) : null;
            if (room != null) notes.Add($"in what was {room}");

            if (notes.Count > 0)
            {
                Say($"({x},{y}) {TerrainName(t)}, with:", MessageKind.Info);
                foreach (var s in notes) Say("  " + s, MessageKind.Info);
                // A single thing on the floor is worth a closer look: what it is made of, who made it, who had it.
                if (items != null && items.Count == 1 && Map.IsVisible(x, y))
                {
                    var lines = DescribeItem(items[0]);
                    for (int i = 1; i < lines.Count; i++) Say("  " + lines[i], MessageKind.Info);
                }
            }
            else Say($"({x},{y}) {TerrainName(t)}.", MessageKind.Info);
        }

        public static string TerrainName(TileKind t) => Tiles.Get(t).Name;

        public static string TrapName(Traps k)
        {
            switch (k)
            {
                case Traps.Spike: return "spike";
                case Traps.Hole: return "pit";
                case Traps.Dart: return "dart";
                case Traps.Teleport: return "teleportation";
                case Traps.Alarm: return "alarm";
                case Traps.Fire: return "fire";
                case Traps.Web: return "web";
                default: return "unknown";
            }
        }
    }
}
