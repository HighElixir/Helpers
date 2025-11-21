using HighElixir.Implements.Observables;
using System;

namespace HighElixir.Timers.Extensions
{
    public static class TimerExtensions
    {
        public static IObservable<float> GetCurrentReactive(this Timer timer, TimerTicket ticket)
        {
            if (timer.TryGetTimer(ticket, out var t))
            {
                return t.TimeReactive.Convert(x => x.Current);
            }
            return new Empty<float>();
        }

        /// <summary>
        /// 経過正規化 [0..1] を取得（未登録及びカウントアップなど正規化不可能なタイマーは 1 として返す）。
        /// </summary>
        public static bool TryGetNormalizedElapsed(this Timer timer, TimerTicket ticket, out float elapsed)
        {
            elapsed = 1f;
            if (timer.TryGetTimer(ticket, out var t) && t is INormalizeable normalizeable)
            {
                elapsed = normalizeable.NormalizedElapsed;
                return true;
            }
            return false;
        }
    }
}