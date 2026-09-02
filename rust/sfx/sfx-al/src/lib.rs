//! `openstack-sfx-al` — 1:1 port of .NET project `OpenStack.Sfx.Al`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod al;
pub mod al_base;
pub mod alc;
pub mod extensions;
pub mod native;
