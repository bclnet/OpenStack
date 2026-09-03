// PORT-SOURCE: Core/OpenStack.Polyfills/System/BitReverseX.cs
// PORT-SHA: d94eab2fe6512403
// PORT-STATUS: done
//
// Bit-reversal via a 256-entry lookup table built in a static constructor.
// Rust builds it as a `const fn` at compile time instead, so there is no
// initialisation order to reason about and the table lands in `.rodata`.

/// C# `BitReverseX.Byte8` — reversed bits for every byte value.
pub static BYTE8: [u8; 256] = build_table();

const fn build_table() -> [u8; 256] {
    let mut t = [0u8; 256];
    let mut i = 0;
    while i < 256 {
        // Reverse the 8 bits of `i`.
        let mut v = i as u8;
        v = (v >> 4) | (v << 4);
        v = ((v & 0xCC) >> 2) | ((v & 0x33) << 2);
        v = ((v & 0xAA) >> 1) | ((v & 0x55) << 1);
        t[i] = v;
        i += 1;
    }
    t
}

/// C# `Reverse32(uint)` — reverse bit order across all 32 bits.
#[inline]
pub fn reverse32(v: u32) -> u32 {
    ((BYTE8[(v & 0xFF) as usize] as u32) << 24)
        | ((BYTE8[((v >> 8) & 0xFF) as usize] as u32) << 16)
        | ((BYTE8[((v >> 16) & 0xFF) as usize] as u32) << 8)
        | (BYTE8[((v >> 24) & 0xFF) as usize] as u32)
}

/// Reverse a single byte.
#[inline]
pub fn reverse8(v: u8) -> u8 {
    BYTE8[v as usize]
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn table_matches_the_std_intrinsic() {
        // The C# built this with an incremental algorithm; verify ours agrees
        // with the obvious definition for every input.
        for i in 0..=255u8 {
            assert_eq!(BYTE8[i as usize], i.reverse_bits(), "byte {i}");
        }
    }

    #[test]
    fn reverse32_matches_the_std_intrinsic() {
        for v in [0u32, 1, 0xFFFF_FFFF, 0x1234_5678, 0xDEAD_BEEF] {
            assert_eq!(reverse32(v), v.reverse_bits(), "value {v:#x}");
        }
    }

    #[test]
    fn reversal_is_an_involution() {
        assert_eq!(reverse32(reverse32(0xCAFE_0001)), 0xCAFE_0001);
    }
}
