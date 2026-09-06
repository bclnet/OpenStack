//! `openstack` — 1:1 port of .NET project `OpenStack`.
//!
//! The integration crate: it depends on `polyio`, `polyfills`, `gfx`, `sfx`, and
//! `vfx`, all of which are ported. Module layout mirrors the C# file layout; see
//! PORT_MAP.tsv and PORTING.md.
//!
//! All 19 files ported. Several are decisions rather than translations — the
//! vendored LZMA SDK, the ASN.1 key parser, and the three `Platform_*` stubs
//! (every member of which throws in the C#). Each explains itself in place.

pub mod algorithms;
pub mod cache;
pub mod client;
pub mod manager;
pub mod platform;
pub mod platform_system;
pub mod platform_test;
pub mod platform_unknown;
pub mod profiler;
pub mod util;
pub mod vendor;

pub mod prelude {
    pub use crate::algorithms::{crc32_digest, murmur_hash2, murmur_hash3};
    pub use crate::manager::{Cell, CellBuilder, CellManager, CellXref, Land, Query};
    pub use crate::platform::{activate, Caps, Os, Platform, UnknownPlatform, EPSILON};
    pub use crate::client::{ClientHost, GlobalTime, Scene, SceneState};
    pub use crate::profiler::{ProfileData, Profiler, ProfilerError};
    pub use crate::util::{decode_path, PathRoots, SettingsDict};
}
