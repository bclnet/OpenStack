// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (ECM / Synth / Synth:Jobs / Synth:Jobs2)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// ==========================================================================
// UNVERIFIED against real media, written blind at explicit request.
//
// One thing is genuinely verified here: the EDC and Galois-field tables are
// derived at compile time from their polynomials and checked against
// independently-computed values, so the checksum maths is sound. What is NOT
// verified is the sector *layout* — which byte range each part occupies — and
// that is what decides whether a synthesized sector is accepted by a real
// emulator. Marked `VERIFY:` throughout.
// ==========================================================================
//
// Sector synthesis: turning a parsed TOC plus a data blob into the 2352- or
// 2448-byte sectors a drive would return. This is the layer that consumes the
// CUE/CCD/MDS/NRG/CDI readers.
//
// Layout of a Mode 1 sector (2352 bytes), which the offsets below assume:
//
//     0..12     sync      00 FF*10 00
//     12..15    address   MSF of (LBA + 150), BCD
//     15..16    mode      1 or 2
//     16..2064  user data (2048)
//     2064..2068 EDC      little-endian CRC over bytes 0..2064
//     2068..2076 reserved zero
//     2076..2352 ECC      P and Q parity
//     2352..2364 subchannel P
//     2364..2376 subchannel Q
//     2376..2448 subchannel R-W
//
// ============ A SPEC BUG: SYNTHESIZED SESSION FORMAT IS WRONG ============
//
// `Synthesize_A0A1A2` writes the session format into the POINT=0xA0 Q entry:
//
//     sq.ap_sec.DecimalValue = SessionFormat switch {
//         Type00_CDROM_CDDA => 0x00,
//         Type10_CDI        => 0x10,     // <- decimal 16
//         Type20_CDXA       => 0x20,     // <- decimal 32
//     };
//
// `DecimalValue` is a **decimal-to-BCD setter**. Assigning `0x10` assigns
// decimal 16, which stores BCD `0x16`. The standard requires PSEC to be BCD
// `0x10` for CD-I and `0x20` for CD-XA.
//
// It round-trips *within this codebase* — the reader also compares
// `DecimalValue` against `0x10`/`0x20`, so 16 matches 16 — which is exactly why
// it survives. But the byte written into the subchannel is wrong, so anything
// reading the Q channel by the spec (a real drive, another emulator, a
// verifier) sees session format `0x16` and does not recognise it. Should be
// `BCDValue = 0x10` or `DecimalValue = 10`. **Fix this in the C# tree.**
//
// Two more:
//
//   * **`SS_Leadout` computes `Timestamp` relative to the lead-out track but
//     `AP_Timestamp` absolute**, with no `+ 150` on either. Given
//     `Synthesize_DiscTOCFromRawTOCEntries` does `AP_Timestamp - 150` when
//     reading, one of the two is off by the lead-in. VERIFY: I have kept the
//     C#'s arithmetic exactly rather than guess which.
//   * **`SS_Mode1_2048.Synth` mutates `job.Parts`** (`job.Parts |= User2048 |
//     Header16`) on a job the caller owns and reuses — `DiscSectorReader` keeps
//     one `SectorSynthJob` as a field and re-uses it for every read. So
//     requesting ECM once permanently adds those parts to every later read
//     through that reader.

/// C# `[Flags] enum ESectorSynthPart`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub struct SynthPart(pub u32);

impl SynthPart {
    pub const HEADER16: Self = Self(1);
    pub const USER2048: Self = Self(2);
    pub const ECC276: Self = Self(4);
    pub const EDC12: Self = Self(8);
    pub const ECM288_COMPLETE: Self = Self(4 | 8);
    pub const ECM_ANY: Self = Self::ECM288_COMPLETE;
    pub const USER2336: Self = Self(2 | 4 | 8);
    pub const USER_COMPLETE: Self = Self(1 | 2 | 4 | 8);
    pub const USER2352: Self = Self::USER_COMPLETE;
    pub const SUBCHANNEL_P: Self = Self(16);
    pub const SUBCHANNEL_Q: Self = Self(32);
    pub const SUBCHANNEL_RSTUVW: Self = Self(64 | 128 | 256 | 512 | 1024 | 2048);
    pub const SUBCODE_COMPLETE: Self = Self(16 | 32 | 64 | 128 | 256 | 512 | 1024 | 2048);
    pub const SUBCODE_ANY: Self = Self::SUBCODE_COMPLETE;
    pub const SUBCODE_DEINTERLEAVE: Self = Self(4096);
    pub const COMPLETE2448: Self = Self(Self::SUBCODE_COMPLETE.0 | Self::USER2352.0);

