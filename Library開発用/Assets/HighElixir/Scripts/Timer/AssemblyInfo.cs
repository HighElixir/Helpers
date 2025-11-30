using System.Runtime.CompilerServices;

// アセンブリ間でinternalメンバーを共有するための設定
#if UNITY_2017_1_OR_NEWER
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("HighElixir.Editor.Timers")]
#endif
[assembly: InternalsVisibleTo("HighElixir.Unity.Timers")]
#endif