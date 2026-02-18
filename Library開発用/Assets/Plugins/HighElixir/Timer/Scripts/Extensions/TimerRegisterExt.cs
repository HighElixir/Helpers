using HighElixir.Implements.Observables;
using HighElixir.Timers.Internal;
using System;

namespace HighElixir.Timers
{
    public static class TimerRegisterExt
    {
        private static IObservable<TimeEventType> ToTyped(IObservable<int> stream)
            => stream.Convert(TimerEventRegistry.ToType);

        /// <summary>
        /// カウントダウンタイマーの登録。
        /// </summary>
        public static IObservable<TimeEventType> CountDownRegister(this Timer t, float duration, out TimerTicket ticket, TimerRegisterOptions options)
        {
            var events = CountDownRegisterInternal(t, duration, out ticket, options);
            return ToTyped(events);
        }

        /// <summary>
        /// カウントダウンタイマーの登録
        /// </summary>
        /// <returns><see cref="TimerEventRegistry"/>をもとにしたイベントIDを通知するオブザーバブル</returns>
        [Obsolete("Use CountDownRegister(duration, out ticket, TimerRegisterOptions options).")]
        public static IObservable<int> CountDownRegister(this Timer t, float duration, out TimerTicket ticket, string name = "", bool isTick = false, bool initZero = false, bool andStart = false)
        {
            return CountDownRegisterInternal(t, duration, out ticket, new TimerRegisterOptions
            {
                Name = name,
                IsTick = isTick,
                InitZero = initZero,
                AndStart = andStart
            });
        }

        private static IObservable<int> CountDownRegisterInternal(this Timer t, float duration, out TimerTicket ticket, TimerRegisterOptions options)
        {
            lock (t._lock)
            {
                if (duration <= 0f)
                {
                    t.SendError(new ArgumentException("CountDownRegister: duration は 0 より大きい必要があります。"));
                    ticket = default;
                    return Empty<int>.Instance;
                }

                IObservable<int> res = options.IsTick
                    ? t.Register<TickCountDownTimer>(options.Name, duration, out ticket, 1f, options.AndStart)
                    : t.Register<CountDownTimer>(options.Name, duration, out ticket, 1f, options.AndStart);

                if (options.InitZero && t.TryGetTimer(ticket, out var timer))
                    timer.Current = 0f;

                t.SetMetadata(ticket, new TimerMeta(options.Group, options.Tag));
                return res ?? Empty<int>.Instance;
            }
        }

        /// <summary>
        /// カウントアップタイマーの登録。
        /// </summary>
        public static IObservable<TimeEventType> CountUpRegister(this Timer t, float initializeTime, out TimerTicket ticket, TimerRegisterOptions options)
        {
            var events = CountUpRegisterInternal(t, initializeTime, out ticket, options);
            return ToTyped(events);
        }

        /// <summary>
        /// カウントアップタイマーの登録
        /// </summary>
        /// <returns><see cref="TimerEventRegistry"/>をもとにしたイベントIDを通知するオブザーバブル</returns>
        [Obsolete("Use CountUpRegister(initializeTime, out ticket, TimerRegisterOptions options).")]
        public static IObservable<int> CountUpRegister(this Timer t, float initializeTime, out TimerTicket ticket, string name = "", bool isTick = false, bool andStart = false)
        {
            return CountUpRegisterInternal(t, initializeTime, out ticket, new TimerRegisterOptions
            {
                Name = name,
                IsTick = isTick,
                AndStart = andStart
            });
        }

