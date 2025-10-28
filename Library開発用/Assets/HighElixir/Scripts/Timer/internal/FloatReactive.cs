using HighElixir.Implements;
using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    // 依存をなるべく減らすための簡易的な実装
    internal class FloatReactive : IObservable<TimeData>, IDisposable
    {
        private readonly static float Threshold = 0.0005f;
        private readonly HashSet<IObserver<TimeData>> _observers;

        private float _before;

        public FloatReactive(float before = 0)
        {
            _before = before;
            _observers = new();
        }
        public IDisposable Subscribe(IObserver<TimeData> observer)
        {
            observer.OnNext(new TimeData(_before, 0f));
            _observers.Add(observer);
            return Disposable.Create(() => Dispose_Internal(observer));
        }

        internal void Notify(float newAmount, bool shouldNotify)
        {
            var delta = newAmount - _before;
            float abs = Math.Abs(delta);
            if (shouldNotify && abs > Threshold)
            {
                foreach (var observer in _observers)
                    observer.OnNext(new TimeData(newAmount, delta));
            }
            _before = newAmount;
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
            _observers.Clear();
        }
        private void Dispose_Internal(IObserver<TimeData> observer)
        {
            if (_observers.Remove(observer))
                observer.OnCompleted();
        }
    }
}