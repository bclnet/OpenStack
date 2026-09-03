// PORT-SOURCE: Platforms/OpenStack.Platform.Mg/Gfx/Mg_Render.cs
// PORT-SHA: d6e26757ebd5e5e7
// PORT-STATUS: done
//
// NOT PORTED — MonoGame has no Rust counterpart.
//
// This crate binds to **MonoGame**, which is a .NET-only game framework. There is no
// Rust library to bind the same calls to, so a "port" would mean rewriting the
// backend against a different engine entirely — a design decision, not a
// translation, and one that should be made against a real target rather than
// implied by a file-by-file mapping.
//
// If this backend is wanted in Rust, the equivalents are:
//   * `bevy` — closest in scope.
//   * `wgpu` + `winit` — for the graphics/windowing subset.
//   The `NameMe/` subtree (Renderer, ScissorStack,
//   SolidColorTextureCache) is generic 2D-batching logic that would
//   transfer, but it is written against `Microsoft.Xna.Framework.Graphics`
//   types throughout.
//
// The abstraction it plugs into is already ported and engine-agnostic:
// implement `openstack_gfx::gfx::Backend` plus the `TextureBuilder` /
// `MaterialBuilder` / `ShaderBuilder` traits, and `openstack::platform::Platform`.
// Nothing above this layer needs to change.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
