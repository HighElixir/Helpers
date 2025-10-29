using System;
using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachine.Extention
{
    public static class SendEventExt
    {
        public static void DelaySendEvt<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> stateMachine, TimeSpan time, TEvt evt, CancellationToken token = default)
        {
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(time, token);
                if (!token.IsCancellationRequested)
                    stateMachine.Send(evt);
            });
        }

        public static async Task DelaySendEvtAsync<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> stateMachine, TimeSpan time, TEvt evt, CancellationToken token = default)
        {
            await Task.Delay(time, token);
            if (!token.IsCancellationRequested)
                stateMachine.Send(evt);
        }
    }
}