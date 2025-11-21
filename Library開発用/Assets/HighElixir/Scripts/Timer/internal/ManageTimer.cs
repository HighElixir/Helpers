using System.Collections.Generic;

#if UNITY_EDITOR
namespace HighElixir.Timers.Internal
{
    /// <summary>
    /// エディタから監視するための管理クラス
    /// </summary>
    internal static class TimerManager
    {
        public static readonly List<Timer> _timers = new List<Timer>();

        public static IReadOnlyList<Timer> Timers => _timers.AsReadOnly();
        public static void Register(Timer timer) { _timers.Add(timer); }
        public static void Unregister(Timer timer) { _timers.Remove(timer); }
        public static void Clear() { _timers?.Clear(); }
    }
}
#endif