using System;

namespace HighElixir 
{
    /// <summary>
    /// ダミーの型
    /// </summary>
    public sealed class UnknownType 
    { 
        public static Type Type => typeof(UnknownType);
        public static string Name => nameof(UnknownType);
    }
}
