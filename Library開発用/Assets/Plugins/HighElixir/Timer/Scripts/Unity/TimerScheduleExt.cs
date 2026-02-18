using System;
using HighElixir.Implements.Observables;
namespace HighElixir.Timers.Unity
{
    public static class TimerScheduleExt
    {
        public static TimerHandle Delay(this Timer timer, float seconds, Action onCompleted = null, TimerRegisterOptions options = default, bool autoUnregister = true)
        {
            options.AndStart = true;
            if (string.IsNullOrWhiteSpace(options.Name))
                options.Name = $"Delay({seconds:0.###})";

            var events = timer.CountDownRegister(seconds, out var ticket, options);
            var handle = new TimerHandle(timer, ticket, autoUnregister);
            if (onCompleted != null)
            {
                handle.Track(events.Subscribe(evt =>
                {
                    if (evt == TimeEventType.Finished)
                        onCompleted();
                }));
            }
            return handle;
        }

        public static TimerHandle Timeout(this Timer timer, float seconds, Action onTimeout = null, TimerRegisterOptions options = default, bool autoUnregister = true)
        {
            if (string.IsNullOrWhiteSpace(options.Name))
                options.Name = $"Timeout({seconds:0.###})";
            return timer.Delay(seconds, onTimeout, options, autoUnregister);
        }

        public static TimerHandle Every(this Timer timer, float interval, Action onTick, TimerRegisterOptions options = default, bool autoUnregister = false)
        {
            options.AndStart = true;
            if (string.IsNullOrWhiteSpace(options.Name))
                options.Name = $"Every({interval:0.###})";

            var events = timer.PulseRegister(0f, interval, out var ticket, options);
            var handle = new TimerHandle(timer, ticket, autoUnregister);
            if (onTick != null)
            {
                handle.Track(events.Subscribe(evt =>
                {
                    if (evt == TimeEventType.Finished)
                        onTick();
                }));
            }
            return handle;
        }

        public static TimerHandle Countdown(this Timer timer, float seconds, Action<float> onTick = null, Action onCompleted = null, TimerRegisterOptions options = default, bool autoUnregister = true)
        {
            options.AndStart = true;
            if (string.IsNullOrWhiteSpace(options.Name))
                options.Name = $"Countdown({seconds:0.###})";

            var events = timer.CountDownRegister(seconds, out var ticket, options);
            var handle = new TimerHandle(timer, ticket, autoUnregister);

            if (onTick != null)
            {
                handle.Track(timer.GetReactiveProperty(ticket).Subscribe(data => onTick(data.Current)));
            }

            if (onCompleted != null)
            {
                handle.Track(events.Subscribe(evt =>
                {
                    if (evt == TimeEventType.Finished)
                        onCompleted();
                }));
            }

            return handle;
        }
    }
}
