// エディタ監視用のスナップショット。
using System;

namespace HighElixir.Timers
{
    // TODO : スナップショットから復元する機能の実装
    [Serializable]
    public readonly struct TimerSnapshot
    {
        public readonly string ParentName;
        public readonly string Key;
        public readonly string Name;
        public readonly float Initialize;
        public readonly float Current;
        public readonly float NormalizedElapsed;
        public readonly bool IsRunning;
        public readonly bool IsFinished;
        public readonly CountType CountType;
        public readonly float Optional; // PulseTypeのパルス数などを保存
        public TimerSnapshot(string parentName,TimerTicket ticket, float initialize, float current, float normalized, bool isRunning, bool isFinished, CountType countType, float optional = -1)
        {
            ParentName = parentName;
            Key = ticket.Key;
            Name = ticket.Name;
            Initialize = initialize;
            Current = current;
            NormalizedElapsed = normalized;
            IsRunning = isRunning;
            IsFinished = isFinished;
            CountType = countType;
            Optional = optional;
        }
    }
}