// mirrors dotnet folder `System.Numerics` — see PORT_MAP.tsv
//
// The generic `Vector2<T>/Vector3<T>/Vector4<T>` in the C# (80KB combined) have
// four real instantiations in the whole solution, all integer. Those files port
// to `glam` re-exports; see each for the count.
pub mod cry_unused;
pub mod matrix2x2;
pub mod matrix3x3;
pub mod matrix3x3x;
pub mod matrix3x4;
pub mod matrix4x3;
pub mod polyfill;
pub mod vector2;
pub mod vector3;
pub mod vector4;
