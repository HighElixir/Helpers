using HighElixir.Pools;
using UnityEngine;

namespace HighElixir.Unity.Pools
{
    public abstract class SharedPool<T> : SingletonBehavior<SharedPool<T>>
        where T : UnityEngine.Object
    {
        [Header("PoolSettings")]
        [SerializeField] private T _pref;
        [SerializeField] private int _capacity;
        [SerializeField] private bool _lazy = true;

        private ObjectPool<T> _pool;

        public ObjectPool<T> Pool => _pool;

        protected override void Awake()
        {
            base.Awake();
            _pool = new(_pref, _capacity, transform, _lazy);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _pool.Dispose();
        }
    }
}