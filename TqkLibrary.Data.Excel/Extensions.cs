using System;
using System.Collections.Generic;
using System.Text;

namespace TqkLibrary.Data.Excel
{
    internal static class Extensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> sources, params T[] values)
        {
            foreach (var item in sources)
                yield return item;
            foreach (var item in values)
                yield return item;
        }
        public static IEnumerable<T> Append<T>(this IEnumerable<T> sources, IEnumerable<T> values)
        {
            foreach (var item in sources)
                yield return item;
            foreach (var item in values)
                yield return item;
        }
    }
}
