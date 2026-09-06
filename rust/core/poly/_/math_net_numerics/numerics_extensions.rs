// PORT-SOURCE: Core/OpenStack.Polyfills/MathNet.Numerics/NumericsExtensions.cs
// PORT-SHA: 350576d153a6da63
// PORT-STATUS: done
//
// Conversions between the C# matrix/vector types and MathNet.Numerics'
// heap-allocated `Matrix<float>` / `Vector<float>`, so MathNet could supply
// `Inverse`, `Conjugate`, and friends.
//
// NOT PORTED, and nothing is lost. This file exists purely to reach a linear
// algebra library; `glam` provides those operations directly on `Mat3`/`Mat4`
// with no allocation and no conversion, and `openstack-polyio`'s `Matrix3x3`
// already implements all five call sites in closed form.
//
// Dropping this removes a dependency and a per-call heap allocation from every
// 3x3 inverse in the codebase.
//
// Kept as a file so the 1:1 mapping holds and `sync-check.sh` notices if the C#
// grows a routine that is not just a conversion.
