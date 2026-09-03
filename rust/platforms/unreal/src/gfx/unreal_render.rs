// PORT-SOURCE: Platforms/OpenStack.Platform.Unreal/Gfx/Unreal_Render.cs
// PORT-SHA: 82578114130e7b59
// PORT-STATUS: done
//
// NOT PORTED — there is no implementation here to port.
//
// The C# `OpenStack.Platform.Unreal` project is a skeleton: 115 live lines and **19** `NotImplementedException` throws across ~35 members — nearly every member. No Unreal binding exists; Unreal's API is C++ and this project never reaches it. It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against Unreal's C++ API (which in practice means writing the plugin in C++) — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
