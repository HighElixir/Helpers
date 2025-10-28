using System;

namespace HighElixir.Hedgeable
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
        T Value { get; set; }
        T MinValue { get; }
        T MaxValue { get; }

        /// <summary>
        /// 値の変化方向を取得する。
        /// </summary>
        int Direction { get; }

        /// <summary>
        /// この値を設定できるかどうかを判定する。
        /// </summary>
        bool CanSetValue(T newValue);

        TSelf SetMax(T maxValue);
        TSelf SetMin(T minValue);

        /// <summary>
        /// ヘッジ処理が行われたときに呼び出されるイベントを購読する。
        /// 1つめは変更前、2つめは変更後
        /// </summary>
        IDisposable Subscribe(Action<ChangeResult<T>> onValueChanged);
    }
}