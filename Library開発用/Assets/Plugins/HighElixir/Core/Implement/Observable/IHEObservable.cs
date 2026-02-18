using System;

namespace HighElixir.Implements.Observables
{
    public interface IHEObservable<T> : IObservable<T>
    {
        void UnSubscribe(IObserver<T> observer);
    }
}