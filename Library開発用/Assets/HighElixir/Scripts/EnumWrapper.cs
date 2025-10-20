using System;
using System.Collections.Generic;
using System.Linq;

namespace HighElixir
{
    public static class EnumWrapper
    {
        public static IReadOnlyList<T> GetEnumList<T>(bool skipDefault = true) where T : struct, Enum =>
            GetEnumerable<T>(skipDefault).ToList();

        public static HashSet<T> GetEnumHashSet<T>(bool skipDefault = true) where T : struct, Enum =>
            GetEnumerable<T>(skipDefault).ToHashSet();

        public static IReadOnlyList<string> GetEnumNames<T>(bool skipDefault = true) where T : struct, Enum =>
            GetEnumerable<T>(skipDefault).Select(v => Enum.GetName(typeof(T), v)!).ToList();

        public static Dictionary<T, string> GetValueNameMap<T>(bool skipDefault = true) where T : struct, Enum =>
            GetEnumerable<T>(skipDefault).ToDictionary(v => v, v => Enum.GetName(typeof(T), v)!);
        public static Dictionary<string, T> GetNameValueMap<T>(bool skipDefault = true)
    where T : struct, Enum =>
    GetEnumerable<T>(skipDefault)
        .ToDictionary(v => Enum.GetName(typeof(T), v)!, v => v);

        public static IEnumerable<T> GetEnumerable<T>(bool skipDefault) where T : struct, Enum
        {
#if NET5_0_OR_GREATER
            var values = Enum.GetValues<T>();
#else
            var values = (T[])Enum.GetValues(typeof(T));
#endif
            return skipDefault
                ? values.Where(v => Convert.ToInt64(v) != 0) // “0=デフォルト(None)” を除外
                : values;
        }
    }
}
