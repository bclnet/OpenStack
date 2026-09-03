// PORT-SOURCE: Platforms/OpenStack.Platform.Godot/Gfx/Godot_Render.cs
// PORT-SHA: c0c0677f388a3203
// PORT-STATUS: done
//
// NOT PORTED — there is no implementation here to port.
//
// The C# `OpenStack.Platform.Godot` project is a skeleton: 468 live lines that reference Godot types (`XShader`) without any Godot package reference — so it does not compile as given, the same defect as `phy2`. It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against `godot` (gdext), which is a first-class Rust binding for Godot 4 — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
