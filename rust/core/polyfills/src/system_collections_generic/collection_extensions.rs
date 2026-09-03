// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/CollectionExtensions.cs
// PORT-SHA: dc167f5211f8b7fb
// PORT-STATUS: done
//
// Keyed binary search over a sorted list.
//
// The C# hand-rolls the loop with `max = mid - 1` inside `while (min < max)`,
// which is a non-standard bound update; the exit conditions then need the
// trailing fix-up checks it has. Rather than transcribe a subtle loop, this
// delegates to `slice::binary_search_by`, which is correct by construction.
//
// One interface change: C# `BinarySearch` **throws `InvalidOperationException`**
// when the key is absent. That is an ordinary outcome of a lookup, not an
// exceptional one, so it returns `Option` here.

/// C# `BinarySearch<T, TKey>(list, keySelector, key)`.
///
/// `list` must be sorted by `key_of`. Returns the matching element.
pub fn binary_search<'a, T, K, F>(list: &'a [T], key_of: F, key: &K) -> Option<&'a T>
where
    K: Ord,
    F: Fn(&T) -> K,
{
    list.binary_search_by(|item| key_of(item).cmp(key))
        .ok()
        .map(|i| &list[i])
}

/// C# `BinarySearchLowerBound<T, TKey>(list, keySelector, key)`.
///
/// Index of the last element whose key is `<= key`, or `None` when every
/// element is greater (the C# returned -1 for both that case and an empty
/// list, which callers had to disambiguate by checking `Count` themselves).
pub fn binary_search_lower_bound<T, K, F>(list: &[T], key_of: F, key: &K) -> Option<usize>
where
    K: Ord,
    F: Fn(&T) -> K,
{
    match list.binary_search_by(|item| key_of(item).cmp(key)) {
        // On a run of equal keys, `binary_search_by` may land anywhere in it;
        // walk to the last, which is what "lower bound" meant here.
        Ok(i) => {
            let mut i = i;
            while i + 1 < list.len() && key_of(&list[i + 1]) == *key {
                i += 1;
            }
            Some(i)
        }
        Err(0) => None,
        Err(i) => Some(i - 1),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    const ITEMS: [(i32, &str); 5] = [
        (10, "a"),
        (20, "b"),
        (30, "c"),
        (40, "d"),
        (50, "e"),
    ];

    #[test]
    fn finds_every_present_key() {
        for (k, v) in ITEMS {
            assert_eq!(binary_search(&ITEMS, |i| i.0, &k).unwrap().1, v);
        }
    }

    #[test]
    fn absent_key_returns_none_instead_of_throwing() {
        assert!(binary_search(&ITEMS, |i| i.0, &35).is_none());
    }

    #[test]
    fn lower_bound_finds_the_greatest_key_at_or_below() {
        assert_eq!(binary_search_lower_bound(&ITEMS, |i| i.0, &35), Some(2));
        assert_eq!(binary_search_lower_bound(&ITEMS, |i| i.0, &30), Some(2));
        assert_eq!(binary_search_lower_bound(&ITEMS, |i| i.0, &99), Some(4));
    }

    #[test]
    fn lower_bound_below_everything_is_none() {
        assert_eq!(binary_search_lower_bound(&ITEMS, |i| i.0, &1), None);
    }

    #[test]
    fn empty_list_is_handled() {
        let empty: [(i32, &str); 0] = [];
        assert!(binary_search(&empty, |i| i.0, &1).is_none());
        assert_eq!(binary_search_lower_bound(&empty, |i| i.0, &1), None);
    }

    #[test]
    fn duplicate_keys_resolve_to_the_last() {
        let dups = [(1, "a"), (2, "b"), (2, "c"), (3, "d")];
        assert_eq!(binary_search_lower_bound(&dups, |i| i.0, &2), Some(2));
    }
}
