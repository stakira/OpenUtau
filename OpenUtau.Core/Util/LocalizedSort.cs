using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OpenUtau.Core.Util {

    public class LocalizedComparer : IComparer<string> {
        CultureInfo cultureInfo;
        public LocalizedComparer(CultureInfo cultureInfo) {
            this.cultureInfo = cultureInfo;
        }

        public int Compare(string x, string y) {
            return cultureInfo.CompareInfo.Compare(x, y);
        }
    }
    public static class LocalizedSort {
        public static IEnumerable<T> LocalizedOrderBy<T>(this IEnumerable<T> source, Func<T, string> selector) {
            var sortingOrder = Preferences.Default.SortingOrder;
            if(sortingOrder == String.Empty) {
                sortingOrder = Preferences.Default.Language;
            }
            var comparer = new LocalizedComparer(CultureInfo.GetCultureInfo(sortingOrder));
            return source.OrderBy(selector, comparer);
        }
    }
}
