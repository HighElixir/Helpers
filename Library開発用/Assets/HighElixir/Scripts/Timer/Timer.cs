using HighElixir.Implements;
using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using static HighElixir.Timers.Internal.CommandQueue;

// Timers.cs
namespace HighElixir.Timers
{
    /// <summary>
    /// KEY 付きクールダウン/タイマー管理。
    /// </summary>
    public sealed class Timer : IReadOnlyTimer, IDisposable
    {
        private readonly string _parentName;
        private readonly Dictionary<TimerTicket, ITimer> _timers = new();

        // 管理
        private Action<Exception> _onError;
        private bool _disposed;

        // 外部
        private readonly CommandQueue _commandQueue;
        private readonly TimerFactory _timerFactory;
        private readonly object _lock = new object();
        private readonly IReadOnlyTimer _readonlyTimer;

        // スナップショット管理用
        private static readonly List<IReadOnlyTimer> _readOnlytimers = new();

        public string ParentName
        {
            get
            {
                if (string.IsNullOrEmpty(_parentName))
                {
                    return UnknownType.Name;
                }
                return _parentName;
            }
        }
        public static IReadOnlyList<IReadOnlyTimer> AllTimers => _readOnlytimers.AsReadOnly();

        // TimerWatcherから監視する用の変数
        public int CommandCount => _commandQueue.CommandCount;

        public Timer(string parentName = null)
        {
            _parentName = parentName ?? UnknownType.Name;
            _readonlyTimer = new ReadOnlyTimer(this);
            _timerFactory = new TimerFactory(this);
            _commandQueue = new CommandQueue(1000, this);
            _readOnlytimers.Add(_readonlyTimer);
        }

        #region 基本操作
        /// <summary>
        /// 進行開始。イベントや非同期から呼び出す場合は遅延実行を推奨。<br/>
        /// 遅延実行の場合、コマンドが最大数未満の時にtrue
        /// </summary>
        public bool Start(TimerTicket ticket, bool init = true, bool isLazy = false)
        {
            if (!GetTimerSafety(ticket, out var t)) return false;
            if (isLazy)
            {
                var command = LazyCommand.Start | (init ? LazyCommand.Init : 0);
                return _commandQueue.Enqueue(ticket, command);
            }
            if (init) t.Initialize();
            t.Start();
            return true;
        }

        /// <summary>
        /// 再スタート。イベントや非同期から呼び出す場合は遅延実行を推奨。<br/>
        /// 遅延実行の場合、コマンドが最大数未満の時にtrue
        /// </summary>
        public bool Restart(TimerTicket ticket, bool isLazy = true)
        {
            if (!GetTimerSafety(ticket, out var t)) return false;
            if (isLazy)
            {
                return _commandQueue.Enqueue(ticket, LazyCommand.Restart);
            }
            else
            {
                t.Restart();
                BumpGenerationAndCancelWaiters(ticket);
            }
            return true;
        }

        /// <summary>
        /// 停止。イベントや非同期から呼び出す場合は遅延実行を推奨。<br/>
        /// 遅延実行の場合、コマンドが最大数未満の時にtrue
        /// </summary>
        public bool Stop(TimerTicket ticket, bool init = false, bool isLazy = false)
            => Stop_Internal(ticket, out _, init, isLazy);

        /// <summary>
        /// 停止。イベントや非同期から呼び出す場合は遅延実行を推奨。<br/>
        /// 遅延実行の場合、コマンドが最大数未満の時にtrue
        /// </summary>
        public bool Stop(TimerTicket ticket, out float remaining, bool init = false)
            => Stop_Internal(ticket, out remaining, init);

        private bool Stop_Internal(TimerTicket ticket, out float remaining, bool init = false, bool isLazy = false)
        {
            remaining = 0;
            if (!GetTimerSafety(ticket, out var t)) return false;
            if (isLazy)
            {
                return _commandQueue.Enqueue(ticket, LazyCommand.Stop | (init ? LazyCommand.Init : LazyCommand.None));
            }
            else
            {
                remaining = t.Stop();
                if (init) t.Initialize();
            }
            return true;
        }

        /// <summary>
        /// リセット。イベントや非同期から呼び出す場合は遅延実行を推奨。<br/>
        /// 遅延実行の場合、コマンドが最大数未満の時にtrue <br/>
        /// 対象がカウントアップの場合、完了イベントが呼ばれる。
        /// </summary>
        public bool Reset(TimerTicket ticket, bool isLazy = false)
        {
            if (!GetTimerSafety(ticket, out var t)) return false;
            if (isLazy)
            {
                _commandQueue.Enqueue(ticket, LazyCommand.Reset);
                return true;
            }
            t.Reset();
            BumpGenerationAndCancelWaiters(ticket);
            return true;
        }

