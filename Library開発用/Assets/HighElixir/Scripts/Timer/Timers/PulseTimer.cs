using System;
using UnityEngine;

namespace HighElixir.Timers.Internal
{
    public class PulseTimer : TimerBase, IPulseTimer
    {
        public override float NormalizedElapsed
        {
            get
            {
                if (PulseDuration <= 0f)
                    return 0f;

                // 今のパルス番号（0開始）
                var currentPulseIndex = Mathf.Max(PulseCount - 1, 0);

                // 今のパルスの中でどれだけ進んだか
                var withinPulse = Current - PulseDuration * currentPulseIndex;
                var ratio = withinPulse / PulseDuration;

                return Mathf.Clamp01(ratio);
            }
        }


        public override float Current
        {
            get
            {
                return base.Current;
            }
            set
            {
                base.Current = value;
            }
        }
        public override CountType CountType => CountType.Pulse;

        public override bool IsFinished => false;

        public int PulseCount => (int)Math.Ceiling(Current / PulseDuration);

        public float PulseDuration { get; set; }

        public PulseTimer(TimerConfig config)
            : base(config)
        {
            InitialTime = config.InitializeTime;
            PulseDuration = config.ArgumentTime;
        }

        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視
            var before = PulseCount;
            Current += dt;

            // 通常の等間隔パルス動作
            if (Current >= PulseDuration * before)
            {
                NotifyComplete();
            }
        }
    }
    public sealed class TickPulseTimer : PulseTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;
        public TickPulseTimer(TimerConfig config)
            : base(config)
        {
            InitialTime = (int)InitialTime;
        }

        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}