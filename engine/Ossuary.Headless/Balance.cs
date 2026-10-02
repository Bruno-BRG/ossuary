using System;
using System.Collections.Generic;
using System.Text;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Tools
{
    /// <summary>One bot run: who it was, how far it got, and what ended it.</summary>
    public sealed class BotRun
    {
        public string Race, Role, KilledBy = "";
        public bool Died;
        public int Turns, Depth, Level, Kills;
        public int DeathDepth;
    }

    /// <summary>
    /// A deterministic player-bot used to compare classes and races. It studies its books, spends its
    /// perks, fights with whatever spells and abilities the class has, rests, eats, and walks down.
    /// It is not clever; it is the same for everyone, which is what makes the comparison meaningful.
    /// Gear found on the way is not simulated: this measures class, race, level and skills.
    /// </summary>
    public static class BalanceBot
    {
        static readonly Dictionary<string, string[]> PerkOrder = new Dictionary<string, string[]>
        {
            { "fighter", new[] { "weapon-master", "tough", "second-wind", "cleave", "shield-wall", "hale", "mighty" } },
            { "paladin", new[] { "aura", "shield-wall", "holy-strike", "weapon-master", "tough", "second-wind" } },
            { "rogue", new[] { "weapon-master", "light-feet", "tough", "lucky", "vanish", "agile" } },
            { "ranger", new[] { "keen-eye", "weapon-master", "tough", "light-feet", "lucky" } },
            { "wizard", new[] { "mind-expansion", "spell-power", "focus", "quick-recovery", "tough", "hale" } },
            { "necromancer", new[] { "mind-expansion", "spell-power", "focus", "quick-recovery", "tough", "hale" } },
            { "cleric", new[] { "mind-expansion", "shield-wall", "holy-strike", "tough", "focus", "hale" } },
            { "adventurer", new[] { "tough", "weapon-master", "hale", "lucky", "mighty" } },
        };

        // Spells in the order the bot prefers them, by purpose.
        static readonly string[] Attack = { "chain-lightning", "fireball", "lightning-bolt", "frost-ray", "drain-life", "smite", "magic-missile", "shocking-grasp" };
        static readonly string[] Heal = { "greater-heal", "cure-wounds" };
        static readonly string[] Buffs = { "stone-skin", "ward", "bless" };
        static readonly string[] Summons = { "army-of-bones", "raise-skeleton", "summon-beast", "familiar" };

        public static bool Trace;

        public static BotRun Play(ulong seed, string race, string role, int maxTurns, bool dive)
        {
            var g = Game.NewHero(seed, "Bot", race, role);
            var run = new BotRun { Race = race, Role = role };
            var cmd = new Commands(g); _cmd = cmd; Ignored.Clear();
            StudyAll(g);

            int stuck = 0, lastX = -1, lastY = -1, guard = 0, lastProgress = 0, lastKills = 0, lastDepth = 1;
            var recent = new Queue<int>();
            while (g.Turn < maxTurns && g.Mode == GameMode.Dungeon && guard++ < maxTurns * 4)
            {
                int turn = g.Turn;
                try { Think(g, cmd, dive, ref stuck); }
                catch (Exception e) { throw new Exception($"bot {race}/{role} seed {seed} turn {g.Turn}: {e.Message}\n{e.StackTrace}"); }
                if (Trace && (g.Turn % 50 == 0 || (g.Turn > 1200 && g.Turn < 1212)) && g.Turn != turn) Console.WriteLine($"T{g.Turn} d{g.Depth} L{g.Player.Level} hp{g.Player.HP}/{g.Player.MaxHP} at {g.Player.X},{g.Player.Y} stuck{stuck} foes{VisibleFoes(g).Count} food{g.Player.Nutrient} last=\"{(g.Log.Count > 0 ? g.Log[g.Log.Count - 1].Text : "")}\"");
                if (g.UiState.Active != Panel.None) g.UiState.Active = Panel.None;
                if (g.PendingChoice.Active) g.PendingChoice.Clear();
                if (g.UiState.IsTargeting) g.UiState.Targeting = TargetingMode.None;
                if (g.Turn == turn && g.Mode == GameMode.Dungeon) cmd.Execute(".");   // never spin without time passing
                bool brawling = VisibleFoes(g).Exists(f => Pathfinder.Chebyshev(g.Player.X, g.Player.Y, f.X, f.Y) <= 1);
                if (brawling) stuck = 0;
                else if (g.Player.X == lastX && g.Player.Y == lastY) stuck++;
                else { stuck = 0; lastX = g.Player.X; lastY = g.Player.Y; }
                // Pacing back and forth between two cells counts as being stuck too.
                recent.Enqueue(g.Player.X * 1000 + g.Player.Y);
                if (recent.Count > 12) recent.Dequeue();
                if (recent.Count == 12 && new HashSet<int>(recent).Count <= 2 && g.Turn - lastProgress > 12) { stuck = Math.Max(stuck, 16); recent.Clear(); }
                if (g.Player.Kills != lastKills || g.Depth != lastDepth) { lastProgress = g.Turn; lastKills = g.Player.Kills; lastDepth = g.Depth; }
            }

            var p = g.Player;
            run.Died = g.Mode == GameMode.GameOver;
            run.Turns = g.Turn; run.Depth = p.MaxDepth; run.Level = p.Level; run.Kills = p.Kills;
            run.DeathDepth = g.Depth;
            if (run.Died) run.KilledBy = KillerOf(g);
            return run;
        }

        static string KillerOf(Game g)
        {
            for (int i = g.Log.Count - 1; i >= 0 && i >= g.Log.Count - 8; i--)
            {
                string t = g.Log[i].Text;
                if (t.StartsWith("The ", StringComparison.Ordinal))
                {
                    int end = t.IndexOf(" ", 4, StringComparison.Ordinal);
                    int verb = t.IndexOfAny(new[] { 'h', 'k', 'e' }, 4);
                    // "The <name> hits/kills/explodes ..." — cut at the first verb after the name
                    foreach (string v in new[] { " hits ", " kills ", " explodes ", " bites ", " misses " })
                    {
                        int at = t.IndexOf(v, StringComparison.Ordinal);
                        if (at > 0) return t.Substring(4, at - 4);
                    }
                }
                if (t.Contains("starv")) return "hunger";
                if (t.Contains("burn")) return "fire";
                if (t.Contains("poison")) return "poison";
            }
            return "unknown";
        }

        static void StudyAll(Game g)
        {
            var p = g.Player;
            for (int tries = 0; tries < 60; tries++)
            {
                bool learned = false;
                foreach (var it in new List<Item>(p.Inventory))
                {
                    if (it.Def.Kind != ItemKind.Book) continue;
                    foreach (string id in Spells.InBook(it.Def.Name))
                        if (!p.Spells.Contains(id)) { learned = true; break; }
                    if (!learned) continue;
                    p.Confused = false; p.ConfusionTurns = 0;
                    g.StudyBook(it);
                    break;
                }
                if (!learned) break;
            }
            p.Confused = false; p.ConfusionTurns = 0;
        }

        static void Think(Game g, Commands cmd, bool dive, ref int stuck)
        {
            var p = g.Player;
            SpendPerks(g);
            Eat(g);

            var foes = VisibleFoes(g);
            if (foes.Count > 0 && stuck < 15) { Fight(g, cmd, foes); return; }

            // Between fights: grab what lies about, then put it on, and use what is plainly good.
            if (Loot(g, cmd, ref stuck)) return;
            foreach (var it in new List<Item>(p.Inventory))
            {
                if (it.Def.Kind == ItemKind.Scroll && it.Def.Name.Contains("enchant")) { g.UseScroll(it); return; }
                if (it.Def.Kind == ItemKind.Potion && (it.Def.Name.Contains("gain") || (it.Def.Name.Contains("speed") && false))) { g.Quaff(it); return; }
            }

            // Between fights: recover before moving on.
            bool starving = p.Nutrient < 150;
            if (!starving && (p.HP < p.MaxHP * 0.75 || (p.MpMax >= 10 && p.Mp < p.MpMax * 8 / 10) || p.PoisonResist > 0)) { cmd.Execute("."); return; }

            // Summons and buffs are pointless while idle, so go: find the way down.
            bool ready = dive || p.Level >= g.Depth + 1;
            if (ready && TryWalkToStairs(g, cmd, ref stuck)) return;
            Explore(g, cmd, ref stuck);
        }

        static void SpendPerks(Game g)
        {
            var p = g.Player;
            while (p.PendingAdvances > 0)
            {
                string pick = null;
                if (PerkOrder.TryGetValue(p.RoleId, out var order))
                    foreach (string id in order)
                        if (Progression.IsAvailable(p, Progression.Find(id))) { pick = id; break; }
                if (pick == null)
                {
                    var open = Progression.Available(p);
                    if (open.Count == 0) break;
                    pick = open[0].Id;
                }
                if (!g.ApplyLevelAdvance(pick)) break;
            }
        }

        static void Eat(Game g)
        {
            var p = g.Player;
            if (p.Nutrient > 300) return;
            foreach (var it in new List<Item>(p.Inventory))
                if (it.Def.Kind == ItemKind.Food) { g.EatFood(it); return; }
        }

        static List<Monster> VisibleFoes(Game g)
        {
            var list = new List<Monster>();
            foreach (var m in g.Monsters)
                if (!m.IsDead && !m.Ally && g.Map.IsVisible(m.X, m.Y) && ((m.Def.Level > 0 && m.Def.Speed > 3) || Pathfinder.Chebyshev(g.Player.X, g.Player.Y, m.X, m.Y) <= 1)) list.Add(m);
            var p = g.Player;
            list.Sort((a, b) => Pathfinder.Chebyshev(p.X, p.Y, a.X, a.Y).CompareTo(Pathfinder.Chebyshev(p.X, p.Y, b.X, b.Y)));
            return list;
        }

        static bool Knows(Player p, string id) => p.Spells.Contains(id);

        static bool Cast(Game g, string id, int x, int y)
        {
            var s = Spells.Find(id);
            if (s == null || !g.Player.Spells.Contains(id) || g.Player.Mp < s.Cost) return false;
            return g.CastSpell(id, x, y);
        }

        static bool Use(Game g, string id, int x, int y)
        {
            var a = Abilities.Find(id);
            if (a == null || !g.Player.Abilities.Contains(id) || g.Player.Vigor < a.Cost) return false;
            return g.UseAbility(id, x, y);
        }

        static void Fight(Game g, Commands cmd, List<Monster> foes)
        {
            var p = g.Player;
            var near = foes[0];
            int d = Pathfinder.Chebyshev(p.X, p.Y, near.X, near.Y);
            int adjacent = 0, nearby = 0;
            foreach (var f in foes)
            {
                int fd = Pathfinder.Chebyshev(p.X, p.Y, f.X, f.Y);
                if (fd == 1) adjacent++;
                if (fd <= 3) nearby++;
            }

            // 1. Stay alive.
            if (p.HP * 100 < p.MaxHP * 40)
            {
                foreach (string id in Heal) if (Cast(g, id, p.X, p.Y)) return;
                foreach (string id in new[] { "lay-on-hands", "second-wind" }) if (Use(g, id, p.X, p.Y)) return;
                foreach (string kind in new[] { "full healing", "extra healing", "healing" })
                    foreach (var it in new List<Item>(p.Inventory))
                        if (it.Def.Kind == ItemKind.Potion && it.Def.Name.EndsWith(kind, StringComparison.Ordinal)) { g.Quaff(it); return; }
                if (p.HP * 100 < p.MaxHP * 25 && Use(g, "vanish", p.X, p.Y)) return;
            }

            // 2. Prepare: summons and one defensive buff when a fight is on.
            if (d <= 6)
            {
                bool hasAlly = false;
                foreach (var m in g.Monsters) if (m.Ally && !m.IsDead) { hasAlly = true; break; }
                if (!hasAlly) foreach (string id in Summons) if (Cast(g, id, p.X, p.Y)) return;
                foreach (string id in Buffs)
                    if (Knows(p, id) && p.BuffTurns(id) == 0 && Cast(g, id, p.X, p.Y)) return;
                if (nearby >= 2 && Use(g, "war-cry", p.X, p.Y)) return;
            }

            // 3. Hit.
            bool caster = p.MpMax >= 10 && (p.RoleId == "wizard" || p.RoleId == "necromancer" || p.RoleId == "cleric");
            if (d == 1)
            {
                if (adjacent >= 2 && Use(g, "cleave", p.X, p.Y)) return;
                foreach (string id in new[] { "holy-strike", "backstab", "power-strike", "shield-bash" })
                    if (Use(g, id, near.X, near.Y)) return;
                if (caster || p.RoleId == "paladin")
                    foreach (string id in Attack)
                        if (Cast(g, id, near.X, near.Y)) return;
                Step(g, near.X, near.Y);
                return;
            }

            if (caster || (p.RoleId == "paladin" && p.Mp >= 6))
            {
                foreach (string id in Attack)
                {
                    var s = Spells.Find(id);
                    if (!Knows(p, id) || p.Mp < s.Cost || d > s.Range) continue;
                    if (!Fov.HasLine(g.Map, p.X, p.Y, near.X, near.Y)) continue;
                    if (id == "fireball" && nearby < 2 && p.Mp < s.Cost * 2) continue;
                    if (Cast(g, id, near.X, near.Y)) return;
                }
            }
            if (d <= 8 && Fov.HasLine(g.Map, p.X, p.Y, near.X, near.Y) && Use(g, "aimed-shot", near.X, near.Y)) return;

            // Let them come when we can hit them first; otherwise close in.
            if (d <= 2) { cmd.Execute("."); return; }
            Step(g, near.X, near.Y);
        }

        static readonly HashSet<long> Ignored = new HashSet<long>();

        static int MeleeScore(Item w)
        {
            if (w == null) return 2;
            if (w.Def.Class == ItemClass.Bow || w.Def.Class == ItemClass.Throw) return 0;
            return Math.Max(1, w.Def.Damage) * (w.Def.Sides + 1) / 2 + w.Def.DmgBonus + w.Mods.Dmg + w.Mods.ToHit / 2;
        }

        /// <summary>Wears better armour into every slot, wields a better weapon.</summary>
        static void ManageGear(Game g)
        {
            var p = g.Player;
            foreach (var it in new List<Item>(p.Inventory))
            {
                if (it.Def.Kind.IsWearable())
                {
                    if (it.Def.Kind == ItemKind.Shield && p.Wielded != null && (p.Wielded.Def.Flags & ItemFlags.TwoHanded) != 0) continue;
                    if ((it.Def.Flags & ItemFlags.Cursed) != 0) continue;
                    var cur = p.PieceIn(it.Def.Kind);
                    if (cur == null || it.TotalAc > cur.TotalAc) { p.Wear(it); g.RevealGear(it); }
                }
                else if (it.Def.Kind == ItemKind.Weapon)
                {
                    if ((it.Def.Flags & ItemFlags.Cursed) != 0) continue;
                    if ((it.Def.Flags & ItemFlags.TwoHanded) != 0 && p.WornShield != null) continue;
                    if (MeleeScore(it) > MeleeScore(p.Wielded))
                    {
                        if (p.Wielded != null) p.Inventory.Add(p.Wielded);
                        p.Inventory.Remove(it); p.Wielded = it; p.RefreshGear(); g.RevealGear(it);
                    }
                }
            }
        }

        /// <summary>Picks up what is underfoot, or walks to the nearest seen item. Returns true when it acted.</summary>
        static bool Loot(Game g, Commands cmd, ref int stuck)
        {
            var p = g.Player; var map = g.Map;
            var here = GroundItems.At(map.Number, p.X, p.Y);
            long key = map.Number * 1000000L + p.Y * 1000 + p.X;
            if (here != null && here.Count > 0 && !Ignored.Contains(key))
            {
                int before = here.Count;
                cmd.Execute("g");
                ManageGear(g);
                var after = GroundItems.At(map.Number, p.X, p.Y);
                if (after != null && after.Count >= before) Ignored.Add(key);   // too heavy or unwanted: leave it
                return true;
            }
            if (stuck > 8) return false;
            return StepToward(g, (x, y) =>
            {
                if (!map.WasSeen(x, y) || Ignored.Contains(map.Number * 1000000L + y * 1000 + x)) return false;
                var list = GroundItems.At(map.Number, x, y);
                return list != null && list.Count > 0 && Pathfinder.Manhattan(p.X, p.Y, x, y) <= 25;
            });
        }

        static Commands _cmd;

        /// <summary>One step like a player's: doors are opened with the door verb, everything else is a plain move.</summary>
        static void Move(Game g, int dx, int dy)
        {
            var p = g.Player;
            g.FacingX = dx; g.FacingY = dy;
            var t = g.Map.Get(p.X + dx, p.Y + dy);
            if (t == TileKind.ClosedDoor || t == TileKind.LockedDoor) _cmd.Execute("D");
            else g.TryMovePlayer(dx, dy);
        }

        static void Step(Game g, int gx, int gy)
        {
            if (StepToward(g, (x, y) => x == gx && y == gy)) return;
            var p = g.Player;
            Move(g, Math.Sign(gx - p.X), Math.Sign(gy - p.Y));
        }

        /// <summary>Breadth-first over walkable cells and doors; steps once toward the nearest cell that satisfies <paramref name="goal"/>.</summary>
        static bool StepToward(Game g, Func<int, int, bool> goal)
        {
            var map = g.Map; var p = g.Player;
            int n = map.W * map.H;
            var first = new int[n];
            for (int i = 0; i < n; i++) first[i] = -2;
            var queue = new Queue<int>();
            int start = p.Y * map.W + p.X;
            first[start] = -1; queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue(); int cx = cur % map.W, cy = cur / map.W;
                if (cur != start && goal(cx, cy))
                {
                    int dir = first[cur];
                    Move(g, Pathfinder.Dx8[dir], Pathfinder.Dy8[dir]);
                    return true;
                }
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Pathfinder.Dx8[k], ny = cy + Pathfinder.Dy8[k];
                    if (!map.InBounds(nx, ny)) continue;
                    int ni = ny * map.W + nx;
                    if (first[ni] != -2) continue;
                    var t = map.Get(nx, ny);
                    bool door = (t == TileKind.ClosedDoor || t == TileKind.HiddenDoor || (t == TileKind.LockedDoor && p.FindFirst("lock pick") != null)) && k % 2 == 0;
                    if (!map.CanStep(cx, cy, nx, ny, true) && !door) continue;
                    if (t == TileKind.Altar || t == TileKind.Fountain) continue;
                    first[ni] = cur == start ? k : first[cur];
                    queue.Enqueue(ni);
                }
            }
            return false;
        }

        static bool TryWalkToStairs(Game g, Commands cmd, ref int stuck)
        {
            var p = g.Player; var map = g.Map;
            if (map.Get(p.X, p.Y) == TileKind.StairsDown) { cmd.Execute(">"); return true; }
            if (stuck > 12) { Wander(g); return true; }
            return StepToward(g, (x, y) => map.Get(x, y) == TileKind.StairsDown);
        }

        static void Explore(Game g, Commands cmd, ref int stuck)
        {
            var map = g.Map;
            if (stuck > 12) { Wander(g); return; }
            if (StepToward(g, (x, y) => map.Walkable(x, y) && !map.WasSeen(x, y))) return;
            if (!TryWalkToStairs(g, cmd, ref stuck)) Wander(g);   // level explored: move on
        }

        static void Wander(Game g)
        {
            int k = g.Rng.Range(0, 8);
            Move(g, Pathfinder.Dx8[k], Pathfinder.Dy8[k]);
        }

        // ------------------------------------------------------------------ the report

        sealed class Stat
        {
            public int N, Died, Depth, Level, Kills, DeathTurns;
            public void Add(BotRun r)
            {
                N++; Depth += r.Depth; Level += r.Level; Kills += r.Kills;
                if (r.Died) { Died++; DeathTurns += r.Turns; }
            }
            public string Row(string label)
            {
                if (N == 0) return label.PadRight(13) + "-";
                double surv = 100.0 * (N - Died) / N;
                return $"{label,-13}{N,4}  {surv,5:0}%  depth {(double)Depth / N,4:0.0}  lv {(double)Level / N,4:0.0}  kills {(double)Kills / N,5:0.0}  die@ {(Died > 0 ? (double)DeathTurns / Died : 0),6:0}";
            }
        }

        public static int Report(string[] args)
        {
            int seeds = 8, turns = 4000;
            bool dive = false;
            string onlyRole = null, onlyRace = null;
            foreach (string a in args)
            {
                if (a == "balance") continue;
                if (a == "dive") dive = true;
                else if (a == "trace") Trace = true;
                else if (a.StartsWith("role=")) onlyRole = a.Substring(5);
                else if (a.StartsWith("race=")) onlyRace = a.Substring(5);
                else if (int.TryParse(a, out int n)) { if (seeds == 8 && !a.Contains("t")) seeds = n; else turns = n; }
            }
            // "balance 10 6000": first number seeds, second number turns.
            var nums = new List<int>();
            foreach (string a in args) if (int.TryParse(a, out int n)) nums.Add(n);
            if (nums.Count > 0) seeds = nums[0];
            if (nums.Count > 1) turns = nums[1];

            var byRole = new Dictionary<string, Stat>(); var byRace = new Dictionary<string, Stat>();
            var cell = new Dictionary<string, Stat>(); var killers = new Dictionary<string, int>();
            var all = new Stat();
            foreach (var role in Roles.All)
            {
                if (onlyRole != null && role.Id != onlyRole) continue;
                foreach (var race in Races.All)
                {
                    if (onlyRace != null && race.Id != onlyRace) continue;
                    for (int s = 0; s < seeds; s++)
                    {
                        var r = Play((ulong)(1000 + s * 7919 + role.Id.Length * 13), race.Id, role.Id, turns, dive);
                        if (!byRole.ContainsKey(role.Id)) byRole[role.Id] = new Stat();
                        if (!byRace.ContainsKey(race.Id)) byRace[race.Id] = new Stat();
                        string key = role.Id + "/" + race.Id;
                        if (!cell.ContainsKey(key)) cell[key] = new Stat();
                        byRole[role.Id].Add(r); byRace[race.Id].Add(r); cell[key].Add(r); all.Add(r);
                        if (r.Died) killers[r.KilledBy] = killers.ContainsKey(r.KilledBy) ? killers[r.KilledBy] + 1 : 1;
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"balance: {seeds} seeds per combination, {turns} turns, policy {(dive ? "dive" : "cautious")}");
            sb.AppendLine(all.Row("ALL"));
            sb.AppendLine("\n-- by class");
            foreach (var role in Roles.All) if (byRole.ContainsKey(role.Id)) sb.AppendLine(byRole[role.Id].Row(role.Id));
            sb.AppendLine("\n-- by race");
            foreach (var race in Races.All) if (byRace.ContainsKey(race.Id)) sb.AppendLine(byRace[race.Id].Row(race.Id));
            sb.AppendLine("\n-- class x race, survival % (depth)");
            sb.Append("".PadRight(13));
            foreach (var race in Races.All) if (byRace.ContainsKey(race.Id)) sb.Append(race.Id.PadLeft(11));
            sb.AppendLine();
            foreach (var role in Roles.All)
            {
                if (!byRole.ContainsKey(role.Id)) continue;
                sb.Append(role.Id.PadRight(13));
                foreach (var race in Races.All)
                {
                    if (!byRace.ContainsKey(race.Id)) continue;
                    var c = cell[role.Id + "/" + race.Id];
                    sb.Append($"{100.0 * (c.N - c.Died) / c.N,4:0}%({(double)c.Depth / c.N,4:0.0})");
                }
                sb.AppendLine();
            }
            sb.AppendLine("\n-- killers");
            var list = new List<KeyValuePair<string, int>>(killers);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < Math.Min(10, list.Count); i++) sb.AppendLine($"{list[i].Key,-20}{list[i].Value}");
            Console.Write(sb.ToString());
            return 0;
        }
    }
}
