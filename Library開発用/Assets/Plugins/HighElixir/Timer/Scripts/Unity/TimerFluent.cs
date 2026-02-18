using System;
using HighElixir.Implements.Observables;
namespace HighElixir.Timers.Unity
{
    public static class TimerFluentExt
    {
        public static TimerBuilder NewCountDown(this Timer timer, float duration)
            => TimerBuilder.CountDown(timer, duration);

        public static TimerBuilder NewCountUp(this Timer timer, float initializeTime = 0f)
            => TimerBuilder.CountUp(timer, initializeTime);

        public static TimerBuilder NewPulse(this Timer timer, float pulseInterval, float initializeTime = 0f)
            => TimerBuilder.Pulse(timer, initializeTime, pulseInterval);

        public static TimerBuilder NewUpDown(this Timer timer, float duration)
            => TimerBuilder.UpDown(timer, duration);
    }

    public sealed class TimerBuilder
    {
        private enum Kind
        {
            CountDown,
            CountUp,
            Pulse,
            UpDown
        }

        private readonly Timer _timer;
        private readonly Kind _kind;
        private readonly float _time;
        private readonly float _arg;
        private TimerRegisterOptions _options;
        private Action<TimeEventType> _onEvent;
        private Action _onFinished;
        private Action<TimeData> _onTime;

        private TimerBuilder(Timer timer, Kind kind, float time, float arg)
        {
            _timer = timer;
            _kind = kind;
            _time = time;
            _arg = arg;
            _options = TimerRegisterOptions.Default;
        }

        internal static TimerBuilder CountDown(Timer timer, float duration) => new(timer, Kind.CountDown, duration, 0f);
        internal static TimerBuilder CountUp(Timer timer, float initializeTime) => new(timer, Kind.CountUp, initializeTime, 0f);
        internal static TimerBuilder Pulse(Timer timer, float initializeTime, float pulseInterval) => new(timer, Kind.Pulse, initializeTime, pulseInterval);
        internal static TimerBuilder UpDown(Timer timer, float duration) => new(timer, Kind.UpDown, duration, 0f);

        public TimerBuilder Name(string name)
        {
            _options.Name = name;
            return this;
        }

        public TimerBuilder Group(string group)
        {
            _options.Group = group;
            return this;
        }

        public TimerBuilder Tag(string tag)
        {
            _options.Tag = tag;
            return this;
        }

        public TimerBuilder Tick(bool enabled = true)
        {
            _options.IsTick = enabled;
            return this;
        }

        public TimerBuilder InitZero(bool enabled = true)
        {
            _options.InitZero = enabled;
            return this;
        }

        public TimerBuilder AutoStart(bool enabled = true)
        {
            _options.AndStart = enabled;
            return this;
        }

        public TimerBuilder Reversing(bool enabled = true)
        {
            _options.Reversing = enabled;
            return this;
        }

        public TimerBuilder OnEvent(Action<TimeEventType> onEvent)
        {
            _onEvent += onEvent;
            return this;
        }

        public TimerBuilder OnFinished(Action onFinished)
        {
            _onFinished += onFinished;
            return this;
        }

        public TimerBuilder OnTime(Action<TimeData> onTime)
        {
            _onTime += onTime;
            return this;
        }

        public TimerHandle Build(bool autoUnregister = false)
        {
            var events = Register(out var ticket);
            var handle = new TimerHandle(_timer, ticket, autoUnregister);
            WireCallbacks(handle, ticket, events);
            return handle;
        }

        public IObservable<TimeEventType> Build(out TimerTicket ticket)
        {
            if (_onEvent != null || _onFinished != null || _onTime != null)
                throw new InvalidOperationException("Build(out ticket) does not support callbacks. Use Build() to attach callbacks.");

            var events = Register(out ticket);
            return events;
        }

        private IObservable<TimeEventType> Register(out TimerTicket ticket)
        {
            return _kind switch
            {
                Kind.CountDown => _timer.CountDownRegister(_time, out ticket, _options),
                Kind.CountUp => _timer.CountUpRegister(_time, out ticket, _options),
                Kind.Pulse => _timer.PulseRegister(_time, _arg, out ticket, _options),
                Kind.UpDown => _timer.UpDownRegister(_time, out ticket, _options),
                _ => throw new InvalidOperationException($"Unknown timer kind: {_kind}")
            };
        }

        private void WireCallbacks(TimerHandle handle, TimerTicket ticket, IObservable<TimeEventType> events)
        {
            if (_onEvent != null)
                handle.Track(events.Subscribe(_onEvent));

            if (_onFinished != null)
            {
                handle.Track(events.Subscribe(evt =>
                {
                    if (evt == TimeEventType.Finished)
                        _onFinished();
                }));
            }

            if (_onTime != null)
                handle.Track(_timer.GetReactiveProperty(ticket).Subscribe(_onTime));
        }
    }
}
