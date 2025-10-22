using System;

namespace HighElixir.Implements
{
    public class NullObservable<T> : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnCompleted();
            return Disposable.Create(() => { });
        }
    }

    public class ActionObservable<T> : IObserver<T>
    {
        private Action<T> _onNext;
        private Action _onCompleted;
        private Action<Exception> _onError;

        public ActionObservable(Action<T> onNext, Action onCompleted = null, Action<Exception> onError = null)
        {
            _onNext = onNext;
            _onCompleted = onCompleted;
            _onError = onError;
        }
        public void OnCompleted()
        {
            _onCompleted?.Invoke();
        }

        public void OnError(Exception error)
        {
            _onError?.Invoke(error);
        }

        public void OnNext(T value)
        {
            try
            {
                _onNext?.Invoke(value);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }
    }
}