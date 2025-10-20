using System;
using System.Collections.Generic;

namespace HighElixir.Timers.Extensions
{
    public static class TimerExtentions
    {
        private sealed class Obs : IObserver<TimeData>, IDisposable
        {
            private Action _onLaze;
            private bool _isExecuted = false;
            private float _lazyTime;
            private IDisposable _disposable;
            public Obs(float lazy, Action action)
            {
                _onLaze = action;
                _lazyTime = lazy;
            }
            public void OnNext(TimeData value)
            {
                if (!_isExecuted && value.Current > _lazyTime)
                {
                    _onLaze.Invoke();
                    _disposable?.Dispose();
                    Remove(this);
                    _isExecuted = true;
                }
            }

            public IDisposable AddDisposeAsAction(IDisposable disposable)
            {
                _disposable = disposable;
                return this;
            }
            public void OnCompleted() { }

            // 握りつぶす
            public void OnError(Exception error) { }

            public void Dispose()
            {
                _disposable?.Dispose();
                _onLaze = null;
            }
        }
        // TODO : 遅延実行、タグ付き管理(別クラス)の実装
        // GlobalTimerを使用して実行
        private static TimerTicket _lazyTicket;
        private static bool _lazyInitialized = false;
        private static List<Obs> _lazyCommand;
        private static object _lock = new object();

        public static IDisposable DOStart(this Timer t, TimerTicket ticket, float lazyTime)
        {
            Action ac = () => t.Start(ticket, false, true);
            return RegisterTicket(ticket, lazyTime, ac);
        }
        public static IDisposable DOStop(this Timer t, TimerTicket ticket, float lazyTime, bool init = true)
        {
            Action ac = () => t.Stop(ticket, init, true);
            return RegisterTicket(ticket, lazyTime, ac);
        }
        public static IDisposable DORestart(this Timer t, TimerTicket ticket, float lazyTime)
        {
            Action ac = () => t.Restart(ticket, true);
            return RegisterTicket(ticket, lazyTime, ac);
        }
        public static IDisposable DOReset(this Timer t, TimerTicket ticket, float lazyTime)
        {
            Action ac = () => t.Reset(ticket, true);
            return RegisterTicket(ticket, lazyTime, ac);
        }

        private static IDisposable RegisterTicket(TimerTicket ticket, float lazyTime, Action action)
        {
            lock (_lock)
            {
                if (!_lazyInitialized)
                {
                    _lazyCommand = new();
                    _lazyTicket = GlobalTimer.Update.CountUpRegister("Lazy", andStart: true);
                    _lazyInitialized = true;
                }
                if (!GlobalTimer.Update.IsRunning(_lazyTicket))
                    GlobalTimer.Update.Start(_lazyTicket);
            }
            if (!GlobalTimer.Update.TryGetCurrentTime(_lazyTicket, out var t))
                t = 0f;
            var obs = new Obs(t + lazyTime, action);
            var dis = obs.AddDisposeAsAction(GlobalTimer.Update.GetReactiveProperty(_lazyTicket).Subscribe(obs));
            _lazyCommand.Add(obs);
            return dis;
        }

        private static void Remove(Obs obs)
        {
            lock (_lock)
            {
                if (_lazyCommand == null) return;
                _lazyCommand.Remove(obs);
                if (_lazyCommand.Count <= 0)
                {
                    GlobalTimer.Update.Reset(_lazyTicket);
                }
            }
        }
        static TimerExtentions()
        {
#if UNITY_2017_1_OR_NEWER
            UnityEngine.Application.quitting += () =>
            {
                foreach (var item in _lazyCommand)
                {
                    item?.Dispose();
                }
            };
#endif
        }
    }
}