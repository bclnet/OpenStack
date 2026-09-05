//:ref https://referencesource.microsoft.com/#System.Numerics/System/Numerics/Matrix4x4.cs,48ce53b7e55d0436
namespace System.Numerics;

/// <summary>
/// A structure encapsulating a 4x3 matrix.
/// </summary>
public struct Matrix4x3(float m11, float m12, float m13,
         float m21, float m22, float m23,
         float m31, float m32, float m33,
         float m41, float m42, float m43) : IEquatable<Matrix4x3> {
    public float M11 = m11;
    public float M12 = m12;
    public float M13 = m13;
    public float M21 = m21;
    public float M22 = m22;
    public float M23 = m23;
    public float M31 = m31;
    public float M32 = m32;
    public float M33 = m33;
    public float M41 = m41;
    public float M42 = m42;
    public float M43 = m43;

    /// <summary>
    /// Returns a boolean indicating whether this matrix instance is equal to the other given matrix.
    /// </summary>
    /// <param name="other">The matrix to compare this instance to.</param>
    /// <returns>True if the matrices are equal; False otherwise.</returns>
    public bool Equals(Matrix4x3 other)
        => M11 == other.M11 && M22 == other.M22 && M33 == other.M33 && M43 == other.M43 && // Check diagonal element first for early out.
        M12 == other.M12 && M13 == other.M13 &&
        M21 == other.M21 && M23 == other.M23 &&
        M31 == other.M31 && M32 == other.M32 &&
        M41 == other.M41 && M42 == other.M42;
}
