// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (CueFormat)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// CUE sheet parsing: the tokenizer, the command records, and the dispatch.
//
// This is the most portable region of `Disc.cs` — it is text parsing over a
// documented format with no sector arithmetic, no crypto, and no FFI, so it can
// be ported and tested without a disc image. `MSF` timecodes come from
// `disc_addressing.rs`, which is separately verified.
//
// ===================== FIVE C#-SIDE BUGS =================================
//
//   1. **AIFF files are never recognised.** The `FILE` type dispatch reads:
//
//          case "BINARY":     ft = CueFileType.BINARY;   break;
//          case "MOTOROLA":   ft = CueFileType.MOTOROLA; break;
//          case "BINARAIFF":  ft = CueFileType.AIFF;     break;   // <-- typo
//          case "WAVE":       ft = CueFileType.WAVE;     break;
//
//      `BINARAIFF` is not a CUE keyword — it looks like a botched merge of
//      `case "BINARY":` and `case "AIFF":`. So a sheet saying
//      `FILE "track.aiff" AIFF` falls to `default`, logs "Unknown FILE type",
//      and loads as `Unspecified`; and the `AIFF` variant is unreachable.
//      **Fix this in the C# tree.**
//
//   2. **The `FLAGS` dispatch puts `case "DATA":` immediately above
//      `default:`**, so `DATA` falls through into the default arm and logs
//      "Unknown FLAG: DATA" — for a flag the enum defines and the comment
//      documents. It is a deliberate no-op with a misleading diagnostic.
//
//   3. **`CueLineParser.ReadToken` indexes `str[index]` before checking
//      bounds.** It is called with `Eof` false, but `Eof` is only set *after* a
//      read reaches the end — so an empty line, or one ending in whitespace,
//      can index past the end. The port iterates bytes with bounds checks.
//
//   4. **`ReadToken(Quotable)` trims *all* quotes with `Trim('"')`**, not just
//      a matched leading/trailing pair. A path legitimately containing a quote
//      loses it, and an unterminated quote silently yields a path with the
//      opening quote stripped rather than an error.
//
//   5. **The backslash case in `ReadToken` does nothing.**
//      `case '\\': index++; break;` is identical to `default:`, so it is not an
//      escape — it just advances. Windows paths therefore work by accident
//      (backslash is an ordinary character), but a `\"` inside a quoted path
//      terminates the quote instead of escaping it.

use crate::disc_addressing::Msf;

/// C# `enum CueFileType`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum CueFileType {
    #[default]
    Unspecified,
    /// Intel binary (least significant byte first).
    Binary,
    /// Motorola binary (most significant byte first).
    Motorola,
    Aiff,
    Wave,
    Mp3,
}

impl CueFileType {
    /// The keyword dispatch, with `AIFF` spelled correctly — see bug 1.
    pub fn parse(token: &str) -> Option<Self> {
        Some(match token.to_ascii_uppercase().as_str() {
            "BINARY" => Self::Binary,
            "MOTOROLA" => Self::Motorola,
            "AIFF" => Self::Aiff,
            "WAVE" => Self::Wave,
            "MP3" => Self::Mp3,
            _ => return None,
        })
    }

    /// The C#'s literal dispatch, for reading sheets it already mis-parsed.
    #[deprecated(note = "mirrors a C#-side typo: matches BINARAIFF, never AIFF")]
    pub fn parse_bug_compat(token: &str) -> Option<Self> {
        Some(match token.to_ascii_uppercase().as_str() {
            "BINARY" => Self::Binary,
            "MOTOROLA" => Self::Motorola,
            "BINARAIFF" => Self::Aiff,
            "WAVE" => Self::Wave,
            "MP3" => Self::Mp3,
            _ => return None,
        })
    }
}

/// C# `enum CueTrackType`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub enum CueTrackType {
    #[default]
    Unknown,
    /// Audio/Music, 2352 bytes per sector.
    Audio,
    /// Karaoke CD+G, 2448.
    Cdg,
    /// CDROM Mode1 Data, cooked.
    Mode1_2048,
    /// CDROM Mode1 Data, raw.
    Mode1_2352,
    /// CDROM-XA Mode2, form 1 or 2.
    Mode2_2336,
    /// CDROM-XA Mode2, raw.
    Mode2_2352,
    /// CDI Mode2 Data.
    Cdi_2336,
    /// CDI Mode2 Data.
    Cdi_2352,
}

