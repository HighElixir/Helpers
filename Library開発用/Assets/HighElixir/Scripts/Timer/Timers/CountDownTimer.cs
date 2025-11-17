using System;

namespace HighElixir.Timers.Internal
{
    public class CountDownTimer : TimerBase
    {
        public override float NormalizedElapsed => Current <= 0f ? 1f : 1f - Math.Clamp(Current / InitialTime, 0f, 1f);

        public override CountType CountType => CountType.CountDown;

        public override bool IsFinished => Current <= 0 && !IsRunning;

        public CountDownTimer(TimerConfig config) :
            base(config)
        {
            if (config.InitializeTime <= 0f) OnError(new ArgumentOutOfRangeException(nameof(config.InitializeTime)));
            InitialTime = config.InitializeTime;
        }

        public override void Update(float dt)
        {
            if (InitialTime <= 0f) return;
            if (dt <= 0f) return; // 負やゼロを無視

            var next = Current - dt;
            if (next >= 0f)
            {
                Current = next;
                return;
            }
            // ちょうど/下回った → 0 に丸め、完了を 1 回だけ通知
            Current = 0f;
            Stop();
            NotifyComplete();
        }
    }
    public sealed class TickCountDownTimer : CountDownTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;

        // 小数点以下切り捨てのため int 型で扱う
        public TickCountDownTimer(TimerConfig config) : base(config)
        {
            InitialTime = (int)InitialTime;
        }

        public override void Update(float dt)
        {
            if (dt <= 0) return; // 負やゼロを無視

            // 常に 1 ずつ減らす
            base.Update(1);
        }
    }
}