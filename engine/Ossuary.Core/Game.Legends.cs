using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// History in play. The hero learns the past piece by piece (chronicles in libraries, books on shelves, the Scholar, rumours,
    /// tavern songs, relic biographies, engravings and tombs below) and the Legends panel (F8) lists what they know. The overworld's
    /// ruins and keeps carry the chronicle's names, and the dead heroes of earlier runs (the graveyard) enter this world's history.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Legend keys the hero has learned: "e:i" events, "f:i" figures, "h:name" heroes of earlier runs.</summary>
        public readonly HashSet<string> Legends = new HashSet<string>();

        public Chronicle Chronicle => History.Of(Rng.Seed);

        /// <summary>Learns a piece of the past; true when it was new.</summary>
        public bool LearnLegend(string key) => !string.IsNullOrEmpty(key) && Legends.Add(key);

        /// <summary>A line said in English with its Portuguese registered.</summary>
        static string Both(string en, string pt) => TownText.L(en, pt);

        // ---------------------------------------------------------------- heroes of earlier runs

        List<Figure> _pastHeroes;

        /// <summary>The dead of earlier runs, as figures of this world's history: delvers who fell in the Pit before the hero came.</summary>
        public List<Figure> PastHeroes
        {
            get
            {
                if (_pastHeroes != null && _pastCount == (Graveyard?.Count ?? 0)) return _pastHeroes;
                _pastCount = Graveyard?.Count ?? 0;
                _pastHeroes = new List<Figure>();
                var c = Chronicle;
                if (Graveyard == null) return _pastHeroes;
                for (int i = 0; i < Graveyard.Count; i++)
                {
                    var b = Graveyard[i];
                    int died = c.Now - 2 - 3 * i;
                    _pastHeroes.Add(new Figure
                    {
                        Index = 1000 + i, Name = b.Name, Role = "delver", Epithet = "the " + b.Role, EpithetPt = Loc.PtOf("the " + b.Role),
                        Place = Both($"fell on {b.Branch} {b.Depth}, killed by {b.Cause}", $"caiu em {Loc.PtOf(b.Branch)} {b.Depth}, morto por {Loc.PtOf(b.Cause)}"),
                        Born = died - 20 - b.Level, Died = died, TombBranch = b.Branch, TombDepth = b.Depth,
                    });
                }
                return _pastHeroes;
            }
        }

        static string HeroKey(Figure f) => "h:" + f.Name;
        int _pastCount = -1;

        // ---------------------------------------------------------------- what is told

        /// <summary>A rumour from the past: an event, a figure, or a hero who died below. The hero learns it.</summary>
        public string HistoryRumour(int n)
        {
            var c = Chronicle;
            uint h = Rumours.Hash(Rng.Seed ^ 0x415EUL, Town?.Name ?? "road", n);
            var heroes = PastHeroes;
            if (heroes.Count > 0 && h % 4 == 0)
            {
                var f = heroes[(int)((h >> 4) % (uint)heroes.Count)];
                LearnLegend(HeroKey(f));
                return Both($"They still talk of {f.Name} {f.Epithet}, who {f.Place}.", $"Ainda falam de {f.Name} {f.EpithetPt}, que {Loc.PtOf(f.Place)}.");
            }
            if (h % 2 == 0)
            {
                var e = c.Events[(int)((h >> 8) % (uint)c.Events.Count)];
                LearnLegend(e.Key);
                return Both($"They say that in the year {e.Year}, {e.En}.", $"Dizem que no ano {e.Year}, {e.Pt}.");
            }
            var fig = c.Figures[(int)((h >> 8) % (uint)c.Figures.Count)];
            LearnLegend(fig.Key);
            var (en, pt) = History.Describe(c, fig);
            return Both("They say of " + en, "Dizem de " + pt);
        }

        int _scholarTold;

        /// <summary>What the Scholar tells of the old days: the chronicle in order, one entry at a time.</summary>
        public string ScholarLine()
        {
            var (en, pt, key) = History.Entry(Rng.Seed, 7 + 3 * _scholarTold++);
            LearnLegend(key);
            return Both("\"" + en + "\"", "\"" + pt + "\"");
        }

        int _songs;

        /// <summary>A tavern song about a war, a king, a buried knight or a hero who died below. The hero learns its subject.</summary>
        public string SongLine()
        {
            var c = Chronicle;
            int n = _songs++;
            var heroes = PastHeroes;
            if (heroes.Count > 0 && n % 3 == 2)
            {
                var f = heroes[n / 3 % heroes.Count];
                LearnLegend(HeroKey(f));
                return Both($"The bard sings of {f.Name} {f.Epithet}: down the stair with a lamp and a debt, and the Pit kept both.",
                            $"O bardo canta {f.Name} {f.EpithetPt}: escada abaixo com uma lanterna e uma dívida, e o Poço ficou com as duas.");
            }
            uint h = Rumours.Hash(Rng.Seed ^ 0x5096UL, Town?.Name ?? "road", n);
            var wars = c.Events.FindAll(e => e.Kind == "war" || e.Kind == "crowned" || e.Kind == "buried" || e.Kind == "razed");
            var ev = wars[(int)(h % (uint)wars.Count)];
            LearnLegend(ev.Key);
            return Both($"The bard sings of the year {ev.Year}, when {ev.En}. Everyone knows the chorus.",
                        $"O bardo canta o ano {ev.Year}, quando {ev.Pt}. Todo mundo sabe o refrão.");
        }

        /// <summary>A book taken down from a library shelf: a page of the chronicle, chosen by the shelf.</summary>
        public bool TryReadShelf(int x, int y)
        {
            var b = Town?.BuildingAt(x, y, TownZ);
            if (b == null || (b.Kind != BuildingKind.Library && b.Kind != BuildingKind.Emporium)) return false;
            Say(ReadShelf(x, y), MessageKind.Narrative);
            return true;
        }

        public string ReadShelf(int x, int y)
        {
            var (en, pt, key) = History.Entry(Rng.Seed, (int)(Rumours.Hash(Rng.Seed ^ 0xB00CUL, Town?.Name ?? "", x * 131 + y) % 4096));
            LearnLegend(key);
            return Both("You take down a worn book and read: " + en, "Você pega um livro gasto e lê: " + pt);
        }

        /// <summary>The ruins and keeps of the overworld take the chronicle's names: the towns that burned, the houses that fell.</summary>
        void NamePlaces()
        {
            if (World == null) return;
            var c = Chronicle;
            var razed = c.Events.FindAll(e => (e.Kind == "razed" || e.Kind == "war" || e.Kind == "plague") && e.Place != null);
            int ri = 0, ki = 0;
            for (int i = 0; i < World.Tiles.Length; i++)
            {
                var t = World.Tiles[i];
                if (t.Feature == OverworldFeature.Ruin && razed.Count > 0)
                {
                    string p = razed[ri++ % razed.Count].Place;
                    t.Name = Both($"the ruins of old {p}", $"as ruínas da velha {p}");
                }
                else if (t.Feature == OverworldFeature.Keep && c.Houses.Count > 0)
                {
                    var house = c.Houses[ki++ % c.Houses.Count];
                    t.Name = Both($"the keep of House {house.Name}", $"a fortaleza da Casa {house.Name}");
                }
                else continue;
                World.Tiles[i] = t;
            }
        }

        // ---------------------------------------------------------------- the legends panel

        /// <summary>What the hero knows of the past, as lines for the Legends panel: a header, then entries. Both languages registered.</summary>
        public List<(string Text, bool Header)> LegendLines()
        {
            var c = Chronicle;
            var lines = new List<(string, bool)>();
            var events = c.Events.FindAll(e => Legends.Contains(e.Key));
            var figures = c.Figures.FindAll(f => Legends.Contains(f.Key));
            var heroes = PastHeroes.FindAll(f => Legends.Contains(HeroKey(f)));
            var places = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var e in events) if (e.Place != null) places.Add(e.Place);
            foreach (var f in figures) places.Add(f.Place);
            lines.Add(($"The present year is {c.Now}. The Pit opened in {c.PitOpened}.", false));
            if (events.Count > 0)
            {
                lines.Add(("Events", true));
                foreach (var e in events) { var (en, pt) = History.Describe(e); lines.Add((Both(en, pt), false)); }
            }
            if (figures.Count > 0)
            {
                lines.Add(("People", true));
                foreach (var f in figures) { var (en, pt) = History.Describe(c, f); lines.Add((Both(en, pt), false)); }
            }
            if (places.Count > 0)
            {
                lines.Add(("Places", true));
                foreach (string p in places)
                {
                    int n = events.FindAll(e => e.Place == p).Count + figures.FindAll(f => f.Place == p).Count;
                    lines.Add((Both($"{p}: {n} thing(s) you know of.", $"{p}: {n} coisa(s) que você sabe."), false));
                }
            }
            if (heroes.Count > 0)
            {
                lines.Add(("Heroes before you", true));
                foreach (var f in heroes) lines.Add((Both($"{f.Name} {f.Epithet}, who {f.Place}.", $"{f.Name} {f.EpithetPt}, que {Loc.PtOf(f.Place)}."), false));
            }
            if (Legends.Count == 0) lines.Add(("You know nothing of the past yet. Libraries, scholars, bards and old walls do.", false));
            return lines;
        }

        public int LegendsKnown => Legends.Count;

        // ---------------------------------------------------------------- the hero's own legend

        /// <summary>
        /// What this hero will be remembered for, in a few lines: the bosses they killed, the relics they carried, the towns they
        /// saved, the works they made. The morgue prints it, and their bones carry it into the next world's history.
        /// </summary>
        public List<string> LegendOf()
        {
            var lines = new List<string>();
            var bossNames = new HashSet<string>();
            foreach (string id in BossesSlain) { var b = Bosses.Find(id); if (b == null) continue; bossNames.Add(b.Name); lines.Add(Both($"slew the {b.Name}", $"matou {Loc.PtOf("the " + b.Name)}")); }
            foreach (var d in Ledger) if (d.Kind == Deed.Killed && !bossNames.Contains(d.Subject) && lines.Count < 6)
                    lines.Add(Both($"killed {d.Subject}", $"matou {Loc.PtOf(d.Subject)}"));
            foreach (string w in Player.Works) lines.Add(Both($"made {w}", $"fez {Loc.PtOf(w)}"));
            foreach (string r in RaidChronicle) if (r.StartsWith(Player.CharName + " beat off")) lines.Add(r.TrimEnd('.'));
            var relics = new List<Items.Item>();
            if (Player.Wielded != null && Player.Wielded.IsRelic) relics.Add(Player.Wielded);
            foreach (var piece in Player.WornPieces()) if (piece.IsRelic) relics.Add(piece);
            foreach (var it in relics) lines.Add(Both($"carried {it.Name}", $"carregou {Loc.PtOf(it.Name)}"));
            if (lines.Count > 8) lines.RemoveRange(8, lines.Count - 8);
            return lines;
        }
    }
}
