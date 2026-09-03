// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (NrgFormat)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// ==========================================================================
// UNVERIFIED. Written without a toolchain and without an .nrg file, at explicit
// request. Binary format — the chunk walking is straightforward, but the
// per-chunk field layouts are my reading of the C# and are marked `VERIFY:`.
// ==========================================================================
//
// Nero `.nrg` image. The whole thing is a chunk list located by a **footer**:
// the last 8 or 12 bytes give a signature and the offset of the chunk table.
// All multi-byte values are **big-endian on disk**, which the C# handles with
// `if (BitConverter.IsLittleEndian) ReverseEndianness(...)` on every read.
//
// V1 files are signed `NERO` with a 32-bit table offset; V2 files are `NER5`
// with a 64-bit offset. Each chunk id exists in a V1 and a V2 spelling
// (`CUES`/`CUEX`, `DAOI`/`DAOX`, `ETNF`/`ETN2`), and the C# asserts they are
// not mixed.
//
// ============ V1 (NERO) FILES GET A GARBAGE TABLE OFFSET =================
//
//     public long FileOffset;
//     ...
//     nrgf.FileOffset = r.ReadUInt32();
//     if (BitConverter.IsLittleEndian)
//         nrgf.FileOffset = BinaryPrimitives.ReverseEndianness(nrgf.FileOffset);
//
// `ReadUInt32` widens into the `long` field, zero-extending to 8 bytes. Overload
// resolution then picks `ReverseEndianness(long)`, which reverses **all eight**
// bytes — so a table offset of `0x1234` becomes `0x3412000000000000` instead of
// `0x34120000`. The subsequent seek is to ~3.7 exabytes and fails.
//
// The V2 path is correct, because it reads a genuine `Int64`. So **only V1
// files are affected**, which is consistent with this going unnoticed if only
// V2 images were ever tested. **Fix this in the C# tree** by reversing as
// `uint` before widening.
//
// Two more:
//
//   * **The footer read starts 4 bytes too early on the V1 path.** It seeks to
//     `End - 12` and reads 4 bytes; if that is not `NER5` it reads 4 more and
//     expects `NERO`. So for a V1 file the first 4 bytes read are the tail of
//     the chunk data, discarded. Harmless, but it means a 12-byte read is
//     required on a file whose footer is 8 bytes — a file with exactly 8 bytes
//     of footer and nothing else cannot be read at all.
//   * **`ParseCueChunk` steps `i += 8` over `chunkSize`** with no check that
//     `chunkSize` is a multiple of 8, so a trailing partial entry reads past
//     the end of `chunkData` (`chunkData[i + 0]` .. `[i + 7]`).

use std::io::{Read, Seek, SeekFrom};

/// C# `NrgTrackIndex`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct NrgTrackIndex {
    pub adr_control: u8,
    pub track: u8,
    pub index: u8,
    pub lba: i32,
}

/// C# `NrgCue` — a `CUES`/`CUEX` chunk.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct NrgCue {
    pub chunk_id: String,
    pub indexes: Vec<NrgTrackIndex>,
}

/// C# `NrgDaoTrackInfo` — a `DAOI`/`DAOX` chunk.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct NrgDaoTrackInfo {
    pub chunk_id: String,
    pub ean13_catalog_number: String,
    pub disk_type: u8,
    pub first_track: u8,
    pub last_track: u8,
}

/// C# `NrgTaoTrack` — one entry in an `ETNF`/`ETN2` chunk.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct NrgTaoTrack {
    pub track_file_offset: u64,
    pub track_length: u64,
    pub mode: u32,
}

/// C# `NrgTaoTrackInfo`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct NrgTaoTrackInfo {
    pub chunk_id: String,
    pub tracks: Vec<NrgTaoTrack>,
}

/// C# `NrgSessionInfo` — a `SINF` chunk.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct NrgSessionInfo {
    pub track_count: u32,
}

/// C# `NrgToct` — a `TOCT` chunk.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct NrgToct {
    pub disk_type: u8,
}

