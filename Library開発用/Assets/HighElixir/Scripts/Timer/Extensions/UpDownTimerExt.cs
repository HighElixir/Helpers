using HighElixir.Implements;
using System;

namespace HighElixir.Timers.Extensions
{
    public static class UpDownTimerExt
    {
        public static IDisposable OnReverseEvt(this Timer t, TimerTicket ticket, Action<bool> action)
        {
            if (TryGet(t, ticket, out var up))
            {
                up.OnReversed += action;
                return Disposable.Create(() =>
                {
                    if (up != null)
                        up.OnReversed -= action;
                });
            }
            return null;
        }

        public static void ReverseDirection(this Timer t, TimerTicket ticket)
        {
            if (TryGet(t, ticket, out var up))
            {
                up.ReverseDirection();
            }
        }
        public static void SetDirection(this Timer t, TimerTicket ticket, bool isUp)
        {
            if (TryGet(t, ticket, out var up))
            {
                up.SetDirection(isUp);
            }
        }
        public static void ReverseAndStart(this Timer t, TimerTicket ticket, bool isLazy = false, bool onlyNotRun = false)
        {
            if (TryGet(t, ticket, out var up))
            {
                up.ReverseDirection();
                if (onlyNotRun && up.IsRunning) return;
                t.Start(ticket, false, isLazy);
            }
        }
        public static void SetDirectionAndStart(this Timer t, TimerTicket ticket, bool isUp, bool isLazy = false, bool onlyNotRun = false)
        {
            if (TryGet(t, ticket, out var up))
            {
                up.SetDirection(isUp);
                if (onlyNotRun && up.IsRunning) return;
                t.Start(ticket, false, isLazy);
            }
        }

        private static bool TryGet(Timer t, TimerTicket ticket, out IUpAndDown upAndDown)
        {
            upAndDown = null;
            if (!t.TryGetTimer(ticket, out var ti)) return false;
            if (ti is not IUpAndDown up) return false;
            upAndDown = up;
            return true;
        }
    }
}