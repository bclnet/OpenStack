// PORT-SOURCE: Platforms/OpenStack.Platform.Vk/Vk.cs
// PORT-SHA: 3d03ec17fddb8ae7
// PORT-STATUS: done
//
// NOT PORTED — there is no implementation here to port.
//
// The C# `OpenStack.Platform.Vk` project is a skeleton: **3 live lines** — a namespace declaration and an empty class. The `OpenTK.NetStandard` package reference is never used. It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against `ash` (thin Vulkan bindings) or `vulkano` (safe wrapper) — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
