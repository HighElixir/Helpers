using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class CountUpTimer : InternalTimerBase
    {
        public override float NormalizedElapsed => 0f;


        public CountUpTimer(Action onReset = null)
            : base(onReset)
        {
            InitialTime = 0f;
        }

        public override void Reset()
        {
            EventInvokeSafely();
            base.Reset();
        }

        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視

            var next = Current + dt;
            if (next > 0f)
            {
                Current = next;
                return;
            }
            Current = 0f;
        }
    }
}