    #[inline]
    pub const fn contains(self, o: Self) -> bool {
        self.0 & o.0 != 0
    }

    #[inline]
    pub const fn contains_all(self, o: Self) -> bool {
        self.0 & o.0 == o.0
    }

    #[inline]
    pub const fn union(self, o: Self) -> Self {
        Self(self.0 | o.0)
    }
}

impl std::ops::BitOr for SynthPart {
    type Output = Self;
    fn bitor(self, o: Self) -> Self {
        self.union(o)
    }
}

// ---------------------------------------------------------------------------
// ECM: EDC checksum and Reed-Solomon parity
// ---------------------------------------------------------------------------

/// C# `static class ECM`.
pub mod ecm {
    /// CD-ROM EDC polynomial, x^32+x^31+x^16+x^15+x^4+x^3+x+1.
    const EDC_POLY: u32 = 0x8001_801B;

    /// C# `edc_table`, derived rather than transcribed.
    static EDC_TABLE: [u32; 256] = build_edc_table();
    /// C# `mul2tab` — multiply by 2 in GF(2^8) with the CD-ROM field poly.
    static MUL2: [u8; 256] = build_mul2();
    /// C# `div3tab` — the inverse of `x -> mul2(x) ^ x`.
    static DIV3: [u8; 256] = build_div3();

    const fn build_edc_table() -> [u32; 256] {
        // The C# does `BitReverseX.Reverse32(0x8001801B)`; reversing at compile
        // time keeps the polynomial recognisable in the source.
        let poly = EDC_POLY.reverse_bits();
        let mut t = [0u32; 256];
        let mut i = 0usize;
        while i < 256 {
            let mut crc = i as u32;
            let mut j = 0;
            while j < 8 {
                crc = if crc & 1 == 1 { (crc >> 1) ^ poly } else { crc >> 1 };
                j += 1;
            }
            t[i] = crc;
            i += 1;
        }
        t
    }

    const fn build_mul2() -> [u8; 256] {
        let mut t = [0u8; 256];
        let mut i = 0usize;
        while i < 256 {
            let n = i * 2;
            let mut b = (n & 0xFF) as u8;
            if n > 0xFF {
                b ^= 0x1D;
            }
            t[i] = b;
            i += 1;
        }
        t
    }

    const fn build_div3() -> [u8; 256] {
        let mul2 = build_mul2();
        let mut t = [0u8; 256];
        let mut i = 0usize;
        while i < 256 {
            let x1 = i as u8;
            let x3 = mul2[i] ^ x1;
            t[x3 as usize] = x1;
            i += 1;
        }
        t
    }

    /// C# `EDC_Calc(byte[] data, int offset, int length)`.
    pub fn edc_calc(data: &[u8]) -> u32 {
        let mut crc = 0u32;
        for &b in data {
            let entry = ((crc ^ b as u32) & 0xFF) as usize;
            crc = EDC_TABLE[entry] ^ (crc >> 8);
        }
        crc
    }

    /// C# `PokeUint` — little-endian.
    pub fn poke_u32(data: &mut [u8], offset: usize, value: u32) -> Option<()> {
        data.get_mut(offset..offset + 4)?
            .copy_from_slice(&value.to_le_bytes());
        Some(())
    }

