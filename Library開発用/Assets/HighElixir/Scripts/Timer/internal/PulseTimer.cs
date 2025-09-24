using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class PulseTimer : InternalTimerBase
    {
        private int _pulseCount = 1;
        public override float NormalizedElapsed => Current <= 0f ? 1f : 1f - Math.Clamp(Current / (InitialTime * _pulseCount), 0f, 1f);


        public PulseTimer(float pulseInterval, Action onPulse = null)
            : base(onPulse)
        {
            InitialTime = pulseInterval > 0 ? pulseInterval : 1f;
            _pulseCount = 1;
            InitialTime = 0f;
        }

        public override void Reset()
        {
            _pulseCount = 1;
            base.Reset();
        }
        public override void Update(float dt)
        {
            if (InitialTime <= 0f) return;
            if (dt <= 0f) return; // 負やゼロを無視

            var next = Current + dt;
            if (next > 0f)
            {
                Current = next;
                if (Current >= InitialTime / _pulseCount)
                {
                    EventInvokeSafely();
                    _pulseCount++;
                }
                return;
            }
            Current = 0f;
        }
    }
}