// PORT-SOURCE: Platforms/OpenStack.Platform.Stride/Gfx/Stride_Render.cs
// PORT-SHA: 4e1004ba6389b5c7
// PORT-STATUS: done
//
// NOT PORTED — Stride has no Rust counterpart.
//
// This crate binds to **Stride**, which is a .NET-only game engine. There is no
// Rust library to bind the same calls to, so a "port" would mean rewriting the
// backend against a different engine entirely — a design decision, not a
// translation, and one that should be made against a real target rather than
// implied by a file-by-file mapping.
//
// If this backend is wanted in Rust, the equivalents are:
//   * `bevy` — a full ECS engine, the closest match in scope.
//   * `wgpu` + `winit` — if only rendering and windowing are wanted.
//
// The abstraction it plugs into is already ported and engine-agnostic:
// implement `openstack_gfx::gfx::Backend` plus the `TextureBuilder` /
// `MaterialBuilder` / `ShaderBuilder` traits, and `openstack::platform::Platform`.
// Nothing above this layer needs to change.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
