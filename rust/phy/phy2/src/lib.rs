//! `openstack-phy2` — 1:1 port of .NET project `OpenStack.Phy2`.
//!
//! # This project does not compile in C#
//!
//! `OpenStack.Phy2` is a mid-migration copy of ACE (Asheron's Call Emulator)
//! server physics. **107 of its 164 files reference 21 namespaces that exist
//! nowhere in the solution** — `ACE.Entity.Enum`, `ACE.Server.Physics.Common`,
//! `ACE.DatLoader.Entity`, and so on — and the `.csproj` has no
//! `PackageReference` or `ProjectReference` supplying any of them.
//!
//! Ported here are files that stand on their own. The rest cannot be ported
//! faithfully: their signatures name types whose definitions are not available,
//! so a port would be inventing them. See PORTING.md for the full accounting
//! and what is needed to unblock it.

pub mod common;
pub mod extensions;
pub mod physics_globals;
pub mod ray;
pub mod sphere;

pub mod prelude {
    pub use crate::common::vector::{is_zero, normalize_check_small};
    pub use crate::extensions::float_extensions::FloatExt;
    pub use crate::extensions::quaternion_extensions::QuatExt;
    pub use crate::physics_globals::{EPSILON, GRAVITY};
    pub use crate::ray::Ray;
    pub use crate::sphere::Sphere;
}
