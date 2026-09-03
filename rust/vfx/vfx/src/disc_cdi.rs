// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (CdiFormat)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// ==========================================================================
// UNVERIFIED. Written without a toolchain and without a .cdi file, at explicit
// request. This is the least verifiable of the four image formats: the layout
// is a chain of "unknown bytes" skips (15, 29, 13, 2, 7 ...) with no
// self-describing lengths, so a single wrong skip width silently shifts
// everything after it and still parses. Every skip is marked `VERIFY:`.
// ==========================================================================
//
// DiscJuggler `.cdi`. The descriptor lives at the **end** of the file: the last
// 4 bytes are an "entrypoint" giving the descriptor's length, so the descriptor
// starts at `EOF - entrypoint`.
//
// ===================== FOUR C#-SIDE ISSUES ==============================
//
//   1. **The session loop runs `i <= NumSessions`**, one more than the declared
//      count, and adds every iteration to `Sessions`. So `Sessions.Count` is
//      always `NumSessions + 1`. The extra pass appears to be deliberate — the
//      guard `if (NumTracks == 0 && i != NumSessions)` exempts exactly the last
//      one — so the final entry is an end-marker being stored as data. Any
//      caller iterating `Sessions` gets a phantom empty session. The port keeps
//      the parse but exposes `sessions()` (real) separately from
//      `sessions_including_terminator()`.
//
//   2. **`s.Seek(-ret.Entrypoint, SeekOrigin.End)` is unchecked.** `Entrypoint`
//      is a `uint` read straight from the file; an entrypoint larger than the
//      file seeks before the start and throws from inside the parse.
//
//   3. **Track and session numbers are validated against loop indices.**
//      `SessionNumber != i` and `TrackNumber != j` throw, with `i` a 0-based
//      session index and `j` a **per-session** track index. So CDI track
//      numbers are 0-based and restart each session — unlike CD track numbers,
//      which are 1-based and disc-global. Preserved, but it is a trap for
//      anyone comparing these to a TOC.
//
//   4. **The CD-Text loop always reads 18 length-prefixed strings per block**,
//      discarding any with zero length. There is no count; 18 is hard-coded. A
//      block with fewer fields desynchronises the rest of the parse.

use std::io::{Read, Seek, SeekFrom};

/// C# `CdiTrackHeader.MediumType` values.
pub const MEDIUM_CD: u16 = 0x0098;
pub const MEDIUM_DVD: u16 = 0x0038;

/// Maximum tracks on a CD, which the C# enforces.
pub const MAX_TRACKS: usize = 99;

/// C# `CdiCDText`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct CdiCdText {
    pub texts: Vec<String>,
}

/// C# `CdiTrackHeader`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct CdiTrackHeader {
    pub num_tracks: u8,
    pub path: String,
    pub medium_type: u16,
}

/// C# `CdiTrack : CdiTrackHeader`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct CdiTrack {
    pub header: CdiTrackHeader,
    pub index_sector_counts: Vec<u32>,
    pub cd_text_blocks: Vec<CdiCdText>,
    pub track_mode: u8,
    pub session_number: u32,
    pub track_number: u32,
    pub track_start_address: u32,
    pub track_length: u32,
}

/// C# `CdiSession`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct CdiSession {
    pub num_tracks: u8,
}

#[derive(Debug, Clone, PartialEq)]
pub enum CdiError {
    TooShort(u64),
    /// The trailing entrypoint points before the start of the file.
    BadEntrypoint { entrypoint: u32, len: u64 },
    NoSessions,
    /// C#: "No tracks in session!"
    EmptySession(usize),
    TooManyTracks(usize),
    DvdNotSupported,
    InvalidMediumType(u16),
    /// C#: "Less than 2 indexes in track!"
    TooFewIndexes(u16),
    InvalidTrackMode(u8),
    SessionNumberMismatch { expected: usize, found: u32 },
    TrackNumberMismatch { expected: usize, found: u32 },
    Io(String),
}

