using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.World
{
    /// <summary>Somebody in the chronicle: a smith, a king, a knight, a priest, a thief, a scholar or a warlord, or a hero of an earlier run.</summary>
    public sealed class Figure
    {
        public int Index;
        public string Name, Role, Epithet, EpithetPt, Place, House;
        public int Born, Died;
        /// <summary>Still living in the present year.</summary>
        public bool Alive;
        /// <summary>Where they lie, when they are buried below: a branch and a level (null when they lie elsewhere).</summary>
        public string TombBranch;
        public int TombDepth;
        public string Full => Name + " " + Epithet;
        public string FullPt => Name + " " + EpithetPt;
        public string Key => "f:" + Index;
    }

    /// <summary>One thing that happened, in both languages.</summary>
    public sealed class HistEvent
    {
        public int Index, Year;
        public string Kind, En, Pt, Place;
        public string Key => "e:" + Index;
    }

    /// <summary>A noble house: where it sat, when it rose, and when (if) it fell.</summary>
    public sealed class House
    {
        public string Name, Seat;
        public int Rose, Fell;
        public bool Standing => Fell == 0;
    }

    /// <summary>The generated past of a run: who lived, what happened, the houses and the kings, and when the Pit opened.</summary>
    public sealed class Chronicle
    {
        public int Now, PitOpened;
        public readonly List<Figure> Figures = new List<Figure>();
        public readonly List<HistEvent> Events = new List<HistEvent>();
        public readonly List<House> Houses = new List<House>();
        /// <summary>Every king in order, oldest first; the last one reigns now.</summary>
        public readonly List<Figure> Kings = new List<Figure>();
        public readonly List<string> Places = new List<string>();
    }

    /// <summary>
    /// The history pass: three centuries from the seed. Noble houses rise and fall; a line of kings succeeds itself by blood,
    /// by the sword or by the lords' choice; smiths, knights, priests, thieves, scholars and warlords are born and die, some
    /// still living, some buried in the levels below; towns are founded and razed, wars and plagues pass, and the Pit opens.
    /// A pure function of the seed (its own Rng, never the game's). Every line is born in English with its Portuguese beside it.
    /// Heroes of earlier runs (the graveyard) are added by the game on top, so the seed's history itself never changes.
    /// </summary>
    public static class History
    {
        static readonly (string En, string Pt)[] Epithets =
        {
            ("the Grey", "o Cinzento"), ("the Bold", "o Ousado"), ("the Lame", "o Coxo"), ("Ninefingers", "Nove-Dedos"), ("the Pious", "o Piedoso"),
            ("the Red", "o Vermelho"), ("Ashborn", "Nascido-das-Cinzas"), ("the Quiet", "o Calado"), ("Halfhand", "Meia-Mão"), ("the Drowned", "o Afogado"),
            ("the Younger", "o Moço"), ("the Elder", "o Velho"), ("Coldiron", "Ferro-Frio"), ("the Unlucky", "o Azarado"), ("Saltbeard", "Barba-de-Sal"),
            ("the Mild", "o Brando"), ("Blackhand", "Mão-Negra"), ("the Wanderer", "o Andarilho"), ("the Patient", "o Paciente"), ("Oathbreaker", "Quebra-Jura"),
        };

        public static readonly string[] PlaceNames =
        {
            "Ashford", "Greymere", "Holloway", "Saltmarsh", "Thornwick", "Coldwater", "Ravensmoor", "Duskfield", "Ironvale", "Marrowgate", "Wychwood", "Stonereach",
        };

        static readonly string[] HouseNames = { "Varn", "Ashcombe", "Morrow", "Blackwater", "Thorne", "Elsby", "Coldharbour", "Wren" };

        static readonly (string En, string Pt)[] Roles =
        {
            ("smith", "ferreiro"), ("knight", "cavaleiro"), ("priest", "sacerdote"), ("thief", "ladrão"), ("scholar", "erudito"), ("warlord", "senhor da guerra"),
            ("king", "rei"), ("delver", "aventureiro"),
        };

        static readonly string[] TombBranches = { "The Dungeons", "The Mines of Dwarfdeep", "The Warrens", "The Sunken Vaults", "The Ashen Spire" };
        static readonly int[] TombMax = { 9, 7, 8, 11, 14 };

        public static string RolePt(string role) { foreach (var r in Roles) if (r.En == role) return r.Pt; return role; }

        static readonly Dictionary<ulong, Chronicle> _cache = new Dictionary<ulong, Chronicle>();

        public static Chronicle Of(ulong seed)
        {
            if (_cache.TryGetValue(seed, out var c)) return c;
            if (_cache.Count > 8) _cache.Clear();
            c = Build(seed);
            _cache[seed] = c;
            return c;
        }

        static Chronicle Build(ulong seed)
        {
            var r = new Rng(seed ^ 0x415702EUL);
            var c = new Chronicle { Now = 300 + r.Range(0, 300) };
            int start = c.Now - 300;
            c.Places.AddRange(PlaceNames);

            // Houses: six of the eight, each rising somewhere; about half have fallen since.
            var houseNames = new List<string>(HouseNames);
            for (int i = 0; i < 6; i++)
            {
                string n = houseNames[r.Range(0, houseNames.Count)]; houseNames.Remove(n);
                var h = new House { Name = n, Seat = PlaceNames[r.Range(0, PlaceNames.Length)], Rose = start + r.Range(0, 150) };
                if (r.Chance(50)) h.Fell = Math.Min(c.Now - 5, h.Rose + r.Range(40, 160));
                c.Houses.Add(h);
            }

            // People: every role several times over, each living 30 to 70 years; those whose span reaches the present are alive.
            for (int i = 0; i < 30; i++)
            {
                var role = Roles[i % 6];
                var f = NewFigure(r, role.En, start + r.Range(0, 280), c);
                if (r.Chance(55)) f.House = c.Houses[r.Range(0, c.Houses.Count)].Name;
                c.Figures.Add(f);
            }

            // Kings: a line from the first house to the present, each succeeding by blood, by the sword or by election.
            int year = start + 20;
            string ruling = c.Houses[0].Name;
            Figure prev = null;
            while (year < c.Now)
            {
                var king = NewFigure(r, "king", year - r.Range(18, 40), c);
                int reign = r.Range(12, 41);
                king.Died = year + reign;
                if (king.Died >= c.Now) { king.Died = 0; king.Alive = true; }
                string how = "blood", howPt = "pelo sangue";
                if (prev != null)
                {
                    int roll = r.Range(0, 100);
                    if (roll < 25) { how = "sword"; howPt = "pela espada"; ruling = c.Houses[r.Range(0, c.Houses.Count)].Name; }
                    else if (roll < 40) { how = "choice"; howPt = "pela escolha dos senhores"; ruling = c.Houses[r.Range(0, c.Houses.Count)].Name; }
                }
                king.House = ruling;
                c.Figures.Add(king); c.Kings.Add(king);
                string before = prev == null ? "" : prev.Full;
                if (prev == null)
                    Add(c, year, "crowned", null, $"{king.Full} of House {ruling} was crowned the first king", $"{king.FullPt}, da Casa {ruling}, foi coroado o primeiro rei");
                else if (how == "blood")
                    Add(c, year, "crowned", null, $"{king.Full} of House {ruling} took the throne by blood after {before}", $"{king.FullPt}, da Casa {ruling}, herdou o trono de {prev.FullPt}");
                else if (how == "sword")
                    Add(c, year, "crowned", null, $"{king.Full} of House {ruling} took the throne by the sword from {before}", $"{king.FullPt}, da Casa {ruling}, tomou o trono de {prev.FullPt} pela espada");
                else
                    Add(c, year, "crowned", null, $"the lords chose {king.Full} of House {ruling} to follow {before}", $"os senhores escolheram {king.FullPt}, da Casa {ruling}, para suceder {prev.FullPt}");
                prev = king;
                year += reign;
            }

            // Houses in the chronicle: risen, and fallen.
            foreach (var h in c.Houses)
            {
                Add(c, h.Rose, "house", h.Seat, $"House {h.Name} rose at {h.Seat}", $"a Casa {h.Name} se ergueu em {h.Seat}");
                if (!h.Standing) Add(c, h.Fell, "house", h.Seat, $"House {h.Name} fell, and its hall at {h.Seat} was given to the crows", $"a Casa {h.Name} caiu, e seu salão em {h.Seat} ficou para os corvos");
            }

            // Events: founding, wars, plagues, a town razed, and the Pit.
            c.PitOpened = c.Now - r.Range(40, 120);
            for (int i = 0; i < 4; i++)
            {
                string p = PlaceNames[r.Range(0, PlaceNames.Length)];
                Add(c, start + r.Range(0, 120), "founded", p, $"{p} was founded on a river crossing", $"{p} foi fundada numa travessia de rio");
            }
            for (int i = 0; i < 4; i++)
            {
                string p = PlaceNames[r.Range(0, PlaceNames.Length)], q = PlaceNames[r.Range(0, PlaceNames.Length)];
                if (q == p) q = PlaceNames[(Array.IndexOf(PlaceNames, p) + 1) % PlaceNames.Length];
                Add(c, start + r.Range(60, 290), "war", p, $"the War of {p} and {q} was fought", $"foi travada a Guerra de {p} e {q}");
            }
            string[] plagues = { "Grey", "Weeping", "Red", "Quiet" }, plaguesPt = { "Cinzenta", "Chorosa", "Vermelha", "Calada" };
            for (int i = 0; i < 3; i++)
            {
                int k = r.Range(0, plagues.Length);
                string p = PlaceNames[r.Range(0, PlaceNames.Length)];
                Add(c, start + r.Range(40, 290), "plague", p, $"the {plagues[k]} Plague emptied {p}", $"a Peste {plaguesPt[k]} esvaziou {p}");
            }
            for (int i = 0; i < 2; i++)
            {
                string p = PlaceNames[r.Range(0, PlaceNames.Length)];
                Add(c, start + r.Range(100, 285), "razed", p, $"{p} was burned to the ground", $"{p} foi queimada até o chão");
            }
            Add(c, c.PitOpened, "pit", null, "the Pit opened and the dead began to be thrown into it", "o Poço se abriu e os mortos começaram a ser jogados nele");

            // Some of the dead lie below: knights, warlords, kings and priests buried in the levels of the Pit.
            foreach (var f in c.Figures)
            {
                if (f.Alive || (f.Role != "knight" && f.Role != "warlord" && f.Role != "king" && f.Role != "priest")) continue;
                if (f.Died < c.PitOpened || !r.Chance(45)) continue;
                int b = r.Range(0, TombBranches.Length);
                f.TombBranch = TombBranches[b];
                f.TombDepth = r.Range(2, TombMax[b] + 1);
                Add(c, f.Died, "buried", null, $"{f.Full} was carried down into the Pit and buried on {f.TombBranch} {f.TombDepth}",
                    $"{f.FullPt} foi levado ao Poço e enterrado em {f.TombBranch} {f.TombDepth}");
            }

            c.Events.Sort((a, b) => a.Year != b.Year ? a.Year.CompareTo(b.Year) : string.CompareOrdinal(a.En, b.En));
            for (int i = 0; i < c.Events.Count; i++) c.Events[i].Index = i;
            for (int i = 0; i < c.Figures.Count; i++) c.Figures[i].Index = i;
            return c;
        }

        static Figure NewFigure(Rng r, string role, int born, Chronicle c)
        {
            var ep = Epithets[r.Range(0, Epithets.Length)];
            var f = new Figure
            {
                Name = TownText.GivenName(r), Role = role, Epithet = ep.En, EpithetPt = ep.Pt, Place = PlaceNames[r.Range(0, PlaceNames.Length)],
                Born = born, Died = born + r.Range(30, 71),
            };
            if (f.Died >= c.Now) { f.Died = 0; f.Alive = true; }
            return f;
        }

        static void Add(Chronicle c, int year, string kind, string place, string en, string pt, Figure who = null) =>
            c.Events.Add(new HistEvent { Year = year, Kind = kind, Place = place, En = en, Pt = pt });

        static uint H(ulong seed, string key, int n) => Rumours.Hash(seed ^ 0xB10C0DEUL, key, n);

        static Figure Pick(Chronicle c, string role, uint h, int aliveIn = int.MinValue)
        {
            var pool = c.Figures.FindAll(f => (role == null || f.Role == role) && (aliveIn == int.MinValue || (f.Born + 16 <= aliveIn && (f.Alive || f.Died >= aliveIn))));
            if (pool.Count == 0) pool = c.Figures.FindAll(f => role == null || f.Role == role);
            return pool[(int)(h % (uint)pool.Count)];
        }

        static int End(Chronicle c, Figure f) => f.Alive ? c.Now : f.Died;

        /// <summary>
        /// A relic's story: who forged it, of what, for whom, who carried it, how it was lost. Each line comes as English with its
        /// Portuguese; <paramref name="keys"/> collects the figures and events it names, for the legends the hero learns.
        /// </summary>
        public static List<(string En, string Pt)> Biography(ulong seed, string key, string what, string material, List<string> keys = null)
        {
            var c = Of(seed);
            var lines = new List<(string, string)>();
            var smith = Pick(c, "smith", H(seed, key, 1));
            keys?.Add(smith.Key);
            int span = Math.Max(1, End(c, smith) - smith.Born - 20);
            int forged = Math.Min(c.Now - 20, smith.Born + 20 + (int)(H(seed, key, 2) % (uint)span));
            string mat = material ?? "iron";
            lines.Add(($"Forged of {mat} by {smith.Full}, a smith of {smith.Place}, in the year {forged}.",
                       $"Forjado de {Loc.MaterialPt(mat)} por {smith.FullPt}, ferreiro de {smith.Place}, no ano {forged}."));
            var owner = Pick(c, (H(seed, key, 3) & 1) == 0 ? "king" : "knight", H(seed, key, 4), forged);
            keys?.Add(owner.Key);
            lines.Add(($"Made for {owner.Full}, {owner.Role} of {owner.Place}.", $"Feito para {owner.FullPt}, {RolePt(owner.Role)} de {owner.Place}."));
            int year = Math.Max(forged, End(c, owner));
            var carriers = new List<Figure>();
            for (int i = 0; i < 2; i++)
            {
                if (year >= c.Now - 5) break;
                var f = Pick(c, null, H(seed, key, 5 + i), year + 1);
                if (f == owner || carriers.Contains(f) || End(c, f) <= year) break;
                carriers.Add(f);
                keys?.Add(f.Key);
                lines.Add(($"Carried by {f.Full}, {f.Role}, until the year {End(c, f)}.", $"Carregado por {f.FullPt}, {RolePt(f.Role)}, até o ano {End(c, f)}."));
                year = End(c, f);
            }
            HistEvent lost = null;
            foreach (var e in c.Events) if (e.Year >= year && (e.Kind == "war" || e.Kind == "plague" || e.Kind == "razed")) { lost = e; break; }
            if (lost == null || (H(seed, key, 9) % 3) == 0)
                lines.Add(($"Lost in the year {Math.Max(year, c.PitOpened)}, when it went down into the Pit with its last owner.",
                           $"Perdido no ano {Math.Max(year, c.PitOpened)}, quando desceu ao Poço com seu último dono."));
            else
            {
                keys?.Add(lost.Key);
                lines.Add(($"Lost in the year {lost.Year}, when {lost.En}.", $"Perdido no ano {lost.Year}, quando {lost.Pt}."));
            }
            return lines;
        }

        /// <summary>One line about a figure, in both languages.</summary>
        public static (string En, string Pt) Describe(Chronicle c, Figure f)
        {
            string house = f.House != null ? $" of House {f.House}" : "", housePt = f.House != null ? $", da Casa {f.House}," : "";
            if (f.Role == "delver")
                return ($"{f.Full}, a delver of an earlier age, {f.Place}.", $"{f.FullPt}, aventureiro de outra era, {f.Place}.");
            if (f.Alive)
                return ($"{f.Full}{house}, {f.Role} of {f.Place}, born in {f.Born}, still living.",
                        $"{f.FullPt}{housePt} {RolePt(f.Role)} de {f.Place}, nascido em {f.Born}, ainda vivo.");
            string tomb = f.TombBranch != null ? $" Buried on {f.TombBranch} {f.TombDepth}." : "", tombPt = f.TombBranch != null ? $" Enterrado em {f.TombBranch} {f.TombDepth}." : "";
            return ($"{f.Full}{house}, {f.Role} of {f.Place}, lived from {f.Born} to {f.Died}.{tomb}",
                    $"{f.FullPt}{housePt} {RolePt(f.Role)} de {f.Place}, viveu de {f.Born} a {f.Died}.{tombPt}");
        }

        /// <summary>One line about an event, in both languages.</summary>
        public static (string En, string Pt) Describe(HistEvent e) => ($"In the year {e.Year}, {e.En}.", $"No ano {e.Year}, {e.Pt}.");

        /// <summary>One entry of the chronicle by index (events, then figures), with the legend key it teaches.</summary>
        public static (string En, string Pt, string Key) Entry(ulong seed, int n)
        {
            var c = Of(seed);
            int total = c.Events.Count + c.Figures.Count;
            int i = ((n % total) + total) % total;
            if (i < c.Events.Count) { var (en, pt) = Describe(c.Events[i]); return (en, pt, c.Events[i].Key); }
            var f = c.Figures[i - c.Events.Count];
            var (fe, fp) = Describe(c, f);
            return (fe, fp, f.Key);
        }

        /// <summary>The dead buried on a level, if any.</summary>
        public static List<Figure> TombsOn(ulong seed, string branch, int depth) =>
            Of(seed).Figures.FindAll(f => f.TombBranch == branch && f.TombDepth == depth);
    }
}
