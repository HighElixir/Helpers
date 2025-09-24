using System;

namespace HighElixir
{
    public class LoopableFloat : IHedgeable<float, LoopableFloat>
    {
        private float _value;
        private float _minValue = float.MinValue;
        private float _maxValue = float.MaxValue;

        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
        private int _direction = 0;
        private Action<float, float> _onLoop;

        public float Value
        {
            get => _value;
            set
            {
                float oldValue = _value;
                float rangeSize = _maxValue - _minValue;

                if (rangeSize <= 0f)
                {
                    _value = _minValue;
                    return;
                }

                float relative = (value - _minValue) % rangeSize;
                if (relative < 0f) relative += rangeSize;

                float newValue = _minValue + relative;

                int diff = (int)(newValue - oldValue);
                Direction = diff;

                _value = newValue;

                if (Math.Abs(value - oldValue) >= rangeSize)
                {
                    _onLoop?.Invoke(oldValue, newValue);
                }
            }
        }

        public int Direction
        {
            get => _direction;
            private set => _direction = (value == 0) ? 0 : value / Math.Abs(value);
        }

        public float MinValue => _minValue;
        public float MaxValue => _maxValue;
        public LoopableFloat(float initialValue)
        {
            Value = initialValue;
        }

        public LoopableFloat(float minValue, float maxValue, float initialValue = 0f)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            Value = initialValue;
        }

        public LoopableFloat SetMin(float minValue)
        {
            _minValue = minValue;
            if (Value < minValue)
                Value = Value;
            return this;
        }

        public LoopableFloat SetMax(float maxValue)
        {
            _maxValue = maxValue;
            if (Value > maxValue)
                Value = Value;
            return this;
        }
        public IDisposable Subscribe(Action<float, float> onLoop)
        {
            _onLoop += onLoop;
            return Disposable.Create(() => _onLoop -= onLoop);
        }

        public bool CanSetValue(float newValue)
        {
            // LoopableFloatは常に値を設定できる
            return true;
        }

        public static implicit operator float(LoopableFloat loop) => loop.Value;
    }
}