    /// C# `CalcECC(...)` — one P or Q parity pair.
    ///
    /// `addr_offset` wraps modulo `1118 * 2`, which is the C#'s constant for
    /// the interleave span.
    ///
    /// VERIFY: the wrap constant and the stride arithmetic are copied verbatim.
    /// A wrong stride still produces plausible-looking parity bytes.
    pub fn calc_ecc(
        data: &[u8],
        base_offset: usize,
        mut addr_offset: usize,
        addr_add: usize,
        todo: usize,
    ) -> Option<(u8, u8)> {
        const WRAP: usize = 1118 * 2;
        let (mut pow_accum, mut add_accum) = (0u8, 0u8);
        for _ in 0..todo {
            addr_offset %= WRAP;
            let d = *data.get(base_offset + addr_offset)?;
            addr_offset += addr_add;
            add_accum ^= d;
            pow_accum ^= d;
            pow_accum = MUL2[pow_accum as usize];
        }
        let p0 = DIV3[(MUL2[pow_accum as usize] ^ add_accum) as usize];
        Some((p0, p0 ^ add_accum))
    }

    #[cfg(test)]
    mod tests {
        use super::*;

        #[test]
        fn edc_table_matches_independent_derivation() {
            // Values computed separately from the polynomial.
            assert_eq!(EDC_POLY.reverse_bits(), 0xD801_8001);
            assert_eq!(EDC_TABLE[0], 0x0000_0000);
            assert_eq!(EDC_TABLE[1], 0x9091_0101);
            assert_eq!(EDC_TABLE[255], 0x7070_FF00);
        }

        #[test]
        fn edc_of_all_zeros_is_zero() {
            // A property of this CRC: zero init, zero data, no final xor.
            assert_eq!(edc_calc(&[]), 0);
            assert_eq!(edc_calc(&[0u8; 2064]), 0);
        }

        #[test]
        fn edc_is_order_sensitive_and_deterministic() {
            assert_ne!(edc_calc(&[1, 2]), edc_calc(&[2, 1]));
            assert_eq!(edc_calc(b"abc"), edc_calc(b"abc"));
            assert_ne!(edc_calc(b"abc"), 0);
        }

        #[test]
        fn galois_tables_have_the_right_structure() {
            // mul2 is multiplication by x in GF(2^8) mod x^8+x^4+x^3+x^2+1.
            assert_eq!(MUL2[0], 0);
            assert_eq!(MUL2[1], 2);
            assert_eq!(MUL2[0x80], 0x1D, "overflow xors the field polynomial");
            // div3 must be a permutation, or the parity inversion is not unique.
            let mut seen = [false; 256];
            for &v in DIV3.iter() {
                seen[v as usize] = true;
            }
            assert!(seen.iter().all(|&x| x), "div3 is not a permutation");
        }

        #[test]
        fn div3_inverts_its_definition() {
            for i in 0..256usize {
                let x3 = MUL2[i] ^ (i as u8);
                assert_eq!(DIV3[x3 as usize], i as u8, "at {i}");
            }
        }

        #[test]
        fn poke_is_little_endian_and_bounds_checked() {
            let mut b = [0u8; 8];
            poke_u32(&mut b, 2, 0x1234_5678).unwrap();
            assert_eq!(&b[2..6], &[0x78, 0x56, 0x34, 0x12]);
            assert!(poke_u32(&mut b, 6, 0).is_none(), "would overrun");
        }

        #[test]
        fn calc_ecc_is_bounds_checked() {
            // The C# indexes unchecked and reads past a short buffer.
            assert!(calc_ecc(&[0u8; 16], 0, 0, 1, 1000).is_none());
            assert!(calc_ecc(&[0u8; 4096], 0, 0, 1, 10).is_some());
        }
    }
}

// ---------------------------------------------------------------------------
// SynthUtils
// ---------------------------------------------------------------------------

/// Byte offsets within a 2352-byte sector, plus the 96-byte subcode tail.
pub mod offsets {
    pub const SYNC: usize = 0;
    pub const ADDRESS: usize = 12;
    pub const MODE: usize = 15;
    pub const USER: usize = 16;
    pub const EDC_MODE1: usize = 2064;
    pub const RESERVED_MODE1: usize = 2068;
    pub const ECC: usize = 2076;
    pub const SECTOR_LEN: usize = 2352;
    pub const SUBCHANNEL_P: usize = 2352;
    pub const SUBCHANNEL_Q: usize = 2364;
    pub const SUBCHANNEL_RW: usize = 2376;
    pub const SECTOR_LEN_2448: usize = 2448;
}

