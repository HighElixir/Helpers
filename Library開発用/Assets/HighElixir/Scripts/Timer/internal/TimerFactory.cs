using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    public sealed class TimerFactory
    {
        private readonly Timer _timer;
        private static readonly Dictionary<CountType, Func<TimerConfig, ITimer>> _factory = new()
        {
            // カウントダウン
            { CountType.CountDown | CountType.Tick, (cfg) =>
                new TickCountDownTimer(cfg)
            },
            { CountType.CountDown, (cfg) =>
                new CountDownTimer(cfg)
            },
            // カウントアップ
            { CountType.CountUp | CountType.Tick, (cfg) =>
                new TickCountUpTimer(cfg)
            },
            { CountType.CountUp, (cfg) =>
                new CountUpTimer(cfg)
            },
            // パルスタイマー
            { CountType.Pulse | CountType.Tick, (cfg) =>
                new TickPulseTimer(cfg)
            },
            { CountType.Pulse, (cfg) =>
                 new PulseTimer(cfg)
            },
            // アップアンドダウンタイマー
            { CountType.UpAndDown | CountType.Tick, (cfg) =>
                new TickUpAndDownTimer(cfg)
            },
            { CountType.UpAndDown, (cfg) =>
                new UpAndDownTimer(cfg)
            },
        };

        internal TimerFactory(Timer timer)
        {
            _timer = timer;
        }

        /// <summary>
        /// 内部タイマー生成
        /// </summary>
        internal ITimer Create(CountType type, float initTime, Action action = null)
        {
            if (_factory.TryGetValue(type, out var func))
            {
                var timer = func.Invoke(new TimerConfig(_timer, initTime, action));
                timer.Initialize();
                return timer;
            }
            _timer.OnError(new InvalidOperationException($"TimerFactory: CountType '{type}' 未登録"));
            return null;
        }

        public void Register(CountType type, Func<TimerConfig, ITimer> func)
        {
            if (!_factory.ContainsKey(type))
            {
                _factory.Add(type, func);
            }
            else
            {
                _timer.OnError(new InvalidOperationException($"TimerFactory: CountType '{type}' は既に登録されています"));
            }
        }
    }
}