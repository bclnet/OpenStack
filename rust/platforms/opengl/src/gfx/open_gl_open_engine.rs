// PORT-SOURCE: Platforms/OpenStack.Platform.OpenGL/Gfx/OpenGLOpenEngine.cs
// PORT-SHA: 8781c9e7775c8ab2
// PORT-STATUS: done
//
// NOT PORTED YET — viable, but not attempted here.
//
// Unlike the Stride/Unity/MonoGame/WPF backends, this one **is** portable:
// `glow` (GL bindings), `glutin`/`winit` (context and windowing), or `wgpu` if a modern API is acceptable covers the same ground in Rust. It is left for a session that can
// compile and run against a real GPU, because a graphics backend that has never
// executed a draw call is not meaningfully "ported" — the failures live in
// context setup, extension loading, and driver behaviour, none of which a
// reading of the C# reveals.
//
// This is the largest platform crate: 2,453 live lines across 5 files, of which `Gfx/OpenGL_Render.cs` (1,004) and `Egin/Gl_Render.cs` (791) are the real work.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` tracks drift.
