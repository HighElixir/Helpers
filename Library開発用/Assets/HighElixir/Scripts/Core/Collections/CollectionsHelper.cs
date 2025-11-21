using System.Collections.Generic;

namespace HighElixir.Collections
{
    public static class CollectionsHelper
    {
        public static void AddRange<T>(this HashSet<T> values, IEnumerable<T> adds)
        {
            foreach (var item in adds)
            {
                values.Add(item);
            }
        }
    }
}