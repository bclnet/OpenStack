//! `openstack-gfx-other` — 1:1 mapping of .NET project `OpenStack.Gfx.Other`.
//!
//! 8 of its 9 files live in an `Unused/` folder wrapped in `#if false` — not
//! compiled, no references. The one live file, `HalfPrecConverter`, is the
//! third binary16 implementation in the solution and maps to `half::f16`; see
//! that module for why that matters.

pub mod algorithms;
pub mod unused;
