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
    /// TimerTicket をキーに複数のタイマーを管理する中心クラス。
    /// ・登録／解除
    /// ・状態取得
    /// ・Update による時間進行
    /// ・スナップショット作成
    /// ・非同期イベントの発行
    /// をまとめて担当する。
    /// </summary>
    public sealed class Timer : IReadOnlyTimer, IDisposable
    {
        private string _parentName;

        // TimerTicket → ITimer の実体
        private readonly Dictionary<TimerTicket, ITimer> _timers = new();
        private readonly Dictionary<TimerTicket, TimerMeta> _meta = new();
        private readonly List<(TimerTicket ticket, ITimer timer)> _timersCache = new();
        private bool _isDirty = true;

        // エラーハンドラ
        private Action<Exception> _onError;
        private bool _disposed;

        // 外部同期ロック
        internal readonly object _lock = new();

        // 遅延操作キュー（Start/Stop/Reset を後で処理）
        private readonly CommandQueue _commandQueue;

        // 単一時間ストリーム（複数周期イベントの基準）
        private readonly Dictionary<StreamTicket, StreamEntry> _streamSchedules = new();
        private readonly List<StreamDispatch> _streamDispatchBuffer = new();
        private float _streamNow = 0f;
        private const float StreamEpsilon = 0.0001f;

        public string ParentName
        {
            get => _parentName;
            set => _parentName = string.IsNullOrEmpty(value) ? UnknownType.Name : value;
        }

        public event Action<Exception> OnError { add => _onError += value; remove => _onError -= value; }

        // CommandQueue がいくつ溜まっているか（監視用）
        public int CommandCount => _commandQueue.CommandCount;

        /// <summary>
        /// 単一時間ストリームの現在時刻（秒）。
        /// </summary>
        public float StreamNow => _streamNow;

        public Timer(string parentName = null)
        {
            _parentName = parentName ?? UnknownType.Name;

            // 遅延操作用キュー
            _commandQueue = new CommandQueue(1000, this);

#if UNITY_EDITOR
            TimerManager.Register(this);
#endif
        }

        #region 基本操作
        /// <summary>
        /// 即時送信。TimerTicket の元にある ITimer に対して操作を実行。
        /// </summary>
        public float Send(TimerTicket ticket, TimeOperation operation)
        {
            if (TryGetTimer(ticket, out var timer))
            {
                return timer.Operation(operation);
            }
            return -1;
        }

        /// <summary>
        /// 遅延実行（次の Update で処理）
        /// </summary>
        public void LazySend(TimerTicket ticket, TimeOperation operation)
            => _commandQueue.Enqueue(ticket, operation);
        #endregion

        #region TimeStream
        /// <summary>
        /// Stream に周期コールバックを登録する。
        /// </summary>
        public StreamTicket StreamEvery(float intervalSeconds, Action callback, string name = null, StreamScheduleOptions options = default)
        {
            if (callback == null)
            {
                SendError(new ArgumentNullException(nameof(callback)));
                return default;
            }

            if (intervalSeconds <= 0f)
            {
                SendError(new ArgumentOutOfRangeException(nameof(intervalSeconds), "intervalSeconds は 0 より大きい必要があります。"));
                return default;
            }

            lock (_lock)
            {
                var ticket = StreamTicket.Take(name);
                var start = options.RunImmediately ? _streamNow : _streamNow + intervalSeconds;
                _streamSchedules[ticket] = new StreamEntry(intervalSeconds, start, callback, options);
                return ticket;
            }
        }

        public bool StreamContains(StreamTicket ticket)
        {
            lock (_lock)
            {
                return _streamSchedules.ContainsKey(ticket);
            }
        }

        public bool StreamUnregister(StreamTicket ticket)
        {
            lock (_lock)
            {
                return _streamSchedules.Remove(ticket);
            }
        }

        public bool StreamPause(StreamTicket ticket, bool pause = true)
        {
            lock (_lock)
            {
                if (_streamSchedules.TryGetValue(ticket, out var entry))
                {
                    entry.Paused = pause;
                    return true;
                }
            }
            return false;
        }

        public bool StreamResume(StreamTicket ticket) => StreamPause(ticket, false);

        public int StreamScheduleCount
        {
            get
            {
                lock (_lock)
                {
                    return _streamSchedules.Count;
                }
            }
        }

        public void StreamClear()
        {
            lock (_lock)
            {
                _streamSchedules.Clear();
                _streamDispatchBuffer.Clear();
            }
        }

        /// <summary>
        /// Stream時刻をリセットし、必要に応じて各スケジュールを再基準化する。
        /// </summary>
        public void StreamReset(float now = 0f, bool restartSchedules = true)
        {
            lock (_lock)
            {
                _streamNow = Math.Max(0f, now);
                if (!restartSchedules)
                    return;

                foreach (var entry in _streamSchedules.Values)
                {
                    entry.NextDue = entry.Options.RunImmediately
                        ? _streamNow
                        : _streamNow + entry.IntervalSeconds;
                }
            }
        }
        #endregion

        #region 登録処理
        /// <summary>
        /// 生成済み ITimer を登録。
        /// </summary>
        public void Register(TimerTicket ticket, ITimer timer)
        {
            _isDirty = true;
            _timers[ticket] = timer;
        }

        internal void SetMetadata(TimerTicket ticket, in TimerMeta meta)
        {
            lock (_lock)
            {
                if (!meta.HasAny)
                {
                    _meta.Remove(ticket);
                    return;
                }

                _meta[ticket] = meta;
            }
        }

        internal bool TryGetMetadata(TimerTicket ticket, out TimerMeta meta)
        {
            lock (_lock)
            {
                return _meta.TryGetValue(ticket, out meta);
            }
        }

        internal TimerTicket[] GetTicketsByGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return Array.Empty<TimerTicket>();

            lock (_lock)
            {
                return _meta
                    .Where(x => string.Equals(x.Value.Group, group, StringComparison.Ordinal))
                    .Select(x => x.Key)
                    .ToArray();
            }
        }

        internal TimerTicket[] GetTicketsByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return Array.Empty<TimerTicket>();

            lock (_lock)
            {
                return _meta
                    .Where(x => string.Equals(x.Value.Tag, tag, StringComparison.Ordinal))
                    .Select(x => x.Key)
                    .ToArray();
            }
        }

        /// <summary>
        /// T: ITimer を生成して登録する（Factory 経由）。
        /// ReactiveTimerEvent を返すのでイベント購読もここで可能。
        /// </summary>
        /// <returns><see cref="TimerEventRegistry"/>をもとにしたイベントIDを通知するオブザーバブル</returns>
        public IObservable<int> Register<T>(string name, float initTime, out TimerTicket ticket, float arg = 1, bool andStart = false)
            where T : ITimer
        {
            lock (_lock)
            {
                _isDirty = true;
                var timer = this.Create(typeof(T), initTime, arg);
                ticket = TimerTicket.Take(name, this);

                if (andStart)
                    timer.Start();

                _timers[ticket] = timer;

                // イベントストリームを返す
                return timer.ReactiveTimerEvent;
            }
        }

        /// <summary>
        /// タイマー登録解除。存在しない場合は false。
        /// </summary>
        public bool UnRegister(TimerTicket ticket)
        {
            lock (_lock)
            {
                if (_timers.TryGetValue(ticket, out var timer))
                {
                    _isDirty = true;
                    if (timer is TimerBase timerBase)
                        timerBase.NotifyRemoved();
                    timer.Dispose();      // イベント破棄
                    _timers.Remove(ticket);
                    _meta.Remove(ticket);
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region 情報取得
        /// <summary>
        /// 経過時間などの TimeData を reactive に得る
        /// </summary>
        public IObservable<TimeData> GetReactiveProperty(TimerTicket ticket)
        {
            if (TryGetTimer(ticket, out var t))
                return t.TimeReactive;

            return new Empty<TimeData>();
        }

        /// <summary>
        /// タイマーが存在するか？
        /// </summary>
        public bool Contains(TimerTicket ticket)
        {
            lock (_lock)
            {
                return _timers.ContainsKey(ticket);
            }
        }

        /// <summary>終了済みか？</summary>
        public bool IsFinished(TimerTicket ticket)
            => TryGetTimer(ticket, out var t) && t.IsFinished;

        /// <summary>動作中か？</summary>
        public bool IsRunning(TimerTicket ticket)
            => TryGetTimer(ticket, out var t) && t.IsRunning;

        /// <summary>
        /// 現在値を取得する
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
        /// 登録中のタイマー状態をスナップショットとして列挙。
        /// 保存・復元用。
        /// </summary>
        public IEnumerable<TimerSnapshot> GetSnapshot()
        {
            KeyValuePair<TimerTicket, ITimer>[] local;
            lock (_lock)
            {
                // Dictionary をコピーしてスレッド安全に操作
                local = _timers.ToArray();
            }

            foreach (var kv in local)
            {
                var key = kv.Key;
                var t = kv.Value;
                yield return new TimerSnapshot(ParentName, key, t);
            }
        }

        /// <summary>
        /// ticket から ITimer を取得。内部は lock 保護。
        /// </summary>
        public bool TryGetTimer(TimerTicket ticket, out ITimer timer)
        {
            bool found;
            lock (_lock)
            {
                found = _timers.TryGetValue(ticket, out timer);
            }
            return found;
        }

        internal IEnumerable<(TimerTicket ticket, ITimer)> GetTimers()
        {
            if (_isDirty)
            {
                _isDirty = false;
                _timersCache.Clear();
                foreach(var item in _timers)
                {
                    _timersCache.Add((item.Key, item.Value));
                }
            }
            return _timersCache;
        }
        #endregion

        /// <summary>
        /// Update によりタイマーの時間進行を行う。
        /// ① 遅延コマンドキューの処理
        /// ② deltaTime > 0 の場合に実タイマーの Update
        /// </summary>
        public void Update(float deltaTime)
        {
            // LazySend（Queue 操作）を処理
            _commandQueue.Update();

            if (deltaTime <= 0f) return;

            UpdateStream(deltaTime);

            // 安全のためコピー
            KeyValuePair<TimerTicket, ITimer>[] local;
            lock (_lock)
            {
                local = _timers.ToArray();
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

        private void UpdateStream(float deltaTime)
        {
            StreamDispatch[] dispatches;
            lock (_lock)
            {
                _streamNow += deltaTime;
                _streamDispatchBuffer.Clear();

                foreach (var pair in _streamSchedules)
                {
                    var entry = pair.Value;
                    if (entry.Paused || entry.Callback == null) continue;
                    if (_streamNow + StreamEpsilon < entry.NextDue) continue;

                    int dispatchCount = CalculateDispatchCount(entry, _streamNow);
                    if (dispatchCount <= 0) continue;

                    _streamDispatchBuffer.Add(new StreamDispatch(entry.Callback, dispatchCount));
                }

                dispatches = _streamDispatchBuffer.ToArray();
            }

            for (int i = 0; i < dispatches.Length; i++)
            {
                var dispatch = dispatches[i];
                for (int c = 0; c < dispatch.Count; c++)
                {
                    try
                    {
                        dispatch.Callback.Invoke();
                    }
                    catch (Exception ex)
                    {
                        SendError(ex);
                        break;
                    }
                }
            }
        }

        private static int CalculateDispatchCount(StreamEntry entry, float now)
        {
            if (now + StreamEpsilon < entry.NextDue)
                return 0;

            switch (entry.Options.CatchUpMode)
            {
                case StreamCatchUpMode.CatchUpAll:
                    return AdvanceAndCountCatchUpAll(entry, now);

                case StreamCatchUpMode.CatchUpMax:
                    return AdvanceAndCountCatchUpMax(entry, now);

                case StreamCatchUpMode.Skip:
                default:
                    AdvanceToAfterNow(entry, now);
                    return 1;
            }
        }

        private static int AdvanceAndCountCatchUpAll(StreamEntry entry, float now)
        {
            int count = 0;
            while (now + StreamEpsilon >= entry.NextDue)
            {
                entry.NextDue += entry.IntervalSeconds;
                count++;

                // 異常設定への安全弁
                if (count >= 10000) break;
            }
            return count;
        }

        private static int AdvanceAndCountCatchUpMax(StreamEntry entry, float now)
        {
            int max = entry.Options.ResolveMaxCatchUp();
            int count = 0;

            while (count < max && now + StreamEpsilon >= entry.NextDue)
            {
                entry.NextDue += entry.IntervalSeconds;
                count++;
            }

            // 上限超過分は捨てて、次の周期へ進める
            if (now + StreamEpsilon >= entry.NextDue)
                AdvanceToAfterNow(entry, now);

            return count;
        }

        private static void AdvanceToAfterNow(StreamEntry entry, float now)
        {
            if (entry.IntervalSeconds <= 0f)
                return;

            if (now + StreamEpsilon < entry.NextDue)
                return;

            var delta = now - entry.NextDue;
            int step = (int)Math.Floor(delta / entry.IntervalSeconds) + 1;
            entry.NextDue += step * entry.IntervalSeconds;
        }

        #region タイマーへの操作
        /// <summary>
        /// TimerEvent（Start/Stop/Reset/Finished など）を購読
        /// </summary>
        public IObservable<int> GetTimerEvt(TimerTicket ticket)
        {
            if (TryGetTimer(ticket, out var t))
                return t.ReactiveTimerEvent;

            return Empty<int>.Instance;
        }

        /// <summary>
        /// TimerEvent（Start/Stop/Reset/Finished など）を型付きで購読
        /// </summary>
        public IObservable<TimeEventType> GetTimerEvtType(TimerTicket ticket)
        {
            if (TryGetTimer(ticket, out var t))
                return t.ReactiveTimerEvent.Convert(TimerEventRegistry.ToType);

            return Empty<TimeEventType>.Instance;
        }

        /// <summary>
        /// タイマーの初期値（InitialTime）を変更する。
        /// パルスタイマーの場合は振る舞いに影響するので注意。
        /// </summary>
        public void ChangeInitialTime(TimerTicket ticket, float newInitial)
        {
            lock (_lock)
            {
                if (newInitial < 0f)
                {
                    SendError(new ArgumentException(
                        "ChangeDuration: newDuration は 0 以上である必要があります。"));
                    return;
                }

                if (_timers.TryGetValue(ticket, out var t))
                    t.InitialTime = newInitial;
            }
        }
        #endregion

        #region エラーハンドリング
        public void OnErrorAction(Action<Exception> onError)
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

                // 各タイマー破棄
                foreach (var t in _timers.Values)
                {
                    if (t is TimerBase timerBase)
                        timerBase.NotifyRemoved();
                    t.Dispose();
                }
                _timers.Clear();
                _meta.Clear();
                _streamSchedules.Clear();
                _streamDispatchBuffer.Clear();

                _commandQueue.Dispose();
                _onError = null;
#if UNITY_EDITOR
                TimerManager.Unregister(this);
#endif
            }
        }

        private sealed class StreamEntry
        {
            public readonly float IntervalSeconds;
            public float NextDue;
            public readonly Action Callback;
            public readonly StreamScheduleOptions Options;
            public bool Paused;

            public StreamEntry(float intervalSeconds, float nextDue, Action callback, StreamScheduleOptions options)
            {
                IntervalSeconds = intervalSeconds;
                NextDue = nextDue;
                Callback = callback;
                Options = options;
                Paused = false;
            }
        }

        private readonly struct StreamDispatch
        {
            public readonly Action Callback;
            public readonly int Count;

            public StreamDispatch(Action callback, int count)
            {
                Callback = callback;
                Count = count;
            }
        }
        #endregion
    }
}
