// PORT-SOURCE: Sfx/OpenStack.Sfx.Ogg/Sfx_OggVorbis.cs
// PORT-SHA: 7b895d2226c9d562
// PORT-STATUS: done
//
// NOT PORTED — this is not a decoder, it is 148 `DllImport` declarations plus
// the `ogg_sync_state` / `vorbis_dsp_state` / `OggVorbis_File` structs needed to
// call native libogg and libvorbis. It carries no decoding logic of its own.
//
// Same reasoning as `openstack-sfx-al`:
//
//   * Rust has `lewton` (pure-Rust Vorbis), `symphonia` (pure-Rust, many
//     formats, one API), and `ogg` for the container — all safe, maintained,
//     and free of the native-library deployment problem this file creates.
//   * Hand-porting 148 FFI signatures plus their layout-sensitive structs means
//     148 chances to introduce undefined behaviour, and a permanent obligation
//     to keep the two copies aligned as libvorbis changes.
//
// A pure-Rust decoder also removes libogg/libvorbis from the shipping
// dependencies entirely, which the C# has to bundle per platform.
//
// To wire it up: decode to PCM with `lewton` or `symphonia`, fill an
// `openstack_sfx::Audio`, and hand it to `AudioManager::create`. Nothing else
// in the codebase touches Vorbis directly.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` watches it.