impl std::fmt::Display for CdiError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::TooShort(n) => write!(f, "malformed CDI: {n} bytes"),
            Self::BadEntrypoint { entrypoint, len } => write!(
                f,
                "malformed CDI: entrypoint {entrypoint} exceeds the file length {len}"
            ),
            Self::NoSessions => write!(f, "malformed CDI: 0 sessions"),
            Self::EmptySession(i) => write!(f, "malformed CDI: no tracks in session {i}"),
            Self::TooManyTracks(n) => {
                write!(f, "malformed CDI: {n} tracks, more than {MAX_TRACKS}")
            }
            Self::DvdNotSupported => write!(f, "malformed CDI: DVD is not supported"),
            Self::InvalidMediumType(t) => write!(f, "malformed CDI: medium type {t:#06x}"),
            Self::TooFewIndexes(n) => {
                write!(f, "malformed CDI: {n} indexes in a track, need at least 2")
            }
            Self::InvalidTrackMode(m) => write!(f, "malformed CDI: track mode {m}"),
            Self::SessionNumberMismatch { expected, found } => {
                write!(f, "malformed CDI: session number {found}, expected {expected}")
            }
            Self::TrackNumberMismatch { expected, found } => {
                write!(f, "malformed CDI: track number {found}, expected {expected}")
            }
            Self::Io(m) => write!(f, "{m}"),
        }
    }
}

impl std::error::Error for CdiError {}

/// C# `CdiFile`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct CdiFile {
    pub num_sessions: u8,
    /// Includes the trailing end-marker entry the C# also stores — see issue 1.
    sessions_raw: Vec<CdiSession>,
    pub tracks: Vec<CdiTrack>,
    pub entrypoint: u32,
}

impl CdiFile {
    /// The real sessions, excluding the end-marker the C# leaves in the list.
    pub fn sessions(&self) -> &[CdiSession] {
        let n = (self.num_sessions as usize).min(self.sessions_raw.len());
        &self.sessions_raw[..n]
    }

    /// Every entry the C# would have put in `Sessions`, terminator included.
    pub fn sessions_including_terminator(&self) -> &[CdiSession] {
        &self.sessions_raw
    }

