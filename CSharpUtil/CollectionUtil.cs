using System.Collections.Generic;
using System.Linq;

namespace CSharpUtil.CollectionUtil
{
    /// <summary>
    /// Null-safe enumeration helpers. Everything here leaves the non-null path untouched: no copy, no wrapper.
    /// </summary>
    public static class CollectionUtil
    {
        /// <summary>
        /// Returns the sequence itself, or an empty one when it is null, so a null check can be dropped before
        /// enumerating or chaining LINQ operators. Enumerable.Empty is a shared singleton, so nothing is allocated.
        /// </summary>
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> items)
        {
            return items ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// True when the collection is null or holds no element. Only countable collections are accepted:
        /// counting an arbitrary sequence would silently enumerate it a second time.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> items)
        {
            return items == null || items.Count == 0;
        }
    }
}
