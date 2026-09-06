// PORT-SOURCE: Core/OpenStack/Algorithms/MurmurHash3.cs
// PORT-SHA: e889830211845e78
// PORT-STATUS: done
//
// MurmurHash3 x86 32-bit.
//
// Two things to know, both preserved:
//
//   1. **The seed is 0xFFFFFFFF**, not the conventional 0. Hashes here will not
//      match any other MurmurHash3 implementation's defaults.
//   2. **Strings are hashed as UTF-16LE** (`Encoding.Unicode.GetBytes`), while
//      `MurmurHash2` in the same folder uses ASCII. The two therefore disagree
//      on identical string input — worth knowing before assuming they are
//      interchangeable.
//
// The C# reads through a `BinaryReader` over a `MemoryStream` and allocates a
// fresh 4-byte array per block; this iterates the slice directly.

const C1: u32 = 0xcc9e_2d51;
const C2: u32 = 0x1b87_3593;
/// C# `MurmurHash3.Seed`.
pub const SEED: u32 = 0xffff_ffff;

#[inline]
fn fmix(mut h: u32) -> u32 {
    h ^= h >> 16;
    h = h.wrapping_mul(0x85eb_ca6b);
    h ^= h >> 13;
    h = h.wrapping_mul(0xc2b2_ae35);
    h ^= h >> 16;
    h
}

/// C# `MurmurHash3.Hash(byte[] data)`.
pub fn hash(data: &[u8]) -> u32 {
    hash_with_seed(data, SEED)
}

/// Seeded form; the C# hard-coded [`SEED`].
pub fn hash_with_seed(data: &[u8], seed: u32) -> u32 {
    let mut h = seed;
    let mut chunks = data.chunks_exact(4);
    for c in chunks.by_ref() {
        let mut k = u32::from_le_bytes([c[0], c[1], c[2], c[3]]);
        k = k.wrapping_mul(C1);
        k = k.rotate_left(15);
        k = k.wrapping_mul(C2);
        h ^= k;
        h = h.rotate_left(13);
        h = h.wrapping_mul(5).wrapping_add(0xe654_6b64);
    }
    let tail = chunks.remainder();
    if !tail.is_empty() {
        let mut k = 0u32;
        for (i, &b) in tail.iter().enumerate() {
            k |= (b as u32) << (8 * i);
        }
        k = k.wrapping_mul(C1);
        k = k.rotate_left(15);
        k = k.wrapping_mul(C2);
        h ^= k;
    }
    h ^= data.len() as u32;
    fmix(h)
}

/// C# `MurmurHash3.Hash(string data)` — UTF-16LE, unlike `MurmurHash2`.
pub fn hash_str(data: &str) -> u32 {
    let bytes: Vec<u8> = data
        .encode_utf16()
        .flat_map(|u| u.to_le_bytes())
        .collect();
    hash(&bytes)
}

#[cfg(test)]
mod tests {
    use super::*;

    /// SMHasher's `VerificationTest`, which is the authoritative check for a
    /// MurmurHash implementation — much stronger than fixed string vectors,
    /// because it exercises every key length 0..=255 and a different seed for
    /// each, then hashes the concatenated results.
    ///
    /// Expected value for `Murmur3A` (MurmurHash3_x86_32) is `0xB0F57EE3`,
    /// from `rurban/smhasher`'s `main.cpp` hash table.
    fn smhasher_verification_value() -> u32 {
        let mut key = [0u8; 256];
        let mut hashes = Vec::with_capacity(4 * 256);
        for i in 0..256usize {
            key[i] = i as u8;
            // Key i is bytes 0..i; seed is 256 - i.
            let h = hash_with_seed(&key[..i], (256 - i) as u32);
            hashes.extend_from_slice(&h.to_le_bytes());
        }
        hash_with_seed(&hashes, 0)
    }

    #[test]
    fn passes_the_smhasher_verification_test() {
        // If this holds, the algorithm is correct for every length and seed —
        // not just for the handful of strings below.
        assert_eq!(smhasher_verification_value(), 0xB0F5_7EE3);
    }

    #[test]
    fn matches_the_reference_with_the_standard_seed() {
        // Published MurmurHash3 x86_32 vectors, seed 0.
        assert_eq!(hash_with_seed(b"", 0), 0);
        assert_eq!(hash_with_seed(b"a", 0), 0x3c2569b2);
        assert_eq!(hash_with_seed(b"abc", 0), 0xb3dd93fa);
        assert_eq!(hash_with_seed(b"abcd", 0), 0x43ed676a);
    }

    #[test]
    fn the_projects_seed_is_not_the_conventional_zero() {
        assert_eq!(SEED, 0xffff_ffff);
        assert_ne!(hash(b"abc"), hash_with_seed(b"abc", 0));
    }

    #[test]
    fn all_tail_lengths_hash_distinctly() {
        let hs: Vec<u32> = ["a", "ab", "abc", "abcd", "abcde"]
            .iter()
            .map(|s| hash(s.as_bytes()))
            .collect();
        let mut s = hs.clone();
        s.sort_unstable();
        s.dedup();
        assert_eq!(s.len(), hs.len(), "collision: {hs:?}");
    }

    #[test]
    fn string_hashing_is_utf16_not_ascii() {
        // "A" is 0x41 0x00 in UTF-16LE, so it must not equal the 1-byte hash.
        assert_eq!(hash_str("A"), hash(&[0x41, 0x00]));
        assert_ne!(hash_str("A"), hash(b"A"));
    }

    #[test]
    fn the_two_murmur_variants_disagree_on_strings() {
        // MurmurHash2 encodes ASCII, MurmurHash3 UTF-16 — documented, not a bug
        // to fix here, but not interchangeable either.
        use super::super::murmur_hash2;
        assert_ne!(hash_str("abc"), murmur_hash2::hash_str("abc", SEED));
    }
}
