using System;

namespace HighElixir.Implements
{
    public class NullObservable<T> : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Disposable.Create(() => { });
        }
    }
}