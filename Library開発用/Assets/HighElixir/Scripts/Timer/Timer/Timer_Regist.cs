using HighElixir.Timers.Internal;
using System;
using UnityEngine;

namespace HighElixir.Timers
{
    // 登録用partial
    public sealed partial class Timer
    {
        /// <summary>
        /// カウントダウンタイマーの登録
        /// </summary>
        public TimerTicket CountDownRegister(float duration, string name = "", Action onFinished = null, bool isTick = false)
        {
            var t = TimerTicket.Take(name);
            if (duration < 0f) duration = 1f;
            var type = CountType.CountDown | (isTick ? CountType.Tick : CountType.Invalid);
            _timers[t] = GetTimer(type, duration, onFinished);
            return t;
        }
        /// <summary>
        /// カウントアップタイマーの登録
        /// </summary>
        public TimerTicket CountUpRegister(string name = "", Action onReseted = null, bool isTick = false)
        {
            var t = TimerTicket.Take(name);
            var type = CountType.CountUp | (isTick ? CountType.Tick : CountType.Invalid);
            _timers[t] = GetTimer(type, 1, onReseted);
            return t;
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        public TimerTicket PulseRegister(float pulseInterval, string name = "", Action onPulse = null, bool isTick = false)
        {
            var t = TimerTicket.Take(name);
            if (pulseInterval < 0f) pulseInterval = 1f;
            var type = CountType.Pulse | (isTick ? CountType.Tick : CountType.Invalid);
            _timers[t] = GetTimer(type, pulseInterval, onPulse);
            return t;
        }

        /// <summary>
        /// 登録解除。存在しなければ false。
        /// </summary>
        public bool Unregister(TimerTicket ticket) => _timers.Remove(ticket);

        private ITimer GetTimer(CountType type, float arg, Action action = null)
        {
            ITimer timer = null;
            if (!type.Has(CountType.Tick))
            {
                if (type.Has(CountType.CountDown))
                    timer = new CountDownTimer(arg, action);
                else if (type.Has(CountType.CountUp))
                    timer = new CountUpTimer(action);
                else
                    timer = new PulseTimer(arg, action);
            }
            else
            {
                if (type.Has(CountType.CountDown))
                    timer = new TickCountDownTimer(arg, action);
                else if (type.Has(CountType.CountUp))
                    timer = new TickCountUpTimer(action);
                else
                    timer = new TickPulseTimer(arg, action);
            }
            timer.Initialize();
            return timer;
        }
    }
}
