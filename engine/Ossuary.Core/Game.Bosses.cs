using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>A named boss, as data. Where it waits, what it is built from, and (in <see cref="Game"/>) what it does.</summary>
    public sealed class BossDef
    {
        public string Id, Name, Branch, Base, Intro, Phase2, Fall;
        public int Depth, Level, HP, AC, Speed, Sides, ToHit, Color;
        public char Glyph;
    }

    public static class Bosses
    {
        public static readonly BossDef[] All =
        {
            new BossDef { Id = "gaoler", Name = "Gaoler", Branch = "The Dungeons", Depth = 10, Base = "dread knight", Glyph = 'K', Color = 0xB0B0C8,
                Level = 12, HP = 140, AC = 4, Speed = 11, Sides = 8, ToHit = 6,
                Intro = "Chains drag somewhere ahead. The Gaoler keeps the way out, and he has not opened a cell in a long time.",
                Phase2 = "The Gaoler howls for his hounds.", Fall = "The Gaoler's keys clatter across the stone." },
            new BossDef { Id = "stone-warden", Name = "Stone Warden", Branch = "The Mines of Dwarfdeep", Depth = 8, Base = "ore golem", Glyph = 'G', Color = 0xA89A80,
                Level = 10, HP = 160, AC = 2, Speed = 8, Sides = 12, ToHit = 7,
                Intro = "The floor hums. The Stone Warden never left the post the dwarves gave it.",
                Phase2 = "The Stone Warden cracks, and moves faster.", Fall = "The Stone Warden crumbles into ordinary rock." },
            new BossDef { Id = "rat-king", Name = "Rat King", Branch = "The Warrens", Depth = 9, Base = "giant rat", Glyph = 'r', Color = 0xC08A5A,
                Level = 11, HP = 110, AC = 5, Speed = 12, Sides = 8, ToHit = 6,
                Intro = "Squeaking, everywhere, in the walls. The Rat King is holding court.",
                Phase2 = "The Rat King screams, and the walls scream back.", Fall = "The Rat King dies, and the Warrens go quiet." },
            new BossDef { Id = "drowned-king", Name = "Drowned King", Branch = "The Sunken Vaults", Depth = 12, Base = "tide wraith", Glyph = 'W', Color = 0x70C8D8,
                Level = 15, HP = 180, AC = 3, Speed = 12, Sides = 8, ToHit = 7,
                Intro = "Black water rises to your ankles. The Drowned King has been waiting under it.",
                Phase2 = "The Drowned King lifts his trident. The water answers.", Fall = "The Drowned King sinks, finally, and the water goes still." },
            new BossDef { Id = "annex-warden", Name = "Annex Warden", Branch = "The Annex", Depth = 3, Base = "lich", Glyph = 'E', Color = 0xD0A0F0,
                Level = 20, HP = 230, AC = 2, Speed = 12, Sides = 10, ToHit = 9,
                Intro = "The last room of the Annex is a ledger, and the Warden is reading it aloud: your name, your debts, your sins.",
                Phase2 = "The Annex Warden closes the ledger. The dead in the walls stand up.", Fall = "The Annex Warden crumples, and every debt you owed is struck out at once." },
            new BossDef { Id = "ashen-regent", Name = "Ashen Regent", Branch = "The Ashen Spire", Depth = 15, Base = "fire giant", Glyph = 'H', Color = 0xFF7A30,
                Level = 18, HP = 260, AC = 3, Speed = 12, Sides = 10, ToHit = 8,
                Intro = "The air at the top of the Spire is thick enough to chew. The Ashen Regent is not hungry. It is angry.",
                Phase2 = "The Ashen Regent splits its crown, and the cinders stand up.", Fall = "The Ashen Regent comes apart like a log in a fire." },
        };

        public static BossDef ForLevel(string branch, int depth)
        {
            foreach (var b in All) if (b.Branch == branch && b.Depth == depth) return b;
            return null;
        }

        public static BossDef Find(string id)
        {
            foreach (var b in All) if (b.Id == id) return b;
            return null;
        }
    }

    public sealed partial class Game
    {
        /// <summary>Ids of the bosses the hero has killed this run.</summary>
        public readonly HashSet<string> BossesSlain = new HashSet<string>();

        /// <summary>Builds a boss at (x, y). Its health, speed and blows come from the def; its habits from <see cref="BossTurn"/>.</summary>
        public Monster CreateBoss(BossDef b, int x, int y)
        {
            var def = Bestiary.Find(b.Base);
            def.Name = b.Name; def.Glyph = b.Glyph; def.Color = b.Color;
            def.Level = b.Level; def.HP = b.HP; def.AC = b.AC; def.Speed = b.Speed;
            def.Ai = AiKind.Hunt; def.Branch = null; def.Trait = null; def.Explodes = false; def.Difficulty = b.Level + 6;
            def.Attacks = new[] { AttackKind.Hit, AttackKind.Kick };
            def.DmgDice = new[] { 2, 1 }; def.DmgSides = new[] { b.Sides, b.Sides / 2 + 2 }; def.ToHit = new[] { b.ToHit, b.ToHit - 1 };
            def.Vision = 14; def.Carries = null; def.CarryWeights = null; def.CorpseValue = 0; def.Regenerates = false;
            var m = new Monster(def, Rng) { X = x, Y = y, HomeX = x, HomeY = y, Depth = Map != null ? Map.Depth : 1, Unique = true, BossId = b.Id, Alert = 0 };
            m.HP = m.MaxHP = b.HP;
            m.XpKill = b.Level * b.Level * 2;
            return m;
        }

        /// <summary>Called when a level is built for the first time: its boss, if it has one, waits far from the stairs.</summary>
        void RaiseBosses(List<LevelBuilder.SpawnPoint> spawns, int startX, int startY)
        {
            var b = Bosses.ForLevel(Branch, Depth);
            if (b == null || BossesSlain.Contains(b.Id)) return;
            var rng = new Rng(Rng.Seed ^ (ulong)(b.Id.GetHashCode() * 17 + 99) ^ 0x424F5353UL);
            int best = -1, bestD = -1;
            for (int tries = 0; tries < 400; tries++)
            {
                int x = rng.Range(1, Map.W - 1), y = rng.Range(1, Map.H - 1);
                if (Map.Get(x, y) != TileKind.Floor && Map.Get(x, y) != TileKind.FloorAlt) continue;
                if (MonsterAt(x, y) != null) continue;
                int d = Pathfinder.Chebyshev(x, y, startX, startY);
                if (d > bestD && d >= 12) { best = x + y * Map.W; bestD = d; if (d >= 28) break; }
            }
            if (best < 0) return;
            var boss = CreateBoss(b, best % Map.W, best / Map.W);
            spawns?.Add(new LevelBuilder.SpawnPoint { X = boss.X, Y = boss.Y, Monster = boss });
            Monsters.Add(boss);
            Say(b.Intro, MessageKind.Narrative);
        }

        int BossPhase(Monster m) => m.HP * 2 < m.MaxHP ? 2 : 1;

        /// <summary>
        /// A boss's own turn, run before it moves or strikes. Returns true when the boss spent the turn on something
        /// other than the plain chase. Cooldowns are counted in the boss's own turns (<see cref="Monster.BossClock"/>).
        /// </summary>
        bool BossTurn(Monster m, int dist)
        {
            var b = Bosses.Find(m.BossId);
            if (b == null) return false;
            if (m.BossPhase == 0) m.BossPhase = 1;
            if (BossPhase(m) == 2 && m.BossPhase == 1)
            {
                m.BossPhase = 2;
                Say(b.Phase2, MessageKind.Warn);
                if (b.Id == "stone-warden" || b.Id == "rat-king") m.Speed = m.Def.Speed + 4;
            }
            bool angry = m.BossPhase == 2;
            int clock = ++m.BossClock;
            bool seen = Map.IsCurrentlyVisible(m.X, m.Y);
            if (m.Alert == 0 && dist > 12) return false;

            switch (b.Id)
            {
                case "gaoler":
                    if (clock % 5 == 0 && dist >= 2 && dist <= 6 && seen && Fov.HasLine(Map, m.X, m.Y, Player.X, Player.Y) && HookPlayer(m)) return true;
                    if (angry && clock % 7 == 0 && CountNamed("gaol hound") < 4) { SummonHostile(m, "gaol hound", 2); return true; }
                    break;
                case "stone-warden":
                    if (clock % 6 == 0 && dist <= 3)
                    {
                        int dmg = Player.ResistDamage(Rng.Roll(2, 8, 0), DamageType.Physical);
                        Say("The Stone Warden slams the floor. The ground jumps.", MessageKind.Combat);
                        if (dist <= 2) { Player.HP -= dmg; HurtBy("the Stone Warden"); Say($"The shockwave throws you down. (-{dmg})", MessageKind.Bad); Player.StunTurns = Math.Max(Player.StunTurns, 1); Player.Stunned = true; CheckDeath(); }
                        return true;
                    }
                    break;
                case "rat-king":
                    if (clock % 4 == 0 && CountRats() < 6) { SummonHostile(m, angry ? "plague rat" : "rat swarm", 2); return true; }
                    break;
                case "drowned-king":
                    if (clock % (angry ? 4 : 6) == 0 && seen)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                int x = Player.X + dx, y = Player.Y + dy;
                                if (Map.InBounds(x, y) && (Map.Get(x, y) == TileKind.Floor || Map.Get(x, y) == TileKind.FloorAlt) && Map.SurfaceAt(x, y) != SurfaceKind.Fire) PutSurface(x, y, SurfaceKind.Water, 30);
                            }
                        Say("The Drowned King beats his trident on the stone. Black water spreads.", MessageKind.Warn);
                        return true;
                    }
                    if (clock % 3 == 0 && seen && Map.SurfaceAt(Player.X, Player.Y) == SurfaceKind.Water && dist <= 7)
                    {
                        int dmg = Player.ResistDamage(Rng.Roll(2, 6, 0), DamageType.Lightning);
                        Player.HP -= dmg; HurtBy("the Drowned King");
                        Say($"Lightning leaps from the Drowned King's trident into the water at your feet. (-{dmg})", MessageKind.Bad);
                        CheckDeath();
                        return true;
                    }
                    break;
                case "annex-warden":
                    if (clock % 5 == 0 && dist <= 7 && seen && Fov.HasLine(Map, m.X, m.Y, Player.X, Player.Y))
                    {
                        int dmg = Player.ResistDamage(Rng.Roll(2, 6, 0), DamageType.Necrotic);
                        Player.HP -= dmg; HurtBy("the Annex Warden");
                        m.HP = Math.Min(m.MaxHP, m.HP + dmg);
                        Say($"The Warden reads out a debt of yours, and takes it in blood. (-{dmg})", MessageKind.Bad);
                        AddCorruption(1, null);
                        CheckDeath();
                        return true;
                    }
                    if (angry && clock % 8 == 0 && CountNamed("skeleton") < 4) { SummonHostile(m, "skeleton", 2); return true; }
                    break;
                case "ashen-regent":
                    if (clock % 5 == 0 && dist <= 5 && seen)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                int x = m.X + dx, y = m.Y + dy;
                                if (Map.InBounds(x, y) && Tiles.Walkable(Map.Get(x, y)) && Map.SurfaceAt(x, y) != SurfaceKind.Water) PutSurface(x, y, SurfaceKind.Fire, 4);
                            }
                        Say("The Ashen Regent exhales. The floor around it ignites.", MessageKind.Warn);
                        if (dist <= 2) { int dmg = Player.ResistDamage(Rng.Roll(2, 6, 0), DamageType.Fire); Player.HP -= dmg; HurtBy("the Ashen Regent"); Say($"The blast sears you. (-{dmg})", MessageKind.Bad); SetAlight(Player); CheckDeath(); }
                        return true;
                    }
                    if (angry && clock % 8 == 0 && CountNamed("ember wisp") < 4) { SummonHostile(m, "ember wisp", 2); return true; }
                    break;
            }
            return false;
        }

        /// <summary>The Gaoler's chain: you are dragged to the cell beside him.</summary>
        bool HookPlayer(Monster m)
        {
            int bx = -1, by = -1, bd = int.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                int nx = m.X + Pathfinder.Dx8[k], ny = m.Y + Pathfinder.Dy8[k];
                if (!FreeCell(nx, ny) && !(nx == Player.X && ny == Player.Y)) continue;
                int d = Pathfinder.Chebyshev(nx, ny, Player.X, Player.Y);
                if (d < bd) { bd = d; bx = nx; by = ny; }
            }
            if (bx < 0) return false;
            Player.X = bx; Player.Y = by;
            Say("The Gaoler's chain bites into you and drags you in!", MessageKind.Bad);
            Map.Version++;
            UpdateFov();
            return true;
        }

        int CountRats()
        {
            int n = 0;
            foreach (var o in Monsters) if (!o.IsDead && !o.Ally && o.Def.Name.Contains("rat")) n++;
            return n;
        }

        /// <summary>Calls hostile creatures out of the dark beside the boss.</summary>
        void SummonHostile(Monster boss, string kind, int count)
        {
            int placed = 0;
            for (int ring = 1; ring <= 2 && placed < count; ring++)
                for (int dy = -ring; dy <= ring && placed < count; dy++)
                    for (int dx = -ring; dx <= ring && placed < count; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                        int x = boss.X + dx, y = boss.Y + dy;
                        if (!FreeCell(x, y)) continue;
                        var def = Bestiary.Find(kind);
                        var m = new Monster(def, Rng) { X = x, Y = y, HomeX = x, HomeY = y, Depth = boss.Depth, Alert = 1 };
                        Monsters.Add(m);
                        placed++;
                    }
            if (placed > 0) Say($"{(placed == 1 ? "A " + kind : placed + " " + kind + "s")} answer{(placed == 1 ? "s" : "")} the {boss.Name}.", MessageKind.Warn);
            Map.Version++;
        }

        /// <summary>The boss is dead: its last words, and what it was guarding.</summary>
        void BossFalls(Monster m)
        {
            var b = Bosses.Find(m.BossId);
            if (b == null) return;
            BossesSlain.Add(b.Id);
            Say(b.Fall, MessageKind.Quest);
            Player.Gold += 150 + 40 * b.Level;
            foreach (string name in new[] { "potion of full healing", "potion of gain ability" })
                foreach (var d in Catalogue.Potions)
                    if (d.Name == name) GroundItems.Add(Map.Number, m.X, m.Y, new Item(d, Rng, NextUid()) { Identified = true });
            Say($"It was guarding {150 + 40 * b.Level} gold and two potions.", MessageKind.Good);
        }
    }
}
