using HighElixir.Timers.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HighElixir.Timers
{
    /// <summary>
    /// ID 付きクールダウン/タイマー管理。
    /// </summary>
    [Serializable]
    public sealed class Timer : IReadOnlyTimer, IDisposable
    {
        [Flags]
        private enum LazyCommand
        {
            Start = 1 << 0,
            Stop = 1 << 1,
            Reset = 1 << 2,
            Init = 1 << 3,
        }
        private static readonly List<IReadOnlyTimer> _timer = new();
        private Type _parentType;
        private readonly IReadOnlyTimer _readonlyTimer;
        private readonly Dictionary<string, ITimer> _timers = new(StringComparer.Ordinal);
        private readonly Queue<(string id, LazyCommand command)> _commands = new();
        public Type ParentType => _parentType;
        public static IReadOnlyList<IReadOnlyTimer> AllTimers => _timer.AsReadOnly();

        public int CommandCount { get; private set; }

        public Timer(Type parentType = null)
        {
            _parentType = parentType ?? UnkouwnType.Instance;
            _readonlyTimer = new ReadOnlyTimer(this);
            _timer.Add(_readonlyTimer);
        }
        /// <summary>
        /// \カウントダウンタイマー。既に同じ id があれば false。
        /// </summary>
        public bool CountDownRegister(string id, float duration, Action onFinished = null, CountType type = CountType.Time)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id is null or empty", nameof(id));
            if (duration < 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            ITimer timer;
            if (type == CountType.Time)
                timer = new CountDownTimer(duration, onFinished);
            else
                timer = new TickCountDownTimer(duration, onFinished);
            timer.Initialize();
            return _timers.TryAdd(id, timer);
        }

        /// <summary>
        /// カウントアップタイマー。既に同じ id があれば false。
        /// </summary>
        public bool CountUpRegister(string id, Action onReseted = null, CountType type = CountType.Time)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id is null or empty", nameof(id));
            ITimer timer;
            if (type == CountType.Time)
                timer = new CountUpTimer(onReseted);
            else
                timer = new TickCountUpTimer(onReseted);
            timer.Initialize();
            return _timers.TryAdd(id, timer);
        }

        /// <summary>
        /// 決まった時間ごとにコールバックを呼ぶパルス式タイマー。既に同じ id があれば false。
        /// </summary>
        public bool PulseRegister(string id, float pulseInterval, Action onPulse = null, CountType type = CountType.Time)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id is null or empty", nameof(id));
            if (pulseInterval < 0f) throw new ArgumentOutOfRangeException(nameof(pulseInterval));
            ITimer timer;
            if (type == CountType.Time)
                timer = new PulseTimer(pulseInterval, onPulse) { InitialTime = pulseInterval };
            else
                timer = new TickPulseTimer(pulseInterval, onPulse) { InitialTime = pulseInterval };
            timer.Initialize();
            return _timers.TryAdd(id, timer);
        }

        /// <summary>
        /// タイマーが存在するか。
        /// </summary>
        public bool Contains(string id) => _timers.ContainsKey(id);

        /// <summary>
        /// タイマーの初期値を変更。存在しなければ無視。
        /// </summary>
        public void ChangeDuration(string id, float newDuration)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (newDuration < 0f) return;
            if (_timers.TryGetValue(id, out var t))
            {
                t.InitialTime = newDuration;
                if (t.Current > newDuration)
                    t.Current = newDuration;
            }
        }
        /// <summary>
        /// 登録解除。存在しなければ false。
        /// </summary>
        public bool Unregister(string id) => _timers.Remove(id);

        /// <summary>
        /// 初期値へリセット。カウント完了などのイベントから呼び出す場合は遅延実行を推奨。
        /// </summary>
        public bool Reset(string id, bool isLazy = false)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                if (isLazy)
                {
                    _commands.Enqueue((id, LazyCommand.Reset));
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
        public bool Start(string id, bool init = true, bool isLazy = false)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                if (isLazy)
                {
                    var command = init ? LazyCommand.Init | LazyCommand.Start : LazyCommand.Start;
                    _commands.Enqueue((id, command));
                    return true;
                }
                if (init) t.Initialize();
                t.Start();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 停止。カウント完了などのイベントから呼び出す場合は遅延実行を推奨。
        /// </summary>
        public bool Stop(string id, bool init = false, bool isLazy = false)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                if (isLazy)
                {
                    var command = init ? LazyCommand.Init | LazyCommand.Stop : LazyCommand.Stop;
                    _commands.Enqueue((id, command));
                    return true;
                }
                t.Stop();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 終了済みか（登録が無ければ false）。
        /// </summary>
        public bool IsFinished(string id)
        {
            return _timers.TryGetValue(id, out var t) && t is CountDownTimer && t.Current <= 0f;
        }

        /// <summary>
        /// 現在の時間を取得。
        /// </summary>
        public bool TryGetRemaining(string id, out float remaining)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                remaining = t.Current;
                return true;
            }
            remaining = 0f;
            return false;
        }

        /// <summary>
        /// 経過正規化 [0..1] を取得（未登録は 1 として返す）。
        /// </summary>
        public float GetNormalizedElapsed(string id)
        {
            return _timers.TryGetValue(id, out var t) ? t.NormalizedElapsed : 1f;
        }

        /// <summary>
        /// 完了時の Action を追加。存在しない場合 false。
        /// </summary>
        public bool AddAction(string id, Action action)
        {
            if (action == null) return false;
            if (_timers.TryGetValue(id, out var t))
            {
                t.OnFinished += action;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 完了時の Action を削除。存在しない場合 false。
        /// </summary>
        public bool RemoveAction(string id, Action action)
        {
            if (action == null) return false;
            if (_timers.TryGetValue(id, out var t))
            {
                t.OnFinished -= action;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 毎フレーム等で呼ぶ。内部で Keys のスナップショットを取るので、
        /// コールバック内で Unregister しても安全。
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
            var ids = _timers.Keys.ToList();
            foreach (var id in ids)
            {
                if (_timers.TryGetValue(id, out var t))
                {
                    if (t.IsRunning)
                        t.Update(deltaTime);
                }
            }
        }

        public IEnumerable<TimerSnapshot> GetSnapshot()
        {
            // キーのスナップショットで安全に列挙
            var ids = _timers.Keys.ToList();
            foreach (var id in ids)
            {
                if (_timers.TryGetValue(id, out var t))
                {
                    yield return new TimerSnapshot(
                        id,
                        t.InitialTime,
                        t.Current,
                        t.NormalizedElapsed,
                        t.IsRunning,
                        t.GetType().Name
                    );
                }
            }
        }

        public void SetSnapshot(TimerSnapshot snapshot)
        {
            Type type = Assembly.GetExecutingAssembly().GetType(snapshot.TimerClass);
            var timer = (ITimer)Activator.CreateInstance(type);
            timer.InitialTime = snapshot.Initialize;
            timer.Current = snapshot.Current;
            if (snapshot.IsRunning) timer.Start();
            _timers[snapshot.Id] = timer;
        }

        public void Dispose()
        {
            _timers.Clear();
            _commands.Clear();
            _timer.RemoveAll(t => t == _readonlyTimer);
        }

        private void InitializeTimer(string id)
        {
            if (_timers.TryGetValue(id, out var t))
            {
                t.Initialize();
            }
        }
    }
}
