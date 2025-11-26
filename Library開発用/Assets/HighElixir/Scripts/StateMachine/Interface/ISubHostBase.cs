using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HighElixir.StateMachines
{
    public interface ISubHostBase<TCont, TEvt> : IDisposable
    {
        Task OnParentEnter();
        void OnParentExit();
        Task Update(float dt);
        Task<bool> TrySend(TEvt evt);
        bool TryGetCurrentSubHost(out ISubHostBase<TCont, TEvt> subHost);
        bool ForwardFirst { get; }

        List<string> CurrentStateTag { get; }
        bool TryGetStateInfo(TEvt evt, out IStateInfo<TCont> stateInfo);
    }
}