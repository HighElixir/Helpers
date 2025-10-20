using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HighElixir.Pools
{
    public class Pool<T> : IDisposable
    {
        private readonly Func<T> _createMethod;
        private readonly Action<T> _destroyMethod;
        private readonly ConcurrentQueue<T> _available = new();
        private readonly ConcurrentDictionary<T, bool> _inUse = new();
        private int _maxPoolSize;
        public bool Disposed { get; private set; } = false;
        public bool Initialized { get; private set; } = false;
        public bool AutoExpand { get; set; } = true;
        public int MaxCapacity { get; set; } = 10000;
        public int AvailableCount => _available.Count;
        public int InUseCount => _inUse.Count;
        public int TotalCount => AvailableCount + InUseCount;


        // 取得・解放するオブジェクトを操作したい時用
        public event Action<T> OnGetEvt;
        public event Action<T> OnReleaseEvt;
        public event Action<PooledObject<T>> OnAcquiredPooledEvt;

        // 生成、破棄時のイベント
        public event Action<T> OnCreateEvt;
        public event Action<T> OnDestroyEvt;

        public Pool(Func<T> createMethod, Action<T> destroyMethod, int maxPoolSize, bool lazyInit = false)
        {
            if (createMethod == null) throw new ArgumentNullException(nameof(createMethod));
            if (destroyMethod == null) throw new ArgumentNullException(nameof(destroyMethod));
            if (maxPoolSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxPoolSize));

            _createMethod = createMethod;
            _destroyMethod = destroyMethod;
            _maxPoolSize = maxPoolSize;
            if (!lazyInit) Initialize();
        }

        public void Initialize()
        {
            if (Initialized) return;
            Initialized = true;

            CreateToMaxPoolSize();
        }

        #region Get / Release
        public T Get()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Pool<T>));
            var obj = Get_Internal();
            OnGetEvt?.Invoke(obj);
            return obj;

        }

        public IPooledObject<T> GetAsPooled()
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Pool<T>));
            var obj = Get_Internal();
            var pooled = new PooledObject<T>(obj, () => Release(obj));
            OnAcquiredPooledEvt?.Invoke(pooled);
            return pooled;

        }

        public void Release(T obj)
        {
            if (Disposed) throw new ObjectDisposedException(nameof(Pool<T>));
            if (obj == null) return;

            if (_inUse.Remove(obj, out _))
            {
                OnReleaseEvt?.Invoke(obj);
                if (_available.Count < _maxPoolSize)
                    _available.Enqueue(obj);
                else
                    DestroyObject(obj);
            }
            else
            {
                LogWarning($"{obj} はプール外のオブジェクトです");
            }

        }


        private T Get_Internal()
        {
            // 残数がない場合は新規作成
            if (!_available.TryDequeue(out T obj))
                obj = CreateInstance();

            if (!_inUse.TryAdd(obj, true))
                LogWarning($"{obj} はすでに使用中です");
            return obj;
        }
        #endregion

        #region Settings

        // プールサイズの設定, 超過分は破棄、足りない分は生成
        public void SetPoolSize(int poolSize)
        {
            _maxPoolSize = poolSize;
            var extra = _available.Count - _maxPoolSize;
            var need = _maxPoolSize - (_available.Count + _inUse.Count);
            if (extra > 0)
            {
                for (int i = 0; i < extra; i++)
                {
                    if (_available.TryDequeue(out T g))
                        DestroyObject(g);
                }
            }
            else if (need > 0)
            {
                for (int i = 0; i < need; i++)
                    CreateInstance(false);
            }
        }
        #endregion

        #region 生成・破棄
        public void ReCreateAll()
        {
            foreach (var obj in _available)
                DestroyObject(obj);
            _available.Clear();
            foreach (var obj in _inUse.Keys)
                DestroyObject(obj);
            _inUse.Clear();
            CreateToMaxPoolSize();
        }
        private void CreateToMaxPoolSize()
        {
            while (TotalCount < _maxPoolSize)
                CreateInstance();
        }
        private T CreateInstance(bool enqueue = true)
        {
            if (TotalCount >= MaxCapacity)
                throw new InvalidOperationException($"[{nameof(T)}] MAX_CAPACITYを超過しました");

            var obj = _createMethod();
            if (AutoExpand && TotalCount > _maxPoolSize)
                _maxPoolSize = TotalCount;

            OnCreateEvt?.Invoke(obj);
            if (enqueue)
                _available.Enqueue(obj);
            return obj;
        }


        private void DestroyObject(T obj)
        {
            OnDestroyEvt?.Invoke(obj);
            _destroyMethod(obj);
        }
        #endregion

        // 全オブジェクトを破棄
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            foreach (var obj in _available)
                DestroyObject(obj);
            foreach (var obj in _inUse.Keys)
                DestroyObject(obj);
            _available.Clear();
            _inUse.Clear();

            // イベントを解除
            OnGetEvt = null;
            OnReleaseEvt = null;
            OnAcquiredPooledEvt = null;
            OnCreateEvt = null;
            OnDestroyEvt = null;
        }

        private void LogWarning(string message)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#elif DEBUG
    Console.WriteLine("[Pool Warning] " + message);
#endif
        }

    }
}
