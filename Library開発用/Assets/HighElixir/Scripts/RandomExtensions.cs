using System;
using System.Threading;

namespace HighElixir
{
    /// <summary>
    /// マルチスレッド対応のRandom。
    /// </summary>
    internal static class RandomProvider
    {
        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> _local =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        public static Random Instance => _local.Value;
    }

    public static class RandomExtensions
    {
        /// <summary>
        /// 確率[0,1]で true。RNG を明示注入する版。
        /// </summary>
        public static bool Chance(this Random random, double probability)
        {
            if (random == null) return false;
            if (double.IsNaN(probability) || double.IsInfinity(probability)) probability = 0.0;
            if (probability <= 0.0) return false;
            if (probability >= 1.0) return true;
            return random.NextDouble() < probability;
        }

        /// <summary>
        /// float 版（内部は double に委譲）
        /// </summary>
        public static bool Chance(this Random random, float probability)
            => random.Chance((double)probability);

        /// <summary>
        /// 共有RNG代替（スレッドローカル）を使う簡易版。
        /// </summary>
        public static bool Chance(double probability)
            => RandomProvider.Instance.Chance(probability);

        /// <summary>
        /// パーセント指定（0〜100）
        /// </summary>
        public static bool Percent(this Random random, double percent)
            => random.Chance(percent / 100.0);

        /// <summary>
        /// パーセント指定（共有RNG代替）
        /// </summary>
        public static bool Percent(double percent)
            => RandomProvider.Instance.Chance(percent / 100.0);

        public static int Rand(int min, int max)
            => RandomProvider.Instance.Next(min, max);
        public static double Rand(double min, double max)
        {
            if (double.IsNaN(min) || double.IsNaN(max)) return double.NaN;
            if (min == max) return min;
            if (min > max) { var t = min; min = max; max = t; }

            var u = RandomProvider.Instance.NextDouble();  // [0,1)
            return u * (max - min) + min;                  // [min, max)
        }
        public static float Rand(float min, float max) => (float)Rand((double)min, (double)max);
    }
}
