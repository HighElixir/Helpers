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
        public readonly Type CountType;
        public readonly float Optional; // PulseTypeのパルス数などを保存

        public TimerSnapshot(string parentName, TimerTicket ticket, ITimer timer)
        {
            ParentName = parentName;
            Key = ticket.Key;
            Name = ticket.Name;
            Initialize = timer.InitialTime;
            Current = timer.Current;
            NormalizedElapsed = timer is INormalizeable normalizeable ? normalizeable.NormalizedElapsed : 1f;
            IsRunning = timer.IsRunning;
            IsFinished = timer.IsFinished;
            CountType = timer.GetType();
            Optional = timer.ArgTime;
        }
    }
}