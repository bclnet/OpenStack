// PORT-SOURCE: Platforms/OpenStack.Platform.Sdl/Gfx/Sdl.cs
// PORT-SHA: a34ed15ecd261563
// PORT-STATUS: done
//
// NOT PORTED YET — viable, but not attempted here.
//
// Unlike the Stride/Unity/MonoGame/WPF backends, this one **is** portable:
// `sdl2` or `sdl3-sys` covers the same ground in Rust. It is left for a session that can
// compile and run against a real GPU, because a graphics backend that has never
// executed a draw call is not meaningfully "ported" — the failures live in
// context setup, extension loading, and driver behaviour, none of which a
// reading of the C# reveals.
//
// Small: 107 live lines across 3 files, mostly window and event plumbing.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
