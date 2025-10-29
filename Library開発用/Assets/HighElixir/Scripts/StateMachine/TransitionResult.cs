using System;

namespace HighElixir.StateMachine
{
    public sealed partial class StateMachine<TCont, TEvt, TState>

    {
        public readonly struct TransitionResult
        {
            public readonly TState FromState;
            public readonly TState ToState;
            public readonly TEvt Event;

            public TransitionResult(TState from, TEvt evt, TState to)
            {
                FromState = from;
                ToState = to;
                Event = evt;
            }

            public override string ToString()
            {
                return $"{FromState} => \"{Event}\" => {ToState}";
            }
        }
    }
}