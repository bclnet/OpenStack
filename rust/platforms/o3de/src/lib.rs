//! `openstack-platform-o3de` — 1:1 port of its .NET project.
//!
//! The platform **registration** layer is translated: id, name, capability
//! flags, and which `GfX.X*` manager slots the backend fills. That half is
//! engine-independent and ports directly.
//!
//! The **render** half is not: see the individual modules for why, and
//! PORTING.md for the per-backend viability assessment.

pub mod platform_o3de;
pub mod slots;
pub mod gfx;
