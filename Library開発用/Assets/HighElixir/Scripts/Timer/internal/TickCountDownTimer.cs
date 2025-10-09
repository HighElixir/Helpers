using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickCountDownTimer : CountDownTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;
        public TickCountDownTimer(float duration, Action onFinished) : base(duration, onFinished) { }

        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}