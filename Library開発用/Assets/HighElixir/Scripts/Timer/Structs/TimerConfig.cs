using System;

namespace HighElixir.Timers.Internal
{
    public readonly struct TimerConfig
    {
        public readonly Timer Timer;
        public readonly float InitializeTime;
        public readonly float ArgumentTime;
        public readonly Action OnFinished;

        public TimerConfig(Timer timer, float initializeTime, float argumentTime, Action onFinished = null)
        {
            Timer = timer;
            InitializeTime = initializeTime;
            ArgumentTime = argumentTime;
            OnFinished = onFinished;
        }
    }
}