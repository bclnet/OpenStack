// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (MdsFormat)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// ==========================================================================
// UNVERIFIED. Written without a toolchain and without an .mds file, at explicit
// request. Binary format, so unlike disc_ccd.rs there is no text half to check
// by inspection — essentially all of the offset arithmetic below is my reading
// of the C#, not something observed working. Marked `VERIFY:` where I am least
// confident.
// ==========================================================================
//
// Alcohol 120% `.mds` descriptor, paired with an `.mdf` data file.
//
// ============ THE C# MDS PARSER IS DESYNCHRONISED BY 80 BYTES ============
//
// This is not a subtle bug, and it means MDS loading cannot have worked:
//
//     var trackHeader = new byte[80];
//     var track = new ATrack();
//     var bytesRead = s.Read(trackHeader, offset: 0, count: trackHeader.Length);
//     Log.Assert(bytesRead == trackHeader.Length, "reached end-of-file ...");
//     track.Mode = r.ReadByte();          // <- reads from position + 80
//     track.SubMode = r.ReadByte();
//     ...
//
// `trackHeader` is allocated, filled with 80 bytes, and **never read again** —
// its only other mention is the `Log.Assert` on the byte count. Then the fields
// are read from the *same stream* through `r`, which the 80-byte read has
// already advanced.
//
// The field reads sum to exactly 80 bytes (12 + 4 + 2 + 18 + 4 + 8 + 4 + 4 + 24),
// so each iteration consumes **160** bytes of an 80-byte block: every track is
// parsed from the *next* block's bytes, and only every other block is visited.
// And `Log.Assert` has an empty body (see the `polyfills` findings), so the
// end-of-file check does nothing either.
//
// The evident intent was to read the block into `trackHeader` and parse from
// that buffer. This port does exactly that, which also makes the length check
// meaningful. **Fix this in the C# tree** — either parse from `trackHeader`, or
// seek back 80 bytes before reading the fields.
//
// Two more:
//
//   * **`AHeader.Parse` reads `BCAOffset`/`StructureOffset`/`SessionOffset`/
//     `DPMOffset` as `Int32` into `long` fields.** A file placing any structure
//     past 2 GiB yields a negative offset and a failed seek. `.mds` descriptors
//     are small, but the offsets point into a `.mdf` that routinely is not.
//   * **`s.Length < 88` is the only size check.** Every subsequent seek is to
//     an offset read from the file with no bounds test, so a corrupt descriptor
//     seeks anywhere.

use std::io::{Read, Seek, SeekFrom};

use openstack_polyio::prelude::BinaryReaderExt;

/// One 80-byte track block, as it appears on disk.
pub const TRACK_BLOCK_LEN: usize = 80;
/// C#'s minimum descriptor length.
pub const MIN_HEADER_LEN: u64 = 88;
/// C# `Signature` — "MEDIA DESCRIPTOR".
pub const SIGNATURE: &[u8; 16] = b"MEDIA DESCRIPTOR";

#[derive(Debug, Clone, PartialEq)]
pub enum MdsError {
    TooShort(u64),
    BadSignature(String),
    /// C#: only 1.x is supported.
    UnsupportedVersion(u8, u8),
    /// C#: "DVD Detected. Not currently supported!"
    DvdNotSupported(i32),
    /// An offset read from the file points outside it.
    BadOffset { what: &'static str, offset: i64 },
    Io(String),
}

impl std::fmt::Display for MdsError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::TooShort(n) => write!(f, "malformed MDS: {n} bytes, need {MIN_HEADER_LEN}"),
            Self::BadSignature(s) => write!(f, "malformed MDS: signature {s:?}"),
            Self::UnsupportedVersion(a, b) => {
                write!(f, "only MDS version 1.x is supported; found {a}.{b}")
            }
            Self::DvdNotSupported(m) => write!(f, "DVD medium {m:#x} is not supported"),
            Self::BadOffset { what, offset } => {
                write!(f, "malformed MDS: {what} offset {offset} is out of range")
            }
            Self::Io(m) => write!(f, "{m}"),
        }
    }
}

impl std::error::Error for MdsError {}

/// C# `AHeader`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct MdsHeader {
    pub signature: String,
    pub version: (u8, u8),
    pub medium: i32,
    pub session_count: i32,
    pub bca_length: i32,
    pub bca_offset: i64,
    pub structure_offset: i64,
    pub session_offset: i64,
    pub dpm_offset: i64,
}

