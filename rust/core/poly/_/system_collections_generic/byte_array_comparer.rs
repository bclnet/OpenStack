// PORT-SOURCE: Core/OpenStack.Polyfills/System.Collections.Generic/ByteArrayComparer.cs
// PORT-SHA: f8a449f301ee571c
// PORT-STATUS: done
//
// C#-SIDE BUG, and a consequential one: `GetHashCode(byte[] key)` is
// `key.Sum(b => b)`.
//
//   * It is **order-insensitive**, so [1,2] and [2,1] collide. Byte arrays used
//     as dictionary keys are usually hashes or file ids, where permutations are
//     exactly what you need to tell apart.
//   * The range is tiny — a 32-byte key sums to at most 8160 — so a dictionary
//     of file hashes degenerates toward a linear scan.
//   * `Sum` on `int` **throws OverflowException in a checked context** for long
//     enough inputs.
//
// Rust has no separate comparer object: `[u8]` already implements `Eq + Hash`
// correctly and `HashMap<Vec<u8>, V>` just works. This file exists so the C#
// name resolves, and to record why nothing needs porting.
//
// C# `Equals(null, null)` returns true and `GetHashCode(null)` throws; Rust has
// no null slice, so both cases disappear.

/// C# `ByteArrayComparer` — no Rust equivalent is needed.
///
/// Use `Vec<u8>` / `[u8]` directly as a key:
///
/// ```
/// use std::collections::HashMap;
/// let mut m: HashMap<Vec<u8>, u32> = HashMap::new();
/// m.insert(vec![1, 2], 10);
/// assert_eq!(m.get(&vec![1, 2][..]).copied(), Some(10));
/// assert_eq!(m.get(&vec![2, 1][..]), None); // order matters, unlike the C#
/// ```
pub struct ByteArrayComparer;

#[cfg(test)]
mod tests {
    use std::collections::HashMap;
    use std::hash::{DefaultHasher, Hash, Hasher};

    fn hash(v: &[u8]) -> u64 {
        let mut h = DefaultHasher::new();
        v.hash(&mut h);
        h.finish()
    }

    #[test]
    fn hashing_is_order_sensitive_unlike_the_c_sharp() {
        // The C# `Sum(b => b)` gives both of these the same hash code.
        assert_ne!(hash(&[1, 2]), hash(&[2, 1]));
    }

    #[test]
    fn byte_vectors_work_as_map_keys_directly() {
        let mut m: HashMap<Vec<u8>, u32> = HashMap::new();
        m.insert(vec![0xDE, 0xAD], 1);
        m.insert(vec![0xAD, 0xDE], 2);
        assert_eq!(m.len(), 2, "permutations must be distinct keys");
    }
}
