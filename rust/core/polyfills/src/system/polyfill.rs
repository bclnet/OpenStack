// PORT-SOURCE: Core/OpenStack.Polyfills/System/Polyfill.cs
// PORT-SHA: 0755aae74d992b3a
// PORT-STATUS: done
//
// Assorted extension methods. Roughly half depend on .NET reflection over enums
// (`GetAttributeOfType`, `GetFlags`, `EnumNumFlags`, `GetEnumDescription`) —
// those become traits a caller implements or derives, in the same spirit as
// `type_x`'s registration pattern, since Rust has no attribute reflection.

use std::fmt::Write as _;

/// C# `Hex(this byte[] value)` — lowercase hex, no separators.
pub fn hex(value: &[u8]) -> String {
    let mut s = String::with_capacity(value.len() * 2);
    for b in value {
        let _ = write!(s, "{b:02x}");
    }
    s
}

/// C# `FromBGR555(ushort)` — 15-bit BGR to 32-bit ARGB.
///
/// The C# widens each 5-bit channel with `<< 3`, so 0x1F maps to 0xF8 and pure
/// white comes out as (248,248,248) rather than (255,255,255). Preserved, since
/// changing it would shift every decoded palette; `from_bgr555_exact` does the
/// correct expansion for new code.
pub fn from_bgr555(v: u16) -> u32 {
    let b = ((v >> 10) & 0x1F) as u32;
    let g = ((v >> 5) & 0x1F) as u32;
    let r = (v & 0x1F) as u32;
    0xFF00_0000 | ((r << 3) << 16) | ((g << 3) << 8) | (b << 3)
}

/// Full-range 5-bit expansion: replicates the high bits into the low ones, so
/// 0x1F maps to 0xFF.
pub fn from_bgr555_exact(v: u16) -> u32 {
    let x = |c: u32| (c << 3) | (c >> 2);
    let b = x(((v >> 10) & 0x1F) as u32);
    let g = x(((v >> 5) & 0x1F) as u32);
    let r = x((v & 0x1F) as u32);
    0xFF00_0000 | (r << 16) | (g << 8) | b
}

/// C# `ChangeRange(float value, float oldMin, float oldMax, float newMin, float newMax)`.
///
/// Returns `new_min` when the old range is empty; the C# divided by zero.
pub fn change_range(value: f32, old_min: f32, old_max: f32, new_min: f32, new_max: f32) -> f32 {
    let span = old_max - old_min;
    if span.abs() < f32::EPSILON {
        return new_min;
    }
    (value - old_min) / span * (new_max - new_min) + new_min
}

/// C# `GetExtrema` overloads — min and max of a sequence in one pass.
///
/// The C# returns them via `out` parameters and leaves them at their default
/// for an empty input, which is indistinguishable from a real zero. `None` here.
pub fn extrema<T: PartialOrd + Copy>(items: &[T]) -> Option<(T, T)> {
    let mut it = items.iter().copied();
    let first = it.next()?;
    Some(it.fold((first, first), |(lo, hi), v| {
        (if v < lo { v } else { lo }, if v > hi { v } else { hi })
    }))
}

/// C# `Last<T>(this T[] source)` / `Last<T>(this IList<T>)`.
///
/// The C# indexes `[Count - 1]` directly, so an empty collection throws
/// `IndexOutOfRangeException`. `Option` here — and `slice::last` already does
/// this, so prefer that.
#[inline]
pub fn last<T>(items: &[T]) -> Option<&T> {
    items.last()
}

/// C# `Reverse(this string)`.
///
/// The C# reverses the `char[]`, i.e. UTF-16 code units, which corrupts any
/// character outside the BMP (an emoji comes back as two broken halves). This
/// reverses by `char`, which is the intent. Grapheme clusters still need a
/// dedicated crate if combining marks matter.
pub fn reverse_string(s: &str) -> String {
    s.chars().rev().collect()
}

/// C# `ReadAtLeast(this Stream, Span<byte>, int minimumBytes)` — read until at
/// least `minimum` bytes have arrived or the stream ends.
pub fn read_at_least<R: std::io::Read>(
    r: &mut R,
    buf: &mut [u8],
    minimum: usize,
) -> std::io::Result<usize> {
    let mut total = 0;
    while total < minimum.min(buf.len()) {
        match r.read(&mut buf[total..]) {
            Ok(0) => break,
            Ok(n) => total += n,
            Err(e) if e.kind() == std::io::ErrorKind::Interrupted => continue,
            Err(e) => return Err(e),
        }
    }
    Ok(total)
}

