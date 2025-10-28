using System;

namespace HighElixir
{
    /// <summary>
    /// 不明または未設定の型を表すダミークラス
    /// <br/>型情報が必要だが具体的な型が存在しない場合のプレースホルダーとして使用する
    /// </summary>
    public sealed class UnknownType
    {
        // 実際のTypeオブジェクト
        private static readonly Type _type;

        // 固定の型名
        private static readonly string _name;

        /// <summary>
        /// UnknownType の Type 情報
        /// </summary>
        public static Type Type => _type;

        /// <summary>
        /// UnknownType の名称 ("Unknown")
        /// </summary>
        public static string Name => _name;

        // 静的コンストラクタで初期化
        static UnknownType()
        {
            _type = typeof(UnknownType);
            _name = "Unknown";
        }
    }
}
