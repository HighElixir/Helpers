using System;

namespace HighElixir.Math.Hedgeable
{
    /// <summary>
    /// ヘッジ可能な要素を表すインターフェース。
    /// </summary>
    public interface IHedgeable<T, TSelf> : IComparable<TSelf>, IEquatable<TSelf>
        where TSelf : IHedgeable<T, TSelf>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        /// <summary>
        /// ヘッジ可能な値を取得する。
        /// </summary>
        T CurrentValue { get; set; }
        T MinValue { get; }
        T MaxValue { get; }

        /// <summary>
        /// 値の変化方向を取得する。
        /// </summary>
        int Direction { get; }

        void SetValue(T newValue);

        /// <summary>
        /// この値を設定できるかどうかを判定する。
        /// </summary>
        bool CanSetValue(T newValue);

        /// <summary>
        /// 値が設定可能であれば、新しい値を設定する。
        /// 設定できない場合、自動で補正されfalseを返す。
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        bool TrySetValue(T newValue);

        void SetMax(T maxValue);
        void SetMin(T minValue);
    }
}