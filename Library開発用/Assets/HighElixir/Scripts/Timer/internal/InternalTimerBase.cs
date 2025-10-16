using HighElixir.Implements;
using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Internal
{
    internal abstract class InternalTimerBase : ITimer
    {
        // 依存をなるべく減らすための簡易的な実装
        protected class Reactive : IObservable<float>, IDisposable
        {
            private readonly static float Threshold = 0.0005f;
            private readonly HashSet<IObserver<float>> _observers;

            private float _before;

            public Reactive(float before = 0, HashSet<IObserver<float>> observers = null)
            {
                _before = before;
                _observers = observers ?? new();
            }
            public IDisposable Subscribe(IObserver<float> observer)
            {
                observer.OnNext(_before);
                _observers.Add(observer);
                return Disposable.Create(() => Dispose_Internal(observer));
            }

            internal void Notify(float newAmount, bool notify = true)
            {
                float abs = Math.Abs(newAmount - _before);
                if (abs > Threshold)
                {
                    _before = newAmount;
                    if (notify)
                    {
                        foreach (var observer in _observers)
                        {
                            observer.OnNext(abs);
                        }
                    }
                }
            }

            public void Dispose()
            {
                foreach (var observer in _observers)
                {
                    observer.OnCompleted();
                }
                _observers.Clear();
            }
            private void Dispose_Internal(IObserver<float> observer)
            {
                if (_observers.Remove(observer))
                    observer.OnCompleted();
            }
        }
        protected Reactive _reactive;
        private readonly object _lock = new();
        private float _current;
        public virtual float InitialTime { get; set; }
        public virtual float Current
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
                _reactive.Notify(_current, IsRunning);
            }
        }
        public bool IsRunning { get; protected set; } = false;
        public abstract float NormalizedElapsed { get; }
        public abstract bool IsFinished { get; }
        public abstract CountType CountType { get; }

        public IObservable<float> ElapsedReactiveProperty => _reactive;

        public event Action OnFinished; // null 許容

        private Timer _timer;
        public InternalTimerBase(Timer parent, Action onFinished = null)
        {
            if (onFinished != null) OnFinished += onFinished;
            _reactive = new(InitialTime);
        }

        public virtual void Initialize()
        {
            Stop();
            Current = InitialTime;
        }
        public virtual void Reset()
        {
            Stop();
            Current = InitialTime;
        }

        public virtual void Start()
        {
            if (IsFinished)
                Reset();
            IsRunning = true;
        }

        public virtual float Stop()
        {
            IsRunning = false;
            return Current;
        }
        public void Restart()
        {
            Reset();
            Start();
        }

        public abstract void Update(float dt);

        protected void EventInvokeSafely()
        {
            lock (_lock)
            {
                try
                {
                    OnFinished?.Invoke();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public void Dispose()
        {
            _reactive.Dispose();
            OnFinished = null;
        }

        public void OnError(System.Exception exception)
        {
            _timer?.OnError(exception);
        }
    }
}