using System;

namespace HighElixir.Timers.Internal
{
    public readonly struct TimerConfig
    {
        public readonly Timer Timer;
        public readonly float Duration;
        public readonly Action OnFinished;

        public TimerConfig(Timer timer, float duration, Action onFinished = null)
        {
            Timer = timer;
            Duration = duration;
            OnFinished = onFinished;
        }
    }
}