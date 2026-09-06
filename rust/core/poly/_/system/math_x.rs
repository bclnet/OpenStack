// PORT-SOURCE: Core/OpenStack.Polyfills/System/MathX.cs
// PORT-SHA: ba1b500f912fcc2c
// PORT-STATUS: done
//
// Scalar helpers: clamping, interpolation, endian swaps, bit twiddling.
//
// C# overloads `Clamp`, `SwapEndian`, and `Reverse` per numeric type and lets
// overload resolution pick. Rust has no overloading, so the width is in the
// name (`swap_endian_u32`) or the call is generic (`clamp`). Most of these are
// one-liners over `std` — `swap_bytes`, `reverse_bits`, `clamp` — so the port
// leans on those rather than re-deriving the arithmetic.

/// C# `Align(int value, int align)` — round up to a multiple.
///
/// The C# is `(value + align - 1) & ~(align - 1)`, valid only for powers of
/// two. Made explicit; use [`align_to`] for the general case.
#[inline]
pub fn align(value: i32, alignment: i32) -> i32 {
    debug_assert!(
        alignment > 0 && (alignment as u32).is_power_of_two(),
        "align() masks and so requires a power of two, got {alignment}"
    );
    (value + alignment - 1) & !(alignment - 1)
}

/// Round up to any multiple, power of two or not.
#[inline]
pub fn align_to(value: u64, alignment: u64) -> u64 {
    if alignment == 0 {
        return value;
    }
    let rem = value % alignment;
    if rem == 0 { value } else { value + alignment - rem }
}

/// C# `Safe(double)` — replace non-finite values with zero.
#[inline]
pub fn safe(v: f64) -> f64 {
    if v.is_finite() { v } else { 0.0 }
}

/// C# `Clamp<T>(value, min, max)`.
///
/// When `min > max` the C# returns `min`; `f32::clamp` would panic, so the
/// bounds are ordered first.
#[inline]
pub fn clamp<T: PartialOrd>(value: T, min: T, max: T) -> T {
    if min > max {
        return min;
    }
    if value < min {
        min
    } else if value > max {
        max
    } else {
        value
    }
}

/// C# `Lerp(float a, float b, float t)` — `t` clamped to `[0, 1]`.
#[inline]
pub fn lerp(a: f32, b: f32, t: f32) -> f32 {
    a + (b - a) * clamp(t, 0.0, 1.0)
}

/// C# `LerpUnclamped(float a, float b, float t)`.
#[inline]
pub fn lerp_unclamped(a: f32, b: f32, t: f32) -> f32 {
    a + (b - a) * t
}

/// C# `InverseLerp(float a, float b, float value)`.
///
/// Returns 0 when `a == b`; the C# divides by zero and yields NaN or infinity.
#[inline]
pub fn inverse_lerp(a: f32, b: f32, value: f32) -> f32 {
    if (b - a).abs() < f32::EPSILON {
        return 0.0;
    }
    clamp((value - a) / (b - a), 0.0, 1.0)
}

/// C# `Repeat(float t, float length)` — wrap into `[0, length)`.
#[inline]
pub fn repeat(t: f32, length: f32) -> f32 {
    if length == 0.0 {
        return 0.0;
    }
    clamp(t - (t / length).floor() * length, 0.0, length)
}

/// C# `LerpAngle(float a, float b, float t)` — interpolate the short way round,
/// in degrees.
pub fn lerp_angle(a: f32, b: f32, t: f32) -> f32 {
    let mut delta = repeat(b - a, 360.0);
    if delta > 180.0 {
        delta -= 360.0;
    }
    a + delta * clamp(t, 0.0, 1.0)
}

/// C# `Swap<T>(ref T, ref T)`.
#[inline]
pub fn swap<T>(a: &mut T, b: &mut T) {
    std::mem::swap(a, b);
}

/// C# `NextPower(int)` — the next power of two at or above `value`.
#[inline]
pub fn next_power(value: u32) -> u32 {
    if value <= 1 {
        1
    } else {
        value.next_power_of_two()
    }
}

/// C# `GetBits(ulong value, int offset, int count)` — extract a bit field.
#[inline]
pub fn get_bits(value: u64, offset: u32, count: u32) -> u64 {
    if count == 0 || offset >= 64 {
        return 0;
    }
    let count = count.min(64 - offset);
    if count == 64 {
        return value;
    }
    (value >> offset) & ((1u64 << count) - 1)
}

/// C# `TryParseInt32(string, out int)`.
#[inline]
pub fn try_parse_i32(s: &str) -> Option<i32> {
    s.trim().parse().ok()
}

// -- endian swaps -----------------------------------------------------------
// C# `SwapEndian` overloads; `std`'s `swap_bytes` is the same operation.

#[inline]
pub fn swap_endian_u16(v: u16) -> u16 {
    v.swap_bytes()
}

#[inline]
pub fn swap_endian_u32(v: u32) -> u32 {
    v.swap_bytes()
}

