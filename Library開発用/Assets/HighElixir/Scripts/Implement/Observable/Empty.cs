using System;

namespace HighElixir.Implements.Observables
{
    public sealed class Empty<T> : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}