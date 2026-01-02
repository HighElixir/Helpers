namespace HighElixir.Timers
{
    public readonly struct TimeData
    {
        public readonly float Current;
        public readonly float Delta;
        public TimeData(float current, float delta)
        {
            Current = current; Delta = delta;
        }
    }
}