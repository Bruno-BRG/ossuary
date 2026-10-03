using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    public enum RTruth { True, Partial, False }

    /// <summary>A piece of news that points at something real: a branch boss or a place on the map. It can be true, vague or wrong.</summary>
    public sealed class Rumour
    {
        public string Id, Text;
        public RTruth Truth;
    }

    /// <summary>
    /// Rumours with teeth. A fact is picked from the generated world (a boss lair, a named place), the teller's temperament decides
    /// how much of it survives (drunks mix things up, scholars rarely do), and a true one acts: a place is marked on the map,
    /// a boss becomes a Region quest. Everything is a pure function of the seed, the town and how many times the hero has asked.
    /// </summary>
    public static class Rumours
    {
        static string L(string en, string pt) => TownText.L(en, pt);

        internal static uint Hash(ulong a, string s, int n)
        {
            ulong h = 1469598103934665603UL ^ a;
            foreach (char c in s ?? "") { h ^= c; h *= 1099511628211UL; }
            h ^= (ulong)(n + 1) * 0x9E3779B97F4A7C15UL; h *= 1099511628211UL;
            h ^= h >> 29;
            return (uint)(h ^ (h >> 32));
        }

        /// <summary>Chance, in percent, that this teller gets the fact wrong; and that they only half remember it.</summary>
        static void Reliability(Monster teller, out int wrong, out int vague)
        {
            wrong = 20; vague = 25;
            var p = teller?.Persona;
            if (teller != null && teller.Role == TownRole.Drunk) { wrong = 55; vague = 25; }
            else if (p != null && (p.Has(Trait.Curious) || teller.Role == TownRole.Scholar)) { wrong = 5; vague = 15; }
            else if (p != null && p.Has(Trait.Coward)) { wrong = 30; vague = 25; }
            else if (p != null && p.Has(Trait.Proud)) { wrong = 12; vague = 30; }
        }

        sealed class Fact { public string Kind; public BossDef Boss; public int Tile; public string Region; }

        static List<Fact> Facts(Game g)
        {
            var list = new List<Fact>();
            foreach (var b in Bosses.All)
                if (b.Id != "annex-warden" && !g.BossesSlain.Contains(b.Id)) list.Add(new Fact { Kind = "boss", Boss = b });
            var w = g.World;
            if (w != null)
                for (int i = 0; i < w.Tiles.Length; i++)
                {
                    var t = w.Tiles[i];
                    if (t.Discovered || string.IsNullOrEmpty(t.Name)) continue;
                    if (t.Feature == OverworldFeature.Ruin || t.Feature == OverworldFeature.Shrine || t.Feature == OverworldFeature.Cave
                        || t.Feature == OverworldFeature.Mine || t.Feature == OverworldFeature.Keep)
                        list.Add(new Fact { Kind = "place", Tile = i, Region = w.RegionAt(i % w.W, i / w.W).Name });
                }
            return list;
        }

        static readonly Dictionary<string, QuestDef> BossQuests = new Dictionary<string, QuestDef>();

        static QuestDef BossQuest(BossDef b)
        {
            if (BossQuests.TryGetValue(b.Id, out var q)) return q;
            q = new QuestDef { Id = "region.boss." + b.Id, Track = QuestDef.Region, Giver = "a rumour", RewardGold = 120,
                Title = L("A king in the dark", "Um rei no escuro") };
            q.Step(ObjKind.Reach, L("Go down to where the rumour says it waits.", "Desça até onde o rumor diz que ele espera."), b.Branch, b.Depth, b.Branch, b.Name + ", " + b.Branch + " " + b.Depth)
             .Step(ObjKind.Flag, L("Bring it down.", "Derrube-o."), "boss.slain." + b.Id, 1, b.Branch, b.Name);
            q.OnComplete = g => g.AddRep(Houses.Guild, 5, "putting down a king of the dark");
            BossQuests[b.Id] = q;
            return q;
        }

        /// <summary>The next rumour of this town for this teller; the hero learns it (and a true one takes effect).</summary>
        public static Rumour Tell(Game g, Monster teller, int n)
        {
            var facts = Facts(g);
            if (facts.Count == 0) return null;
            string town = g.Town != null ? g.Town.Name : "road";
            uint h = Hash(g.Rng.Seed, town, n);
            var f = facts[(int)(h % (uint)facts.Count)];
            Reliability(teller, out int wrong, out int vague);
            int roll = (int)((h >> 8) % 100);
            var truth = roll < wrong ? RTruth.False : roll < wrong + vague ? RTruth.Partial : RTruth.True;
            var r = new Rumour { Truth = truth };

            if (f.Kind == "boss")
            {
                var b = f.Boss;
                r.Id = "boss." + b.Id;
                int depth = truth == RTruth.False ? Math.Max(1, b.Depth + (h % 2 == 0 ? 2 : -3)) : b.Depth;
                r.Text = truth == RTruth.Partial
                    ? $"Something old and angry waits in the deep of {b.Branch}."
                    : $"The {b.Name} waits on level {depth} of {b.Branch}.";
                if (truth != RTruth.False) g.StartQuest(BossQuest(b));
            }
            else
            {
                var t = g.World.Tiles[f.Tile];
                r.Id = "place." + f.Tile;
                string region = f.Region;
                if (truth == RTruth.False)
                {
                    var others = new List<string>();
                    foreach (var rg in g.World.Regions) if (rg.Name != region) others.Add(rg.Name);
                    if (others.Count > 0) region = others[(int)((h >> 4) % (uint)others.Count)];
                }
                r.Text = truth == RTruth.Partial
                    ? $"They say something worth finding lies in {region}."
                    : $"They say {t.Name} in {region} still holds something worth the walk.";
                if (truth == RTruth.True) { g.World.Tiles[f.Tile].Discovered = true; g.Say("You mark it on your map.", MessageKind.Quest); }
            }
            g.LearnRumour(r);
            return r;
        }

        public static void Translations(Action<string, string> rx)
        {
            rx(@"The (.+) waits on level (\d+) of (.+)\.", "$1 espera no nível $2 de $3.");
            rx(@"Something old and angry waits in the deep of (.+)\.", "Algo velho e furioso espera no fundo de $1.");
            rx(@"They say (.+) in (.+) still holds something worth the walk\.", "Dizem que $1 em $2 ainda guarda algo que vale a caminhada.");
            rx(@"They say something worth finding lies in (.+)\.", "Dizem que há algo que vale a pena achar em $1.");
        }
    }
}
