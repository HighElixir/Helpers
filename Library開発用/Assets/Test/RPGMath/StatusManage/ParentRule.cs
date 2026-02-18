using System;

namespace HighElixir.RPGMath.StatusManage
{
    /// <summary>
    /// 参照すべきステータスのルールを定義する構造体。</br>
    /// なお、自己参照や見つけられなかった場合は無視される。
    /// </summary>
    public readonly struct ParentRule<T>
        where T : IEquatable<T>
    {
        public T Key { get; }
        public float UnderRate { get; }
        public float OverRate { get; }
        public float SyncRate { get; }
        public int Priority { get; } // 大きいほうを優先

        public SyncHandleMode RuleType { get; }

        public ParentRule(
            T key,
            SyncHandleMode ruleType,
            float underRate = 1.0f,
            float overRate = 1.0f,
            float syncRate = 1.0f,
            int priority = 1)
        {
            Key = key;
            RuleType = ruleType;
            UnderRate = underRate;
            OverRate = overRate;
            SyncRate = syncRate;
            Priority = priority;
        }
    }

    public enum SyncHandleMode : byte
    {
        Under, // 親を下限値として参照
        Over, // 親を上限値として参照
        FullSync,　// 親と同期
        Wrap // 親を上下限値として参照（同期はしない）
    }
}