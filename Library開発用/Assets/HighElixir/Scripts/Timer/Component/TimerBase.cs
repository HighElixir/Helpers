using HighElixir.Implements.Observables;
using System;

namespace HighElixir.Timers
{
    public abstract class TimerBase : ITimer
    {
        protected ReactiveTimer _reactive;
        protected ReactiveProperty<int> _event = new();
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
        public abstract bool IsFinished { get; }
        public abstract float ArgTime { get; }
        public IObservable<TimeData> TimeReactive => _reactive;

        public IObservable<int> ReactiveTimerEvent => _event;

        private Timer _timer;
        public TimerBase(TimerConfig config)
        {
            _timer = config.Timer;
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

        public abstract void Update(float dt);

        #region 通知
        protected void NotifyComplete()
        {
            lock (_lock)
                try { _event.Value = TimerEventRegistry.TakeEvt(TimeEventType.Finished); }
                catch (Exception ex) { SendError(ex); }
        }
        protected void NotifyStarted()
        {
            lock (_lock)
                try { _event.Value = TimerEventRegistry.TakeEvt(TimeEventType.Start); }
                catch (Exception ex) { SendError(ex); }
        }
        protected void NotifyReset()
        {
            lock (_lock)
                try { _event.Value = TimerEventRegistry.TakeEvt(TimeEventType.Reset); }
                catch (Exception ex) { SendError(ex); }
        }
        protected void NotifyStopped()
        {
            lock (_lock)
                try { _event.Value = TimerEventRegistry.TakeEvt(TimeEventType.Stop); }
                catch (Exception ex) { SendError(ex); }
        }
        protected void NotifyInitialized()
        {
            lock (_lock)
                try { _event.Value = TimerEventRegistry.TakeEvt(TimeEventType.Initialize); }
                catch (Exception ex) { SendError(ex); }
        }
        #endregion

        public void Dispose()
        {
            _reactive.Dispose();
            _event.Dispose();
        }

        public void SendError(System.Exception exception)
        {
            _timer?.SendError(exception);
        }
    }
}