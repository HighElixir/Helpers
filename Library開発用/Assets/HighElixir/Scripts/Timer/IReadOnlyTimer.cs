using System.Collections.Generic;

namespace HighElixir.Timers
{
    /// <summary>
    /// 読み取り専用のタイマーインターフェース
    /// </summary>
    public interface IReadOnlyTimer
    {
        int CommandCount { get; }
        string ParentName { get; }
        IEnumerable<TimerSnapshot> GetSnapshot();
    }
}