// PORT-SOURCE: Platforms/OpenStack.Wpf.Control/OpenTK/Wpf/GLWpfControlRenderer.cs
// PORT-SHA: aa641faec0ad08db
// PORT-STATUS: done
//
// NOT PORTED — WPF has no Rust counterpart.
//
// This crate binds to **WPF**, which is a .NET-only UI framework (Windows-only, and not available outside .NET). There is no
// Rust library to bind the same calls to, so a "port" would mean rewriting the
// backend against a different engine entirely — a design decision, not a
// translation, and one that should be made against a real target rather than
// implied by a file-by-file mapping.
//
// If this backend is wanted in Rust, the equivalents are:
//   * `egui` — immediate-mode, easiest to embed next to a GL context.
//   * `iced` or `slint` — retained-mode, closer to WPF's model.
//   The `OpenTK/` subtree here is a vendored copy of OpenTK's WPF
//   interop (GLWpfControl, DXInterop, GLControl/Native) — ~2,700 lines
//   of Win32 and D3D-GL sharing glue that exists purely to put a GL
//   surface inside a WPF window. None of it has a reason to exist in a
//   Rust application, which would use `winit` and own its own surface.
//
// The abstraction it plugs into is already ported and engine-agnostic:
// implement `openstack_gfx::gfx::Backend` plus the `TextureBuilder` /
// `MaterialBuilder` / `ShaderBuilder` traits, and `openstack::platform::Platform`.
// Nothing above this layer needs to change.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