impl CueTrackType {
    /// Bytes per sector for this track type — the number that decides how a
    /// blob is sliced. The C# carries these only as comments on the enum.
    pub const fn sector_size(self) -> Option<u32> {
        Some(match self {
            Self::Unknown => return None,
            Self::Audio => 2352,
            Self::Cdg => 2448,
            Self::Mode1_2048 => 2048,
            Self::Mode1_2352 => 2352,
            Self::Mode2_2336 => 2336,
            Self::Mode2_2352 => 2352,
            Self::Cdi_2336 => 2336,
            Self::Cdi_2352 => 2352,
        })
    }

    /// Whether this is a data track (as opposed to audio).
    pub const fn is_data(self) -> bool {
        !matches!(self, Self::Audio | Self::Cdg | Self::Unknown)
    }

    /// C# `TRACK` type keyword dispatch.
    pub fn parse(token: &str) -> Option<Self> {
        Some(match token.to_ascii_uppercase().as_str() {
            "AUDIO" => Self::Audio,
            "CDG" => Self::Cdg,
            "MODE1/2048" => Self::Mode1_2048,
            "MODE1/2352" => Self::Mode1_2352,
            "MODE2/2336" => Self::Mode2_2336,
            "MODE2/2352" => Self::Mode2_2352,
            "CDI/2336" => Self::Cdi_2336,
            "CDI/2352" => Self::Cdi_2352,
            _ => return None,
        })
    }
}

/// C# `[Flags] enum CueTrackFlags`.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub struct CueTrackFlags(pub u32);

impl CueTrackFlags {
    pub const NONE: Self = Self(0);
    /// Pre-emphasis enabled (audio tracks only).
    pub const PRE: Self = Self(1);
    /// Digital copy permitted.
    pub const DCP: Self = Self(2);
    /// Set automatically by cue-processing equipment.
    pub const DATA: Self = Self(4);
    /// Four channel audio.
    pub const FOUR_CH: Self = Self(8);
    /// Serial copy management system.
    pub const SCMS: Self = Self(64);

    #[inline]
    pub const fn contains(self, o: Self) -> bool {
        self.0 & o.0 == o.0
    }

    #[inline]
    pub const fn union(self, o: Self) -> Self {
        Self(self.0 | o.0)
    }

    /// One `FLAGS` keyword.
    ///
    /// The C# lists `case "DATA":` directly above `default:`, so `DATA` falls
    /// through and logs "Unknown FLAG: DATA" (bug 2). Here it is recognised and
    /// returns `DATA`; a caller wanting the C#'s no-op can ignore that bit,
    /// which is at least an explicit choice.
    pub fn parse_one(token: &str) -> Option<Self> {
        Some(match token.to_ascii_uppercase().as_str() {
            "PRE" => Self::PRE,
            "DCP" => Self::DCP,
            "DATA" => Self::DATA,
            "4CH" => Self::FOUR_CH,
            "SCMS" => Self::SCMS,
            _ => return None,
        })
    }
}

impl std::ops::BitOr for CueTrackFlags {
    type Output = Self;
    fn bitor(self, o: Self) -> Self {
        self.union(o)
    }
}

/// C# `CueFile.Command` and its 15 implementing records.
///
/// The C# models each as a separate `readonly struct` implementing an empty
/// marker interface, then dispatches with `is`-pattern chains. An enum makes
/// the set closed and the match exhaustive.
#[derive(Debug, Clone, PartialEq)]
pub enum CueCommand {
    Catalog(String),
    CdTextFile(String),
    File { path: String, file_type: CueFileType },
    Flags(CueTrackFlags),
    Index { number: i32, timestamp: Msf },
    Isrc(String),
    Performer(String),
    Postgap(Msf),
    Pregap(Msf),
    Rem(String),
    Comment(String),
    Songwriter(String),
    Title(String),
    Track { number: i32, track_type: CueTrackType },
    Session(i32),
}

