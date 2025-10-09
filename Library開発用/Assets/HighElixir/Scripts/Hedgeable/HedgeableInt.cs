using System;

namespace HighElixir.Hedgeable
{
    /// <summary>
    /// 特定の範囲内で値を変化させることができる int 型のラッパー。
    /// </summary>
    public class HedgeableInt : IHedgeable<int, HedgeableInt>
    {
        private int _value;
        private int _minValue = int.MinValue;
        private int _maxValue = int.MaxValue;
        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
        private int _direction = 0;
        private Action<int, int> _onHedge;
        public int Value
        {
            get => _value;
            set
            {
                var oldValue = _value;
                if (value > _maxValue)
                {
                    _value = _maxValue;
                    _onHedge?.Invoke(oldValue, _value);
                }
                else if (value < _minValue)
                {
                    _value = _minValue;
                    _onHedge?.Invoke(oldValue, _value);
                }
                else
                {
                    _value = value;
                }
                // Direction の更新
                int diff = _value - oldValue;
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
        public int MinValue => _minValue;
        public int MaxValue => _maxValue;
        public HedgeableInt(int initialValue = 0)
        {
            Value = initialValue;
        }
        public HedgeableInt(int minValue, int maxValue, int initialValue = 0)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            Value = initialValue;
        }

        public HedgeableInt SetMax(int maxValue)
        {
            _maxValue = maxValue;
            if (_value > _maxValue)
            {
                Value = _maxValue;
            }
            return this;
        }
        public HedgeableInt SetMin(int minValue)
        {
            _minValue = minValue;
            if (_value < _minValue)
            {
                Value = _minValue;
            }
            return this;
        }
        public IDisposable Subscribe(Action<int, int> onHedge)
        {
            _onHedge = onHedge;
            return Disposable.Create(() => _onHedge = null);
        }

        public bool CanSetValue(int newValue)
        {
            return newValue >= _minValue && newValue <= _maxValue;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
        public static implicit operator int(HedgeableInt h) => h.Value;
    }
}