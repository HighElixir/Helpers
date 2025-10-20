using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    internal sealed class ReadOnlyTimer : IReadOnlyTimer
    {
        private IReadOnlyTimer _timer;
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

        public void Dispose()
        {
            _timer = null;
        }
    }
}