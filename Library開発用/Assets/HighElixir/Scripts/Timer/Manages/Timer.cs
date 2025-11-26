using HighElixir.Implements;
using HighElixir.Implements.Observables;
using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using UnityEditor;

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
        private List<(TimerTicket ticket, ITimer timer)> _timersCache = new();
        private bool _isDirty = true;

        // エラーハンドラ
        private Action<Exception> _onError;
        private bool _disposed;

        // 外部同期ロック
        internal readonly object _lock = new object();

        // 遅延操作キュー（Start/Stop/Reset を後で処理）
        private readonly CommandQueue _commandQueue;

        public string ParentName
        {
            get => _parentName;
            set => _parentName = string.IsNullOrEmpty(value) ? UnknownType.Name : value;
        }

        public event Action<Exception> OnError { add => _onError += value; remove => _onError -= value; }

        // CommandQueue がいくつ溜まっているか（監視用）
        public int CommandCount => _commandQueue.CommandCount;

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

        #region 登録処理
        /// <summary>
        /// 生成済み ITimer を登録。
        /// </summary>
        public void Register(TimerTicket ticket, ITimer timer)
        {
            _isDirty = true;
            _timers[ticket] = timer;
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
                ticket = TimerTicket.Take(name);

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
                    timer.Dispose();      // イベント破棄
                    _timers.Remove(ticket);
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

        #region タイマーへの操作
        /// <summary>
        /// TimerEvent（Start/Stop/Reset/Finished など）を購読
        /// </summary>
        public IObservable<int> GetTimerEvt(TimerTicket ticket)
        {
            if (TryGetTimer(ticket, out var t))
                return t.ReactiveTimerEvent;

            return null;
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
                    t.Dispose();
                _timers.Clear();

                _commandQueue.Dispose();
                _onError = null;
#if UNITY_EDITOR
                TimerManager.Unregister(this);
#endif
            }
        }
        #endregion
    }
}
