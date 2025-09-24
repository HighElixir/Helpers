using System;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.Pool
{
    public class Pool<T> : IDisposable where T : UnityEngine.Object
    {
        private readonly T _original;
        private readonly Stack<T> _available = new();
        private readonly HashSet<T> _inUse = new();
        private int _maxPoolSize;
        private Transform _container;

        public Transform Container => _container;
        public List<T> InUse => new List<T>(_inUse);
        public bool Initialized { get; private set; } = false;
        public Pool(T original, int maxPoolSize, Transform container = null, bool LazeCreate = false)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (maxPoolSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxPoolSize));

            _original = original;
            _maxPoolSize = maxPoolSize;
            _container = container;
            if (!LazeCreate) Initialize();
        }

        public void Initialize()
        {
            Dispose();
            CreateInstances(_maxPoolSize);
            Initialized = true;
        }
        public T Get()
        {
            T obj = _available.Count > 0
                ? _available.Pop()
                : CreateInstance();

            if (!_inUse.Add(obj))
                Debug.LogWarning($"{obj.name} はすでに使用中です", obj);

            SetActive(obj, true);
            return obj;
        }

        public PooledObject<T> GetPooled()
        {
            var pooled = new PooledObject<T>(Get(), this);
            return pooled;
        }
        public void Release(T obj)
        {
            if (obj == null) return;
            if (_available.Contains(obj)) return;

            if (_inUse.Remove(obj))
            {
                SetActive(obj, false);
                SetParent(obj, _container);
                if (_available.Count < _maxPoolSize)
                    _available.Push(obj);
                else
                    DestroyObject(obj);
            }
            else
            {
                Debug.LogWarning($"{obj.name} はプール外のオブジェクトです", obj);
            }
        }

        public void Dispose()
        {
            foreach (var obj in _available)
                DestroyObject(obj);
            foreach (var obj in _inUse)
                DestroyObject(obj);
            _available.Clear();
            _inUse.Clear();
        }
        public void SetPoolSize(int poolSize)
        {
            _maxPoolSize = poolSize;
            var extra = _available.Count - _maxPoolSize;
            var need = _maxPoolSize - (_available.Count + _inUse.Count);
            if (extra > 0)
            {
                for (int i = 0; i < extra; i++)
                {
                    var g = _available.Pop();
                    DestroyObject(g);
                }
            }
            else if (need > 0)
            {
                CreateInstances(need);
            }
        }
        public void SetContainer(Transform container)
        {
            _container = container;
            foreach (var obj in _available)
                SetParent(obj, _container);
        }
        // 👅 GameObject / Component 両対応の生成処理
        private void CreateInstances(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = CreateInstance();
                SetActive(obj, false);
                _available.Push(obj);
            }
        }
        private T CreateInstance()
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

            throw new InvalidOperationException($"PoolはGameObjectまたはComponentにしか対応していません: {typeof(T)}");
        }

        private void SetActive(T obj, bool active)
        {
            if (obj is GameObject go)
                go.SetActive(active);
            else if (obj is Component comp)
                comp.gameObject.SetActive(active);
        }

        private void SetParent(T obj, Transform parent)
        {
            if (obj is GameObject go)
                go.transform.SetParent(parent, false);
            else if (obj is Component comp)
                comp.transform.SetParent(parent, false);
        }
        private static void DestroyObject(T obj)
        {
            if (obj is GameObject go) UnityEngine.Object.Destroy(go);
            else if (obj is Component comp) UnityEngine.Object.Destroy(comp.gameObject);
            else UnityEngine.Object.Destroy(obj); // 念のため
        }
    }
}
