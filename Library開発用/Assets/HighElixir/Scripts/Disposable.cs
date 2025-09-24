using System;

namespace HighElixir
{
    internal static class Disposable
    {
        private class ActionDisposable : IDisposable
        {
            private readonly Action _action;
            private bool _disposed;
            public ActionDisposable(Action action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _action.Invoke();
            }
        }
        public static IDisposable Create(Action action)
        {
            return new ActionDisposable(action);
        }
    }
}