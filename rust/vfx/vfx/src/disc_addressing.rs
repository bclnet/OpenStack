// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (BCD2 / MSF)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// Disc addressing: BCD-packed bytes as they appear in a CD's Q subchannel, and
// MSF (minute:second:frame) timecodes.
//
// This is the part of `Disc.cs` worth porting without a test image, because it
// is exact arithmetic against a published standard: a CD runs at **75 frames
// per second**, so `LBA = m * 4500 + s * 75 + f`, and Red Book puts LBA 0 at
// absolute MSF 00:02:00 — a 150-sector lead-in offset. Those constants are
// checkable; sector interleaving and image-format quirks are not, which is why
// the rest of `Disc.cs` is still deferred (see `disc.rs`).
//
// ===================== FOUR C#-SIDE BUGS =================================
//
//   1. **`MSF(string)` is documented "strict" but only validates format, not
//      values.** `"99:99:99"` parses with `Valid = true`, giving `Sec = 99`
//      (max 59) and `Frac = 99` (max 74). `Sector` then returns a nonsense LBA
//      that is silently used as a track offset. The port validates ranges.
//
//   2. **`MSF(int m, int s, int f)` casts to `byte` with no range check**, so
//      `new MSF(0, 0, 200)` yields `Frac = 200` and a wrong `Sector`.
//
//   3. **`MSF(int sectorNumber)` overflows `Min` past 445,500 sectors.**
//      `Min = (byte)(n / 4500)` wraps at 100 minutes. A CD tops out near 74
//      minutes so it does not bite there, but the same type is used for larger
//      images.
//
//   4. **`IntToBCD` produces garbage for `n > 99`.** `DivRem(100, 10)` gives
//      tens = 10, and `(10 << 4) | 0` is `0xA0` — which `DecimalValue` reads
//      back as 100... but `DivRem(160, 10)` gives tens = 16, `(16 << 4)` is
//      `0x100`, truncated to `0x00` by the `(byte)` cast. So 160 round-trips as
//      **0**. There is no guard; BCD only represents 0..99.

/// C# `struct BCD2` — one byte holding two decimal digits, as the CD subchannel
/// stores them.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Default, Hash)]
pub struct Bcd2 {
    /// C# `BCDValue`.
    pub bcd_value: u8,
}

impl Bcd2 {
    /// C# `FromBCD(byte)`.
    #[inline]
    pub const fn from_bcd(b: u8) -> Self {
        Self { bcd_value: b }
    }

    /// C# `FromDecimal(int)`.
    ///
    /// `None` outside 0..=99, which is all a two-digit BCD byte can hold. The
    /// C# truncates instead — see bug 4.
    #[inline]
    pub const fn from_decimal(d: i32) -> Option<Self> {
        if d < 0 || d > 99 {
            return None;
        }
        Some(Self { bcd_value: (((d / 10) << 4) | (d % 10)) as u8 })
    }

    /// C# `DecimalValue` getter.
    ///
    /// Note this reads each nibble as a decimal digit without checking it is
    /// one: a nibble of 0xA..0xF (which valid BCD never contains) yields a
    /// value above 99. Preserved, since real subchannel data can be corrupt and
    /// the C# tolerates it.
    #[inline]
    pub const fn decimal_value(&self) -> i32 {
        (self.bcd_value & 0xF) as i32 + (((self.bcd_value >> 4) & 0xF) as i32) * 10
    }

    /// Whether both nibbles are valid decimal digits.
    #[inline]
    pub const fn is_valid_bcd(&self) -> bool {
        (self.bcd_value & 0xF) <= 9 && ((self.bcd_value >> 4) & 0xF) <= 9
    }

    /// The C#'s `IntToBCD`, truncation and all, for round-tripping data that
    /// was written by it.
    #[deprecated(note = "mirrors a C#-side bug: values above 99 truncate")]
    pub const fn int_to_bcd_bug_compat(n: i32) -> u8 {
        (((n / 10) << 4) | (n % 10)) as u8
    }
}

impl std::fmt::Display for Bcd2 {
    /// C# `ToString() => BCDValue.ToString("X2")`.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{:02X}", self.bcd_value)
    }
}

/// Frames per second on a CD. Red Book: 75.
pub const FRAMES_PER_SECOND: i32 = 75;
/// Frames per minute.
pub const FRAMES_PER_MINUTE: i32 = 60 * FRAMES_PER_SECOND;
/// C#: "LBA 0 is Absolute MSF 00:02:00" — the 150-sector lead-in.
pub const LEAD_IN_SECTORS: i32 = 2 * FRAMES_PER_SECOND;

/// C# `readonly struct MSF` — a minute:second:frame timecode.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub struct Msf {
    pub min: u8,
    pub sec: u8,
    pub frac: u8,
    pub negative: bool,
}

