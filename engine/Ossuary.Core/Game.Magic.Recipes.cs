using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>
    /// Spells written as data (see <see cref="SpellDef"/>): who they hit, the damage roll, the rider, the buff, the summon, the surface,
    /// and the few hand-coded specials. The older spells with their own method in Game.Magic.Effects.cs never reach this.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Resolves a recipe spell. Returns false if the spell has no recipe (so the caller can say nothing happened).</summary>
        bool ApplyRecipe(SpellDef sp, Monster target, int tx, int ty)
        {
            var p = Player;
            int lvl = CasterLevel, rank = SkillRanks.Rank(p.Skills[Skill.Magic]);
            bool self = sp.Target == SpellTarget.Self;
            int cx = self ? p.X : tx, cy = self ? p.Y : ty;
            bool did = false;

            var shape = sp.Shape;
            if (shape == Shape.Chain) { RecipeChain(sp, target, rank); did = true; }
            else if (shape == Shape.Scatter) { RecipeScatter(sp, rank); did = true; }
            else if (shape != Shape.None)
            {
                did = true;
                var victims = RecipeVictims(sp, shape, target, tx, ty);
                int dmg = sp.Dice > 0 ? Rng.Roll(sp.Dice + (sp.Div > 0 ? lvl / sp.Div : 0), sp.Sides, rank + sp.Flat) : 0;
                int dealt = 0;
                if (victims.Count == 0 && shape != Shape.Single) Say(shape == Shape.Nova ? "Nothing living stirs near you." : "The spell lands on empty ground.", MessageKind.Info);
                foreach (var m in victims)
                {
                    if (dmg > 0) dealt += Hurt(m, dmg, sp.Type, sp.Verb ?? "Magic strikes");
                    if (m.IsDead) continue;
                    ApplyRider(m, sp);
                    if (sp.Push != 0 && !m.IsDead)
                    {
                        bool pull = sp.Push < 0;
                        int fromX = pull && shape == Shape.Ball ? cx : p.X, fromY = pull && shape == Shape.Ball ? cy : p.Y;
                        PushMonster(m, fromX, fromY, sp.Push);
                    }
                }
                if (sp.DrainPct > 0 && dealt > 0)
                {
                    int heal = Math.Min(p.MaxHP - p.HP, Math.Max(1, dealt * sp.DrainPct / 100));
                    if (heal > 0) { p.HP += heal; Say($"You drink in {heal} life.", MessageKind.Good); }
                }
                if (sp.Type == DamageType.Fire && dmg > 0 && (shape == Shape.Ball || shape == Shape.Nova)) ScorchGround(cx, cy, sp.Radius, false);
            }

            if (sp.HealDice > 0)
            {
                did = true;
                Heal(Rng.Roll(sp.HealDice, sp.HealSides, sp.HealFlat + (p.Wis - 10) / 2 + lvl / 2));
            }
            if (sp.HasSurface)
            {
                did = true;
                int r = sp.Radius;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (Map.InBounds(cx + dx, cy + dy) && Map.IsVisible(cx + dx, cy + dy) && CanHoldSurface(cx + dx, cy + dy)) PutSurface(cx + dx, cy + dy, sp.Surface, sp.SurfaceTurns);
                Map.Version++;
            }
            if (sp.Buff != null)
            {
                did = true;
                int turns = sp.BuffTurns + p.Skills[Skill.Magic] / 4;
                var bd = SpellBuffs.Find(sp.Buff);
                p.SetBuff(sp.Buff, turns);
                Say(bd != null ? bd.Msg : sp.Buff == "haste" ? "The world slows around you." : sp.Buff == "levitating" ? "You rise off the floor. Traps cannot reach you." : "The spell takes hold.", MessageKind.Good);
            }
            if (sp.SummonDef != null) { did = true; SummonAllies(sp.SummonDef, sp.Summons, sp.SummonTurns + p.Skills[Skill.Magic] / 2, sp.Elem); }
            if (sp.Special != null) { did = true; ApplySpecial(sp, target, tx, ty, rank); }
            if (sp.Corrupt > 0) AddCorruption(sp.Corrupt, null);
            return did;
        }

        // ------------------------------------------------------------ who gets hit

        List<Monster> RecipeVictims(SpellDef sp, Shape shape, Monster target, int tx, int ty)
        {
            var hit = new List<Monster>();
            var p = Player;
            switch (shape)
            {
                case Shape.Single:
                    if (target != null && !target.IsDead) hit.Add(target);
                    break;
                case Shape.Ball:
                case Shape.Nova:
                    {
                        int cx = shape == Shape.Nova ? p.X : tx, cy = shape == Shape.Nova ? p.Y : ty;
                        foreach (var m in Monsters)
                            if (!m.IsDead && !m.Ally && !m.Townsperson && Pathfinder.Chebyshev(cx, cy, m.X, m.Y) <= sp.Radius && Map.IsVisible(m.X, m.Y)) hit.Add(m);
                        break;
                    }
                case Shape.Line:
                    {
                        int dx = tx - p.X, dy = ty - p.Y, steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        if (steps == 0) break;
                        for (int k = 1; k <= sp.Range; k++)
                        {
                            int x = p.X + (int)Math.Round((double)dx * k / steps), y = p.Y + (int)Math.Round((double)dy * k / steps);
                            if (!Map.InBounds(x, y) || !Tiles.Walkable(Map.Get(x, y))) break;
                            var m = MonsterAt(x, y);
                            if (m != null && !m.IsDead && !m.Ally && !m.Townsperson) hit.Add(m);
                        }
                        break;
                    }
                case Shape.Cone:
                    {
                        var cells = new HashSet<int>();
                        foreach (var c in Shapes.Cone(p.X, p.Y, tx, ty, Math.Max(2, sp.Radius))) cells.Add(c.y * Map.W + c.x);
                        foreach (var m in Monsters)
                            if (!m.IsDead && !m.Ally && !m.Townsperson && Map.IsVisible(m.X, m.Y) && cells.Contains(m.Y * Map.W + m.X) && Fov.HasLine(Map, p.X, p.Y, m.X, m.Y)) hit.Add(m);
                        break;
                    }
            }
            return hit;
        }

        // ------------------------------------------------------------ chains and scatters

        void RecipeChain(SpellDef sp, Monster first, int rank)
        {
            var struck = new HashSet<Monster>();
            var points = new List<(int x, int y)> { (Player.X, Player.Y) };
            var cur = first;
            int dice = sp.Dice + (sp.Div > 0 ? CasterLevel / sp.Div : 0);
            for (int hop = 0; hop <= sp.Hops && cur != null; hop++)
            {
                struck.Add(cur);
                int px = cur.X, py = cur.Y;
                points.Add((px, py));
                Hurt(cur, Rng.Roll(dice, sp.Sides, rank + sp.Flat), sp.Type, hop == 0 ? sp.Verb ?? "Magic strikes" : "The magic leaps to");
                if (!cur.IsDead) ApplyRider(cur, sp);
                Monster next = null; int best = int.MaxValue;
                foreach (var m in Monsters)
                {
                    if (m.IsDead || m.Ally || struck.Contains(m) || !Map.IsVisible(m.X, m.Y)) continue;
                    int d = Pathfinder.Chebyshev(px, py, m.X, m.Y);
                    if (d <= 4 && d < best) { next = m; best = d; }
                }
                cur = next;
            }
            var pts = points;
            Fx((tl, s) => FxLib.Chain(tl, s, pts, sp.Elem));
        }

        void RecipeScatter(SpellDef sp, int rank)
        {
            var p = Player;
            var pool = new List<Monster>();
            int reach = sp.Range > 0 ? sp.Range : 8;
            foreach (var m in Monsters)
                if (!m.IsDead && !m.Ally && !m.Townsperson && Map.IsVisible(m.X, m.Y) && Pathfinder.Chebyshev(p.X, p.Y, m.X, m.Y) <= reach) pool.Add(m);
            if (pool.Count == 0) { Say("There is nothing in view to strike.", MessageKind.Info); return; }
            int dice = sp.Dice + (sp.Div > 0 ? p.Level / sp.Div : 0);
            for (int i = 0; i < sp.Hits; i++)
            {
                var alive = pool.FindAll(m => !m.IsDead);
                if (alive.Count == 0) break;
                var m = alive[Rng.Range(0, alive.Count)];
                int mx = m.X, my = m.Y;
                int at = i;
                Fx((tl, s) => PlayHit(tl, s + at, sp, mx, my));
                Hurt(m, Rng.Roll(dice, sp.Sides, rank + sp.Flat), sp.Type, sp.Verb ?? "Magic strikes");
                if (!m.IsDead) ApplyRider(m, sp);
            }
        }

        int PlayHit(FxTimeline tl, int s, SpellDef sp, int x, int y)
        {
            switch (sp.HitFx)
            {
                case FxKind.Bolt: { int a = FxLib.Bolt(tl, s, Player.X, Player.Y, x, y, sp.Elem, sp.Glyph); return FxLib.Flash(tl, a, x, y, sp.Elem); }
                case FxKind.Pillar: return FxLib.Pillar(tl, s, x, y, sp.Elem);
                case FxKind.Meteor: return FxLib.Meteor(tl, s, x, y, 1, sp.Elem);
                case FxKind.Zap: return FxLib.Zap(tl, s, Player.X, Player.Y, x, y, sp.Elem);
                default: return FxLib.Flash(tl, s, x, y, sp.Elem);
            }
        }

        // ------------------------------------------------------------ riders

        /// <summary>The status a spell leaves on a creature it hit. A chance to take hold, then (for the mind-and-body kind) a chance to be resisted.</summary>
        void ApplyRider(Monster m, SpellDef sp)
        {
            if (sp.Rider == Rider.None || m == null || m.IsDead) return;
            var p = Player;
            int lvl = CasterLevel, turns = sp.RiderTurns + lvl / 4;
            if (sp.RiderPct < 100 && Rng.Dice(100) > sp.RiderPct) return;
            string n = $"The {m.TheName}";
            bool mindless = m.Def.Undead || m.Def.Mindless;
            switch (sp.Rider)
            {
                case Rider.Burn:
                    SetAlight(m);
                    break;
                case Rider.Slow:
                    if (Resists(m, 20)) { Say($"{n} shakes off the spell.", MessageKind.Info); break; }
                    m.SlowTurns = Math.Max(m.SlowTurns, turns); m.Speed = Math.Max(4, m.Def.Speed / 2);
                    Say($"{n} slows to a crawl.", MessageKind.Good);
                    break;
                case Rider.Fear:
                    if (mindless) { Say($"{n} knows no fear.", MessageKind.Info); break; }
                    if (Resists(m, 20)) { Say($"{n} stands firm.", MessageKind.Info); break; }
                    m.FearTurns = Math.Max(m.FearTurns, turns); m.Asleep = false;
                    Say($"{n} flees in terror!", MessageKind.Good);
                    break;
                case Rider.Sleep:
                    if (mindless) { Say($"{n} is unaffected.", MessageKind.Info); break; }
                    if (Resists(m, 20)) { Say($"{n} shrugs off the spell.", MessageKind.Info); break; }
                    m.Asleep = true; m.SleepTurns = turns + Rng.Range(0, 8); m.Alert = 0;
                    Say($"{n} slumps, asleep.", MessageKind.Good);
                    break;
                case Rider.Confuse:
                    if (Resists(m, 20)) { Say($"{n} shakes off the spell.", MessageKind.Info); break; }
                    m.Confused = true; m.ConfusionTurns = Math.Max(m.ConfusionTurns, turns);
                    Say($"{n} staggers, confused.", MessageKind.Good);
                    break;
                case Rider.Blind:
                    if (m.Def.Skeleton) { Say($"{n} has no eyes to dazzle.", MessageKind.Info); break; }
                    if (Resists(m, 15)) { Say($"{n} blinks it away.", MessageKind.Info); break; }
                    m.Confused = true; m.ConfusionTurns = Math.Max(m.ConfusionTurns, turns);
                    Say($"{n} blunders about, blind.", MessageKind.Good);
                    break;
                case Rider.Stun:
                    if (Resists(m, 15)) { Say($"{n} stays on its feet.", MessageKind.Info); break; }
                    m.HeldTurns = Math.Max(m.HeldTurns, m.BossId != null ? Math.Max(1, turns / 2) : turns); m.Asleep = false;
                    Say($"{n} reels, stunned.", MessageKind.Good);
                    break;
                case Rider.Root:
                    if (Resists(m, 10)) { Say($"{n} tears free.", MessageKind.Info); break; }
                    m.HeldTurns = Math.Max(m.HeldTurns, m.BossId != null ? Math.Max(1, turns / 2) : turns);
                    Say($"{n} is held fast.", MessageKind.Good);
                    break;
                case Rider.Poison:
                    if (m.Def.Undead || m.Def.Skeleton) { Say($"{n} cannot be poisoned.", MessageKind.Info); break; }
                    m.DotTurns = Math.Max(m.DotTurns, turns); m.DotDmg = Math.Max(m.DotDmg, 1 + lvl / 6); m.DotType = DamageType.Poison;
                    Say($"{n} sickens.", MessageKind.Good);
                    break;
                case Rider.Bleed:
                    if (m.Def.Undead || m.Def.Skeleton) { Say($"{n} has no blood to spill.", MessageKind.Info); break; }
                    m.DotTurns = Math.Max(m.DotTurns, turns); m.DotDmg = Math.Max(m.DotDmg, 2 + lvl / 5); m.DotType = DamageType.Physical;
                    Say($"{n} bleeds freely.", MessageKind.Good);
                    break;
                case Rider.Weaken:
                    m.VulnTurns = Math.Max(m.VulnTurns, turns);
                    Say($"{n} is left open to every blow.", MessageKind.Good);
                    break;
                case Rider.Charm:
                    if (mindless) { Say($"{n} has no mind to sway.", MessageKind.Info); break; }
                    if (m.BossId != null || Resists(m, 30, 12)) { Say($"{n} resists.", MessageKind.Info); break; }
                    MakeAlly(m, turns + p.Skills[Skill.Magic] / 2, "charmed ");
                    Say($"The {m.Def.Name} is yours, for now.", MessageKind.Good);
                    break;
            }
        }

        /// <summary>Knocks a creature away from a point (or pulls it toward it, if cells is negative); hitting a wall hurts.</summary>
        void PushMonster(Monster m, int fromX, int fromY, int cells)
        {
            int sx = Math.Sign(m.X - fromX), sy = Math.Sign(m.Y - fromY);
            if (cells < 0) { sx = -sx; sy = -sy; cells = -cells; }
            if (sx == 0 && sy == 0) return;
            for (int k = 0; k < cells; k++)
            {
                int nx = m.X + sx, ny = m.Y + sy;
                if (!Map.InBounds(nx, ny) || !Tiles.Walkable(Map.Get(nx, ny)))
                {
                    if (k < cells) Hurt(m, Rng.Roll(1, 6, 0), DamageType.Physical, "It slams into the wall:", false);
                    return;
                }
                if (MonsterAt(nx, ny) != null || (nx == Player.X && ny == Player.Y)) return;
                m.X = nx; m.Y = ny;
            }
            Map.Version++;
        }

        // ------------------------------------------------------------ specials

        void CleanseSelf()
        {
            var p = Player;
            p.PoisonResist = 0;
            p.Confused = false; p.ConfusionTurns = 0;
            p.Blinded = false; p.BlindTurns = 0;
            p.Hallucinating = false; p.HallucinationTurns = 0;
            p.Stunned = false; p.StunTurns = 0;
            p.BurnTurns = 0;
        }

        void ApplySpecial(SpellDef sp, Monster target, int tx, int ty, int rank)
        {
            var p = Player;
            int lvl = CasterLevel;
            switch (sp.Special)
            {
                case "blink":
                    p.X = tx; p.Y = ty;
                    Map.Version++;
                    Say("The world folds; you step through.", MessageKind.Narrative);
                    UpdateFov();
                    break;
                case "swap":
                    {
                        int ox = p.X, oy = p.Y;
                        p.X = target.X; p.Y = target.Y;
                        target.X = ox; target.Y = oy;
                        Map.Version++;
                        Say($"You and the {target.TheName} trade places.", MessageKind.Narrative);
                        UpdateFov();
                        break;
                    }
                case "banish":
                    if (target.BossId != null || Resists(target, 15)) { Say($"The {target.TheName} will not be moved.", MessageKind.Info); break; }
                    for (int tries = 0; tries < 200; tries++)
                    {
                        int x = Rng.Range(1, Map.W - 1), y = Rng.Range(1, Map.H - 1);
                        if (!Map.Walkable(x, y) || MonsterAt(x, y) != null || Pathfinder.Chebyshev(x, y, p.X, p.Y) < 10) continue;
                        target.X = x; target.Y = y; target.Alert = 0;
                        Say($"The {target.TheName} is torn away.", MessageKind.Good);
                        Map.Version++;
                        return;
                    }
                    Say("The spell finds nowhere to send it.", MessageKind.Info);
                    break;
                case "light":
                    {
                        int r = sp.Radius > 0 ? sp.Radius : 5;
                        for (int dy = -r; dy <= r; dy++)
                            for (int dx = -r; dx <= r; dx++)
                                if (Map.InBounds(p.X + dx, p.Y + dy)) Map.Remember(p.X + dx, p.Y + dy);
                        Say("Light blooms around you.", MessageKind.Good);
                        UpdateFov();
                        break;
                    }
                case "detect-traps":
                case "commune":
                    {
                        int r = sp.Special == "commune" ? 14 : Math.Max(4, sp.Radius * 2);
                        int found = 0;
                        for (int dy = -r; dy <= r; dy++)
                            for (int dx = -r; dx <= r; dx++)
                            {
                                int x = p.X + dx, y = p.Y + dy;
                                if (!Map.InBounds(x, y)) continue;
                                if (sp.Special == "commune") Map.Remember(x, y);
                                if (TrapTable.TryGet(Map.Number, x, y, out _, out _) && !TrapTable.IsRevealed(Map.Number, x, y)) { TrapTable.Reveal(Map.Number, x, y); found++; }
                            }
                        Say(found > 0 ? $"You sense {found} trap{(found == 1 ? "" : "s")} nearby." : sp.Special == "commune" ? "The land shows you its shape." : "No traps nearby.", MessageKind.Good);
                        Map.Version++;
                        break;
                    }
                case "knock":
                    {
                        int r = sp.Radius > 0 ? sp.Radius : 3, n = 0;
                        for (int dy = -r; dy <= r; dy++)
                            for (int dx = -r; dx <= r; dx++)
                            {
                                int x = p.X + dx, y = p.Y + dy;
                                if (Map.InBounds(x, y) && Map.Get(x, y) == TileKind.LockedDoor) { Map.Set(x, y, TileKind.OpenDoor); n++; }
                            }
                        Say(n > 0 ? $"{n} lock{(n == 1 ? "" : "s")} spring open." : "Nothing here is locked.", n > 0 ? MessageKind.Good : MessageKind.Info);
                        if (n > 0) Map.Version++;
                        break;
                    }
                case "identify":
                    foreach (var it in p.Inventory.ToArray()) Learn(it, false);
                    if (p.Wielded != null) p.Wielded.Identified = true;
                    foreach (var piece in p.WornPieces()) piece.Identified = true;
                    for (int i = 0; i < 2; i++) if (p.Rings[i] != null) { p.Rings[i].Identified = true; p.RingKnown[i] = true; }
                    Say("You feel a surge of insight.", MessageKind.Good);
                    break;
                case "dig":
                    {
                        int dx = tx - p.X, dy = ty - p.Y, steps = Math.Max(Math.Abs(dx), Math.Abs(dy)), n = 0;
                        for (int k = 1; k <= sp.Range && steps > 0; k++)
                        {
                            int x = p.X + (int)Math.Round((double)dx * k / steps), y = p.Y + (int)Math.Round((double)dy * k / steps);
                            if (!Map.InBounds(x, y) || x <= 0 || y <= 0 || x >= Map.W - 1 || y >= Map.H - 1) break;
                            var t = Map.Get(x, y);
                            if (Tiles.Walkable(t)) continue;
                            if ((Tiles.Get(t).Flags & TileFlags.Dig) == 0) break;
                            Map.Set(x, y, TileKind.Floor); n++;
                        }
                        Say(n > 0 ? $"The rock crumbles away ({n} cells)." : "There is nothing there to dig.", n > 0 ? MessageKind.Good : MessageKind.Info);
                        Map.Version++;
                        UpdateFov();
                        break;
                    }
                case "life-tap":
                    if (p.HP <= 9) { Say("You are too weak to spare it.", MessageKind.Warn); break; }
                    p.HP -= 8; HurtBy("your own spell");
                    p.Mp = Math.Min(p.MpMax, p.Mp + 8);
                    Say("You burn some of yourself for mana. (+8 Mp, -8 HP)", MessageKind.Info);
                    break;
                case "blood-ritual":
                    {
                        int loss = Math.Min(p.HP - 1, Math.Max(1, p.MaxHP / 4));
                        p.HP -= loss; HurtBy("a ritual of blood");
                        p.Mp = p.MpMax;
                        Say($"Blood runs and the well fills. (-{loss} HP, mana full)", MessageKind.Info);
                        break;
                    }
                case "command-undead":
                    if (!target.Def.Undead) { Say($"The {target.TheName} is not a thing you can command.", MessageKind.Info); break; }
                    if (target.BossId != null || Resists(target, 30, 12)) { Say($"The {target.TheName} will not kneel.", MessageKind.Info); break; }
                    MakeAlly(target, 40 + p.Skills[Skill.Magic] / 2, "bound ");
                    Say($"The {target.Def.Name} kneels to you, for now.", MessageKind.Good);
                    break;
                case "feign-death":
                    p.SetBuff("invisibility", 8); p.Invisible = true;
                    LoseTrack(10);
                    Say("You go still and cold. They lose track of you.", MessageKind.Good);
                    break;
                case "darkness":
                    LoseTrack(8);
                    p.SetBuff("shadow-cloak", 10);
                    Say("The dark thickens around you.", MessageKind.Good);
                    break;
                case "vanish":
                    p.SetBuff("invisibility", 20); p.Invisible = true;
                    p.SetBuff("shadow-cloak", 20);
                    LoseTrack(10);
                    Say("A puff of smoke, and you are gone.", MessageKind.Good);
                    break;
                case "shadow-walk":
                    Say("You step into the dark...", MessageKind.Narrative);
                    TeleportPlayerAway();
                    p.SetBuff("invisibility", 15); p.Invisible = true;
                    break;
                case "execute":
                    if (target.HP * 100 <= 35 * target.MaxHP) Hurt(target, target.HP + 20, DamageType.Necrotic, "A single word unmakes");
                    else Hurt(target, Rng.Roll(5, 6, rank), DamageType.Necrotic, "A word of death strikes");
                    break;
                case "assassinate":
                    {
                        bool unaware = target.Asleep || target.Alert == 0 || target.FearTurns > 0 || target.Confused;
                        int dmg = Rng.Roll(5 + lvl / 3, 6, rank) * (unaware ? 2 : 1);
                        Hurt(target, dmg, DamageType.Physical, unaware ? "You open the throat of" : "You stab");
                        break;
                    }
                case "coup-de-grace":
                    if (target.HP * 100 <= 40 * target.MaxHP) Hurt(target, target.HP + 20, DamageType.Physical, "You finish");
                    else Hurt(target, Rng.Roll(4, 6, rank), DamageType.Physical, "You cut");
                    break;
                case "pickpocket":
                    if (Rng.Dice(100) <= 55 + (p.Dex - 10) * 3 + p.Skills[Skill.Stealth] / 5)
                    {
                        int gold = Rng.Range(2, 8 + target.Level * 4);
                        p.Gold += gold;
                        Say($"You lift {gold} gold off the {target.TheName}.", MessageKind.Good);
                    }
                    else { target.Alert = 1; target.Dormant = false; Say($"The {target.TheName} feels your hand!", MessageKind.Warn); }
                    break;
                case "disarm":
                    {
                        int r = Math.Max(1, sp.Radius), n = 0;
                        for (int dy = -r; dy <= r; dy++)
                            for (int dx = -r; dx <= r; dx++)
                                if (TrapTable.Remove(Map.Number, p.X + dx, p.Y + dy)) n++;
                        Say(n > 0 ? $"{n} trap{(n == 1 ? "" : "s")} fall{(n == 1 ? "s" : "")} apart." : "There is no trap to disarm here.", n > 0 ? MessageKind.Good : MessageKind.Info);
                        Map.Version++;
                        break;
                    }
                case "remedy":
                case "antidote":
                    if (p.PoisonResist > 0) { p.PoisonResist = 0; Say("The poison leaves your blood.", MessageKind.Good); }
                    else Say("You are not poisoned.", MessageKind.Info);
                    break;
                case "second-wind":
                    p.Vigor = p.VigorMax;
                    Say("Your breath comes back to you.", MessageKind.Good);
                    break;
                case "uncurse":
                    {
                        int n = 0;
                        void Lift(Item it) { if (it != null && (it.Def.Flags & ItemFlags.Cursed) != 0) { it.Def.Flags &= ~ItemFlags.Cursed; n++; } }
                        foreach (var it in p.Inventory) Lift(it);
                        Lift(p.Wielded);
                        foreach (var piece in p.WornPieces()) Lift(piece);
                        for (int i = 0; i < 2; i++) Lift(p.Rings[i]);
                        Lift(p.Amulet);
                        p.RefreshGear();
                        Say(n > 0 ? $"A weight lifts: {n} thing{(n == 1 ? " is" : "s are")} no longer cursed." : "Nothing you carry is cursed.", n > 0 ? MessageKind.Good : MessageKind.Info);
                        break;
                    }
                case "restoration":
                    Heal(Rng.Roll(3, 8, (p.Wis - 10) / 2 + lvl));
                    CleanseSelf();
                    break;
                case "mass-cure":
                    {
                        Heal(Rng.Roll(3, 8, (p.Wis - 10) / 2 + lvl / 2));
                        int n = 0;
                        foreach (var m in Monsters)
                            if (m.Ally && !m.IsDead && Pathfinder.Chebyshev(p.X, p.Y, m.X, m.Y) <= Math.Max(3, sp.Radius))
                            {
                                m.HP = Math.Min(m.MaxHP, m.HP + Rng.Roll(3, 8, lvl / 2)); n++;
                            }
                        if (n > 0) Say($"Your allies are mended too ({n}).", MessageKind.Good);
                        break;
                    }
                case "full-heal":
                    p.HP = p.MaxHP;
                    CleanseSelf();
                    p.SetBuff("sanctuary", 6);
                    Say("Heaven takes a hand. You are whole.", MessageKind.Good);
                    break;
                case "goodberries":
                    Heal(Rng.Roll(1, 6, lvl / 2));
                    p.Nutrient = Math.Min(2000, p.Nutrient + 150);
                    break;
                case "charm-beast":
                    if (FactionOf(target) != Faction.Wild) { Say($"The {target.TheName} will not listen to you.", MessageKind.Info); break; }
                    if (Resists(target, 20)) { Say($"The {target.TheName} will have none of it.", MessageKind.Info); break; }
                    MakeAlly(target, 60 + p.Skills[Skill.Magic] / 2, "charmed ");
                    Say($"The {target.Def.Name} falls in beside you.", MessageKind.Good);
                    break;
                case "calm-beasts":
                    {
                        int n = 0;
                        foreach (var m in Monsters)
                        {
                            if (m.IsDead || m.Ally || FactionOf(m) != Faction.Wild || !Map.IsVisible(m.X, m.Y)) continue;
                            if (Pathfinder.Chebyshev(p.X, p.Y, m.X, m.Y) > Math.Max(3, sp.Radius) || Resists(m, 15)) continue;
                            m.Asleep = true; m.SleepTurns = Rng.Range(12, 24); m.Alert = 0; n++;
                        }
                        Say(n > 0 ? $"{n} beast{(n == 1 ? "" : "s")} lie down and sleep." : "No beasts answer.", n > 0 ? MessageKind.Good : MessageKind.Info);
                        break;
                    }
            }
        }

        /// <summary>Spells on you that hurt whatever strikes you (thorns, holy aura, death aura).</summary>
        void Retaliate(Monster m)
        {
            foreach (var kv in new List<KeyValuePair<string, int>>(Player.Buffs))
            {
                var bd = SpellBuffs.Find(kv.Key);
                if (bd == null || bd.RetSides <= 0 || m.IsDead) continue;
                Hurt(m, Rng.Roll(bd.RetDice, bd.RetSides, Player.Level / 4), bd.RetType, "Your " + bd.Label.ToLowerInvariant() + " lashes", false);
            }
        }

        /// <summary>Hostiles in view within a few cells forget where you are.</summary>
        void LoseTrack(int radius)
        {
            foreach (var m in Monsters)
            {
                if (m.IsDead || m.Ally || m.BossId != null || m.Townsperson) continue;
                if (Pathfinder.Chebyshev(Player.X, Player.Y, m.X, m.Y) <= radius) m.Alert = 0;
            }
        }
    }
}
