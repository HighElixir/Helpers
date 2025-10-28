using System;

namespace HighElixir.StateMachine
{
    public sealed partial class StateMachine<TCont, TEvt, TState>
        where TState : IEquatable<TState>
    {
        public readonly struct TransitionResult
        {
            public readonly TState FromState;
            public readonly TState ToState;
            public readonly TEvt Event;

            public TransitionResult(TState to, TEvt evt, TState from)
            {
                FromState = from;
                ToState = to;
                Event = evt;
            }
        }
    }
}