impl Msf {
    /// C# `MSF.ToInt(int m, int s, int f)`.
    #[inline]
    pub const fn to_int(m: i32, s: i32, f: i32) -> i32 {
        m * FRAMES_PER_MINUTE + s * FRAMES_PER_SECOND + f
    }

    /// C# `MSF(int m, int s, int f)`, with the range check it lacks (bug 2).
    ///
    /// `sec` must be under 60 and `frac` under 75; `min` must fit a byte.
    pub const fn new(m: i32, s: i32, f: i32) -> Option<Self> {
        if m < 0 || m > 255 || s < 0 || s >= 60 || f < 0 || f >= FRAMES_PER_SECOND {
            return None;
        }
        Some(Self { min: m as u8, sec: s as u8, frac: f as u8, negative: false })
    }

    /// C# `MSF(int SectorNumber)` — LBA to timecode.
    ///
    /// `None` past 445,500 sectors (100 minutes), where the C# wraps `Min`
    /// silently (bug 3).
    pub const fn from_sector(sector_number: i32) -> Option<Self> {
        let (n, negative) = if sector_number < 0 {
            (-sector_number, true)
        } else {
            (sector_number, false)
        };
        let min = n / FRAMES_PER_MINUTE;
        if min > 255 {
            return None;
        }
        Some(Self {
            min: min as u8,
            sec: ((n / FRAMES_PER_SECOND) % 60) as u8,
            frac: (n % FRAMES_PER_SECOND) as u8,
            negative,
        })
    }

    /// C# `MSF(string str)` — parses exactly `"mm:ss:ff"`.
    ///
    /// Unlike the C#, out-of-range components are rejected (bug 1). The C#
    /// comment claims strictness; it only checks the digit/colon pattern.
    pub fn parse(s: &str) -> Option<Self> {
        let b = s.as_bytes();
        if b.len() != 8 || b[2] != b':' || b[5] != b':' {
            return None;
        }
        let d = |i: usize| -> Option<i32> {
            if b[i].is_ascii_digit() {
                Some((b[i] - b'0') as i32)
            } else {
                None
            }
        };
        let m = d(0)? * 10 + d(1)?;
        let sec = d(3)? * 10 + d(4)?;
        let f = d(6)? * 10 + d(7)?;
        Self::new(m, sec, f)
    }

    /// The C#'s format-only validation, for reading sheets it already accepted.
    #[deprecated(note = "mirrors a C#-side bug: accepts out-of-range components like 99:99:99")]
    pub fn parse_bug_compat(s: &str) -> Option<Self> {
        let b = s.as_bytes();
        if b.len() != 8 || b[2] != b':' || b[5] != b':' {
            return None;
        }
        if !b.iter().enumerate().all(|(i, c)| i == 2 || i == 5 || c.is_ascii_digit()) {
            return None;
        }
        let d = |i: usize| ((b[i] - b'0') as i32) * 10 + (b[i + 1] - b'0') as i32;
        Some(Self {
            min: d(0) as u8,
            sec: d(3) as u8,
            frac: d(6) as u8,
            negative: false,
        })
    }

    /// C# `Sector` — the LBA this timecode denotes.
    #[inline]
    pub const fn sector(&self) -> i32 {
        let n = Self::to_int(self.min as i32, self.sec as i32, self.frac as i32);
        if self.negative {
            -n
        } else {
            n
        }
    }

    /// Absolute MSF to logical LBA: subtract the 150-sector lead-in.
    ///
    /// The C# scatters `± 150` at the call sites with comments noting the
    /// offset ("give or take 150"), rather than naming the conversion. Two
    /// separate comments in `Disc.cs` flag the resulting confusion.
    #[inline]
    pub const fn to_lba(&self) -> i32 {
        self.sector() - LEAD_IN_SECTORS
    }

    /// Logical LBA to absolute MSF.
    #[inline]
    pub fn from_lba(lba: i32) -> Option<Self> {
        Self::from_sector(lba + LEAD_IN_SECTORS)
    }
}