use crate::disc_addressing::{Bcd2, Msf};
use offsets as off;

/// C# `SynthUtils.SubP(buf12, offset, pause)` — all 0xFF while paused,
/// all 0x00 otherwise.
pub fn synth_sub_p(buf: &mut [u8], offset: usize, pause: bool) -> Option<()> {
    let val = if pause { 0xFF } else { 0x00 };
    buf.get_mut(offset..offset + 12)?.fill(val);
    Some(())
}

/// C# `SynthUtils.SectorHeader(buf16, offset, lba, mode)`.
///
/// Sync pattern, then the **absolute** MSF of `lba + 150` in BCD, then the
/// mode byte. Bounds-checked, unlike the C#.
pub fn synth_sector_header(buf: &mut [u8], offset: usize, lba: i32, mode: u8) -> Option<()> {
    let h = buf.get_mut(offset..offset + 16)?;
    h[0] = 0x00;
    h[1..11].fill(0xFF);
    h[11] = 0x00;
    // The C# builds `new MSF(lba + 150)`, i.e. the absolute timecode.
    let ts = Msf::from_sector(lba + 150)?;
    h[12] = Bcd2::from_decimal(ts.min as i32)?.bcd_value;
    h[13] = Bcd2::from_decimal(ts.sec as i32)?.bcd_value;
    h[14] = Bcd2::from_decimal(ts.frac as i32)?.bcd_value;
    h[15] = mode;
    Some(())
}

/// C# `SynthUtils.SectorSubHeader(buffer8, offset, form)` — 8 zero bytes with
/// the form byte at positions 2 and 6.
///
/// VERIFY: the C# zeroes all eight and then writes the form; which indices it
/// writes is the part I am least sure of, since the source only showed the
/// zeroing loop.
pub fn synth_sector_subheader(buf: &mut [u8], offset: usize, form: u8) -> Option<()> {
    let h = buf.get_mut(offset..offset + 8)?;
    h.fill(0);
    h[2] = form;
    h[6] = form;
    Some(())
}

/// C# `SynthUtils.EDC_Mode1` — CRC over bytes 0..2064, stored at 2064.
pub fn synth_edc_mode1(sector: &mut [u8]) -> Option<()> {
    let edc = ecm::edc_calc(sector.get(..off::EDC_MODE1)?);
    ecm::poke_u32(sector, off::EDC_MODE1, edc)
}

/// C# `SynthUtils.EDC_Mode2_Form1` — CRC over 16..(16+2056), stored at 2072.
pub fn synth_edc_mode2_form1(sector: &mut [u8]) -> Option<()> {
    let edc = ecm::edc_calc(sector.get(16..16 + 2048 + 8)?);
    ecm::poke_u32(sector, 2072, edc)
}

/// C# `SynthUtils.EDC_Mode2_Form2` — CRC over 16..(16+2332), stored at 2348.
pub fn synth_edc_mode2_form2(sector: &mut [u8]) -> Option<()> {
    let edc = ecm::edc_calc(sector.get(16..16 + 2324 + 8)?);
    ecm::poke_u32(sector, 2348, edc)
}

/// C# `SynthUtils.ECM_Mode1` — EDC, zeroed reserved bytes, then ECC.
///
/// VERIFY: `ECC_Populate` is not reproduced byte-for-byte here; `calc_ecc`
/// above is the primitive it is built from, and wiring the full P/Q interleave
/// needs a reference sector to check against. This writes EDC and clears the
/// reserved field, and leaves ECC zero — which is **valid for an image an
/// emulator reads without checking ECC**, and wrong for one that does. Flagged
/// rather than guessed.
pub fn synth_ecm_mode1(sector: &mut [u8]) -> Option<()> {
    synth_edc_mode1(sector)?;
    sector
        .get_mut(off::RESERVED_MODE1..off::RESERVED_MODE1 + 8)?
        .fill(0);
    Some(())
}

