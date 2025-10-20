//using MathNet.Numerics.LinearAlgebra;
using System.Globalization;

//:ref https://github.com/microsoft/referencesource/tree/master/System.Numerics/System/Numerics
namespace System.Numerics;

/// <summary>
/// A structure encapsulating a 2x2 matrix.
/// </summary>
public struct Matrix2x2 : IEquatable<Matrix2x2> {
    #region Public Fields

    /// <summary>
    /// Value at row 1, column 1 of the matrix.
    /// </summary>
    public float M11;
    /// <summary>
    /// Value at row 1, column 2 of the matrix.
    /// </summary>
    public float M12;

    /// <summary>
    /// Value at row 2, column 1 of the matrix.
    /// </summary>
    public float M21;
    /// <summary>
    /// Value at row 2, column 2 of the matrix.
    /// </summary>
    public float M22;

    #endregion Public Fields

    #region Added

    /// <summary>
    /// Gets the copy.
    /// </summary>
    /// <returns>copy of the matrix33</returns>
    public Matrix2x2 GetCopy() => new() {
        M11 = M11,
        M12 = M12,
        M21 = M21,
        M22 = M22
    };

    /// <summary>
    /// Gets the transpose.
    /// </summary>
    /// <returns>copy of the matrix33</returns>
    public Matrix3x3 Transpose => new() {
        M11 = M11,
        M12 = M21,
        M21 = M12,
        M22 = M22
    };

    /// <summary>
    /// Multiply the 3x3 matrix by a Vector 3 to get the rotation
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns></returns>
    public Vector2 Mult(Vector2 vector) => new() {
        X = (vector.X * M11) + (vector.Y * M21),
        Y = (vector.X * M12) + (vector.Y * M22)
    };

    #endregion

    static readonly Matrix2x2 _identity = new(
        1f, 0f,
        0f, 1f
    );

    /// <summary>
    /// Returns the multiplicative identity matrix.
    /// </summary>
    public static Matrix2x2 Identity => _identity;

    /// <summary>
    /// Returns whether the matrix is the identity matrix.
    /// </summary>
    public bool IsIdentity
        => M11 == 1f && M22 == 1f && // Check diagonal element first for early out.
        M12 == 0f &&
        M21 == 0f;

    /// <summary>
    /// Constructs a Matrix3x3 from the given components.
    /// </summary>
    public Matrix2x2(float m11, float m12,
                     float m21, float m22) {
        M11 = m11;
        M12 = m12;
        M21 = m21;
        M22 = m22;
    }

    /// <summary>
    /// Returns a new matrix with the negated elements of the given matrix.
    /// </summary>
    /// <param name="value">The source matrix.</param>
    /// <returns>The negated matrix.</returns>
    public static Matrix2x2 Negate(Matrix2x2 value) {
        Matrix2x2 result;

        result.M11 = -value.M11;
        result.M12 = -value.M12;
        result.M21 = -value.M21;
        result.M22 = -value.M22;

        return result;
    }

    /// <summary>
    /// Adds two matrices together.
    /// </summary>
    /// <param name="value1">The first source matrix.</param>
    /// <param name="value2">The second source matrix.</param>
    /// <returns>The resulting matrix.</returns>
    public static Matrix2x2 Add(Matrix2x2 value1, Matrix2x2 value2) {
        Matrix2x2 result;

        result.M11 = value1.M11 + value2.M11;
        result.M12 = value1.M12 + value2.M12;
        result.M21 = value1.M21 + value2.M21;
        result.M22 = value1.M22 + value2.M22;

        return result;
    }

    /// <summary>
    /// Subtracts the second matrix from the first.
    /// </summary>
    /// <param name="value1">The first source matrix.</param>
    /// <param name="value2">The second source matrix.</param>
    /// <returns>The result of the subtraction.</returns>
    public static Matrix2x2 Subtract(Matrix2x2 value1, Matrix2x2 value2) {
        Matrix2x2 result;

        result.M11 = value1.M11 - value2.M11;
        result.M12 = value1.M12 - value2.M12;
        result.M21 = value1.M21 - value2.M21;
        result.M22 = value1.M22 - value2.M22;

        return result;
    }

    /// <summary>
    /// Multiplies a matrix by another matrix.
    /// </summary>
    /// <param name="value1">The first source matrix.</param>
    /// <param name="value2">The second source matrix.</param>
    /// <returns>The result of the multiplication.</returns>
    public static Matrix2x2 Multiply(Matrix2x2 value1, Matrix2x2 value2) {
        Matrix2x2 result;

        // First row
        result.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21;
        result.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22;

        // Second row
        result.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21;
        result.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22;

        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a scalar value.
    /// </summary>
    /// <param name="value1">The source matrix.</param>
    /// <param name="value2">The scaling factor.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix2x2 Multiply(Matrix2x2 value1, float value2) {
        Matrix2x2 result;

        result.M11 = value1.M11 * value2;
        result.M12 = value1.M12 * value2;
        result.M21 = value1.M21 * value2;
        result.M22 = value1.M22 * value2;

        return result;
    }

