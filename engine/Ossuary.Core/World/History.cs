using System;
using System.Collections.Generic;
using Ossuary.Core.Items;

namespace Ossuary.Core.World
{
    /// <summary>Somebody in the chronicle: a smith, a king, a knight, a priest, a thief, a scholar or a warlord.</summary>
    public sealed class Figure
    {
        public string Name, Role, Epithet, EpithetPt, Place;
        public int Born, Died;
        public string Full => Name + " " + Epithet;
        public string FullPt => Name + " " + EpithetPt;
    }

    /// <summary>One thing that happened, in both languages.</summary>
    public sealed class HistEvent
    {
        public int Year;
        public string Kind, En, Pt, Place;
    }

    /// <summary>The generated past of a run: who lived, what happened, and when the Pit opened.</summary>
    public sealed class Chronicle
    {
        public int Now, PitOpened;
        public readonly List<Figure> Figures = new List<Figure>();
        public readonly List<HistEvent> Events = new List<HistEvent>();
    }

    /// <summary>
    /// The history pass: three centuries from the seed, people with births and deaths, wars, plagues, towns founded and razed, and
    /// the year the Ossuary opened. A pure function of the seed (its own Rng, never the game's), read by relic biographies and
    /// the chronicles in libraries. Every line is born in English with its Portuguese beside it.
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

        static readonly string[] Places =
        {
            "Ashford", "Greymere", "Holloway", "Saltmarsh", "Thornwick", "Coldwater", "Ravensmoor", "Duskfield", "Ironvale", "Marrowgate", "Wychwood", "Stonereach",
        };

