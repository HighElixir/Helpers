using HighElixir.StateMachines.Thead;
using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachines
{
    public static class StateExecuter
    {
        public static async Task StateExit<TCont, TEvt, TState>(StateMachine<TCont, TEvt, TState>.StateInfo info, CancellationToken token = default)
        {
            if (info.State is StateAsync<TCont> fromAsync)
            {
                info._onTrans.Value = EventState.Exiting;
                await fromAsync.ExitAsync(token);
            }
            else
            {
                info._onTrans.Value = EventState.Exiting;
                info.State.Exit();
            }
        }
        public static async Task StateEnter<TCont, TEvt, TState>(StateMachine<TCont, TEvt, TState>.StateInfo info, CancellationToken token = default)
        {
            if (info.State is StateAsync<TCont> fromAsync)
            {
                info._onTrans.Value = EventState.Exiting;
                await fromAsync.EnterAsync(token);
            }
            else
            {
                info._onTrans.Value = EventState.Exiting;
                info.State.Enter();
            }
        }
    }
}