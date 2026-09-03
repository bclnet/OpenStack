// PORT-SOURCE: Core/OpenStack/Platform_Unknown/Platform_Unknown.cs
// PORT-SHA: ca6f91c02171fabb
// PORT-STATUS: done
//
// The fallback platform, selected whenever no real backend is active — and it
// is `PlatformX.Current`'s initial value, so it is what runs before anything
// registers.
//
// C#-SIDE PROBLEM, and this is the concrete form of the `GfxFactory` default
// noted in `platform.rs`:
//
//     GfxFactory = () => [null, null, null, null, null, null];
//     SfxFactory = () => [null];
//
// It hands back arrays of nulls. `CellBuilder`'s constructor then does
// `(IOpenGfxApi<Object, Material>)gfx[GfX.XApi]` — casting null succeeds — and
// the first actual call NPEs, far from the cause. A platform that cannot render
// should say so at activation, not hand out null-filled arrays that fail later
// at an unrelated call site.
//
// `UnknownClientHost.Dispose()` also `throw new NotImplementedException()`,
// so the type cannot be used in a `using` block at all — the same defect as
// `DirectBitmap.Dispose` in `gfx`.
//
// The Rust `UnknownPlatform` lives in `platform.rs` (it is the module's default)
// and simply provides no managers, which is a state callers can test for rather
// than crash on.

pub use crate::platform::UnknownPlatform;

// NOT PORTED: `UnknownClientHost`. Both members throw; see `client.rs` for the
// `ClientHost` trait a real host would implement.