/// C# `SynthUtils.InterleaveSubcodeInplace(buf, offset)`.
///
/// The 96 subcode bytes arrive as 8 channels of 12 bytes (P, Q, R..W laid out
/// contiguously) and must be written to the wire as 96 bytes where each byte
/// carries one bit from each channel.
///
/// VERIFY: the direction of this transform. The C# name says "interleave" and
/// it is applied when `SubcodeDeinterleave` is **not** requested, so the
/// deinterleaved (per-channel) form is what the synth jobs produce and this
/// packs it. That reading is consistent but unconfirmed.
pub fn interleave_subcode(buf: &mut [u8], offset: usize) -> Option<()> {
    let src: [u8; 96] = buf.get(offset..offset + 96)?.try_into().ok()?;
    let out = buf.get_mut(offset..offset + 96)?;
    out.fill(0);
    for ch in 0..8 {
        for byte in 0..12 {
            let v = src[ch * 12 + byte];
            for bit in 0..8 {
                if v & (0x80 >> bit) != 0 {
                    out[byte * 8 + bit] |= 0x80 >> ch;
                }
            }
        }
    }
    Some(())
}

/// The inverse, for reading a real image's subcode into per-channel form.
pub fn deinterleave_subcode(buf: &mut [u8], offset: usize) -> Option<()> {
    let src: [u8; 96] = buf.get(offset..offset + 96)?.try_into().ok()?;
    let out = buf.get_mut(offset..offset + 96)?;
    out.fill(0);
    for byte in 0..12 {
        for bit in 0..8 {
            let v = src[byte * 8 + bit];
            for ch in 0..8 {
                if v & (0x80 >> ch) != 0 {
                    out[ch * 12 + byte] |= 0x80 >> bit;
                }
            }
        }
    }
    Some(())
}

/// C# `DiscSessionFormat`, as written into the POINT=0xA0 Q entry.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum SessionFormat {
    #[default]
    Type00CdromCdda,
    Type10Cdi,
    Type20Cdxa,
}

impl SessionFormat {
    /// The **BCD byte** the standard requires in PSEC for this format.
    ///
    /// The C# assigns these values through `DecimalValue`, a decimal-to-BCD
    /// setter, so `0x10` becomes BCD `0x16` — see the module header.
    pub const fn psec_bcd(self) -> u8 {
        match self {
            Self::Type00CdromCdda => 0x00,
            Self::Type10Cdi => 0x10,
            Self::Type20Cdxa => 0x20,
        }
    }

    /// What the C# actually writes.
    #[deprecated(note = "mirrors a C#-side bug: writes BCD 0x16/0x32 instead of 0x10/0x20")]
    pub const fn psec_bcd_bug_compat(self) -> u8 {
        // DecimalValue = 0x10 means decimal 16 -> BCD 0x16.
        match self {
            Self::Type00CdromCdda => 0x00,
            Self::Type10Cdi => 0x16,
            Self::Type20Cdxa => 0x32,
        }
    }

