using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;

namespace HighElixir.Timers
{
    /// <summary>
    /// 読み取り専用のタイマーインターフェース
    /// </summary>
    public interface IReadOnlyTimer
    {
        int CommandCount { get; }
        Type ParentType { get; }
        IEnumerable<TimerSnapshot> GetSnapshot();
    }
}