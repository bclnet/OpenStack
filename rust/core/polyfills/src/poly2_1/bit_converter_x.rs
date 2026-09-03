// PORT-SOURCE: Core/OpenStack.Polyfills/Poly2.1/BitConverterX.cs
// PORT-SHA: b82739d30e582ed8
// PORT-STATUS: done
//
// netstandard2.1 shims for `BitConverter`'s `Half` overloads, which the BCL
// only gained later. `half::f16` provides all of them, so these are one-liners.

use half::f16;

/// C# `ToHalf(byte[] value, int startIndex)` / `ToHalf(ReadOnlySpan<byte>)`.
///
/// Little-endian, matching `BitConverter` on every platform this targets.
/// The C# threw `ArgumentOutOfRangeException` on a short input; `None` here.
pub fn to_half(value: &[u8]) -> Option<f16> {
    let b = value.get(..2)?;
    Some(f16::from_bits(u16::from_le_bytes([b[0], b[1]])))
}

/// C# `HalfToInt16Bits(Half)`.
#[inline]
pub fn half_to_i16_bits(value: f16) -> i16 {
    value.to_bits() as i16
}

/// C# `Int16BitsToHalf(short)`.
#[inline]
pub fn i16_bits_to_half(value: i16) -> f16 {
    f16::from_bits(value as u16)
}

/// The bytes of a half, little-endian.
#[inline]
pub fn half_to_bytes(value: f16) -> [u8; 2] {
    value.to_bits().to_le_bytes()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bit_conversions_round_trip() {
        let h = f16::from_f32(1.5);
        assert_eq!(i16_bits_to_half(half_to_i16_bits(h)), h);
    }

    #[test]
    fn reads_little_endian() {
        // 1.0 is 0x3C00.
        assert_eq!(to_half(&[0x00, 0x3C]).unwrap().to_f32(), 1.0);
    }

    #[test]
    fn short_input_returns_none_instead_of_throwing() {
        assert!(to_half(&[0x00]).is_none());
        assert!(to_half(&[]).is_none());
    }

    #[test]
    fn negative_bit_patterns_survive_the_i16_round_trip() {
        let h = f16::from_f32(-2.0);
        assert!(half_to_i16_bits(h) < 0, "sign bit sets the high bit");
        assert_eq!(i16_bits_to_half(half_to_i16_bits(h)), h);
    }
}
