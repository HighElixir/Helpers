using HighElixir.Implements;
using HighElixir.Implements.Observables;
using HighElixir.Loggings;
using HighElixir.StateMachine.Extension;
using HighElixir.StateMachine.Internal;
using HighElixir.StateMachine.Thead;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace HighElixir.StateMachine
{
    /// <summary>
    /// ステートマシンの本体クラス。
    /// 任意のコンテキスト・イベント・ステート型を扱う汎用ステートマシン。
    /// ネストされたサブホスト構造にも対応し、イベント駆動で状態遷移を制御する。
    /// </summary>
    public sealed partial class StateMachine<TCont, TEvt, TState> : IStateMachine<TCont>, IDisposable
    {
        #region Fields
        private IStateMachine<TCont> _parent;

        // コンテキスト（任意の実行対象データ）
        internal TCont _cont;
        private TState _initial;

        // 外部依存コンポーネント
        private IEventQueue<TCont, TEvt, TState> _queue;
        private TransitionExecutor<TCont, TEvt, TState> _executor;
        private readonly Dictionary<TState, StateInfo> _states = new();
        private ILogger _logger;
        private RequiredLoggerLevel _logLevel = RequiredLoggerLevel.Error | RequiredLoggerLevel.Fatal;

        // 通知用プロパティ
        private readonly ReactiveProperty<TransitionResult> _onTransition = new();
        private readonly ReactiveProperty<StateInfo> _onCompletion = new();

        // 現在状態管理
        private (TState id, StateInfo info) _current;
        private bool _disposed;
        private bool _enableOverriding;
        private bool _enableSelfTransition;
        #endregion

        #region Delegates / Hooks
        /// <summary> エラーハンドラ。外部で例外処理をカスタマイズできる。 </summary>
        public IStateMachineErrorHandler ErrorHandler { get; set; }

        /// <summary> ステート登録時の外部プロセッサ。外部フック用。 </summary>
        public IStateRegisterProcessor<TCont, TEvt, TState> RegisterProcessor { get; set; }
        #endregion

        #region Properties
        public IStateMachine<TCont> Parent { get => _parent; internal set => _parent = value; }
        public TCont Context => _cont;
        public (TState id, StateInfo info) Current { get => _current; internal set => _current = value; }
        public ILogger Logger { get => _logger; internal set => _logger = value; }
        public bool Awaked { get; internal set; }
        public bool IsRunning { get; internal set; }
        public bool EnableSelfTransition => _enableSelfTransition;
        public bool Disposed => _disposed;
        public IObservable<TransitionResult> OnTransition => _onTransition;
        public IObservable<StateInfo> OnCompletion => _onCompletion;
        #endregion

        #region Constructors
        /// <summary>
        /// コンテキストとキュー設定を指定して構築する基本コンストラクタ。
        /// </summary>
        public StateMachine(
            TCont context,
            QueueMode mode = QueueMode.UntilFailures,
            IEventQueue<TCont, TEvt, TState> eventQueue = null,
            ILogger logger = null)
        {
            _cont = context;
            _queue = eventQueue ?? new DefaultEventQueue<TCont, TEvt, TState>(this, mode);
            _executor = new(this);
            _logger = logger;
        }

        /// <summary>
        /// StateMachineOption から構築するオーバーロード。
        /// - EnableOverriding
        /// - Logger/LogLevel
        /// - Queue/QueueMode
        /// - EnableSelfTransition  
        /// などのオプションを反映する。
        /// </summary>
        public StateMachine(StateMachineOption<TCont, TEvt, TState> option)
        {
            if (option == null) throw new ArgumentNullException(nameof(option));
            _cont = option.Cont;
            _queue = option.Queue ?? new DefaultEventQueue<TCont, TEvt, TState>(this, option.QueueMode);
            _executor = new(this);
            _logger = option.Logger;
            _logLevel = option.LogLevel;
            _enableOverriding = option.EnableOverriding;
            _enableSelfTransition = option.EnableSelfTransition;
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// ステートマシンを初期化して起動する。
        /// 指定された初期ステートが未登録なら例外を投げる。
        /// </summary>
        public async Task Awake(TState initialState)
        {
            if (!_states.ContainsKey(initialState))
                throw new ArgumentNullException($"[StateMachine]初期ステート {initialState} が存在しません");

            _initial = initialState;
            Awaked = true;

            // 未バインド検知（デバッグ支援）
            if (_logger != null)
            {
                foreach (var state in _states.Values)
                {
                    if (!state.Binded)
                        Log(RequiredLoggerLevel.Warning, $"{state.ID} does not bind.");
                }
            }

            Reset();
            await Start();
        }

        /// <summary>
        /// ステートマシンを一時停止する。
        /// initialize=true の場合、状態を初期ステートにリセット。
        /// </summary>
        public void Pause(bool initialize = true)
        {
            IsRunning = false;
            if (initialize) Reset();
        }

        /// <summary> 一時停止中のマシンを再開する。 </summary>
        public async Task Resume() => await Start();

        /// <summary>
        /// 起動処理。初期ステートをアクティブにして開始する。
        /// </summary>
        private async Task Start()
        {
            if (!_states.ContainsKey(_initial)) return;
            try
            {
                await StateExecuter.StateEnter(_current.info);
                _current.info.SubHost?.OnParentEnter();
                IsRunning = true;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>
        /// 現在の状態を初期ステートにリセットする。
        /// </summary>
        public void Reset()
        {
            if (_states.TryGetValue(_initial, out var state))
                _current = (_initial, state);
        }

        /// <summary>
        /// ステートマシンの定期更新処理。
        /// イベントキュー処理や状態更新を実行。
        /// </summary>
        public async Task Update(float deltaTime = 0f)
        {
            if (_disposed || !Awaked || !IsRunning) return;
            var s = _current.info;

            // キュー処理がブロックされていなければ実行
            if ((s.blockCommandDequeueFunc == null || !s.blockCommandDequeueFunc()) && !s.State.BlockCommandDequeue())
                await _queue.Process();

            if (s.State is StateAsync<TCont> asyncState)
            {
                await asyncState.UpdateAsync();
            }
            else
            {
                s.State.Update(deltaTime);
            }
            s.SubHost?.Update(deltaTime);
        }
        #endregion

        #region Transition
        /// <summary>
        /// イベントを即時送信して遷移を試みる。
        /// サブホスト設定に応じて優先転送を行う。
        /// </summary>
        public async Task<bool> Send(TEvt evt)
        {
            if (_disposed || !Awaked) return false;
            var s = Current.info;

            try
            {
                if (s.SubHost != null && s.SubHost.ForwardFirst && await s.SubHost.TrySend(evt))
                    return true;

                if (!await _executor.TryTransition(evt))
                {
                    if (s.SubHost != null && !s.SubHost.ForwardFirst)
                        return await s.SubHost.TrySend(evt);
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

        /// <summary>
        /// イベントを遅延送信キューに追加する。
        /// 既に同一イベントが存在する場合、skipIfExist=trueでスキップ可能。
        /// </summary>
        public bool LazySend(TEvt evt, bool skipIfExist = false)
        {
            if (_disposed || !Awaked) return false;
            return _queue.Enqueue(evt, skipIfExist);
        }
        #endregion

        #region Registration
        /// <summary>
        /// ステートを登録する。必要に応じて上書き可。
        /// 登録後は外部プロセッサによる初期化フックも呼び出される。
        /// </summary>
        public StateInfo RegisterState(TState id, State<TCont> state, params string[] tags)
        {
            if (_disposed) return null;
            if (Awaked)
                throw new InvalidOperationException("[StateMachine]ステートマシンは起動済みです");

            state.Tags.AddRange(tags);
            state.Parent = this;
            if (_states.ContainsKey(id))
            {
                if (_states[id].Binded && !_enableOverriding)
                    throw new InvalidOperationException($"[StateMachine]このIDは既に登録されています: {id}");
                else
                    _states[id]._state = state;
            }
            else
            {
                _states[id] = new() { _state = state, Parent = this, ID = id };
            }

            // 完了通知を購読
            if (state is INotifyStateCompletion)
            {
                var d = _states[id].ObserveAction().Subscribe(x => _onCompletion.Value = x);
                _states[id]._obs.Join(d);
            }

            try
            {
                RegisterProcessor?.OnRegisterState(id, _states[id]);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }

            Log(RequiredLoggerLevel.Info, $"[{Context?.ToString()}] ステート登録：{id?.ToString()}");
            return _states[id];
        }

        /// <summary> 通常のステート間遷移を登録する。 </summary>
        public void RegisterTransition(TState fromState, TEvt evt, TState toState)
            => RegisterTransition(fromState, evt, toState, null, null);

        /// <summary>
        /// ステート遷移を登録し、任意の購読処理を追加する。
        /// </summary>
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
                state = CreateInfo(fromState);

            state.RegisterTransition(evt, toState);
            Log(RequiredLoggerLevel.Info, $"Registered : {fromState} = \"{evt}\" => {toState}");

            if (onTransition != null)
            {
                return this.OnTransWhere(fromState, evt, toState)
                           .Where(x => predicate == null || predicate(x))
                           .Subscribe(onTransition);
            }
            return null;
        }

        /// <summary> 任意ステートからの遷移を登録する（グローバル遷移）。 </summary>
        public void RegisterAnyTransition(TEvt evt, TState toState)
            => RegisterAnyTransition(evt, toState, null);

        /// <summary>
        /// 任意遷移登録＋遷移時イベント購読を行う。
        /// </summary>
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
        /// <summary> ステートのEnter / Exitイベントを購読する。 </summary>
        public IObservable<EventState> GetObservableState(TState state)
        {
            if (_disposed) return null;
            var info = GetOrCreate(state);
            return info._onTrans;
        }

        /// <summary>
        /// 遷移許可条件を追加する。
        /// 条件がfalseの場合、ステートの遷移を拒否。
        /// </summary>
        public IDisposable AllowTrans(TState state, Func<EventState, bool> predicate)
        {
            if (_disposed) return Disposable.Empty;
            var s = GetOrCreate(state);
            s.AllowTrans += predicate;
            return Disposable.Create(() => s.AllowTrans -= predicate);
        }

        /// <summary>
        /// コマンドキューのデキュー処理をブロックする条件を設定。
        /// </summary>
        public IDisposable BlockCommandDequeue(TState state, Func<bool> predicate)
        {
            if (_disposed) return null;
            var s = GetOrCreate(state);
            s.BlockCommandDequeueFunc += predicate;
            return Disposable.Create(() => s.BlockCommandDequeueFunc -= predicate);
        }
        #endregion

        #region Error Handling
        /// <summary>
        /// エラー発生時の処理。
        /// 外部ハンドラがあれば委譲し、未設定なら例外を再スロー。
        /// </summary>
        public void OnError(Exception ex)
        {
            if (ErrorHandler != null)
            {
                try { ErrorHandler.Handle(ex); }
                catch { /* 外部ハンドラ内の例外は握りつぶす */ }
            }
            else
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
        #endregion

        #region Dispose Management
        public bool IsDisposed => _disposed;

        /// <summary> 破棄処理。内部リソースを解放。 </summary>
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

        /// <summary>
        /// デバッグ用の文字列表現を返す。
        /// ネストされた場合は親の階層構造も含む。
        /// </summary>
        public override string ToString()
        {
            if (_disposed) return string.Empty;
            if (_parent != null)
                return $"{_parent.ToString()}.[{nameof(TState)}Machine]";
            else
                return $"[{nameof(TState)}Machine]";
        }

        #region Internal Helpers
        /// <summary> 指定ステートの情報を取得。存在しない場合は false を返す。 </summary>
        public bool TryGetStateInfo(TState state, out StateInfo info)
            => _states.TryGetValue(state, out info);

        /// <summary> ステート情報を取得し、存在しなければ新規作成する。 </summary>
        internal StateInfo GetOrCreate(TState state)
        {
            if (!TryGetStateInfo(state, out var info))
                info = CreateInfo(state);
            return info;
        }

        /// <summary> 遷移完了通知を送出。 </summary>
        internal void Notify(TransitionResult transitionResult)
        {
            if (_disposed) return;
            _onTransition.Value = transitionResult;
        }

        /// <summary> ステート情報を新規作成し、辞書に登録。 </summary>
        internal StateInfo CreateInfo(TState state)
        {
            var info = new StateInfo();
            info.ID = state;
            info.Parent = this;
            _states.Add(state, info);
            return info;
        }

        // TODO : internalなLogHelperに分離
        /// <summary> ログ出力可否を判定。 </summary>
        private bool IsLogEnabled(RequiredLoggerLevel level) => (_logLevel & level) != 0;

        /// <summary> 指定レベルでログを出力。 </summary>
        internal void Log(RequiredLoggerLevel level, string message)
        {
            if (_logger == null || !IsLogEnabled(level)) return;
            switch (level)
            {
                case RequiredLoggerLevel.Info: _logger.Info(message); break;
                case RequiredLoggerLevel.Warning: _logger.Warn(message); break;
                case RequiredLoggerLevel.Error: _logger.Error(message); break;
                case RequiredLoggerLevel.Fatal: _logger.Error(message); break;
                default: break;
            }
        }

        // TODO : internalなLogHelperに分離
        /// <summary> 例外をログ出力。 </summary>
        private void Log(RequiredLoggerLevel level, Exception ex)
        {
            if (_logger == null || !IsLogEnabled(level)) return;
            switch (level)
            {
                case RequiredLoggerLevel.Error: _logger.Error(ex); break;
                case RequiredLoggerLevel.Fatal: _logger.Error(ex); break;
                default: _logger.Error(ex); break;
            }
        }
        #endregion
    }
}
