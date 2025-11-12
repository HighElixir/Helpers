using System;
using System.Collections.Generic;

namespace HighElixir.Implements.Observables
{
    /// <summary>
    /// 外部依存なくRxライクな処理を実現するための簡易実装群
    /// <br/>Observable／Observerパターンを簡潔に利用できる拡張を提供する
    /// </summary>
    public static class ObservableExt
    {
        /// <summary>
        /// IObservableにActionベースの購読を追加する
        /// </summary>
        public static IDisposable Subscribe<T>(
            this IObservable<T> observable,
            Action<T> onNext,
            Action onComplete = null,
            Action<Exception> onError = null)
        {
            return observable.Subscribe(new ActionObserver<T>(onNext, onComplete, onError));
        }

        /// <summary>
        /// 値をフィルタリングするWhere拡張
        /// </summary>
        public static IObservable<T> Where<T>(this IObservable<T> observable, Func<T, bool> predicate, bool overWrite = false)
        {
            if (observable is ObservableWrapper<T> obs)
                return obs.SetPredicate(predicate, overWrite);
            else
                return new ObservableWrapper<T>(observable, predicate);
        }

        /// <summary>
        /// 指定回数分の値をスキップする
        /// </summary>
        public static IObservable<T> Skip<T>(this IObservable<T> observable, int count = 1)
        {
            if (observable is ObservableWrapper<T> obs)
                return obs.SetSkipCount(count);
            else
                return new ObservableWrapper<T>(observable, count);
        }

        /// <summary>
        /// 複数のIDisposableをまとめて破棄できるようにする
        /// </summary>
        public static IDisposable Join(this IDisposable source, params IDisposable[] disposables)
        {
            if (source is UniteDisposable unite)
                return unite.Add(disposables);
            else
                return new UniteDisposable(disposables);
        }
        public static IDisposable Join(params IDisposable[] disposables)
        {
            return new UniteDisposable(disposables);
        }

        private class UniteDisposable : IDisposable
        {
            private readonly List<IDisposable> _dis = new();

            public UniteDisposable(params IDisposable[] disposables) =>
                Add(disposables);

            public IDisposable Add(params IDisposable[] disposables)
            {
                _dis.AddRange(disposables);
                return this;
            }
            public void Dispose()
            {
                foreach (var disposable in _dis)
                    disposable?.Dispose();
                _dis.Clear();
            }
        }
    }
}