        static readonly (string En, string Pt)[] Roles =
        {
            ("smith", "ferreiro"), ("king", "rei"), ("knight", "cavaleiro"), ("priest", "sacerdote"), ("thief", "ladrão"), ("scholar", "erudito"), ("warlord", "senhor da guerra"),
        };

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
            // People: every role several times over three centuries, each living 30 to 70 years.
            for (int i = 0; i < 28; i++)
            {
                var role = Roles[i % Roles.Length];
                var ep = Epithets[r.Range(0, Epithets.Length)];
                int born = start + r.Range(0, 260);
                var f = new Figure
                {
                    Name = TownText.GivenName(r), Role = role.En, Epithet = ep.En, EpithetPt = ep.Pt, Place = Places[r.Range(0, Places.Length)],
                    Born = born, Died = Math.Min(c.Now - 1, born + r.Range(30, 71)),
                };
                c.Figures.Add(f);
            }
            c.Figures.Sort((a, b) => a.Born.CompareTo(b.Born));
            // Events: founding, wars, plagues, a town razed, and the Pit.
            c.PitOpened = c.Now - r.Range(40, 120);
            for (int i = 0; i < 4; i++)
            {
                string p = Places[r.Range(0, Places.Length)];
                Add(c, start + r.Range(0, 120), "founded", p, $"{p} was founded on a river crossing.", $"{p} foi fundada numa travessia de rio.");
            }
            for (int i = 0; i < 4; i++)
            {
                string p = Places[r.Range(0, Places.Length)], q = Places[r.Range(0, Places.Length)];
                if (q == p) q = Places[(Array.IndexOf(Places, p) + 1) % Places.Length];
                int y = start + r.Range(60, 290);
                Add(c, y, "war", p, $"the War of {p} and {q}", $"a Guerra de {p} e {q}");
            }
            string[] plagues = { "Grey", "Weeping", "Red", "Quiet" }, plaguesPt = { "Cinzenta", "Chorosa", "Vermelha", "Calada" };
            for (int i = 0; i < 3; i++)
            {
                int k = r.Range(0, plagues.Length);
                string p = Places[r.Range(0, Places.Length)];
                Add(c, start + r.Range(40, 290), "plague", p, $"the {plagues[k]} Plague emptied {p}", $"a Peste {plaguesPt[k]} esvaziou {p}");
            }
            {
                string p = Places[r.Range(0, Places.Length)];
                Add(c, start + r.Range(100, 280), "razed", p, $"{p} was burned to the ground", $"{p} foi queimada até o chão");
            }
            Add(c, c.PitOpened, "pit", null, "the Pit opened and the dead began to be thrown into it", "o Poço se abriu e os mortos começaram a ser jogados nele");
            c.Events.Sort((a, b) => a.Year.CompareTo(b.Year));
            return c;
        }

        static void Add(Chronicle c, int year, string kind, string place, string en, string pt) =>
            c.Events.Add(new HistEvent { Year = year, Kind = kind, Place = place, En = en, Pt = pt });

        static uint H(ulong seed, string key, int n) => Rumours.Hash(seed ^ 0xB10C0DEUL, key, n);

        static Figure Pick(Chronicle c, string role, uint h, int aliveIn = int.MinValue)
        {
            var pool = c.Figures.FindAll(f => (role == null || f.Role == role) && (aliveIn == int.MinValue || (f.Born + 16 <= aliveIn && f.Died >= aliveIn)));
            if (pool.Count == 0) pool = c.Figures.FindAll(f => role == null || f.Role == role);
            return pool[(int)(h % (uint)pool.Count)];
        }

        /// <summary>
        /// A relic's story: who forged it, of what, for whom, who carried it, how it was lost. Each line comes as English with its
        /// Portuguese; the English is what the game says, and the Portuguese is registered beside it.
        /// </summary>
        public static List<(string En, string Pt)> Biography(ulong seed, string key, string what, string material)
        {
            var c = Of(seed);
            var lines = new List<(string, string)>();
            var smith = Pick(c, "smith", H(seed, key, 1));
            int forged = Math.Min(c.Now - 20, smith.Born + 20 + (int)(H(seed, key, 2) % (uint)Math.Max(1, smith.Died - smith.Born - 20)));
            string mat = material ?? "iron";
            lines.Add(($"Forged of {mat} by {smith.Full}, a smith of {smith.Place}, in the year {forged}.",
                       $"Forjado de {Loc.MaterialPt(mat)} por {smith.FullPt}, ferreiro de {smith.Place}, no ano {forged}."));
            var owner = Pick(c, (H(seed, key, 3) & 1) == 0 ? "king" : "knight", H(seed, key, 4), forged);
            lines.Add(($"Made for {owner.Full}, {owner.Role} of {owner.Place}.", $"Feito para {owner.FullPt}, {RolePt(owner.Role)} de {owner.Place}."));
            int year = Math.Max(forged, owner.Died);
            var carriers = new List<Figure>();
            for (int i = 0; i < 2; i++)
            {
                if (year >= c.Now - 5) break;
                var f = Pick(c, null, H(seed, key, 5 + i), year + 1);
                if (f == owner || carriers.Contains(f) || f.Died <= year) break;
                carriers.Add(f);
                lines.Add(($"Carried by {f.Full}, {f.Role}, until the year {f.Died}.", $"Carregado por {f.FullPt}, {RolePt(f.Role)}, até o ano {f.Died}."));
                year = f.Died;
            }
            // Lost: in the first war, plague or razing after the last holder died, or thrown into the Pit with the dead.
            HistEvent lost = null;
            foreach (var e in c.Events) if (e.Year >= year && e.Kind != "founded") { lost = e; break; }
            if (lost == null || lost.Kind == "pit" || (H(seed, key, 9) % 3) == 0)
                lines.Add(($"Lost in the year {Math.Max(year, c.PitOpened)}, when it went down into the Pit with its last owner.",
                           $"Perdido no ano {Math.Max(year, c.PitOpened)}, quando desceu ao Poço com seu último dono."));
            else
                lines.Add(($"Lost in the year {lost.Year}, when {lost.En}.", $"Perdido no ano {lost.Year}, quando {lost.Pt}."));
            return lines;
        }

        /// <summary>One entry of the chronicle by index, as a line read aloud from a library's books.</summary>
        public static (string En, string Pt) Entry(ulong seed, int n)
        {
            var c = Of(seed);
            int total = c.Events.Count + c.Figures.Count;
            int i = ((n % total) + total) % total;
            if (i < c.Events.Count)
            {
                var e = c.Events[i];
                return ($"In the year {e.Year}, {e.En}.", $"No ano {e.Year}, {e.Pt}.");
            }
            var f = c.Figures[i - c.Events.Count];
            return ($"{f.Full}, {f.Role} of {f.Place}, lived from {f.Born} to {f.Died}.",
                    $"{f.FullPt}, {RolePt(f.Role)} de {f.Place}, viveu de {f.Born} a {f.Died}.");
        }
    }
}
