using System.Collections.Generic;
using System.Linq;

namespace HighElixir.Collections
{
    /// <summary>
    /// リスト操作、要素の取得に関するヘルパークラス。
    /// </summary>
    public static class ItemPicker
    {
        /// <summary>
        /// リストからランダムに1つの要素を選ぶ。
        /// </summary>
        /// <typeparam name="T">リストの要素の型。</typeparam>
        /// <param name="values">要素を持つリスト。</param>
        /// <returns>ランダムに選ばれた要素。リストが空またはnullの場合はデフォルト値を返す。</returns>
        public static T RandomPick<T>(this List<T> values)
        {
            if (values == null || values.Count == 0) return default;
            return values[RandomExtensions.Rand(0, values.Count)];
        }

        public static T RandomPick<T>(this List<T> values, HashSet<T> exists)
        {
            if (values == null || values.Count == 0) return default;
            List<T> v = values.Where(item => !exists.Contains(item)).ToList();
            // もし未使用の要素がない場合は、デフォルト値を返す。
            if (v.Count == 0) return default;
            return RandomPick(v);
        }

        public static bool TryPickUnUsed<T>(this List<T> values, HashSet<T> exists, out T result)
        {
            result = values.RandomPick(exists);
            if (EqualityComparer<T>.Default.Equals(result, default(T)))
            {
                return false; // 未使用の要素がない場合はfalseを返す。
            }
            return true;
        }
        /// <summary>
        /// 特定の大きさを超過したアイテムをすべて返す.
        /// </summary>
        /// <param name="allowSize">許可されるリストの大きさ</param>
        /// <param name="res">最大許容量を超過したアイテム</param>
        /// <returns>超過していたかどうか</returns>
        public static bool TryGetOverItem<T>(this List<T> list, int allowSize, out List<T> res)
        {
            res = new List<T>();
            var temp = list.Count - allowSize;
            if (temp > 0)
            {
                var index = list.Count - 1;
                for (int i = 0; i < temp; i++)
                {
                    res.Add(list[index - i]);
                }
                res.Reverse();
                return true;
            }
            return false;
        }
        public static bool TryGetOverItem<T>(this Stack<T> stack, int allowSize, out List<T> res)
        {
            return TryGetOverItem(stack.ToList(), allowSize, out res);
        }
    }
}