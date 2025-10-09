using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    internal sealed class ReadOnlyTimer : IReadOnlyTimer
    {
        private readonly IReadOnlyTimer _timer;
        internal ReadOnlyTimer(IReadOnlyTimer timer)
        {
            _timer = timer;
        }
        public int CommandCount => _timer.CommandCount;
        public string ParentName => _timer.ParentName;
        public IEnumerable<TimerSnapshot> GetSnapshot()
        {
            return _timer.GetSnapshot();
        }
    }
}