#[inline]
pub fn swap_endian_u64(v: u64) -> u64 {
    v.swap_bytes()
}

#[inline]
pub fn swap_endian_i32(v: i32) -> i32 {
    v.swap_bytes()
}

/// Byte-swaps the representation, not the value.
#[inline]
pub fn swap_endian_f32(v: f32) -> f32 {
    f32::from_bits(v.to_bits().swap_bytes())
}

#[inline]
pub fn swap_endian_f64(v: f64) -> f64 {
    f64::from_bits(v.to_bits().swap_bytes())
}

/// C# `SwapEndian(byte[] value, int sizeOf)` — reverse each element in place.
pub fn swap_endian_bytes(data: &mut [u8], size_of: usize) {
    if size_of > 1 {
        for chunk in data.chunks_mut(size_of) {
            chunk.reverse();
        }
    }
}

// -- bit reversal -----------------------------------------------------------

#[inline]
pub fn reverse_u16(v: u16) -> u16 {
    v.reverse_bits()
}

#[inline]
pub fn reverse_u32(v: u32) -> u32 {
    v.reverse_bits()
}

#[inline]
pub fn reverse_i32(v: i32) -> i32 {
    v.reverse_bits()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn align_rounds_up_and_is_idempotent() {
        assert_eq!(align(0, 4), 0);
        assert_eq!(align(1, 4), 4);
        assert_eq!(align(4, 4), 4);
        assert_eq!(align(5, 8), 8);
        assert_eq!(align_to(10, 3), 12, "non-power-of-two");
    }

    #[test]
    fn clamp_handles_inverted_bounds_without_panicking() {
        // f32::clamp panics when min > max; the C# returns min.
        assert_eq!(clamp(5.0, 10.0, 1.0), 10.0);
        assert_eq!(clamp(5, 0, 10), 5);
        assert_eq!(clamp(-5, 0, 10), 0);
    }

    #[test]
    fn lerp_clamps_but_unclamped_does_not() {
        assert_eq!(lerp(0.0, 10.0, 0.5), 5.0);
        assert_eq!(lerp(0.0, 10.0, 2.0), 10.0);
        assert_eq!(lerp_unclamped(0.0, 10.0, 2.0), 20.0);
    }

    #[test]
    fn inverse_lerp_inverts_lerp() {
        assert_eq!(inverse_lerp(10.0, 20.0, 15.0), 0.5);
        // Degenerate range: the C# yields NaN here.
        assert_eq!(inverse_lerp(5.0, 5.0, 5.0), 0.0);
    }

    #[test]
    fn lerp_angle_takes_the_short_way_round() {
        // 350 -> 10 should cross zero, not run backwards through 180.
        let mid = lerp_angle(350.0, 10.0, 0.5);
        assert!((mid - 360.0).abs() < 1e-3 || mid.abs() < 1e-3, "got {mid}");
    }

    #[test]
    fn repeat_wraps_into_range() {
        assert!((repeat(370.0, 360.0) - 10.0).abs() < 1e-4);
        assert!((repeat(-10.0, 360.0) - 350.0).abs() < 1e-4);
        assert_eq!(repeat(5.0, 0.0), 0.0, "zero length must not divide by zero");
    }

    #[test]
    fn next_power_never_returns_zero() {
        assert_eq!(next_power(0), 1);
        assert_eq!(next_power(1), 1);
        assert_eq!(next_power(5), 8);
        assert_eq!(next_power(1024), 1024);
    }

    #[test]
    fn get_bits_extracts_the_right_field() {
        assert_eq!(get_bits(0b1111_0000, 4, 4), 0b1111);
        assert_eq!(get_bits(0xDEAD_BEEF, 0, 16), 0xBEEF);
        assert_eq!(get_bits(u64::MAX, 0, 64), u64::MAX, "full width must not overflow");
        assert_eq!(get_bits(1, 0, 0), 0);
        assert_eq!(get_bits(1, 99, 4), 0, "offset past the end");
    }

    #[test]
    fn endian_swaps_are_involutions() {
        assert_eq!(swap_endian_u32(swap_endian_u32(0x1234_5678)), 0x1234_5678);
        assert_eq!(swap_endian_u32(0x1234_5678), 0x7856_3412);
        assert_eq!(swap_endian_f32(swap_endian_f32(1.5)), 1.5);
    }

    #[test]
    fn buffer_swap_reverses_each_element() {
        let mut b = [1u8, 2, 3, 4, 5, 6, 7, 8];
        swap_endian_bytes(&mut b, 4);
        assert_eq!(b, [4, 3, 2, 1, 8, 7, 6, 5]);
    }

    #[test]
    fn bit_reversal_matches_std() {
        assert_eq!(reverse_u32(1), 0x8000_0000);
        assert_eq!(reverse_u16(reverse_u16(0xABCD)), 0xABCD);
    }
}
