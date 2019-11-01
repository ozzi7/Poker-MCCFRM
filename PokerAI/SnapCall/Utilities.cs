using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapCall
{
    public static class Utilities
    {
        public static uint? BinarySearch<T>(this IList<T> list, IComparable<T> item)
        {
            uint low = 0;
            uint high = (uint)list.Count - 1;

            while (true)
            {
                if (low > high)
                {
                    return null;
                }
                uint index = ((low + high) / 2);
                var comparison = item.CompareTo(list.ElementAt((int)index));
                if (comparison > 0) low = index + 1;
                else if (comparison < 0) high = index - 1;
                else return index;
            }
        }

        public static int? BinaryInsert<T>(this IList<T> list, IComparable<T> item)
        {
            int low = 0;
            int high = list.Count - 1;

            while (true)
            {
                if (low > high)
                {
                    list.Insert(low, (T)item);
                    return low;
                }
                int index = (int)((low + high) / 2);
                var comparison = item.CompareTo(list.ElementAt(index));
                if (comparison > 0) low = index + 1;
                else if (comparison < 0) high = index - 1;
                else return null;
            }
        }
    }
}