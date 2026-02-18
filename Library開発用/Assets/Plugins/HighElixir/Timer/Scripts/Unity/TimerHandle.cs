using HighElixir.Timers.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.Timers.Unity
{
    /// <summary>
    /// Timer + TimerTicket をまとめて扱う軽量ハンドル。
    /// </summary>
    public sealed class TimerHandle : IDisposable
    {
        private Timer _timer;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly bool _autoUnregister;
        private bool _disposed;

        public TimerTicket Ticket { get; }
        public string Name => Ticket.Name;
        public bool IsValid => _timer != null && _timer.Contains(Ticket);
        public bool IsRunning => _timer != null && _timer.IsRunning(Ticket);

        public TimerHandle(Timer timer, TimerTicket ticket, bool autoUnregister = true)
        {
            _timer = timer;
            Ticket = ticket;
            _autoUnregister = autoUnregister;
        }

        public IObservable<TimeData> ObserveTime()
        {
            return _timer != null
                ? _timer.GetReactiveProperty(Ticket)
                : new HighElixir.Implements.Observables.Empty<TimeData>();
        }

        public IObservable<TimeEventType> ObserveEvents()
        {
            return _timer != null
                ? _timer.GetTimerEvtType(Ticket)
                : new HighElixir.Implements.Observables.Empty<TimeEventType>();
        }

        public void Start(bool isLazy = false) => _timer?.Start(Ticket, isLazy);
        public void Stop(bool isLazy = false) => _timer?.Stop(Ticket, isLazy);
        public void Reset(bool isLazy = false) => _timer?.Reset(Ticket, isLazy);
        public void Restart(bool isLazy = false) => _timer?.Restart(Ticket, isLazy);
        public void Initialize(bool isLazy = false) => _timer?.Initialize(Ticket, isLazy);

        public TimerHandle Track(IDisposable disposable)
        {
            if (disposable == null)
                return this;

            if (_disposed)
            {
                disposable.Dispose();
                return this;
            }

            _subscriptions.Add(disposable);
            return this;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            for (int i = 0; i < _subscriptions.Count; i++)
                _subscriptions[i]?.Dispose();
            _subscriptions.Clear();

            var timer = _timer;
            _timer = null;

            if (!_autoUnregister || timer == null)
                return;

            timer.UnRegister(Ticket);
        }
    }

    public static class TimerHandleExtensions
    {
        public static TimerHandle AsHandle(this Timer timer, TimerTicket ticket, bool autoUnregister = false)
            => new TimerHandle(timer, ticket, autoUnregister);

        public static TimerHandle BindTo(this Timer timer, TimerTicket ticket, MonoBehaviour owner, bool autoUnregister = true)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            return BindTo(timer, ticket, owner.gameObject, autoUnregister);
        }

        public static TimerHandle BindTo(this Timer timer, TimerTicket ticket, GameObject owner, bool autoUnregister = true)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            var handle = new TimerHandle(timer, ticket, autoUnregister);
            var binder = owner.GetComponent<TimerHandleBinder>();
            if (binder == null)
                binder = owner.AddComponent<TimerHandleBinder>();

            binder.Register(handle);
            return handle;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class TimerHandleBinder : MonoBehaviour
    {
        private readonly List<TimerHandle> _handles = new();

        internal void Register(TimerHandle handle)
        {
            if (handle == null) return;
            _handles.Add(handle);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _handles.Count; i++)
                _handles[i]?.Dispose();

            _handles.Clear();
        }
    }
}
