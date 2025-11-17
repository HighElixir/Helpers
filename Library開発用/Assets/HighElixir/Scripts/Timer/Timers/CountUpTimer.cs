namespace HighElixir.Timers.Internal
{
    public class CountUpTimer : TimerBase
    {
        public CountUpTimer(TimerConfig config) : base(config)
        {
        }

        public override float NormalizedElapsed => 1f;

        public override CountType CountType => CountType.CountUp;

        public override bool IsFinished => false;

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
    public sealed class TickCountUpTimer : CountUpTimer
    {
        public TickCountUpTimer(TimerConfig config) : base(config)
        {
        }

        public override CountType CountType => base.CountType | CountType.Tick;

        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}