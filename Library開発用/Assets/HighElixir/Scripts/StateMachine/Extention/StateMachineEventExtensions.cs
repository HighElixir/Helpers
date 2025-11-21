using System;
using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachine.Extension
{
    public static class StateMachineEventExtensions
    {
        #region Send Event with Delay
        public static async Task SendEventWithDelayAsync<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TimeSpan delay,
            TEvt evt,
            CancellationToken token = default)
        {
            await Task.Delay(delay, token);
            if (!token.IsCancellationRequested)
                await stateMachine.Send(evt);
        }

        public static async Task SendEventWithDelayAsync<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            float milliseconds,
            TEvt evt,
            CancellationToken token = default)
            => await stateMachine.SendEventWithDelayAsync(
                TimeSpan.FromMilliseconds(milliseconds), evt, token);
        #endregion

        #region Send Event and Temporarily Lock Exit

        // アンロックイベントは常に呼ばれる
        public static async Task SendEventAndLockExitAsync<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TEvt evt,
            TimeSpan lockDuration,
            CancellationToken token = default,
            Action onUnlock = null)
            => await stateMachine.SendEventAndLockExitAsync(evt, lockDuration, DefaultLockGuard, token, onUnlock);

        // アンロックイベントは常に呼ばれる
        public static async Task SendEventAndLockExitAsync<TCont, TEvt, TState>(
            this StateMachine<TCont, TEvt, TState> stateMachine,
            TEvt evt,
            TimeSpan lockDuration,
            Func<EventState, bool> lockPredicate,
            CancellationToken token = default,
            Action onUnlock = null)
        {
            if (stateMachine.TryGetStateInfo(evt, out IStateInfo<TCont> stateInfo))
            {
                if (!await stateMachine.Send(evt))
                    return;

                stateInfo.AllowTrans += lockPredicate;
                await Task.Delay(lockDuration, token);
                if (stateInfo != null)
                    stateInfo.AllowTrans -= lockPredicate;
                onUnlock?.Invoke();
            }
        }

        private static bool DefaultLockGuard(EventState stateInfo) => false;
        #endregion
    }
}
