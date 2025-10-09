using System;

namespace HighElixir.Timers
{
    // 重複防止のためのチケット。GUIDを使用しているためほとんど衝突の心配がない
    public readonly struct TimerTicket
    {
        public readonly string Key;
        public readonly string Name;

        private static readonly string Unnamed = "unnamed";
        internal static TimerTicket Take(string name)
        {
            var k = Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(name))
                name = Unnamed;
            return new TimerTicket(k, name);
        }
        public override string ToString()
        {
            return $"[TimerTicket] Key:{Key}, Name:{Name ?? "Unnamed"}";
        }
        internal TimerTicket(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public static explicit operator string(TimerTicket t) => t.Key;
    }
}