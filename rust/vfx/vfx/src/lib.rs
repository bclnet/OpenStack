//! `openstack-vfx` — 1:1 port of .NET project `OpenStack.Vfx`.
//!
//! Module layout mirrors the C# file layout. See PORT_MAP.tsv and PORTING.md.
//!
//! Ported: the `FileSystem` abstraction and its directory/virtual/aggregate
//! implementations (`vfx`), the endianness and stream helpers (`util`), and N64
//! ROM header parsing (`n64`).
//!
//! Outstanding: `disc` (4,145 live C# lines) and `x3ds` (2,658) are real binary
//! container readers — CD/DVD images and 3DS cartridges. Neither is started.
//! `vfx_network` and `ext_services` are decisions rather than ports; each module
//! explains what to use instead.

pub mod disc;
pub mod disc_addressing;
pub mod disc_ccd;
pub mod disc_cdi;
pub mod disc_cue;
pub mod disc_mds;
pub mod disc_nrg;
pub mod disc_sector;
pub mod disc_synth;
pub mod ext_services;
pub mod n64;
pub mod util;
pub mod vfx;
pub mod vfx_network;
pub mod x3ds;

pub mod prelude {
    pub use crate::disc_addressing::{Bcd2, Msf, FRAMES_PER_SECOND, LEAD_IN_SECTORS};
    pub use crate::disc_ccd::{CcdFile, CcdSession, CcdTocEntry, CcdTrack};
    pub use crate::disc_cdi::{CdiFile, CdiSession, CdiTrack, CdiTrackHeader};
    pub use crate::disc_mds::{MdsFile, MdsHeader, MdsSession, MdsTrack};
    pub use crate::disc_nrg::{NrgCue, NrgFile, NrgTaoTrack, NrgTrackIndex, NrgVersion};
    pub use crate::disc_sector::{Blob, DiscToc, MemoryBlob, SbiFile, SbiPatch, SectorReaderPolicy, TocItem};
    pub use crate::disc_synth::{ecm, offsets, SessionFormat, SynthPart};
    pub use crate::disc_cue::{CueCommand, CueFile, CueFileType, CueTrack, CueTrackFlags, CueTrackType};
    pub use crate::n64::{ImageType, N64Rom, RomHeader, SystemType};
    pub use crate::util::{align, copy_file, endianness, from_hex_string, pad_file, to_hex_string};
    pub use crate::vfx::{
        create_matcher, AggregateFileSystem, DirectoryFileSystem, FileInfo, FileSystem,
        VirtualFileSystem,
    };
}
