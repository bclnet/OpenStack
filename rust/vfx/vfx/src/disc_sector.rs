// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (Blob / DiscSector / Sbi / Records)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// ==========================================================================
// UNVERIFIED against real media, written blind at request. The Sbi record
// layout and the sector-read policies are traceable from the C#; the sector
// *contents* they produce depend on `disc_synth.rs`, whose layout caveats
// apply here too.
// ==========================================================================
//
// The plumbing that wires the format readers to a sector-level API: the `Blob`
// data source, the read policies, `.sbi` subchannel patches, and the TOC model.
//
// ===================== FIVE C#-SIDE BUGS =================================
//
//   1. **`Blob_CHD.Read` corrupts any read crossing a hunk boundary.**
//
//          while (count > 0) {
//              var targetHunk = (uint)(byte_pos / _hunkSize);
//              ...
//              var hunkOffset = (uint)(byte_pos - targetHunk * _hunkSize);
//              var bytesToCopy = Math.Min((int)(_hunkSize - hunkOffset), count);
//              Buffer.BlockCopy(_hunkCache, (int)hunkOffset, buffer, offset, bytesToCopy);
//              offset += bytesToCopy;
//              count -= bytesToCopy;
//          }
//
//      **`byte_pos` is never advanced.** So `targetHunk` and `hunkOffset` never
//      change: the second iteration re-copies the *same* source bytes to the
//      next destination offset. A read spanning a hunk boundary duplicates the
//      tail of the first hunk instead of continuing into the next one. It
//      terminates (because `count` shrinks) and returns the full count, so the
//      caller sees success and wrong data. **Fix this in the C# tree** by adding
//      `byte_pos += bytesToCopy;`.
//
//   2. **`DiscSectorReader.ReadLBA_2448` clears only 2352 bytes** —
//      `PrepareBuffer(buffer, offset, 2352)` — then writes 2448. With
//      `DeterministicClearBuffer` set, the trailing 96 subcode bytes keep
//      whatever the caller's buffer held, which is exactly the determinism the
//      policy exists to provide.
//
//   3. **`SbiLoader` accumulates every record's patch into one flat list.**
//      `bytes` is a single `List<short>` appended across all records, and
//      `Abas` collects the addresses separately — so the association between an
//      address and its 12 patch entries is positional and implicit. A
//      malformed record that appends the wrong count silently shifts every
//      later patch onto the wrong sector.
//
//   4. **`SbiLoader` stores patch bytes as `short` with -1 for "no change"**,
//      then assigns them into a `byte[]` subchannel elsewhere. The sentinel and
//      the data share a type with no wrapper, so a missing -1 check writes 0xFF.
//
//   5. **The record loop tests `s.Position == s.Length` for termination**, so a
//      stream whose position has overshot (which the unchecked reads above can
//      cause) never equals the length and the loop runs past the end.

use crate::disc_addressing::{Bcd2, Msf};
use crate::disc_synth::offsets as off;

/// C# `abstract class Blob` — a byte source addressed by absolute position.
pub trait Blob {
    /// C# `Read(long byte_pos, byte[] buffer, int offset, int count)`.
    ///
    /// Returns the number of bytes actually read. The C#'s implementations
    /// return the requested count regardless of what happened — see bug 1.
    fn read_at(&mut self, byte_pos: u64, buffer: &mut [u8]) -> std::io::Result<usize>;
}

/// A `Blob` over an in-memory buffer, for tests and for small images.
#[derive(Debug, Clone)]
pub struct MemoryBlob(pub Vec<u8>);

impl Blob for MemoryBlob {
    fn read_at(&mut self, byte_pos: u64, buffer: &mut [u8]) -> std::io::Result<usize> {
        let start = byte_pos as usize;
        if start >= self.0.len() {
            return Ok(0);
        }
        let n = buffer.len().min(self.0.len() - start);
        buffer[..n].copy_from_slice(&self.0[start..start + n]);
        Ok(n)
    }
}

/// A hunk-cached `Blob`, the shape `Blob_CHD` has.
///
/// This is the corrected form of bug 1: `pos` advances with each chunk copied,
/// so a read spanning hunks continues into the next one.
pub struct HunkedBlob<F> {
    hunk_size: usize,
    cache: Vec<u8>,
    current_hunk: Option<u64>,
    /// Loads hunk `n` into the provided buffer.
    load: F,
}

