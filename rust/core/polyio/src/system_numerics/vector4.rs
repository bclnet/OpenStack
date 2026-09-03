// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Vector4.cs
// PORT-SHA: 82eba87a652bef21
// PORT-STATUS: done
//
// Same story as `vector3.rs`. The only real instantiations of the generic form
// are two `Vector4<int>` returns in `Gfx/OpenStack.Gfx/TextureSequences.cs`
// (`GetCroppedRect` / `GetUncroppedRect`), which are rectangles-as-vectors.

/// C# `System.Numerics.Vector4` (BCL, float).
pub use glam::Vec4;

/// C# `Vector4<int>` — the cropped/uncropped rect returns in `TextureSequences`.
pub use glam::IVec4;