impl MdsHeader {
    /// Whether the medium is a DVD. C# `Medium is 0x10 or 0x12`.
    pub fn is_dvd(&self) -> bool {
        matches!(self.medium, 0x10 | 0x12)
    }

    /// C# `AHeader.Parse(Stream)`.
    ///
    /// Field order and skip widths are the C#'s exactly. Offsets are read as
    /// `i32` as the C# does, but sign-checked rather than silently negative.
    ///
    /// VERIFY: the skip widths (4, 8, 24, 12) are the least confident part —
    /// they are copied verbatim, and a wrong one shifts every later offset.
    pub fn parse<R: Read + Seek>(r: &mut R) -> Result<Self, MdsError> {
        let io = |e: openstack_polyio::system_io::polyfill_binary_reader::ReadError| {
            MdsError::Io(e.to_string())
        };
        let mut sig = [0u8; 16];
        r.read_exact(&mut sig).map_err(|e| MdsError::Io(e.to_string()))?;
        let signature = String::from_utf8_lossy(&sig).to_string();
        if &sig != SIGNATURE {
            return Err(MdsError::BadSignature(signature));
        }
        let version = (r.read_u8().map_err(io)?, r.read_u8().map_err(io)?);
        let medium = r.read_i16().map_err(io)? as i32;
        let session_count = r.read_i16().map_err(io)? as i32;
        r.skip(4).map_err(io)?;
        let bca_length = r.read_i16().map_err(io)? as i32;
        r.skip(8).map_err(io)?;
        let bca_offset = r.read_i32().map_err(io)? as i64;
        r.skip(24).map_err(io)?;
        let structure_offset = r.read_i32().map_err(io)? as i64;
        r.skip(12).map_err(io)?;
        let session_offset = r.read_i32().map_err(io)? as i64;
        let dpm_offset = r.read_i32().map_err(io)? as i64;
        Ok(Self {
            signature,
            version,
            medium,
            session_count,
            bca_length,
            bca_offset,
            structure_offset,
            session_offset,
            dpm_offset,
        })
    }
}

/// C# `ASession`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct MdsSession {
    pub session_start: i32,
    pub session_end: i32,
    pub session_number: i32,
    /// Number of all data blocks (lead-in plus tracks).
    pub all_blocks: u8,
    /// Number of lead-in data blocks.
    pub non_track_blocks: u8,
    pub first_track: i32,
    pub last_track: i32,
    pub track_offset: i64,
}

/// C# `ATrackExtra`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct MdsTrackExtra {
    pub pregap: i64,
    pub sectors: i64,
}

/// C# `ATrack` — one 80-byte block.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct MdsTrack {
    pub mode: u8,
    pub sub_mode: u8,
    pub adr_control: i32,
    pub track_no: i32,
    pub point: i32,
    pub a_min: i32,
    pub a_sec: i32,
    pub a_frame: i32,
    pub zero: i32,
    pub p_min: i32,
    pub p_sec: i32,
    pub p_frame: i32,
    pub extra_offset: i64,
    pub sector_size: i32,
    pub plba: i64,
    pub start_offset: u64,
    pub files: i64,
    pub footer_offset: i64,
    pub extra: MdsTrackExtra,
    pub image_file_names: Vec<String>,
}

impl MdsTrack {
    /// Whether this block describes a real track rather than a lead-in entry.
    ///
    /// VERIFY: the C# distinguishes these by `Point` — points 0xA0..0xA2 are
    /// TOC metadata (first track, last track, lead-out) and 1..99 are tracks.
    /// That is the standard Q-subchannel convention, but the C# never states it.
    pub fn is_track(&self) -> bool {
        (1..=99).contains(&self.point)
    }

