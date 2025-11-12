using System;
using System.Collections.Generic;

namespace HighElixir.StateMachine
{
    public interface ISubHostBase<TCont, TEvt> : IDisposable
    {
        void OnParentEnter();
        void OnParentExit();
        void Update(float dt);
        bool TrySend(TEvt evt);
        bool TryGetCurrentSubHost(out ISubHostBase<TCont, TEvt> subHost);
        bool ForwardFirst { get; }

        List<string> CurrentStateTag { get; }
        bool TryGetStateInfo(TEvt evt, out IStateInfo<TCont> stateInfo);
    }
}