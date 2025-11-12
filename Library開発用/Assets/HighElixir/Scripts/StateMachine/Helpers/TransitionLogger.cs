using HighElixir.Implements.Observables;
using System;

namespace HighElixir.StateMachine.Extention
{
    public static class TransitionLogger
    {
        public static IDisposable OnTransitionLogging<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s)
        {
#if DEBUG
            return s.OnTransition.Subscribe(x =>
            {
                if (s.Logger == null) return;
                s.Logger.Info(x.ToString());
            });
#endif
        }
    }
}