using System;
using System.Collections.Concurrent;

namespace HighElixir.Implements.Observables
{
    // 型コンバートを行う
    public static class TypeConverter
    {
        public static IObservable<TTo> Convert<TFrom, TTo>(this IObservable<TFrom> source, Func<TFrom, TTo> converter)
            => new TypeConverter_Internal<TFrom, TTo>(source, converter);
    }
    internal class TypeConverter_Internal<TFrom, TTo> : IObservable<TTo>, IDisposable
    {
        private readonly ConcurrentDictionary<IObserver<TTo>, bool> _observers = new();
        private readonly IObservable<TFrom> _source;
        private Func<TFrom, TTo> _converter;
        private IDisposable _sourceDisposer;
        private TTo _value;
        private volatile bool _hasValue = false;
        private volatile bool _isDisposed = false;
        public TypeConverter_Internal(IObservable<TFrom> source, Func<TFrom, TTo> converter)
        {
            if (source == null) throw new ArgumentNullException("source is Null");
            if (converter == null) throw new ArgumentNullException("converter is Null");
            _source = source;
            _converter = converter;
            _sourceDisposer = _source.Subscribe(OnNext, OnCompleted, OnError);
        }
        public IDisposable Subscribe(IObserver<TTo> observer)
        {
            if (_observers.TryAdd(observer, true))
            {
                if (_hasValue)
                    observer.OnNext(_value);
                return Disposable.Create(() =>
                {
                    observer.OnCompleted();
                    _observers.TryRemove(observer, out _);
                });
            }
            return Disposable.Empty;
        }

        private void OnNext(TFrom value)
        {
            if (_isDisposed) return;
            _value = _converter(value);
            _hasValue = true;
            try
            {
                var obses = _observers.Keys;
                foreach (var obs in obses)
                {
                    obs.OnNext(_value);
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void OnError(Exception ex)
        {
            if (_isDisposed) return;
            try
            {
                var obses = _observers.Keys;
                foreach (var obs in obses)
                {
                    obs.OnError(ex);
                }
            }
            catch { /* 握りつぶし */ }
        }

        public void OnCompleted()
        {
            if (_isDisposed) return;
            try
            {
                var obses = _observers.Keys;
                foreach (var obs in obses)
                {
                    obs.OnCompleted();
                }
            }
            catch { /* 握りつぶし */ }
        }

        public void Dispose()
        {
            OnCompleted();
            _isDisposed = true;
            _observers.Clear();
            _sourceDisposer.Dispose();
            _sourceDisposer = null;
            _converter = null;
        }
    }
}