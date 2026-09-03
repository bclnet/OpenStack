//! `openstack-polyfills` — 1:1 port of .NET project `OpenStack.Polyfills`.
//!
//! Module layout mirrors the C# folder/file layout. See PORT_MAP.tsv and
//! PORTING.md at the workspace root.
//!
//! Much of the C# here polyfills BCL types missing from netstandard2.1, or
//! reaches for a third-party library (MathNet, ArrayPool) that Rust's std or
//! `glam` already covers. Those files port to re-exports or to nothing; each
//! one explains what it collapsed to and why.

pub mod log;
pub mod math_net_numerics;
pub mod poly2_1;
pub mod system;
pub mod system_collections_generic;
pub mod system_drawing;
pub mod system_globalization;
pub mod system_numerics;
pub mod system_security_cryptography;
