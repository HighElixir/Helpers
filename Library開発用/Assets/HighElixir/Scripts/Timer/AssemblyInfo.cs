using System.Runtime.CompilerServices;

#if UNITY_2017_1_OR_NEWER
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("HighElixir.Editor")]
#endif
[assembly: InternalsVisibleTo("HighElixir.Unity")]
#endif