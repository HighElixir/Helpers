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

        private static readonly Regex NumberTagPattern =
            new(@"[\(（]\p{Nd}+[\)）]", RegexOptions.Compiled);
    }
}