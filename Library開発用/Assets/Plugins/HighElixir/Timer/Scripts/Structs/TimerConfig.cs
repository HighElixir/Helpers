using System;

namespace HighElixir.Timers
{
    public readonly struct TimerConfig
    {
        public readonly Timer Timer;
        public readonly float InitializeTime;
        public readonly float ArgumentTime;

        public TimerConfig(Timer timer, float initializeTime, float argumentTime)
        {
            Timer = timer;
            InitializeTime = initializeTime;
            ArgumentTime = argumentTime;
        }
    }
}