    /// C# `ParseFrom(Stream)`.
    pub fn parse<R: Read + Seek>(r: &mut R) -> Result<Self, CdiError> {
        let io = |e: std::io::Error| CdiError::Io(e.to_string());
        let len = r.seek(SeekFrom::End(0)).map_err(io)?;
        if len < 4 {
            return Err(CdiError::TooShort(len));
        }

        r.seek(SeekFrom::End(-4)).map_err(io)?;
        let mut b4 = [0u8; 4];
        r.read_exact(&mut b4).map_err(io)?;
        // VERIFY: the C# reads this with ReadUInt32, i.e. little-endian.
        let entrypoint = u32::from_le_bytes(b4);
        // The C# negates and seeks with no bounds test (issue 2).
        if entrypoint as u64 > len {
            return Err(CdiError::BadEntrypoint { entrypoint, len });
        }
        r.seek(SeekFrom::End(-(entrypoint as i64))).map_err(io)?;

        let mut me = Self { entrypoint, ..Default::default() };
        me.num_sessions = read_u8(r)?;
        if me.num_sessions == 0 {
            return Err(CdiError::NoSessions);
        }

        // `i <= num_sessions` is the C#'s loop bound (issue 1).
        for i in 0..=(me.num_sessions as usize) {
            skip(r, 1)?; // VERIFY: 1 unknown byte
            let num_tracks = read_u8(r)?;
            skip(r, 13)?; // VERIFY: 13 unknown bytes
            me.sessions_raw.push(CdiSession { num_tracks });
            if num_tracks == 0 && i != me.num_sessions as usize {
                return Err(CdiError::EmptySession(i));
            }
            if num_tracks as usize + me.tracks.len() > MAX_TRACKS {
                return Err(CdiError::TooManyTracks(num_tracks as usize + me.tracks.len()));
            }
            for j in 0..num_tracks as usize {
                let header = Self::parse_track_header(r)?;
                let indexes = read_u16(r)?;
                if indexes < 2 {
                    // At least a pregap index and a real one.
                    return Err(CdiError::TooFewIndexes(indexes));
                }
                let mut index_sector_counts = Vec::with_capacity(indexes as usize);
                for _ in 0..indexes {
                    index_sector_counts.push(read_u32(r)?);
                }
                let num_cd_text_blocks = read_u32(r)?;
                let mut cd_text_blocks = Vec::new();
                for _ in 0..num_cd_text_blocks {
                    let mut block = CdiCdText::default();
                    // VERIFY: 18 is hard-coded in the C# with no count (issue 4).
                    for _ in 0..18 {
                        let n = read_u8(r)?;
                        if n > 0 {
                            block.texts.push(read_string(r, n as usize)?);
                        }
                    }
                    cd_text_blocks.push(block);
                }
                skip(r, 2)?; // VERIFY: 2 unknown bytes
                let track_mode = read_u8(r)?;
                if track_mode > 2 {
                    return Err(CdiError::InvalidTrackMode(track_mode));
                }
                skip(r, 7)?; // VERIFY: 7 unknown bytes
                let session_number = read_u32(r)?;
                if session_number as usize != i {
                    return Err(CdiError::SessionNumberMismatch {
                        expected: i,
                        found: session_number,
                    });
                }
                let track_number = read_u32(r)?;
                if track_number as usize != j {
                    return Err(CdiError::TrackNumberMismatch {
                        expected: j,
                        found: track_number,
                    });
                }
                let track_start_address = read_u32(r)?;
                let track_length = read_u32(r)?;
                me.tracks.push(CdiTrack {
                    header,
                    index_sector_counts,
                    cd_text_blocks,
                    track_mode,
                    session_number,
                    track_number,
                    track_start_address,
                    track_length,
                });
            }
        }
        Ok(me)
    }

    /// C# `ParseTrackHeader`.
    fn parse_track_header<R: Read + Seek>(r: &mut R) -> Result<CdiTrackHeader, CdiError> {
        skip(r, 15)?; // VERIFY: 15 unknown bytes
        let num_tracks = read_u8(r)?;
        let path_len = read_u8(r)?;
        let path = read_string(r, path_len as usize)?;
        skip(r, 29)?; // VERIFY: 29 unknown bytes
        let medium_type = read_u16(r)?;
        match medium_type {
            MEDIUM_DVD => return Err(CdiError::DvdNotSupported),
            MEDIUM_CD => {}
            other => return Err(CdiError::InvalidMediumType(other)),
        }
        Ok(CdiTrackHeader { num_tracks, path, medium_type })
    }
}

fn skip<R: Seek>(r: &mut R, n: i64) -> Result<(), CdiError> {
    r.seek(SeekFrom::Current(n))
        .map(|_| ())
        .map_err(|e| CdiError::Io(e.to_string()))
}

fn read_u8<R: Read>(r: &mut R) -> Result<u8, CdiError> {
    let mut b = [0u8; 1];
    r.read_exact(&mut b).map_err(|e| CdiError::Io(e.to_string()))?;
    Ok(b[0])
}

fn read_u16<R: Read>(r: &mut R) -> Result<u16, CdiError> {
    let mut b = [0u8; 2];
    r.read_exact(&mut b).map_err(|e| CdiError::Io(e.to_string()))?;
    Ok(u16::from_le_bytes(b))
}

fn read_u32<R: Read>(r: &mut R) -> Result<u32, CdiError> {
    let mut b = [0u8; 4];
    r.read_exact(&mut b).map_err(|e| CdiError::Io(e.to_string()))?;
    Ok(u32::from_le_bytes(b))
}

