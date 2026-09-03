// PORT-SOURCE: Vfx/OpenStack.Vfx/Disc.cs (CcdFormat)
// PORT-SHA: SHARED
// PORT-SHARED: yes  (extracted from the source file above, which has its own .rs)
// PORT-STATUS: done
//
// ==========================================================================
// UNVERIFIED: written without a toolchain and without a single .ccd file to
// test against. Ported at explicit request after I advised against it. Do not
// trust this against real images until it has been run on some.
//
// It is a reviewed translation, and the CCD container is INI text (which is why
// I did this one first) — but the TOC semantics below are my reading of the C#,
// not something I have observed working. The specific things to check first are
// marked `VERIFY:` inline.
// ==========================================================================
//
// CloneCD `.ccd` sidecar: an INI file describing the disc TOC, paired with a
// `.img` (raw 2352-byte sectors) and optionally a `.sub` (subchannel).
//
// ===================== SIX C#-SIDE BUGS =================================
//
//   1. **`TOCENTRIES` and `SESSIONS` are read and never used.** Both are
//      assigned to locals with a comment saying "its conceivable that this
//      could be missing" — and then nothing reads them, so the declared counts
//      are never checked against what was actually parsed. A truncated CCD
//      loads silently with fewer entries than it claims.
//
//   2. **Those same two reads use the indexer, not `FetchOrDefault`.** So the
//      "conceivable" missing key throws `KeyNotFoundException` — the comment
//      identifies the hazard and the code does the one thing that fails on it.
//
//   3. **A consistency warning is thrown as a fatal error.** The ALBA/PLBA
//      checks throw `InvalidOperationException("Warning: inconsistency ...")`.
//      A message beginning "Warning:" that aborts the load is one or the other,
//      not both. The port reports these as diagnostics and keeps loading.
//
//   4. **`line.Split('=')` requires exactly two parts**, so any value
//      containing `=` throws. CD-Text and some writers emit those.
//
//   5. **`int.Parse` on every value.** A non-numeric value anywhere outside the
//      skipped `FLAGS` key throws, so one unexpected string aborts the whole
//      parse rather than being ignored.
//
//   6. **`section.Name.Split(' ')[1]` is unguarded**, so a bare `[TRACK]`
//      section (no number) throws `IndexOutOfRangeException`. Same for
//      `INDEX`-prefixed keys without a space.

use std::collections::HashMap;

use crate::disc_addressing::Msf;

/// C# `CcdTocEntry`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct CcdTocEntry {
    pub entry_num: i32,
    pub session: i32,
    pub alba: i32,
    pub plba: i32,
    pub control: i32,
    pub adr: i32,
    pub track_no: i32,
    pub point: i32,
    pub a_min: i32,
    pub a_sec: i32,
    pub a_frame: i32,
    pub zero: i32,
    pub p_min: i32,
    pub p_sec: i32,
    pub p_frame: i32,
}

impl CcdTocEntry {
    /// Absolute MSF as a timecode. `None` if the stored components are out of
    /// range — the C# builds an `MSF` regardless (see the `MSF` range bugs in
    /// PORTING.md).
    pub fn absolute_msf(&self) -> Option<Msf> {
        Msf::new(self.a_min, self.a_sec, self.a_frame)
    }

    /// Position MSF as a timecode.
    pub fn position_msf(&self) -> Option<Msf> {
        Msf::new(self.p_min, self.p_sec, self.p_frame)
    }

    /// Whether the stored `ALBA` agrees with the absolute MSF.
    ///
    /// VERIFY: the C# asserts `MSF(AMin,ASec,AFrame).Sector == ALBA + 150`,
    /// i.e. ALBA is a *logical* LBA and the MSF is absolute. That matches the
    /// lead-in relationship in `disc_addressing`, but I have not seen it hold
    /// on a real file.
    pub fn alba_consistent(&self) -> bool {
        self.absolute_msf().map(|m| m.to_lba() == self.alba).unwrap_or(false)
    }

    /// Whether the stored `PLBA` agrees with the position MSF.
    pub fn plba_consistent(&self) -> bool {
        self.position_msf().map(|m| m.to_lba() == self.plba).unwrap_or(false)
    }
}

