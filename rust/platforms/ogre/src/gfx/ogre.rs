// PORT-SOURCE: Platforms/OpenStack.Platform.Ogre/Gfx/Ogre.cs
// PORT-SHA: 82dfdfafcbcaaa5d
// PORT-STATUS: done
//
// NOT PORTED — there is no implementation here to port.
//
// The C# `OpenStack.Platform.Ogre` project is a skeleton: 105 live lines, 6 members throwing `NotImplementedException`, and no Ogre binding of any kind — no package reference, no P/Invoke. It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against Ogre's C++ API via `bindgen`, or `wgpu` directly — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
