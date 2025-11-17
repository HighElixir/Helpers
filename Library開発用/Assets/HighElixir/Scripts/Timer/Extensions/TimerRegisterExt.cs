using HighElixir.Timers.Internal;
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
                    t.SendError(new ArgumentException("CountDownRegister: duration は 0 以上である必要があります。"));
                    return default;
                }

                TimerTicket res;
                if (isTick)
                    res = t.Register<TickCountDownTimer>(name, duration, 1, onFinished, andStart);
                else
                    res = t.Register<CountDownTimer>(name, duration, 1, onFinished, andStart);

                t.TryGetTimer(res, out var timer);
                if (initZero)
                    timer.Current = 0f;

                return res;
            }
        }

        /// <summary>
        /// カウントアップタイマーの登録
        /// </summary>
        public static TimerTicket CountUpRegister(this Timer t, float initializeTime, string name = "", Action onReseted = null, bool isTick = false, bool andStart = false)
        {
            lock (t._lock)
            {
                TimerTicket res;
                if (isTick)
                    res = t.Register<TickCountUpTimer>(name, initializeTime, 1, onReseted, andStart);
                else
                    res = t.Register<CountUpTimer>(name, initializeTime, 1, onReseted, andStart);

                return res;
            }
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        public static TimerTicket PulseRegister(this Timer t, float initializeTime, float pulseInterval, string name = "", Action onPulse = null, bool isTick = false, bool andStart = false)
        {
            lock (t._lock)
            {
                if (pulseInterval < 0f)
                {
                    t.SendError(new ArgumentException("PulseRegister: pulseInterval は 0 以上である必要があります。"));
                    return default;
                }

                TimerTicket res;
                if (isTick)
                    res = t.Register<TickPulseTimer>(name, initializeTime, pulseInterval, onPulse, andStart);
                else
                    res = t.Register<PulseTimer>(name, initializeTime, pulseInterval, onPulse, andStart);

                return res;
            }
        }

        /// <summary>
        /// アップダウンタイマーの登録
        /// </summary>
        public static TimerTicket UpDownRegister(this Timer t, float duration, string name = "", Action onFinished = null, bool reversing = false, bool isTick = false, bool initZero = false, bool andStart = false)
        {
            lock (t._lock)
            {
                if (duration < 0f)
                {
                    t.SendError(new ArgumentException("UpDownRegister: duration は 0 以上である必要があります。"));
                    return default;
                }

                TimerTicket res;
                if (isTick)
                    res = t.Register<TickUpAndDownTimer>(name, duration, 0, onFinished, andStart);
                else
                    res = t.Register<UpAndDownTimer>(name, duration, 0, onFinished, andStart);

                if (!t.TryGetTimer(res, out var timer)) return res;

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
                TimerTicket ticket;
                var ct = snapshot.CountType;
                bool isTick = ct.Has(CountType.Tick);

                if (ct.Has(CountType.CountDown))
                {
                    if (isTick)
                        ticket = t.Register<TickCountDownTimer>(snapshot.Name, snapshot.Initialize, 1, null, andStart);
                    else
                        ticket = t.Register<CountDownTimer>(snapshot.Name, snapshot.Initialize, 1, null, andStart);
                }
                else if (ct.Has(CountType.CountUp))
                {
                    if (isTick)
                        ticket = t.Register<TickCountUpTimer>(snapshot.Name, snapshot.Initialize, 1, null, andStart);
                    else
                        ticket = t.Register<CountUpTimer>(snapshot.Name, snapshot.Initialize, 1, null, andStart);
                }
                else if (ct.Has(CountType.Pulse))
                {
                    // Optional の意味は既存実装に合わせてそのまま arg に渡してる
                    if (isTick)
                        ticket = t.Register<TickPulseTimer>(snapshot.Name, snapshot.Initialize, snapshot.Optional, null, andStart);
                    else
                        ticket = t.Register<PulseTimer>(snapshot.Name, snapshot.Initialize, snapshot.Optional, null, andStart);
                }
                else if (ct.Has(CountType.UpAndDown))
                {
                    if (isTick)
                        ticket = t.Register<TickUpAndDownTimer>(snapshot.Name, snapshot.Initialize, snapshot.Optional, null, andStart);
                    else
                        ticket = t.Register<UpAndDownTimer>(snapshot.Name, snapshot.Initialize, snapshot.Optional, null, andStart);
                }
                else
                {
                    t.SendError(new ArgumentException($"Restore: unsupported CountType '{ct}'"));
                    return default;
                }

                if (t.TryGetTimer(ticket, out var timer))
                {
                    timer.Current = snapshot.Current;
                }

                return ticket;
            }
        }
    }
}
