namespace HighElixir.Timers.Unity
{
    public static class TimerCreateHandleExt
    {
        public static TimerHandle CreateCountDown(this Timer timer, float duration, TimerRegisterOptions options = default, bool autoUnregister = false)
        {
            timer.CountDownRegister(duration, out var ticket, options);
            return new TimerHandle(timer, ticket, autoUnregister);
        }

        public static TimerHandle CreateCountUp(this Timer timer, float initializeTime = 0f, TimerRegisterOptions options = default, bool autoUnregister = false)
        {
            timer.CountUpRegister(initializeTime, out var ticket, options);
            return new TimerHandle(timer, ticket, autoUnregister);
        }

        public static TimerHandle CreatePulse(this Timer timer, float initializeTime, float pulseInterval, TimerRegisterOptions options = default, bool autoUnregister = false)
        {
            timer.PulseRegister(initializeTime, pulseInterval, out var ticket, options);
            return new TimerHandle(timer, ticket, autoUnregister);
        }

        public static TimerHandle CreateUpDown(this Timer timer, float duration, TimerRegisterOptions options = default, bool autoUnregister = false)
        {
            timer.UpDownRegister(duration, out var ticket, options);
            return new TimerHandle(timer, ticket, autoUnregister);
        }
    }
}
