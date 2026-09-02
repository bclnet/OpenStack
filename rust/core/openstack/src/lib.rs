//! `openstack` — 1:1 port of .NET project `OpenStack`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

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
