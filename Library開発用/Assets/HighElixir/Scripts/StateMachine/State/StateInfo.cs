using HighElixir.Implements.Observables;
using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine
{
    public sealed partial class StateMachine<TCont, TEvt, TState>
    {
        public class StateInfo : IStateInfo<TCont>, IDisposable
        {
            internal ISubHostBase<TCont, TEvt> SubHost;
            internal State<TCont> _state;

            /// <summary>遷移イベントマップ</summary>
            internal Dictionary<TEvt, TState> _transitionMap = new();
            private ReactiveProperty<IStateInfo<TCont>> _onEnter = new();
            private ReactiveProperty<IStateInfo<TCont>> _onExit = new();
            private ActionAsObservable<StateInfo> _onCompletion = new();
            internal IDisposable _obs;

            public TState ID { get; internal set; }
            public State<TCont> State => _state;
            public StateMachine<TCont, TEvt, TState> Parent { get; internal set; }

            #region イベント
            /// <summary>Enter発火時の通知（Reactive）</summary>
            public IObservable<IStateInfo<TCont>> OnEnter => _onEnter;

            /// <summary>Exit発火時の通知（Reactive）</summary>
            public IObservable<IStateInfo<TCont>> OnExit => _onExit;

            public bool Binded => State != null;

            /// <summary>
            /// Enterの前に呼ばれる
            /// </summary>
            /// <param name="prev">前のステート</param>
            internal void InvokeEnterAction(IStateInfo<TCont> prev)
            {
                try
                {
                    _onEnter.Value = prev;
                }
                catch (Exception ex)
                {
                    _state.Parent.OnError(ex);
                }
            }

            /// <summary>
            /// Exitの後に呼ばれる
            /// </summary>
            /// <param name="next">次のステート</param>
            internal void InvokeExitAction(IStateInfo<TCont> next)
            {
                try
                {
                    _onExit.Value = next;
                }
                catch (Exception ex)
                {
                    _state.Parent.OnError(ex);
                }
            }

            internal IObservable<StateInfo> ObserveAction()
            {
                if (_state is INotifyStateCompletion completion)
                {
                    _onCompletion.SetContext(this);
                    _obs = completion.Completion.Subscribe(_ => _onCompletion.Invoke());
                    return _onCompletion;
                }
                throw new FieldAccessException();
            }
            #endregion

            #region 遷移許可

            internal Func<IStateInfo<TCont>, bool> allowEnterFunc; // 前のステート
            internal Func<IStateInfo<TCont>, bool> allowExitFunc; // 次のステート
            internal Func<bool> blockCommandDequeueFunc;

            /// <summary>
            /// ステート自身の<see cref="AllowEnter"/>よりも先に呼ばれる
            /// <br/>TState => Preview State
            /// </summary>
            public event Func<IStateInfo<TCont>, bool> AllowEnterFunc
            {
                add => allowEnterFunc += value;
                remove => allowEnterFunc -= value;
            }

            /// <summary>
            /// ステート自身の<see cref="AllowExit"/>よりも先に呼ばれる
            /// <br/>TState => Next State
            /// </summary>
            public event Func<IStateInfo<TCont>, bool> AllowExitFunc
            {
                add => allowExitFunc += value;
                remove => allowExitFunc -= value;
            }

            /// <summary>
            /// EventQueueによる遅延遷移処理の実行をブロックするかどうか。
            /// <br/><see cref="BlockCommandDequeue"/>よりも先に呼ばれる
            /// </summary>
            public event Func<bool> BlockCommandDequeueFunc
            {
                add => blockCommandDequeueFunc += value;
                remove => blockCommandDequeueFunc -= value;
            }
            #endregion

            #region
            public void RegisterTransition(TEvt evt, TState to)
            {
                _transitionMap.Add(evt, to);
            }
            #endregion

            public void Dispose()
            {
                _state?.Dispose();
                SubHost?.Dispose();
                _obs?.Dispose();
            }

            public override string ToString()
            {
                if (Parent != null)
                    return $"{Parent.ToString()}.{ID.ToString()}";
                else
                    return $"{ID.ToString()}";
            }
        }
    }
}