        /// <summary>
        /// リセット。イベントや非同期から呼び出す場合は遅延実行を推奨。<br/>
        /// 遅延実行の場合、コマンドが最大数未満の時にtrue
        /// </summary>
        public bool Initialize(TimerTicket ticket, bool isLazy = false)
        {
            if (!GetTimerSafety(ticket, out var t)) return false;
            if (isLazy)
            {
                _commandQueue.Enqueue(ticket, LazyCommand.Init);
                return true;
            }
            t.Initialize();
            BumpGenerationAndCancelWaiters(ticket);
            return true;
        }
        #endregion

        #region 登録処理
        /// <summary>
        /// カウントダウンタイマーの登録
        /// </summary>
        public TimerTicket CountDownRegister(float duration, string name = "", Action onFinished = null, bool isTick = false, bool initZero = false, bool andStart = false)
        {
            lock (_lock)
            {
                if (duration < 0f)
                {
                    OnError(new ArgumentException("CountDownRegister: duration は 0 以上である必要があります。"));
                    return default;
                }
                var res = Register_Internal(CountType.CountDown, name, duration, isTick, onFinished, andStart);
                GetTimerSafety(res, out var t);
                if (initZero)
                    t.Current = 0f;
                return res;
            }
        }
        /// <summary>
        /// カウントアップタイマーの登録
        /// </summary>
        public TimerTicket CountUpRegister(string name = "", Action onReseted = null, bool isTick = false, bool andStart = false)
        {
            lock (_lock)
            {
                return Register_Internal(CountType.CountUp, name, 1, isTick, onReseted, andStart);
            }
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマーの登録。
        /// </summary>
        public TimerTicket PulseRegister(float pulseInterval, string name = "", Action onPulse = null, bool isTick = false, bool andStart = false)
        {
            lock (_lock)
            {
                if (pulseInterval < 0f)
                {
                    OnError(new ArgumentException("PulseRegister: pulseInterval は 0 以上である必要があります。"));
                    return default;
                }
                return Register_Internal(CountType.Pulse, name, pulseInterval, isTick, onPulse, andStart);
            }
        }

        public TimerTicket Restore(TimerSnapshot snapshot, bool andStart = false)
        {
            lock (_lock)
            {
                var ticket = Register_Internal(snapshot.CountType, snapshot.Name, snapshot.Initialize, snapshot.CountType.Has(CountType.Tick), null, andStart);
                _timers[ticket].Current = snapshot.Current;
                return ticket;
            }
        }

        private TimerTicket Register_Internal(CountType type, string name, float initTime, bool isTick, Action action = null, bool andStart = false)
        {
            lock (_lock)
            {
                if (isTick) type |= CountType.Tick;
                var timer = _timerFactory.Create(type, initTime, action);
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
                    if (_awaits.TryGetValue(ticket, out var st))
                    {
                        var waiters = st.FinishWaiters.ToArray();
                        st.FinishWaiters.Clear();
                        _awaits.Remove(ticket);
                        // ロック外でキャンセル通知
                        Task.Run(() => {
                            foreach (var tcs in waiters) tcs.TrySetCanceled();
                        });
                    }
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region 情報取得
        public IObservable<TimeData> GetReactiveProperty(TimerTicket ticket)
        {
            if (GetTimerSafety(ticket, out var t))
            {
                return t.TimeReactive;
            }
            return null;
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
                 GetTimerSafety(ticket, out var t) && t.IsFinished;

        /// <summary>
        /// 終了済みか（登録が無ければ false）。
        /// </summary>
        public bool IsRunning(TimerTicket ticket) =>
            GetTimerSafety(ticket, out var t) && t.IsRunning;

        /// <summary>
        /// 現在の時間を取得。
        /// </summary>
        public bool TryGetCurrentTime(TimerTicket ticket, out float current)
        {
            if (GetTimerSafety(ticket, out var t))
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
        public bool GetNormalizedElapsed(TimerTicket ticket, out float elapsed)
        {
            bool res = GetTimerSafety(ticket, out var t);
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

                yield return new TimerSnapshot(ParentName, key, t, op);
            }
        }

        #endregion

        #region Timerごとの独自操作

        #region UpAndDown操作
        public void ReverseDirection(TimerTicket ticket)
        {
            if (GetTimerSafety(ticket, out var t) && t is IUpAndDown ud)
            {
                ud.ReverseDirection();
            }
        }
        public void SetDirection(TimerTicket ticket, bool isUp)
        {
            if (GetTimerSafety(ticket, out var t) && t is IUpAndDown ud)
            {
                ud.SetDirection(isUp);
            }
        }
        public void ReverseAndStart(TimerTicket ticket)
        {
            if (GetTimerSafety(ticket, out var t) && t is IUpAndDown ud)
            {
                ud.ReverseDirection();
                t.Start();
            }
        }
        public void SetDirectionAndStart(TimerTicket ticket, bool isUp)
        {
            if (GetTimerSafety(ticket, out var t) && t is IUpAndDown ud)
            {
                ud.SetDirection(isUp);
                t.Start();
            }
        }
        #endregion

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
                OnError(ex);
            }
        }


        #region タイマーへの操作
        /// <summary>
        /// 完了時の Action を追加。
        /// </summary>
        public IDisposable AddAction(TimerTicket ticket, Action action)
        {
            lock (_lock)
            {
                if (action != null && _timers.TryGetValue(ticket, out var t))
                {
                    t.OnFinished += action;
                    var dis = Disposable.Create(() =>
                    {
                        RemoveAction(ticket, action);
                    });
                    return dis;
                }
                return null;
            }
        }

        /// <summary>
        /// 完了時の Action を削除。
        /// </summary>
        public void RemoveAction(TimerTicket ticket, Action action)
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
        public void ChangeDuration(TimerTicket ticket, float newDuration)
        {
            lock (_lock)
            {
                if (newDuration < 0f)
                {
                    OnError(new ArgumentException("ChangeDuration: newDuration は 0 以上である必要があります。"));
                    return;
                }
                if (_timers.TryGetValue(ticket, out var t))
                {
                    t.InitialTime = newDuration;
                }
            }
        }
        #endregion

        #region エラーハンドリング
        public void OnErrorAction(Action<System.Exception> onError)
        {
            _onError += onError;
        }

        internal void OnError(Exception ex)
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
                _awaits.Clear();
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

        #region 非同期処理

        // チケットごとの待機状態
        private sealed class AwaitState
        {
            public int Version; // Restart/Resetで++する
            public List<TaskCompletionSource<bool>> FinishWaiters = new();
        }
        private readonly Dictionary<TimerTicket, AwaitState> _awaits = new();

        public Task WaitUntilFinishedAsync(TimerTicket ticket, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (!_timers.TryGetValue(ticket, out var t))
                    return Task.FromException(new InvalidOperationException("Timer not found."));

                if (t.IsFinished)
                    return Task.CompletedTask;

                if (!_awaits.TryGetValue(ticket, out var st))
                {
                    st = new AwaitState();
                    _awaits[ticket] = st;
                }

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                // キャンセル対応
                if (ct.CanBeCanceled)
                {
                    var reg = ct.Register(() =>
                    {
                        lock (_lock)
                        {
                            tcs.TrySetCanceled(ct);
                            st.FinishWaiters.Remove(tcs);
                        }
                    });
                    // Task完了時に登録解除
                    tcs.Task.ContinueWith(_ => reg.Dispose(), TaskScheduler.Default);
                }

                st.FinishWaiters.Add(tcs);
                return tcs.Task;
            }
        }

        // --- 完了トリガ ---
        internal void NotifyFinished(TimerTicket ticket)
        {
            List<TaskCompletionSource<bool>> waiters = null;
            lock (_lock)
            {
                if (_awaits.TryGetValue(ticket, out var st) && st.FinishWaiters.Count > 0)
                {
                    waiters = new List<TaskCompletionSource<bool>>(st.FinishWaiters);
                    st.FinishWaiters.Clear();
                }
            }
            if (waiters != null)
            {
                // ロック外で完了を返す
                foreach (var tcs in waiters) tcs.TrySetResult(true);
                _awaits.Remove(ticket);
            }
        }

        // Restart/Reset時は世代更新＆未完了待機者をキャンセル/完了扱いに
        private void BumpGenerationAndCancelWaiters(TimerTicket ticket)
        {
            List<TaskCompletionSource<bool>> waiters = null;
            lock (_lock)
            {
                if (_awaits.TryGetValue(ticket, out var st))
                {
                    waiters = new List<TaskCompletionSource<bool>>(st.FinishWaiters);
                    st.FinishWaiters.Clear();
                    st.Version++; // 将来、条件待機にも使うなら活きる
                }
            }
            if (waiters != null)
            {
                foreach (var tcs in waiters) tcs.TrySetCanceled();
            }
        }

        // 既存の操作にフック（例：Reset/Restart/Stop完了箇所）

        // ITimer 内の完了イベントから呼ぶ（CountDown終わり、CountUpがReset等）
        internal void OnTimerFinished(TimerTicket ticket)
        {
            NotifyFinished(ticket);
        }
        #endregion

        private bool GetTimerSafety(TimerTicket ticket, out ITimer timer)
        {
            bool found = false;
            lock (_lock)
            {
                found = _timers.TryGetValue(ticket, out timer);
            }
            return found;
        }
    }
}
