using System;

namespace HighElixir.Implements
{
    /// <summary>
    /// IDisposableを簡潔に生成・管理するためのユーティリティクラス
    /// <br/>Rxなどで使用されるDisposeパターンの軽量実装
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// 何もしない空のIDisposable
        /// <br/>明示的に破棄処理が不要な場合に使用する
        /// </summary>
        public static IDisposable Empty => Create(() => { });

        /// <summary>
        /// アクションを受け取り、Dispose時に実行する
        /// </summary>
        private class ActionDisposable : IDisposable
        {
            private readonly Action _action;
            private bool _disposed;

            /// <summary>
            /// アクションを指定して初期化
            /// </summary>
            internal ActionDisposable(Action action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }

            /// <summary>
            /// Dispose時に指定されたアクションを一度だけ実行する
            /// </summary>
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _action.Invoke();
            }
        }

        /// <summary>
        /// 指定したアクションをDispose時に呼び出すIDisposableを生成
        /// <br/>例: <c>Disposable.Create(() =&gt; Debug.Log("Dispose"));</c>
        /// </summary>
        public static IDisposable Create(Action action)
        {
            return new ActionDisposable(action);
        }
    }
}
