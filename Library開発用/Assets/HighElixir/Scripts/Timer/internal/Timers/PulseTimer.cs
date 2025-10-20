using System;

namespace HighElixir.Timers.Internal
{
    internal class PulseTimer : InternalTimerBase, IPulse
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
        public PulseTimer(float pulseInterval, Timer parent, Action onPulse = null)
            : base(parent, onPulse)
        {
            InitialTime = pulseInterval;
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
                InvokeEventSafely();
                _pulseCount++;
            }
        }

        private void CalcPulse()
        {
            _pulseCount = (int)Math.Ceiling(Current / InitialTime);
        }
    }
}