#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Numerics;

/// <summary>
/// Represents a curve.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Curve : IEquatable<Curve>, IFormattable {
    /// <summary>
    /// LoopType
    /// </summary>
    public enum LoopType : int { Constant, Cycle, CycleOffset, Oscillate, Linear }

    /// <summary>
    /// Continuity
    /// </summary>
    public enum Continuity : int { Smooth, Step }

    /// <summary>
    /// Loop
    /// </summary>
    public struct Key(float position, float value, float tangentIn, float tangentOut, int continuity) {
        public float Position = position;
        public float Value = value;
        public float TangentIn = tangentIn;
        public float TangentOut = tangentOut;
        public Continuity Continuity = (Continuity)continuity;
    }

    /// <summary>The PreLoop component of the curve.</summary>
    public LoopType PreLoop;

    /// <summary>The PostLoop component of the curve.</summary>
    public LoopType PostLoop;

    /// <summary>The Loops component of the curve.</summary>
    public Key[] Keys;

    /// <summary>Creates a curve whose elements have the specified values.</summary>
    /// <param name="preLoop">The value to assign to the <see cref="System.Numerics.Curve.PreLoop" /> field.</param>
    /// <param name="postLoop">The value to assign to the <see cref="System.Numerics.Curve.PostLoop" /> field.</param>
    /// <param name="keys">The value to assign to the <see cref="System.Numerics.Curve.Keys" /> field.</param>
    public Curve(int preLoop, int postLoop, Key[] keys) {
        PreLoop = (LoopType)preLoop;
        PostLoop = (LoopType)postLoop;
        Keys = keys;
    }

    /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
    /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Numerics.curve2" /> object and their <see cref="System.Numerics.curve2.X" /> and <see cref="System.Numerics.curve2.Y" /> elements are equal.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals([NotNullWhen(true)] object? obj)
        => (obj is Curve other) && Equals(other);

    /// <summary>Returns a value that indicates whether this instance and another curve are equal.</summary>
    /// <param name="other">The other curve.</param>
    /// <returns><see langword="true" /> if the two curves are equal; otherwise, <see langword="false" />.</returns>
    /// <remarks>Two curves are equal if their <see cref="System.Numerics.curve2.X" /> and <see cref="System.Numerics.curve2.Y" /> elements are equal.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Curve other) {
        return PreLoop.Equals(other.PreLoop)
            && PostLoop.Equals(other.PostLoop)
            && Keys.Equals(other.Keys);
    }

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() {
        return HashCode.Combine(PreLoop, PostLoop, Keys);
    }

    /// <summary>Returns the string representation of the current instance using default formatting.</summary>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the curve is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    public override string ToString() {
        return ToString("G", CultureInfo.CurrentCulture);
    }

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the curve is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
    public string ToString(string format) {
        return ToString(format, CultureInfo.CurrentCulture);
    }

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
    /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
    /// <returns>The string representation of the current instance.</returns>
    /// <remarks>This method returns a string in which each element of the curve is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
    public string ToString(string format, IFormatProvider formatProvider) {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{PreLoop}{separator} {PostLoop}{separator} {Keys}>";
    }
}
