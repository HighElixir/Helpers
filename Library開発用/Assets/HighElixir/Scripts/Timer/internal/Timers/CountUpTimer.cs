using System;

namespace HighElixir.Timers.Internal
{
    internal class CountUpTimer : InternalTimerBase, ICountUp
    {
        public override float NormalizedElapsed => 1f;

        public override CountType CountType => CountType.CountUp;

        public override bool IsFinished => false;

        public CountUpTimer(Timer parent, Action onReset = null)
            : base(parent, onReset)
        {
            InitialTime = 0f;
        }

        public override void Reset()
        {
            InvokeEventSafely();
            base.Reset();
        }

        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視
            Current += dt;
        }
    }
}