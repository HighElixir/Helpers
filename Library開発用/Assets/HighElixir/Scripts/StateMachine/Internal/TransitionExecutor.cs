using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachines.Internal
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

        public async Task<bool> TryTransition(TEvt evt, CancellationToken token = default)
        {
            var current = _machine.Current.info;

            // 遷移候補探索
            if (!current._transitionMap.TryGetValue(evt, out var to) &&
                !_anyTransition.TryGetValue(evt, out to))
                return false;

            // チェック
            if (!ValidateTransition(current, to)) return false;

            // 実行
            await Execute(current, to, evt, token);
            return true;
        }

        private bool ValidateTransition(StateMachine<TCont, TEvt, TState>.StateInfo from, TState to)
        {
            try
            {
                if (!_machine.EnableSelfTransition && from.ID.Equals(to)) return false;

                if (!_machine.TryGetStateInfo(to, out StateMachine<TCont, TEvt, TState>.StateInfo toState))
                    return false;
                if (!toState.Binded)
                    throw new InvalidOperationException($"[{to} does not bind.]");
                if ((from.allowTransFunc != null && !from.allowTransFunc(EventState.Exiting)) || !from.State.AllowTrans(EventState.Exiting))
                    return false;

                if ((toState.allowTransFunc != null && !toState.allowTransFunc(EventState.Entering)) || !toState.State.AllowTrans(EventState.Entering))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _machine.OnError(ex);
                return false;
            }
        }

        private async Task Execute(StateMachine<TCont, TEvt, TState>.StateInfo from, TState to, TEvt evt, CancellationToken token)
        {
            if (!_machine.TryGetStateInfo(to, out StateMachine<TCont, TEvt, TState>.StateInfo toState)) return;
            _machine.Notify(new StateMachine<TCont, TEvt, TState>.TransitionResult(from.ID, evt, to));

            // ★ 先に親のExit系、その後で子FSMを停止
            await StateExecuter.StateExit(_machine.Current.info, token);
            from.SubHost?.OnParentExit();

            // ★ 次の親ステートEnter前後で子FSMを起動
            await StateExecuter.StateEnter(toState, token);
            toState.SubHost?.OnParentEnter();

            _machine.Current = (to, toState);
        }
    }

}