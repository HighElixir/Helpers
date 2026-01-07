using HighElixir.Pools;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighElixir.Unity.Pools
{
    public class ObjectPool<T> : IDisposable where T : UnityEngine.Object
    {
        private readonly Pool<T> _pool;
        private T _original;
        private Transform _container;

        public Pool<T> Pool => _pool;
        public ObjectPool(T original, int capacity, Transform container, bool lazyInit = false)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _original = original;
            _container = container ?? new GameObject($"{typeof(T).Name}_Pool").transform;
            _pool = new Pool<T>(
                () =>
                {
                    if (_container == null)
                    {
                        var go = new GameObject($"[{_original.name}]Container");
                        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
                        _container = go.transform;
                    }
                    var instance = UnityEngine.Object.Instantiate(_original, _container);
                    return instance;
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

        public T Get() => _pool.Get();

        public void Release(T obj) => _pool.Release(obj);

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