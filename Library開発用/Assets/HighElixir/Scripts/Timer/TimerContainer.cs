using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;

namespace HighElixir.Timers
{
    public static class TimerContainer
    {
        private static readonly Dictionary<Type, Func<TimerConfig, ITimer>> _factory = new();
        private static readonly List<ITimerInstaller> _installers = new();

        public static bool IsAwake { get; private set; }
        /// <summary>
        /// 内部タイマー生成
        /// </summary>
        internal static ITimer Create(this Timer timer, Type type, float initTime, float argTime, Action action = null)
        {
            if (!IsAwake) Awake();
            if (_factory.TryGetValue(type, out var func))
            {
                var t = func.Invoke(new TimerConfig(timer, initTime, argTime, action));
                t.Initialize();
                return t;
            }
            timer.SendError(new InvalidOperationException($"TimerFactory: CountType '{type}' 未登録"));
            return null;
        }

        public static void Register(ITimerInstaller timerInstaller)
        {
            _installers.Add(timerInstaller);
        }

        public static void Awake()
        {
            if (IsAwake) return;
            IsAwake = true;
            if (_installers.Count <= 0) _installers.Add(new TimerInstaller());
            var count = _installers.Count;
            for (int i = 0; i < count; i++)
            {
                foreach(var t in _installers[i].Register())
                {
                    _factory[t.Key] = t.Value;
                }
            }
        }
    }

    public interface ITimerInstaller
    {
        Dictionary<Type, Func<TimerConfig, ITimer>> Register();
    }

    public sealed class TimerInstaller : ITimerInstaller
    {
        private static readonly Dictionary<Type, Func<TimerConfig, ITimer>> _factory = new()
        {
            // カウントダウン
            { typeof(TickCountDownTimer), (cfg) =>
                new TickCountDownTimer(cfg)
            },
            { typeof(CountDownTimer), (cfg) =>
                new CountDownTimer(cfg)
            },
            // カウントアップ
            { typeof(TickCountUpTimer), (cfg) =>
                new TickCountUpTimer(cfg)
            },
            { typeof(CountUpTimer), (cfg) =>
                new CountUpTimer(cfg)
            },
            // パルスタイマー
            { typeof(TickPulseTimer), (cfg) =>
                new TickPulseTimer(cfg)
            },
            { typeof(PulseTimer), (cfg) =>
                 new PulseTimer(cfg)
            },
            // アップアンドダウンタイマー
            { typeof(TickUpAndDownTimer), (cfg) =>
                new TickUpAndDownTimer(cfg)
            },
            { typeof(UpAndDownTimer), (cfg) =>
                new UpAndDownTimer(cfg)
            },
        };

        public Dictionary<Type, Func<TimerConfig, ITimer>> Register()
        {
            return _factory;
        }
    }
}