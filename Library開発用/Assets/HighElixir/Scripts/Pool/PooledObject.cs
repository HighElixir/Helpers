using System;

namespace HighElixir.Pool
{
    public struct PooledObject<T> : IDisposable
        where T : UnityEngine.Object
    {
        public T Value;
        public Pool<T> Pool;
        public PooledObject(T value, Pool<T> pool)
        {
            Value = value;
            Pool = pool;
        }

        public void Dispose()
        {
            Pool.Release(Value);
        }
    }
}