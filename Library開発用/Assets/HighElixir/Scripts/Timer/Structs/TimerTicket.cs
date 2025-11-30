using System;

namespace HighElixir.Timers
{
    // 重複防止のためのチケット
    public readonly struct TimerTicket : IEquatable<TimerTicket>
    {
        public readonly string Key;
        public readonly string Name;

        private static readonly string Unnamed = "unnamed";

        internal static TimerTicket Take(string name)
        {
            var k = Guid.NewGuid().ToString("N");
            name = name ?? Unnamed;
            return new TimerTicket(k, name);
        }

        public override string ToString()
        {
            return $"[TimerTicket] Key:{Key}, Name:{Name ?? "Unnamed"}";
        }

        public bool Equals(TimerTicket other)
        {
            return string.Equals(this.Key, other.Key, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode() => Key?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;

        internal TimerTicket(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public static explicit operator string(TimerTicket t) => t.Key;
    }
}