/// C# `GetEnumDescription(this Enum)` / `GetAttributeOfType<T>(this Enum)`.
///
/// Both read `[Description]` off an enum field at runtime. Rust has no
/// attribute reflection, so the description becomes a method — write the
/// `match` by hand, or derive it.
pub trait Described {
    fn description(&self) -> &'static str;
}

/// C# `GetFlags(this Enum)`, `EnumNumFlags`, `EnumHasMultiple`.
///
/// These enumerate the set bits of a `[Flags]` enum reflectively. In Rust a
/// flags type is a `bitflags!` struct or a plain integer newtype, and the same
/// three questions are `iter()`, `bits().count_ones()`, and `> 1`.
pub trait FlagsExt: Copy {
    fn bits(self) -> u64;

    /// C# `EnumNumFlags` — how many flags are set.
    fn num_flags(self) -> u32 {
        self.bits().count_ones()
    }

    /// C# `EnumHasMultiple` — more than one flag set.
    fn has_multiple(self) -> bool {
        self.num_flags() > 1
    }
}

// NOT PORTED: `CreateDelegate<T>` (runtime delegate construction — Rust uses
// `fn` pointers and closures directly), `CastToArray` (boxes an `object` into a
// typed array via reflection), and `Equals(object, object)` (a null-tolerant
// reference comparison with no Rust analogue). None has a call site.

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn hex_is_lowercase_and_zero_padded() {
        assert_eq!(hex(&[0x00, 0x0f, 0xff]), "000fff");
        assert_eq!(hex(&[]), "");
    }

    #[test]
    fn bgr555_channels_are_in_the_right_order() {
        // Pure red in BGR555 is the low 5 bits.
        assert_eq!(from_bgr555(0x001F) & 0x00FF_0000, 0x00F8_0000);
        // Pure blue is the high 5 bits.
        assert_eq!(from_bgr555(0x7C00) & 0x0000_00FF, 0x0000_00F8);
    }

    #[test]
    fn exact_expansion_reaches_full_white() {
        // The C# `<< 3` tops out at 0xF8; documented, with the fix beside it.
        assert_eq!(from_bgr555(0x7FFF), 0xFFF8_F8F8);
        assert_eq!(from_bgr555_exact(0x7FFF), 0xFFFF_FFFF);
    }

    #[test]
    fn change_range_maps_endpoints() {
        assert_eq!(change_range(0.5, 0.0, 1.0, 0.0, 100.0), 50.0);
        assert_eq!(change_range(5.0, 5.0, 5.0, 1.0, 9.0), 1.0, "empty old range");
    }

    #[test]
    fn extrema_finds_both_ends() {
        assert_eq!(extrema(&[3, 1, 4, 1, 5]), Some((1, 5)));
        assert_eq!(extrema(&[7]), Some((7, 7)));
        assert_eq!(extrema::<i32>(&[]), None, "empty is distinguishable");
    }

    #[test]
    fn string_reversal_handles_astral_characters() {
        // The C# reverses UTF-16 units and breaks this one into two halves.
        assert_eq!(reverse_string("ab"), "ba");
        assert_eq!(reverse_string("a\u{1F600}b"), "b\u{1F600}a");
    }

    #[test]
    fn last_of_empty_is_none_not_a_panic() {
        assert_eq!(last(&[1, 2, 3]), Some(&3));
        assert_eq!(last::<i32>(&[]), None);
    }

    #[test]
    fn read_at_least_stops_at_eof() {
        use std::io::Cursor;
        let mut c = Cursor::new(b"abc".to_vec());
        let mut buf = [0u8; 10];
        assert_eq!(read_at_least(&mut c, &mut buf, 10).unwrap(), 3);
    }

    #[test]
    fn flag_counting_matches_the_reflective_version() {
        #[derive(Clone, Copy)]
        struct F(u64);
        impl FlagsExt for F {
            fn bits(self) -> u64 {
                self.0
            }
        }
        assert_eq!(F(0b1011).num_flags(), 3);
        assert!(F(0b1011).has_multiple());
        assert!(!F(0b0100).has_multiple());
        assert_eq!(F(0).num_flags(), 0);
    }
}
