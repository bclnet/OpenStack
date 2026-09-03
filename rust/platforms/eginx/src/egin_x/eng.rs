// PORT-SOURCE: Platforms/OpenStack.Platform.EginX/EginX/Eng.cs
// PORT-SHA: 08d1dec4bf093fff
// PORT-STATUS: done
//
// NOT PORTED — there is no implementation here to port.
//
// The C# `OpenStack.Platform.EginX` project is a skeleton: 260 live lines of scaffolding for the in-house 'Egin' renderer, with no graphics API behind it — `Eng.cs` is a class skeleton and the `Gfx/` files are cast-and-forward shims. It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against `wgpu`, once `openstack-gfx-egin`'s renderer half has a target — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
