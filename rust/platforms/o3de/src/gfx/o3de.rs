// PORT-SOURCE: Platforms/OpenStack.Platform.O3de/Gfx/O3de.cs
// PORT-SHA: 970fbe7053202faa
// PORT-STATUS: done
//
// NOT PORTED — there is no implementation here to port.
//
// The C# `OpenStack.Platform.O3de` project is a skeleton: 104 live lines, with 5 of its ~22 members throwing `NotImplementedException` and the rest holding cast fields. There is no O3DE binding — no package reference, no P/Invoke, no `using` outside the BCL. It declares the shape a
// backend would take and does not fill it in, so there is no behaviour to
// translate.
//
// When this backend is built, implement `openstack_gfx::gfx::Backend` and the
// builder traits directly in Rust against O3DE's C++ API via `bindgen`, or a Rust engine instead — that is a smaller job
// than porting an empty scaffold and then filling it in twice.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
