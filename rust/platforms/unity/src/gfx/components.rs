// PORT-SOURCE: Platforms/OpenStack.Platform.Unity/Gfx/Components.cs
// PORT-SHA: 260bf455ea7a2776
// PORT-STATUS: done
//
// NOT PORTED — Unity has no Rust counterpart.
//
// This crate binds to **Unity**, which is a .NET-only engine whose scripting layer is C#. There is no
// Rust library to bind the same calls to, so a "port" would mean rewriting the
// backend against a different engine entirely — a design decision, not a
// translation, and one that should be made against a real target rather than
// implied by a file-by-file mapping.
//
// If this backend is wanted in Rust, the equivalents are:
//   * Nothing directly. Rust can build Unity *native plugins* (a C ABI
//     library Unity calls into), but the `MonoBehaviour`/`UnityEngine`
//     code in this crate is exactly the part that must stay C#.
//   * The right split is: keep this crate in C#, and have it call into a
//     Rust `cdylib` built from the ported `openstack-*` crates.
//
// The abstraction it plugs into is already ported and engine-agnostic:
// implement `openstack_gfx::gfx::Backend` plus the `TextureBuilder` /
// `MaterialBuilder` / `ShaderBuilder` traits, and `openstack::platform::Platform`.
// Nothing above this layer needs to change.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
