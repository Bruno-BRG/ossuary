using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    public enum GameMode
    {
        Dungeon,
        Overworld,
        TownMap,
        GameOver,
        Won,
    }

    /// <summary>
    /// The whole simulation. Turn-based: the player's action resolves, then every
    /// monster accumulates energy and acts when its meter fills, which gives
    /// NetHack's fast-monsters-are-lethal pacing without real-time timing.
    /// </summary>
    public sealed partial class Game
    {
        public Rng Rng;
        public Player Player;
        public Dungeon Dungeon;
        public Overworld World;

        public GameMode Mode = GameMode.Dungeon;
        public GameMap Map;
        public readonly List<Monster> Monsters = new List<Monster>();
        public int Turn;
        public string Branch = "The Dungeons";
        public int Depth = 1;
        public Town Town;
        public GameMap TownMap;

        public readonly List<Message> Log = new List<Message>();
        public readonly List<Message> Transcript = new List<Message>();
        /// <summary>Messages ever said; unlike the capped logs it only grows, so it detects "something was reported".</summary>
        public long Said;

        public readonly UiState UiState = new UiState();
        public readonly UiRequests UiRequests = new UiRequests();
        public readonly ChoiceRequest PendingChoice = new ChoiceRequest();
        public int FacingX = 1, FacingY;

        // Overworld encounter state.
        public Monster EncounterMonster;
        public int EncounterX, EncounterY;
        public bool ActiveEncounter;

        public bool InShop;
        public Shop CurrentShop;
        public string ShopName;

        long _uid = 1;
        public long NextUid() => _uid++;

        public Game(ulong seed) : this(seed, "adventurer")
        {
        }

        public Game(ulong seed, string roleId) : this(seed, roleId, "human", null)
        {
        }

        public Game(ulong seed, string roleId, string raceId, string charName)
        {
            Rng = new Rng(seed);
            Dungeon = new Dungeon(Rng);
            Player = new Player(Rng, roleId, raceId) { Name = "you", CharName = Heroes.CleanName(charName) };
            World = OverworldGen.Generate(96, 60, seed ^ 0xA5A5A5A5UL);

            int startX = 0, startY = 0;
            for (int y = 0; y < World.H; y++)
            {
                for (int x = 0; x < World.W; x++)
                {
                    if (World.Tiles[x + y * World.W].Feature != OverworldFeature.Town) continue;
                    startX = x; startY = y;
                    goto located;
                }
            }
            located:
            World.PlayerX = startX;
            World.PlayerY = startY;
            World.CurrentRegionName = World.RegionAt(startX, startY).Name;
            World.Discover(6);

            StartingKit(Player.RoleId, Player.RaceId);
            DescendTo(Branch, 1);
            Say($"Seed {seed}. Press ? for help.", MessageKind.Info);
        }

        static ItemDef Find(IReadOnlyList<ItemDef> list, string name)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].Name == name) return list[i];
            return list.Count > 0 ? list[0] : default;
        }

        // ------------------------------------------------------------- logging

        public void Say(string text, MessageKind kind = MessageKind.Neutral)
        {
            if (string.IsNullOrEmpty(text)) return;
            var m = new Message(Loc.T(text), kind, Turn);
            Log.Add(m);
            Transcript.Add(m);
            Said++;
            CueFor(kind);
            while (Log.Count > 200) Log.RemoveAt(0);
            while (Transcript.Count > 2000) Transcript.RemoveAt(0);
        }

        // ------------------------------------------------------------- levels

        public void DescendTo(string branchName, int depth)
        {
            Branch = branchName;
            Depth = depth;
            Player.CurrentBranch = branchName;
            Player.CurrentDepth = depth;
            if (depth > Player.MaxDepth) Player.MaxDepth = depth;
            ContractDepth();
            QuestDepth();

            var map = Dungeon.Ensure(branchName, depth, out var spawns, out int sx, out int sy);
            Map = map;
            Monsters.Clear();
            if (spawns != null)
                for (int i = 0; i < spawns.Count; i++) Monsters.Add(spawns[i].Monster);

            Map.ClearVisibility();
            Player.X = sx;
            Player.Y = sy;
            if (sx < 0 || sy < 0 || !Map.Walkable(sx, sy))
            {
                int f = DungeonGen.FindAnyFloor(Map);
                if (f >= 0) { Player.X = f % Map.W; Player.Y = f / Map.W; }
            }

            Player.InsideDungeon = true;
            Mode = GameMode.Dungeon;
            PlaceCompanions();
            Dungeon.Remember(branchName, depth, sx, sy);
            if (spawns != null) RaiseBones(spawns, sx, sy);
            if (spawns != null) RaiseBosses(spawns, sx, sy);
            EnsureQuestAmulet(); // fallback: levels generated before the quest still get their amulet
            PlaceMainDocs();

            Say($"You arrive at {Map.LevelName}.", MessageKind.Narrative);
            if (depth == 1) Say(Dungeon.Get(branchName).EntryText, MessageKind.Narrative);
            UpdateFov();
            NoteThreat();   // arriving in sight of something is a fight beginning as much as walking into one
        }

        /// <summary>Steps through the portal under the player: into the side branch it leads to, or back out of one.</summary>
        bool UsePortal()
        {
            var here = Dungeon.Get(Branch);
            if (here.Parent != null && Depth == 1)
            {
                string parent = here.Parent; int pd = here.ParentDepth;
                Say("You step back through the portal.", MessageKind.Narrative);
                DescendTo(parent, pd);
                Player.X = _portalX; Player.Y = _portalY;
                Map.Version++; UpdateFov();
                return true;
            }
            var side = Dungeon.SideBranchAt(Branch, Depth);
            if (side == null) { Say("The portal is dead. Whatever it led to is gone."); return false; }
            _portalX = Player.X; _portalY = Player.Y;
            Say("The portal takes you, and the world folds.", MessageKind.Narrative);
            AnnexVisited = true;
            DescendTo(side.Name, 1);
            return true;
        }

        int _portalX, _portalY;
        /// <summary>The hero has been through the portal at least once.</summary>
        public bool AnnexVisited;

        public bool Descend()
        {
            if (Map.Get(Player.X, Player.Y) == TileKind.Portal) return UsePortal();
            if (Map.Get(Player.X, Player.Y) != TileKind.StairsDown) { Say("There is no staircase down here."); return false; }
            var b = Dungeon.Get(Branch);
            if (Depth + 1 > b.MaxDepth)
            {
                Say("The stairs lead no further down. This is the bottom of this branch.");
                return false;
            }
            Say("You descend the staircase.", MessageKind.Narrative); Cue("stairs");
            DescendTo(Branch, Depth + 1);
            return true;
        }

        public bool Ascend()
        {
            TileKind t = Map.Get(Player.X, Player.Y);
            if (t == TileKind.Portal && Dungeon.Get(Branch).Parent != null) return UsePortal();
            if (t != TileKind.StairsUp && t != TileKind.LadderDown) { Say("There is no way up here."); return false; }
            if (Depth <= 1)
            {
                Say("The stairs lead up to daylight.");
                LeaveToOverworld();
                return true;
            }
            Say("You climb the stairs.", MessageKind.Narrative); Cue("stairs");
            DescendTo(Branch, Depth - 1);
            return true;
        }

        public void LeaveToOverworld()
        {
            if (CheckVictory()) return; // surfacing with the amulet wins the run
            Mode = GameMode.Overworld;
            Player.InsideDungeon = false;
            Map = null;
            Town = null;
            Monsters.Clear();
            NoteThreat();   // nothing is in sight on the road surface
            World.Discover(5);
            Say("You emerge into the open air.", MessageKind.Narrative);
        }

        public void EnterDungeonFromOverworld(int x, int y)
        {
            var t = World.Get(x, y);
            Say($"You enter {t.Name ?? "the darkness"}.", MessageKind.Narrative);
            int d = World.RegionAt(x, y).Depth;
            string branch = OverworldGen.BranchForEntrance(t.Name);
            int start = branch == QuestBranch ? Math.Max(1, d / 4) : Math.Max(1, Math.Min(Dungeon.Get(branch).MaxDepth / 2, d / 8));
            DescendTo(branch, start);
        }

        // ---------------------------------------------------------------- FOV

        public void UpdateFov()
        {
            if (Map == null) return;
            Map.ClearVisibility();
            int radius = Player.Blinded ? 1 : Mode == GameMode.TownMap ? 16 : 10;
            for (int i = 0; i < 2; i++)
                if (Player.Rings[i] != null && Player.RingKnown[i] && Player.Rings[i].Name == "ring of warning") radius += 2;
            if (!Player.Blinded) radius += MutationSight();
            if (!Player.Blinded) radius = Math.Max(2, radius - Bodies.EyePenalty(Player));
            Fov.Compute(Map, Player.X, Player.Y, radius, null);
            Map.Version++;
        }

        // ------------------------------------------------------------ monsters

        public Monster MonsterAt(int x, int y)
        {
            for (int i = 0; i < Monsters.Count; i++)
                if (Monsters[i].X == x && Monsters[i].Y == y) return Monsters[i];
            return null;
        }

        public bool PlayerBlockedAt(int x, int y) => MonsterAt(x, y) != null;

        // ------------------------------------------------------------ movement

        public bool TryMovePlayer(int dx, int dy)
        {
            if (Map == null) return false;
            int nx = Player.X + dx, ny = Player.Y + dy;
            if (!Map.InBounds(nx, ny)) return false;

            var target = MonsterAt(nx, ny);
            if (target != null) return Attack(target);
            if (Map.Get(nx, ny) == TileKind.Altar && Mode == GameMode.Dungeon) { OpenAltar(nx, ny); return true; }

            if (!Map.CanStep(Player.X, Player.Y, nx, ny, true))
            {
                InteractWithWall(nx, ny);
                EndPlayerTurn();
                return true;
            }

            Player.X = nx; Player.Y = ny;
            MakeNoise(ArmourClatter());
            Map.Version++;

            if (TrapTable.TryGet(Map.Number, nx, ny, out var trapKind, out var trapLevel) && Player.PoisonResist < 3 && Player.BuffTurns("levitating") == 0)
            {
                TrapTable.Remove(Map.Number, nx, ny);
                TriggerTrap(trapKind, trapLevel);
            }

            UpdateFov();
            SenseTraps();
            bool slipped = Map.SurfaceAt(nx, ny) == SurfaceKind.Ice && Rng.Chance(30);
            if (slipped) Say("You slip on the ice!", MessageKind.Warn);
            EndPlayerTurn();
            if (slipped && Mode == GameMode.Dungeon) RunMonsters();
            return true;
        }

        void InteractWithWall(int x, int y)
        {
            MakeNoise(2);
            TileKind t = Map.Get(x, y);
            switch (t)
            {
                case TileKind.Rubble:
                    Say("You climb over the rubble.", MessageKind.Neutral);
                    Map.Set(x, y, TileKind.Floor);
                    Map.Version++;
                    return;
                case TileKind.LockedDoor:
                    if (Player.FindFirst("lock pick") != null)
                    {
                        Say("You pick the lock.", MessageKind.Good); Cue("door");
                        Map.Set(x, y, TileKind.OpenDoor);
                        Map.Version++;
                        return;
                    }
                    if (SpendBrassKey())
                    {
                        Say("The brass key turns, and the lock lets go. The key crumbles in your hand.", MessageKind.Good); Cue("door");
                        Map.Set(x, y, TileKind.OpenDoor);
                        Map.Version++;
                        return;
                    }
                    Say("This door is locked.", MessageKind.Info);
                    Cue("warn");   // a blocked way is worth hearing even when it is only a door
                    return;
                case TileKind.HiddenDoor:
                    Map.Set(x, y, TileKind.OpenDoor);
                    Map.Version++;
                    Say("You find a secret door and open it.", MessageKind.Good); Cue("door");
                    return;
                case TileKind.Altar:
                    Say("You feel a chill. It is not a place to linger.");
                    return;
                case TileKind.Fountain:
                    Say("The fountain water is cold and clean.");
                    return;
                default:
                    Say("There is a wall in the way.", MessageKind.Info);
                    return;
            }
        }

        void TriggerTrap(Traps kind, int level)
        {
            HurtBy(kind == Traps.Spike ? "a spike trap" : kind == Traps.Dart ? "a poison dart trap" : kind == Traps.Fire ? "a fire trap" : "a trap");
            switch (kind)
            {
                case Traps.Spike:
                    {
                        int dmg = Rng.Range(1 + level, 6 + level);
                        Player.HP -= dmg;
                        Say($"A spike trap pierces your foot for {dmg} damage!", MessageKind.Bad);
                        break;
                    }
                case Traps.Hole:
                    Say("You fall into a hole!", MessageKind.Bad);
                    Down(Rng.Range(1, 4));
                    break;
                case Traps.Dart:
                    {
                        int dmg = Rng.Range(1, 3 + level);
                        Player.HP -= dmg;
                        if (Rng.Chance(30) && Rng.Chance(100 - Player.ResistPct(DamageType.Poison))) { Say("The dart poisons you!", MessageKind.Bad); PoisonPlayer(1); }
                        else Say($"A dart hits you for {dmg} damage.", MessageKind.Bad);
                        break;
                    }
                case Traps.Teleport:
                    Say("You feel a wrenching pull...", MessageKind.Narrative);
                    TeleportPlayerAway();
                    break;
                case Traps.Alarm:
                    Say("A shrill alarm echoes!", MessageKind.Bad);
                    int alx = Player.X, aly = Player.Y;
                    Fx((tl, s) => FxLib.Nova(tl, s, alx, aly, 4, Elem.Wind));
                    AlertMonsters(14);
                    break;
                case Traps.Web:
                    Say("You are caught in a web and shake free slowly.", MessageKind.Bad);
                    Player.Energy -= 6;
                    break;
                case Traps.Fire:
                    Say("Flames flare around you!", MessageKind.Bad);
                    int trx = Player.X, try0 = Player.Y;
                    Fx((tl, s) => FxLib.Burst(tl, s, trx, try0, 1, Elem.Fire));
                    Player.HP -= Player.ResistDamage(Rng.Range(1, 4), DamageType.Fire);
                    PutSurface(Player.X, Player.Y, SurfaceKind.Fire, 3);
                    SetAlight(Player);
                    break;
            }
            CheckDeath();
        }

        public void PoisonPlayer(int amount)
        {
            amount = Player.ResistDamage(amount, DamageType.Poison);
            Player.PoisonResist = Math.Max(Player.PoisonResist, 1);
            Player.HP -= amount;
            Say($"The poison burns for {amount} damage.", MessageKind.Bad);
            CheckDeath();
        }

        public void Down(int amount)
        {
            Player.HP -= amount;
            if (Player.HP > 0) Say($"You collapse for {amount} damage.", MessageKind.Bad);
            CheckDeath();
        }

        void AlertMonsters(int radius)
        {
            for (int i = 0; i < Monsters.Count; i++)
            {
                var m = Monsters[i];
                if (Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y) > radius) continue;
                m.Alert = 1;
                m.Dormant = false;
            }
            Say("The noise carries through the halls.", MessageKind.Warn);
        }

        // -------------------------------------------------------------- combat

        public bool Attack(Monster target)
        {
            if (target == null) return false;
            MakeNoise(3);
            if (target.Townsperson)
            {
                Assault(target);
                return true;
            }
            if (target.Ally)
            {
                int ox = Player.X, oy = Player.Y;
                Player.X = target.X; Player.Y = target.Y; target.X = ox; target.Y = oy;
                Map.Version++;
                UpdateFov();
                EndPlayerTurn();
                return true;
            }
            bool crit;
            bool unaware = target.Asleep || target.Alert == 0 || target.FearTurns > 0 || target.Confused;
            var res = Battles.PlayerMelee(Player, target, Rng, out crit, 1, -Bodies.AimPenalty(Player.Aim));
            if (res.Hit) target.Asleep = false;
            if (res.Killed) { _killSneak = unaware; _killType = DamageType.Physical; }
            Say(res.Message, res.Killed ? MessageKind.Kill : MessageKind.Combat);
            WoundFrom(target, res, Bodies.EdgedWeapon(Player), crit, Player.Aim);
            AfterBlow(res, crit);
            Map.Version++;
            if (res.Hit) MeleeProcs(target, res.Damage, !res.Killed);

            if (res.Killed) KillMonster(target);
            else
            {
                target.Alert = 1;
                target.Dormant = false;
                Player.GainSkill(Skill.Combat, res.Hit ? 1 : 0);
            }
            EndPlayerTurn();
            return true;
        }

        public void KillMonster(Monster m)
        {
            GodsOnKill(m);
            Monsters.Remove(m);
            if (m.Def.Trait != null) TraitOnDeath(m);
            ContractKill(m);
            QuestKill(m);
            if (m.BossId != null || m.Unique) RecordDeed(Deed.Killed, m.Name, m.BossId != null ? 3 : 2);
            if (m.BossId != null) BossFalls(m);
            if (m.BonesKey != null) { LaidToRest.Add(m.BonesKey); Say("The restless shade is laid to rest at last.", MessageKind.Good); }
            Player.Kills++;
            bool leveled = Player.AddXp(m.XpKill);
            Player.GainSkill(Skill.Combat, 2);
            Say($"You have killed the {m.TheName}. ({m.XpKill} experience)", MessageKind.Kill);
            if (leveled) AnnounceLevelUp();

            for (int i = 0; i < m.Inventory.Count; i++)
            {
                if (m.Inventory[i].Def.Kind == ItemKind.Gold) Player.Gold += m.Inventory[i].Quantity;
                else GroundItems.Add(Map.Number, m.X, m.Y, m.Inventory[i]);
            }
            if (m.Def.CorpseValue > 0 && Rng.Chance(60))
            {
                string corpseName = m.Def.Skeleton ? "skeleton corpse"
                                 : m.Def.Undead ? "remains"
                                 : m.Name + " corpse";
                var corpseDef = new ItemDef { Name = corpseName, Glyph = '%', Kind = ItemKind.Corpse, Cost = 0, Weight = 60 };
                GroundItems.Add(Map.Number, m.X, m.Y, new Item(corpseDef, Rng, NextUid()));
            }
        }

        public void CheckDeath()
        {
            if (Player.HP > 0) return;
            if (Player.BuffTurns("revive") > 0)
            {
                Player.SetBuff("revive", 0);
                Player.HP = Math.Max(1, Player.MaxHP / 2);
                Say("A white light drags you back from the dark!", MessageKind.Good);
                return;
            }
            if (Player.Amulet != null && Player.Amulet.Def.Name == "amulet of life saving")
            {
                Player.Amulet = null; Player.RefreshGear();
                Player.HP = Math.Max(1, Player.MaxHP);
                Say("The amulet blazes, shatters, and you wake whole.", MessageKind.Good);
                return;
            }
            Mode = GameMode.GameOver;
            DeathCause = CauseOfDeath();
            Say("You die...", MessageKind.Death);
        }

        // ---------------------------------------------------------------- turn

        public void EndPlayerTurn()
        {
            if (Mode != GameMode.Dungeon && Mode != GameMode.TownMap) return;
            Turn++;
            Player.Turns = Turn;
            ResolveNoise();
            AmuletCorrupts();
            ProcessHunger();
            DecrementStatus();
            TickSurfaces();
            Regenerate();
            TickWounds();
            if (!(Player.BuffTurns("haste") > 0 && (Turn & 1) == 0)) RunMonsters();
            {
                // A limping hero gives the monsters an extra move every fourth turn; a crawling one, every other turn.
                int limp = Bodies.Limp(Player);
                if (limp > 0 && Turn % (limp == 1 ? 4 : 2) == 0) RunMonsters();
            }
            UpdateFov();
            NoteThreat();
            CheckDeath();
            ReapCompanions();
            CheckAchievements();
            QuestCheck();
        }

        /// <summary>
        /// Satiety, NetHack style: a nutrient pool that drains every turn. Eating
        /// refills it; running dry is survivable for a while and then lethal. The
        /// previous version compared a counter that eating never reset against a
        /// pool that started at zero, so every run starved to death on turn ~80
        /// regardless of what the player did.
        /// </summary>
        void ProcessHunger()
        {
            var p = Player;
            if (Difficulty == Difficulty.Classic) { p.Hunger = 0; return; }
            if (p.PerkRank("gourmand") == 0 || (Turn & 1) == 1) p.Nutrient--;
            p.Nutrient -= MutationHunger();
            if (p.Nutrient > 0) { p.Hunger = 0; return; }

            p.Hunger++;
            if (p.Nutrient == 0) Say("You are getting hungry.", MessageKind.Warn);
            else if (p.Nutrient <= -100 && p.Hunger % 20 == 0)
            {
                p.HP -= 1; HurtBy("starvation");
                Say("You are starving.", MessageKind.Bad);
            }
        }

        /// <summary>Eat one item of food. Returns false if it was not edible.</summary>
        public bool EatFood(Item food)
        {
            if (food == null) return false;

            int before = Player.Nutrient;
            Player.Nutrient = Math.Min(2000, Player.Nutrient + food.Def.Nutrition);
            Player.Hunger = 0;
            Player.Inventory.Remove(food);

            int gained = Player.Nutrient - before;
            if (gained <= 0)
                Say($"You eat {food.Name}. You were not hungry enough to taste it.", MessageKind.Neutral);
            else
                Say($"You eat {food.Name}. That was good (+{gained} nourishment).", MessageKind.Good);
            return true;
        }

        void DecrementStatus()
        {
            if (Player.SleepTurns > 0) Player.SleepTurns--;
            if (Player.Buffs.Count > 0)
                foreach (string id in new List<string>(Player.Buffs.Keys))
                {
                    if (--Player.Buffs[id] > 0) continue;
                    Player.Buffs.Remove(id);
                    if (id == "invisibility") Player.Invisible = false;
                    if (SpellBuffs.Find(id) != null) Player.RefreshGear();
                    Say($"Your {Spells.BuffLabel(id).ToLowerInvariant()} fades.", MessageKind.Info);
                }
            if (Player.Buffs.Count > 0)
                foreach (var kv in Player.Buffs)
                {
                    var bd = SpellBuffs.Find(kv.Key);
                    if (bd != null && bd.Regen > 0 && Player.HP < Player.MaxHP) Player.HP = Math.Min(Player.MaxHP, Player.HP + bd.Regen);
                }
            if (Player.StunTurns > 0) Player.StunTurns--;
            if (Player.ConfusionTurns > 0)
            {
                Player.ConfusionTurns--;
                if (Player.ConfusionTurns == 0) { Player.Confused = false; Say("You feel less confused.", MessageKind.Good); }
            }
            if (Player.BlindTurns > 0)
            {
                Player.BlindTurns--;
                if (Player.BlindTurns == 0) { Player.Blinded = false; Say("You can see again.", MessageKind.Good); }
            }
            if (Player.HallucinationTurns > 0)
            {
                Player.HallucinationTurns--;
                if (Player.HallucinationTurns == 0) { Player.Hallucinating = false; Say("The visions fade.", MessageKind.Good); }
            }
            if (Player.PoisonResist > 0)
            {
                if (Rng.Chance(100 - Math.Max(0, Player.ResistPct(DamageType.Poison)))) { Player.HP -= 1; HurtBy("poison"); }
                if (Player.PoisonResist <= 2 && Rng.Chance(30))
                {
                    Player.PoisonResist--;
                    if (Player.PoisonResist == 0) Say("The poison finally wears off.", MessageKind.Good);
                }
            }
            if (Player.Amulet != null && Player.Amulet.Name == "amulet of strangulation" && Turn % 15 == 0)
            {
                Player.HP -= Rng.Range(1, 4); HurtBy("an amulet of strangulation");
                Say("The amulet tightens around your throat!", MessageKind.Bad);
            }
        }

        void RunMonsters()
        {
            for (int i = Monsters.Count - 1; i >= 0; i--)
            {
                var m = Monsters[i];
                if (m.IsDead) { Monsters.RemoveAt(i); continue; }
                TickMonsterEffects(m);
                if (m.IsDead) { Monsters.Remove(m); continue; }   // (a spell's damage over time may already have removed it)
                TickMonsterWounds(m);
                if (m.IsDead) { Monsters.Remove(m); continue; }
                if (m.HeldTurns > 0) { m.HeldTurns--; continue; }

                if (m.Asleep && m.SleepTurns > 0 && --m.SleepTurns == 0) { m.Asleep = false; m.Alert = 1; }
                if (m.Asleep) { m.Energy += m.Speed / 2; if (m.Energy < 12) continue; m.Energy -= 12; continue; }

                if (m.Confused && Rng.Chance(50))
                {
                    m.Energy += MoveSpeed(m);
                    if (m.Energy < 12) continue;
                    m.Energy -= 12;
                    TryMonsterStep(m, Rng.Range(-1, 2), Rng.Range(-1, 2));
                    continue;
                }

                m.Energy += MoveSpeed(m);
                if (m.Energy < 12) continue;
                m.Energy -= 12;
                if (m.Ally) { AllyTurn(m); continue; }
                MonsterTurn(m);
                if (Mode != GameMode.Dungeon && Mode != GameMode.TownMap) return;
            }
        }

        void MonsterTurn(Monster m)
        {
            if (m.Townsperson) { TownsfolkTurn(m); return; }
            int dist = Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y);
            if (m.IsGuard && Mode != GameMode.TownMap && dist > 24) return;
            if (m.FearTurns > 0) { if (FleeStep(m)) return; }
            else if (dist > 1 && AttackAdjacentAlly(m)) return;
            if (dist > 1 && m.BossId == null && FightRival(m, dist)) return;
            if (m.BossId != null && BossTurn(m, dist)) return;
            if (m.Def.Trait != null && TraitBeforeAct(m, dist)) return;

            if (dist == 1)
            {
                if (Player.BuffTurns("sanctuary") > 0 && Rng.Chance(70)) return;   // a hush: it cannot bring itself to strike
                if (WildSwing(m)) return;
                if (m.Def.Explodes)
                {
                    int dmg = Rng.Range(4, 12);
                    Player.HP -= dmg;
                    HurtBy(Article(m));
                    Say($"The {m.Name} explodes for {dmg} damage!", MessageKind.Bad);
                    int ex = m.X, ey = m.Y;
                    Fx((tl, s) => FxLib.Burst(tl, s, ex, ey, 1, Elem.Fire));
                    Monsters.Remove(m);
                    Map.Version++;
                    CheckDeath();
                    return;
                }
                HurtBy(Article(m));
                var res = Battles.MeleeAttack(m, Player, Rng);
                Say(res.Message, res.Killed ? MessageKind.Death : MessageKind.Combat);
                if (Bodies.Wounding(res.Kind)) WoundFrom(Player, res, Bodies.EdgedAttack(res.Kind), false);
                ArmourTakesBlow(res);
                if (res.Hit && !res.Killed && Player.Buffs.Count > 0) Retaliate(m);
                if (res.Hit && !res.Killed) ImbueReaction();
                if (m.Def.Trait != null) TraitAfterHit(m, res);
                Map.Version++;
                CheckDeath();
                return;
            }

            LearnFromHiding(m, dist);
            bool canSee = dist <= NoticeRadius(m) && !Player.Invisible && !m.Dormant;
            if (canSee) { m.Alert = 1; m.Dormant = false; }

            switch (m.Def.Ai)
            {
                case AiKind.Ambush: if (dist > 2 && canSee) m.Alert = 0; break;
                case AiKind.Guard: if (dist > 8) m.Alert = 0; break;
                case AiKind.Territorial:
                    if (Pathfinder.Chebyshev(m.X, m.Y, m.HomeX, m.HomeY) > 8) m.Alert = 0;
                    break;
            }

            if (m.Alert == 0)
            {
                int hx = m.HomeX - m.X, hy = m.HomeY - m.Y;
                int dx, dy;
                if (Math.Abs(hx) + Math.Abs(hy) < 6 && Rng.Chance(60)) { dx = Math.Sign(hx); dy = Math.Sign(hy); }
                else { dx = Rng.Range(-1, 2); dy = Rng.Range(-1, 2); }
                if (dx != 0 || dy != 0) TryMonsterStep(m, dx, dy);
                return;
            }

            int bestX = 0, bestY = 0, bestD = int.MaxValue;
            for (int k = 0; k < 8; k++)
            {
                int nx = m.X + Pathfinder.Dx8[k], ny = m.Y + Pathfinder.Dy8[k];
                if (!Map.CanStep(m.X, m.Y, nx, ny, true)) continue;
                if (PlayerBlockedAt(nx, ny)) continue;
                int d = Pathfinder.Chebyshev(nx, ny, Player.X, Player.Y);
                if (d < bestD) { bestD = d; bestX = Pathfinder.Dx8[k]; bestY = Pathfinder.Dy8[k]; }
            }

            if (bestX == 0 && bestY == 0) return;
            if (TryMonsterStep(m, bestX, bestY)) return;

            int tx = m.X + bestX, ty = m.Y + bestY;
            if (Map.Get(tx, ty) == TileKind.Rubble && Rng.Chance(30))
            {
                Map.Set(tx, ty, TileKind.Floor);
                Say($"The {m.Name} smashes through the rubble.", MessageKind.Combat);
                Map.Version++;
            }
        }

        bool TryMonsterStep(Monster m, int dx, int dy)
        {
            int nx = m.X + dx, ny = m.Y + dy;
            if (!Map.InBounds(nx, ny)) return false;
            if (!Map.CanStep(m.X, m.Y, nx, ny, true)) return false;
            if (nx == Player.X && ny == Player.Y) return false;
            if (PlayerBlockedAt(nx, ny)) return false;
            if (AvoidsCell(m, nx, ny)) return false;
            m.X = nx; m.Y = ny;
            if (Map.SurfaceAt(nx, ny) == SurfaceKind.Ice && Rng.Chance(30)) m.Energy -= 12;
            if (m.Def.Regenerates && m.HP < m.MaxHP) m.HP = Math.Min(m.MaxHP, m.HP + 1);
            Map.Version++;
            return true;
        }
    }

    public enum MessageKind { Neutral, Good, Bad, Combat, Kill, Info, Warn, Death, Narrative, Quest }

    public struct Message
    {
        public readonly string Text;
        public readonly MessageKind Kind;
        public readonly int Turn;
        public Message(string text, MessageKind kind, int turn) { Text = text; Kind = kind; Turn = turn; }
    }
}
