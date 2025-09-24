using System;

namespace HighElixir.Timers.Internal
{
    internal sealed class TickCountDownTimer : InternalTimerBase
    {
        public override float NormalizedElapsed => Current <= 0f ? 1f : 1f - Math.Clamp(Current / InitialTime, 0f, 1f);


        public TickCountDownTimer(float duration, Action onFinished)
            : base(onFinished)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            InitialTime = duration;
        }

        public override void Update(float _)
        {
            if (InitialTime <= 0f) return;

            var next = Current - 1f;
            if (next > 0f)
            {
                Current = next;
                return;
            }

            // ちょうど/下回った → 0 に丸め、完了を 1 回だけ通知
            Current = 0f;
            EventInvokeSafely();
            Reset();
        }
    }
}