/// C# `ReadFUString(n)` — fixed-length UTF-8, NUL-trimmed.
fn read_string<R: Read>(r: &mut R, n: usize) -> Result<String, CdiError> {
    let mut v = vec![0u8; n];
    r.read_exact(&mut v).map_err(|e| CdiError::Io(e.to_string()))?;
    Ok(String::from_utf8_lossy(&v).trim_end_matches('\0').to_string())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Cursor;

    /// Builds a synthetic CDI descriptor. The skip widths follow my reading of
    /// the C#, so these tests verify self-consistency only — if a skip width is
    /// wrong, the builder and the parser are wrong together and still agree.
    /// That is the fundamental limit of testing this format blind.
    fn synth(sessions: u8, tracks_per_session: u8) -> Vec<u8> {
        let mut d: Vec<u8> = Vec::new();
        d.push(sessions);
        for i in 0..=(sessions as usize) {
            d.push(0); // unknown
            let n = if i == sessions as usize { 0 } else { tracks_per_session };
            d.push(n); // num tracks
            d.extend_from_slice(&[0; 13]); // unknown
            for j in 0..n as usize {
                // track header
                d.extend_from_slice(&[0; 15]);
                d.push(1); // num tracks in header
                let path = b"image.cdi";
                d.push(path.len() as u8);
                d.extend_from_slice(path);
                d.extend_from_slice(&[0; 29]);
                d.extend_from_slice(&MEDIUM_CD.to_le_bytes());
                // indexes
                d.extend_from_slice(&2u16.to_le_bytes());
                d.extend_from_slice(&150u32.to_le_bytes());
                d.extend_from_slice(&1000u32.to_le_bytes());
                // cd text blocks
                d.extend_from_slice(&0u32.to_le_bytes());
                d.extend_from_slice(&[0; 2]);
                d.push(1); // track mode
                d.extend_from_slice(&[0; 7]);
                d.extend_from_slice(&(i as u32).to_le_bytes()); // session number
                d.extend_from_slice(&(j as u32).to_le_bytes()); // track number
                d.extend_from_slice(&0u32.to_le_bytes()); // start address
                d.extend_from_slice(&1000u32.to_le_bytes()); // length
            }
        }
        // Descriptor sits at the end; the trailing u32 is its length + 4.
        let mut v: Vec<u8> = vec![0xAA; 64]; // some payload
        v.extend_from_slice(&d);
        let entrypoint = (d.len() + 4) as u32;
        v.extend_from_slice(&entrypoint.to_le_bytes());
        v
    }

    #[test]
    fn parses_a_single_session_image() {
        let mut c = Cursor::new(synth(1, 2));
        let f = CdiFile::parse(&mut c).expect("should parse");
        assert_eq!(f.num_sessions, 1);
        assert_eq!(f.tracks.len(), 2);
    }

    #[test]
    fn the_terminator_session_is_separated_from_the_real_ones() {
        // The C# loop is `i <= NumSessions`, so Sessions.Count is one too many.
        let mut c = Cursor::new(synth(1, 2));
        let f = CdiFile::parse(&mut c).unwrap();
        assert_eq!(f.sessions().len(), 1, "real sessions");
        assert_eq!(
            f.sessions_including_terminator().len(),
            2,
            "what the C# would expose"
        );
        assert_eq!(f.sessions_including_terminator()[1].num_tracks, 0);
    }

    #[test]
    fn multiple_sessions_parse() {
        let mut c = Cursor::new(synth(2, 1));
        let f = CdiFile::parse(&mut c).unwrap();
        assert_eq!(f.num_sessions, 2);
        assert_eq!(f.tracks.len(), 2, "one track in each of two sessions");
        assert_eq!(f.tracks[0].session_number, 0);
        assert_eq!(f.tracks[1].session_number, 1);
    }

    #[test]
    fn track_numbers_are_zero_based_and_per_session() {
        // Worth pinning: CD track numbers are 1-based and disc-global, but the
        // C# validates these against a per-session 0-based index (issue 3).
        let mut c = Cursor::new(synth(2, 2));
        let f = CdiFile::parse(&mut c).unwrap();
        let nums: Vec<u32> = f.tracks.iter().map(|t| t.track_number).collect();
        assert_eq!(nums, vec![0, 1, 0, 1], "restarts each session");
    }

    #[test]
    fn track_header_fields_are_read() {
        let mut c = Cursor::new(synth(1, 1));
        let f = CdiFile::parse(&mut c).unwrap();
        assert_eq!(f.tracks[0].header.path, "image.cdi");
        assert_eq!(f.tracks[0].header.medium_type, MEDIUM_CD);
        assert_eq!(f.tracks[0].index_sector_counts, vec![150, 1000]);
        assert_eq!(f.tracks[0].track_length, 1000);
    }

    #[test]
    fn zero_sessions_is_rejected() {
        let mut v: Vec<u8> = vec![0xAA; 16];
        v.push(0); // num sessions
        v.extend_from_slice(&5u32.to_le_bytes());
        let mut c = Cursor::new(v);
        assert_eq!(CdiFile::parse(&mut c), Err(CdiError::NoSessions));
    }

    #[test]
    fn an_entrypoint_past_the_file_is_rejected() {
        // The C# negates and seeks with no check (issue 2).
        let mut v: Vec<u8> = vec![0xAA; 32];
        v.extend_from_slice(&0xFFFF_FFFFu32.to_le_bytes());
        let mut c = Cursor::new(v);
        assert!(matches!(
            CdiFile::parse(&mut c),
            Err(CdiError::BadEntrypoint { .. })
        ));
    }

    #[test]
    fn short_files_are_rejected() {
        let mut c = Cursor::new(vec![0u8; 2]);
        assert_eq!(CdiFile::parse(&mut c), Err(CdiError::TooShort(2)));
    }

    #[test]
    fn dvd_medium_is_refused_as_in_the_c_sharp() {
        let mut v = synth(1, 1);
        let at = v
            .windows(2)
            .position(|w| w == MEDIUM_CD.to_le_bytes())
            .expect("medium type present");
        v[at..at + 2].copy_from_slice(&MEDIUM_DVD.to_le_bytes());
        let mut c = Cursor::new(v);
        assert_eq!(CdiFile::parse(&mut c), Err(CdiError::DvdNotSupported));
    }

    #[test]
    fn an_unknown_medium_type_is_rejected() {
        let mut v = synth(1, 1);
        let at = v
            .windows(2)
            .position(|w| w == MEDIUM_CD.to_le_bytes())
            .unwrap();
        v[at..at + 2].copy_from_slice(&0x1234u16.to_le_bytes());
        let mut c = Cursor::new(v);
        assert_eq!(
            CdiFile::parse(&mut c),
            Err(CdiError::InvalidMediumType(0x1234))
        );
    }

    #[test]
    fn fewer_than_two_indexes_is_rejected() {
        let mut v = synth(1, 1);
        let at = v
            .windows(2)
            .position(|w| w == 2u16.to_le_bytes())
            .expect("index count present");
        v[at..at + 2].copy_from_slice(&1u16.to_le_bytes());
        let mut c = Cursor::new(v);
        assert!(matches!(
            CdiFile::parse(&mut c),
            Err(CdiError::TooFewIndexes(1))
        ));
    }

    #[test]
    fn an_empty_non_final_session_is_rejected() {
        // Session 0 with 0 tracks, where the terminator is session 1.
        let mut v: Vec<u8> = vec![0xAA; 16];
        let mut d: Vec<u8> = Vec::new();
        d.push(1); // one session
        d.push(0); // unknown
        d.push(0); // num tracks == 0, but i != num_sessions
        d.extend_from_slice(&[0; 13]);
        v.extend_from_slice(&d);
        v.extend_from_slice(&((d.len() + 4) as u32).to_le_bytes());
        let mut c = Cursor::new(v);
        assert_eq!(CdiFile::parse(&mut c), Err(CdiError::EmptySession(0)));
    }

    #[test]
    fn medium_constants_match_the_c_sharp() {
        assert_eq!(MEDIUM_CD, 0x0098);
        assert_eq!(MEDIUM_DVD, 0x0038);
        assert_eq!(MAX_TRACKS, 99);
    }
}
