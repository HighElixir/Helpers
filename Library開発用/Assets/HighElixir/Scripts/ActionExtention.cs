using HighElixir.Implements;
using HighElixir.Implements.Observables;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace HighElixir
{
    public static class ActionExtension
    {
        #region Subscribe
        /// <summary>
        /// Actionイベントをリアクティブ購読します。
        /// </summary>
        /// <param name="evt">購読対象のAction。instanceのフィールドとして定義されている必要があります。</param>
        /// <param name="instance">evtを定義しているオブジェクト</param>
        /// <param name="onNext">アクションが呼び出された時の動作</param>
        /// <param name="onComplete">Dispose時に呼び出されます</param>
        /// <param name="onError">エラー時のハンドリングを設定できます。nullの場合、エラーを再スローします</param>
        public static IDisposable SubscribeAs(this Action evt, object instance, Action onNext, Action onComplete = null, Action<Exception> onError = null)
        {
            Action<object> ac = _ => onNext();
            Action<object> et = _ => evt();
            var eventName = evt.Method.Name;
            return SubscribeAs_Internal<object>(et, eventName, instance, ac, onComplete, onError);
        }

        /// <summary>
        /// Actionイベントをリアクティブ購読します。
        /// </summary>
        /// <param name="evt">購読対象のAction。instanceのフィールドとして定義されている必要があります。</param>
        /// <param name="instance">evtを定義しているオブジェクト</param>
        /// <param name="onNext">アクションが呼び出された時の動作</param>
        /// <param name="onComplete">Dispose時に呼び出されます</param>
        /// <param name="onError">エラー時のハンドリングを設定できます。nullの場合、エラーを再スローします</param>
        public static IDisposable SubscribeAs<T>(this Action<T> evt, object instance, Action<T> onNext, Action onComplete = null, Action<Exception> onError = null)
        {
            var eventName = evt.Method.Name;
            return SubscribeAs_Internal<T>(evt, eventName, instance, onNext, onComplete, onError);
        }

        private static IDisposable SubscribeAs_Internal<T>(this Action<T> evt, string methodName, object instance, Action<T> onNext, Action onComplete = null, Action<Exception> onError = null)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            onComplete ??= () => { };
            onError ??= (ex) => ExceptionDispatchInfo.Capture(ex).Throw();
            // フィールド or イベント情報を取得
            var type = instance.GetType();
            EventInfo eventInfo = null;
            var obs = new ActionObserver<T>(x => onNext(x), onComplete, onError);
            eventInfo = type.GetEvent(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (eventInfo == null)
                throw new InvalidOperationException($"イベント '{methodName}' が {type.Name} に見つかりません。");

            // 登録
            Action ac = () => obs.OnNext(default(T));
            eventInfo.AddEventHandler(instance, ac);

            // 解除用Disposableを返す
            return Disposable.Create(() =>
            {
                eventInfo.RemoveEventHandler(instance, ac);
                obs.OnCompleted();
            });
        }
        #endregion
    }
}