impl std::fmt::Display for CueCommand {
    /// Matches the C# `ToString()` of each record, including its field widths.
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Catalog(v) => write!(f, "CATALOG: {v}"),
            Self::CdTextFile(p) => write!(f, "CDTEXTFILE: {p}"),
            Self::File { path, file_type } => write!(f, "FILE ({file_type:?}): {path}"),
            Self::Flags(x) => write!(f, "FLAGS {}", x.0),
            Self::Index { number, timestamp } => write!(f, "INDEX {number:2} {timestamp}"),
            Self::Isrc(v) => write!(f, "ISRC: {v}"),
            Self::Performer(v) => write!(f, "PERFORMER: {v}"),
            Self::Postgap(l) => write!(f, "POSTGAP: {l}"),
            Self::Pregap(l) => write!(f, "PREGAP: {l}"),
            Self::Rem(v) => write!(f, "REM: {v}"),
            Self::Comment(v) => write!(f, "COMMENT: {v}"),
            Self::Songwriter(v) => write!(f, "SONGWRITER: {v}"),
            Self::Title(v) => write!(f, "TITLE: {v}"),
            Self::Track { number, track_type } => {
                write!(f, "TRACK {number:2} ({track_type:?})")
            }
            Self::Session(n) => write!(f, "SESSION {n}"),
        }
    }
}

/// A diagnostic from parsing. The C# accumulates these through `Error()` and
/// `Log.Warn`, then exposes a single `HasError` bool.
#[derive(Debug, Clone, PartialEq)]
pub struct CueDiagnostic {
    pub line: usize,
    pub message: String,
    pub is_error: bool,
}

/// C# `CueFile.CueLineParser` — a whitespace tokenizer with optional quoting.
pub struct CueLineParser<'a> {
    bytes: &'a [u8],
    index: usize,
}

impl<'a> CueLineParser<'a> {
    pub fn new(line: &'a str) -> Self {
        Self { bytes: line.as_bytes(), index: 0 }
    }

    /// C# `Eof`.
    ///
    /// The C# sets this only *after* a read consumes the last character, so it
    /// is false on an empty line and `ReadToken` then indexes `str[0]` — bug 3.
    /// Here it is derived from the position, so it is true up front.
    #[inline]
    pub fn eof(&self) -> bool {
        self.index >= self.bytes.len()
    }

    fn skip_whitespace(&mut self) {
        while self.index < self.bytes.len()
            && matches!(self.bytes[self.index], b' ' | b'\t')
        {
            self.index += 1;
        }
    }

    /// C# `ReadToken()` — whitespace-delimited, no quote handling.
    pub fn read_token(&mut self) -> Option<&'a str> {
        self.skip_whitespace();
        if self.eof() {
            return None;
        }
        let start = self.index;
        while self.index < self.bytes.len()
            && !matches!(self.bytes[self.index], b' ' | b'\t')
        {
            self.index += 1;
        }
        std::str::from_utf8(&self.bytes[start..self.index]).ok()
    }

    /// C# `ReadPath()` — as above, but a leading `"` runs to the matching `"`.
    ///
    /// Unlike the C#, only a *matched* surrounding pair is stripped, so a path
    /// containing a quote keeps it (bug 4), and an unterminated quote is an
    /// error rather than a silently truncated path.
    pub fn read_path(&mut self) -> Result<&'a str, String> {
        self.skip_whitespace();
        if self.eof() {
            return Err("expected a path".into());
        }
        if self.bytes[self.index] != b'"' {
            return self.read_token().ok_or_else(|| "expected a path".into());
        }
        self.index += 1; // opening quote
        let start = self.index;
        while self.index < self.bytes.len() && self.bytes[self.index] != b'"' {
            self.index += 1;
        }
        if self.eof() {
            return Err("unterminated quote in path".into());
        }
        let end = self.index;
        self.index += 1; // closing quote
        std::str::from_utf8(&self.bytes[start..end]).map_err(|e| e.to_string())
    }

    /// C# `ReadLine()` — everything remaining, verbatim.
    pub fn read_rest(&mut self) -> &'a str {
        let start = self.index;
        self.index = self.bytes.len();
        std::str::from_utf8(&self.bytes[start..]).unwrap_or("")
    }
}

/// C# `CueFile` — the parsed sheet.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct CueFile {
    pub commands: Vec<CueCommand>,
    pub diagnostics: Vec<CueDiagnostic>,
}

