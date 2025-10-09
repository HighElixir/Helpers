using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickCountUpTimer : CountUpTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;

        public TickCountUpTimer(Action onReset) : base(onReset) { }
        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}