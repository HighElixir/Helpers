using HighElixir.Implements.Observables;
using System;

namespace HighElixir.StateMachines.Extension
{
    public static class AllowTransExt
    {
        /// <summary> ステートのEnterイベントを購読する。 </summary>
        public static IObservable<EventState> OnEnterEvent<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> machine, TState state)
        {
            if (machine == null || machine.IsDisposed)
                return Empty<EventState>.Instance;
            var info = machine.GetOrCreate(state);
            return info._onTrans.Where<EventState>(s => s == EventState.Entering);
        }

        /// <summary> ステートのExitイベントを購読する。 </summary>
        public static IObservable<EventState> OnExitEvent<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> machine, TState state)
        {
            if (machine == null || machine.IsDisposed)
                return Empty<EventState>.Instance;
            var info = machine.GetOrCreate(state);
            return info._onTrans.Where<EventState>(s => s == EventState.Exiting);
        }
    }
}