impl<F> HunkedBlob<F>
where
    F: FnMut(u64, &mut [u8]) -> std::io::Result<()>,
{
    pub fn new(hunk_size: usize, load: F) -> Option<Self> {
        if hunk_size == 0 {
            return None; // the C# would divide by zero
        }
        Some(Self {
            hunk_size,
            cache: vec![0; hunk_size],
            current_hunk: None,
            load,
        })
    }
}

impl<F> Blob for HunkedBlob<F>
where
    F: FnMut(u64, &mut [u8]) -> std::io::Result<()>,
{
    fn read_at(&mut self, byte_pos: u64, buffer: &mut [u8]) -> std::io::Result<usize> {
        let mut pos = byte_pos;
        let mut written = 0usize;
        while written < buffer.len() {
            let target = pos / self.hunk_size as u64;
            if self.current_hunk != Some(target) {
                (self.load)(target, &mut self.cache)?;
                self.current_hunk = Some(target);
            }
            let hunk_offset = (pos - target * self.hunk_size as u64) as usize;
            let avail = self.hunk_size - hunk_offset;
            let n = avail.min(buffer.len() - written);
            buffer[written..written + n]
                .copy_from_slice(&self.cache[hunk_offset..hunk_offset + n]);
            written += n;
            // The line the C# is missing.
            pos += n as u64;
        }
        Ok(written)
    }
}

/// C# `DiscSectorReaderPolicy.EUserData2048Mode`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum UserData2048Mode {
    #[default]
    InspectSector,
    AssumeMode1,
    AssumeMode2Form1,
    InspectSectorAssumeForm1,
}

/// C# `class DiscSectorReaderPolicy`.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct SectorReaderPolicy {
    pub user_data_2048_mode: UserData2048Mode,
    pub throw_exceptions_2048: bool,
    pub deinterleaved_subcode: bool,
    pub deterministic_clear_buffer: bool,
}

impl Default for SectorReaderPolicy {
    /// The C#'s field initialisers.
    fn default() -> Self {
        Self {
            user_data_2048_mode: UserData2048Mode::InspectSector,
            throw_exceptions_2048: true,
            deinterleaved_subcode: true,
            deterministic_clear_buffer: true,
        }
    }
}

/// The mode byte at offset 15 of a raw sector.
pub fn sector_mode(sector: &[u8]) -> Option<u8> {
    sector.get(off::MODE).copied()
}

/// Where the 2048 bytes of user data live for a given mode, per the C#'s
/// `ReadLBA_2048_*` family.
///
/// * Mode 1: 16..2064
/// * Mode 2 Form 1: 24..2072 (an 8-byte subheader precedes the data)
///
/// VERIFY: the Mode 2 offset follows from the subheader being at 16..24, which
/// `SectorSubHeader` writes; the C# never states it in one place.
pub fn user_data_2048_range(mode: u8) -> Option<std::ops::Range<usize>> {
    Some(match mode {
        1 => 16..2064,
        2 => 24..2072,
        _ => return None,
    })
}

/// C# `DiscSectorReader.ReadLBA_2048(...)` — extract cooked user data from a
/// raw 2352-byte sector according to the policy.
pub fn read_user_2048(
    sector: &[u8],
    policy: &SectorReaderPolicy,
    out: &mut [u8],
) -> Result<usize, SectorError> {
    if sector.len() < off::SECTOR_LEN {
        return Err(SectorError::ShortSector(sector.len()));
    }
    if out.len() < 2048 {
        return Err(SectorError::ShortDestination(out.len()));
    }
    let mode = match policy.user_data_2048_mode {
        UserData2048Mode::AssumeMode1 => 1,
        UserData2048Mode::AssumeMode2Form1 => 2,
        UserData2048Mode::InspectSector | UserData2048Mode::InspectSectorAssumeForm1 => {
            sector_mode(sector).ok_or(SectorError::ShortSector(sector.len()))?
        }
    };
    let range = user_data_2048_range(mode).ok_or(SectorError::UnknownMode(mode))?;
    out[..2048].copy_from_slice(&sector[range]);
    Ok(2048)
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SectorError {
    ShortSector(usize),
    ShortDestination(usize),
    /// C# throws or returns 0 here depending on `ThrowExceptions2048`.
    UnknownMode(u8),
}

impl std::fmt::Display for SectorError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::ShortSector(n) => write!(f, "sector is {n} bytes, need {}", off::SECTOR_LEN),
            Self::ShortDestination(n) => write!(f, "destination is {n} bytes, need 2048"),
            Self::UnknownMode(m) => write!(f, "cannot extract 2048 bytes from mode {m}"),
        }
    }
}

