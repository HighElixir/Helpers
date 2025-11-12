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
        private static ThreadLocal<Random> _local =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        public static Random Instance => _local.Value;

        internal static void ResetSeed(int newSeed)
        {
            _seed = newSeed;
            _local.Dispose();
            _local = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        }
    }

    public static class RandomExtensions
    {
        #region 確率判定
        /// <summary>
        /// 確率[0,1]で true。
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
        /// パーセント指定（0〜100）
        /// </summary>
        public static bool Percent(this Random random, double percent)
            => random.Chance(percent / 100.0);
        #endregion

        public static int GetDir(this Random random, bool ignoreZero = true)
        {
            if (random == null) return 0;
            if (!ignoreZero)
            {
                var value = random.Next(0, 3);
                return value - 1;
            }
            else
            {
                return random.Chance(0.5) ? 1 : -1;
            }
        }

        #region 共有RNG代替（スレッドローカル）
        /// <summary>
        /// 確率[0,1]で true。（共有RNG代替）
        /// </summary>
        public static bool Chance(double probability)
            => RandomProvider.Instance.Chance(probability);
        /// <summary>
        /// パーセント指定（共有RNG代替）
        /// </summary>
        public static bool Percent(double percent)
            => RandomProvider.Instance.Chance(percent / 100.0);

        /// <summary>
        /// Min 〜 Max の範囲でランダムな整数を返す。(Maxを含む)
        /// </summary>
        public static int Rand(int min, int max)
            => RandomProvider.Instance.Next(min, max + 1);
        public static double Rand(double min, double max)
        {
            if (double.IsNaN(min) || double.IsNaN(max)) return double.NaN;
            if (min == max) return min;
            if (min > max) { var t = min; min = max; max = t; }

            var u = RandomProvider.Instance.NextDouble();
            return min + (max - min) * u; // [min, max)
        }

        public static int Next() => RandomProvider.Instance.Next();

        public static int GetDir(bool ignoreZero = true)
            => RandomProvider.Instance.GetDir(ignoreZero);
        #endregion

        #region シードリセット
        public static void ResetRandomSeed(int newSeed)
            => RandomProvider.ResetSeed(newSeed);
        #endregion
    }
}
