using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HighElixir
{
    public static class TextFilters
    {
        /// <summary>
        /// 入力文字列から「(数字)」パターンをすべて取り除く。(全角、半角対応)<br/>
        /// 例: "Hello(123)World(４５６)" -> "HelloWorld"
        /// </summary>
        public static string RemoveNumericTags(this string input)
            => string.IsNullOrEmpty(input)
                ? input ?? string.Empty
                : NumberTagPattern.Replace(input, string.Empty);
        public static void AutoRename(ref Dictionary<string, string> names)
        {
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();
            foreach (var key in names.Keys)
            {
                string originalBaseName = names[key].RemoveNumericTags();
                string baseName = originalBaseName.ToLower();
                if (nameCounts.ContainsKey(baseName))
                {
                    nameCounts[baseName]++;
                    names[key] = $"{originalBaseName}({nameCounts[baseName]})";
                }
                else
                {
                    nameCounts[baseName] = 0;
                    names[key] = originalBaseName;
                }
            }
        }

        private static readonly Regex NumberTagPattern =
            new(@"[\(（]\p{Nd}+[\)）]", RegexOptions.Compiled);
    }
}