using HighElixir.Implements.Observables;
using System;

namespace HighElixir.Timers.Extensions
{
    public static class TimerExtentions
    {
        public static IObservable<float> GetCurrentReactive(this Timer timer, TimerTicket ticket)
        {
            if (timer.TryGetTimer(ticket, out var t))
            {
                return t.TimeReactive.Convert(x => x.Current);
            }
            return new Empty<float>();
        }
    }
}