using HighElixir.Implements;
using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;

namespace HighElixir.Timers
{
    /// <summary>
    /// KEY 付きクールダウン/タイマー管理。
    /// </summary>
    [Serializable]
    public sealed partial class Timer : IReadOnlyTimer, IDisposable
    {
        [Flags]
        private enum LazyCommand
        {
            None = 0,
            Start = 1 << 0,
            Stop = 1 << 1,
            Reset = 1 << 2,
            Init = 1 << 3,
            Restart = Reset | Start,
        }
        // スナップショット管理用
        private static readonly List<IReadOnlyTimer> _readOnlytimers = new();

        private readonly string _parentName;
        private readonly IReadOnlyTimer _readonlyTimer;
        private readonly Dictionary<TimerTicket, ITimer> _timers = new();
        private readonly Queue<(TimerTicket key, LazyCommand command)> _commands = new();
        private readonly object _lock = new object();
        private Action<Exception> _onError;
        public string ParentName
        {
            get
            {
                if (string.IsNullOrEmpty(_parentName))
                {
                    return Guid.NewGuid().ToString();
                }
                return _parentName;
            }
        }
        public static IReadOnlyList<IReadOnlyTimer> AllTimers => _readOnlytimers.AsReadOnly();

        public int CommandCount { get; private set; }

        public Timer(string parentName = null)
        {
            _parentName = parentName ?? nameof(UnkouwnType.Instance);
            _readonlyTimer = new ReadOnlyTimer(this);
            _readOnlytimers.Add(_readonlyTimer);
        }

        /// <summary>
        /// タイマーの初期値を変更。存在しなければ無視。
        /// <br />パルスタイマーだけコールバック呼び出し頻度にかかわるため注意
        /// </summary>
        public void ChangeDuration(TimerTicket ticket, float newDuration)
        {
            if (newDuration < 0f) return;
            if (_timers.TryGetValue(ticket, out var t))
            {
                t.InitialTime = newDuration;
                if (t.InitialTime > newDuration)
                    t.InitialTime = newDuration;
            }
        }

        /// <summary>
        /// 初期値へリセット。カウント完了などのイベントから呼び出す場合は遅延実行を推奨。
        /// </summary>
        public bool Reset(TimerTicket ticket, bool isLazy = false)
        {
            if (_timers.TryGetValue(ticket, out var t))
            {
                if (isLazy)
                {
                    _commands.Enqueue((ticket, LazyCommand.Reset));
                    return true;
                }
                t.Reset();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 進行開始。カウント完了などのイベントから呼び出す場合は遅延実行を推奨。
        /// </summary>
        public bool Start(TimerTicket ticket, bool init = true, bool isLazy = false)
        {
            if (_timers.TryGetValue(ticket, out var t))
            {
                if (isLazy)
                {
                    var command = LazyCommand.Start | (init ? LazyCommand.Init : 0);
                    _commands.Enqueue((ticket, command));
                    return true;
                }
                if (init) t.Initialize();
                t.Start();
                return true;
            }
            return false;
        }

        public bool Restart(TimerTicket ticket, bool isLazy = true)
        {
            if (!_timers.TryGetValue(ticket, out var t)) return false;
            if (isLazy)
                _commands.Enqueue((ticket, LazyCommand.Restart));
            else
                t.Restart();
            return true;
        }
        /// <summary>
        /// 停止。カウント完了などのイベントから呼び出す場合は遅延実行を推奨。
        /// </summary>
        public bool Stop(TimerTicket ticket, bool init = false, bool isLazy = false)
            => Stop_Internal(ticket, out _, init, isLazy);

        public bool Stop(TimerTicket ticket, out float remaining, bool init = false)
            => Stop_Internal(ticket, out remaining, init);

        /// <summary>
        /// 完了時の Action を追加。
        /// </summary>
        public IDisposable AddAction(TimerTicket ticket, Action action)
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

        /// <summary>
        /// 完了時の Action を削除。
        /// </summary>
        public void RemoveAction(TimerTicket ticket, Action action)
        {
            if (action != null && _timers.TryGetValue(ticket, out var t))
            {
                t.OnFinished -= action;
                return;
            }
        }

        public IObservable<float> GetReactiveProperty(TimerTicket ticket)
        {
            if (_timers.TryGetValue(ticket, out var timer))
            {
                return timer.ElapsedReactiveProperty;
            }
            return null;
        }
        /// <summary>
        /// 更新処理
        /// </summary>
        public void Update(float deltaTime)
        {
            lock (_lock)
            {
                // 遅延コマンドの処理
                int count = 0;
                CommandCount = _commands.Count;
                while (_commands.Count > 0)
                {
                    var (timer, command) = _commands.Dequeue();
                    if ((command & LazyCommand.Init) != 0)
                        InitializeTimer(timer);
                    if ((command & LazyCommand.Reset) != 0)
                        Reset(timer, isLazy: false);
                    if ((command & LazyCommand.Start) != 0)
                        Start(timer, init: false, isLazy: false);
                    if ((command & LazyCommand.Stop) != 0)
                        Stop(timer, init: false, isLazy: false);
                    if (++count > 1000) break; // 無限ループ防止
                }
                if (deltaTime <= 0f) return;

                // 変更に強いようにキーのスナップショットで回す
                try
                {
                    var keys = _timers.Keys;
                    foreach (var key in keys)
                    {
                        if (_timers.TryGetValue(key, out var t))
                        {
                            if (t.IsRunning)
                                t.Update(deltaTime);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        // エラーハンドリング
        public void OnErrorAction(Action<System.Exception> onError)
        {
            _onError += onError;
        }

        internal void OnError(Exception ex)
        {
            if (_onError != null)
                _onError.Invoke(ex);
            else
                throw ex;
        }
        // Disposable
        public void Dispose()
        {
            foreach (var t in _timers.Values)
            {
                t.Dispose();
            }
            _timers.Clear();
            _commands.Clear();
            _readOnlytimers.Remove(this);
            _onError = null;
        }

        public void DisposeAll()
        {
            for (int i = _readOnlytimers.Count - 1; i >= 0; --i)
            {
                _readOnlytimers[i].Dispose();
            }
        }
        // private
        private void InitializeTimer(TimerTicket ticket)
        {
            if (_timers.TryGetValue(ticket, out var t))
            {
                t.Initialize();
            }
        }
        private bool Stop_Internal(TimerTicket ticket, out float remaining, bool init = false, bool isLazy = false)
        {
            remaining = 0;
            if (!_timers.TryGetValue(ticket, out var t)) return false;
            if (isLazy)
            {
                _commands.Enqueue((ticket, LazyCommand.Stop | (init ? LazyCommand.Init : LazyCommand.None)));
            }
            else
            {
                remaining = t.Stop();
                if (init) t.Initialize();
            }
            return true;
        }
    }
}
