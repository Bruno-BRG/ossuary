using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;

namespace Ossuary.Core
{
    /// <summary>A monster that met the hero and lived, remembered for the run: its name, its titles and where it holds.</summary>
    public sealed class Survivor
    {
        public string Id;
        public string Name;
        /// <summary>The species it was (bestiary name), for the morgue.</summary>
        public string Species;
        public string Branch;
        public int Depth, Level;
        public bool Slain;
        public readonly List<string> Titles = new List<string>();
    }

    /// <summary>
    /// Named survivors (Version 24). A monster that trades a blow with the hero and is still standing when the hero walks away
    /// earns a name, a title and a memory; it keeps the level it holds, and may follow. Everything here is a pure function of
    /// what already happened (the monster's uid, its wounds, what it did), never of the simulation RNG: a save is a replay.
    /// </summary>
    public sealed partial class Game
    {
        public readonly List<Survivor> Survivors = new List<Survivor>();

        /// <summary>
        /// Called when the hero leaves a level: survivors he has met grow and may earn another title, and at most two of the
        /// living monsters that traded blows with him take a name.
        /// </summary>
        void PromoteSurvivors()
        {
            if (Map == null || Mode != GameMode.Dungeon) return;

            for (int i = 0; i < Monsters.Count; i++)
            {
                var m = Monsters[i];
                if (m.IsDead || m.Companion || m.Ally || m.SurvivorId == null || !m.Encountered) continue;
                m.SurvivorMet++;
                m.Encountered = false;
                if (m.SurvivorMet >= 2 && m.SurvivorGrowth < 5) Grow(m);
                else m.SurvivorHurtHero = false;
                var rec = FindSurvivor(m.SurvivorId);
                if (rec != null) { rec.Level = m.Level; rec.Titles.Clear(); rec.Titles.AddRange(m.SurvivorTitles); }
            }

            var candidates = new List<Monster>();
            for (int i = 0; i < Monsters.Count; i++)
            {
                var m = Monsters[i];
                if (m.IsDead || !m.Encountered || m.SurvivorId != null || m.Companion || m.Ally) continue;
                if (m.Townsperson || m.Folk != FolkKind.None || !m.IsThreat || m.Uid == 0) continue;
                candidates.Add(m);
            }
            candidates.Sort((a, b) =>
                a.Level != b.Level ? b.Level.CompareTo(a.Level)
                : a.HP != b.HP ? b.HP.CompareTo(a.HP)
                : a.Uid.CompareTo(b.Uid));
            for (int i = 0; i < candidates.Count && i < 2; i++) Promote(candidates[i]);
        }

        void Promote(Monster m)
        {
            m.SurvivorId = "s" + m.Uid;
            m.Name = TownText.SurvivorName(m.Uid);
            m.SurvivorMet = 1;
            m.Encountered = false;
            AddTitle(m, FirstTitle(m));
            m.SurvivorHurtHero = false;
            var rec = new Survivor
            {
                Id = m.SurvivorId, Name = m.Name, Species = m.Def.Name,
                Branch = Branch, Depth = Depth, Level = m.Level,
            };
            rec.Titles.AddRange(m.SurvivorTitles);
            Survivors.Add(rec);
        }

        static string FirstTitle(Monster m)
        {
            if (m.SurvivorHurtHero) return "Biter of Heroes";
            if (m.Fled) return "the Sly";
            if (m.HP < m.MaxHP) return "the Unbroken";
            return "the Patient";
        }

        static void AddTitle(Monster m, string title)
        {
            if (title == null || m.SurvivorTitles.Contains(title) || m.SurvivorTitles.Count >= 3) return;
            m.SurvivorTitles.Add(title);
        }

        /// <summary>Outliving the hero a second time is worth a level, slowly and with a ceiling.</summary>
        void Grow(Monster m)
        {
            m.SurvivorGrowth++;
            m.Level++;
            m.MaxHP += 2;
            m.HP = Math.Min(m.MaxHP, m.HP + 2);
            m.XpKill = Math.Max(m.XpKill, m.Level * m.Level + m.Def.Difficulty);
            string[] titles = { "the Twice-Lived", "the Unkillable", "the Name That Stays" };
            AddTitle(m, titles[Math.Min(m.SurvivorGrowth - 1, titles.Length - 1)]);
            m.SurvivorHurtHero = false;
        }