/// C# `CcdTrack`.
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct CcdTrack {
    pub number: i32,
    pub mode: i32,
    /// `INDEX n = lba` pairs.
    pub indexes: HashMap<i32, i32>,
}

/// C# `CcdSession`.
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct CcdSession {
    pub number: i32,
    pub pregap_mode: i32,
    pub pregap_subcode: i32,
}

/// A parse diagnostic. The C# throws on all of these.
#[derive(Debug, Clone, PartialEq)]
pub struct CcdDiagnostic {
    pub line: usize,
    pub message: String,
    pub is_error: bool,
}

/// C# `CcdFile`.
#[derive(Debug, Clone, Default, PartialEq)]
pub struct CcdFile {
    pub version: i32,
    pub data_tracks_scrambled: i32,
    pub cd_text_length: i32,
    pub sessions: Vec<CcdSession>,
    pub toc_entries: Vec<CcdTocEntry>,
    pub tracks: Vec<CcdTrack>,
    /// Declared `[Disc] TOCEntries`, which the C# reads and discards (bug 1).
    pub declared_toc_entries: Option<i32>,
    /// Declared `[Disc] Sessions`, likewise discarded by the C#.
    pub declared_sessions: Option<i32>,
    pub diagnostics: Vec<CcdDiagnostic>,
}

/// One `[Section]` and its `KEY=value` pairs, uppercased as the C# does.
#[derive(Debug, Clone, Default)]
struct CcdSection {
    name: String,
    values: HashMap<String, i32>,
    line: usize,
}

impl CcdSection {
    /// C# `FetchOrDefault`.
    fn get_or(&self, key: &str, default: i32) -> i32 {
        self.values.get(key).copied().unwrap_or(default)
    }

    /// C# `FetchOrFail` — an error here rather than a throw.
    fn require(&self, key: &str) -> Result<i32, String> {
        self.values
            .get(key)
            .copied()
            .ok_or_else(|| format!("missing required [{}] key: {key}", self.name))
    }

    /// The trailing number in `[SESSION 1]` / `[ENTRY 3]` / `[TRACK 2]`.
    ///
    /// `None` when absent, where the C# indexes `Split(' ')[1]` and throws
    /// (bug 6).
    fn ordinal(&self) -> Option<i32> {
        self.name.split_whitespace().nth(1)?.parse().ok()
    }
}

#[derive(Debug, Clone, PartialEq)]
pub enum CcdError {
    NoSections,
    InsufficientSections(usize),
    /// First section must be `[CloneCD]`.
    BadFirstSection(String),
    MissingVersion,
    /// Second section must be `[Disc]`.
    BadSecondSection(String),
    /// C# refuses these outright, asking for a bug report.
    ScrambledDataTracks,
    Malformed(String),
}

impl std::fmt::Display for CcdError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::NoSections => write!(f, "malformed CCD: no sections"),
            Self::InsufficientSections(n) => {
                write!(f, "malformed CCD: only {n} section(s)")
            }
            Self::BadFirstSection(s) => {
                write!(f, "malformed CCD: first section is [{s}], expected [CloneCD]")
            }
            Self::MissingVersion => {
                write!(f, "malformed CCD: no Version in the [CloneCD] section")
            }
            Self::BadSecondSection(s) => {
                write!(f, "malformed CCD: second section is [{s}], expected [Disc]")
            }
            Self::ScrambledDataTracks => write!(
                f,
                "malformed CCD: DataTracksScrambled=1 is not supported"
            ),
            Self::Malformed(m) => write!(f, "malformed CCD: {m}"),
        }
    }
}

impl std::error::Error for CcdError {}

