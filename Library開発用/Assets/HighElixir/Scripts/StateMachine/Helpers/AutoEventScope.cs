using System;

namespace HighElixir.StateMachine.Helpers
{
    public static class AutoEventScope
    {
        public  static IDisposable SendWith<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, TEvt evt, TEvt after, bool isLazy)
        {
            s.Send(evt);
            return new Sender<TCont, TEvt, TState>(s, after, isLazy);
        }
        public  static IDisposable LazySendWith<TCont, TEvt, TState>(this StateMachine<TCont, TEvt, TState> s, TEvt evt, TEvt after, bool isLazy)
        {
            s.LazySend(evt);
            return new Sender<TCont, TEvt, TState>(s, after, isLazy);
        }
    }

    internal class Sender<TCont, TEvt, TState> : IDisposable
    {
        private readonly StateMachine<TCont, TEvt, TState> _state;
        private readonly TEvt _event;
        private readonly bool _isLazy;
        private bool _disposed;

        public Sender(StateMachine<TCont, TEvt, TState> state, TEvt evt, bool isLazy)
        {
            _state = state;
            _event = evt;
            _isLazy = isLazy;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (!_isLazy)
                _state.Send(_event);
            else
                _state.LazySend(_event);
            _disposed = true;
        }
    }
}