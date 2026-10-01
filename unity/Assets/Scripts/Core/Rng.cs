using System;

namespace Ossuary.Core
{
    /// <summary>
    /// xoshiro256** — small, fast, deterministic. The whole game is reproducible from
    /// (seed, sequence of player decisions), which is what makes a roguelike fair and
    /// what lets us replay a run from a seed alone.
    /// </summary>
    public sealed class Rng
    {
        ulong s0, s1, s2, s3;

        public ulong Seed { get; private set; }
        public long Calls { get; private set; }

        public Rng(ulong seed)
        {
            Seed = seed;
            // SplitMix64 to spread a single seed across the four words.
            ulong z = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
            s0 = SplitMix(ref z);
            s1 = SplitMix(ref z);
            s2 = SplitMix(ref z);
            s3 = SplitMix(ref z);
        }

        public static Rng FromString(string text)
        {
            ulong h = 1469598103934665603UL;
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    h ^= text[i];
                    h *= 1099511628211UL;
                }
            }
            return new Rng(h);
        }

        static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public ulong NextULong()
        {
            Calls++;
            ulong result = Rotl(s1 * 5, 7) * 9;
            ulong t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = Rotl(s3, 45);
            return result;
        }

        public uint NextUInt() => (uint)(NextULong() >> 32);

        /// <summary>Uniform in [0,1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        public float NextFloat() => (float)NextDouble();

        /// <summary>Uniform int in [min,max).</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min));
        }

        public int Dice(int sides)
        {
            return sides <= 0 ? 0 : Range(0, sides);
        }

        /// <summary>Rolls "count dice of n sides + bonus", NetHack style.</summary>
        public int Roll(int count, int sides, int bonus)
        {
            if (count <= 0) return bonus;
            int total = bonus;
            for (int i = 0; i < count; i++) total += Dice(sides);
            return total;
        }

        public bool Chance(int percent) => Range(0, 100) < percent;

        public bool OneIn(int n) => n <= 1 || Range(0, n) == 0;

        public double Gaussian(double mean = 0, double sigma = 1)
        {
            // Irwin-Hall approximation: bounded, cheap, and plenty good for damage rolls.
            double sum = 0;
            for (int i = 0; i < 6; i++) sum += NextDouble();
            return mean + sigma * ((sum - 3.0) / Math.Sqrt(0.5));
        }

        public T Pick<T>(T[] items)
        {
            if (items == null || items.Length == 0) return default(T);
            return items[Range(0, items.Length)];
        }

        public T Pick<T>(System.Collections.Generic.IList<T> items)
        {
            if (items == null || items.Count == 0) return default(T);
            return items[Range(0, items.Count)];
        }

        public void Shuffle<T>(System.Collections.Generic.IList<T> list)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>Weighted pick. Weights must be non-negative; zero-weight entries are skipped.</summary>
        public int WeightedIndex(int[] weights)
        {
            if (weights == null || weights.Length == 0) return -1;
            int total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0) total += weights[i];
            }
            if (total <= 0) return -1;
            int roll = Range(0, total);
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0) continue;
                roll -= weights[i];
                if (roll < 0) return i;
            }
            return weights.Length - 1;
        }

        public Rng Fork()
        {
            return new Rng(NextULong());
        }
    }
}
