using HighElixir.Implements;
using HighElixir.Implements.Observables;
using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

// Timers.cs
namespace HighElixir.Timers
{
    /// <summary>
    /// KEY 付きクールダウン/タイマー管理。
    /// </summary>
    public sealed class Timer : IReadOnlyTimer, IDisposable
    {
        private string _parentName;
        private readonly Dictionary<TimerTicket, ITimer> _timers = new();

        // 管理
        private Action<Exception> _onError;
        private bool _disposed;

        // 外部
        internal readonly object _lock = new object();
        private readonly CommandQueue _commandQueue;
        private readonly IReadOnlyTimer _readonlyTimer;

        // スナップショット管理用
        private static readonly List<IReadOnlyTimer> _readOnlytimers = new();

        public string ParentName
        {
            get
            {
                return _parentName;
            }
            set
            {

                if (string.IsNullOrEmpty(value))
                {
                    _parentName = UnknownType.Name;
                }
                else
                {
                    _parentName = value;
                }
            }
        }

        public event Action<Exception> OnError { add => _onError += value; remove => _onError -= value; }
        public static IReadOnlyList<IReadOnlyTimer> AllTimers => _readOnlytimers.AsReadOnly();

        // TimerWatcherから監視する用の変数
        public int CommandCount => _commandQueue.CommandCount;

        public Timer(string parentName = null)
        {
            _parentName = parentName ?? UnknownType.Name;
            _readonlyTimer = new ReadOnlyTimer(this);
            _commandQueue = new CommandQueue(1000, this);
            _readOnlytimers.Add(_readonlyTimer);
        }

        #region 基本操作
        public float Send(TimerTicket ticket, TimeOperation operation)
        {
            if (TryGetTimer(ticket, out var timer))
            {
                return timer.Operation(operation);
            }
            return -1;
        }

        public void LazySend(TimerTicket ticket, TimeOperation operation)
            => _commandQueue.Enqueue(ticket, operation);
        #endregion

        #region 登録処理