/// C# `ParseSections` — the INI reader.
///
/// Differences from the C#, all of them cases where it throws:
/// * a value containing `=` is kept whole (bug 4);
/// * a non-numeric value is skipped with a diagnostic rather than aborting the
///   parse (bug 5);
/// * whitespace-only lines are skipped (the C# tests `Length is 0`, so a line
///   of spaces reaches the `=` split and throws).
fn parse_sections(text: &str) -> Result<(Vec<CcdSection>, Vec<CcdDiagnostic>), CcdError> {
    let mut sections: Vec<CcdSection> = Vec::new();
    let mut diags = Vec::new();
    for (n, raw) in text.lines().enumerate() {
        let line = raw.trim();
        if line.is_empty() {
            continue;
        }
        if let Some(inner) = line.strip_prefix('[') {
            let name = inner.trim_end_matches(']').trim().to_ascii_uppercase();
            sections.push(CcdSection { name, values: HashMap::new(), line: n + 1 });
            continue;
        }
        let Some(section) = sections.last_mut() else {
            return Err(CcdError::Malformed("started without a [section]".into()));
        };
        // `splitn(2, '=')` keeps an '=' inside the value.
        let Some((k, v)) = line.split_once('=') else {
            diags.push(CcdDiagnostic {
                line: n + 1,
                message: format!("no '=' in {line:?}"),
                is_error: false,
            });
            continue;
        };
        let key = k.trim().to_ascii_uppercase();
        if key == "FLAGS" {
            continue; // skipped by the C# too
        }
        let v = v.trim();
        let parsed = if let Some(hex) = v
            .strip_prefix("0x")
            .or_else(|| v.strip_prefix("0X"))
        {
            i32::from_str_radix(hex, 16).ok()
        } else {
            v.parse::<i32>().ok()
        };
        match parsed {
            Some(value) => {
                section.values.insert(key, value);
            }
            None => diags.push(CcdDiagnostic {
                line: n + 1,
                message: format!("non-numeric value for {key}: {v:?}"),
                is_error: false,
            }),
        }
    }
    Ok((sections, diags))
}