impl std::error::Error for SectorError {}

/// One `.sbi` patch: a sector address plus 12 optional subchannel-Q bytes.
///
/// The C# keeps the addresses in one list and all the patch bytes in another,
/// flat across every record, with the association positional (bug 3). Pairing
/// them makes a miscount impossible.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct SbiPatch {
    /// Absolute sector this patch applies to.
    pub aba: i32,
    /// `None` means "leave this byte alone" — the C#'s `-1` sentinel stored in
    /// a `short` alongside real data (bug 4).
    pub subq: [Option<u8>; 12],
}

impl SbiPatch {
    /// Apply this patch to a 12-byte subchannel-Q buffer.
    pub fn apply(&self, subq: &mut [u8]) -> Option<()> {
        let q = subq.get_mut(..12)?;
        for (i, v) in self.subq.iter().enumerate() {
            if let Some(b) = v {
                q[i] = *b;
            }
        }
        Some(())
    }
}

/// C# `SbiLoader`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct SbiFile {
    pub patches: Vec<SbiPatch>,
}

#[derive(Debug, Clone, PartialEq)]
pub enum SbiError {
    BadMagic(u32),
    BrokenRecord { at: u64 },
    /// The record's MSF components were out of range.
    BadTimestamp { at: u64 },
    UnknownRecordType { at: u64, kind: u8 },
}

impl std::fmt::Display for SbiError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::BadMagic(m) => write!(f, "bad SBI magic {m:#010x}"),
            Self::BrokenRecord { at } => write!(f, "broken SBI record at {at}"),
            Self::BadTimestamp { at } => write!(f, "bad SBI timestamp at {at}"),
            Self::UnknownRecordType { at, kind } => {
                write!(f, "unknown SBI record type {kind} at {at}")
            }
        }
    }
}

impl std::error::Error for SbiError {}

impl SbiFile {
    /// C# `SbiLoader.MAGIC` — "SBI\0".
    pub const MAGIC: u32 = 0x0049_4253;

    /// C# `SbiLoader(FileSystem, string path)`, over bytes.
    ///
    /// The loop is driven by remaining length rather than
    /// `Position == Length` (bug 5), so an overshoot cannot run past the end.
    pub fn parse(data: &[u8]) -> Result<Self, SbiError> {
        if data.len() < 4 {
            return Err(SbiError::BrokenRecord { at: 0 });
        }
        let magic = u32::from_le_bytes(data[..4].try_into().unwrap());
        if magic != Self::MAGIC {
            return Err(SbiError::BadMagic(magic));
        }
        let mut me = Self::default();
        let mut i = 4usize;
        while i < data.len() {
            let at = i as u64;
            if i + 4 > data.len() {
                return Err(SbiError::BrokenRecord { at });
            }
            let ts = Msf::new(
                Bcd2::from_bcd(data[i]).decimal_value(),
                Bcd2::from_bcd(data[i + 1]).decimal_value(),
                Bcd2::from_bcd(data[i + 2]).decimal_value(),
            )
            .ok_or(SbiError::BadTimestamp { at })?;
            let kind = data[i + 3];
            i += 4;
            let mut subq = [None; 12];
            // Each record type supplies a different contiguous span of Q bytes.
            let (start, count) = match kind {
                1 => (0usize, 10usize), // Q0..Q9
                2 => (3, 3),            // Q3..Q5
                3 => (7, 3),            // Q7..Q9
                other => return Err(SbiError::UnknownRecordType { at, kind: other }),
            };
            if i + count > data.len() {
                return Err(SbiError::BrokenRecord { at });
            }
            for k in 0..count {
                subq[start + k] = Some(data[i + k]);
            }
            i += count;
            me.patches.push(SbiPatch { aba: ts.sector(), subq });
        }
        Ok(me)
    }

