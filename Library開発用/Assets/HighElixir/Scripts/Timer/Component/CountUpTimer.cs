namespace HighElixir.Timers
{
    public class CountUpTimer : TimerBase
    {
        public CountUpTimer(TimerConfig config) : base(config)
        {
        }

        public override bool IsFinished => false;

        public override float ArgTime => 1f;

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
    public sealed class TickCountUpTimer : CountUpTimer, ITick
    {
        public TickCountUpTimer(TimerConfig config) : base(config)
        {
        }
        public override void Update(float _)
        {
            base.Update(1);
        }
    }
}