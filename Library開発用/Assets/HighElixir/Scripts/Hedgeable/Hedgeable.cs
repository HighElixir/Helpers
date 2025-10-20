using HighElixir.Implements;
using System;

namespace HighElixir.Hedgeable
{
    /// <summary>
    /// 特定の範囲内で値を変化させることができる int 型のラッパー。
    /// </summary>
    public struct Hedgeable<T> : IHedgeable<T, Hedgeable<T>>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private T _value;
        private T _minValue;
        private T _maxValue;
        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
        private int _direction;
        private Action<T, T> _onHedge;
        public T Value
        {
            get => _value;
            set
            {
                var oldValue = _value;
                if (value.CompareTo(_maxValue) > 0)
                {
                    _value = _maxValue;
                    _onHedge?.Invoke(oldValue, _value);
                }
                else if (value.CompareTo(_minValue) < 0)
                {
                    _value = _minValue;
                    _onHedge?.Invoke(oldValue, _value);
                }
                else
                {
                    _value = value;
                }
                // Direction の更新
                int diff = _value.CompareTo(oldValue);
                if (diff > 0)
                {
                    Direction = 1;
                    return;
                }
                Direction = diff;
            }
        }
        /// <summary>
        /// 直前の値からの変化方向を示す。
        /// </summary>
        public int Direction
        {
            get => _direction;
            private set => _direction = (value == 0) ? 0 : value / Math.Abs(value);
        }
        public T MinValue => _minValue;
        public T MaxValue => _maxValue;

        public Hedgeable(T initialValue, T minValue, T maxValue, Action<T, T> onHedge = null)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _value = initialValue;
            _direction = 0;
            _onHedge = onHedge;
        }


        public Hedgeable<T> SetMax(T maxValue)
        {
            _maxValue = maxValue;
            if (_value.CompareTo(_maxValue) > 0)
            {
                Value = _maxValue;
            }
            return this;
        }
        public Hedgeable<T> SetMin(T minValue)
        {
            _minValue = minValue;
            if (_value.CompareTo(_minValue) < 0)
            {
                Value = _minValue;
            }
            return this;
        }
        public IDisposable Subscribe(Action<T, T> onHedge)
        {
            _onHedge = onHedge;
            var ac = _onHedge;
            return Disposable.Create(() => ac -= onHedge);
        }

        public bool CanSetValue(T newValue)
        {
            return newValue.CompareTo(_minValue) >= 0 && newValue.CompareTo(_maxValue) <= 0;
        }


        public override string ToString()
        {
            return _value.ToString();
        }
        public static implicit operator T(Hedgeable<T> h) => h.Value;
    }
}