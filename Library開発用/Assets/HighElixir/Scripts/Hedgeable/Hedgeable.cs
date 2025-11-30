using HighElixir.Implements;
using System;

namespace HighElixir.Hedgeable
{
    /// <summary>
    /// 特定の範囲内で値を変化させることができるラッパー。
    /// </summary>
    public sealed class Hedgeable<T> : IHedgeable<T, Hedgeable<T>>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private T _value;
        private T _minValue;
        private T _maxValue;
        private FailedChangeHandle _handle;
        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
        private int _direction;
        private Action<ChangeResult<T>> _onValueChanged;

        public T Value
        {
            get => _value;
            set
            {
                var oldValue = _value;
                bool result = true;
                if (value.CompareTo(_maxValue) > 0)
                {
                    FailedHandle(value, oldValue);
                    result = false;
                }
                else if (value.CompareTo(_minValue) < 0)
                {
                    FailedHandle(value, oldValue);
                    result = false;
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
                // イベントの発火
                _onValueChanged?.Invoke(new ChangeResult<T>(result, oldValue, _value));
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

        public Hedgeable(T initialValue, T minValue, T maxValue, FailedChangeHandle handle = FailedChangeHandle.Revert, Action<ChangeResult<T>> onValueChanged = null)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _value = initialValue;
            _direction = 0;
            _onValueChanged = onValueChanged;
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
        public IDisposable Subscribe(Action<ChangeResult<T>> onValueChanged)
        {
            _onValueChanged = onValueChanged;
            var ac = _onValueChanged;
            return Disposable.Create(() => ac -= onValueChanged);
        }

        public bool CanSetValue(T newValue)
        {
            return newValue.CompareTo(_minValue) >= 0 && newValue.CompareTo(_maxValue) <= 0;
        }

        private void FailedHandle(T value, T oldValue)
        {
            switch (_handle)
            {
                case FailedChangeHandle.Revert:
                    _value = oldValue;
                    break;
                case FailedChangeHandle.Clamp:
                    if (value.CompareTo(_maxValue) > 0)
                    {
                        _value = _maxValue;
                    }
                    else if (value.CompareTo(_minValue) < 0)
                    {
                        _value = _minValue;
                    }
                    break;
                case FailedChangeHandle.ReturnToZero:
                    _value = default;
                    if (_value.CompareTo(_minValue) < 0)
                        _value = _minValue;
                    else if (_value.CompareTo(_maxValue) > 0)
                        _value = _maxValue;
                    break;
            }
        }
        #region
        public override string ToString()
        {
            return _value.ToString();
        }

        public int CompareTo(Hedgeable<T> other)
        {
            return _value.CompareTo(other._value);
        }

        public bool Equals(Hedgeable<T> other)
        {
            return _value.Equals(other._value);
        }
        #endregion

        public static implicit operator T(Hedgeable<T> h) => h.Value;
    }
}