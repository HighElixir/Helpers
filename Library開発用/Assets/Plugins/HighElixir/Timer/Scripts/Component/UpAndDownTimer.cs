using System;

namespace HighElixir.Timers
{
    public class UpAndDownTimer : TimerBase, IUpAndDown
    {
        public virtual float NormalizedElapsed => Math.Clamp(Current / InitialTime, 0, 1);
        public override bool IsFinished => !IsRunning && (Current <= 0 || Current >= InitialTime);

        // true: 上昇中, false: 下降中
        public bool IsReversing { get; private set; } = false;

        public override float ArgTime => IsReversing ? 1 : 0;

        public event Action<bool> OnReversed;
        public UpAndDownTimer(TimerConfig config) : base(config)
        {
            if (config.InitializeTime <= 0f) SendError(new ArgumentOutOfRangeException(nameof(config.InitializeTime)));
            IsReversing = config.ArgumentTime == 0;
            InitialTime = config.InitializeTime;
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

    public sealed class TickUpAndDownTimer : UpAndDownTimer, ITick
    {
        public override bool IsFinished => !IsRunning && (Current <= 0 || Current >= InitialTime);
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