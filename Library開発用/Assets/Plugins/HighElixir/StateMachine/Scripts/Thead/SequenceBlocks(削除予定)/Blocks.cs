using System;
using System.Threading;
using System.Threading.Tasks;

namespace HighElixir.StateMachines.Thead.Blocks
{
    public static class Blocks
    {
        public static EventBlock<TCont, TEvt, TState> CreateBox<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm)
                => new(sm, (token) => Task.CompletedTask);
        #region === Send Block ===
        /// <summary>
        /// イベントを送信し、その完了を待機するイベントブロックを作成します。
        /// StateMachine.Send の戻り Task をそのまま使用するため、キャンセルや例外は Send に従います。
        /// </summary>
        public static EventBlock<TCont, TEvt, TState> SendBlock<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, TEvt evt)
                => new(sm, (token) => sm.Send(evt, token));

        /// <summary>
        /// 既存の EventBlock に対して子ブロックを追加し、直列/並列チェーンを構築できます。
        /// </summary>
        public static EventBlock<TCont, TEvt, TState> SendBlock<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, TEvt evt)
                => new(sender, (token) => sender.StateMachine.Send(evt, token));

        /// <summary>
        /// 指定ミリ秒だけ待機するブロックを作成します。
        /// 意図的に Task.Delay をラップしているため、CancellationToken を流す場合は Operate に token を渡してください。
        /// </summary>
        public static EventBlock<TCont, TEvt, TState> DelayBlock<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, int millisecondsDelay)
                => new(sm, (token) => Task.Delay(millisecondsDelay, token));
        public static EventBlock<TCont, TEvt, TState> DelayBlock<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, int millisecondsDelay)
                => new(sender, (token) => Task.Delay(millisecondsDelay, token));
        public static EventBlock<TCont, TEvt, TState> DelayBlock<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, TimeSpan timeSpan)
                => new(sm, (token) => Task.Delay(timeSpan, token));
        public static EventBlock<TCont, TEvt, TState> DelayBlock<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, TimeSpan timeSpan)
                => new(sender, (token) => Task.Delay(timeSpan, token));
        #endregion

        #region === Custom Block ===
        public static EventBlock<TCont, TEvt, TState> CustomAsyncBlock<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, Func<CancellationToken, Task> operation)
                => new(sm, operation);
        public static EventBlock<TCont, TEvt, TState> CustomAsyncBlock<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, Func<CancellationToken, Task> operation)
                => new(sender, operation);

        public static EventBlock<TCont, TEvt, TState> CustomActionBlock<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, Action operation)
                => new(sm, CreateTask(operation));
        public static EventBlock<TCont, TEvt, TState> CustomActionBlock<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, Action operation)
                => new(sender, CreateTask(operation));

        private static Func<CancellationToken, Task> CreateTask(Action action)
        {
            return new Func<CancellationToken, Task>( token =>
            {
                action();
                return Task.CompletedTask;
            });
        }
        #endregion

        #region === Wait Block ===
        public static EventBlock<TCont, TEvt, TState> WaitUntil<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, Func<bool> operation, int checkSpanMilliseconds = 16)
                => new(sm, GetPredicate(operation, checkSpanMilliseconds));
        public static EventBlock<TCont, TEvt, TState> WaitUntil<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, Func<bool> operation, int checkSpanMilliseconds = 16)
                => new(sender, GetPredicate(operation, checkSpanMilliseconds));
        public static EventBlock<TCont, TEvt, TState> WaitWhile<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> sm, Func<bool> operation, int checkSpanMilliseconds = 16)
                => new(sm, GetPredicate(operation, checkSpanMilliseconds, true));
        public static EventBlock<TCont, TEvt, TState> WaitWhile<TCont, TEvt, TState>(this EventBlock<TCont, TEvt, TState> sender, Func<bool> operation, int checkSpanMilliseconds = 16)
                => new(sender, GetPredicate(operation, checkSpanMilliseconds, true));

        private static Func<CancellationToken, Task> GetPredicate(Func<bool> action, int checkSpanMilliseconds, bool isWhile = false)
        {
            return new Func<CancellationToken, Task>(async token =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (action() ^ isWhile)
                    {
                        break;
                    }
                    await Task.Delay(checkSpanMilliseconds);
                }
            });
        }
        #endregion
    }
}