using HighElixir.Implements.Observables;
using System;
using System.Collections.Generic;

namespace HighElixir.StateMachines
{
    public sealed partial class StateMachine<TCont, TEvt, TState>
    {
        public class StateInfo : IStateInfo<TCont>, IDisposable
        {
            internal ISubHostBase<TCont, TEvt> SubHost;
            internal State<TCont> _state;

            /// <summary>遷移イベントマップ</summary>
            internal Dictionary<TEvt, TState> _transitionMap = new();
            internal ReactiveProperty<EventState> _onTrans = new();
            private readonly ActionAsObservable<StateInfo> _onCompletion = new();
            internal IDisposable _obs;

            public TState ID { get; internal set; }
            public State<TCont> State => _state;
            public StateMachine<TCont, TEvt, TState> Parent { get; internal set; }

            public bool Binded => State != null;
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

            #region 遷移許可

            internal Func<EventState, bool> allowTransFunc;
            internal Func<bool> blockCommandDequeueFunc;

            public event Func<EventState, bool> AllowTrans
            {
                add => allowTransFunc += value;
                remove => allowTransFunc -= value;
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
                    return $"{Parent}.{ID}";
                else
                    return $"{ID}";
            }

            public IDisposable Subscribe(IObserver<EventState> observer)
            {
                return _onTrans.Subscribe(observer);
            }
        }
    }
}