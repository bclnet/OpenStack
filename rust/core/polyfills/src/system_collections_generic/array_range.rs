// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/ArrayRange.cs
// PORT-SHA: 28c63ee49bd7671f
// PORT-STATUS: done
//
// C# `struct ArrayRange<T> : IEnumerable<T>` — an array plus an offset and a
// length, with a hand-written enumerator.
//
// This is `&[T]`. Rust slices are exactly "array plus offset plus length", are
// bounds-checked by the compiler rather than by `Debug.Assert` (which compiles
// away in release, so the C# invariants are unchecked in shipping builds), and
// come with the whole iterator API for free.
//
// The alias exists so ported signatures read the same. `ArrayRangeBuf` covers
// the case where an owned, sliceable handle is genuinely needed.

/// C# `ArrayRange<T>` — use a slice.
pub type ArrayRange<'a, T> = &'a [T];

/// Owned equivalent, for the places the C# stored an `ArrayRange<T>` in a field
/// rather than passing it along.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct ArrayRangeBuf<T> {
    array: Vec<T>,
    offset: usize,
    length: usize,
}

impl<T> ArrayRangeBuf<T> {
    /// C# `ArrayRange(T[] array)`.
    pub fn new(array: Vec<T>) -> Self {
        let length = array.len();
        Self { array, offset: 0, length }
    }

    /// C# `ArrayRange(T[] array, int offset, int length)`.
    ///
    /// The C# checks the bounds with `Debug.Assert`, which is stripped in
    /// release builds — so an out-of-range offset there is silent corruption.
    /// This returns `None`.
    pub fn with_range(array: Vec<T>, offset: usize, length: usize) -> Option<Self> {
        if offset.checked_add(length)? > array.len() {
            return None;
        }
        Some(Self { array, offset, length })
    }

    /// C# `offset`.
    #[inline]
    pub fn offset(&self) -> usize {
        self.offset
    }

    /// C# `length`.
    #[inline]
    pub fn len(&self) -> usize {
        self.length
    }

    #[inline]
    pub fn is_empty(&self) -> bool {
        self.length == 0
    }

    /// The range as a slice — replaces C# `GetEnumerator`.
    #[inline]
    pub fn as_slice(&self) -> &[T] {
        &self.array[self.offset..self.offset + self.length]
    }

    #[inline]
    pub fn iter(&self) -> std::slice::Iter<'_, T> {
        self.as_slice().iter()
    }
}

impl<'a, T> IntoIterator for &'a ArrayRangeBuf<T> {
    type Item = &'a T;
    type IntoIter = std::slice::Iter<'a, T>;
    fn into_iter(self) -> Self::IntoIter {
        self.iter()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn iterates_only_the_range() {
        let r = ArrayRangeBuf::with_range(vec![0, 1, 2, 3, 4, 5], 2, 3).unwrap();
        assert_eq!(r.iter().copied().collect::<Vec<_>>(), vec![2, 3, 4]);
    }

    #[test]
    fn out_of_bounds_range_is_rejected() {
        // The C# only asserts, so this is silent corruption in release builds.
        assert!(ArrayRangeBuf::with_range(vec![0, 1, 2], 2, 5).is_none());
        assert!(ArrayRangeBuf::with_range(vec![0, 1, 2], 9, 0).is_none());
    }

    #[test]
    fn whole_array_constructor_covers_everything() {
        let r = ArrayRangeBuf::new(vec![7, 8, 9]);
        assert_eq!(r.offset(), 0);
        assert_eq!(r.len(), 3);
        assert_eq!(r.as_slice(), &[7, 8, 9]);
    }

    #[test]
    fn empty_range_iterates_zero_times() {
        let r = ArrayRangeBuf::with_range(vec![1, 2, 3], 1, 0).unwrap();
        assert!(r.is_empty());
        assert_eq!(r.iter().count(), 0);
    }
}