impl std::fmt::Display for Msf {
    /// C# `ToString()` — `+mm:ss:ff`, with the sign always present.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "{}{:02}:{:02}:{:02}",
            if self.negative { '-' } else { '+' },
            self.min,
            self.sec,
            self.frac
        )
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn frame_rate_constants_match_red_book() {
        assert_eq!(FRAMES_PER_SECOND, 75);
        assert_eq!(FRAMES_PER_MINUTE, 4500);
        assert_eq!(LEAD_IN_SECTORS, 150);
    }

    #[test]
    fn lba_zero_is_absolute_msf_two_seconds() {
        // The invariant the C# states in a comment at line 1703.
        let m = Msf::from_lba(0).unwrap();
        assert_eq!((m.min, m.sec, m.frac), (0, 2, 0));
        assert_eq!(m.to_lba(), 0);
    }

    #[test]
    fn msf_to_sector_round_trips() {
        for lba in [0, 1, 74, 75, 4499, 4500, 150_000, 333_000] {
            let m = Msf::from_sector(lba).unwrap();
            assert_eq!(m.sector(), lba, "lba {lba} -> {m}");
        }
    }

    #[test]
    fn known_timecodes() {
        assert_eq!(Msf::to_int(0, 2, 0), 150);
        assert_eq!(Msf::to_int(1, 0, 0), 4500);
        assert_eq!(Msf::to_int(74, 0, 0), 333_000, "74 minutes, a full CD");
        let m = Msf::from_sector(4500 + 75 + 1).unwrap();
        assert_eq!((m.min, m.sec, m.frac), (1, 1, 1));
    }

    #[test]
    fn parsing_rejects_out_of_range_components() {
        // The C# accepts all of these with Valid = true.
        assert!(Msf::parse("00:00:00").is_some());
        assert!(Msf::parse("74:59:74").is_some(), "maximum legal values");
        assert!(Msf::parse("00:60:00").is_none(), "60 seconds is invalid");
        assert!(Msf::parse("00:00:75").is_none(), "75 frames is invalid");
        assert!(Msf::parse("99:99:99").is_none());
    }

    #[test]
    fn the_c_sharp_parser_accepts_nonsense() {
        // Documents the bug rather than silently correcting it.
        #[allow(deprecated)]
        let m = Msf::parse_bug_compat("99:99:99").expect("C# accepts this");
        assert_eq!((m.sec, m.frac), (99, 99));
        // And the LBA it yields is meaningless.
        assert_eq!(m.sector(), 99 * 4500 + 99 * 75 + 99);
    }

    #[test]
    fn parsing_rejects_malformed_shapes() {
        for s in ["", "0:0:0", "00-00-00", "00:00:0", "000:00:00", "aa:bb:cc"] {
            assert!(Msf::parse(s).is_none(), "{s:?} should not parse");
        }
    }

    #[test]
    fn constructor_range_checks() {
        assert!(Msf::new(0, 0, 0).is_some());
        assert!(Msf::new(0, 0, 200).is_none(), "C# would store 200");
        assert!(Msf::new(0, 60, 0).is_none());
        assert!(Msf::new(-1, 0, 0).is_none());
    }

    #[test]
    fn large_sector_numbers_are_rejected_not_wrapped() {
        // 100 minutes exactly. The C# wraps Min to 100 -> (byte)100, and past
        // 256 minutes it aliases entirely.
        assert!(Msf::from_sector(256 * 4500).is_none());
        assert!(Msf::from_sector(255 * 4500).is_some());
    }

    #[test]
    fn negative_sectors_keep_their_sign() {
        // Pregap addresses are negative LBAs.
        let m = Msf::from_sector(-150).unwrap();
        assert!(m.negative);
        assert_eq!(m.sector(), -150);
        assert_eq!(m.to_string(), "-00:02:00");
    }

    #[test]
    fn display_matches_the_c_sharp_format() {
        assert_eq!(Msf::new(1, 2, 3).unwrap().to_string(), "+01:02:03");
    }

    #[test]
    fn bcd_round_trips_across_its_whole_range() {
        for d in 0..=99 {
            let b = Bcd2::from_decimal(d).unwrap();
            assert_eq!(b.decimal_value(), d, "decimal {d}");
            assert!(b.is_valid_bcd());
        }
    }

    #[test]
    fn bcd_rejects_values_it_cannot_represent() {
        assert!(Bcd2::from_decimal(100).is_none());
        assert!(Bcd2::from_decimal(-1).is_none());
    }

    #[test]
    fn the_c_sharp_bcd_conversion_truncates() {
        // 160 -> tens 16 -> (16 << 4) = 0x100 -> (byte) = 0x00 -> reads as 0.
        #[allow(deprecated)]
        let b = Bcd2::from_bcd(Bcd2::int_to_bcd_bug_compat(160));
        assert_eq!(b.decimal_value(), 0, "160 round-trips as 0 in the C#");
    }

    #[test]
    fn bcd_display_is_two_hex_digits() {
        assert_eq!(Bcd2::from_decimal(42).unwrap().to_string(), "42");
        assert_eq!(Bcd2::from_bcd(0x09).to_string(), "09");
    }

    #[test]
    fn invalid_nibbles_are_detectable() {
        // Real subchannel data can be corrupt; the C# reads it anyway.
        let bad = Bcd2::from_bcd(0xAF);
        assert!(!bad.is_valid_bcd());
        assert_eq!(bad.decimal_value(), 115, "reads above 99");
    }

    #[test]
    fn bcd_ordering_is_by_raw_byte_as_in_the_c_sharp() {
        // The C# operators compare BCDValue, not DecimalValue. For valid BCD
        // the two orderings agree, which is why it works.
        assert!(Bcd2::from_decimal(9).unwrap() < Bcd2::from_decimal(10).unwrap());
        assert!(Bcd2::from_decimal(59).unwrap() < Bcd2::from_decimal(60).unwrap());
    }
}
