using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickPulseTimer : InternalTimerBase
    {
        private int _pulseCount = 1;
        public override float NormalizedElapsed => Current <= 0f ? 1f : 1f - Math.Clamp(Current / (InitialTime * _pulseCount), 0f, 1f);


        public TickPulseTimer(float pulseInterval, Action onPulse = null)
            : base(onPulse)
        {
            InitialTime = pulseInterval >= 1 ? pulseInterval : 1f;
            _pulseCount = 1;
            InitialTime = 0f;
        }

        public override void Reset()
        {
            _pulseCount = 1;
            base.Reset();
        }
        public override void Update(float _)
        {
            var next = Current + 1;
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