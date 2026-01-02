namespace HighElixir.Timers.Internal
{
    internal static class TimeOperator
    {
        // 操作前のCurrentを返す
        public static float Operation(this ITimer timer, TimeOperation op)
        {
            // 優先順
            // Init => Reset => Start => Stop
            var current = timer.Current;
            if (op.Has(TimeOperation.Initialize))
                timer.Initialize();
            if (op.Has(TimeOperation.Reset))
                timer.Reset();
            if (op.Has(TimeOperation.Start))
                timer.Start();
            if (op.Has(TimeOperation.Stop))
                timer.Stop();
            return current;
        }
    }
}