    /// The patch for a given absolute sector, if any.
    pub fn patch_for(&self, aba: i32) -> Option<&SbiPatch> {
        self.patches.iter().find(|p| p.aba == aba)
    }
}

/// C# `DiscTOC.TOCItem`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct TocItem {
    pub lba: i32,
    pub control: u8,
    pub exists: bool,
}

impl TocItem {
    /// Bit 2 of the control field marks a data track.
    pub const fn is_data(&self) -> bool {
        self.control & 0x04 != 0
    }
}

/// C# `DiscTOC`.
#[derive(Debug, Clone, PartialEq)]
pub struct DiscToc {
    /// Indices 1..=99 are tracks; 100 is the lead-out. Index 0 is unused, which
    /// the C# also reserves ("arguably could be -150, but let's not just yet").
    pub items: [TocItem; 101],
    pub first_recorded_track_number: i32,
    pub last_recorded_track_number: i32,
    pub session_format: crate::disc_synth::SessionFormat,
}

impl Default for DiscToc {
    fn default() -> Self {
        Self {
            items: [TocItem::default(); 101],
            first_recorded_track_number: 0,
            last_recorded_track_number: 0,
            session_format: Default::default(),
        }
    }
}

impl DiscToc {
    /// The lead-out entry, which the C# stores at index 100.
    pub fn leadout(&self) -> &TocItem {
        &self.items[100]
    }

    /// Tracks that exist, in number order.
    pub fn tracks(&self) -> impl Iterator<Item = (usize, &TocItem)> {
        self.items
            .iter()
            .enumerate()
            .skip(1)
            .take(99)
            .filter(|(_, t)| t.exists)
    }