    /// Parse one 80-byte block **from a buffer**, which is what the C# meant to
    /// do and did not — see the module header.
    pub fn parse_block(b: &[u8; TRACK_BLOCK_LEN]) -> Self {
        let u8at = |o: usize| b[o];
        let i16at = |o: usize| i16::from_le_bytes([b[o], b[o + 1]]) as i32;
        let i32at = |o: usize| i32::from_le_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);
        let u64at = |o: usize| {
            u64::from_le_bytes([
                b[o], b[o + 1], b[o + 2], b[o + 3], b[o + 4], b[o + 5], b[o + 6], b[o + 7],
            ])
        };
        // Offsets follow the C#'s read order: 12 single bytes, then the rest.
        Self {
            mode: u8at(0),
            sub_mode: u8at(1),
            adr_control: u8at(2) as i32,
            track_no: u8at(3) as i32,
            point: u8at(4) as i32,
            a_min: u8at(5) as i32,
            a_sec: u8at(6) as i32,
            a_frame: u8at(7) as i32,
            zero: u8at(8) as i32,
            p_min: u8at(9) as i32,
            p_sec: u8at(10) as i32,
            p_frame: u8at(11) as i32,
            extra_offset: i32at(12) as i64,
            sector_size: i16at(16),
            // 18 bytes skipped: 18..36
            plba: i32at(36) as i64,
            start_offset: u64at(40),
            files: i32at(48) as i64,
            footer_offset: i32at(52) as i64,
            // 24 bytes skipped: 56..80
            extra: MdsTrackExtra::default(),
            image_file_names: Vec::new(),
        }
    }
}

/// C# `AFile`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct MdsFile {
    pub header: MdsHeader,
    pub sessions: Vec<MdsSession>,
    pub tracks: Vec<MdsTrack>,
}

impl MdsFile {
    /// C# `Parse(Stream, string path)`.
    pub fn parse<R: Read + Seek>(r: &mut R) -> Result<Self, MdsError> {
        let io = |e: openstack_polyio::system_io::polyfill_binary_reader::ReadError| {
            MdsError::Io(e.to_string())
        };
        let len = r.stream_len().map_err(io)?;
        if len < MIN_HEADER_LEN {
            return Err(MdsError::TooShort(len));
        }
        r.seek(SeekFrom::Start(0)).map_err(|e| MdsError::Io(e.to_string()))?;
        let header = MdsHeader::parse(r)?;
        if header.version.0 > 1 {
            return Err(MdsError::UnsupportedVersion(header.version.0, header.version.1));
        }
        if header.is_dvd() {
            return Err(MdsError::DvdNotSupported(header.medium));
        }

        let seek_checked = |r: &mut R, off: i64, what: &'static str| -> Result<(), MdsError> {
            if off < 0 || off as u64 > len {
                return Err(MdsError::BadOffset { what, offset: off });
            }
            r.seek(SeekFrom::Start(off as u64))
                .map(|_| ())
                .map_err(|e| MdsError::Io(e.to_string()))
        };

        let mut me = Self { header, ..Default::default() };
        seek_checked(r, me.header.session_offset, "session")?;
        for _ in 0..me.header.session_count.max(0) {
            let s = MdsSession {
                session_start: r.read_i32().map_err(io)?,
                session_end: r.read_i32().map_err(io)?,
                session_number: r.read_i16().map_err(io)? as i32,
                all_blocks: r.read_u8().map_err(io)?,
                non_track_blocks: r.read_u8().map_err(io)?,
                first_track: r.read_i16().map_err(io)? as i32,
                last_track: r.read_i16().map_err(io)? as i32,
                track_offset: {
                    r.skip(4).map_err(io)?;
                    r.read_i32().map_err(io)? as i64
                },
            };
            me.sessions.push(s);
        }

