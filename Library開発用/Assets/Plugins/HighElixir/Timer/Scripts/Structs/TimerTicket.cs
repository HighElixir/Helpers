using System;

namespace HighElixir.Timers
{
    /// <summary>
    /// Timerのチケット情報を表します。
    /// TimerTicketを使用して、特定のTimerインスタンスに関連付けられた時間情報を取得および監視できます。
    /// </summary>
    public readonly struct TimerTicket : IEquatable<TimerTicket>
    {
        public readonly string Key;
        public readonly string Name;
        private readonly Timer _timer;

        private static readonly string Unnamed = "unnamed";

        internal static TimerTicket Take(string name, Timer timer)
        {
            var k = Guid.NewGuid().ToString("N");
            name ??= Unnamed;
            return new TimerTicket(k, name, timer);
        }

        public override string ToString()
        {
            return $"[TimerTicket] Key:{Key}, Name:{Name ?? "Unnamed"}";
        }

        public bool Equals(TimerTicket other)
        {
            return string.Equals(this.Key, other.Key, StringComparison.OrdinalIgnoreCase);
        }

        public float GetCurrent()
        {
            if (_timer.TryGetCurrentTime(this, out var t))
            {
                return t;
            }
            throw new InvalidOperationException("Timer not found.");
        }

        public IObservable<TimeData> ObserveCurrent()
        {
            if (_timer.TryGetTimer(this, out var timer))
            {
                return timer.TimeReactive;
            }
            throw new InvalidOperationException("Timer not found.");
        }
        public override int GetHashCode() => Key?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;

        internal TimerTicket(string key, string name, Timer timer)
        {
            Key = key;
            Name = name;
            _timer = timer;
        }

        public static explicit operator string(TimerTicket t) => t.Key;
    }
}