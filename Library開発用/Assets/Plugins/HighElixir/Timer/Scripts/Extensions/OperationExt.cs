namespace HighElixir.Timers.Extensions
{
    public static class OperationExt
    {
        public static void Start(this Timer timer, TimerTicket ticket, bool isLazy = false)
        {
            Execute(timer, ticket, TimeOperation.Start, isLazy);
        }
        public static void Stop(this Timer timer, TimerTicket ticket, bool isLazy = false)
        {
            Execute(timer, ticket, TimeOperation.Stop, isLazy);
        }
        public static void Stop(this Timer timer, TimerTicket ticket, out float current)
        {
            current = Execute(timer, ticket, TimeOperation.Stop, false);
        }

        public static void Reset(this Timer timer, TimerTicket ticket, bool isLazy = false)
        {
            Execute(timer, ticket, TimeOperation.Reset, isLazy);
        }
        public static void Reset(this Timer timer, TimerTicket ticket, out float current)
        {
            current = Execute(timer, ticket, TimeOperation.Reset, false);
        }
        public static void Restart(this Timer timer, TimerTicket ticket, bool isLazy = false)
        {
            Execute(timer, ticket, TimeOperation.Restart, isLazy);
        }
        public static void Restart(this Timer timer, TimerTicket ticket, out float current)
        {
            current = Execute(timer, ticket, TimeOperation.Restart, false);
        }
        public static void Initialize(this Timer timer, TimerTicket ticket, bool isLazy = false)
        {
            Execute(timer, ticket, TimeOperation.Initialize, isLazy);
        }
        private static float Execute(Timer timer, TimerTicket ticket, TimeOperation operation, bool isLazy)
        {
            if (isLazy)
            {
                timer.LazySend(ticket, operation);
                return -1;
            }
            else
            {
                return timer.Send(ticket, operation);
            }
        }
    }
}