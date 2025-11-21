using HighElixir.StateMachine.Thead;
using System.Threading.Tasks;

namespace HighElixir.StateMachine
{
    public static class StateExecuter
    {
        public static async Task StateExit<TCont, TEvt, TState>(StateMachine<TCont, TEvt, TState>.StateInfo info)
        {
            if (info.State is StateAsync<TCont> fromAsync)
            {
                info._onTrans.Value = EventState.Exiting;
                await fromAsync.ExitAsync();
            }
            else
            {
                info._onTrans.Value = EventState.Exiting;
                info.State.Exit();
            }
        }
        public static async Task StateEnter<TCont, TEvt, TState>(StateMachine<TCont, TEvt, TState>.StateInfo info)
        {
            if (info.State is StateAsync<TCont> fromAsync)
            {
                info._onTrans.Value = EventState.Exiting;
                await fromAsync.EnterAsync();
            }
            else
            {
                info._onTrans.Value = EventState.Exiting;
                info.State.Enter();
            }
        }
    }
}