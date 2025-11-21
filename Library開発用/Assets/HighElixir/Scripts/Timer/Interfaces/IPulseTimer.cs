namespace HighElixir.Timers
{
    public interface IPulseTimer : INormalizeable
    {
        public int PulseCount { get; }

        public float PulseDuration { get; set; }
    }
}