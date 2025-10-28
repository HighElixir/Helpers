using System;

namespace HighElixir.Timers
{
    public static class TimerRegisterExt
    {
        /// <summary>
        /// カウントダウンタイマーの登録
        /// </summary>
        public static TimerTicket CountDownRegister(this Timer t, float duration, string name = "", Action onFinished = null, bool isTick = false, bool initZero = false, bool andStart = false)
        {
            lock (t._lock)
            {
                if (duration < 0f)
                {
                    t.OnError(new ArgumentException("CountDownRegister: duration は 0 以上である必要があります。"));
                    return default;
                }
                var res = t.Register_Internal(CountType.CountDown, name, duration, isTick, onFinished, andStart);
                t.TryGetTimer(res, out var timer);
                if (initZero)
                    timer.Current = 0f;
                return res;
            }
        }
        /// <summary>
        /// カウントアップタイマーの登録
        /// </summary>
        public static TimerTicket CountUpRegister(this Timer t, string name = "", Action onReseted = null, bool isTick = false, bool andStart = false)
        {
            lock (t._lock)
            {
                return t.Register_Internal(CountType.CountUp, name, 1, isTick, onReseted, andStart);
            }
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        public static TimerTicket PulseRegister(this Timer t, float pulseInterval, string name = "", Action onPulse = null, bool isTick = false, bool andStart = false)
        {
            lock (t._lock)
            {
                if (pulseInterval < 0f)
                {
                    t.OnError(new ArgumentException("PulseRegister: pulseInterval は 0 以上である必要があります。"));
                    return default;
                }
                return t.Register_Internal(CountType.Pulse, name, pulseInterval, isTick, onPulse, andStart);
            }
        }
        /// <summary>
        /// カウントダウンタイマーの登録
        /// </summary>
        public static TimerTicket UpDownRegister(this Timer t, float duration, string name = "", Action onFinished = null, bool reversing = false, bool isTick = false, bool initZero = false, bool andStart = false)
        {
            lock (t._lock)
            {
                if (duration < 0f)
                {
                    t.OnError(new ArgumentException("UpDownRegister: duration は 0 以上である必要があります。"));
                    return default;
                }
                var res = t.Register_Internal(CountType.UpAndDown, name, duration, isTick, onFinished, andStart);

                if (!t.TryGetTimer(res, out var timer)) return res;
                if (initZero)
                {
                    timer.Current = 0f;
                }
                if (reversing && timer is IUpAndDown up)
                {
                    up.SetDirection(reversing);
                }
                return res;
            }
        }
        public static TimerTicket Restore(this Timer t, TimerSnapshot snapshot, bool andStart = false)
        {
            lock (t._lock)
            {
                var ticket = t.Register_Internal(snapshot.CountType, snapshot.Name, snapshot.Initialize, snapshot.CountType.Has(CountType.Tick), null, andStart);
                t.TryGetTimer(ticket, out var timer);
                timer.Current = snapshot.Current;
                return ticket;
            }
        }
    }
}