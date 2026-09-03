// PORT-SOURCE: Vfx/OpenStack.Vfx/ExtServices/FFmpegService.cs
// PORT-SHA: 8b1305fa00ce261a
// PORT-STATUS: done
//
// Locates an FFmpeg binary on disk and shells out to it.
//
// NOT PORTED yet: it is process orchestration around an external executable,
// which `std::process::Command` covers directly, but the discovery logic is
// tied to .NET's `AppContext.BaseDirectory` and Windows path conventions. It
// wants rewriting against the Rust binary's own layout rather than transcribing.
//
// Nothing in the ported tree calls it. When video decoding is needed, weigh
// shelling out against `ffmpeg-next` (bindings to libav*), which avoids the
// "is FFmpeg installed and on PATH" deployment question entirely.
