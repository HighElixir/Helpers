using System;

namespace HighElixir
{
    public class HedgeableFloat : IHedgeable<float, HedgeableFloat>
    {
        private float _value;
        private float _minValue = float.MinValue;
        private float _maxValue = float.MaxValue;
        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
        private int _direction = 0;

        // before, after
        private Action<float, float> _onHedge;
        public float Value
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
                var tmp = _value - oldValue;
                if (tmp > 0)
                    Direction = 1;
                else if (tmp < 0)
                    Direction = -1;
                else
                    Direction = 0;
            }
        }
        public int Direction
        {
            get => _direction;
            private set => _direction = value;
        }
        public float MinValue => _minValue;
        public float MaxValue => _maxValue;
        public HedgeableFloat(float initialValue = 0)
        {
            Value = initialValue;
        }
        public HedgeableFloat(float minValue, float maxValue, float initialValue = 0)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            Value = initialValue;
        }

        public HedgeableFloat SetMax(float maxValue)
        {
            _maxValue = maxValue;
            if (_value > _maxValue)
            {
                Value = _value;
            }
            return this;
        }
        public HedgeableFloat SetMin(float minValue)
        {
            _minValue = minValue;
            if (_value < _minValue)
            {
                Value = _value;
            }
            return this;
        }
        public IDisposable Subscribe(Action<float, float> onHedge)
        {
            _onHedge += onHedge;
            return Disposable.Create(() => _onHedge -= onHedge);
        }

        public bool CanSetValue(float newValue)
        {
            return newValue >= _minValue && newValue <= _maxValue;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
        public static implicit operator float(HedgeableFloat h) => h.Value;
    }
}