impl CcdFile {
    /// C# `ParseFrom(Stream)`.
    pub fn parse(text: &str) -> Result<Self, CcdError> {
        let (sections, diagnostics) = parse_sections(text)?;
        let mut me = Self { diagnostics, ..Default::default() };

        // C# `PreParseIntegrityCheck`.
        match sections.len() {
            0 => return Err(CcdError::NoSections),
            n @ 1 => return Err(CcdError::InsufficientSections(n)),
            _ => {}
        }
        if sections[0].name != "CLONECD" {
            return Err(CcdError::BadFirstSection(sections[0].name.clone()));
        }
        me.version = sections[0]
            .values
            .get("VERSION")
            .copied()
            .ok_or(CcdError::MissingVersion)?;
        if sections[1].name != "DISC" {
            return Err(CcdError::BadSecondSection(sections[1].name.clone()));
        }

        let disc = &sections[1];
        // The C# uses the indexer here and throws on a missing key, despite its
        // own comment calling that "conceivable" (bugs 1 and 2). Optional here,
        // and actually checked at the end.
        me.declared_toc_entries = disc.values.get("TOCENTRIES").copied();
        me.declared_sessions = disc.values.get("SESSIONS").copied();
        me.data_tracks_scrambled = disc.get_or("DATATRACKSSCRAMBLED", 0);
        me.cd_text_length = disc.get_or("CDTEXTLENGTH", 0);
        if me.data_tracks_scrambled == 1 {
            return Err(CcdError::ScrambledDataTracks);
        }

        for section in &sections[2..] {
            if section.name.starts_with("SESSION") {
                let Some(number) = section.ordinal() else {
                    me.warn(section.line, "session section has no number");
                    continue;
                };
                if number as usize != me.sessions.len() + 1 {
                    me.warn(
                        section.line,
                        format!(
                            "session {number} out of sequence (expected {})",
                            me.sessions.len() + 1
                        ),
                    );
                }
                me.sessions.push(CcdSession {
                    number,
                    pregap_mode: section.get_or("PREGAPMODE", 0),
                    pregap_subcode: section.get_or("PREGAPSUBC", 0),
                });
            } else if section.name.starts_with("ENTRY") {
                let Some(entry_num) = section.ordinal() else {
                    me.warn(section.line, "entry section has no number");
                    continue;
                };
                let build = || -> Result<CcdTocEntry, String> {
                    Ok(CcdTocEntry {
                        entry_num,
                        session: section.require("SESSION")?,
                        point: section.require("POINT")?,
                        adr: section.require("ADR")?,
                        control: section.require("CONTROL")?,
                        track_no: section.require("TRACKNO")?,
                        a_min: section.require("AMIN")?,
                        a_sec: section.require("ASEC")?,
                        a_frame: section.require("AFRAME")?,
                        alba: section.require("ALBA")?,
                        zero: section.require("ZERO")?,
                        p_min: section.require("PMIN")?,
                        p_sec: section.require("PSEC")?,
                        p_frame: section.require("PFRAME")?,
                        plba: section.require("PLBA")?,
                    })
                };
                match build() {
                    Ok(e) => {
                        // The C# throws here with a message starting "Warning:"
                        // (bug 3). Recorded and carried on.
                        if !e.alba_consistent() {
                            me.warn(
                                section.line,
                                format!(
                                    "ALBA {} disagrees with A-MSF {:02}:{:02}:{:02}",
                                    e.alba, e.a_min, e.a_sec, e.a_frame
                                ),
                            );
                        }
                        if !e.plba_consistent() {
                            me.warn(
                                section.line,
                                format!(
                                    "PLBA {} disagrees with P-MSF {:02}:{:02}:{:02}",
                                    e.plba, e.p_min, e.p_sec, e.p_frame
                                ),
                            );
                        }
                        me.toc_entries.push(e);
                    }
                    Err(m) => return Err(CcdError::Malformed(m)),
                }
            } else if section.name.starts_with("TRACK") {
                let Some(number) = section.ordinal() else {
                    me.warn(section.line, "track section has no number");
                    continue;
                };
                let mut track = CcdTrack { number, ..Default::default() };
                for (k, v) in &section.values {
                    if k == "MODE" {
                        track.mode = *v;
                    } else if k.starts_with("INDEX") {
                        // The C# does `k.Split(' ')[1]` and throws on "INDEX1".
                        match k.split_whitespace().nth(1).and_then(|n| n.parse().ok()) {
                            Some(i) => {
                                track.indexes.insert(i, *v);
                            }
                            None => me.warn(section.line, format!("malformed index key {k:?}")),
                        }
                    }
                }
                me.tracks.push(track);
            }
        }

        // The check the C# reads the values for and then never performs (bug 1).
        if let Some(n) = me.declared_toc_entries {
            if n as usize != me.toc_entries.len() {
                me.warn(
                    disc.line,
                    format!(
                        "[Disc] TOCEntries={n} but {} entries were parsed",
                        me.toc_entries.len()
                    ),
                );
            }
        }
        if let Some(n) = me.declared_sessions {
            if n as usize != me.sessions.len() {
                me.warn(
                    disc.line,
                    format!(
                        "[Disc] Sessions={n} but {} sessions were parsed",
                        me.sessions.len()
                    ),
                );
            }
        }
        Ok(me)
    }

    fn warn(&mut self, line: usize, message: impl Into<String>) {
        self.diagnostics.push(CcdDiagnostic {
            line,
            message: message.into(),
            is_error: false,
        });
    }

    /// C# `TracksByNumber`.
    pub fn track(&self, number: i32) -> Option<&CcdTrack> {
        self.tracks.iter().find(|t| t.number == number)
    }

