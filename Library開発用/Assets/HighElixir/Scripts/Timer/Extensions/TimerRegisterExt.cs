using HighElixir.Implements.Observables;
using HighElixir.Timers.Internal;
using System;

namespace HighElixir.Timers
{
    public static class TimerRegisterExt
    {
        /// <summary>
        /// カウントダウンタイマーの登録
        /// </summary>
        public static IObservable<int> CountDownRegister(this Timer t, float duration, out TimerTicket ticket, string name = "", bool isTick = false, bool initZero = false, bool andStart = false)
        {
            lock (t._lock)
            {
                if (duration < 0f)
                {
                    t.SendError(new ArgumentException("CountDownRegister: duration は 0 以上である必要があります。"));
                    ticket = default;
                    return Empty<int>.Instance;
                }
                IObservable<int> res;
                if (isTick)
                    res = t.Register<TickCountDownTimer>(name, duration, out ticket, 1, andStart);
                else
                    res = t.Register<CountDownTimer>(name, duration, out ticket, 1, andStart);

                t.TryGetTimer(ticket, out var timer);
                if (initZero)
                    timer.Current = 0f;

                return res;
            }
        }

        /// <summary>
        /// カウントアップタイマーの登録
        /// </summary>
        public static IObservable<int> CountUpRegister(this Timer t, float initializeTime, out TimerTicket ticket, string name = "", bool isTick = false, bool andStart = false)
        {
            lock (t._lock)
            {
                IObservable<int> res;
                if (isTick)
                    res = t.Register<TickCountUpTimer>(name, initializeTime, out ticket, 1, andStart);
                else
                    res = t.Register<CountUpTimer>(name, initializeTime, out ticket, 1, andStart);

                return res;
            }
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        public static IObservable<int> PulseRegister(this Timer t, float initializeTime, float pulseInterval, out TimerTicket ticket, string name = "", bool isTick = false, bool andStart = false)
        {
            lock (t._lock)
            {
                ticket = default;
                if (pulseInterval < 0f)
                {
                    t.SendError(new ArgumentException("PulseRegister: pulseInterval は 0 以上である必要があります。"));
                    return Empty<int>.Instance;
                }

                IObservable<int> res;
                if (isTick)
                    res = t.Register<TickPulseTimer>(name, initializeTime, out ticket, pulseInterval, andStart);
                else
                    res = t.Register<PulseTimer>(name, initializeTime, out ticket, pulseInterval, andStart);

                return res;
            }
        }

        /// <summary>
        /// アップダウンタイマーの登録
        /// </summary>
        public static IObservable<int> UpDownRegister(this Timer t, float duration, out TimerTicket ticket, string name = "", bool reversing = false, bool isTick = false, bool initZero = false, bool andStart = false)
        {
            lock (t._lock)
            {
                ticket = default;
                if (duration < 0f)
                {
                    t.SendError(new ArgumentException("UpDownRegister: duration は 0 以上である必要があります。"));
                    return default;
                }

                IObservable<int> res;
                if (isTick)
                    res = t.Register<TickUpAndDownTimer>(name, duration, out ticket, 0, andStart);
                else
                    res = t.Register<UpAndDownTimer>(name, duration, out ticket, 0, andStart);

                if (!t.TryGetTimer(ticket, out var timer)) return res;

                if (initZero)
                    timer.Current = 0f;

                if (reversing && timer is IUpAndDown up)
                    up.SetDirection(reversing);

                return res;
            }
        }

        public static TimerTicket Restore(this Timer t, TimerSnapshot snapshot, bool andStart = false)
        {
            lock (t._lock)
            {
                var timer = t.Create(snapshot.CountType, snapshot.Initialize, snapshot.Optional);
                timer.Current = snapshot.Current;
                var ticket = TimerTicket.Take(snapshot.Name);
                t.Register(ticket, timer);
                return ticket;
            }
        }
    }
}
