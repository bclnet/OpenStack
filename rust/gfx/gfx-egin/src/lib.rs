//! `openstack-gfx-egin` — 1:1 port of .NET project `OpenStack.Gfx.Egin`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod egin;
pub mod egin_animate;
pub mod egin_particle;
pub mod egin_render;
