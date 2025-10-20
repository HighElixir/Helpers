using System;
using UnityEngine;

namespace HighElixir.Pools
{
    public class ObjectPool<T> : IDisposable where T : UnityEngine.Object
    {
        private readonly Pool<T> _pool;
        private T _original;
        private Transform _container;

        public ObjectPool(T original, int capacity, Transform container, bool lazyInit = false)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _original = original;
            _container = container ?? new GameObject($"{typeof(T).Name}_Pool").transform;
            _pool = new Pool<T>(
                () =>
                {
                    if (_original is GameObject go)
                    {
                        var instance = UnityEngine.Object.Instantiate(go, _container);
                        return instance as T;
                    }
                    else if (_original is Component comp)
                    {
                        var instance = UnityEngine.Object.Instantiate(comp, _container);
                        return instance as T;
                    }
                    return null;
                },
                (obj) =>
                {
                    UnityEngine.Object.Destroy(obj);
                },
                capacity,
                lazyInit
            );

            // イベント登録
            _pool.OnGetEvt += (obj) =>
            {
                SetActive(obj, true);
            };
            _pool.OnReleaseEvt += (obj) =>
            {
                SetActive(obj, false);
                SetParent(obj);
            };
            _pool.OnCreateEvt += (obj) =>
            {
                SetActive(obj, false);
                SetParent(obj);
            };
            _pool.OnAcquiredPooledEvt += (pooled) =>
            {
                SetActive(pooled.Value, true);
            };
            if (!lazyInit)
                _pool.Initialize();
        }

        #region Unity Object Handling
        private void SetActive(T obj, bool active)
        {
            if (obj is GameObject go)
                go.SetActive(active);
            else if (obj is Component comp)
                comp.gameObject.SetActive(active);
        }

        private void SetParent(T obj)
        {
            if (obj is GameObject go)
                go.transform.SetParent(_container, false);
            else if (obj is Component comp)
                comp.transform.SetParent(_container, false);
        }
        #endregion

        public void SetOriginal(T original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _original = original;
            _pool.ReCreateAll();
        }
        public void Dispose()
        {
            _pool.Dispose();
            _container = null;
            _original = null;
        }

        public static explicit operator Pool<T>(ObjectPool<T> objectPool)
        {
            return objectPool._pool;
        }
    }
}