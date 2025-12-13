#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Drawing;

/// <summary>
/// Represents a BoundingSphere.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct BoundingSphere : IEquatable<BoundingSphere>, IFormattable {
    /// <summary>The Center component of the BoundingShere.</summary>
    public Vector3 Center;

    /// <summary>The Radius component of the BoundingShere.</summary>
    public float Radius;

    /// <summary>Creates a bounding sphere whose elements have the specified values.</summary>
    /// <param name="center">The value to assign to the <see cref="System.Drawing.BoundingSphere.Center" /> field.</param>
    /// <param name="radius">The value to assign to the <see cref="System.Drawing.BoundingSphere.Radius" /> field.</param>
    public BoundingSphere(Vector3 center, float radius) {
        Center = center;
        Radius = radius;
    }

    /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
    /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Drawing.BoundingSphere" /> object and their <see cref="System.Drawing.BoundingSphere.Center" /> and <see cref="System.Drawing.BoundingSphere.Radius" /> elements are equal.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => (obj is BoundingSphere other) && Equals(other);

    /// <summary>Returns a value that indicates whether this instance and another ray are equal.</summary>
    /// <param name="other">The other ray.</param>
    /// <returns><see langword="true" /> if the two rays are equal; otherwise, <see langword="false" />.</returns>
    /// <remarks>Two spheres are equal if their <see cref="System.Drawing.BoundingSphere.Center" /> and <see cref="System.Drawing.BoundingSphere.Radius" /> elements are equal.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(BoundingSphere other) {
        return Center.Equals(other.Center)
            && Radius.Equals(other.Radius);
    }

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>The hash code.</returns>
    public override readonly int GetHashCode() {
        return HashCode.Combine(Center, Radius);
    }

    /// <summary>Returns the string representation of the current instance using default formatting.</summary>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the sphere is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    public override readonly string ToString() {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the sphere is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
    public readonly string ToString(string format) {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
    /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the sphere is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
    public readonly string ToString(string format, IFormatProvider formatProvider) {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{Center}{separator} {Radius}>";
    }
}