        Survivor FindSurvivor(string id)
        {
            for (int i = 0; i < Survivors.Count; i++) if (Survivors[i].Id == id) return Survivors[i];
            return null;
        }

        void SurvivorSlain(Monster m)
        {
            var rec = FindSurvivor(m.SurvivorId);
            if (rec == null) return;
            rec.Slain = true;
            Say($"{m.Name} was a name in the deep, and is one no longer.", MessageKind.Kill);
        }

        /// <summary>The name and its newest title, for a rumour or a look: "Grak, Biter of Heroes".</summary>
        string SurvivorFullName(Monster m)
        {
            if (m.SurvivorTitles.Count == 0) return m.Name;
            return m.Name + ", " + m.SurvivorTitles[m.SurvivorTitles.Count - 1];
        }

        /// <summary>
        /// A rumour with teeth: one living survivor, named with its newest title and the level it holds. Null when the run has
        /// no living survivor, so the caller falls through to the other bands.
        /// </summary>
        string SurvivorRumour(int n)
        {
            var alive = new List<Survivor>();
            foreach (var s in Survivors) if (!s.Slain) alive.Add(s);
            if (alive.Count == 0) return null;
            var pick = alive[(int)(Rumours.Hash(Rng.Seed, "survivor-rumour", n) % (uint)alive.Count)];
            string title = pick.Titles.Count > 0 ? ", " + pick.Titles[pick.Titles.Count - 1] : "";
            return $"They say {pick.Name}{title} holds level {pick.Depth} of {pick.Branch}.";
        }

        /// <summary>A survivor from a shallower level of this branch has followed the hero down.</summary>
        void SurvivorFollows()
        {
            if (Map == null || Mode != GameMode.Dungeon) return;
            for (int i = 0; i < Survivors.Count; i++)
            {
                var s = Survivors[i];
                if (s.Slain || s.Branch != Branch || s.Depth >= Depth || Depth - s.Depth > 3) continue;
                if (Rumours.Hash(Rng.Seed, "survivor-follow-" + s.Id, Turn) % 100 >= 12) continue;
                var m = Dungeon.TakeFromRosters(s.Id, out _);
                if (m == null || m.IsDead) continue;
                if (!PlaceNearPlayer(m))
                {
                    int f = DungeonGen.FindAnyFloor(Map);
                    if (f < 0) continue;
                    m.X = f % Map.W; m.Y = f / Map.W;
                }
                m.HomeX = m.X; m.HomeY = m.Y;
                Monsters.Add(m);
                s.Depth = Depth;
                s.Branch = Branch;
                Say($"Something follows you down: {SurvivorFullName(m)}.", MessageKind.Warn);
                return;
            }
        }

        /// <summary>Puts a follower on a free floor cell close to the hero, never on top of them.</summary>
        bool PlaceNearPlayer(Monster m)
        {
            for (int radius = 2; radius <= 8; radius++)
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                        int x = Player.X + dx, y = Player.Y + dy;
                        if (!Map.InBounds(x, y) || !Map.Walkable(x, y) || MonsterAt(x, y) != null) continue;
                        var t = Map.Get(x, y);
                        if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                        m.X = x; m.Y = y; return true;
                    }
            return false;
        }

        /// <summary>
        /// The detailed look: build and age from a stable hash of the uid (never the simulation RNG), mood from what it is
        /// doing now, then its gear and its wounds (the caller adds the latter). Null for anything that is not a monster.
        /// </summary>
        public string DescribeLook(Monster m)
        {
            if (m == null) return null;
            string who = m.SurvivorId != null ? SurvivorFullName(m) : m.Subj;
            return $"{who} looks {LookPhrase(m)}.";
        }

        static string LookPhrase(Monster m)
        {
            string[] builds = { "lean and quick", "broad and slow", "scarred and wary", "hunched and old", "gaunt and hungry", "wiry and young" };
            uint h = Rumours.Hash((ulong)m.Uid, "survivor-look", m.Level);
            if (m.Asleep) return "heavy with sleep";
            if (m.FearTurns > 0) return "ready to run";
            if (m.HP * 3 < m.MaxHP) return "wounded and desperate";
            return builds[h % (uint)builds.Length];
        }

        /// <summary>The gear it is holding, when it is worth naming.</summary>
        public string DescribeGear(Monster m)
        {
            if (m == null || m.Wielded == null) return null;
            return $"It grips {m.Wielded.Name}.";
        }
    }
}
