using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>What each spell does. One method per family; <see cref="ApplySpell"/> dispatches by id.</summary>
    public sealed partial class Game
    {
        void ApplySpell(SpellDef spell, Monster target, int tx, int ty)
        {
            var p = Player;
            int lvl = p.Level;
            int rank = SkillRanks.Rank(p.Skills[Skill.Magic]);
            switch (spell.Id)
            {
                // ---- Evocation
                case "magic-missile": Hurt(target, Rng.Roll(2 + lvl / 3, 4, rank), DamageType.Physical, "Bolts of force strike"); break;
                case "shocking-grasp": Hurt(target, Rng.Roll(2 + lvl / 3, 6, rank), DamageType.Lightning, "Lightning arcs into"); break;
                case "frost-ray": Hurt(target, Rng.Roll(3 + lvl / 3, 4, rank), DamageType.Cold, "A ray of frost hits"); break;
                case "fireball": Burst(tx, ty, spell.Radius, Rng.Roll(3 + lvl / 3, 6, rank), DamageType.Fire, "Fire engulfs"); break;
                case "ice-lance": Hurt(target, Rng.Roll(2 + lvl / 3, 6, rank), DamageType.Cold, "A lance of ice pierces"); break;
                case "steam-burst": SteamBurst(tx, ty, spell.Radius, Rng.Roll(2 + lvl / 3, 6, rank)); break;
                case "meteor": Burst(tx, ty, spell.Radius, Rng.Roll(6 + lvl / 2, 6, rank), DamageType.Fire, "A meteor crushes"); break;
                case "lightning-bolt": Bolt(tx, ty, spell.Range, Rng.Roll(3 + lvl / 3, 6, rank), DamageType.Lightning); break;
                case "chain-lightning": Chain(target, 3 + lvl / 4, rank); break;
                case "wall-of-fire":
                    {
                        int[] ox = { 0, 1, -1, 0, 0 }, oy = { 0, 0, 0, 1, -1 };
                        for (int i = 0; i < 5; i++) PutSurface(tx + ox[i], ty + oy[i], SurfaceKind.Fire, 8);
                        Say("A wall of flame roars up.", MessageKind.Combat);
                        break;
                    }
                case "create-water":
                    for (int dy = -spell.Radius; dy <= spell.Radius; dy++)
                        for (int dx = -spell.Radius; dx <= spell.Radius; dx++)
                            if (Map.IsVisible(tx + dx, ty + dy)) PutSurface(tx + dx, ty + dy, SurfaceKind.Water, 60);
                    Say("Water wells up from the stone.", MessageKind.Good);
                    break;

                // ---- Conjuration
                case "familiar": SummonAllies(lvl < 6 ? "jackal" : "giant rat", 1, 80 + p.Skills[Skill.Magic]); break;
                case "summon-beast": SummonAllies(lvl < 5 ? "cave spider" : lvl < 9 ? "jackal warden" : "ogre", 1, 60 + p.Skills[Skill.Magic]); break;
                case "create-oil":
                    for (int dy = -spell.Radius; dy <= spell.Radius; dy++)
                        for (int dx = -spell.Radius; dx <= spell.Radius; dx++)
                            if (Map.IsVisible(tx + dx, ty + dy)) PutSurface(tx + dx, ty + dy, SurfaceKind.Oil);
                    Say("Black oil seeps across the floor.", MessageKind.Info);
                    break;
                case "blink":
                    p.X = tx; p.Y = ty;
                    Map.Version++;
                    Say("The world folds; you step through.", MessageKind.Narrative);
                    UpdateFov();
                    break;
                case "teleport":
                    Say("Space wrenches around you...", MessageKind.Narrative);
                    TeleportPlayerAway();
                    break;

                // ---- Alteration
                case "ward": p.SetBuff("ward", 40 + p.Skills[Skill.Magic] / 2); Say("A pale shimmer wraps around you. (AC +3)", MessageKind.Good); break;
                case "stone-skin": p.SetBuff("stone-skin", 60 + p.Skills[Skill.Magic] / 2); Say("Your skin turns to stone. (AC +6)", MessageKind.Good); break;
                case "haste": p.SetBuff("haste", 20 + p.Skills[Skill.Magic] / 5); Say("The world slows around you.", MessageKind.Good); break;
                case "clairvoyance":
                    for (int y = 0; y < Map.H; y++)
                        for (int x = 0; x < Map.W; x++) Map.Remember(x, y);
                    Say("The shape of this level floods your mind.", MessageKind.Good);
                    break;
                case "slow":
                    if (Resists(target, 20)) Say($"The {target.TheName} shakes off the spell.", MessageKind.Info);
                    else
                    {
                        target.SlowTurns = 15 + lvl;
                        target.Speed = Math.Max(4, target.Def.Speed / 2);
                        Say($"The {target.TheName} slows to a crawl.", MessageKind.Good);
                    }
                    break;

                // ---- Illusion
                case "sleep":
                    if (target.Def.Undead || target.Def.Mindless) Say($"The {target.TheName} is unaffected.", MessageKind.Info);
                    else if (Resists(target, 20)) Say($"The {target.TheName} shrugs off the spell.", MessageKind.Info);
                    else
                    {
                        target.Asleep = true; target.SleepTurns = Rng.Range(10, 26); target.Alert = 0;
                        Say($"The {target.TheName} slumps, asleep.", MessageKind.Good);
                    }
                    break;
                case "confuse":
                    if (Resists(target, 20)) Say($"The {target.TheName} shakes off the spell.", MessageKind.Info);
                    else
                    {
                        target.Confused = true; target.ConfusionTurns = Rng.Range(8, 17);
                        Say($"The {target.TheName} staggers, confused.", MessageKind.Good);
                    }
                    break;
                case "invisibility":
                    p.SetBuff("invisibility", 30 + p.Skills[Skill.Magic] / 3);
                    p.Invisible = true;
                    Say("You fade from sight.", MessageKind.Good);
                    break;
                case "charm":
                    if (target.Def.Undead || target.Def.Mindless) Say($"The {target.TheName} has no mind to sway.", MessageKind.Info);
                    else if (Resists(target, 30, 12)) Say($"The {target.TheName} resists your charm.", MessageKind.Info);
                    else
                    {
                        MakeAlly(target, 40 + p.Skills[Skill.Magic] / 2, "charmed ");
                        Say($"The {target.Def.Name} is yours, for now.", MessageKind.Good);
                    }
                    break;

                // ---- Necromancy
                case "drain-life":
                    {
                        int dealt = Hurt(target, Rng.Roll(3 + lvl / 3, 6, rank), DamageType.Necrotic, "Life tears out of");
                        if (dealt > 0)
                        {
                            int heal = Math.Min(p.MaxHP - p.HP, Math.Max(1, dealt / 2));
                            if (heal > 0) { p.HP += heal; Say($"You drink in {heal} life.", MessageKind.Good); }
                        }
                        break;
                    }
                case "raise-skeleton": SummonAllies("skeleton", 1, 150); break;
                case "ossify":
                    p.SetBuff("ossify", 50 + p.Skills[Skill.Magic] / 2);
                    Say("Bone creeps over your skin. (AC +4)", MessageKind.Good);
                    AddCorruption(2, null);
                    break;
                case "reshape-flesh":
                    if (!GainMutation()) Say("There is nothing left in you to reshape.", MessageKind.Info);
                    AddCorruption(10, null);
                    break;
                case "marrow-bolt":
                    Hurt(target, Rng.Roll(4 + lvl / 3, 6, rank), DamageType.Necrotic, "A marrow spike drives into");
                    AddCorruption(2, null);
                    break;
                case "army-of-bones": SummonAllies("skeleton", 3, 120); break;
                case "fear":
                    if (target.Def.Undead || target.Def.Mindless) Say($"The {target.TheName} knows no fear.", MessageKind.Info);
                    else if (Resists(target, 20)) Say($"The {target.TheName} stands firm.", MessageKind.Info);
                    else
                    {
                        target.FearTurns = 12 + lvl; target.Asleep = false;
                        Say($"The {target.TheName} flees in terror!", MessageKind.Good);
                    }
                    break;
                case "finger-of-death": Hurt(target, Rng.Roll(8 + lvl / 2, 6, rank), DamageType.Necrotic, "A pointed finger unmakes"); break;

                // ---- Sacred
                case "cure-wounds": Heal(Rng.Roll(2, 6, (p.Wis - 10) / 2 + lvl / 2)); break;
                case "greater-heal": Heal(Rng.Roll(4, 8, (p.Wis - 10) + lvl)); break;
                case "bless": p.SetBuff("bless", 60 + p.Skills[Skill.Magic] / 2); Say("A calm certainty steadies your hand. (+2 to hit)", MessageKind.Good); break;
                case "smite": Hurt(target, Rng.Roll(3 + lvl / 3, 6, rank), DamageType.Holy, "Radiance scours"); break;
                case "purify":
                    p.Corruption = Math.Max(0, p.Corruption - 15);
                    Say("A clean light burns the Ossuary out of you. (-15 corruption)", MessageKind.Good);
                    break;
                case "cleanse":
                    p.PoisonResist = 0;
                    p.Confused = false; p.ConfusionTurns = 0;
                    p.Blinded = false; p.BlindTurns = 0;
                    p.Hallucinating = false; p.HallucinationTurns = 0;
                    Say("A clean light burns the sickness out of you.", MessageKind.Good);
                    break;
                case "turn-undead":
                    {
                        int turned = 0;
                        foreach (var m in new List<Monster>(Monsters))
                        {
                            if (m.IsDead || m.Ally || !m.Def.Undead || !Map.IsVisible(m.X, m.Y)) continue;
                            if (Pathfinder.Chebyshev(p.X, p.Y, m.X, m.Y) > 8) continue;
                            m.FearTurns = 15 + lvl; m.Asleep = false;
                            Hurt(m, Rng.Roll(2, 6, rank), DamageType.Holy, "Holy light sears");
                            turned++;
                        }
                        if (turned == 0) Say("No undead stir in your sight.", MessageKind.Info);
                        break;
                    }
                case "revive":
                    p.SetBuff("revive", 300);
                    Say("Your soul is bound to this flesh. (the next death is undone)", MessageKind.Good);
                    break;
            }
        }

        // ------------------------------------------------------------ helpers

        /// <summary>Spell resist roll: base percent plus per-level difference between target and caster.</summary>
        bool Resists(Monster m, int basePct, int perLevel = 10)
        {
            int resist = Math.Max(5, Math.Min(90, basePct + (m.Def.Level - Player.Level) * perLevel));
            return Rng.Dice(100) <= resist;
        }

        void Heal(int amount)
        {
            int before = Player.HP;
            Player.HP = Math.Min(Player.MaxHP, Player.HP + Math.Max(1, amount));
            Say($"Warmth knits your wounds (+{Player.HP - before} HP).", MessageKind.Good);
        }

        /// <summary>Damages a monster. The dead shrug off necrotic harm and take double holy. Returns damage dealt.</summary>
        int Hurt(Monster m, int dmg, DamageType type, string verb, bool spell = true)
        {
            if (m == null || m.IsDead) return 0;
            if (type == DamageType.Necrotic && m.Def.Undead) { Say($"The {m.TheName} is unmoved by death magic.", MessageKind.Info); return 0; }
            if (type == DamageType.Holy && m.Def.Undead) dmg *= 2;
            int res = MonsterResist(m, type);
            if (res >= 100) { Say($"The {m.TheName} is unharmed.", MessageKind.Info); return 0; }
            dmg = Math.Max(1, dmg + (spell ? 2 * Player.PerkRank("spell-power") : 0));
            if (res != 0) dmg = Math.Max(1, dmg * (100 - res) / 100);
            if (m.WetTurns > 0 && (type == DamageType.Cold || type == DamageType.Lightning)) dmg = dmg * 3 / 2;
            m.HP -= dmg;
            m.Asleep = false;
            if (m.HP <= 0)
            {
                Say($"{verb} the {m.TheName} for {dmg}, and it dies.", MessageKind.Kill);
                _killType = type; _killSneak = false;
                KillMonster(m);
            }
            else
            {
                Say($"{verb} the {m.TheName} for {dmg} damage.", MessageKind.Combat);
                if (!m.Ally) { m.Alert = 1; m.Dormant = false; }
            }
            Aftermath(m, type, dmg, res);
            return dmg;
        }

        /// <summary>What an elemental hit does beyond its damage: ignites, chills and freezes, or runs through water.</summary>
        void Aftermath(Monster m, DamageType type, int dmg, int res)
        {
            switch (type)
            {
                case DamageType.Fire:
                    if (!m.IsDead) SetAlight(m);
                    break;
                case DamageType.Cold:
                    if (m.IsDead || res >= 50) break;
                    m.SlowTurns = Math.Max(m.SlowTurns, 6 + (m.WetTurns > 0 ? 4 : 0));
                    m.Speed = Math.Max(4, m.Def.Speed / 2);
                    if (SurfaceAt(m.X, m.Y) == SurfaceKind.Water)
                    {
                        PutSurface(m.X, m.Y, SurfaceKind.Ice, 40);
                        m.Energy -= 24;
                        Say($"The {m.TheName} freezes to the spot.", MessageKind.Good);
                    }
                    break;
                case DamageType.Lightning:
                    Conduct(m.X, m.Y, Math.Max(1, dmg / 2), m);
                    break;
            }
        }

        /// <summary>Everything hostile within radius of a cell and in your view takes the same roll.</summary>
        void Burst(int cx, int cy, int radius, int dmg, DamageType type, string verb)
        {
            var hit = new List<Monster>();
            foreach (var m in Monsters)
                if (!m.IsDead && !m.Ally && Pathfinder.Chebyshev(cx, cy, m.X, m.Y) <= radius && Map.IsVisible(m.X, m.Y)) hit.Add(m);
            if (hit.Count == 0) Say("The blast lands on empty ground.", MessageKind.Info);
            foreach (var m in hit) Hurt(m, dmg, type, verb);
            if (type == DamageType.Fire) ScorchGround(cx, cy, radius, radius >= 3);
        }

        /// <summary>Boils the water in a burst: scalding for anything wet, a hiss for anything dry.</summary>
        void SteamBurst(int cx, int cy, int radius, int dmg)
        {
            bool water = false;
            var hit = new List<Monster>();
            foreach (var m in Monsters)
                if (!m.IsDead && !m.Ally && Pathfinder.Chebyshev(cx, cy, m.X, m.Y) <= radius && Map.IsVisible(m.X, m.Y)) hit.Add(m);
            foreach (var m in hit)
            {
                bool wet = Map.SurfaceAt(m.X, m.Y) == SurfaceKind.Water || m.WetTurns > 0;
                Hurt(m, wet ? dmg * 3 / 2 : Math.Max(1, dmg / 3), DamageType.Fire, wet ? "Scalding steam sears" : "Steam hisses over");
            }
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (Map.InBounds(x, y) && Map.SurfaceAt(x, y) == SurfaceKind.Water) { Map.SetSurface(x, y, SurfaceKind.None); water = true; }
                }
            Say(water ? "The water boils away in a scream of steam." : "Steam hisses up from the stone, and thins to nothing.", water ? MessageKind.Combat : MessageKind.Info);
            Map.Version++;
        }

        /// <summary>A piercing line from you through (tx, ty), out to range, stopped by walls.</summary>
        void Bolt(int tx, int ty, int range, int dmg, DamageType type)
        {
            int dx = tx - Player.X, dy = ty - Player.Y;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            var hit = new List<Monster>();
            for (int k = 1; k <= range; k++)
            {
                int x = Player.X + (int)Math.Round((double)dx * k / steps);
                int y = Player.Y + (int)Math.Round((double)dy * k / steps);
                if (!Map.InBounds(x, y) || !Tiles.Walkable(Map.Get(x, y))) break;
                var m = MonsterAt(x, y);
                if (m != null && !m.IsDead && !m.Ally) hit.Add(m);
            }
            if (hit.Count == 0) Say("The bolt crackles away down the passage.", MessageKind.Info);
            foreach (var m in hit) Hurt(m, dmg, type, "Lightning tears through");
        }

        /// <summary>Hits the target, then leaps to up to three more hostiles within 4 cells of the last.</summary>
        void Chain(Monster first, int dice, int rank)
        {
            var struck = new HashSet<Monster>();
            var cur = first;
            for (int jump = 0; jump < 4 && cur != null; jump++)
            {
                struck.Add(cur);
                int px = cur.X, py = cur.Y;
                Hurt(cur, Rng.Roll(dice, 6, rank), DamageType.Lightning, jump == 0 ? "Lightning strikes" : "The lightning leaps to");
                Monster next = null; int best = int.MaxValue;
                foreach (var m in Monsters)
                {
                    if (m.IsDead || m.Ally || struck.Contains(m) || !Map.IsVisible(m.X, m.Y)) continue;
                    int d = Pathfinder.Chebyshev(px, py, m.X, m.Y);
                    if (d <= 4 && d < best) { next = m; best = d; }
                }
                cur = next;
            }
        }

        // ------------------------------------------------------------ allies

        int CountFreeCellsNear(int needed)
        {
            int n = 0;
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (FreeCell(Player.X + dx, Player.Y + dy)) n++;
                    if (n >= needed) return n;
                }
            return n;
        }

        bool FreeCell(int x, int y) =>
            Map.InBounds(x, y) && Tiles.Walkable(Map.Get(x, y)) && MonsterAt(x, y) == null && !(x == Player.X && y == Player.Y);

        void SummonAllies(string defName, int count, int turns)
        {
            int placed = 0;
            for (int ring = 1; ring <= 2 && placed < count; ring++)
                for (int dy = -ring; dy <= ring && placed < count; dy++)
                    for (int dx = -ring; dx <= ring && placed < count; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                        int x = Player.X + dx, y = Player.Y + dy;
                        if (!FreeCell(x, y)) continue;
                        var m = new Monster(Bestiary.Find(defName), Rng) { X = x, Y = y, HomeX = x, HomeY = y, Depth = Map.Depth };
                        MakeAlly(m, turns, "allied ");
                        Monsters.Add(m);
                        placed++;
                    }
            Say(placed == 1 ? $"A {Bestiary.Find(defName).Name} answers your call." : $"{placed} {Bestiary.Find(defName).Name}s answer your call.", MessageKind.Good);
        }

        void MakeAlly(Monster m, int turns, string prefix)
        {
            m.Ally = true;
            m.SummonTurns = Math.Max(1, turns);
            m.Alert = 0; m.Dormant = false; m.Asleep = false; m.FearTurns = 0;
            m.Name = prefix + m.Def.Name;
        }

        /// <summary>An ally's turn: fight the nearest hostile it can see, else keep close to you.</summary>
        void AllyTurn(Monster a)
        {
            Monster foe = null; int best = int.MaxValue;
            foreach (var m in Monsters)
            {
                if (m.IsDead || m.Ally || m.Def.Level == 0) continue;
                if (!Map.IsVisible(m.X, m.Y)) continue;
                int d = Pathfinder.Chebyshev(a.X, a.Y, m.X, m.Y);
                if (d <= 8 && d < best) { foe = m; best = d; }
            }
            if (foe != null)
            {
                if (best == 1)
                {
                    var res = Battles.MeleeAttack(a, foe, Rng);
                    if (res.Hit) foe.Asleep = false;
                    Say(res.Hit ? $"The {a.Name} hits the {foe.TheName} for {res.Damage}." : $"The {a.Name} misses the {foe.TheName}.", MessageKind.Combat);
                    if (res.Killed) { _killByAlly = true; KillMonster(foe); }
                    else if (res.Hit) { foe.Alert = 1; foe.Dormant = false; }
                    return;
                }
                StepToward(a, foe.X, foe.Y);
                return;
            }
            if (Pathfinder.Chebyshev(a.X, a.Y, Player.X, Player.Y) > 2) StepToward(a, Player.X, Player.Y);
        }

        void StepToward(Monster m, int gx, int gy)
        {
            int bx = 0, by = 0, bestD = Pathfinder.Chebyshev(m.X, m.Y, gx, gy);
            for (int k = 0; k < 8; k++)
            {
                int nx = m.X + Pathfinder.Dx8[k], ny = m.Y + Pathfinder.Dy8[k];
                if (!Map.CanStep(m.X, m.Y, nx, ny, true) || MonsterAt(nx, ny) != null || (nx == Player.X && ny == Player.Y) || AvoidsCell(m, nx, ny)) continue;
                int d = Pathfinder.Chebyshev(nx, ny, gx, gy);
                if (d < bestD) { bestD = d; bx = Pathfinder.Dx8[k]; by = Pathfinder.Dy8[k]; }
            }
            if (bx != 0 || by != 0) { m.X += bx; m.Y += by; Map.Version++; }
        }

        /// <summary>A monster in terror moves to the cell farthest from you, or fights back if cornered.</summary>
        bool FleeStep(Monster m)
        {
            int bx = 0, by = 0, bestD = Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y);
            for (int k = 0; k < 8; k++)
            {
                int nx = m.X + Pathfinder.Dx8[k], ny = m.Y + Pathfinder.Dy8[k];
                if (!Map.CanStep(m.X, m.Y, nx, ny, true) || MonsterAt(nx, ny) != null || (nx == Player.X && ny == Player.Y) || AvoidsCell(m, nx, ny)) continue;
                int d = Pathfinder.Chebyshev(nx, ny, Player.X, Player.Y);
                if (d > bestD) { bestD = d; bx = Pathfinder.Dx8[k]; by = Pathfinder.Dy8[k]; }
            }
            if (bx == 0 && by == 0) return false;
            m.X += bx; m.Y += by; Map.Version++;
            return true;
        }

        /// <summary>A hostile next to one of your allies (and not next to you) turns on it.</summary>
        bool AttackAdjacentAlly(Monster m)
        {
            foreach (var a in Monsters)
            {
                if (!a.Ally || a.IsDead || Pathfinder.Chebyshev(m.X, m.Y, a.X, a.Y) != 1) continue;
                var res = Battles.MeleeAttack(m, a, Rng);
                if (res.Hit) Say($"The {m.Name} hits the {a.Name}.", MessageKind.Combat);
                if (res.Killed) { Say($"The {a.Name} is destroyed.", MessageKind.Info); a.HP = 0; Monsters.Remove(a); Map.Version++; }
                return true;
            }
            return false;
        }

        /// <summary>Per-turn upkeep of spell timers on monsters: summons expire, fear and slow wear off.</summary>
        void TickMonsterEffects(Monster m)
        {
            if (m.Def.Trait != null) TraitTick(m);
            if (m.SlowTurns > 0 && --m.SlowTurns == 0) m.Speed = m.Def.Speed;
            if (m.FearTurns > 0) m.FearTurns--;
            if (m.Ally && m.SummonTurns > 0 && --m.SummonTurns == 0)
            {
                bool charmed = m.Name.StartsWith("charmed ", StringComparison.Ordinal);
                if (charmed) { m.Ally = false; m.Name = m.Def.Name; m.Alert = 1; Say($"The {m.Name} shakes off your charm.", MessageKind.Warn); }
                else { m.HP = 0; Say($"The {m.Name} fades away.", MessageKind.Info); }
                Map.Version++;
            }
        }
    }
}
