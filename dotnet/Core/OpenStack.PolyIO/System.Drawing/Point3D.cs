#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Drawing;

/// <summary>
/// Represents a point.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Point3D : IEquatable<Point3D>, IFormattable {
    public static Point3D Zero = new();
    
    /// <summary>The X component of the point.</summary>
    public int X;

    /// <summary>The Y component of the point.</summary>
    public int Y;

    /// <summary>The Y component of the point.</summary>
    public int Z;

    /// <summary>Creates a point whose elements have the specified values.</summary>
    /// <param name="x">The value to assign to the <see cref="System.Drawing.Point3D.X" /> field.</param>
    /// <param name="y">The value to assign to the <see cref="System.Drawing.Point3D.Y" /> field.</param>
    /// <param name="z">The value to assign to the <see cref="System.Drawing.Point3D.Z" /> field.</param>
    public Point3D(int x, int y, int z) {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
    /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Drawing.Point3D" /> object and their <see cref="System.Drawing.Point3D.Position" /> and <see cref="System.Drawing.Point3D.Direction" /> elements are equal.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => (obj is Point3D other) && Equals(other);

    /// <summary>Returns a value that indicates whether this instance and another point are equal.</summary>
    /// <param name="other">The other point.</param>
    /// <returns><see langword="true" /> if the two points are equal; otherwise, <see langword="false" />.</returns>
    /// <remarks>Two points are equal if their <see cref="System.Drawing.Point3D.X" /> and <see cref="System.Drawing.Point3D.Y" /> and <see cref="System.Drawing.Point3D.Z" /> elements are equal.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Point3D other) {
        return X.Equals(other.X)
            && Y.Equals(other.Y)
            && Z.Equals(other.Z);
    }

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>The hash code.</returns>
    public override readonly int GetHashCode() {
        return HashCode.Combine(X, Y, Z);
    }

    /// <summary>Returns the string representation of the current instance using default formatting.</summary>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the point is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    public override readonly string ToString() {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the point is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
    public readonly string ToString(string format) {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
    /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the point is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
    public readonly string ToString(string format, IFormatProvider formatProvider) {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X}{separator} {Y}{separator} {Z}>";
    }
}