        public TimerTicket Register<T>(string name, float initTime, float arg = 1, Action action = null, bool andStart = false)
            where T : ITimer
        {
            lock (_lock)
            {
                var timer = this.Create(typeof(T), initTime, arg, action);
                var ticket = TimerTicket.Take(name);
                if (andStart)
                    timer.Start();
                _timers[ticket] = timer;
                return ticket;
            }
        }
        /// <summary>
        /// 登録解除。存在しなければ false。
        /// </summary>
        public bool UnRegister(TimerTicket ticket)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(ticket, out var timer))
                {
                    timer.Dispose();
                    _timers.Remove(ticket);
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region 情報取得
        public IObservable<TimeData> GetReactiveProperty(TimerTicket ticket)
        {
            if (TryGetTimer(ticket, out var t))
            {
                return t.TimeReactive;
            }
            return new Empty<TimeData>();
        }


        /// <summary>
        /// タイマーが存在するか。
        /// </summary>
        public bool Contains(TimerTicket ticket)
        {
            lock (_lock)
            {
                return _timers.ContainsKey(ticket);
            }
        }

        /// <summary>
        /// 終了済みか（登録が無ければ false）。
        /// </summary>
        public bool IsFinished(TimerTicket ticket) =>
                 TryGetTimer(ticket, out var t) && t.IsFinished;

        /// <summary>
        /// 動作中か（登録が無ければ false）。
        /// </summary>
        public bool IsRunning(TimerTicket ticket) =>
            TryGetTimer(ticket, out var t) && t.IsRunning;

        /// <summary>
        /// 現在の時間を取得。
        /// </summary>
        public bool TryGetCurrentTime(TimerTicket ticket, out float current)
        {
            if (TryGetTimer(ticket, out var t))
            {
                current = t.Current;
                return true;
            }
            current = 0f;
            return false;
        }

        /// <summary>
        /// 経過正規化 [0..1] を取得（未登録及びカウントアップなど正規化不可能なタイマーは 1 として返す）。
        /// </summary>
        public bool TryGetNormalizedElapsed(TimerTicket ticket, out float elapsed)
        {
            bool res = TryGetTimer(ticket, out var t);
            elapsed = res ? t.NormalizedElapsed : 1f;
            return res;
        }
        public IEnumerable<TimerSnapshot> GetSnapshot()
        {
            KeyValuePair<TimerTicket, ITimer>[] local;
            lock (_lock)
            {
                local = _timers.ToArray();
            }

            foreach (var kv in local)
            {
                var key = kv.Key;
                var t = kv.Value;

                float op = -1;
                if (t.CountType.Has(CountType.Pulse))
                    op = ((PulseTimer)t).PulseCount;
                if (t.CountType.Has(CountType.UpAndDown))
                    op = ((UpAndDownTimer)t).IsReversing ? 1 : 0;

                yield return new TimerSnapshot(ParentName, key, t, op);
            }
        }
        internal bool TryGetTimer(TimerTicket ticket, out ITimer timer)
        {
            bool found = false;
            lock (_lock)
            {
                found = _timers.TryGetValue(ticket, out timer);
            }
            return found;
        }

        #endregion

        /// <summary>
        /// 更新処理。
        /// </summary>
        public void Update(float deltaTime)
        {
            _commandQueue.Update();
            if (deltaTime <= 0f) return;

            KeyValuePair<TimerTicket, ITimer>[] local;
            lock (_lock)
            {
                local = _timers.ToArray(); // (ticket, ITimer) をコピー
            }

            try
            {
                foreach (var kv in local)
                {
                    var t = kv.Value;
                    if (t.IsRunning)
                        t.Update(deltaTime);
                }
            }
            catch (Exception ex)
            {
                SendError(ex);
            }
        }


        #region タイマーへの操作
        public ITimerEvt GetTimerEvt(TimerTicket ticket)
        {
            if (TryGetTimer(ticket, out var t))
            {
                return t;
            }
            return null;
        }
        /// <summary>
        /// 完了時の Action を追加。
        /// </summary>
        public IDisposable AddCompleteAction(TimerTicket ticket, Action action)
        {
            lock (_lock)
            {
                if (action != null && _timers.TryGetValue(ticket, out var t))
                {
                    t.OnFinished += action;
                    var dis = Disposable.Create(() =>
                    {
                        RemoveCompleteAction(ticket, action);
                    });
                    return dis;
                }
                return null;
            }
        }

        /// <summary>
        /// 完了時の Action を削除。
        /// </summary>
        public void RemoveCompleteAction(TimerTicket ticket, Action action)
        {
            lock (_lock)
            {
                if (action != null && _timers.TryGetValue(ticket, out var t))
                {
                    t.OnFinished -= action;
                    return;
                }
            }
        }

        /// <summary>
        /// タイマーの初期値を変更。存在しなければ無視。<br/>
        /// パルスタイマーはコールバック呼び出し頻度にかかわる(他のタイマーと扱いが異なる)ため注意
        /// </summary>
        public void ChangeInitialTime(TimerTicket ticket, float newInitial)
        {
            lock (_lock)
            {
                if (newInitial < 0f)
                {
                    SendError(new ArgumentException("ChangeDuration: newDuration は 0 以上である必要があります。"));
                    return;
                }
                if (_timers.TryGetValue(ticket, out var t))
                {
                    t.InitialTime = newInitial;
                }
            }
        }
        #endregion

        #region エラーハンドリング
        public void OnErrorAction(Action<System.Exception> onError)
        {
            _onError += onError;
        }

        internal void SendError(Exception ex)
        {
            if (_onError != null)
                _onError.Invoke(ex);
            else
                ExceptionDispatchInfo.Capture(ex).Throw();
        }
        #endregion

        #region Disposable
        public void Dispose()
        {
            if (_disposed) return;
            lock (_lock)
            {
                _disposed = true;
                foreach (var t in _timers.Values)
                {
                    t.Dispose();
                }
                _timers.Clear();
                _commandQueue.Dispose();
                _readOnlytimers.Remove(_readonlyTimer);
                _onError = null;
            }
        }

        public static void DisposeAll()
        {
            for (int i = _readOnlytimers.Count - 1; i >= 0; --i)
            {
                _readOnlytimers[i].Dispose();
            }
        }
        #endregion

    }
}
