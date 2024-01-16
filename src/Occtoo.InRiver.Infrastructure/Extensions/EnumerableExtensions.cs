using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Infrastructure.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
            int maxItems)
        {
            return items.Select((item, inx) => new { item, inx })
                .GroupBy(x => x.inx / maxItems)
                .Select(g => g.Select(x => x.item));
        }

        public static bool EqualsIgnoreOrder<T>(this IEnumerable<T> list, IEnumerable<T> matchingList, IEqualityComparer<T> comparer = null)
        {
            if (list is ICollection<T> collection && matchingList is ICollection<T> matchingCollection && collection.Count != matchingCollection.Count)
                return false;

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;

            var itemCounts = new Dictionary<T, int>(comparer);
            foreach (var s in list)
            {
                if (itemCounts.ContainsKey(s))
                {
                    itemCounts[s]++;
                }
                else
                {
                    itemCounts.Add(s, 1);
                }
            }
            foreach (var s in matchingList)
            {
                if (itemCounts.ContainsKey(s))
                {
                    itemCounts[s]--;
                }
                else
                {
                    return false;
                }
            }
            return itemCounts.Values.All(c => c == 0);
        }
    }
}