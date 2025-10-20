using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    internal sealed class TimerFactory
    {
        private Timer _timer;
        private static readonly Dictionary<CountType, Func<Timer, float, Action, ITimer>> _factory = new()
        {
            // カウントダウン
            { CountType.CountDown | CountType.Tick, (timer, initTime, action) =>
                new TickCountDownTimer(initTime, timer, action)
            },
            { CountType.CountDown, (timer, initTime, action) =>
                new CountDownTimer(initTime, timer, action)
            },
            // カウントアップ
            { CountType.CountUp | CountType.Tick, (timer, initTime, action) =>
                new TickCountUpTimer(timer, action)
            },
            { CountType.CountUp, (timer, initTime, action) =>
                new CountUpTimer(timer, action)
            },
            // パルスタイマー
            { CountType.Pulse | CountType.Tick, (timer, initTime, action) =>
                new TickPulseTimer(initTime, timer, action)
            },
            { CountType.Pulse, (timer, initTime, action) =>
                 new PulseTimer(initTime, timer, action)
            }
        };

        public TimerFactory(Timer timer)
        {
            _timer = timer;
        }

        /// <summary>
        /// 内部タイマー生成
        /// </summary>
        public ITimer Create(CountType type, float initTime, Action action = null)
        {
            try
            {
                ITimer timer = null;
                timer = _factory[type].Invoke(_timer, initTime, action);
                timer.Initialize();
                return timer;
            }
            catch (KeyNotFoundException)
            {
                _timer.OnError(new InvalidOperationException($"TimerFactory: CountType '{type}' に対応するタイマー生成関数が登録されていません。"));
                return null;
            }
            catch (Exception ex)
            {
                _timer.OnError(ex);
                return null;
            }
        }
    }
}