// エディタ監視用のスナップショット。
using System;

namespace HighElixir.Timers.Internal
{
    // TODO : スナップショットから復元する機能の実装
    [Serializable]
    public readonly struct TimerSnapshot
    {
        public readonly string Id;
        public readonly float Initialize;
        public readonly float Current;
        public readonly float NormalizedElapsed;
        public readonly bool IsRunning;
        public readonly string TimerClass;
        public readonly bool IsTick => TimerClass.Contains("Tick", StringComparison.Ordinal);
        public TimerSnapshot(string id, float initialize, float current, float normalized, bool isRunning, string timerClass)
        {
            Id = id; Initialize = initialize; Current = current; NormalizedElapsed = normalized; IsRunning = isRunning; TimerClass = timerClass;
        }
    }
}