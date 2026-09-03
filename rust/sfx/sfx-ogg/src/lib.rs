//! `openstack-sfx-ogg` — 1:1 mapping of .NET project `OpenStack.Sfx.Ogg`.
//!
//! Not ported: the C# is 148 `DllImport`s over native libogg/libvorbis, not a
//! decoder. Use `lewton` or `symphonia` and feed the PCM to
//! `openstack_sfx::AudioManager`. See the module for the full rationale.

pub mod sfx_ogg_vorbis;