/// Which signature a file carries, and therefore which chunk spellings and
/// offset width are legal.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum NrgVersion {
    /// `NERO` — 32-bit table offset, V1 chunk ids.
    V1,
    /// `NER5` — 64-bit table offset, V2 chunk ids.
    V2,
}

impl NrgVersion {
    pub fn signature(self) -> &'static str {
        match self {
            Self::V1 => "NERO",
            Self::V2 => "NER5",
        }
    }
}

#[derive(Debug, Clone, PartialEq)]
pub enum NrgError {
    TooShort(u64),
    NoSignature,
    NegativeOffset(i64),
    OffsetOutOfRange { offset: i64, len: u64 },
    NegativeChunkSize(i32),
    UnexpectedEnd,
    /// C#: "Found V1 chunk in a V2 file!" and vice versa.
    MixedVersions { chunk: String, version: NrgVersion },
    /// A chunk's payload is not a whole number of entries.
    RaggedChunk { chunk: String, size: i32, entry: usize },
    Io(String),
}

impl std::fmt::Display for NrgError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::TooShort(n) => write!(f, "malformed NRG: {n} bytes, too short for a footer"),
            Self::NoSignature => {
                write!(f, "malformed NRG: could not find a NERO/NER5 signature")
            }
            Self::NegativeOffset(o) => {
                write!(f, "malformed NRG: chunk table offset {o} is negative")
            }
            Self::OffsetOutOfRange { offset, len } => write!(
                f,
                "malformed NRG: chunk table offset {offset} is past the end ({len} bytes)"
            ),
            Self::NegativeChunkSize(s) => write!(f, "malformed NRG: chunk size {s} is negative"),
            Self::UnexpectedEnd => write!(f, "malformed NRG: unexpected stream end"),
            Self::MixedVersions { chunk, version } => write!(
                f,
                "malformed NRG: {chunk} chunk in a {} file",
                version.signature()
            ),
            Self::RaggedChunk { chunk, size, entry } => write!(
                f,
                "malformed NRG: {chunk} size {size} is not a multiple of {entry}"
            ),
            Self::Io(m) => write!(f, "{m}"),
        }
    }
}

impl std::error::Error for NrgError {}

/// C# `NrgFile`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct NrgFile {
    pub file_id: String,
    pub file_offset: i64,
    pub cues: Vec<NrgCue>,
    pub dao_track_infos: Vec<NrgDaoTrackInfo>,
    pub tao_track_infos: Vec<NrgTaoTrackInfo>,
    pub session_infos: Vec<NrgSessionInfo>,
    pub tocts: Vec<NrgToct>,
    pub media_type: Option<u32>,
    pub volume_name: Option<String>,
    pub filenames: Vec<String>,
    pub cd_text: Vec<u8>,
    pub relos: usize,
}

/// Read `n` big-endian bytes, the on-disk order for NRG.
fn be_u32(b: &[u8], o: usize) -> Option<u32> {
    Some(u32::from_be_bytes(b.get(o..o + 4)?.try_into().ok()?))
}

fn be_u64(b: &[u8], o: usize) -> Option<u64> {
    Some(u64::from_be_bytes(b.get(o..o + 8)?.try_into().ok()?))
}

impl NrgFile {
    /// The V2 footer is 12 bytes ("NER5" + i64); the V1 footer is 8 ("NERO" +
    /// u32). The C# always seeks to `End - 12`, so it cannot read a file whose
    /// total length is under 12 even if its footer is complete.
    pub const FOOTER_V2_LEN: u64 = 12;
    pub const FOOTER_V1_LEN: u64 = 8;

