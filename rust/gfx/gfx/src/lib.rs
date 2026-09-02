//! `openstack-gfx` — 1:1 port of .NET project `OpenStack.Gfx`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod gfx;
pub mod gfx_bitmap;
pub mod gfx_render;
pub mod gfx_texture;
pub mod texture_helper;
pub mod texture_sequences;
