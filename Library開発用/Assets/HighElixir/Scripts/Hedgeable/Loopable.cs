using System;
using HighElixir.Implements;

namespace HighElixir.Hedgeable
{
    /// <summary>
    /// 特定の範囲内で循環（ループ）する値を扱う構造体。
    /// </summary>
    public struct Loopable<T> : IHedgeable<T, Loopable<T>>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private T _value;
        private T _minValue;
        private T _maxValue;
        private int _direction;
        private Action<T, T> _onLoop;

        public int Direction
        {
            get => _direction;
            private set => _direction = (value == 0) ? 0 : value / Math.Abs(value);
        }

        public T Value
        {
            get => _value;
            set
            {
                var oldValue = _value;
                _value = LoopValue(value);

                int diff = Compare(_value, oldValue);
                Direction = diff;

                // ループ判定：範囲を超えて循環した時だけ通知
                if (IsLooped(oldValue, value))
                {
                    _onLoop?.Invoke(oldValue, _value);
                }
            }
        }

        public T MinValue => _minValue;
        public T MaxValue => _maxValue;

        public Loopable(T minValue, T maxValue, T initialValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _direction = 0;
            _onLoop = null;
            _value = LoopValueStatic(initialValue, minValue, maxValue);
        }

        public IDisposable Subscribe(Action<T, T> onLoop)
        {
            _onLoop += onLoop;
            var ac = _onLoop;
            return Disposable.Create(() => ac -= onLoop);
        }

        public Loopable<T> SetMax(T maxValue)
        {
            _maxValue = maxValue;
            _value = LoopValue(_value);
            return this;
        }

        public Loopable<T> SetMin(T minValue)
        {
            _minValue = minValue;
            _value = LoopValue(_value);
            return this;
        }

        public bool CanSetValue(T newValue)
        {
            return Compare(newValue, _minValue) >= 0 && Compare(newValue, _maxValue) <= 0;
        }

        public override string ToString() => _value.ToString();
        public static implicit operator T(Loopable<T> h) => h.Value;

        // --- 内部ヘルパー ---

        private T LoopValue(T value)
        {
            return LoopValueStatic(value, _minValue, _maxValue);
        }

        private static T LoopValueStatic(T value, T min, T max)
        {
            // dynamicで数値型の演算を汎用化
            dynamic v = value, mn = min, mx = max;
            dynamic range = (mx - mn) + 1;
            if (range <= 0) return mn;

            dynamic mod = (v - mn) % range;
            if (mod < 0) mod += range;

            return (T)(mn + mod);
        }

        private bool IsLooped(T oldValue, T newInput)
        {
            dynamic oldV = oldValue, newV = newInput, mn = _minValue, mx = _maxValue;
            dynamic range = (mx - mn) + 1;
            return Math.Abs(newV - oldV) >= range;
        }

        private static int Compare(T a, T b)
        {
            return a.CompareTo(b);
        }
    }
}
