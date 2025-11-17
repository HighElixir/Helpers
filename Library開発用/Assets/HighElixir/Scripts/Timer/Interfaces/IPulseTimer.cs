namespace HighElixir.Timers.Internal
{
    public interface IPulseTimer
    {
        public int PulseCount { get; }

        public float PulseDuration { get; set; }
    }
}