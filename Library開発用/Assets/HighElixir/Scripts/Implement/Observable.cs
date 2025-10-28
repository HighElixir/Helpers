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
            return Disposable.Create(() =>
            {
                source?.Dispose();
                foreach (var disposable in disposables)
                    disposable?.Dispose();
            });
        }
    }

    /// <summary>
    /// 値を監視・通知できるプロパティ実装
    /// <br/>値が更新されるたびにOnNextを発行する
    /// </summary>
    public class ReactiveProperty<T> : IObservable<T>, IDisposable
    {
        protected T _value;
        private bool _isDisposed;
        private HashSet<IObserver<T>> _observers = new();

        /// <summary>現在の値（変更時に購読者へ通知）</summary>
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnNext();
            }
        }

        public ReactiveProperty(T value = default)
        {
            _value = value;
        }

        /// <summary>購読を開始し、現在値を即時通知</summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (_observers.Add(observer))
            {
                observer.OnNext(_value);
                return Disposable.Create(() =>
                {
                    if (_observers.Remove(observer))
                        observer?.OnCompleted();
                });
            }
            return null;
        }

        /// <summary>全購読者へ値を通知</summary>
        protected void OnNext()
        {
            try
            {
                foreach (var item in _observers)
                    item.OnNext(_value);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>全購読者へ例外を通知</summary>
        protected void OnError(Exception ex)
        {
            foreach (var item in _observers)
                item.OnError(ex);
        }

        /// <summary>リソース破棄（購読解除・値のDispose含む）</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (disposing && _observers != null)
            {
                foreach (var observer in _observers)
                    observer?.OnCompleted();

                _observers.Clear();
                _observers = null;
            }

            if (Value is IDisposable disposable)
                disposable.Dispose();
        }

        ~ReactiveProperty()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// 戻り値を持たないアクションをObservable化
    /// </summary>
    internal class ActionAsObservable : ReactiveProperty<byte>
    {
        public void Invoke() => OnNext();
    }

    /// <summary>
    /// コンテキスト付きアクションをObservable化
    /// </summary>
    internal class ActionAsObservable<TCont> : ReactiveProperty<TCont>
    {
        public void SetContext(TCont context) => _value = context;
        public void Invoke() => OnNext();
    }

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

            return _observable;
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
