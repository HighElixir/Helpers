//using HighElixir.Implements;
//using System;

//namespace HighElixir.Hedgeable
//{
//    public class LoopableInt : IHedgeable<int, LoopableInt>
//    {
//        private int _value;
//        private int _minValue = int.MinValue;
//        private int _maxValue = int.MaxValue;

//        // -1 ならば負方向、+1 ならば正方向、0 ならば変化なし
//        private int _direction = 0;
//        private Action<int, int> _onLoop;
//        public int Direction
//        {
//            get => _direction;
//            private set => _direction = (value == 0) ? 0 : value / Math.Abs(value);
//        }
//        public int Value
//        {
//            get => _value;
//            set
//            {
//                int oldValue = _value;
//                int rangeSize = _maxValue - _minValue + 1;

//                if (rangeSize <= 0)
//                {
//                    _value = _minValue;
//                    return;
//                }

//                int mod = (value - _minValue) % rangeSize;
//                if (mod < 0) mod += rangeSize;

//                int newValue = _minValue + mod;

//                // Direction の更新
//                int diff = newValue - oldValue;
//                Direction = diff;

//                // 値の更新
//                _value = newValue;

//                // ループ発動条件：入力と出力の差が rangeSize を超えていたら
//                if (Math.Abs(value - oldValue) >= rangeSize)
//                {
//                    _onLoop?.Invoke(oldValue, _value);
//                }
//            }
//        }
//        public int MinValue => _minValue;
//        public int MaxValue => _maxValue;
//        public LoopableInt(int initialValue = 0)
//        {
//            Value = initialValue;
//        }
//        public LoopableInt(int minValue, int maxValue, int initialValue = 0)
//        {
//            _minValue = minValue;
//            _maxValue = maxValue;
//            Value = initialValue;
//        }
//        public LoopableInt SetMax(int maxValue)
//        {
//            _maxValue = maxValue;
//            if (Value > maxValue)
//                Value = Value;
//            return this;
//        }
//        public LoopableInt SetMin(int minValue)
//        {
//            _minValue = minValue;
//            if (Value < minValue)
//                Value = minValue;
//            return this;
//        }
//        public IDisposable Subscribe(Action<int, int> onLoop)
//        {
//            _onLoop += onLoop;
//            return Disposable.Create(() => _onLoop -= onLoop);
//        }

//        public bool CanSetValue(int newValue)
//        {
//            throw new NotImplementedException();
//        }

//        public static implicit operator int(LoopableInt loop) => loop.Value;
//    }
//}