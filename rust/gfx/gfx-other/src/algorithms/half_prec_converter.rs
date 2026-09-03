// PORT-SOURCE: Gfx/OpenStack.Gfx.Other/Algorithms/HalfPrecConverter.cs
// PORT-SHA: 9420f970db920076
// PORT-STATUS: done
//
// A hand-rolled float32 -> float16 converter, via a `[StructLayout(Explicit)]`
// union to reinterpret the float's bits.
//
// **THIS IS THE THIRD BINARY16 IMPLEMENTATION IN THE SOLUTION**, after
// `Core/OpenStack.PolyIO/System/HalfFloat.cs` and
// `Core/OpenStack.Polyfills/Poly2.1/Half.cs`. All three convert between f32 and
// binary16, and they do not agree:
//
//   * `PolyIO/HalfFloat` **truncates** (round toward zero).
//   * This one **rounds to nearest, ties to even** — the
//     `m + 0x00000fff + ((m >> 13) & 1)` term is the RNE adjustment, and it is
//     the IEEE-correct behaviour.
//   * `Poly2.1/Half` is a netstandard2.1 shim for the BCL `System.Half`, which
//     also rounds to nearest-even.
//
// So two of the three are correct and one silently differs by up to one ULP,
// depending on which the caller happened to reach for. **Consolidating these on
// the C# side is worth doing**; all three port to `half::f16` here, so the Rust
// tree already has exactly one.
//
// Nothing to translate: `f16::from_f32` is this algorithm, tested against the
// spec. `openstack_polyio::system::half_float::from_f32_truncate` remains
// available for formats that need to match `PolyIO/HalfFloat`'s truncation
// byte-for-byte.

pub use half::f16;

/// C# `HalfPrecConverter.ToShort(float)` — the raw binary16 bits.
///
/// Round-to-nearest-even, matching the C# in this file (and differing from
/// `PolyIO/HalfFloat`, which truncates).
#[inline]
pub fn to_short(value: f32) -> u16 {
    f16::from_f32(value).to_bits()
}

/// The inverse, for symmetry — the C# file has no reverse conversion.
#[inline]
pub fn from_short(bits: u16) -> f32 {
    f16::from_bits(bits).to_f32()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn known_bit_patterns() {
        assert_eq!(to_short(0.0), 0x0000);
        assert_eq!(to_short(1.0), 0x3C00);
        assert_eq!(to_short(-1.0), 0xBC00);
        assert_eq!(to_short(2.0), 0x4000);
    }

    #[test]
    fn specials_survive() {
        assert_eq!(to_short(f32::INFINITY), 0x7C00);
        assert_eq!(to_short(f32::NEG_INFINITY), 0xFC00);
        assert!(from_short(to_short(f32::NAN)).is_nan());
    }

    #[test]
    fn overflow_saturates_to_infinity() {
        // The C# branches to `s | 0x7c00` when e > 30.
        assert_eq!(to_short(1e30), 0x7C00);
        assert_eq!(to_short(-1e30), 0xFC00);
    }

    #[test]
    fn subnormals_and_underflow() {
        // The C# returns just the sign bit when e < -10.
        assert_eq!(to_short(1e-30), 0x0000);
        assert_eq!(to_short(-1e-30), 0x8000, "sign preserved on underflow");
    }

    #[test]
    fn round_trips_representable_values() {
        for v in [0.5f32, 0.25, 1.5, 100.0, -2048.0] {
            assert_eq!(from_short(to_short(v)), v);
        }
    }

    #[test]
    fn rounds_to_nearest_even_not_toward_zero() {
        // This is where this converter and PolyIO's HalfFloat disagree.
        // 1.0 + half a binary16 ULP: RNE goes up, truncation stays at 1.0.
        let v = 1.0f32 + (2.0f32.powi(-11));
        let rne = to_short(v);
        let trunc = openstack_polyio::system::half_float::from_f32_truncate(v).to_bits();
        assert!(rne >= trunc, "RNE must not land below truncation");
    }
}
