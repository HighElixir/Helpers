using System;

namespace HighElixir.Timers.Internal
{
    internal class UpAndDownTimer : InternalTimerBase, IUpAndDown
    {
        public override float NormalizedElapsed => Math.Clamp(Current / InitialTime, 0, 1);
        public override bool IsFinished => !IsRunning && (Current <= 0 || Current >= InitialTime);
        public override CountType CountType => CountType.UpAndDown;

        // true: 上昇中, false: 下降中
        public bool IsReversing { get; private set; } = false;

        public event Action<bool> OnReversed;
        public UpAndDownTimer(TimerConfig config) : base(config)
        {
            if (config.Duration <= 0f) OnError(new ArgumentOutOfRangeException(nameof(config.Duration)));
            InitialTime = config.Duration;
        }

        public override void Reset()
        {
            Stop(false);
            if (IsReversing)
                Current = 0f;
            else
                Current = InitialTime;
            NotifyReset();
        }
        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視
            if (IsReversing)
            {
                Current += dt;
                if (Current >= InitialTime)
                {
                    Current = InitialTime;
                    IsRunning = false;
                    NotifyComplete();
                }
            }
            else
            {
                Current -= dt;
                if (Current <= 0f)
                {
                    Current = 0f;
                    IsRunning = false;
                    NotifyComplete();
                }
            }
        }

        public void ReverseDirection()
        {
            SetDirection(!IsReversing);
        }

        public void SetDirection(bool isUp)
        {
            IsReversing = isUp;
            OnReversed?.Invoke(isUp);
        }
    }

    internal sealed class TickUpAndDownTimer : UpAndDownTimer
    {
        public override float NormalizedElapsed => Current / InitialTime;
        public override bool IsFinished => !IsRunning && (Current <= 0 || Current >= InitialTime);
        public override CountType CountType => base.CountType | CountType.Tick;
        public TickUpAndDownTimer(TimerConfig config) : base(config)
        {
            InitialTime = (int)InitialTime;
        }

        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視

            // 常に 1 ずつ増減させる
            base.Update(1);
        }
    }
}