        // The fix: read each 80-byte block once and parse it from the buffer.
        for si in 0..me.sessions.len() {
            let session = me.sessions[si];
            seek_checked(r, session.track_offset, "track")?;
            for _ in 0..session.all_blocks {
                let mut block = [0u8; TRACK_BLOCK_LEN];
                // Unlike the C#'s empty `Log.Assert`, a short read is an error.
                r.read_exact(&mut block).map_err(|_| MdsError::Io(
                    "reached end-of-file while reading a track header".into(),
                ))?;
                let mut track = MdsTrack::parse_block(&block);

                // C# reads the extra block for real tracks.
                if track.extra_offset > 0 && track.is_track() {
                    let here = r.stream_position().map_err(|e| MdsError::Io(e.to_string()))?;
                    if seek_checked(r, track.extra_offset, "extra").is_ok() {
                        track.extra = MdsTrackExtra {
                            pregap: r.read_i32().map_err(io)? as i64,
                            sectors: r.read_i32().map_err(io)? as i64,
                        };
                    }
                    r.seek(SeekFrom::Start(here)).map_err(|e| MdsError::Io(e.to_string()))?;
                }
                me.tracks.push(track);
            }
        }
        Ok(me)
    }

    /// Real tracks, excluding lead-in/TOC blocks.
    pub fn real_tracks(&self) -> impl Iterator<Item = &MdsTrack> {
        self.tracks.iter().filter(|t| t.is_track())
    }

    /// The `.mdf` path the C# derives with `Path.ChangeExtension`.
    pub fn data_path(mds_path: &str) -> String {
        let stem = mds_path.rsplit_once('.').map(|(s, _)| s).unwrap_or(mds_path);
        format!("{stem}.mdf")
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    /// Builds a synthetic MDS: header, one session, two 80-byte track blocks.
    /// Constructed from my reading of the C#'s field order, so it tests
    /// self-consistency — not agreement with Alcohol 120%.
    fn synth() -> Vec<u8> {
        let mut v = Vec::new();
        v.extend_from_slice(SIGNATURE); // 0..16
        v.extend_from_slice(&[1, 3]); // version 1.3
        v.extend_from_slice(&1i16.to_le_bytes()); // medium (CD)
        v.extend_from_slice(&1i16.to_le_bytes()); // session count
        v.extend_from_slice(&[0; 4]); // skip
        v.extend_from_slice(&0i16.to_le_bytes()); // bca length
        v.extend_from_slice(&[0; 8]); // skip
        v.extend_from_slice(&0i32.to_le_bytes()); // bca offset
        v.extend_from_slice(&[0; 24]); // skip
        v.extend_from_slice(&0i32.to_le_bytes()); // structure offset
        v.extend_from_slice(&[0; 12]); // skip
        let session_offset_pos = v.len();
        v.extend_from_slice(&0i32.to_le_bytes()); // session offset (patched)
        v.extend_from_slice(&0i32.to_le_bytes()); // dpm offset

        let session_offset = v.len() as i32;
        let track_offset_pos;
        {
            v.extend_from_slice(&0i32.to_le_bytes()); // session start
            v.extend_from_slice(&1000i32.to_le_bytes()); // session end
            v.extend_from_slice(&1i16.to_le_bytes()); // session number
            v.push(2); // all blocks
            v.push(0); // non-track blocks
            v.extend_from_slice(&1i16.to_le_bytes()); // first track
            v.extend_from_slice(&1i16.to_le_bytes()); // last track
            v.extend_from_slice(&[0; 4]); // skip
            track_offset_pos = v.len();
            v.extend_from_slice(&0i32.to_le_bytes()); // track offset (patched)
        }

        let track_offset = v.len() as i32;
        for (point, plba, sector_size) in [(0xA0, 0i32, 0i16), (1, 0i32, 2352i16)] {
            let mut b = [0u8; TRACK_BLOCK_LEN];
            b[0] = 0x0A; // mode
            b[1] = 0x00; // submode
            b[2] = 0x04; // adr/control
            b[3] = 0; // track no
            b[4] = point as u8;
            b[5..12].copy_from_slice(&[0, 2, 0, 0, 0, 0, 0]); // AMin..PFrame
            b[12..16].copy_from_slice(&0i32.to_le_bytes()); // extra offset
            b[16..18].copy_from_slice(&sector_size.to_le_bytes());
            b[36..40].copy_from_slice(&plba.to_le_bytes());
            b[40..48].copy_from_slice(&0u64.to_le_bytes()); // start offset
            b[48..52].copy_from_slice(&1i32.to_le_bytes()); // files
            b[52..56].copy_from_slice(&0i32.to_le_bytes()); // footer offset
            v.extend_from_slice(&b);
        }

        v[session_offset_pos..session_offset_pos + 4]
            .copy_from_slice(&session_offset.to_le_bytes());
        v[track_offset_pos..track_offset_pos + 4].copy_from_slice(&track_offset.to_le_bytes());
        v
    }

    #[test]
    fn parses_the_synthetic_descriptor() {
        let mut c = Cursor::new(synth());
        let f = MdsFile::parse(&mut c).expect("should parse");
        assert_eq!(f.header.signature, "MEDIA DESCRIPTOR");
        assert_eq!(f.header.version, (1, 3));
        assert_eq!(f.header.session_count, 1);
        assert_eq!(f.sessions.len(), 1);
    }

    #[test]
    fn both_track_blocks_are_visited() {
        // This is the fix: the C# consumes 160 bytes per 80-byte block, so it
        // would read only one block here and parse it from the wrong bytes.
        let mut c = Cursor::new(synth());
        let f = MdsFile::parse(&mut c).unwrap();
        assert_eq!(f.tracks.len(), 2, "all_blocks = 2");
    }

    #[test]
    fn track_fields_come_from_their_own_block() {
        let mut c = Cursor::new(synth());
        let f = MdsFile::parse(&mut c).unwrap();
        assert_eq!(f.tracks[0].point, 0xA0, "first block is a TOC entry");
        assert_eq!(f.tracks[1].point, 1, "second block is track 1");
        assert_eq!(f.tracks[1].sector_size, 2352);
        assert_eq!(f.tracks[0].sector_size, 0);
    }

    #[test]
    fn lead_in_blocks_are_distinguished_from_tracks() {
        let mut c = Cursor::new(synth());
        let f = MdsFile::parse(&mut c).unwrap();
        assert_eq!(f.real_tracks().count(), 1);
        assert!(!f.tracks[0].is_track(), "point 0xA0 is TOC metadata");
        assert!(f.tracks[1].is_track());
    }

    #[test]
    fn block_parsing_is_a_pure_function_of_its_80_bytes() {
        let mut b = [0u8; TRACK_BLOCK_LEN];
        b[4] = 42; // point
        b[16..18].copy_from_slice(&2448i16.to_le_bytes());
        b[36..40].copy_from_slice(&12345i32.to_le_bytes());
        let t = MdsTrack::parse_block(&b);
        assert_eq!(t.point, 42);
        assert_eq!(t.sector_size, 2448);
        assert_eq!(t.plba, 12345);
    }

    #[test]
    fn field_reads_sum_to_the_block_length() {
        // The arithmetic that proves the C# desynchronises: the fields occupy
        // exactly one block, so reading a block *and* the fields is double.
        let widths = [
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // mode..pframe
            4,  // extra offset
            2,  // sector size
            18, // skip
            4,  // plba
            8,  // start offset
            4,  // files
            4,  // footer offset
            24, // skip
        ];
        assert_eq!(widths.iter().sum::<usize>(), TRACK_BLOCK_LEN);
    }

    #[test]
    fn short_files_are_rejected() {
        let mut c = Cursor::new(vec![0u8; 40]);
        assert_eq!(MdsFile::parse(&mut c), Err(MdsError::TooShort(40)));
    }

    #[test]
    fn a_bad_signature_is_rejected() {
        let mut v = synth();
        v[0] = b'X';
        let mut c = Cursor::new(v);
        assert!(matches!(MdsFile::parse(&mut c), Err(MdsError::BadSignature(_))));
    }

    #[test]
    fn version_two_and_above_is_refused_as_in_the_c_sharp() {
        let mut v = synth();
        v[16] = 2;
        let mut c = Cursor::new(v);
        assert_eq!(
            MdsFile::parse(&mut c),
            Err(MdsError::UnsupportedVersion(2, 3))
        );
    }

    #[test]
    fn dvd_media_are_refused_as_in_the_c_sharp() {
        for medium in [0x10i16, 0x12] {
            let mut v = synth();
            v[18..20].copy_from_slice(&medium.to_le_bytes());
            let mut c = Cursor::new(v);
            assert_eq!(
                MdsFile::parse(&mut c),
                Err(MdsError::DvdNotSupported(medium as i32))
            );
        }
    }

    #[test]
    fn out_of_range_offsets_are_rejected_not_seeked_to() {
        // The C# seeks to whatever the file says with no bounds test.
        let mut v = synth();
        // Corrupt the session offset to something past the end.
        let pos = 16 + 2 + 2 + 2 + 4 + 2 + 8 + 4 + 24 + 4 + 12;
        v[pos..pos + 4].copy_from_slice(&0x7FFF_FFFFi32.to_le_bytes());
        let mut c = Cursor::new(v);
        assert!(matches!(
            MdsFile::parse(&mut c),
            Err(MdsError::BadOffset { .. })
        ));
    }

    #[test]
    fn a_truncated_track_block_is_an_error() {
        // The C# guards this with Log.Assert, whose body is empty.
        let mut v = synth();
        v.truncate(v.len() - 40); // cut the last block in half
        let mut c = Cursor::new(v);
        assert!(MdsFile::parse(&mut c).is_err());
    }

    #[test]
    fn data_path_swaps_the_extension() {
        assert_eq!(MdsFile::data_path("game.mds"), "game.mdf");
        assert_eq!(MdsFile::data_path("/a/b/My Game.mds"), "/a/b/My Game.mdf");
    }
}
