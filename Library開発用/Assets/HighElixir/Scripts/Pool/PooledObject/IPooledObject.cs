using System;

namespace HighElixir.Pools
{
    /// <summary>
    /// プールから取得された一時的なオブジェクトを表す。
    /// Dispose() によって自動的にプールへ返却される。
    /// </summary>
    public interface IPooledObject<T> : IDisposable
    {
        /// <summary>使用中のオブジェクト本体。</summary>
        T Value { get; }
    }

}