using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Crime and the Watch (docs/game/living-world.md). Two layers: hostility is local and short (a person is against the hero while
    /// they can see them, plus a short grace), the bounty is persistent and per region (neighbouring regions know half of it).
    /// Penalties follow the harm done: damage for a blow, a fixed heavy price for a death, and so on. Only witnessed crimes count.
    /// </summary>
    public sealed partial class Game
    {
        public readonly Dictionary<string, int> Bounties = new Dictionary<string, int>();
        readonly Dictionary<string, int> _murders = new Dictionary<string, int>();
        int _arrestAfter;

        public const int WitnessRange = 9, HostileGrace = 15, HostileSpan = 40;

        string RegionHere() => World == null ? "" : World.RegionAt(World.PlayerX, World.PlayerY).Name;

        /// <summary>Regions whose rectangles touch this one (within a tile), by name.</summary>
        public List<string> NeighbourRegions(string name)
        {
            var list = new List<string>();
            if (World == null) return list;
            Region me = default; bool found = false;
            foreach (var r in World.Regions) if (r.Name == name) { me = r; found = true; break; }
            if (!found) return list;
            foreach (var r in World.Regions)
            {
                if (r.Name == name) continue;
                bool apartX = me.X + me.W + 1 < r.X || r.X + r.W + 1 < me.X;
                bool apartY = me.Y + me.H + 1 < r.Y || r.Y + r.H + 1 < me.Y;
                if (!apartX && !apartY) list.Add(r.Name);
            }
            return list;
        }

        /// <summary>What the Watch here holds against the hero: this region's bounty, or half of a neighbour's.</summary>
        public int BountyHere()
        {
            string here = RegionHere();
            int best = Bounties.TryGetValue(here, out int own) ? own : 0;
            foreach (string n in NeighbourRegions(here))
                if (Bounties.TryGetValue(n, out int b)) best = Math.Max(best, (b + 1) / 2);
            return best;
        }

        public void AddBounty(int amount, string crime)
        {
            string here = RegionHere();
            Bounties[here] = Math.Min(5000, (Bounties.TryGetValue(here, out int b) ? b : 0) + amount);
            Say($"{Loc.T(crime)} (+{amount}). The Watch of {Loc.T(here)} wants {Bounties[here]} gold.", MessageKind.Warn);
        }

        /// <summary>The fine is paid, the favour is called in or the sentence is served: the Watch here (and the neighbours that echoed it) forget.</summary>
        public void ClearBountyHere()
        {
            string here = RegionHere();
            Bounties.Remove(here);
            foreach (string n in NeighbourRegions(here)) Bounties.Remove(n);
        }

        // ------------------------------------------------------------------ seeing a crime

        bool Sees(Monster n)
        {
            if (n.IsDead || n.DownUntilDay > Today || n.Floor != TownZ) return false;
            if (Pathfinder.Chebyshev(n.X, n.Y, Player.X, Player.Y) > WitnessRange) return false;
            return Fov.HasLine(Map, n.X, n.Y, Player.X, Player.Y);
        }

        List<Monster> WitnessesOf(Monster victim, bool includeVictim)
        {
            var list = new List<Monster>();
            foreach (var n in Monsters)
            {
                if (!n.Townsperson || n.Role == TownRole.Pet || n.Role == TownRole.Prisoner) continue;
                if (n == victim && !includeVictim) continue;
                if (Sees(n)) list.Add(n);
            }
            return list;
        }

        void RaiseAlarm(List<Monster> witnesses)
        {
            bool guard = false;
            foreach (var w in witnesses)
            {
                if (w.IsGuard) { guard = true; w.HostileUntil = Turn + 60; }
                else if (w.Persona != null && w.Persona.Has(Trait.Coward)) w.HostileUntil = Turn + HostileSpan;   // runs, and tells
            }
            // A guard hears it from a witness: every guard of this floor comes.
            foreach (var m in Monsters) if (m.Townsperson && m.IsGuard) { m.HostileUntil = Math.Max(m.HostileUntil, Turn + 60); guard = true; }
            Say(guard ? "Someone shouts for the Watch!" : "Someone cries out in the street!", MessageKind.Warn);
        }

        // ------------------------------------------------------------------ hurting a person

        /// <summary>
        /// The hero strikes a townsperson. Essentials are knocked out, never killed by a blow; a blow at one who already lies
        /// out cold is an extreme act (see <see cref="Outrage"/>). Everyone else can die.
        /// </summary>
        public void Assault(Monster target)
        {
            if (target.IsDead) { Say("There is no point."); return; }
            if (target.Role == TownRole.Pet) { Say("The animal wants no part of it.", MessageKind.Neutral); return; }
            bool helpless = target.Persona != null && target.Persona.Essential && target.DownUntilDay > Today;
            MakeNoise(3);
            StruckAt(target);
            var res = Battles.PlayerMelee(Player, target, Rng, out _);
            Say(res.Message, res.Killed ? MessageKind.Kill : MessageKind.Combat);
            WoundFrom(target, res, Bodies.EdgedWeapon(Player), false);
            AfterBlow(res, false);
            Map.Version++;
            if (helpless) { Outrage(target); EndPlayerTurn(); return; }
            bool essential = target.Persona != null && target.Persona.Essential;
            bool killed = res.Killed && !essential;
            if (res.Killed && essential)
            {
                target.HP = 1; target.DownUntilDay = Today + 3; target.HostileUntil = 0;
                Say($"{target.Name} crumples, out cold. They will wake in a few days.", MessageKind.Warn);
            }
            int harm = res.Hit ? res.Damage : 0;
            var seen = WitnessesOf(target, !killed && target.DownUntilDay <= Today);

            if (killed) SlayPerson(target, seen);
            else if (res.Hit || harm > 0)
            {
                if (seen.Count > 0)
                {
                    int amount = Math.Min(150, 15 + 2 * harm) + (essential ? 100 : 0);
                    AddBounty(amount, essential ? "You laid hands on someone the town leans on." : "You struck a citizen.");
                    RaiseAlarm(seen);
                }
                if (target.DownUntilDay <= Today) target.HostileUntil = Turn + HostileSpan;
            }
            else if (target.DownUntilDay <= Today) target.HostileUntil = Turn + HostileSpan;   // a swing that missed is still a swing
            EndPlayerTurn();
        }

        /// <summary>
        /// A blow at an essential person who already lies out cold. It is deliberate and it counts: the first two leave them
        /// down, with a bounty and reputation lost; the third removes them, and the post passes to their apprentice (Succeed).
        /// </summary>
        void Outrage(Monster target)
        {
            string crime = TownText.L("You struck someone the town leans on while they lay helpless.", "Você feriu alguém de quem a cidade depende enquanto ele jazia indefeso.");
            target.Memory.Outrages++;
            AddRep(Houses.Watch, -6, crime);
            AddRep(Houses.Temple, -4, null);
            var seen = WitnessesOf(target, false);
            if (target.Memory.Outrages < 3)
            {
                target.HP = 1; target.DownUntilDay = Today + 3; target.HostileUntil = 0;
                Say(TownText.L($"{target.Name} is still out cold. One more blow like that, and they will not get up.",
                    $"{target.Name} continua desacordado. Mais um golpe desses, e não se levanta."), MessageKind.Warn);
                if (seen.Count > 0) { AddBounty(300, crime); RaiseAlarm(seen); }
                return;
            }
            target.HP = 0;   // the third blow: gone for good
            SlayPerson(target, seen);
            AddRep(Houses.Watch, -10, TownText.L("You removed someone the town leans on.", "Você removeu alguém de quem a cidade depende."));
            Succeed(target);
        }

        void SlayPerson(Monster m, List<Monster> seen)
        {
            Monsters.Remove(m);
            string key = Town != null ? Town.Name : "road";
            _murders[key] = (_murders.TryGetValue(key, out int k) ? k : 0) + 1;
            RecordDeed(Deed.Killed, m.Name, 5);
            if (m.IsPriest) AddRep(Houses.Temple, -15, "killing a priest");
            if (m.IsGuard) AddRep(Houses.Watch, -20, "killing a guard");
            AddCorruption(5, null);
            if (seen.Count > 0)
            {
                AddBounty(m.IsGuard || m.IsPriest ? 1500 : 1000, m.IsGuard ? "You killed a guard." : m.IsPriest ? "You killed a priest." : "You killed a citizen.");
                RaiseAlarm(seen);
            }
            else Say("No one saw it. The town will still know someone is gone.", MessageKind.Warn);
            Map.Version++;
        }

        public int MurdersIn(string town) => _murders.TryGetValue(town ?? "", out int k) ? k : 0;

        // ------------------------------------------------------------------ people against the hero

        /// <summary>A hostile person's turn: cowards run, the rest close in and strike. Returns false when they are calm again.</summary>
        bool HostileTurn(Monster m)
        {
            if (m.DownUntilDay > Today) return true;
            if (Sees(m)) m.HostileUntil = Math.Max(m.HostileUntil, Turn + HostileGrace);   // the grace begins when they lose sight
            int dist = Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y);
            bool brave = m.IsGuard || m.Persona == null || !m.Persona.Has(Trait.Coward);
            if (!brave) { if (dist < 8) FleeStep(m); return true; }
            if (dist == 1)
            {
                HurtBy(Article(m));
                var res = Battles.MeleeAttack(m, Player, Rng);
                Say(res.Message, res.Killed ? MessageKind.Death : MessageKind.Combat);
                if (Bodies.Wounding(res.Kind)) WoundFrom(Player, res, Bodies.EdgedAttack(res.Kind), false);
                ArmourTakesBlow(res);
                Map.Version++;
                CheckDeath();
                return true;
            }
            StepToward(m, Player.X, Player.Y);
            return true;
        }

        /// <summary>A guard who sees the hero while the Watch holds a bounty: the arrest.</summary>
        bool GuardWatch(Monster m)
        {
            if (Mode != GameMode.TownMap || UiState.Active != Panel.None || CurrentEvent != null || Turn < _arrestAfter) return false;
            if (m.HostileUntil > Turn) return false;   // already fighting
            int bounty = BountyHere();
            if (bounty <= 0 || !Sees(m) || Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y) > 6) return false;
            _arrestAfter = Turn + 6;
            Say("\"Halt! In the name of the Watch!\"", MessageKind.Warn);
            Talking = m; TalkBuilding = null;
            OpenDialogue(Dialogues.Arrest, m);
            return true;
        }

        public int SentenceDays(int bounty) => Math.Max(1, Math.Min(30, 1 + bounty / 100));

        /// <summary>Serve the time: days pass, the prison feeds you badly, timed quests keep running out, and the Watch forgets.</summary>
        public void ServeSentence()
        {
            int days = SentenceDays(BountyHere());
            PassHours(24 * days);
            Player.Nutrient = Math.Max(Math.Min(Player.Nutrient, 400), Player.Nutrient - 40 * days);
            Player.HP = Math.Max(1, Player.HP);
            ClearBountyHere();
            foreach (var n in Monsters) n.HostileUntil = 0;
            RecordDeed(Deed.Jailed, RegionHere(), days);
            Say($"You serve {days} day(s) in the cells. The Watch strikes your name from its book.", MessageKind.Warn);
            QuestCheck();
        }

        /// <summary>Called when the hero resists: every guard of the floor is against them, and the Watch adds to the bounty.</summary>
        public void ResistArrest()
        {
            foreach (var n in Monsters) if (n.Townsperson && n.IsGuard) n.HostileUntil = Turn + 60;
            AddBounty(40, "You resisted arrest.");
        }
    }
}
