using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.WindowsMover.Wrappers
{
    static class EnumerableExtension
    {
        public static int FirstIndexMatch<TItem>(this IEnumerable<TItem> items, Func<TItem, bool> matchCondition)
        {
            if (items == null)
            {
                return -1;
            }
            var index = 0;
            foreach (var item in items)
            {
                if (matchCondition(item))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }
    }
}
