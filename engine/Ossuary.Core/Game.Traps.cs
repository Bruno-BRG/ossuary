using System;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>Finding and disarming traps. Sensing uses a position hash, never the Rng, so it cannot shift a run.</summary>
    public sealed partial class Game
    {
        /// <summary>Percent chance that this hero notices a hidden trap next to them without searching.</summary>
        public int TrapSensePct() => Math.Min(90, Player.Skills[Skill.Search] / 2 + (Player.RoleId == "rogue" ? 25 : 0));

        /// <summary>After a step: traps in the eight neighbouring cells may reveal themselves.</summary>
        public void SenseTraps()
        {
            if (Map == null || Mode != GameMode.Dungeon) return;
            int pct = TrapSensePct();
            if (pct <= 0) return;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = Player.X + dx, y = Player.Y + dy;
                    if ((dx == 0 && dy == 0) || !TrapTable.TryGet(Map.Number, x, y, out var kind, out _)) continue;
                    if (TrapTable.IsRevealed(Map.Number, x, y)) continue;
                    if (Theme.Hash01(x * 31 + Map.Number, y * 17 + 5) * 100 >= pct) continue;
                    TrapTable.Reveal(Map.Number, x, y);
                    Say($"You spot a {TrapTable.Name(kind)}.", MessageKind.Good);
                }
        }

        public int DisarmPct() => Math.Min(95, 35 + Player.Dex * 2 + Player.Skills[Skill.Search] / 2 + (Player.RoleId == "rogue" ? 30 : 0));

        /// <summary>Tries to disarm the found trap at (x, y). Spends the turn; failure may set it off.</summary>
        public bool DisarmTrap(int x, int y)
        {
            if (Map == null || !TrapTable.TryGet(Map.Number, x, y, out var kind, out int level) || !TrapTable.IsRevealed(Map.Number, x, y))
            { Say("There is no known trap to disarm there."); return false; }
            if (Rng.Chance(DisarmPct()))
            {
                TrapTable.Remove(Map.Number, x, y);
                Player.GainSkill(Skill.Search, 3);
                Say($"You disarm the {TrapTable.Name(kind)}.", MessageKind.Good);
            }
            else if (Rng.Chance(50)) Say($"You fail to disarm the {TrapTable.Name(kind)}.", MessageKind.Warn);
            else
            {
                Say("You set it off!", MessageKind.Bad);
                TrapTable.Remove(Map.Number, x, y);
                TriggerTrap(kind, level);
            }
            Map.Version++;
            EndPlayerTurn();
            return true;
        }
    }
}
