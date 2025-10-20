using System;

namespace HighElixir
{
    /// <summary>
    /// UnityのHighElixir/RenameAssetsからフィールド名をファイル名に同期できます。<br />
    /// ファイル中最後の属性が優先されます。
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct, AllowMultiple = false)]
    public class LinkedFileNameAttribute : Attribute
    {

    }
}