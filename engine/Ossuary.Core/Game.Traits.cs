using System;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Habits of the monsters native to one branch (MonsterDef.Trait): bats that flutter, rats that breed, the drowned
    /// that mend in water, wisps that burst, wraiths that set you alight. Each is a few lines at a fixed point in the
    /// monster's turn; none of them needs state beyond what the monster already carries.
    /// </summary>
    public sealed partial class Game
    {
        const int SwarmCap = 8;

        /// <summary>Before a monster moves or fights. Returns true when the trait spent its turn.</summary>
        bool TraitBeforeAct(Monster m, int dist)
        {
            switch (m.Def.Trait)
            {
                case "erratic":
                    if (dist > 1 && Rng.Chance(45)) { TryMonsterStep(m, Rng.Range(-1, 2), Rng.Range(-1, 2)); return true; }
                    break;
                case "swarm":
                    if (m.Alert > 0 && m.HP == m.MaxHP && Rng.Chance(7) && CountNamed(m.Def.Name) < SwarmCap && SplitSwarm(m)) return true;
                    break;
                case "pack":
                    m.Speed = m.Def.Speed + (PackMateNear(m, 3) ? 4 : 0);
                    break;
            }
            return false;
        }

        /// <summary>After a monster's blow on the player: poison, stun, fire.</summary>
        void TraitAfterHit(Monster m, AttackResult res)
        {
            if (!res.Hit || res.Killed) return;
            switch (m.Def.Trait)
            {
                case "plague": if (Rng.Chance(50)) { Say($"The {m.Name}'s bite festers.", MessageKind.Bad); PoisonPlayer(1); } break;
                case "slam": if (Rng.Chance(25) && Player.StunTurns < 2) { Player.StunTurns = 2; Player.Stunned = true; Say($"The {m.Name}'s blow rings through your skull.", MessageKind.Bad); } break;
                case "ashen": if (Rng.Chance(40)) SetAlight(Player); break;
            }
        }

        /// <summary>Each turn the monster is alive: the drowned mend in water.</summary>
        void TraitTick(Monster m)
        {
            if (m.Def.Trait == "drowned" && m.HP < m.MaxHP && Map != null && Map.SurfaceAt(m.X, m.Y) == SurfaceKind.Water) m.HP = Math.Min(m.MaxHP, m.HP + 2);
        }

        /// <summary>When the monster dies: a wisp bursts into flame.</summary>
        void TraitOnDeath(Monster m)
        {
            if (m.Def.Trait != "wisp" || Map == null) return;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = m.X + dx, y = m.Y + dy;
                    if (Map.InBounds(x, y) && Tiles.Walkable(Map.Get(x, y)) && Map.SurfaceAt(x, y) != SurfaceKind.Water) PutSurface(x, y, SurfaceKind.Fire, 4);
                }
            Say($"The {m.Name} bursts into flame!", MessageKind.Combat);
            if (Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y) <= 1) SetAlight(Player);
        }

        int CountNamed(string name)
        {
            int n = 0;
            foreach (var o in Monsters) if (o.Def.Name == name && !o.IsDead) n++;
            return n;
        }

        bool PackMateNear(Monster m, int radius)
        {
            foreach (var o in Monsters)
                if (!ReferenceEquals(o, m) && !o.IsDead && !o.Ally && o.Def.Trait == "pack" && Pathfinder.Chebyshev(o.X, o.Y, m.X, m.Y) <= radius) return true;
            return false;
        }

        bool SplitSwarm(Monster m)
        {
            for (int k = 0; k < 8; k++)
            {
                int nx = m.X + Pathfinder.Dx8[k], ny = m.Y + Pathfinder.Dy8[k];
                if (!FreeCell(nx, ny) || AvoidsCell(m, nx, ny)) continue;
                var kin = new Monster(m.Def, Rng) { X = nx, Y = ny, HomeX = nx, HomeY = ny, Depth = m.Depth, Alert = 1 };
                Monsters.Add(kin);
                if (Map.IsVisible(nx, ny)) Say($"The {m.Name} squeals, and another scurries out of the dark.", MessageKind.Warn);
                Map.Version++;
                return true;
            }
            return false;
        }
    }
}
