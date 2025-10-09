using UnityEngine;

namespace HighElixir.Pool
{
    public abstract class SharedPool<T> : SingletonBehavior<SharedPool<T>>
        where T : MonoBehaviour
    {
        [Header("PoolSettings")]
        [SerializeField] private T _pref;
        [SerializeField] private int _capacity;
        [SerializeField] private bool _lazy = true;

        private Pool<T> _pool;

        public Pool<T> Pool => _pool;

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