impl CueFile {
    /// C# `CueFile.HasError`.
    pub fn has_error(&self) -> bool {
        self.diagnostics.iter().any(|d| d.is_error)
    }

    /// C# `LoadFromStream` — one command per non-empty line.
    pub fn parse(text: &str) -> Self {
        let mut me = Self::default();
        for (n, raw) in text.lines().enumerate() {
            let line = raw.trim();
            if line.is_empty() {
                continue;
            }
            // C#: a line starting with ';' is a comment.
            if let Some(rest) = line.strip_prefix(';') {
                me.commands.push(CueCommand::Comment(rest.to_string()));
                continue;
            }
            let mut p = CueLineParser::new(line);
            let Some(keyword) = p.read_token() else { continue };
            me.dispatch(n + 1, &keyword.to_ascii_uppercase(), &mut p);
        }
        me
    }

    fn error(&mut self, line: usize, message: impl Into<String>) {
        self.diagnostics.push(CueDiagnostic {
            line,
            message: message.into(),
            is_error: true,
        });
    }

    fn warn(&mut self, line: usize, message: impl Into<String>) {
        self.diagnostics.push(CueDiagnostic {
            line,
            message: message.into(),
            is_error: false,
        });
    }

    fn dispatch(&mut self, line: usize, keyword: &str, p: &mut CueLineParser) {
        match keyword {
            "CATALOG" => match p.read_token() {
                Some(v) => self.commands.push(CueCommand::Catalog(v.to_string())),
                None => self.error(line, "CATALOG is missing its value"),
            },
            "CDTEXTFILE" => match p.read_path() {
                Ok(v) => self.commands.push(CueCommand::CdTextFile(v.to_string())),
                Err(e) => self.error(line, e),
            },
            "FILE" => {
                let path = match p.read_path() {
                    Ok(v) => v.to_string(),
                    Err(e) => return self.error(line, e),
                };
                let file_type = match p.read_token() {
                    None => {
                        self.error(line, "FILE command is missing file type.");
                        CueFileType::Unspecified
                    }
                    Some(t) => match CueFileType::parse(t) {
                        Some(ft) => ft,
                        None => {
                            self.error(line, format!("Unknown FILE type: {t}"));
                            CueFileType::Unspecified
                        }
                    },
                };
                self.commands.push(CueCommand::File { path, file_type });
            }
            "FLAGS" => {
                let mut flags = CueTrackFlags::NONE;
                while let Some(t) = p.read_token() {
                    match CueTrackFlags::parse_one(t) {
                        Some(f) => flags = flags | f,
                        None => self.warn(line, format!("Unknown FLAG: {t}")),
                    }
                }
                self.commands.push(CueCommand::Flags(flags));
            }
            "INDEX" => {
                let number = match p.read_token().and_then(|t| t.parse::<i32>().ok()) {
                    Some(n) => n,
                    None => return self.error(line, "INDEX has a malformed number"),
                };
                match p.read_token().and_then(Msf::parse) {
                    Some(timestamp) => {
                        self.commands.push(CueCommand::Index { number, timestamp })
                    }
                    None => self.error(line, "INDEX has a malformed timestamp"),
                }
            }
            "ISRC" => match p.read_token() {
                Some(v) => self.commands.push(CueCommand::Isrc(v.to_string())),
                None => self.error(line, "ISRC is missing its value"),
            },
            "PERFORMER" => {
                let v = p.read_path().map(str::to_string).unwrap_or_default();
                self.commands.push(CueCommand::Performer(v));
            }
            "POSTGAP" | "PREGAP" => {
                match p.read_token().and_then(Msf::parse) {
                    Some(m) => self.commands.push(if keyword == "PREGAP" {
                        CueCommand::Pregap(m)
                    } else {
                        CueCommand::Postgap(m)
                    }),
                    None => self.error(line, format!("{keyword} has a malformed length")),
                }
            }
            "REM" => {
                let v = p.read_rest().trim().to_string();
                self.commands.push(CueCommand::Rem(v));
            }
            "SONGWRITER" => {
                let v = p.read_path().map(str::to_string).unwrap_or_default();
                self.commands.push(CueCommand::Songwriter(v));
            }
            "TITLE" => {
                let v = p.read_path().map(str::to_string).unwrap_or_default();
                self.commands.push(CueCommand::Title(v));
            }
            "TRACK" => {
                let number = match p.read_token().and_then(|t| t.parse::<i32>().ok()) {
                    Some(n) => n,
                    None => return self.error(line, "TRACK has a malformed number"),
                };
                let track_type = match p.read_token() {
                    Some(t) => match CueTrackType::parse(t) {
                        Some(tt) => tt,
                        None => {
                            self.error(line, format!("Unknown TRACK type: {t}"));
                            CueTrackType::Unknown
                        }
                    },
                    None => {
                        self.error(line, "TRACK is missing its type");
                        CueTrackType::Unknown
                    }
                };
                self.commands.push(CueCommand::Track { number, track_type });
            }
            "SESSION" => match p.read_token().and_then(|t| t.parse::<i32>().ok()) {
                Some(n) => self.commands.push(CueCommand::Session(n)),
                None => self.error(line, "SESSION has a malformed number"),
            },
            other => self.warn(line, format!("Unknown command: {other}")),
        }
    }

