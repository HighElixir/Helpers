using System;

namespace HighElixir.StateMachines
{
    public sealed class StateProcessor<TCont, TEvt, TState> : IStateRegisterProcessor<TCont, TEvt, TState>
    {
        private readonly Action<TState, StateMachine<TCont, TEvt, TState>.StateInfo> _process;
        public StateProcessor(Action<TState, StateMachine<TCont, TEvt, TState>.StateInfo> process)
        {
            _process = process ?? throw new ArgumentNullException("process is null");
        }
        public void OnRegisterState(TState id, StateMachine<TCont, TEvt, TState>.StateInfo state)
        {
            _process(id, state);
        }
    }
}