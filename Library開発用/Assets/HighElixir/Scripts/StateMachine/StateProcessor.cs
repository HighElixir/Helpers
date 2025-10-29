using System;

namespace HighElixir.StateMachine
{
    public sealed class StateProcessor<TCont, TEvt, TState> : IStateRegisterProcessor<TCont, TEvt, TState>
    {
        private readonly Action<TState, StateMachine<TCont, TEvt, TState>.StateInfo> _process;
        public StateProcessor(Action<TState, StateMachine<TCont, TEvt, TState>.StateInfo> process)
        {
            if (process == null) throw new ArgumentNullException("process is null");
            _process = process;
        }
        public void OnRegisterState(TState id, StateMachine<TCont, TEvt, TState>.StateInfo state)
        {
            _process(id, state);
        }
    }
}