    /// Length of a track in sectors, from its LBA to the next existing entry.
    pub fn track_length(&self, number: usize) -> Option<i32> {
        if number == 0 || number > 99 || !self.items[number].exists {
            return None;
        }
        let start = self.items[number].lba;
        let next = (number + 1..=100)
            .find(|&n| self.items[n].exists || n == 100)
            .map(|n| self.items[n].lba)?;
        Some(next - start)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn hunked_reads_spanning_a_boundary_continue_into_the_next_hunk() {
        // This is the bug-1 regression test. The C# re-copies the first hunk.
        const HUNK: usize = 16;
        let mut b = HunkedBlob::new(HUNK, |n, buf: &mut [u8]| {
            // Hunk n is filled with the byte value n.
            buf.fill(n as u8);
            Ok(())
        })
        .unwrap();
        let mut out = [0u8; 24];
        // Start 8 bytes into hunk 0 and read 24: 8 from hunk 0, 16 from hunk 1.
        assert_eq!(b.read_at(8, &mut out).unwrap(), 24);
        assert!(out[..8].iter().all(|&x| x == 0), "tail of hunk 0");
        assert!(out[8..].iter().all(|&x| x == 1), "must advance to hunk 1");
    }

    #[test]
    fn hunked_reads_spanning_three_hunks_work() {
        const HUNK: usize = 8;
        let mut b = HunkedBlob::new(HUNK, |n, buf: &mut [u8]| {
            buf.fill(n as u8);
            Ok(())
        })
        .unwrap();
        let mut out = [0u8; 20];
        b.read_at(4, &mut out).unwrap();
        assert_eq!(&out[..4], &[0; 4]);
        assert_eq!(&out[4..12], &[1; 8]);
        assert_eq!(&out[12..20], &[2; 8]);
    }

    #[test]
    fn a_zero_hunk_size_is_rejected() {
        // The C# divides by _hunkSize with no check.
        assert!(HunkedBlob::new(0, |_, _: &mut [u8]| Ok(())).is_none());
    }

    #[test]
    fn memory_blob_clamps_at_the_end() {
        let mut b = MemoryBlob((0u8..10).collect());
        let mut out = [0u8; 20];
        assert_eq!(b.read_at(5, &mut out).unwrap(), 5);
        assert_eq!(&out[..5], &[5, 6, 7, 8, 9]);
        assert_eq!(b.read_at(100, &mut out).unwrap(), 0);
    }

    #[test]
    fn policy_defaults_match_the_c_sharp_field_initialisers() {
        let p = SectorReaderPolicy::default();
        assert_eq!(p.user_data_2048_mode, UserData2048Mode::InspectSector);
        assert!(p.throw_exceptions_2048);
        assert!(p.deinterleaved_subcode);
        assert!(p.deterministic_clear_buffer);
    }

    #[test]
    fn user_data_ranges_are_2048_bytes_wide() {
        for mode in [1u8, 2] {
            let r = user_data_2048_range(mode).unwrap();
            assert_eq!(r.len(), 2048, "mode {mode}");
            assert!(r.end <= off::SECTOR_LEN);
        }
        assert!(user_data_2048_range(3).is_none());
    }

    #[test]
    fn reading_user_data_respects_the_inspect_policy() {
        let mut sector = vec![0u8; off::SECTOR_LEN];
        sector[off::MODE] = 1;
        for (i, b) in sector[16..2064].iter_mut().enumerate() {
            *b = (i % 253) as u8;
        }
        let mut out = vec![0u8; 2048];
        let p = SectorReaderPolicy::default();
        assert_eq!(read_user_2048(&sector, &p, &mut out).unwrap(), 2048);
        assert_eq!(out[0], 0);
        assert_eq!(out[1], 1);
    }

    #[test]
    fn assume_mode_overrides_the_sector_byte() {
        let mut sector = vec![0u8; off::SECTOR_LEN];
        sector[off::MODE] = 1;
        sector[24] = 0xAB; // where mode 2 form 1 data starts
        let mut out = vec![0u8; 2048];
        let p = SectorReaderPolicy {
            user_data_2048_mode: UserData2048Mode::AssumeMode2Form1,
            ..Default::default()
        };
        read_user_2048(&sector, &p, &mut out).unwrap();
        assert_eq!(out[0], 0xAB, "read from the mode 2 offset");
    }

    #[test]
    fn an_unknown_mode_is_reported() {
        let mut sector = vec![0u8; off::SECTOR_LEN];
        sector[off::MODE] = 7;
        let mut out = vec![0u8; 2048];
        assert_eq!(
            read_user_2048(&sector, &SectorReaderPolicy::default(), &mut out),
            Err(SectorError::UnknownMode(7))
        );
    }

    #[test]
    fn short_buffers_are_rejected() {
        let sector = vec![0u8; 100];
        let mut out = vec![0u8; 2048];
        assert!(matches!(
            read_user_2048(&sector, &SectorReaderPolicy::default(), &mut out),
            Err(SectorError::ShortSector(100))
        ));
        let full = vec![0u8; off::SECTOR_LEN];
        let mut small = vec![0u8; 10];
        assert!(matches!(
            read_user_2048(&full, &SectorReaderPolicy::default(), &mut small),
            Err(SectorError::ShortDestination(10))
        ));
    }

    /// Builds an SBI with one record of each type.
    fn sbi_bytes() -> Vec<u8> {
        let mut v = Vec::new();
        v.extend_from_slice(&SbiFile::MAGIC.to_le_bytes());
        // Type 1 at MSF 00:02:00 (BCD), 10 bytes of Q.
        v.extend_from_slice(&[0x00, 0x02, 0x00, 1]);
        v.extend_from_slice(&[0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9]);
        // Type 2 at 00:03:00, 3 bytes covering Q3..Q5.
        v.extend_from_slice(&[0x00, 0x03, 0x00, 2]);
        v.extend_from_slice(&[0xB3, 0xB4, 0xB5]);
        // Type 3 at 00:04:00, 3 bytes covering Q7..Q9.
        v.extend_from_slice(&[0x00, 0x04, 0x00, 3]);
        v.extend_from_slice(&[0xC7, 0xC8, 0xC9]);
        v
    }

    #[test]
    fn parses_all_three_sbi_record_types() {
        let s = SbiFile::parse(&sbi_bytes()).expect("should parse");
        assert_eq!(s.patches.len(), 3);
        assert_eq!(s.patches[0].aba, Msf::new(0, 2, 0).unwrap().sector());
        assert_eq!(s.patches[1].aba, Msf::new(0, 3, 0).unwrap().sector());
    }

    #[test]
    fn record_types_patch_the_right_q_bytes() {
        let s = SbiFile::parse(&sbi_bytes()).unwrap();
        // Type 1: Q0..Q9 set, Q10/Q11 untouched.
        let p0 = &s.patches[0];
        assert_eq!(p0.subq[0], Some(0xA0));
        assert_eq!(p0.subq[9], Some(0xA9));
        assert_eq!(p0.subq[10], None);
        assert_eq!(p0.subq[11], None);
        // Type 2: only Q3..Q5.
        let p1 = &s.patches[1];
        assert_eq!(p1.subq[2], None);
        assert_eq!(p1.subq[3], Some(0xB3));
        assert_eq!(p1.subq[5], Some(0xB5));
        assert_eq!(p1.subq[6], None);
        // Type 3: only Q7..Q9.
        let p2 = &s.patches[2];
        assert_eq!(p2.subq[6], None);
        assert_eq!(p2.subq[7], Some(0xC7));
        assert_eq!(p2.subq[9], Some(0xC9));
    }

    #[test]
    fn applying_a_patch_leaves_unset_bytes_alone() {
        // The C# stores -1 as a sentinel in a short[] alongside real data
        // (bug 4); Option makes "no change" unrepresentable as data.
        let s = SbiFile::parse(&sbi_bytes()).unwrap();
        let mut q = [0x11u8; 12];
        s.patches[1].apply(&mut q).unwrap();
        assert_eq!(q[2], 0x11, "untouched");
        assert_eq!(q[3], 0xB3, "patched");
        assert_eq!(q[6], 0x11, "untouched");
    }

    #[test]
    fn bad_magic_is_rejected() {
        let mut v = sbi_bytes();
        v[0] = 0xFF;
        assert!(matches!(SbiFile::parse(&v), Err(SbiError::BadMagic(_))));
    }

    #[test]
    fn a_truncated_record_is_reported_not_read_past() {
        let mut v = sbi_bytes();
        v.truncate(v.len() - 2);
        assert!(matches!(
            SbiFile::parse(&v),
            Err(SbiError::BrokenRecord { .. })
        ));
    }

    #[test]
    fn an_unknown_record_type_is_reported() {
        let mut v = Vec::new();
        v.extend_from_slice(&SbiFile::MAGIC.to_le_bytes());
        v.extend_from_slice(&[0x00, 0x02, 0x00, 9]);
        assert!(matches!(
            SbiFile::parse(&v),
            Err(SbiError::UnknownRecordType { kind: 9, .. })
        ));
    }

    #[test]
    fn an_out_of_range_timestamp_is_reported() {
        let mut v = Vec::new();
        v.extend_from_slice(&SbiFile::MAGIC.to_le_bytes());
        // BCD 0x99 decodes to 99 seconds, which is out of range.
        v.extend_from_slice(&[0x00, 0x99, 0x00, 1]);
        v.extend_from_slice(&[0u8; 10]);
        assert!(matches!(
            SbiFile::parse(&v),
            Err(SbiError::BadTimestamp { .. })
        ));
    }

    #[test]
    fn patch_lookup_finds_by_address() {
        let s = SbiFile::parse(&sbi_bytes()).unwrap();
        let aba = Msf::new(0, 3, 0).unwrap().sector();
        assert!(s.patch_for(aba).is_some());
        assert!(s.patch_for(999_999).is_none());
    }

    #[test]
    fn toc_control_bit_marks_data_tracks() {
        assert!(TocItem { control: 0x04, ..Default::default() }.is_data());
        assert!(!TocItem { control: 0x00, ..Default::default() }.is_data());
    }

    #[test]
    fn toc_track_lengths_run_to_the_next_entry() {
        let mut toc = DiscToc::default();
        toc.items[1] = TocItem { lba: 0, control: 0x04, exists: true };
        toc.items[2] = TocItem { lba: 1000, control: 0x00, exists: true };
        toc.items[100] = TocItem { lba: 5000, control: 0, exists: true };
        assert_eq!(toc.track_length(1), Some(1000));
        assert_eq!(toc.track_length(2), Some(4000), "runs to the lead-out");
        assert_eq!(toc.track_length(3), None, "does not exist");
        assert_eq!(toc.tracks().count(), 2);
        assert_eq!(toc.leadout().lba, 5000);
    }

    #[test]
    fn toc_index_zero_is_reserved() {
        // The C# sets TOCItems[0] to a non-existent placeholder.
        let toc = DiscToc::default();
        assert!(!toc.items[0].exists);
        assert!(toc.tracks().all(|(n, _)| n >= 1));
    }
}
