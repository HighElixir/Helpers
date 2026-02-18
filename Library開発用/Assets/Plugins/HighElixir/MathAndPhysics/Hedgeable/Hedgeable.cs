using Newtonsoft.Json.Bson;
using System;

namespace HighElixir.Math.Hedgeable
{
    /// <summary>
    /// 特定の範囲内で値を変化させることができるラッパー。
    /// </summary>
    public struct Hedgeable<T> : IHedgeable<T, Hedgeable<T>>, IComparable<Hedgeable<T>>, IEquatable<Hedgeable<T>>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private T _value;
        private T _minValue;
        private T _maxValue;
        private readonly FailedChangeHandle _handle;

        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
        private int _direction;
        private ChangeResult<T> _lastChangeResult;

        public T CurrentValue
        {
            readonly get => _value;
            set
            {
                var oldValue = _value;
                bool isSuccess = true;
                if (value.CompareTo(_maxValue) > 0 ||
                    value.CompareTo(_minValue) < 0)
                {
                    FailedHandle(value, oldValue);
                    isSuccess = false;
                }
                else
                {
                    _value = value;
                }
                // Direction の更新
                int diff = _value.CompareTo(oldValue);
                if (diff > 0)
                {
                    diff = 1;
                }
                Direction = diff;
                // 値の更新
                _lastChangeResult = new ChangeResult<T>(isSuccess, oldValue, _value);
            }
        }
        /// <summary>
        /// 直前の値からの変化方向を示す。
        /// </summary>
        public int Direction
        {
            readonly get => _direction;
            private set => _direction = (value == 0) ? 0 : value / System.Math.Abs(value);
        }
        /// <summary>
        /// 直前の値の変更結果を示す。
        /// </summary>
        public readonly ChangeResult<T> LastChangeResult => _lastChangeResult;

        public readonly T MinValue => _minValue;
        public readonly T MaxValue => _maxValue;

        public Hedgeable(T initialValue, T minValue, T maxValue, FailedChangeHandle handle = FailedChangeHandle.Revert)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            _value = initialValue;
            _direction = 0;
            _handle = handle;
            _lastChangeResult = new(true, _value, _value);
        }

        public void SetValue(T newValue)
        {
            CurrentValue = newValue;
        }

        public readonly bool CanSetValue(T newValue)
        {
            return newValue.CompareTo(_minValue) >= 0 && newValue.CompareTo(_maxValue) <= 0;
        }

        public bool TrySetValue(T newValue)
        {
            if (CanSetValue(newValue))
            {
                CurrentValue = newValue;
                return true;
            }
            else
            {
                CurrentValue = newValue; // 失敗時のハンドルに任せる
                return false;
            }
        }

        public void SetMax(T maxValue)
        {
            _maxValue = maxValue;
            if (_value.CompareTo(_maxValue) > 0)
            {
                CurrentValue = _maxValue;
            }
        }
        public void SetMin(T minValue)
        {
            _minValue = minValue;
            if (_value.CompareTo(_minValue) < 0)
            {
                CurrentValue = _minValue;
            }
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

        public int CompareTo(Hedgeable<T> other)
        {
            return _value.CompareTo(other._value);
        }

        public bool Equals(Hedgeable<T> other)
        {
            return _value.Equals(other._value);
        }
        #endregion

        public static implicit operator T(Hedgeable<T> h) => h.CurrentValue;
    }
}