        private static IObservable<int> CountUpRegisterInternal(this Timer t, float initializeTime, out TimerTicket ticket, TimerRegisterOptions options)
        {
            lock (t._lock)
            {
                IObservable<int> res = options.IsTick
                    ? t.Register<TickCountUpTimer>(options.Name, initializeTime, out ticket, 1f, options.AndStart)
                    : t.Register<CountUpTimer>(options.Name, initializeTime, out ticket, 1f, options.AndStart);

                t.SetMetadata(ticket, new TimerMeta(options.Group, options.Tag));
                return res ?? Empty<int>.Instance;
            }
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        public static IObservable<TimeEventType> PulseRegister(this Timer t, float initializeTime, float pulseInterval, out TimerTicket ticket, TimerRegisterOptions options)
        {
            var events = PulseRegisterInternal(t, initializeTime, pulseInterval, out ticket, options);
            return ToTyped(events);
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        /// <returns><see cref="TimerEventRegistry"/>をもとにしたイベントIDを通知するオブザーバブル</returns>
        [Obsolete("Use PulseRegister(initializeTime, pulseInterval, out ticket, TimerRegisterOptions options).")]
        public static IObservable<int> PulseRegister(this Timer t, float initializeTime, float pulseInterval, out TimerTicket ticket, string name = "", bool isTick = false, bool andStart = false)
        {
            return PulseRegisterInternal(t, initializeTime, pulseInterval, out ticket, new TimerRegisterOptions
            {
                Name = name,
                IsTick = isTick,
                AndStart = andStart
            });
        }

        private static IObservable<int> PulseRegisterInternal(this Timer t, float initializeTime, float pulseInterval, out TimerTicket ticket, TimerRegisterOptions options)
        {
            lock (t._lock)
            {
                ticket = default;
                if (pulseInterval <= 0f)
                {
                    t.SendError(new ArgumentException("PulseRegister: pulseInterval は 0 より大きい必要があります。"));
                    return Empty<int>.Instance;
                }

                IObservable<int> res = options.IsTick
                    ? t.Register<TickPulseTimer>(options.Name, initializeTime, out ticket, pulseInterval, options.AndStart)
                    : t.Register<PulseTimer>(options.Name, initializeTime, out ticket, pulseInterval, options.AndStart);

                t.SetMetadata(ticket, new TimerMeta(options.Group, options.Tag));
                return res ?? Empty<int>.Instance;
            }
        }

        /// <summary>
        /// アップダウンタイマーの登録。
        /// </summary>
        public static IObservable<TimeEventType> UpDownRegister(this Timer t, float duration, out TimerTicket ticket, TimerRegisterOptions options)
        {
            var events = UpDownRegisterInternal(t, duration, out ticket, options);
            return ToTyped(events);
        }

        /// <summary>
        /// アップダウンタイマーの登録
        /// </summary>
        /// <returns><see cref="TimerEventRegistry"/>をもとにしたイベントIDを通知するオブザーバブル</returns>
        [Obsolete("Use UpDownRegister(duration, out ticket, TimerRegisterOptions options).")]
        public static IObservable<int> UpDownRegister(this Timer t, float duration, out TimerTicket ticket, string name = "", bool reversing = false, bool isTick = false, bool initZero = false, bool andStart = false)
        {
            return UpDownRegisterInternal(t, duration, out ticket, new TimerRegisterOptions
            {
                Name = name,
                Reversing = reversing,
                IsTick = isTick,
                InitZero = initZero,
                AndStart = andStart
            });
        }

        private static IObservable<int> UpDownRegisterInternal(this Timer t, float duration, out TimerTicket ticket, TimerRegisterOptions options)
        {
            lock (t._lock)
            {
                ticket = default;
                if (duration <= 0f)
                {
                    t.SendError(new ArgumentException("UpDownRegister: duration は 0 より大きい必要があります。"));
                    return Empty<int>.Instance;
                }

                var directionArg = options.Reversing ? 1f : 0f;
                IObservable<int> res = options.IsTick
                    ? t.Register<TickUpAndDownTimer>(options.Name, duration, out ticket, directionArg, options.AndStart)
                    : t.Register<UpAndDownTimer>(options.Name, duration, out ticket, directionArg, options.AndStart);

                if (!t.TryGetTimer(ticket, out var timer)) return res;

                if (options.InitZero)
                    timer.Current = 0f;

                if (timer is IUpAndDown up)
                    up.SetDirection(options.Reversing);

                t.SetMetadata(ticket, new TimerMeta(options.Group, options.Tag));
                return res ?? Empty<int>.Instance;
            }
        }

        public static TimerTicket Restore(this Timer t, TimerSnapshot snapshot, bool andStart = false)
        {
            lock (t._lock)
            {
                var timer = t.Create(snapshot.CountType, snapshot.Initialize, snapshot.Optional);
                timer.Current = snapshot.Current;
                var ticket = TimerTicket.Take(snapshot.Name, t);
                t.Register(ticket, timer);
                t.SetMetadata(ticket, default);
                if (andStart)
                    timer.Start();
                return ticket;
            }
        }
    }
}
