//! `openstack-platform-tests` — 1:1 port of .NET project `OpenStack.Platform.Tests`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod assembly;
pub mod gl;
pub mod gl_render;
pub mod gl_renderer;
