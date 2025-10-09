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

            private  float _before;

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

            internal void Notify(float newAmount)
            {
                float abs = Math.Abs(newAmount - _before);
                if (abs > Threshold)
                {
                    _before = newAmount;
                    foreach (var observer in _observers)
                    {
                        observer.OnNext(_before);
                    }
                }
            }

            public void Dispose()
            {
                foreach (var observer in _observers)
                {
                    Dispose_Internal(observer);
                }
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
        public float InitialTime { get; set; }
        public float Current
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
                _reactive.Notify(_current);
            }
        }
        public bool IsRunning { get; protected set; }
        public abstract float NormalizedElapsed { get; }
        public abstract bool IsFinished { get; }
        public abstract CountType CountType { get; }

        public IObservable<float> ElapsedReactiveProperty => _reactive;

        public event Action OnFinished; // null 許容


        public InternalTimerBase(Action onFinished = null)
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
            IsRunning = true;
        }

        public virtual void Stop()
        {
            IsRunning = false;
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
                    UnityEngine.Debug.LogError($"[HighElixir.Timers] Timer OnFinished callback threw an exception: {ex}");
                }
            }
        }

        public void Dispose()
        {
            _reactive.Dispose();
            OnFinished = null;
        }
    }
}