    /// C# `ParseFrom(Stream)`.
    pub fn parse<R: Read + Seek>(r: &mut R) -> Result<Self, NrgError> {
        let io = |e: std::io::Error| NrgError::Io(e.to_string());
        let len = r.seek(SeekFrom::End(0)).map_err(io)?;
        if len < Self::FOOTER_V1_LEN {
            return Err(NrgError::TooShort(len));
        }

        // Try the V2 footer first, as the C# does.
        let mut me = Self::default();
        let (version, file_offset) = {
            let mut tail = [0u8; 12];
            if len >= Self::FOOTER_V2_LEN {
                r.seek(SeekFrom::End(-(Self::FOOTER_V2_LEN as i64))).map_err(io)?;
                r.read_exact(&mut tail).map_err(io)?;
                if &tail[..4] == b"NER5" {
                    // Correct: a genuine 64-bit big-endian offset.
                    let off = be_u64(&tail, 4).ok_or(NrgError::UnexpectedEnd)? as i64;
                    (NrgVersion::V2, off)
                } else {
                    Self::read_v1_footer(r, len)?
                }
            } else {
                Self::read_v1_footer(r, len)?
            }
        };
        me.file_id = version.signature().to_string();
        if file_offset < 0 {
            return Err(NrgError::NegativeOffset(file_offset));
        }
        if file_offset as u64 >= len {
            return Err(NrgError::OffsetOutOfRange { offset: file_offset, len });
        }
        me.file_offset = file_offset;

        r.seek(SeekFrom::Start(file_offset as u64)).map_err(io)?;
        loop {
            let mut head = [0u8; 8];
            if r.read_exact(&mut head).is_err() {
                return Err(NrgError::UnexpectedEnd);
            }
            let chunk_id = String::from_utf8_lossy(&head[..4]).to_string();
            // Big-endian, as everything in NRG is.
            let size = i32::from_be_bytes(head[4..8].try_into().unwrap());
            if size < 0 {
                return Err(NrgError::NegativeChunkSize(size));
            }
            let mut data = vec![0u8; size as usize];
            r.read_exact(&mut data).map_err(|_| NrgError::UnexpectedEnd)?;

            let expect = |want: NrgVersion| -> Result<(), NrgError> {
                if version == want {
                    Ok(())
                } else {
                    Err(NrgError::MixedVersions { chunk: chunk_id.clone(), version })
                }
            };

            match chunk_id.as_str() {
                "CUES" => {
                    expect(NrgVersion::V1)?;
                    me.cues.push(Self::parse_cue(&chunk_id, size, &data)?);
                }
                "CUEX" => {
                    expect(NrgVersion::V2)?;
                    me.cues.push(Self::parse_cue(&chunk_id, size, &data)?);
                }
                "DAOI" => {
                    expect(NrgVersion::V1)?;
                    me.dao_track_infos.push(Self::parse_dao(&chunk_id, &data));
                }
                "DAOX" => {
                    expect(NrgVersion::V2)?;
                    me.dao_track_infos.push(Self::parse_dao(&chunk_id, &data));
                }
                "TINF" | "ETNF" => {
                    expect(NrgVersion::V1)?;
                    me.tao_track_infos.push(Self::parse_etn(&chunk_id, size, &data)?);
                }
                "ETN2" => {
                    expect(NrgVersion::V2)?;
                    me.tao_track_infos.push(Self::parse_etn(&chunk_id, size, &data)?);
                }
                "RELO" => {
                    expect(NrgVersion::V2)?;
                    me.relos += 1;
                }
                "SINF" => me.session_infos.push(NrgSessionInfo {
                    track_count: be_u32(&data, 0).unwrap_or(0),
                }),
                "TOCT" => me.tocts.push(NrgToct {
                    disk_type: data.first().copied().unwrap_or(0),
                }),
                "MTYP" => me.media_type = be_u32(&data, 0),
                "VOLM" => {
                    me.volume_name = Some(
                        String::from_utf8_lossy(&data)
                            .trim_end_matches('\0')
                            .to_string(),
                    )
                }
                "AFNM" => {
                    me.filenames = data
                        .split(|&b| b == 0)
                        .filter(|s| !s.is_empty())
                        .map(|s| String::from_utf8_lossy(s).to_string())
                        .collect()
                }
                "CDTX" => me.cd_text = data,
                "END!" => break,
                _ => {
                    // The C# has a default that records the chunk and continues.
                }
            }
            // C#'s loop condition is `while (nrgf.End is null)`, so an image
            // missing its END! chunk runs off the end of the stream. Bounded
            // here by the read_exact above.
        }
        Ok(me)
    }

