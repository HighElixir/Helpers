using System;
using System.Collections.Generic;

namespace HighElixir.Implements.Observables
{
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
}