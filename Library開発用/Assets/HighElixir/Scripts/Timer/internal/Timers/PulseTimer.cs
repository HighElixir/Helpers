using System;

namespace HighElixir.Timers.Internal
{
    internal class PulseTimer : InternalTimerBase
    {
        private int _pulseCount = 0;
        public override float InitialTime
        {
            get
            {
                return base.InitialTime;
            }
            set
            {
                base.InitialTime = value;
                CalcPulse();
            }
        }
        public override float NormalizedElapsed
        {
            get
            {
                float ratio = (Current - InitialTime * _pulseCount) / InitialTime;
                ratio = ratio < 0f ? 0f : (ratio > 1f ? 1f : ratio);
                return ratio;
            }
        }

        public override float Current
        {
            get
            {
                return base.Current;
            }
            set
            {
                base.Current = value;
                CalcPulse();
            }
        }
        public override CountType CountType => CountType.Pulse;

        public override bool IsFinished => false;

        public int PulseCount => _pulseCount;
        public PulseTimer(TimerConfig config)
            : base(config)
        {
            InitialTime = config.Duration;
        }

        public override void Reset()
        {
            _pulseCount = 1;
            Current = 0f;
        }
        public override void Initialize()
        {
            Stop();
            Current = 0f;
        }
        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視

            Current += dt;

            // 通常の等間隔パルス動作
            if (Current >= InitialTime * (_pulseCount + 1))
            {
                NotifyComplete();
                _pulseCount++;
            }
        }

        private void CalcPulse()
        {
            _pulseCount = (int)Math.Ceiling(Current / InitialTime);
        }
    }
    internal sealed class TickPulseTimer : PulseTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;
        public TickPulseTimer(TimerConfig config)
            : base(config)
        {
            InitialTime = (int)InitialTime;
        }

        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}