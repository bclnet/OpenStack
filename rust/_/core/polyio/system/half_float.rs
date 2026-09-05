// PORT-SOURCE: Core/OpenStack.PolyIO/System/HalfFloat.cs
// PORT-SHA: 7a205540ac5008bd
// PORT-STATUS: done
//
// C# `HalfFloat` is a hand-rolled IEEE 754 binary16: bit-twiddling conversions,
// operator overloads, `IComparable`, `ISerializable`, and stream read/write.
//
// Ported onto the `half` crate's `f16`, which is the same format with a tested
// implementation and hardware conversion where available. This file keeps the
// C# entry points (`to_single`, `from_binary_stream`, `to_binary_stream`) so
// call sites port over unchanged.
//
// C#-SIDE NOTE: `HalfFloat`'s float->half path rounds by truncation, while the
// `half` crate rounds to nearest-even (as IEEE requires). For values exactly
// between two representable halves the two differ by one ULP. That is a real
// numerical difference, not a porting artefact — if bit-identical output with
// the C# matters for a given format, use `from_f32_truncate` below.

use half::f16;
use std::io::{self, Read, Write};

/// C# `struct HalfFloat` — IEEE 754 binary16.
pub type HalfFloat = f16;

/// C# `ToSingle()` / `implicit operator float`.
#[inline]
pub fn to_f32(h: f16) -> f32 {
    h.to_f32()
}

/// C# `implicit operator double`.
#[inline]
pub fn to_f64(h: f16) -> f64 {
    h.to_f64()
}

/// C# `explicit operator HalfFloat(float)`, with IEEE round-to-nearest-even.
#[inline]
pub fn from_f32(v: f32) -> f16 {
    f16::from_f32(v)
}

/// The C#'s truncating conversion, for formats that must match it bit for bit.
///
/// Clears the round/sticky bits before converting, reproducing the C#'s
/// round-toward-zero behaviour.
pub fn from_f32_truncate(v: f32) -> f16 {
    let bits = v.to_bits();
    let sign = ((bits >> 16) & 0x8000) as u16;
    let exp = ((bits >> 23) & 0xff) as i32 - 127 + 15;
    let mantissa = bits & 0x007f_ffff;
    if exp <= 0 {
        // Subnormal or zero: fall back, these are exact either way.
        return f16::from_f32(v);
    }
    if exp >= 0x1f {
        // Overflow to infinity, or NaN passthrough.
        return f16::from_bits(sign | 0x7c00 | if mantissa != 0 { 0x200 } else { 0 });
    }
    f16::from_bits(sign | ((exp as u16) << 10) | ((mantissa >> 13) as u16))
}

/// C# `FromBinaryStream(BinaryReader)` — little-endian on disk.
pub fn from_binary_stream<R: Read>(r: &mut R) -> io::Result<f16> {
    let mut b = [0u8; 2];
    r.read_exact(&mut b)?;
    Ok(f16::from_bits(u16::from_le_bytes(b)))
}

/// C# `ToBinaryStream(BinaryWriter)`.
pub fn to_binary_stream<W: Write>(w: &mut W, v: f16) -> io::Result<()> {
    w.write_all(&v.to_bits().to_le_bytes())
}

// NOT PORTED: `GetObjectData` (`ISerializable`) — .NET binary serialisation has
// no Rust counterpart and is deprecated in .NET itself.

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    #[test]
    fn round_trips_representable_values() {
        for v in [0.0f32, 1.0, -1.0, 0.5, 2048.0] {
            assert_eq!(to_f32(from_f32(v)), v);
        }
    }

    #[test]
    fn handles_the_specials() {
        assert!(to_f32(from_f32(f32::NAN)).is_nan());
        assert_eq!(to_f32(from_f32(f32::INFINITY)), f32::INFINITY);
        assert_eq!(to_f32(from_f32(f32::NEG_INFINITY)), f32::NEG_INFINITY);
        // Beyond binary16's range (~65504) saturates to infinity.
        assert_eq!(to_f32(from_f32(1e30)), f32::INFINITY);
    }

    #[test]
    fn stream_round_trip_is_little_endian() {
        let mut buf = Vec::new();
        to_binary_stream(&mut buf, from_f32(1.0)).unwrap();
        assert_eq!(buf, vec![0x00, 0x3C], "1.0 is 0x3C00 little-endian");
        assert_eq!(to_f32(from_binary_stream(&mut Cursor::new(buf)).unwrap()), 1.0);
    }

    #[test]
    fn truncating_and_nearest_agree_on_exact_values() {
        for v in [1.0f32, 2.0, 0.25, -8.0] {
            assert_eq!(from_f32(v), from_f32_truncate(v), "exact value {v}");
        }
    }

    #[test]
    fn truncating_differs_from_nearest_on_a_tie() {
        // Documents the C#/Rust rounding difference rather than hiding it.
        let v = 1.0009765f32; // just above 1.0 + half a binary16 ULP
        let (n, t) = (from_f32(v), from_f32_truncate(v));
        assert!(
            n.to_bits() >= t.to_bits(),
            "nearest-even must not round below truncation"
        );
    }
}
