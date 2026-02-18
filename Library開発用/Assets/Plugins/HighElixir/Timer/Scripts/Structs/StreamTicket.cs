using System;

namespace HighElixir.Timers
{
    /// <summary>
    /// TimeStream のスケジュール識別子。
    /// </summary>
    public readonly struct StreamTicket : IEquatable<StreamTicket>
    {
        public readonly string Key;
        public readonly string Name;

        private static readonly string Unnamed = "stream";

        internal static StreamTicket Take(string name)
        {
            var key = Guid.NewGuid().ToString("N");
            return new StreamTicket(key, name ?? Unnamed);
        }

        internal StreamTicket(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public bool Equals(StreamTicket other)
            => string.Equals(Key, other.Key, StringComparison.Ordinal);

        public override bool Equals(object obj)
            => obj is StreamTicket other && Equals(other);

        public override int GetHashCode()
            => Key?.GetHashCode(StringComparison.Ordinal) ?? 0;

        public override string ToString()
            => $"[StreamTicket] Key:{Key}, Name:{Name ?? Unnamed}";
    }
}
