using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private static readonly List<IReadOnlyTimer> _timer = new();
        private readonly string _parentName;
        private readonly IReadOnlyTimer _readonlyTimer;
        private readonly Dictionary<TimerTicket, ITimer> _timers = new();
        private readonly Queue<(TimerTicket key, LazyCommand command)> _commands = new();

        public string ParentName => _parentName;
        public static IReadOnlyList<IReadOnlyTimer> AllTimers => _timer.AsReadOnly();

        public int CommandCount { get; private set; }

        public Timer(string parentName = null)
        {
            _parentName = parentName ?? nameof(UnkouwnType.Instance);
            _readonlyTimer = new ReadOnlyTimer(this);
            _timer.Add(_readonlyTimer);
        }

        /// <summary>
        /// タイマーが存在するか。
        /// </summary>
        public bool Contains(TimerTicket ticket) => _timers.ContainsKey(ticket);

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
        {
            if (!_timers.TryGetValue(ticket, out var t)) return false;
            if (isLazy)
            {
                _commands.Enqueue((ticket, LazyCommand.Stop | (init ? LazyCommand.Init : LazyCommand.None)));
            }
            else
            {
                t.Stop();
                if (init) t.Initialize();
            }
            return true;
        }

        /// <summary>
        /// 終了済みか（登録が無ければ false）。
        /// </summary>
        public bool IsFinished(TimerTicket ticket)
        {
            return _timers.TryGetValue(ticket, out var t) && t.CountType.Has(CountType.CountDown) && t.Current <= 0;
        }

        /// <summary>
        /// 現在の時間を取得。
        /// </summary>
        public bool TryGetRemaining(TimerTicket ticket, out float remaining)
        {
            if (_timers.TryGetValue(ticket, out var t))
            {
                remaining = t.Current;
                return true;
            }
            remaining = 0f;
            return false;
        }

        /// <summary>
        /// 経過正規化 [0..1] を取得（未登録及びカウントアップなど正規化不可能なタイマーは 1 として返す）。
        /// </summary>
        public bool TryGetNormalizedElapsed(TimerTicket ticket, out float elapsed)
        {
            bool res = _timers.TryGetValue(ticket, out var t);
            elapsed = res ? t.NormalizedElapsed : 1f;
            return res;
        }

        /// <summary>
        /// 完了時の Action を追加。存在しない場合 false。
        /// </summary>
        public bool TryAddAction(TimerTicket ticket, Action action)
        {
            if (action == null) return false;
            if (_timers.TryGetValue(ticket, out var t))
            {
                t.OnFinished += action;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 完了時の Action を削除。存在しない場合 false。
        /// </summary>
        public bool TryRemoveAction(TimerTicket ticket, Action action)
        {
            if (action == null) return false;
            if (_timers.TryGetValue(ticket, out var t))
            {
                t.OnFinished -= action;
                return true;
            }
            return false;
        }

        public bool TryGetIObservable(TimerTicket ticket, out IObservable<float> observable)
        {
            observable = null;
            if (_timers.TryGetValue(ticket, out var timer))
            {
                observable = timer.ElapsedReactiveProperty;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 更新処理
        /// </summary>
        public void Update(float deltaTime)
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
            var keys = _timers.Keys.ToList();
            foreach (var key in keys)
            {
                if (_timers.TryGetValue(key, out var t))
                {
                    if (t.IsRunning)
                        t.Update(deltaTime);
                }
            }
        }

        public void Dispose()
        {
            _timers.Clear();
            _commands.Clear();
            _timer.RemoveAll(t => t == _readonlyTimer);
        }

        private void InitializeTimer(TimerTicket ticket)
        {
            if (_timers.TryGetValue(ticket, out var t))
            {
                t.Initialize();
            }
        }
    }
}
