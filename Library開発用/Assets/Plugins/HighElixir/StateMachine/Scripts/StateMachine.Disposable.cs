using System;

namespace HighElixir.StateMachines
{
    public sealed partial class StateMachine<TCont, TEvt, TState> : IDisposable
    {
        #region Dispose Management
        public bool IsDisposed => _disposed;

        /// <summary> 破棄処理。内部リソースを解放。 </summary>
        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                foreach (var item in _states.Values)
                    item.Dispose();

                _cont = default;
                _executor.AnyTransition.Clear();
                _states.Clear();
                _onTransition.Dispose();
                _disposed = true;
                _parent = null;
            }
        }

        ~StateMachine() => Dispose(false);
#endregion
    }
}