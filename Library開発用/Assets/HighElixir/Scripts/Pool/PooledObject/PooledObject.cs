using System;

namespace HighElixir.Pools
{
    public readonly struct PooledObject<T> : IPooledObject<T>
    {
        private readonly T _value;
        private readonly Action _onDispose;
        public T Value => _value;
        internal PooledObject(T value, Action onDispose)
        {
            _value = value;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}