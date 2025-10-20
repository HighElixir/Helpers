using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickPulseTimer : PulseTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;
        public TickPulseTimer(float pulseInterval, Timer parent, Action onPulse = null)
            : base(pulseInterval, parent, onPulse) { }

        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}