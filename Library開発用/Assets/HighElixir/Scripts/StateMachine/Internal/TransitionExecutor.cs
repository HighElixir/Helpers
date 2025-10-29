using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine.Internal
{
    internal class TransitionExecutor<TCont, TEvt, TState>
    {
        private readonly StateMachine<TCont, TEvt, TState> _machine;
        private readonly Dictionary<TEvt, TState> _anyTransition = new();

        public Dictionary<TEvt, TState> AnyTransition => _anyTransition;
        public TransitionExecutor(StateMachine<TCont, TEvt, TState> machine)
        {
            _machine = machine;
        }

        public bool TryTransition(TEvt evt)
        {
            var current = _machine.Current.info;

            // 遷移候補探索
            if (!current._transitionMap.TryGetValue(evt, out var to) &&
                !_anyTransition.TryGetValue(evt, out to))
                return false;

            // チェック
            if (!ValidateTransition(current, to)) return false;

            // 実行
            Execute(current, to, evt);
            return true;
        }

        private bool ValidateTransition(StateMachine<TCont, TEvt, TState>.StateInfo from, TState to)
        {
            if ((from.allowExitFunc != null && !from.allowExitFunc(to)) || !from.State.AllowExit())
                return false;

            if (!_machine.TryGetStateInfo(to, out var toState))
                return false;

            if ((toState.allowEnterFunc != null && !toState.allowEnterFunc(_machine.Current.id)) || !toState.State.AllowEnter())
                return false;

            return true;
        }

        private void Execute(StateMachine<TCont, TEvt, TState>.StateInfo from, TState to, TEvt evt)
        {
            if (!_machine.TryGetStateInfo(to, out var toState)) return;
            _machine.Notify(new(from.ID, evt, to));

            // ★ 先に親のExit系、その後で子FSMを停止
            from.State.Exit();
            from.InvokeExitAction(to);
            from.SubHost?.OnParentExit();

            // ★ 次の親ステートEnter前後で子FSMを起動
            toState.InvokeEnterAction(from.ID);
            toState.State.Enter();
            toState.SubHost?.OnParentEnter();

            _machine.Current = (to, toState);
        }
    }

}