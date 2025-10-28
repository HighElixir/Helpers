using System;

namespace HighElixir.SearchSystems
{
    /// <summary>
    /// TODO: エディタからプロファイルを操作する機能実装
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProfileSizeAttribute : Attribute
    {
        public float Height { get; set; }
        public float Width { get; set; }
        public bool Expandable { get; set; }
    }
}