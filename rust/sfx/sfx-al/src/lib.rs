//! `openstack-sfx-al` — 1:1 mapping of .NET project `OpenStack.Sfx.Al`.
//!
//! **Nothing here is ported, deliberately.** The C# project is a vendored copy
//! of OpenTK's OpenAL P/Invoke bindings — 4,660 live lines, 134 `DllImport`
//! declarations, no logic. Its only referencing project is `OpenStack.SfxTests`;
//! no shipping code calls it.
//!
//! Rust gets these from a maintained crate (`alto`, `openal-sys`, or `cpal` /
//! `rodio` a level up). Hand-maintaining 134 FFI signatures across two
//! languages is undefined behaviour waiting for a typo.
//!
//! To add audio: implement `openstack_sfx::AudioBuilder` over the crate you
//! pick. That trait is the entire surface the rest of the codebase uses.
//!
//! The module tree below exists so `sync-check.sh` still watches these files
//! and reports if the C# side grows real logic here.

pub mod al;
pub mod al_base;
pub mod alc;
pub mod extensions;
pub mod native;