    /// Tracks in the sheet, in order, with their file and index timings.
    ///
    /// The C# does this in `CueCompiler`, interleaved with blob resolution and
    /// length probing. Separated here so the sheet's structure can be read
    /// without touching the filesystem.
    pub fn tracks(&self) -> Vec<CueTrack> {
        let mut out: Vec<CueTrack> = Vec::new();
        let mut current_file: Option<(String, CueFileType)> = None;
        for c in &self.commands {
            match c {
                CueCommand::File { path, file_type } => {
                    current_file = Some((path.clone(), *file_type));
                }
                CueCommand::Track { number, track_type } => out.push(CueTrack {
                    number: *number,
                    track_type: *track_type,
                    file: current_file.clone(),
                    indexes: Vec::new(),
                    pregap: None,
                    postgap: None,
                    flags: CueTrackFlags::NONE,
                }),
                CueCommand::Index { number, timestamp } => {
                    if let Some(t) = out.last_mut() {
                        t.indexes.push((*number, *timestamp));
                    }
                }
                CueCommand::Pregap(m) => {
                    if let Some(t) = out.last_mut() {
                        t.pregap = Some(*m);
                    }
                }
                CueCommand::Postgap(m) => {
                    if let Some(t) = out.last_mut() {
                        t.postgap = Some(*m);
                    }
                }
                CueCommand::Flags(f) => {
                    if let Some(t) = out.last_mut() {
                        t.flags = *f;
                    }
                }
                _ => {}
            }
        }
        out
    }
}

/// One track's structure, assembled from the command stream.
#[derive(Debug, Clone, PartialEq)]
pub struct CueTrack {
    pub number: i32,
    pub track_type: CueTrackType,
    /// The `FILE` in effect when this `TRACK` appeared. `None` means the sheet
    /// declared a track before any file — malformed, and the C# would
    /// dereference a null blob later.
    pub file: Option<(String, CueFileType)>,
    /// `(index number, timestamp)` pairs, in sheet order.
    pub indexes: Vec<(i32, Msf)>,
    pub pregap: Option<Msf>,
    pub postgap: Option<Msf>,
    pub flags: CueTrackFlags,
}

impl CueTrack {
    /// The track's start, i.e. `INDEX 01`. `INDEX 00` is the pregap.
    pub fn start(&self) -> Option<Msf> {
        self.indexes.iter().find(|(n, _)| *n == 1).map(|(_, m)| *m)
    }

