using HighElixir.Timers.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

namespace HighElixir.Timers
{
    // スナップショット関連
    public sealed partial class Timer 
    {
        public IEnumerable<TimerSnapshot> GetSnapshot()
        {
            // キーのスナップショットで安全に列挙
            var keys = _timers.Keys.ToList();
            foreach (var key in keys)
            {
                if (_timers.TryGetValue(key, out var t))
                {
                    var current = t.Current;
                    var init = t.InitialTime;
                    var normalized = t.NormalizedElapsed;
                    var running = t.IsRunning;
                    var finished = t.IsFinished;
                    var countType = t.CountType;
                    float op = -1;
                    if (countType.Has(CountType.Pulse))
                        op = ((PulseTimer)t).PulseCount;

                    yield return new TimerSnapshot(
                        ParentName,
                        key,
                        init,
                        current,
                        normalized,
                        running,
                        finished,
                        countType,
                        op
                    );

                }
            }
        }
    }
}
