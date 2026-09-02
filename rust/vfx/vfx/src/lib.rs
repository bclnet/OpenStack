//! `openstack-vfx` — 1:1 port of .NET project `OpenStack.Vfx`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod disc;
pub mod ext_services;
pub mod n64;
pub mod util;
pub mod vfx;
pub mod vfx_network;
pub mod x3ds;
