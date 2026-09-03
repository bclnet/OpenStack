// PORT-SOURCE: Vfx/OpenStack.Vfx/ExtServices/LibChd.cs
// PORT-SHA: ec29e1c226382277
// PORT-STATUS: done
//
// P/Invoke bindings to libchdr, for MAME CHD compressed disc images.
//
// NOT PORTED — same call as `openstack-sfx-al` and `openstack-sfx-ogg`: this is
// FFI declarations with no logic. Rust has `chd-rs` (pure Rust) and
// `chd-sys`/`libchdr` bindings, either of which is maintained and tested against
// real images.
//
// This one is consumed by `Disc.cs`, so it is not dead — but it should be wired
// to a crate when `Disc.cs` is ported, not hand-translated. Doing so also drops
// the native libchdr from the shipping dependencies.
