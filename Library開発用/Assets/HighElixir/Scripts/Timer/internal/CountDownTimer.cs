using System;

namespace HighElixir.Timers.Internal
{
    internal class CountDownTimer : InternalTimerBase
    {
        public override float NormalizedElapsed => Current <= 0f ? 1f : 1f - Math.Clamp(Current / InitialTime, 0f, 1f);

        public override CountType CountType => CountType.CountDown;

        public override bool IsFinished => Current <= 0 && !IsRunning;

        public CountDownTimer(float duration, Action onFinished = null) :
            base(onFinished)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            InitialTime = duration;
        }

        public override void Update(float dt)
        {
            if (InitialTime <= 0f) return;
            if (dt <= 0f) return; // 負やゼロを無視

            var next = Current - dt;
            Current = next;
            if (Current >= 0f) return;
            // ちょうど/下回った → 0 に丸め、完了を 1 回だけ通知
            Current = 0f;
            Stop();
            EventInvokeSafely();
        }
    }
}