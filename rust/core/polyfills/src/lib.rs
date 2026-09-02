//! `openstack-polyfills` — 1:1 port of .NET project `OpenStack.Polyfills`.
//!
//! Module layout mirrors the C# folder/file layout exactly so the two trees
//! can be diffed and updated in parallel. See PORT_MAP.tsv at the workspace root.

pub mod log;
pub mod math_net_numerics;
pub mod poly2_1;
pub mod system;
pub mod system_collections_generic;
pub mod system_drawing;
pub mod system_globalization;
pub mod system_numerics;
pub mod system_security_cryptography;
