// PORT-SOURCE: Core/OpenStack.PolyIO/System.Numerics/Vector2.cs
// PORT-SHA: f8eed1b6a56b87f2
// PORT-STATUS: done
//
// Same story as `vector3.rs`: 24KB of generic `Vector2<T>` with exactly one
// real instantiation in the solution — `Vector2<int>` in
// `Gfx/OpenStack.Gfx.Egin/Egin_Render.cs` (`WindowSize`). Ports to glam.

/// C# `System.Numerics.Vector2` (BCL, float).
pub use glam::Vec2;

/// C# `Vector2<int>` — used for `Egin_Render.WindowSize`.
pub use glam::IVec2;
