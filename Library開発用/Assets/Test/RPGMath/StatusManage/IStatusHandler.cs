using System;
using System.Collections.Generic;

namespace HighElixir.RPGMath.StatusManage
{
    /// <summary>
    /// ステータス(例：HP)の振る舞いを管理するインターフェース。
    /// </summary>
    // TODO : 一時的な値の増減の管理方法の検討
    public interface IStatusHandler<T>
        where T : IEquatable<T>
    {
        float Base { get; } // Initialized時に設定される基礎値（変更したい場合はUpdateBaseを使用する）
        float Current { get; }
        float FixedAmount { get; }
        bool ShouldBeRecalculated { get; }
        IReadOnlyList<ParentRule<T>> ParentRules { get; }
        StatusManager<T> StatusManager { get; set; }

        void RecalculateCurrent();
        void Increase(float value);
        void Decrease(float value);
        void Set(float value);
        void ForceSet(float value);
        void ResetToBase();
        void UpdateBase(float newValue);
        void Initialize(float initialValue, params ParentRule<T>[] rules);
    }
}