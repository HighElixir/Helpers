namespace HighElixir.Timers
{
    public enum StreamCatchUpMode
    {
        // フレーム落ちしても 1 回だけ発火。
        Skip = 0,

        // 抜けた分をすべて発火。
        CatchUpAll = 1,

        // 指定上限までキャッチアップし、残りは破棄。
        CatchUpMax = 2,
    }

    /// <summary>
    /// TimeStream の周期スケジュール挙動。
    /// </summary>
    public struct StreamScheduleOptions
    {
        public bool RunImmediately;
        public StreamCatchUpMode CatchUpMode;
        public int MaxCatchUpPerUpdate;

        public static StreamScheduleOptions Default => new()
        {
            RunImmediately = false,
            CatchUpMode = StreamCatchUpMode.Skip,
            MaxCatchUpPerUpdate = 1
        };

        public static StreamScheduleOptions Skip(bool runImmediately = false)
            => new()
            {
                RunImmediately = runImmediately,
                CatchUpMode = StreamCatchUpMode.Skip,
                MaxCatchUpPerUpdate = 1
            };

        public static StreamScheduleOptions CatchUpAll(bool runImmediately = false)
            => new()
            {
                RunImmediately = runImmediately,
                CatchUpMode = StreamCatchUpMode.CatchUpAll,
                MaxCatchUpPerUpdate = int.MaxValue
            };

        public static StreamScheduleOptions CatchUpMax(int maxCatchUpPerUpdate, bool runImmediately = false)
            => new()
            {
                RunImmediately = runImmediately,
                CatchUpMode = StreamCatchUpMode.CatchUpMax,
                MaxCatchUpPerUpdate = maxCatchUpPerUpdate <= 0 ? 1 : maxCatchUpPerUpdate
            };

        internal int ResolveMaxCatchUp()
            => MaxCatchUpPerUpdate <= 0 ? 1 : MaxCatchUpPerUpdate;
    }
}
