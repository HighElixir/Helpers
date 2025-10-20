using System;

namespace HighElixir.Timers.Internal
{
    internal abstract class InternalTimerBase : ITimer
    {
        protected FloatReactive _reactive;
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
                lock (_lock)
                {
                    _current = value;
                    _reactive.Notify(_current, IsRunning);
                }
            }
        }
        public bool IsRunning { get; protected set; } = false;
        public abstract float NormalizedElapsed { get; }
        public abstract bool IsFinished { get; }
        public abstract CountType CountType { get; }

        public IObservable<TimeData> TimeReactive => _reactive;

        public event Action OnFinished; // null 許容

        private Timer _timer;
        private TimerTicket _timerTicket;
        public InternalTimerBase(TimerConfig config)
        {
            _timer = config.Timer;
            if (config.OnFinished != null) OnFinished += config.OnFinished;
            _reactive = new(0);
        }

        // OnFinishedなどのイベントが呼ばれない
        public virtual void Initialize()
        {
            Stop();
            Current = InitialTime;
            _reactive.Notify(InitialTime, false);
        }

        // OnFinishedが呼ばれる可能性がある
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

        protected void InvokeEventSafely()
        {
            lock (_lock)
            {
                try
                {
                    OnFinished?.Invoke();
                    _timer.OnTimerFinished(_timerTicket);
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

        public void SetTicket(TimerTicket ticket)
        {
            _timerTicket = ticket;
        }
    }
}