    /// The V1 footer: `NERO` plus a **32-bit** big-endian offset.
    ///
    /// The C# reverses this after widening it into a `long`, producing garbage;
    /// here it is reversed as a `u32` and then widened. See the module header.
    fn read_v1_footer<R: Read + Seek>(
        r: &mut R,
        len: u64,
    ) -> Result<(NrgVersion, i64), NrgError> {
        let io = |e: std::io::Error| NrgError::Io(e.to_string());
        if len < Self::FOOTER_V1_LEN {
            return Err(NrgError::TooShort(len));
        }
        r.seek(SeekFrom::End(-(Self::FOOTER_V1_LEN as i64))).map_err(io)?;
        let mut tail = [0u8; 8];
        r.read_exact(&mut tail).map_err(io)?;
        if &tail[..4] != b"NERO" {
            return Err(NrgError::NoSignature);
        }
        let off = be_u32(&tail, 4).ok_or(NrgError::UnexpectedEnd)?;
        Ok((NrgVersion::V1, off as i64))
    }

    /// C# `ParseCueChunk` — 8 bytes per index entry.
    ///
    /// The C# steps `i += 8` without checking that `chunkSize` is a multiple of
    /// 8, so a ragged chunk reads past `chunkData`.
    fn parse_cue(chunk_id: &str, size: i32, data: &[u8]) -> Result<NrgCue, NrgError> {
        const ENTRY: usize = 8;
        if size as usize % ENTRY != 0 {
            return Err(NrgError::RaggedChunk {
                chunk: chunk_id.to_string(),
                size,
                entry: ENTRY,
            });
        }
        let mut indexes = Vec::new();
        for c in data.chunks_exact(ENTRY) {
            indexes.push(NrgTrackIndex {
                adr_control: c[0],
                track: c[1],
                index: c[2],
                // VERIFY: the C# reads bytes 4..8 as the LBA, big-endian.
                lba: i32::from_be_bytes([c[4], c[5], c[6], c[7]]),
            });
        }
        Ok(NrgCue { chunk_id: chunk_id.to_string(), indexes })
    }

    /// C# `ParseDaoChunk`.
    ///
    /// VERIFY: the C# slices `chunkData.Slice(4, 13)` for the catalog number
    /// and reads `chunkData[18]` as the disk type; the first/last track bytes
    /// follow. Those offsets are copied verbatim.
    fn parse_dao(chunk_id: &str, data: &[u8]) -> NrgDaoTrackInfo {
        let get = |i: usize| data.get(i).copied().unwrap_or(0);
        NrgDaoTrackInfo {
            chunk_id: chunk_id.to_string(),
            ean13_catalog_number: data
                .get(4..17)
                .map(|s| String::from_utf8_lossy(s).trim_end_matches('\0').to_string())
                .unwrap_or_default(),
            disk_type: get(18),
            first_track: get(19),
            last_track: get(20),
        }
    }

