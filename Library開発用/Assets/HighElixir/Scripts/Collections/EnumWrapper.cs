using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace HighElixir
{
    public static class EnumWrapper
    {
        // おそらく重いので乱用は控える
        /// <typeparam name="T">enum</typeparam>
        /// <returns>特定の列挙型の全ての値を格納したリスト</returns>
        public static List<T> GetEnumList<T>(bool skippingNone = true) where T : Enum
        {
            var res = GetEnumerable<T>().ToList();
            if (skippingNone)
                res.RemoveAll(x => x.ToString().Contains("None", StringComparison.OrdinalIgnoreCase));
            return res;
        }

        public static HashSet<T> GetEnumHashSet<T>(bool skippingNone = true) where T : Enum
        {
            var res = GetEnumerable<T>().ToHashSet();
            if (skippingNone)
                res.RemoveWhere(x => x.ToString().Contains("None", StringComparison.OrdinalIgnoreCase));
            return res;
        }

        public static List<string> GetEnumNames<T>(bool skippingNone = true) where T : Enum
        {
            var res = Enum.GetNames(typeof(T)).ToList();
            if (skippingNone)
                res.RemoveAll(x => x.Contains("None", StringComparison.OrdinalIgnoreCase));
            return res;
        }
        public static Dictionary<T, string> GetDict<T>() where T : Enum
        {
            var values = GetEnumerable<T>();
            var names = GetEnumNames<T>();
            var dict = new Dictionary<T, string>();
            int i = 0;
            foreach (var v in values)
            {
                dict[v] = names[i];
                i++;
            }
            return dict;
        }

        private static IEnumerable<T> GetEnumerable<T>()
            where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}