    /// <summary>
    /// Returns a new matrix with the negated elements of the given matrix.
    /// </summary>
    /// <param name="value">The source matrix.</param>
    /// <returns>The negated matrix.</returns>
    public static Matrix2x2 operator -(Matrix2x2 value) {
        Matrix2x2 m;

        m.M11 = -value.M11;
        m.M12 = -value.M12;
        m.M21 = -value.M21;
        m.M22 = -value.M22;

        return m;
    }

    /// <summary>
    /// Adds two matrices together.
    /// </summary>
    /// <param name="value1">The first source matrix.</param>
    /// <param name="value2">The second source matrix.</param>
    /// <returns>The resulting matrix.</returns>
    public static Matrix2x2 operator +(Matrix2x2 value1, Matrix2x2 value2) {
        Matrix2x2 m;

        m.M11 = value1.M11 + value2.M11;
        m.M12 = value1.M12 + value2.M12;
        m.M21 = value1.M21 + value2.M21;
        m.M22 = value1.M22 + value2.M22;

        return m;
    }

    /// <summary>
    /// Subtracts the second matrix from the first.
    /// </summary>
    /// <param name="value1">The first source matrix.</param>
    /// <param name="value2">The second source matrix.</param>
    /// <returns>The result of the subtraction.</returns>
    public static Matrix2x2 operator -(Matrix2x2 value1, Matrix2x2 value2) {
        Matrix2x2 m;

        m.M11 = value1.M11 - value2.M11;
        m.M12 = value1.M12 - value2.M12;
        m.M21 = value1.M21 - value2.M21;
        m.M22 = value1.M22 - value2.M22;

        return m;
    }

    /// <summary>
    /// Multiplies a matrix by another matrix.
    /// </summary>
    /// <param name="value1">The first source matrix.</param>
    /// <param name="value2">The second source matrix.</param>
    /// <returns>The result of the multiplication.</returns>
    public static Matrix2x2 operator *(Matrix2x2 value1, Matrix2x2 value2) {
        Matrix2x2 m;

        // First row
        m.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21;
        m.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22;

        // Second row
        m.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21;
        m.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22;

        return m;
    }

    /// <summary>
    /// Multiplies a matrix by a scalar value.
    /// </summary>
    /// <param name="value1">The source matrix.</param>
    /// <param name="value2">The scaling factor.</param>
    /// <returns>The scaled matrix.</returns>
    public static Matrix2x2 operator *(Matrix2x2 value1, float value2) {
        Matrix2x2 m;

        m.M11 = value1.M11 * value2;
        m.M12 = value1.M12 * value2;
        m.M21 = value1.M21 * value2;
        m.M22 = value1.M22 * value2;

        return m;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given two matrices are equal.
    /// </summary>
    /// <param name="value1">The first matrix to compare.</param>
    /// <param name="value2">The second matrix to compare.</param>
    /// <returns>True if the given matrices are equal; False otherwise.</returns>
    public static bool operator ==(Matrix2x2 value1, Matrix2x2 value2)
        => value1.M11 == value2.M11 && value1.M22 == value2.M22 && // Check diagonal element first for early out.
        value1.M12 == value2.M12 &&
        value1.M21 == value2.M21;

    /// <summary>
    /// Returns a boolean indicating whether the given two matrices are not equal.
    /// </summary>
    /// <param name="value1">The first matrix to compare.</param>
    /// <param name="value2">The second matrix to compare.</param>
    /// <returns>True if the given matrices are not equal; False if they are equal.</returns>
    public static bool operator !=(Matrix2x2 value1, Matrix2x2 value2)
        => value1.M11 != value2.M11 || value1.M12 != value2.M12 ||
        value1.M21 != value2.M21 || value1.M22 != value2.M22;

    /// <summary>
    /// Returns a boolean indicating whether this matrix instance is equal to the other given matrix.
    /// </summary>
    /// <param name="other">The matrix to compare this instance to.</param>
    /// <returns>True if the matrices are equal; False otherwise.</returns>
    public bool Equals(Matrix2x2 other)
        => M11 == other.M11 && M22 == other.M22 && // Check diagonal element first for early out.
        M12 == other.M12 &&
        M21 == other.M21;

    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
    public override bool Equals(object obj)
        => obj is Matrix2x2 x && Equals(x);

    /// <summary>
    /// Returns a String representing this matrix instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString() {
        var ci = CultureInfo.CurrentCulture;
        return string.Format(ci, "{{ {{M11:{0} M12:{1}}} {{M21:{2} M22:{3}}} }}",
        M11.ToString(ci), M12.ToString(ci),
        M21.ToString(ci), M22.ToString(ci));
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
        => M11.GetHashCode() + M12.GetHashCode() +
        M21.GetHashCode() + M22.GetHashCode();
}