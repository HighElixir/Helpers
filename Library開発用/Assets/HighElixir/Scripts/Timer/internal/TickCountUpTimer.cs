using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickCountUpTimer : InternalTimerBase
    {
        public override float NormalizedElapsed => 0f;


        public TickCountUpTimer(Action onReset = null)
            : base(onReset)
        {
            InitialTime = 0f;
        }

        public override void Reset()
        {
            EventInvokeSafely();
            base.Reset();
        }

        public override void Update(float _)
        {

            var next = Current + 1;
            if (next > 0f)
            {
                Current = next;
                return;
            }
            Current = 0f;
        }
    }
}