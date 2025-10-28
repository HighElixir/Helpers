using HighElixir.Implements;
using HighElixir.Implements.Observables;
using HighElixir.StateMachine.Extention;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace HighElixir.StateMachine
{
    /// <summary>
    /// ステートマシンの本体クラス
    /// <br/>任意のコンテキスト・イベント・ステート型を扱う汎用ステートマシン
    /// </summary>
    public sealed partial class StateMachine<TCont, TEvt, TState> : IDisposable
        where TState : IEquatable<TState>
    {
        // コンテキスト
        internal TCont _cont;

        // イベントキュー（遅延処理）
        private EventQueue<TCont, TEvt, TState> _queue;

        // 起動状態
        public bool Awaked { get; internal set; }

        // ステート一覧
        private Dictionary<TState, State> _states = new();

        // 任意遷移マップ
        private Dictionary<TEvt, TState> _anyTransition = new();

        // 遷移イベント (from, event, to)
        private ReactiveProperty<TransitionResult> _onTransition = new();
        public IObservable<TransitionResult> OnTransition => _onTransition;

        // 現在のステート情報
        private (TState id, State state) _current;
        public (TState id, State state) Current => _current;

        /// <summary>現在のコンテキスト（TCont）</summary>
        public TCont Context => _cont;

        /// <summary>
        /// ステートマシンの生成
        /// </summary>
        /// <param name="context">対象コンテキスト（MonoBehaviourなど）</param>
        /// <param name="queueMode">イベントキュー処理モード</param>
        public StateMachine(TCont context, QueueMode queueMode = QueueMode.UntilFailures)
        {
            _cont = context;
            _queue = new(this, queueMode);
        }

        /// <summary>
        /// ステートマシンを起動し、初期ステートに遷移する
        /// </summary>
        /// <param name="initialState">初期ステートID</param>
        public void Awake(TState initialState)
        {
            // 致命的なため即スロー
            if (initialState == null)
                throw new ArgumentNullException("[StateMachine]初期ステートが設定されていません");

            if (!_states.TryGetValue(initialState, out var state))
                throw new ArgumentNullException($"[StateMachine]初期ステート{initialState}が存在しません");

            _current = (initialState, state);

            try
            {
                state.Enter(default);
                Awaked = true;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>
        /// ステートマシンの更新処理
        /// <br/>キューの処理と現在ステートのUpdateを呼び出す
        /// </summary>
        public void Update(float deltaTime = 0f)
        {
            if (_disposed) return;
            var s = _current.state;

            // コマンドデキューが許可されていればイベントを処理
            if ((s.blockCommandDequeueFunc != null || s.blockCommandDequeueFunc()) &&
                !s.BlockCommandDequeue())
                _queue.Update();

            s.Update(deltaTime);
        }

        #region 遷移イベント処理

        /// <summary>
        /// 即時にイベントを送信して遷移を試みる
        /// </summary>
        public bool Send(TEvt evt)
        {
            if (_disposed) return false;
            var s = Current.state;

            // 現在のステート → 任意遷移 の順で探索
            if (!s._transitionMap.TryGetValue(evt, out var to) &&
                !_anyTransition.TryGetValue(evt, out to))
                return false;

            // 遷移の許可を確認
            if ((s.allowExitFunc != null && !s.allowExitFunc(to)) || !s.AllowExit())
                return false;
            if (!_states.TryGetValue(to, out var toState))
                return false;
            if ((toState.allowEnterFunc != null && !toState.allowEnterFunc(_current.id)) || !toState.AllowEnter())
                return false;

            try
            {
                // 遷移イベント通知
                _onTransition.Value = new(_current.id, evt, to);

                // Exit → Enter 呼び出し順
                s.Exit(to);
                s.InvokeExitAction(to);
                toState.InvokeEnterAction(_current.id);
                toState.Enter(_current.id);

                // 現在ステート更新
                _current.state = toState;
                _current.id = to;
                return true;
            }
            catch (Exception e)
            {
                OnError(e);
                return false;
            }
        }

        /// <summary>
        /// 遅延実行としてイベントをキューに送信する
        /// </summary>
        public void LazySend(TEvt evt)
        {
            if (_disposed) return;
            _queue.Enqueue(evt);
        }

        #endregion

        #region 登録

        /// <summary>
        /// ステートを登録する
        /// </summary>
        public void RegisterState(TState stateID, State state, params string[] tags)
        {
            if (_disposed) return;
            if (Awaked)
                throw new InvalidOperationException("[StateMachine]ステートマシンは起動済みです");

            if (_states.ContainsKey(stateID))
            {
#if DEBUG || UNITY_EDITOR
                throw new InvalidOperationException($"[StateMachine]このIDは既に登録されています:{stateID}");
#else
                return;
#endif
            }

            state.Tags.AddRange(tags);
            state.Parent = this;
            _states[stateID] = state;
        }

        /// <summary>
        /// 通常の遷移を登録する
        /// </summary>
        public void RegisterTransition(TState fromState, TEvt evt, TState toState)
            => RegisterTransition(fromState, evt, toState, null, null);

        /// <summary>
        /// 通常遷移＋購読イベントを登録する
        /// </summary>
        public IDisposable RegisterTransition(
            TState fromState,
            TEvt evt,
            TState toState,
            Action<TransitionResult> onTransition,
            Func<TransitionResult, bool> predicate = null)
        {
            if (!_disposed)
            {
                if (Awaked)
                    throw new InvalidOperationException("[StateMachine]ステートマシンは起動済みです");

                if (!_states.TryGetValue(fromState, out var state))
                {
#if DEBUG || UNITY_EDITOR
                    throw new InvalidOperationException($"[StateMachine]IDが存在しません:{fromState}");
#else
                    return;
#endif
                }

                state._transitionMap.Add(evt, toState);

                // 条件付き購読
                if (onTransition != null)
                {
                    return this.OnTransWhere(fromState, evt, toState)
                               .Where(x => predicate == null || predicate(x))
                               .Subscribe(onTransition);
                }
            }
            return null;
        }

        /// <summary>
        /// 任意遷移を登録する（どのステートからでも遷移可能）
        /// </summary>
        public void RegisterAnyTransition(TEvt evt, TState toState)
            => RegisterAnyTransition(evt, toState, null);

        /// <summary>
        /// 任意遷移＋購読イベントを登録する
        /// </summary>
        public IDisposable RegisterAnyTransition(
            TEvt evt,
            TState toState,
            Action<TransitionResult> onTransition,
            Func<TransitionResult, bool> predicate = null)
        {
            if (!_disposed)
            {
                if (Awaked)
                    throw new InvalidOperationException("[StateMachine]ステートマシンは起動済みです");

                _anyTransition.Add(evt, toState);

                // 条件付き購読
                if (onTransition != null)
                {
                    return this.OnTransWhere(evt, toState)
                               .Where(x => predicate == null || predicate(x))
                               .Subscribe(onTransition);
                }
            }
            return null;
        }

        #endregion

        #region イベント購読

        /// <summary>
        /// 指定ステートへの進入前イベントを購読する
        /// </summary>
        public IObservable<TState> OnEnterEvent(TState state)
        {
            if (_disposed) return null;
            if (!_states.TryGetValue(state, out var s)) return null;
            return s.OnEnter;
        }

        /// <summary>
        /// 指定ステートからの退出後イベントを購読する
        /// </summary>
        public IObservable<TState> OnExitEvent(TState state)
        {
            if (_disposed) return null;
            if (!_states.TryGetValue(state, out var s)) return null;
            return s.OnExit;
        }

        /// <summary>
        /// ステートへの進入を許可する条件を設定
        /// </summary>
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

        /// <summary>
        /// ステートからの脱出を許可する条件を設定
        /// </summary>
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

        /// <summary>
        /// 遅延遷移の実行をブロックする条件を設定
        /// </summary>
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

        #region エラーハンドリング

        /// <summary>
        /// ステートマシン内部での例外発生時処理
        /// </summary>
        internal void OnError(Exception exception)
        {
            // 現状は即時再スロー（後にロギング等に差し替え可能）
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        #endregion

        #region Dispose管理

        private bool _disposed;
        public bool Disposed => _disposed;

        /// <summary>
        /// ステートマシンを破棄し、すべてのステートと購読を解放する
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                foreach (var item in _states.Values)
                    item.Dispose();

                _cont = default;
                _anyTransition.Clear();
                _states.Clear();
                _onTransition.Dispose();

                _disposed = true;
            }
        }

        ~StateMachine()
        {
            Dispose(false);
        }

        #endregion
    }
}
