using System;

namespace HighElixir.Timers.Extensions
{
    public static class TimerGroupExt
    {
        public static int StartGroup(this Timer timer, string group, bool isLazy = false)
            => ExecuteGroup(timer, group, TimeOperation.Start, isLazy);

        public static int StopGroup(this Timer timer, string group, bool isLazy = false)
            => ExecuteGroup(timer, group, TimeOperation.Stop, isLazy);

        public static int ResetGroup(this Timer timer, string group, bool isLazy = false)
            => ExecuteGroup(timer, group, TimeOperation.Reset, isLazy);

        public static int RestartGroup(this Timer timer, string group, bool isLazy = false)
            => ExecuteGroup(timer, group, TimeOperation.Restart, isLazy);

        public static int InitializeGroup(this Timer timer, string group, bool isLazy = false)
            => ExecuteGroup(timer, group, TimeOperation.Initialize, isLazy);

        public static int UnregisterGroup(this Timer timer, string group)
            => UnregisterGroupCore(timer, group);

        public static int StartTag(this Timer timer, string tag, bool isLazy = false)
            => ExecuteTag(timer, tag, TimeOperation.Start, isLazy);

        public static int StopTag(this Timer timer, string tag, bool isLazy = false)
            => ExecuteTag(timer, tag, TimeOperation.Stop, isLazy);

        public static int ResetTag(this Timer timer, string tag, bool isLazy = false)
            => ExecuteTag(timer, tag, TimeOperation.Reset, isLazy);

        public static int RestartTag(this Timer timer, string tag, bool isLazy = false)
            => ExecuteTag(timer, tag, TimeOperation.Restart, isLazy);

        public static int InitializeTag(this Timer timer, string tag, bool isLazy = false)
            => ExecuteTag(timer, tag, TimeOperation.Initialize, isLazy);

        public static int UnregisterTag(this Timer timer, string tag)
            => UnregisterTagCore(timer, tag);

        private static int ExecuteGroup(Timer timer, string group, TimeOperation operation, bool isLazy)
        {
            if (string.IsNullOrWhiteSpace(group))
                return 0;

            var tickets = timer.GetTicketsByGroup(group);
            int count = 0;
            for (int i = 0; i < tickets.Length; i++)
            {
                if (!timer.Contains(tickets[i]))
                    continue;

                if (isLazy)
                    timer.LazySend(tickets[i], operation);
                else
                    timer.Send(tickets[i], operation);
                count++;
            }
            return count;
        }

        private static int ExecuteTag(Timer timer, string tag, TimeOperation operation, bool isLazy)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return 0;

            var tickets = timer.GetTicketsByTag(tag);
            int count = 0;
            for (int i = 0; i < tickets.Length; i++)
            {
                if (!timer.Contains(tickets[i]))
                    continue;

                if (isLazy)
                    timer.LazySend(tickets[i], operation);
                else
                    timer.Send(tickets[i], operation);
                count++;
            }
            return count;
        }

        private static int UnregisterGroupCore(Timer timer, string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return 0;

            var tickets = timer.GetTicketsByGroup(group);
            int count = 0;
            for (int i = 0; i < tickets.Length; i++)
            {
                if (timer.UnRegister(tickets[i]))
                    count++;
            }
            return count;
        }

        private static int UnregisterTagCore(Timer timer, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return 0;

            var tickets = timer.GetTicketsByTag(tag);
            int count = 0;
            for (int i = 0; i < tickets.Length; i++)
            {
                if (timer.UnRegister(tickets[i]))
                    count++;
            }
            return count;
        }
    }
}
