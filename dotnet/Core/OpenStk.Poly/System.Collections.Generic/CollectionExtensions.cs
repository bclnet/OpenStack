namespace System.Collections.Generic;

public static class CollectionExtensions {
    public static int BinarySearchLowerBound<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key) where TKey : IComparable<TKey> {
        if (list.Count == 0) return -1;
        int min = 0, max = list.Count - 1;
        while (min < max) {
            var mid = (max + min) / 2;
            T midItem = list[mid];
            var midKey = keySelector(midItem);
            var comp = midKey.CompareTo(key);
            if (comp < 0) min = mid + 1;
            else if (comp > 0) max = mid - 1;
            else return mid;
        }
        // return something corresponding to lower_bound semantics
        // if min is higher than key, return min - 1. Otherwise, when min is <=key, return min directly.
        return keySelector(list[min]).CompareTo(key) > 0 ? min - 1 : min;
    }

    public static T BinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key) where TKey : IComparable<TKey> {
        int min = 0, max = list.Count - 1;
        while (min < max) {
            var mid = (max + min) / 2;
            T midItem = list[mid];
            TKey midKey = keySelector(midItem);
            var comp = midKey.CompareTo(key);
            if (comp < 0) min = mid + 1;
            else if (comp > 0) max = mid - 1;
            else return midItem;
        }
        if (min == max && keySelector(list[min]).CompareTo(key) == 0) return list[min];
        throw new InvalidOperationException("Item not found");
    }
}
