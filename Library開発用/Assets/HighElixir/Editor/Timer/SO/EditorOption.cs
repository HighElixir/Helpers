using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HighElixir.Editors.Timers
{
    /// <summary>
    /// タイマー用エディタの表示設定などを保持する ScriptableObject。
    /// ・ソートモード
    /// ・昇順/降順
    /// ・ローカライズフラグ（未実装）
    /// ・各タイマー型ごとのカスタムテキスト
    /// などをまとめて保持する。
    /// </summary>
    public sealed class TimeEditorOption : ScriptableObject
    {
        /// <summary>
        /// エディタ用設定アセットの固定パス。
        /// </summary>
        public static readonly string CreatePath = "Assets/HighElixir/Editor/Timer/EditorData.asset";

        /// <summary>タイマーリストのソートモード。</summary>
        public TimeInspector.SortMode SortMode = TimeInspector.SortMode.ParentType;

        /// <summary>昇順なら true、降順なら false。</summary>
        public bool SortAscending = true;

        /// <summary>日本語表示するかどうか（現状未使用）。</summary>
        public bool IsJapanese = true;

        /// <summary>タイマー型ごとのカスタムテキスト定義。</summary>
        public List<CustomText> CustomTexts = new List<CustomText>();

        /// <summary>
        /// カスタムパレット
        /// </summary>
        public bool EnableCustomColor = false;
        public string ColorDataPath = string.Empty;
        public ParentColor ParentColor;

        /// <summary>
        /// 指定の型と引数から、カスタムテキストを取得する。
        /// </summary>
        public bool TryGetText(Type type, float arg, out string message)
        {
            // TimerType が一致（または基底クラス一致）する最初のものを取得
            var text = CustomTexts.FirstOrDefault(x => x.TypeEquals(type));
            if (text == null)
            {
                message = string.Empty;
                return false;
            }

            message = text.Create(arg);
            return true;
        }
    }

    /// <summary>
    /// 特定のタイマー型に紐づくテキスト定義。
    /// ツールチップ等の文言生成に使用する。
    /// </summary>
    [Serializable]
    public class CustomText
    {
        /// <summary>対象となるタイマー型（MonoScript 経由で指定）。</summary>
        public MonoScript TimerType;

        /// <summary>数値の前につけるテキスト。</summary>
        public string PreText;

        /// <summary>数値の後ろにつけるテキスト。</summary>
        public string PostText;

        /// <summary>
        /// Arg を使わない表示にしたいときに使用する完全上書きテキスト。
        /// これが設定されている場合、Pre/Post/arg は無視される。
        /// </summary>
        [Tooltip("Argを不使用のツールチップを作成する場合に使用")]
        public string OverriddenText;

        /// <summary>
        /// 指定の Type が、この CustomText の対象型と互換かを判定する。
        /// （TimerType のクラスか、その派生クラスを対象とする）
        /// </summary>
        public bool TypeEquals(Type type)
        {
            if (TimerType == null) return false;

            var clazz = TimerType.GetClass();
            if (clazz == null) return false;

            // 完全一致 or 派生クラスなら対象とみなす
            return type == clazz || type.IsSubclassOf(clazz);
        }

        /// <summary>
        /// 引数 arg を用いてメッセージを生成。
        /// OverriddenText が設定されている場合はそれを優先する。
        /// </summary>
        public string Create(float arg)
        {
            if (string.IsNullOrEmpty(OverriddenText))
            {
                return $"{PreText}{arg}{PostText}";
            }
            return OverriddenText;
        }
    }
}
