// PORT-SOURCE: Platforms/OpenStack.Wpf.Stride/StrideControl.cs
// PORT-SHA: f6927e53ea1a2fc4
// PORT-STATUS: done
//
// NOT PORTED — Stride (embedded in WPF) has no Rust counterpart.
//
// This crate binds to **Stride (embedded in WPF)**, which is a .NET-only game engine hosted in a .NET-only UI framework. There is no
// Rust library to bind the same calls to, so a "port" would mean rewriting the
// backend against a different engine entirely — a design decision, not a
// translation, and one that should be made against a real target rather than
// implied by a file-by-file mapping.
//
// If this backend is wanted in Rust, the equivalents are:
//   * `bevy` or `wgpu` for the engine half.
//   * `egui`, `iced`, or `tauri` for the UI half.
//   Note this crate needs *both*, which is why it is the least portable
//   thing in the solution.
//
// The abstraction it plugs into is already ported and engine-agnostic:
// implement `openstack_gfx::gfx::Backend` plus the `TextureBuilder` /
// `MaterialBuilder` / `ShaderBuilder` traits, and `openstack::platform::Platform`.
// Nothing above this layer needs to change.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
