using HighElixir.Implements;
using HighElixir.Implements.Observables;
using HighElixir.Loggings;
using HighElixir.StateMachine.Extention;
using HighElixir.StateMachine.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace HighElixir.StateMachine
{
    /// <summary>
    /// ステートマシンの本体クラス
    /// <br/>任意のコンテキスト・イベント・ステート型を扱う汎用ステートマシン
    /// </summary>
    public sealed partial class StateMachine<TCont, TEvt, TState> : IStateMachine<TCont>, IDisposable

    {
        #region Fields
        private IStateMachine<TCont> _parent;

        // コンテクスト
        internal TCont _cont;
        private TState _initial;

        // 外部
        private IEventQueue<TCont, TEvt, TState> _queue;
        private TransitionExecutor<TCont, TEvt, TState> _executor;
        private readonly Dictionary<TState, StateInfo> _states = new();
        private ILogger _logger;

        // 通知
        private readonly ReactiveProperty<TransitionResult> _onTransition = new();
        private readonly ReactiveProperty<StateInfo> _onCompletion = new();

        // 状態管理
        private (TState id, StateInfo info) _current;
        private bool _disposed;

        #endregion

        #region Delegates / Hooks

        // 外部委譲
        public IStateMachineErrorHandler ErrorHandler { get; set; }
        public IStateRegisterProcessor<TCont, TEvt, TState> RegisterProcessor { get; set; }

        #endregion

        #region Properties

        public IStateMachine<TCont> Parent { get => _parent; internal set => _parent = value; }
        public TCont Context => _cont;
        public (TState id, StateInfo info) Current { get => _current; internal set => _current = value; }
        public ILogger Logger { get => _logger; internal set => _logger = value; }
        public bool Awaked { get; internal set; }
        public bool IsRunning { get; internal set; }
        public bool Disposed => _disposed;
        public IObservable<TransitionResult> OnTransition => _onTransition;
        public IObservable<StateInfo> OnCompletion => _onCompletion;

        #endregion

        #region Constructor

        public StateMachine(TCont context, QueueMode mode = QueueMode.UntilFailures, IEventQueue<TCont, TEvt, TState> eventQueue = null, ILogger logger = null)
        {
            _cont = context;
            _queue = eventQueue ?? new DefaultEventQueue<TCont, TEvt, TState>(this, mode);
            _executor = new(this);
            _logger = logger;
        }

        #endregion

        #region Lifecycle

        public void Awake(TState initialState)
        {
            if (!_states.ContainsKey(initialState))
                throw new ArgumentNullException($"[StateMachine]初期ステート {initialState} が存在しません");
            _initial = initialState;
            Awaked = true;
            Reset();
            Start();
        }

        public void Pause(bool initialize = true)
        {
            IsRunning = false;
            if (initialize)
                Reset();
        }

        public void Resume()
            => Start();

        private void Start()
        {
            if (!_states.ContainsKey(_initial)) return;
            try
            {
                _current.info.State.Enter();
                _current.info.SubHost?.OnParentEnter();
                IsRunning = true;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }
        public void Reset()
        {
            if (_states.TryGetValue(_initial, out var state))
                _current = (_initial, state);
        }

        public void Update(float deltaTime = 0f)
        {
            if (_disposed || !Awaked) return;
            var s = _current.info;

            if ((s.blockCommandDequeueFunc != null && s.blockCommandDequeueFunc()) &&
                !s.State.BlockCommandDequeue())
                _queue.Process();

            s.State.Update(deltaTime);
            s.SubHost?.Update(deltaTime); // ★ 追加：子FSM Update
        }


        #endregion

        #region Transition

        public bool Send(TEvt evt)
        {
            if (_disposed || !Awaked) return false;
            var s = Current.info;

            try
            {
                if (s.SubHost != null && s.SubHost.ForwardFirst && s.SubHost.TrySend(evt))
                    return true;

                // 親自身の遷移判定
                if (!_executor.TryTransition(evt))
                {
                    if (s.SubHost != null && !s.SubHost.ForwardFirst)
                        return s.SubHost.TrySend(evt);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                OnError(e);
                return false;
            }
        }


        public void LazySend(TEvt evt)
        {
            if (_disposed || !Awaked) return;
            _queue.Enqueue(evt);
        }

        #endregion

        #region Registration

        /// <summary>
        /// ステートを登録する
        /// </summary>
        public StateInfo RegisterState(TState id, State<TCont> state, params string[] tags)
        {
            if (_disposed) return null;
            if (Awaked)
                throw new InvalidOperationException("[StateMachine]ステートマシンは起動済みです");

            if (_states.ContainsKey(id))
                throw new InvalidOperationException($"[StateMachine]このIDは既に登録されています: {id}");

            state.Tags.AddRange(tags);
            state.Parent = this;
            _states[id] = new() { _state = state, Parent = this, ID = id };
            if (state is INotifyStateCompletion)
            {
                var d = _states[id].ObserveAction().Subscribe(x => _onCompletion.Value = x);
                _states[id]._obs.Join(d);
            }
            try
            {
                // 外部プロセッサに委譲
                RegisterProcessor?.OnRegisterState(id, _states[id]);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }

            if (_logger != null)
                _logger.Info($"[{Context.ToString()}] ステート登録：{id.ToString()}");
            return _states[id];
        }

        public void RegisterTransition(TState fromState, TEvt evt, TState toState)
            => RegisterTransition(fromState, evt, toState, null, null);

        public IDisposable RegisterTransition(
            TState fromState,
            TEvt evt,
            TState toState,
            Action<TransitionResult> onTransition,
            Func<TransitionResult, bool> predicate = null)
        {
            if (_disposed) return null;
            if (Awaked)
                OnError(new InvalidOperationException("[StateMachine]ステートマシンは起動済みです"));

            if (!_states.TryGetValue(fromState, out var state))
            {
                state = CreateInfo(fromState);
            }

            state.RegisterTransition(evt, toState);

            if (onTransition != null)
            {
                return this.OnTransWhere(fromState, evt, toState)
                           .Where(x => predicate == null || predicate(x))
                           .Subscribe(onTransition);
            }
            return null;
        }

        public void RegisterAnyTransition(TEvt evt, TState toState)
            => RegisterAnyTransition(evt, toState, null);

        public IDisposable RegisterAnyTransition(
            TEvt evt,
            TState toState,
            Action<TransitionResult> onTransition,
            Func<TransitionResult, bool> predicate = null)
        {
            if (_disposed) return null;
            if (Awaked)
                OnError(new InvalidOperationException("[StateMachine]ステートマシンは起動済みです"));

            _executor.AnyTransition.Add(evt, toState);

            if (onTransition != null)
            {
                return this.OnTransWhere(evt, toState)
                           .Where(x => predicate == null || predicate(x))
                           .Subscribe(onTransition);
            }
            return null;
        }

        #endregion

        #region Subscription

        public IObservable<TState> OnEnterEvent(TState state)
        {
            if (_disposed) return null;
            return _states.TryGetValue(state, out var s) ? s.OnEnter : null;
        }

        public IObservable<TState> OnExitEvent(TState state)
        {
            if (_disposed) return null;
            return _states.TryGetValue(state, out var s) ? s.OnExit : null;
        }

        public IDisposable AllowEnter(TState state, Func<TState, bool> predicate)
        {
            if (_disposed) return null;
            if (_states.TryGetValue(state, out var s))
            {
                s.AllowEnterFunc += predicate;
                return Disposable.Create(() => s.AllowEnterFunc -= predicate);
            }
            return Disposable.Empty;
        }

        public IDisposable AllowExit(TState state, Func<TState, bool> predicate)
        {
            if (_disposed) return null;
            if (_states.TryGetValue(state, out var s))
            {
                s.AllowExitFunc += predicate;
                return Disposable.Create(() => s.AllowExitFunc -= predicate);
            }
            return Disposable.Empty;
        }

        public IDisposable BlockCommandDequeue(TState state, Func<bool> predicate)
        {
            if (_disposed) return null;
            if (_states.TryGetValue(state, out var s))
            {
                s.BlockCommandDequeueFunc += predicate;
                return Disposable.Create(() => s.BlockCommandDequeueFunc -= predicate);
            }
            return Disposable.Empty;
        }

        #endregion

        #region Error Handling

        public void OnError(Exception ex)
        {
            if (ErrorHandler != null)
            {
                try { ErrorHandler.Handle(ex); }
                catch { /* 外部ハンドラ内の例外は握りつぶす */ }
            }
            else
            {
                if (_logger != null)
                    _logger.Error(ex);
                else
                    ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        #endregion

        #region Dispose Management

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                foreach (var item in _states.Values)
                    item.Dispose();

                _cont = default;
                _executor.AnyTransition.Clear();
                _states.Clear();
                _onTransition.Dispose();
                _disposed = true;
                _parent = null;
            }
        }

        ~StateMachine() => Dispose(false);

        #endregion

        public override string ToString()
        {
            if (_disposed) return string.Empty;
            if (_parent != null)
            {
                return $"{_parent.ToString()}.[{nameof(TState)}Machine]";
            }
            else
            {
                return $"[{nameof(TState)}Machine]";
            }
        }

        #region Internal Helpers

        internal bool TryGetStateInfo(TState state, out StateInfo info)
            => _states.TryGetValue(state, out info);

        internal void Notify(TransitionResult transitionResult)
        {
            if (_disposed) return;
            _onTransition.Value = transitionResult;
        }

        internal StateInfo CreateInfo(TState state)
        {
            var info = new StateInfo();
            info.ID = state;
            _states.Add(state, info);
            return info;
        }
        #endregion
    }
}
