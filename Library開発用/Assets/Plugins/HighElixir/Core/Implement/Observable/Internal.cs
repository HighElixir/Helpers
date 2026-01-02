using System;

namespace HighElixir.Implements.Observables
{
    /// <summary>
    /// Observableにフィルタやスキップなどを適用するラッパー
    /// </summary>
    internal class ObservableWrapper<T> : IObservable<T>
    {
        private readonly IObservable<T> _observable;
        private Func<T, bool> _predicate;
        private int _skip = 0;
        private int _count = 0;

        public ObservableWrapper(IObservable<T> observable, Func<T, bool> predicate)
        {
            _observable = observable;
            _predicate = predicate;
        }

        public ObservableWrapper(IObservable<T> observable, int count)
        {
            _observable = observable;
            _skip = count;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _observable.Subscribe(new ActionObserver<T>(
                x =>
                {
                    if ((_predicate == null || _predicate(x)) &&
                        (_skip == -1 || _count >= _skip))
                        observer?.OnNext(x);

                    if (_skip != 0)
                        _count++;
                },
                () => observer?.OnCompleted(),
                ex => observer?.OnError(ex)
            ));
        }

        /// <summary>Where条件を追加または上書きする</summary>
        public IObservable<T> SetPredicate(Func<T, bool> predicate, bool overWrite)
        {
            if (!overWrite)
                _predicate += predicate;
            else
                _predicate = predicate;

            return this;
        }

        /// <summary>スキップ回数を設定</summary>
        public IObservable<T> SetSkipCount(int skip)
        {
            _skip = skip;
            _count = 0;
            return this;
        }
    }

    /// <summary>
    /// ActionベースのObserver実装
    /// </summary>
    internal class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action _onCompleted;
        private readonly Action<Exception> _onError;

        public ActionObserver(Action<T> onNext, Action onCompleted = null, Action<Exception> onError = null)
        {
            _onNext = onNext;
            _onCompleted = onCompleted;
            _onError = onError;
        }

        public void OnCompleted() => _onCompleted?.Invoke();

        public void OnError(Exception error) => _onError?.Invoke(error);

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
