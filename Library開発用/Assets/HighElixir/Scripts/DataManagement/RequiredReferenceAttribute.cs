using System;

namespace HighElixir.DataManagements
{
    /// <summary>
    /// 他のDef型への参照を必須とすることを示す属性。
    /// この属性を付けると、ReferenceResolverが後で参照を解決する。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class RequiredReferenceAttribute : Attribute
    {
        /// <summary>
        /// 参照先の型（例：typeof(BulletDef)）
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// 参照先を特定するためのDefName文字列を保持しているフィールド名。
        /// （例："BulletType"）
        /// </summary>
        public string DefNameField { get; }

        public RequiredReferenceAttribute(Type targetType, string defNameField)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            DefNameField = defNameField ?? throw new ArgumentNullException(nameof(defNameField));
        }
    }
}
