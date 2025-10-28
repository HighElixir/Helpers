using System;

namespace HighElixir.Timers
{
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

        public TimerSnapshot(string parentName, TimerTicket ticket, ITimer timer, float optional = -1)
        {
            ParentName = parentName;
            Key = ticket.Key;
            Name = ticket.Name;
            Initialize = timer.InitialTime;
            Current = timer.Current;
            NormalizedElapsed = timer.NormalizedElapsed;
            IsRunning = timer.IsRunning;
            IsFinished = timer.IsFinished;
            CountType = timer.CountType;
            Optional = optional;
        }
    }
}