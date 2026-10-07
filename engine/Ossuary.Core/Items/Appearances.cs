using System.Collections.Generic;

namespace Ossuary.Core.Items
{
    /// <summary>
    /// What an unknown potion, scroll or wand looks like. Each kind gets a look from a hash of the run's seed and its name, probing
    /// past looks already taken, so within a run no two kinds look the same and a new seed shuffles them all. Pure data: the game
    /// keeps which kinds the hero has learned (Game.Identify.cs).
    /// </summary>
    public static class Appearances
    {
        /// <summary>The seed of the run being played. Set by the game; names only, never the simulation.</summary>
        public static ulong Seed;

        public static readonly string[] PotionMoods = { "murky", "bubbling", "smoky", "milky", "fizzing", "viscous", "cloudy", "glowing", "oily", "brackish", "clear", "swirling", "thick", "foaming", "sparkling", "dull" };
        public static readonly string[] PotionColours = { "amber", "crimson", "inky", "golden", "pearl", "rust", "bone", "violet", "green", "black", "blue", "silver", "pink", "grey", "orange", "white" };
        public static readonly string[] WandShapes = { "twisted", "straight", "knotted", "carved", "slender", "heavy", "runed", "forked" };
        public static readonly string[] WandStuffs = { "oak", "bone", "iron", "glass", "ebony", "copper", "crystal", "ivory", "pine", "jade", "brass", "marble", "yew", "ash", "tin", "horn" };
        public static readonly string[] ScrollWords =
        {
            "ZELGO", "MER", "VELOX", "NEB", "FOOBIE", "BLETCH", "PRATYAVAYAH", "ELBIB", "YLOH", "VERR", "YED", "THARR", "KERNOD", "WEL",
            "ELAM", "EBOW", "DAIYEN", "FOOELS", "GARVEN", "DEH", "JUYED", "AWK", "HACKEM", "MUCHE", "XIXAXA", "XOXAXA", "VENZAR", "BORGO",
        };

        static readonly Dictionary<ulong, Dictionary<string, string>> _byRun = new Dictionary<ulong, Dictionary<string, string>>();

        /// <summary>Whether this kind of thing starts unknown.</summary>
        public static bool Hidden(ItemDef d) => d.Kind == ItemKind.Potion || d.Kind == ItemKind.Scroll || d.Kind == ItemKind.Wand;

        public static string For(ItemDef d) => For(Seed, d);

        public static string For(ulong seed, ItemDef d)
        {
            if (!Hidden(d) || d.Name == null) return null;
            if (!_byRun.TryGetValue(seed, out var map))
            {
                if (_byRun.Count > 8) _byRun.Clear();
                map = Build(seed);
                _byRun[seed] = map;
            }
            return map.TryGetValue(d.Name, out var look) ? look : Look(d.Kind, (int)(Rumours.Hash(seed, d.Name, 7) % 4096));
        }

        static Dictionary<string, string> Build(ulong seed)
        {
            var map = new Dictionary<string, string>();
            var taken = new HashSet<string>();
            void Assign(IReadOnlyList<ItemDef> list)
            {
                foreach (var d in list)
                {
                    if (d.Name == null || map.ContainsKey(d.Name)) continue;
                    int i = (int)(Rumours.Hash(seed ^ 0xA77EA2UL, d.Name, 1) % 4096);
                    string look = Look(d.Kind, i);
                    for (int probe = 1; taken.Contains(look) && probe < 4096; probe++) look = Look(d.Kind, i + probe);
                    taken.Add(look);
                    map[d.Name] = look;
                }
            }
            Assign(Catalogue.Potions); Assign(Catalogue.Scrolls); Assign(Catalogue.Wands);
            return map;
        }

        static string Look(ItemKind k, int i)
        {
            switch (k)
            {
                case ItemKind.Potion: { int n = PotionMoods.Length * PotionColours.Length; i %= n; return PotionMoods[i / PotionColours.Length] + " " + PotionColours[i % PotionColours.Length] + " potion"; }
                case ItemKind.Wand: { int n = WandShapes.Length * WandStuffs.Length; i %= n; return WandShapes[i / WandStuffs.Length] + " " + WandStuffs[i % WandStuffs.Length] + " wand"; }
                default: { int n = ScrollWords.Length * ScrollWords.Length; i %= n; return "scroll labelled " + ScrollWords[i / ScrollWords.Length] + " " + ScrollWords[i % ScrollWords.Length]; }
            }
        }
    }
}
