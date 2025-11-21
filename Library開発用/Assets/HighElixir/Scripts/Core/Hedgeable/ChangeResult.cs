using System;

namespace HighElixir.Hedgeable
{
    public readonly struct ChangeResult<T>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        public readonly bool IsSuccess;
        public readonly T OldValue;
        public readonly T NewValue;

        internal ChangeResult(bool isSuccess, T oldValue, T newValue)
        {
            IsSuccess = isSuccess;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public static explicit operator bool(ChangeResult<T> result) => result.IsSuccess;
    }
}