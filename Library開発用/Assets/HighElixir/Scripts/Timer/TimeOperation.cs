using System;

namespace HighElixir.Timers
{
    [Flags]
    public enum TimeOperation
    {
        None = 0,
        Initialize = 1 << 0,
        Reset = 1 << 1,
        Start = 1 << 2,
        Stop = 1 << 3,
        Restart = Reset | Start,
    }

    public static class OperationExt
    {
        public static bool Has(this TimeOperation operation, TimeOperation target)
        {
            return (operation & target) != 0;
        }
    }
}