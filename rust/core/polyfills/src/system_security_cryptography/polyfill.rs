// PORT-SOURCE: Core/OpenStack.Polyfills/System.Security.Cryptography/Polyfill.cs
// PORT-SHA: db433efaf0c45cef
// PORT-STATUS: done
//
// C# extension methods over `HashAlgorithm` that wrap the awkward
// `TransformBlock`/`TransformFinalBlock` incremental-hashing API.
//
// Rust hash crates (`sha2`, `md-5`, `digest`) already expose the clean
// `update`/`finalize` shape those wrappers were reaching for, so this is a
// trait matching the C# names over whatever digest a caller brings. No hash
// implementation is pulled in here — the crate that needs one picks it, since
// which algorithm is required depends on the format being parsed.

/// C# `HashAlgorithm` extensions, as a trait a caller implements for its digest.
pub trait HashAlgorithmExt {
    /// C# `TransformBlock(byte[])` — feed bytes incrementally.
    fn transform_block(&mut self, data: &[u8]);

    /// C# `TransformBlock(string)` — the C# encodes as UTF-8 first.
    fn transform_block_str(&mut self, value: &str) {
        self.transform_block(value.as_bytes());
    }

    /// C# `ToFinalHash()` — finish and return the digest.
    ///
    /// The C# calls `TransformFinalBlock` with an empty array and then reads
    /// `.Hash`; consuming `self` here makes the "cannot be reused" rule that
    /// the C# only documented into something the compiler enforces.
    fn to_final_hash(self) -> Vec<u8>;
}

#[cfg(test)]
mod tests {
    use super::*;

    /// Not a real hash — just enough to prove the trait's contract.
    #[derive(Default)]
    struct SumHash(u64);

    impl HashAlgorithmExt for SumHash {
        fn transform_block(&mut self, data: &[u8]) {
            for &b in data {
                self.0 = self.0.wrapping_mul(31).wrapping_add(b as u64);
            }
        }
        fn to_final_hash(self) -> Vec<u8> {
            self.0.to_le_bytes().to_vec()
        }
    }

    #[test]
    fn incremental_matches_one_shot() {
        let mut a = SumHash::default();
        a.transform_block(b"hello world");

        let mut b = SumHash::default();
        b.transform_block(b"hello ");
        b.transform_block(b"world");

        assert_eq!(a.to_final_hash(), b.to_final_hash());
    }

    #[test]
    fn string_input_is_utf8_encoded() {
        let mut a = SumHash::default();
        a.transform_block_str("abc");
        let mut b = SumHash::default();
        b.transform_block(b"abc");
        assert_eq!(a.to_final_hash(), b.to_final_hash());
    }
}
