namespace HighElixir.Timers.Extensions
{
    public static class UpDownTimerExt
    {
        public static void ReverseDirection(this Timer t, TimerTicket ticket)
        {
            if (t.TryGetTimer(ticket, out var timer) && timer is IUpAndDown ud)
            {
                ud.ReverseDirection();
            }
        }
        public static void SetDirection(this Timer t, TimerTicket ticket, bool isUp)
        {
            if (t.TryGetTimer(ticket, out var timer) && timer is IUpAndDown ud)
            {
                ud.SetDirection(isUp);
            }
        }
        public static void ReverseAndStart(this Timer t, TimerTicket ticket, bool isLazy = false, bool onlyNotRun = false)
        {
            if (t.TryGetTimer(ticket, out var timer) && timer is IUpAndDown ud)
            {
                ud.ReverseDirection();
                if (onlyNotRun && timer.IsRunning) return;
                t.Start(ticket, false, isLazy);
            }
        }
        public static void SetDirectionAndStart(this Timer t, TimerTicket ticket, bool isUp, bool isLazy = false)
        {
            if (t.TryGetTimer(ticket, out var timer) && timer is IUpAndDown ud)
            {
                ud.SetDirection(isUp);
                t.Start(ticket, false, isLazy);
            }
        }
    }
}