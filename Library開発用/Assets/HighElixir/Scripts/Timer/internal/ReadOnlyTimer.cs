using System;
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
        public Type ParentType => _timer.ParentType;
        public IEnumerable<TimerSnapshot> GetSnapshot()
        {
            return _timer.GetSnapshot();
        }
    }
}