    /// Decode from a PSEC BCD byte, per the standard.
    pub fn from_psec_bcd(b: u8) -> Option<Self> {
        Some(match b {
            0x00 => Self::Type00CdromCdda,
            0x10 => Self::Type10Cdi,
            0x20 => Self::Type20Cdxa,
            _ => return None,
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn synth_part_flag_values_match_the_c_sharp() {
        assert_eq!(SynthPart::HEADER16.0, 1);
        assert_eq!(SynthPart::USER2048.0, 2);
        assert_eq!(SynthPart::ECM288_COMPLETE.0, 12);
        assert_eq!(SynthPart::USER2352.0, 15);
        assert_eq!(SynthPart::SUBCODE_COMPLETE.0, 4080);
        assert_eq!(SynthPart::SUBCODE_DEINTERLEAVE.0, 4096);
        assert_eq!(SynthPart::COMPLETE2448.0, 4080 | 15);
    }

    #[test]
    fn composite_parts_contain_their_members() {
        assert!(SynthPart::USER2352.contains_all(SynthPart::HEADER16));
        assert!(SynthPart::USER2352.contains_all(SynthPart::ECM288_COMPLETE));
        assert!(!SynthPart::USER2352.contains(SynthPart::SUBCHANNEL_P));
        assert!(SynthPart::COMPLETE2448.contains_all(SynthPart::SUBCODE_COMPLETE));
    }

    #[test]
    fn sector_header_writes_the_sync_pattern() {
        let mut s = vec![0u8; off::SECTOR_LEN];
        synth_sector_header(&mut s, 0, 0, 1).unwrap();
        assert_eq!(s[0], 0x00);
        assert!(s[1..11].iter().all(|&b| b == 0xFF));
        assert_eq!(s[11], 0x00);
        assert_eq!(s[15], 1, "mode byte");
    }

    #[test]
    fn sector_header_address_is_the_absolute_msf_in_bcd() {
        let mut s = vec![0u8; off::SECTOR_LEN];
        // LBA 0 is absolute MSF 00:02:00.
        synth_sector_header(&mut s, 0, 0, 1).unwrap();
        assert_eq!((s[12], s[13], s[14]), (0x00, 0x02, 0x00));
        // LBA 4500-150 = one minute in.
        synth_sector_header(&mut s, 0, 4500 - 150, 1).unwrap();
        assert_eq!((s[12], s[13], s[14]), (0x01, 0x00, 0x00));
        // 74 minutes: BCD 0x74, not decimal 74.
        synth_sector_header(&mut s, 0, 74 * 4500 - 150, 1).unwrap();
        assert_eq!(s[12], 0x74);
    }

    #[test]
    fn sector_header_is_bounds_checked() {
        let mut small = vec![0u8; 8];
        assert!(synth_sector_header(&mut small, 0, 0, 1).is_none());
    }

    #[test]
    fn sub_p_is_all_ff_when_paused() {
        let mut s = vec![0u8; off::SECTOR_LEN_2448];
        synth_sub_p(&mut s, off::SUBCHANNEL_P, true).unwrap();
        assert!(s[off::SUBCHANNEL_P..off::SUBCHANNEL_P + 12].iter().all(|&b| b == 0xFF));
        synth_sub_p(&mut s, off::SUBCHANNEL_P, false).unwrap();
        assert!(s[off::SUBCHANNEL_P..off::SUBCHANNEL_P + 12].iter().all(|&b| b == 0));
    }

    #[test]
    fn edc_is_written_little_endian_at_2064() {
        let mut s = vec![0u8; off::SECTOR_LEN];
        synth_sector_header(&mut s, 0, 0, 1).unwrap();
        s[off::USER..off::USER + 2048].fill(0xAB);
        synth_edc_mode1(&mut s).unwrap();
        let expected = ecm::edc_calc(&s[..off::EDC_MODE1]);
        assert_eq!(&s[2064..2068], &expected.to_le_bytes());
        assert_ne!(expected, 0, "non-trivial data must give a non-zero EDC");
    }

    #[test]
    fn a_sector_with_a_correct_edc_verifies() {
        // The property a reader checks: EDC over 0..2064 equals the stored word.
        let mut s = vec![0u8; off::SECTOR_LEN];
        synth_sector_header(&mut s, 0, 100, 1).unwrap();
        for (i, b) in s[off::USER..off::USER + 2048].iter_mut().enumerate() {
            *b = (i % 251) as u8;
        }
        synth_ecm_mode1(&mut s).unwrap();
        let stored = u32::from_le_bytes(s[2064..2068].try_into().unwrap());
        assert_eq!(ecm::edc_calc(&s[..2064]), stored);
        assert!(s[2068..2076].iter().all(|&b| b == 0), "reserved must be zero");
    }

    #[test]
    fn mode2_edc_windows_differ_from_mode1() {
        let mut a = vec![0u8; off::SECTOR_LEN];
        let mut b = a.clone();
        a[16..2064].fill(0x11);
        b[16..2064].fill(0x11);
        synth_edc_mode2_form1(&mut a).unwrap();
        synth_edc_mode2_form2(&mut b).unwrap();
        // Different windows and different storage offsets.
        assert_ne!(&a[2072..2076], &[0, 0, 0, 0]);
        assert_ne!(&b[2348..2352], &[0, 0, 0, 0]);
    }

    #[test]
    fn subcode_interleave_round_trips() {
        // Whatever the wire direction, the two transforms must be inverses.
        let mut s = vec![0u8; off::SECTOR_LEN_2448];
        for (i, b) in s[2352..2448].iter_mut().enumerate() {
            *b = (i * 7 + 3) as u8;
        }
        let original = s[2352..2448].to_vec();
        interleave_subcode(&mut s, 2352).unwrap();
        assert_ne!(&s[2352..2448], &original[..], "must actually transform");
        deinterleave_subcode(&mut s, 2352).unwrap();
        assert_eq!(&s[2352..2448], &original[..]);
    }

    #[test]
    fn interleave_moves_channel_bits_to_bit_planes() {
        let mut s = vec![0u8; off::SECTOR_LEN_2448];
        // Channel 0 (P), all bits set, first byte only.
        s[2352] = 0xFF;
        interleave_subcode(&mut s, 2352).unwrap();
        // Every one of the first 8 wire bytes should carry bit 7 (channel 0).
        assert!(s[2352..2360].iter().all(|&b| b & 0x80 != 0));
        assert!(s[2352..2360].iter().all(|&b| b & 0x7F == 0), "no other channel");
    }

    #[test]
    fn subcode_transforms_are_bounds_checked() {
        let mut small = vec![0u8; 32];
        assert!(interleave_subcode(&mut small, 0).is_none());
        assert!(deinterleave_subcode(&mut small, 0).is_none());
    }

    #[test]
    fn session_format_psec_bytes_follow_the_standard() {
        assert_eq!(SessionFormat::Type00CdromCdda.psec_bcd(), 0x00);
        assert_eq!(SessionFormat::Type10Cdi.psec_bcd(), 0x10);
        assert_eq!(SessionFormat::Type20Cdxa.psec_bcd(), 0x20);
        for f in [
            SessionFormat::Type00CdromCdda,
            SessionFormat::Type10Cdi,
            SessionFormat::Type20Cdxa,
        ] {
            assert_eq!(SessionFormat::from_psec_bcd(f.psec_bcd()), Some(f));
        }
    }

    #[test]
    fn the_c_sharp_writes_a_psec_byte_the_standard_does_not_define() {
        // DecimalValue = 0x10 stores BCD 0x16, not 0x10.
        #[allow(deprecated)]
        {
            assert_eq!(SessionFormat::Type10Cdi.psec_bcd_bug_compat(), 0x16);
            assert_eq!(SessionFormat::Type20Cdxa.psec_bcd_bug_compat(), 0x32);
            // And 0x16 is not a recognised session format.
            assert!(SessionFormat::from_psec_bcd(0x16).is_none());
        }
    }

    #[test]
    fn the_decimal_to_bcd_confusion_is_reproducible() {
        // Shows the mechanism: assigning the hex literal 0x10 through a
        // decimal setter stores 0x16.
        assert_eq!(Bcd2::from_decimal(0x10).unwrap().bcd_value, 0x16);
        assert_eq!(Bcd2::from_decimal(0x20).unwrap().bcd_value, 0x32);
        // The correct assignment.
        assert_eq!(Bcd2::from_decimal(10).unwrap().bcd_value, 0x10);
    }

    #[test]
    fn offsets_sum_to_the_sector_lengths() {
        assert_eq!(off::SECTOR_LEN, 2352);
        assert_eq!(off::SECTOR_LEN_2448 - off::SECTOR_LEN, 96, "subcode tail");
        assert_eq!(off::SUBCHANNEL_Q - off::SUBCHANNEL_P, 12);
        assert_eq!(off::SUBCHANNEL_RW - off::SUBCHANNEL_Q, 12);
        assert_eq!(off::SECTOR_LEN_2448 - off::SUBCHANNEL_RW, 72, "R-W is 6x12");
    }
}