    /// Sidecar paths the C# derives with `Path.ChangeExtension`.
    pub fn sidecar_paths(ccd_path: &str) -> (String, String) {
        let stem = ccd_path
            .rsplit_once('.')
            .map(|(s, _)| s)
            .unwrap_or(ccd_path);
        (format!("{stem}.img"), format!("{stem}.sub"))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    // A minimal single-track CCD. The TOC values here are constructed to
    // satisfy the ALBA/PLBA relationship the C# asserts; they are NOT taken
    // from a real image, so they verify internal consistency only.
    const CCD: &str = "\
[CloneCD]
Version=3
[Disc]
TocEntries=3
Sessions=1
DataTracksScrambled=0
CDTextLength=0
[Session 1]
PreGapMode=1
PreGapSubC=0
[Entry 0]
Session=1
Point=0xa0
ADR=0x01
Control=0x04
TrackNo=0
AMin=0
ASec=0
AFrame=0
ALBA=-150
Zero=0
PMin=1
PSec=0
PFrame=0
PLBA=-150
[Entry 1]
Session=1
Point=0xa1
ADR=0x01
Control=0x04
TrackNo=0
AMin=0
ASec=0
AFrame=0
ALBA=-150
Zero=0
PMin=1
PSec=0
PFrame=0
PLBA=-150
[Entry 2]
Session=1
Point=0x01
ADR=0x01
Control=0x04
TrackNo=0
AMin=0
ASec=2
AFrame=0
ALBA=0
Zero=0
PMin=0
PSec=2
PFrame=0
PLBA=0
[TRACK 1]
MODE=1
INDEX 1=0
";

    #[test]
    fn parses_a_minimal_ccd() {
        let c = CcdFile::parse(CCD).expect("should parse");
        assert_eq!(c.version, 3);
        assert_eq!(c.sessions.len(), 1);
        assert_eq!(c.toc_entries.len(), 3);
        assert_eq!(c.tracks.len(), 1);
        assert_eq!(c.declared_toc_entries, Some(3));
        assert_eq!(c.declared_sessions, Some(1));
    }

    #[test]
    fn hex_values_are_decoded() {
        let c = CcdFile::parse(CCD).unwrap();
        assert_eq!(c.toc_entries[0].point, 0xA0);
        assert_eq!(c.toc_entries[2].point, 0x01);
        assert_eq!(c.toc_entries[0].control, 0x04);
    }

    #[test]
    fn track_mode_and_indexes_are_read() {
        let c = CcdFile::parse(CCD).unwrap();
        let t = c.track(1).expect("track 1");
        assert_eq!(t.mode, 1);
        assert_eq!(t.indexes.get(&1), Some(&0));
    }

    #[test]
    fn lba_and_msf_agree_on_the_constructed_entries() {
        // Entry 2: A-MSF 00:02:00 is absolute, ALBA 0 is logical -> differ by
        // the 150-sector lead-in.
        let c = CcdFile::parse(CCD).unwrap();
        let e = c.toc_entries[2];
        assert!(e.alba_consistent(), "ALBA {} vs MSF", e.alba);
        assert!(e.plba_consistent());
        assert_eq!(e.absolute_msf().unwrap().to_lba(), 0);
    }

    #[test]
    fn declared_counts_are_actually_checked() {
        // The C# reads TocEntries/Sessions and never compares them (bug 1).
        let bad = CCD.replace("TocEntries=3", "TocEntries=99");
        let c = CcdFile::parse(&bad).unwrap();
        assert!(
            c.diagnostics.iter().any(|d| d.message.contains("TOCEntries=99")),
            "{:?}",
            c.diagnostics
        );
    }

    #[test]
    fn missing_declared_counts_do_not_abort() {
        // The C# uses the indexer and throws KeyNotFoundException (bug 2).
        let bad = CCD.replace("TocEntries=3\n", "").replace("Sessions=1\n", "");
        let c = CcdFile::parse(&bad).expect("must still parse");
        assert_eq!(c.declared_toc_entries, None);
        assert_eq!(c.toc_entries.len(), 3);
    }

    #[test]
    fn inconsistent_lba_is_a_diagnostic_not_a_fatal_error() {
        // The C# throws InvalidOperationException("Warning: ...") here (bug 3).
        let bad = CCD.replace("ALBA=0\nZero=0\nPMin=0\nPSec=2", "ALBA=9999\nZero=0\nPMin=0\nPSec=2");
        let c = CcdFile::parse(&bad).expect("must still load");
        assert!(c.diagnostics.iter().any(|d| d.message.contains("ALBA 9999")));
    }

    #[test]
    fn values_containing_equals_are_kept_whole() {
        // The C# requires Split('=').Length == 2 and throws otherwise (bug 4).
        // Non-numeric, so it becomes a diagnostic rather than a value.
        let s = CCD.replace("CDTextLength=0", "CDTextLength=a=b");
        let c = CcdFile::parse(&s).expect("must not abort");
        assert!(c.diagnostics.iter().any(|d| d.message.contains("non-numeric")));
    }

    #[test]
    fn non_numeric_values_are_skipped_not_fatal() {
        // The C# int.Parse throws (bug 5).
        let s = CCD.replace("CDTextLength=0", "CDTextLength=lots");
        let c = CcdFile::parse(&s).expect("must not abort");
        assert_eq!(c.cd_text_length, 0, "falls back to the default");
    }

    #[test]
    fn whitespace_only_lines_are_skipped() {
        // The C# tests `Length is 0`, so "   " reaches the '=' split and throws.
        let s = CCD.replace("[Disc]", "   \n[Disc]");
        assert!(CcdFile::parse(&s).is_ok());
    }

    #[test]
    fn unnumbered_sections_warn_rather_than_panic() {
        // The C# indexes Split(' ')[1] and throws (bug 6).
        let s = CCD.replace("[TRACK 1]", "[TRACK]");
        let c = CcdFile::parse(&s).expect("must not panic");
        assert!(c.tracks.is_empty());
        assert!(c.diagnostics.iter().any(|d| d.message.contains("no number")));
    }

    #[test]
    fn malformed_index_keys_warn() {
        let s = CCD.replace("INDEX 1=0", "INDEX1=0");
        let c = CcdFile::parse(&s).expect("must not panic");
        assert!(c.track(1).unwrap().indexes.is_empty());
        assert!(c.diagnostics.iter().any(|d| d.message.contains("malformed index")));
    }

    #[test]
    fn integrity_checks_reject_the_wrong_shape() {
        assert_eq!(CcdFile::parse(""), Err(CcdError::NoSections));
        assert_eq!(
            CcdFile::parse("[CloneCD]\nVersion=3\n"),
            Err(CcdError::InsufficientSections(1))
        );
        assert!(matches!(
            CcdFile::parse("[Nope]\nx=1\n[Disc]\ny=2\n"),
            Err(CcdError::BadFirstSection(_))
        ));
        assert!(matches!(
            CcdFile::parse("[CloneCD]\nx=1\n[Disc]\ny=2\n"),
            Err(CcdError::MissingVersion)
        ));
        assert!(matches!(
            CcdFile::parse("[CloneCD]\nVersion=3\n[Nope]\ny=2\n"),
            Err(CcdError::BadSecondSection(_))
        ));
    }

    #[test]
    fn scrambled_data_tracks_are_refused_as_in_the_c_sharp() {
        let s = CCD.replace("DataTracksScrambled=0", "DataTracksScrambled=1");
        assert_eq!(CcdFile::parse(&s), Err(CcdError::ScrambledDataTracks));
    }

    #[test]
    fn a_missing_required_entry_key_is_an_error() {
        let s = CCD.replace("Point=0x01\nADR=0x01\nControl=0x04\nTrackNo=0\nAMin=0\nASec=2", "ADR=0x01\nControl=0x04\nTrackNo=0\nAMin=0\nASec=2");
        assert!(matches!(CcdFile::parse(&s), Err(CcdError::Malformed(_))));
    }

    #[test]
    fn content_before_any_section_is_rejected() {
        assert!(matches!(
            CcdFile::parse("Version=3\n[CloneCD]\n"),
            Err(CcdError::Malformed(_))
        ));
    }

    #[test]
    fn out_of_sequence_sessions_warn() {
        let s = CCD.replace("[Session 1]", "[Session 5]");
        let c = CcdFile::parse(&s).unwrap();
        assert!(c.diagnostics.iter().any(|d| d.message.contains("out of sequence")));
    }

    #[test]
    fn sidecar_paths_swap_the_extension() {
        let (img, sub) = CcdFile::sidecar_paths("game.ccd");
        assert_eq!(img, "game.img");
        assert_eq!(sub, "game.sub");
        let (img, _) = CcdFile::sidecar_paths("/path/to/My Game.ccd");
        assert_eq!(img, "/path/to/My Game.img");
    }

    #[test]
    fn section_names_are_case_insensitive() {
        let s = CCD.replace("[Disc]", "[DISC]").replace("[Session 1]", "[session 1]");
        assert!(CcdFile::parse(&s).is_ok());
    }
}
