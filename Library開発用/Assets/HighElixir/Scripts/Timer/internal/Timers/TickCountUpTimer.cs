using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickCountUpTimer : CountUpTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;

        public TickCountUpTimer(Timer parent, Action onReset = null) : base(parent, onReset) { }
        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}