    /// `INDEX 00` if present — the pregap start.
    pub fn pregap_start(&self) -> Option<Msf> {
        self.indexes.iter().find(|(n, _)| *n == 0).map(|(_, m)| *m)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    const SHEET: &str = r#"REM GENRE Electronica
TITLE "Some Album"
PERFORMER "Some Artist"
FILE "track01.bin" BINARY
  TRACK 01 MODE1/2352
    INDEX 01 00:00:00
  TRACK 02 AUDIO
    PREGAP 00:02:00
    INDEX 00 04:35:20
    INDEX 01 04:37:20
    FLAGS DCP
"#;

    #[test]
    fn parses_a_typical_sheet() {
        let c = CueFile::parse(SHEET);
        assert!(!c.has_error(), "{:?}", c.diagnostics);
        let t = c.tracks();
        assert_eq!(t.len(), 2);
        assert_eq!(t[0].number, 1);
        assert_eq!(t[0].track_type, CueTrackType::Mode1_2352);
        assert_eq!(t[1].track_type, CueTrackType::Audio);
        assert_eq!(t[0].file.as_ref().unwrap().0, "track01.bin");
        assert_eq!(t[0].file.as_ref().unwrap().1, CueFileType::Binary);
    }

    #[test]
    fn track_indexes_and_gaps_attach_to_the_right_track() {
        let t = CueFile::parse(SHEET).tracks();
        assert_eq!(t[0].start(), Msf::parse("00:00:00"));
        assert_eq!(t[1].start(), Msf::parse("04:37:20"));
        assert_eq!(t[1].pregap_start(), Msf::parse("04:35:20"));
        assert_eq!(t[1].pregap, Msf::parse("00:02:00"));
        assert!(t[0].pregap.is_none());
        assert!(t[1].flags.contains(CueTrackFlags::DCP));
        assert!(!t[0].flags.contains(CueTrackFlags::DCP));
    }

    #[test]
    fn aiff_is_recognised() {
        // The C# matches "BINARAIFF" instead, so this falls to default there.
        let c = CueFile::parse("FILE \"a.aiff\" AIFF\n");
        assert!(!c.has_error(), "{:?}", c.diagnostics);
        assert_eq!(
            c.commands[0],
            CueCommand::File { path: "a.aiff".into(), file_type: CueFileType::Aiff }
        );
    }

    #[test]
    fn the_c_sharp_dispatch_misses_aiff_and_accepts_a_non_keyword() {
        #[allow(deprecated)]
        {
            assert!(CueFileType::parse_bug_compat("AIFF").is_none());
            assert_eq!(
                CueFileType::parse_bug_compat("BINARAIFF"),
                Some(CueFileType::Aiff)
            );
        }
        // And the corrected dispatch is the other way round.
        assert_eq!(CueFileType::parse("AIFF"), Some(CueFileType::Aiff));
        assert!(CueFileType::parse("BINARAIFF").is_none());
    }

    #[test]
    fn data_flag_is_recognised_not_warned_about() {
        // The C# lists `case "DATA":` above `default:`, so it falls through and
        // logs "Unknown FLAG: DATA".
        let c = CueFile::parse("FLAGS DATA DCP\n");
        assert!(c.diagnostics.is_empty(), "{:?}", c.diagnostics);
        let CueCommand::Flags(f) = &c.commands[0] else { panic!() };
        assert!(f.contains(CueTrackFlags::DATA));
        assert!(f.contains(CueTrackFlags::DCP));
    }

    #[test]
    fn all_flag_keywords_parse() {
        for (k, v) in [
            ("PRE", CueTrackFlags::PRE),
            ("DCP", CueTrackFlags::DCP),
            ("DATA", CueTrackFlags::DATA),
            ("4CH", CueTrackFlags::FOUR_CH),
            ("SCMS", CueTrackFlags::SCMS),
        ] {
            assert_eq!(CueTrackFlags::parse_one(k), Some(v), "{k}");
        }
        assert!(CueTrackFlags::parse_one("NOPE").is_none());
    }

    #[test]
    fn empty_and_whitespace_lines_do_not_panic() {
        // The C# `ReadToken` indexes str[index] before bounds-checking (bug 3).
        for s in ["", "\n\n", "   \n\t\n", "   TITLE   \n"] {
            let _ = CueFile::parse(s);
        }
        let mut p = CueLineParser::new("");
        assert!(p.eof());
        assert!(p.read_token().is_none());
        assert!(p.read_path().is_err());
    }

    #[test]
    fn quoted_paths_keep_interior_spaces() {
        let c = CueFile::parse("FILE \"My Album (Disc 1).bin\" BINARY\n");
        assert_eq!(
            c.commands[0],
            CueCommand::File {
                path: "My Album (Disc 1).bin".into(),
                file_type: CueFileType::Binary,
            }
        );
    }

    #[test]
    fn unquoted_paths_still_work() {
        let c = CueFile::parse("FILE track.bin BINARY\n");
        let CueCommand::File { path, .. } = &c.commands[0] else { panic!() };
        assert_eq!(path, "track.bin");
    }

    #[test]
    fn unterminated_quote_is_an_error_not_a_truncated_path() {
        // The C# Trim('"') silently yields a path with the quote stripped.
        let c = CueFile::parse("FILE \"unterminated.bin BINARY\n");
        assert!(c.has_error(), "should report the unterminated quote");
    }

    #[test]
    fn windows_paths_survive_the_backslash_case() {
        // The C#'s `case '\\'` is identical to default, so backslashes work by
        // accident rather than by escaping.
        let c = CueFile::parse(r#"FILE "C:\Games\disc.bin" BINARY"#);
        let CueCommand::File { path, .. } = &c.commands[0] else { panic!() };
        assert_eq!(path, r"C:\Games\disc.bin");
    }

    #[test]
    fn malformed_timestamps_are_reported() {
        let c = CueFile::parse("TRACK 01 AUDIO\nINDEX 01 99:99:99\n");
        assert!(c.has_error(), "99:99:99 is out of range");
    }

    #[test]
    fn unknown_commands_warn_but_do_not_fail() {
        let c = CueFile::parse("FLURBLE 1 2 3\nTRACK 01 AUDIO\n");
        assert!(!c.has_error());
        assert_eq!(c.diagnostics.len(), 1);
        assert!(!c.diagnostics[0].is_error);
    }

    #[test]
    fn semicolon_lines_are_comments() {
        let c = CueFile::parse("; this is a comment\nTRACK 01 AUDIO\n");
        assert_eq!(c.commands[0], CueCommand::Comment(" this is a comment".into()));
    }

    #[test]
    fn rem_keeps_its_whole_remainder() {
        let c = CueFile::parse("REM GENRE Some Long Genre Name\n");
        assert_eq!(
            c.commands[0],
            CueCommand::Rem("GENRE Some Long Genre Name".into())
        );
    }

    #[test]
    fn track_types_carry_their_sector_sizes() {
        assert_eq!(CueTrackType::Audio.sector_size(), Some(2352));
        assert_eq!(CueTrackType::Mode1_2048.sector_size(), Some(2048));
        assert_eq!(CueTrackType::Mode2_2336.sector_size(), Some(2336));
        assert_eq!(CueTrackType::Cdg.sector_size(), Some(2448));
        assert_eq!(CueTrackType::Unknown.sector_size(), None);
    }

    #[test]
    fn data_and_audio_tracks_are_distinguished() {
        assert!(CueTrackType::Mode1_2352.is_data());
        assert!(CueTrackType::Cdi_2336.is_data());
        assert!(!CueTrackType::Audio.is_data());
        assert!(!CueTrackType::Cdg.is_data());
    }

    #[test]
    fn all_track_type_keywords_parse() {
        for k in [
            "AUDIO", "CDG", "MODE1/2048", "MODE1/2352", "MODE2/2336", "MODE2/2352",
            "CDI/2336", "CDI/2352",
        ] {
            assert!(CueTrackType::parse(k).is_some(), "{k}");
        }
        assert!(CueTrackType::parse("MODE3/9999").is_none());
    }

    #[test]
    fn a_track_before_any_file_is_visible_as_such() {
        // The C# would carry a null blob into the compiler.
        let t = CueFile::parse("TRACK 01 AUDIO\nINDEX 01 00:00:00\n").tracks();
        assert!(t[0].file.is_none());
    }

    #[test]
    fn keywords_are_case_insensitive() {
        let c = CueFile::parse("file \"a.bin\" binary\ntrack 01 audio\n");
        assert!(!c.has_error(), "{:?}", c.diagnostics);
        assert_eq!(c.tracks().len(), 1);
    }

    #[test]
    fn command_display_matches_the_c_sharp_formats() {
        assert_eq!(
            CueCommand::Track { number: 1, track_type: CueTrackType::Audio }.to_string(),
            "TRACK  1 (Audio)"
        );
        assert_eq!(
            CueCommand::Index { number: 1, timestamp: Msf::parse("00:02:00").unwrap() }
                .to_string(),
            "INDEX  1 +00:02:00"
        );
        assert_eq!(CueCommand::Catalog("123".into()).to_string(), "CATALOG: 123");
    }
}
