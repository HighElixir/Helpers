namespace HighElixir.Timers.Internal
{
    internal class CountUpTimer : InternalTimerBase
    {
        public override float NormalizedElapsed => 1f;

        public override CountType CountType => CountType.CountUp;

        public override bool IsFinished => false;

        public CountUpTimer(TimerConfig config)
            : base(config)
        {
            InitialTime = 0f;
        }

        public override void Reset()
        {
            NotifyComplete();
            base.Reset();
        }

        public override void Update(float dt)
        {
            if (dt <= 0f) return; // 負やゼロを無視
            Current += dt;
        }
    }
    internal sealed class TickCountUpTimer : CountUpTimer
    {
        public override CountType CountType => base.CountType | CountType.Tick;

        public TickCountUpTimer(TimerConfig config) : base(config) { }
        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}