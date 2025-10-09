using System;
using UnityEngine;

namespace HighElixir.Timers.Internal
{
    internal class CountUpTimer : InternalTimerBase
    {
        public override float NormalizedElapsed => 1f;

        public override CountType CountType => CountType.CountUp;

        public override bool IsFinished => false;

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
            Current += dt;
        }
    }
}