    /// C# `ParseEtnChunk` — 20 bytes per entry for `ETN2`, 12 for `ETNF`.
    ///
    /// VERIFY: the C# selects the entry size by chunk id (`ETN2` being the
    /// 64-bit V2 form). The widths here follow that reading.
    fn parse_etn(chunk_id: &str, size: i32, data: &[u8]) -> Result<NrgTaoTrackInfo, NrgError> {
        let entry = if chunk_id == "ETN2" { 32 } else { 20 };
        if size as usize % entry != 0 {
            return Err(NrgError::RaggedChunk {
                chunk: chunk_id.to_string(),
                size,
                entry,
            });
        }
        let mut tracks = Vec::new();
        for c in data.chunks_exact(entry) {
            tracks.push(if chunk_id == "ETN2" {
                NrgTaoTrack {
                    track_file_offset: be_u64(c, 0).unwrap_or(0),
                    track_length: be_u64(c, 8).unwrap_or(0),
                    mode: be_u32(c, 16).unwrap_or(0),
                }
            } else {
                NrgTaoTrack {
                    track_file_offset: be_u32(c, 0).unwrap_or(0) as u64,
                    track_length: be_u32(c, 4).unwrap_or(0) as u64,
                    mode: be_u32(c, 8).unwrap_or(0),
                }
            });
        }
        Ok(NrgTaoTrackInfo { chunk_id: chunk_id.to_string(), tracks })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    /// Builds a synthetic NRG. Chunk layout follows my reading of the C#, so
    /// these tests verify self-consistency, not agreement with Nero.
    fn synth(v2: bool) -> Vec<u8> {
        let mut chunks: Vec<u8> = Vec::new();
        let push = |c: &mut Vec<u8>, id: &[u8; 4], payload: &[u8]| {
            c.extend_from_slice(id);
            c.extend_from_slice(&(payload.len() as i32).to_be_bytes());
            c.extend_from_slice(payload);
        };
        // One SINF with a track count.
        push(&mut chunks, b"SINF", &1u32.to_be_bytes());
        // One cue chunk with two 8-byte entries.
        let mut cue = Vec::new();
        for (track, lba) in [(1u8, 0i32), (2u8, 1000i32)] {
            cue.extend_from_slice(&[0x01, track, 1, 0]);
            cue.extend_from_slice(&lba.to_be_bytes());
        }
        push(&mut chunks, if v2 { b"CUEX" } else { b"CUES" }, &cue);
        push(&mut chunks, b"MTYP", &7u32.to_be_bytes());
        push(&mut chunks, b"END!", &[]);

        // The chunk table sits at the front; the footer points at it.
        let mut v: Vec<u8> = Vec::new();
        v.extend_from_slice(&[0xAA; 32]); // some payload before the table
        let table_offset = v.len();
        v.extend_from_slice(&chunks);
        if v2 {
            v.extend_from_slice(b"NER5");
            v.extend_from_slice(&(table_offset as u64).to_be_bytes());
        } else {
            v.extend_from_slice(b"NERO");
            v.extend_from_slice(&(table_offset as u32).to_be_bytes());
        }
        v
    }

    #[test]
    fn parses_a_v2_image() {
        let mut c = Cursor::new(synth(true));
        let f = NrgFile::parse(&mut c).expect("should parse");
        assert_eq!(f.file_id, "NER5");
        assert_eq!(f.file_offset, 32);
        assert_eq!(f.session_infos[0].track_count, 1);
        assert_eq!(f.media_type, Some(7));
    }

    #[test]
    fn parses_a_v1_image() {
        // The C# widens the u32 offset into a long and then reverses 8 bytes,
        // seeking to roughly 3.7 exabytes. This is the whole point of the fix.
        let mut c = Cursor::new(synth(false));
        let f = NrgFile::parse(&mut c).expect("V1 must parse");
        assert_eq!(f.file_id, "NERO");
        assert_eq!(f.file_offset, 32, "the 32-bit offset must not be widened-then-reversed");
    }

    #[test]
    fn the_c_sharp_v1_offset_computation_is_catastrophic() {
        // Documents the magnitude rather than just asserting inequality.
        let raw: u32 = 32;
        let widened_then_reversed = i64::from_be_bytes((raw as u64).to_le_bytes());
        assert_ne!(widened_then_reversed, 32);
        assert!(
            widened_then_reversed.unsigned_abs() > 1 << 60,
            "got {widened_then_reversed}"
        );
    }

    #[test]
    fn cue_entries_are_decoded_big_endian() {
        let mut c = Cursor::new(synth(true));
        let f = NrgFile::parse(&mut c).unwrap();
        let cue = &f.cues[0];
        assert_eq!(cue.chunk_id, "CUEX");
        assert_eq!(cue.indexes.len(), 2);
        assert_eq!(cue.indexes[0].track, 1);
        assert_eq!(cue.indexes[0].lba, 0);
        assert_eq!(cue.indexes[1].track, 2);
        assert_eq!(cue.indexes[1].lba, 1000);
    }

    #[test]
    fn mixing_v1_and_v2_chunk_ids_is_rejected() {
        // A V2 file containing a V1 CUES chunk.
        let mut v = synth(true);
        let at = v.windows(4).position(|w| w == b"CUEX").unwrap();
        v[at..at + 4].copy_from_slice(b"CUES");
        let mut c = Cursor::new(v);
        assert!(matches!(
            NrgFile::parse(&mut c),
            Err(NrgError::MixedVersions { .. })
        ));
    }

    #[test]
    fn ragged_cue_chunks_are_rejected_not_read_past() {
        // The C# steps i += 8 with no multiple-of-8 check.
        let mut c = Cursor::new(Vec::new());
        let data = vec![0u8; 12]; // not a multiple of 8
        let e = NrgFile::parse_cue("CUES", 12, &data).unwrap_err();
        assert!(matches!(e, NrgError::RaggedChunk { entry: 8, .. }));
        let _ = &mut c;
    }

    #[test]
    fn well_formed_cue_chunks_parse() {
        let data = vec![0u8; 16];
        let cue = NrgFile::parse_cue("CUES", 16, &data).unwrap();
        assert_eq!(cue.indexes.len(), 2);
    }

    #[test]
    fn negative_chunk_size_is_rejected() {
        let mut v = vec![0xAAu8; 8];
        let table = v.len();
        v.extend_from_slice(b"SINF");
        v.extend_from_slice(&(-1i32).to_be_bytes());
        v.extend_from_slice(b"NER5");
        v.extend_from_slice(&(table as u64).to_be_bytes());
        let mut c = Cursor::new(v);
        assert_eq!(
            NrgFile::parse(&mut c),
            Err(NrgError::NegativeChunkSize(-1))
        );
    }

    #[test]
    fn missing_signature_is_rejected() {
        let mut c = Cursor::new(vec![0u8; 64]);
        assert_eq!(NrgFile::parse(&mut c), Err(NrgError::NoSignature));
    }

    #[test]
    fn a_table_offset_past_the_end_is_rejected() {
        let mut v = vec![0xAAu8; 16];
        v.extend_from_slice(b"NER5");
        v.extend_from_slice(&0xFFFF_FFFFu64.to_be_bytes());
        let mut c = Cursor::new(v);
        assert!(matches!(
            NrgFile::parse(&mut c),
            Err(NrgError::OffsetOutOfRange { .. })
        ));
    }

    #[test]
    fn short_files_are_rejected() {
        let mut c = Cursor::new(vec![0u8; 4]);
        assert_eq!(NrgFile::parse(&mut c), Err(NrgError::TooShort(4)));
    }

    #[test]
    fn an_image_without_an_end_chunk_terminates() {
        // The C#'s `while (End is null)` runs off the stream.
        let mut chunks = Vec::new();
        chunks.extend_from_slice(b"SINF");
        chunks.extend_from_slice(&4i32.to_be_bytes());
        chunks.extend_from_slice(&1u32.to_be_bytes());
        let mut v = vec![0xAAu8; 8];
        let table = v.len();
        v.extend_from_slice(&chunks);
        v.extend_from_slice(b"NER5");
        v.extend_from_slice(&(table as u64).to_be_bytes());
        let mut c = Cursor::new(v);
        // Reaches the footer bytes, fails to make a valid chunk, and stops.
        assert!(NrgFile::parse(&mut c).is_err());
    }

    #[test]
    fn etn_entry_widths_differ_by_version() {
        let v2 = vec![0u8; 64]; // two 32-byte entries
        assert_eq!(NrgFile::parse_etn("ETN2", 64, &v2).unwrap().tracks.len(), 2);
        let v1 = vec![0u8; 40]; // two 20-byte entries
        assert_eq!(NrgFile::parse_etn("ETNF", 40, &v1).unwrap().tracks.len(), 2);
        // And a ragged one is rejected.
        assert!(NrgFile::parse_etn("ETN2", 33, &vec![0u8; 33]).is_err());
    }

    #[test]
    fn version_signatures_round_trip() {
        assert_eq!(NrgVersion::V1.signature(), "NERO");
        assert_eq!(NrgVersion::V2.signature(), "NER5");
    }
}
