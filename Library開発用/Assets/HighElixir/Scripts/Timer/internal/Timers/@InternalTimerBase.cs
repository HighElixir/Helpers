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
        public event Action OnStarted;
        public event Action OnReset;
        public event Action OnStopped;
        public event Action OnInitialized;
        public event Action OnRemoved;

        private Timer _timer;
        public InternalTimerBase(TimerConfig config)
        {
            _timer = config.Timer;
            if (config.OnFinished != null) OnFinished += config.OnFinished;
            _reactive = new(0);
        }

        // OnFinishedなどのイベントが呼ばれない
        public virtual void Initialize()
        {
            Stop(false);
            Current = InitialTime;
            _reactive.Notify(InitialTime, false);
            NotifyInitialized();
        }

        // OnFinishedが呼ばれる可能性がある
        public virtual void Reset()
        {
            Stop(false);
            Current = InitialTime;
            NotifyReset();
        }

        public virtual void Start()
        {
            if (IsFinished)
                Reset();
            IsRunning = true;
            NotifyStarted();
        }

        public virtual float Stop(bool evt)
        {
            IsRunning = false;
            if (evt) NotifyStopped();
            return Current;
        }
        public float Stop() => Stop(true);
        public void Restart()
        {
            Reset();
            Start();
        }

        public abstract void Update(float dt);

        #region 通知
        protected void NotifyComplete()
        {
            lock (_lock)
                try { OnFinished?.Invoke(); }
                catch (Exception ex) { OnError(ex); }
        }
        protected void NotifyStarted()
        {
            lock (_lock)
                try { OnStarted?.Invoke(); }
                catch (Exception ex) { OnError(ex); }
        }
        protected void NotifyReset()
        {
            lock (_lock)
                try { OnReset?.Invoke(); }
                catch (Exception ex) { OnError(ex); }
        }
        protected void NotifyStopped()
        {
            lock (_lock)
                try { OnStopped?.Invoke(); }
                catch (Exception ex) { OnError(ex); }
        }
        protected void NotifyInitialized()
        {
            lock (_lock)
                try { OnInitialized?.Invoke(); }
                catch (Exception ex) { OnError(ex); }
        }
        #endregion
        public void Dispose()
        {
            _reactive.Dispose();

            // タイマーから削除されたときのイベントを発火
            OnFinished = null;
            OnStarted = null;
            OnReset = null;
            OnStopped = null;
            OnInitialized = null;
            OnRemoved?.Invoke();
            OnRemoved = null;
        }

        public void OnError(System.Exception exception)
        {
            _timer?.OnError(exception);
        }
    }
}