//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;

//namespace System.Drawing;

///// <summary>Represents a rectangle with four single-precision floating-point values.</summary>
//public partial struct Rectangle : IEquatable<Rectangle>, IFormattable  {
//    /// <summary>The X component of the rectangle.</summary>
//    public int X;

//    /// <summary>The Y component of the rectangle.</summary>
//    public int Y;

//    /// <summary>The Width component of the rectangle.</summary>
//    public int Width;

//    /// <summary>The Height component of the rectangle.</summary>
//    public int Height;

//    internal const int Count = 4;

//    public static readonly Dictionary<char, Func<int, int, int>> Ops = [];

//    /// <summary>Creates a new <see cref="System.Drawing.Rectangle" /> object whose four elements have the same value.</summary>
//    /// <param name="value">The value to assign to all four elements.</param>
//    public Rectangle(int value) : this(value, value, value, value) { }

//    /// <summary>Creates a new <see cref="System.Drawing.Rectangle" /> object from the specified <see cref="System.Drawing.Rectangle" /> object and a Width and a Height component.</summary>
//    /// <param name="value">The rectangle to use for the X and Y components.</param>
//    /// <param name="width">The Width component.</param>
//    /// <param name="height">The Height component.</param>
//    public Rectangle(Vector2<int> value, int width, int height) : this(value.X, value.Y, width, height) { }

//    /// <summary>Creates a rectangle whose elements have the specified values.</summary>
//    /// <param name="x">The value to assign to the <see cref="System.Drawing.Rectangle.X" /> field.</param>
//    /// <param name="y">The value to assign to the <see cref="System.Drawing.Rectangle.Y" /> field.</param>
//    /// <param name="width">The value to assign to the <see cref="System.Drawing.Rectangle.Width" /> field.</param>
//    /// <param name="height">The value to assign to the <see cref="System.Drawing.Rectangle.Height" /> field.</param>
//    public Rectangle(int x, int y, int width, int height) {
//        X = x;
//        Y = y;
//        Width = width;
//        Height = height;
//    }

//    /// <summary>Constructs a rectangle from the given <see cref="ReadOnlySpan{Single}" />. The span must contain at least 4 elements.</summary>
//    /// <param name="values">The span of elements to assign to the rectangle.</param>
//    public Rectangle(ReadOnlySpan<int> values) {
//        if (values.Length < 4) throw new ArgumentOutOfRangeException(nameof(values));

//        this = Unsafe.ReadUnaligned<Rectangle>(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(values)));
//    }

//    /// <summary>Gets a rectangle whose 4 elements are equal to zero.</summary>
//    /// <value>A rectangle whose four elements are equal to zero (that is, it returns the rectangle <c>(0,0,0,0)</c>.</value>
//    public static Rectangle Zero {
//        get => default;
//    }

//    /// <summary>Gets or sets the element at the specified index.</summary>
//    /// <param name="index">The index of the element to get or set.</param>
//    /// <returns>The the element at <paramref name="index" />.</returns>
//    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
//    public int this[int index] {
//        get => GetElement(this, index);
//        set => this = WithElement(this, index, value);
//    }

//    /// <summary>Gets the element at the specified index.</summary>
//    /// <param name="rectangle">The rectangle of the element to get.</param>
//    /// <param name="index">The index of the element to get.</param>
//    /// <returns>The value of the element at <paramref name="index" />.</returns>
//    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
//    internal static int GetElement(Rectangle rectangle, int index) {
//        if ((uint)index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
//        return GetElementUnsafe(ref rectangle, index);
//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    static int GetElementUnsafe(ref Rectangle rectangle, int index) {
//        Debug.Assert(index >= 0 && index < Count);
//        return Unsafe.Add(ref Unsafe.As<Rectangle, int>(ref rectangle), index);
//    }

//    /// <summary>Sets the element at the specified index.</summary>
//    /// <param name="rectangle">The rectangle of the element to get.</param>
//    /// <param name="index">The index of the element to set.</param>
//    /// <param name="value">The value of the element to set.</param>
//    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
//    internal static Rectangle WithElement(Rectangle rectangle, int index, int value) {
//        if ((uint)index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

//        Rectangle result = rectangle;
//        SetElementUnsafe(ref result, index, value);
//        return result;
//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    internal static void SetElementUnsafe(ref Rectangle rectangle, int index, int value) {
//        Debug.Assert(index >= 0 && index < Count);
//        Unsafe.Add(ref Unsafe.As<Rectangle, int>(ref rectangle), index) = value;
//    }

//    /// <summary>Adds two rectangles together.</summary>
//    /// <param name="left">The first rectangle to add.</param>
//    /// <param name="right">The second rectangle to add.</param>
//    /// <returns>The summed rectangle.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Addition" /> method defines the addition operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator +(Rectangle left, Rectangle right) {
//        return new Rectangle(
//            Ops['+'](left.X, right.X),
//            Ops['+'](left.Y, right.Y),
//            Ops['+'](left.Width, right.Width),
//            Ops['+'](left.Width, right.Height)
//        );
//    }

//    /// <summary>Divides the first rectangle by the second.</summary>
//    /// <param name="left">The first rectangle.</param>
//    /// <param name="right">The second rectangle.</param>
//    /// <returns>The rectangle that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Division" /> method defines the division operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator /(Rectangle left, Rectangle right) {
//        return new Rectangle(
//            Ops['/'](left.X, right.X),
//            Ops['/'](left.Y, right.Y),
//            Ops['/'](left.Width, right.Width),
//            Ops['/'](left.Width, right.Height)
//        );
//    }

//    /// <summary>Divides the specified rectangle by a specified scalar value.</summary>
//    /// <param name="value1">The rectangle.</param>
//    /// <param name="value2">The scalar value.</param>
//    /// <returns>The result of the division.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Division" /> method defines the division operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator /(Rectangle value1, int value2) {
//        return value1 / new Rectangle(value2);
//    }

//    /// <summary>Returns a value that indicates whether each pair of elements in two specified rectangles is equal.</summary>
//    /// <param name="left">The first rectangle to compare.</param>
//    /// <param name="right">The second rectangle to compare.</param>
//    /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
//    /// <remarks>Two <see cref="System.Drawing.Rectangle" /> objects are equal if each element in <paramref name="left" /> is equal to the corresponding element in <paramref name="right" />.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static bool operator ==(Rectangle left, Rectangle right) {
//        return (left.X.Equals(right.X))
//            && (left.Y.Equals(right.Y))
//            && (left.Width.Equals(right.Width))
//            && (left.Height.Equals(right.Height));
//    }

//    /// <summary>Returns a value that indicates whether two specified rectangles are not equal.</summary>
//    /// <param name="left">The first rectangle to compare.</param>
//    /// <param name="right">The second rectangle to compare.</param>
//    /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static bool operator !=(Rectangle left, Rectangle right) {
//        return !(left == right);
//    }

//    /// <summary>Returns a new rectangle whose values are the product of each pair of elements in two specified rectangles.</summary>
//    /// <param name="left">The first rectangle.</param>
//    /// <param name="right">The second rectangle.</param>
//    /// <returns>The element-wise product rectangle.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Multiply" /> method defines the multiplication operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator *(Rectangle left, Rectangle right) {
//        return new Rectangle(
//            Ops['*'](left.X, right.X),
//            Ops['*'](left.Y, right.Y),
//            Ops['*'](left.Width, right.Width),
//            Ops['*'](left.Width, right.Height)
//        );
//    }

//    /// <summary>Multiplies the specified rectangle by the specified scalar value.</summary>
//    /// <param name="left">The rectangle.</param>
//    /// <param name="right">The scalar value.</param>
//    /// <returns>The scaled rectangle.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Multiply" /> method defines the multiplication operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator *(Rectangle left, int right) {
//        return left * new Rectangle(right);
//    }

//    /// <summary>Multiplies the scalar value by the specified rectangle.</summary>
//    /// <param name="left">The rectangle.</param>
//    /// <param name="right">The scalar value.</param>
//    /// <returns>The scaled rectangle.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Multiply" /> method defines the multiplication operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator *(int left, Rectangle right) {
//        return right * left;
//    }

//    /// <summary>Subtracts the second rectangle from the first.</summary>
//    /// <param name="left">The first rectangle.</param>
//    /// <param name="right">The second rectangle.</param>
//    /// <returns>The rectangle that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_Subtraction" /> method defines the subtraction operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator -(Rectangle left, Rectangle right) {
//        return new Rectangle(
//            Ops['-'](left.X, right.X),
//            Ops['-'](left.Y, right.Y),
//            Ops['-'](left.Width, right.Width),
//            Ops['-'](left.Width, right.Height)
//        );
//    }

//    /// <summary>Negates the specified rectangle.</summary>
//    /// <param name="value">The rectangle to negate.</param>
//    /// <returns>The negated rectangle.</returns>
//    /// <remarks>The <see cref="System.Drawing.Rectangle.op_UnaryNegation" /> method defines the unary negation operation for <see cref="System.Drawing.Rectangle" /> objects.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle operator -(Rectangle value) {
//        return Zero - value;
//    }

//    /// <summary>Adds two vectors together.</summary>
//    /// <param name="left">The first rectangle to add.</param>
//    /// <param name="right">The second rectangle to add.</param>
//    /// <returns>The summed rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Add(Rectangle left, Rectangle right) {
//        return left + right;
//    }

//    /// <summary>Restricts a rectangle between a minimum and a maximum value.</summary>
//    /// <param name="value1">The rectangle to restrict.</param>
//    /// <param name="min">The minimum value.</param>
//    /// <param name="max">The maximum value.</param>
//    /// <returns>The restricted rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Clamp(Rectangle value1, Rectangle min, Rectangle max) {
//        // We must follow HLSL behavior in the case user specified min value is bigger than max value.
//        return Min(Max(value1, min), max);
//    }

//    /// <summary>Divides the first rectangle by the second.</summary>
//    /// <param name="left">The first rectangle.</param>
//    /// <param name="right">The second rectangle.</param>
//    /// <returns>The rectangle resulting from the division.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Divide(Rectangle left, Rectangle right) {
//        return left / right;
//    }

//    /// <summary>Divides the specified rectangle by a specified scalar value.</summary>
//    /// <param name="left">The rectangle.</param>
//    /// <param name="divisor">The scalar value.</param>
//    /// <returns>The rectangle that results from the division.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Divide(Rectangle left, int divisor) {
//        return left / divisor;
//    }

//    /// <summary>Returns the dot product of two vectors.</summary>
//    /// <param name="vector1">The first rectangle.</param>
//    /// <param name="vector2">The second rectangle.</param>
//    /// <returns>The dot product.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static int Dot(Rectangle vector1, Rectangle vector2) {
//        return Ops['+'](Ops['+'](Ops['+'](
//            Ops['*'](vector1.X, vector2.X),
//            Ops['*'](vector1.Y, vector2.Y)),
//            Ops['*'](vector1.Width, vector2.Width)),
//            Ops['*'](vector1.Height, vector2.Height));
//    }

//    /// <summary>Returns a rectangle whose elements are the maximum of each of the pairs of elements in two specified vectors.</summary>
//    /// <param name="value1">The first rectangle.</param>
//    /// <param name="value2">The second rectangle.</param>
//    /// <returns>The maximized rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Max(Rectangle value1, Rectangle value2) {
//        return new Rectangle(
//            (value1.X.CompareTo(value2.X) > 0) ? value1.X : value2.X,
//            (value1.Y.CompareTo(value2.Y) > 0) ? value1.Y : value2.Y,
//            (value1.Width.CompareTo(value2.Width) > 0) ? value1.Width : value2.Width,
//            (value1.Height.CompareTo(value2.Height) > 0) ? value1.Height : value2.Height
//        );
//    }

//    /// <summary>Returns a rectangle whose elements are the minimum of each of the pairs of elements in two specified vectors.</summary>
//    /// <param name="value1">The first rectangle.</param>
//    /// <param name="value2">The second rectangle.</param>
//    /// <returns>The minimized rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Min(Rectangle value1, Rectangle value2) {
//        return new Rectangle(
//            (value1.X.CompareTo(value2.X) < 0) ? value1.X : value2.X,
//            (value1.Y.CompareTo(value2.Y) < 0) ? value1.Y : value2.Y,
//            (value1.Width.CompareTo(value2.Width) < 0) ? value1.Width : value2.Width,
//            (value1.Height.CompareTo(value2.Height) < 0) ? value1.Height : value2.Height
//        );
//    }

//    /// <summary>Returns a new rectangle whose values are the product of each pair of elements in two specified vectors.</summary>
//    /// <param name="left">The first rectangle.</param>
//    /// <param name="right">The second rectangle.</param>
//    /// <returns>The element-wise product rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Multiply(Rectangle left, Rectangle right) {
//        return left * right;
//    }

//    /// <summary>Multiplies a rectangle by a specified scalar.</summary>
//    /// <param name="left">The rectangle to multiply.</param>
//    /// <param name="right">The scalar value.</param>
//    /// <returns>The scaled rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Multiply(Rectangle left, int right) {
//        return left * right;
//    }

//    /// <summary>Multiplies a scalar value by a specified rectangle.</summary>
//    /// <param name="left">The scaled value.</param>
//    /// <param name="right">The rectangle.</param>
//    /// <returns>The scaled rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Multiply(int left, Rectangle right) {
//        return left * right;
//    }

//    /// <summary>Negates a specified rectangle.</summary>
//    /// <param name="value">The rectangle to negate.</param>
//    /// <returns>The negated rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Negate(Rectangle value) {
//        return -value;
//    }

//    /// <summary>Subtracts the second rectangle from the first.</summary>
//    /// <param name="left">The first rectangle.</param>
//    /// <param name="right">The second rectangle.</param>
//    /// <returns>The difference rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Rectangle Subtract(Rectangle left, Rectangle right) {
//        return left - right;
//    }

//    /// <summary>Copies the elements of the rectangle to a specified array.</summary>
//    /// <param name="array">The destination array.</param>
//    /// <remarks><paramref name="array" /> must have at least four elements. The method copies the rectangle's elements starting at index 0.</remarks>
//    /// <exception cref="System.NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
//    /// <exception cref="System.ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
//    /// <exception cref="System.RankException"><paramref name="array" /> is multidimensional.</exception>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public readonly void CopyTo(int[] array) {
//        // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons
//        if (array.Length < Count) throw new ArgumentException("DestinationTooShort");
//        Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref array[0]), this);
//    }

//    /// <summary>Copies the elements of the rectangle to a specified array starting at a specified index position.</summary>
//    /// <param name="array">The destination array.</param>
//    /// <param name="index">The index at which to copy the first element of the rectangle.</param>
//    /// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the four rectangle elements. In other words, elements <paramref name="index" /> through <paramref name="index" /> + 3 must already exist in <paramref name="array" />.</remarks>
//    /// <exception cref="System.NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
//    /// <exception cref="System.ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
//    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.
//    /// -or-
//    /// <paramref name="index" /> is greater than or equal to the array length.</exception>
//    /// <exception cref="System.RankException"><paramref name="array" /> is multidimensional.</exception>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public readonly void CopyTo(int[] array, int index) {
//        // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons
//        if ((uint)index >= (uint)array.Length) throw new ArgumentOutOfRangeException("IndexMustBeLess");
//        if ((array.Length - index) < Count) throw new ArgumentException("DestinationTooShort");

//        Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref array[index]), this);
//    }

//    /// <summary>Copies the rectangle to the given <see cref="Span{int}" />. The length of the destination span must be at least 4.</summary>
//    /// <param name="destination">The destination span which the values are copied into.</param>
//    /// <exception cref="System.ArgumentException">If number of elements in source rectangle is greater than those available in destination span.</exception>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public readonly void CopyTo(Span<int> destination) {
//        if (destination.Length < Count) throw new ArgumentException("DestinationTooShort");

//        Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(destination)), this);
//    }

//    /// <summary>Attempts to copy the rectangle to the given <see cref="Span{Single}" />. The length of the destination span must be at least 4.</summary>
//    /// <param name="destination">The destination span which the values are copied into.</param>
//    /// <returns><see langword="true" /> if the source rectangle was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source rectangle.</returns>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public readonly bool TryCopyTo(Span<int> destination) {
//        if (destination.Length < Count) return false;
//        Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(destination)), this);
//        return true;
//    }

//    /// <summary>Returns a value that indicates whether this instance and another rectangle are equal.</summary>
//    /// <param name="other">The other rectangle.</param>
//    /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
//    /// <remarks>Two vectors are equal if their <see cref="System.Drawing.Rectangle.X" />, <see cref="System.Drawing.Rectangle.Y" />, <see cref="System.Drawing.Rectangle.Width" />, and <see cref="System.Drawing.Rectangle.Height" /> elements are equal.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public readonly bool Equals(Rectangle other) {
//        return X.Equals(other.X)
//            && Y.Equals(other.Y)
//            && Width.Equals(other.Width)
//            && Height.Equals(other.Height);
//    }

//    /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
//    /// <param name="obj">The object to compare with the current instance.</param>
//    /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
//    /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="System.Drawing.Rectangle" /> object and their corresponding elements are equal.</remarks>
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public override readonly bool Equals(object obj) {
//        return (obj is Rectangle other) && Equals(other);
//    }

//    /// <summary>Returns the hash code for this instance.</summary>
//    /// <returns>The hash code.</returns>
//    public override readonly int GetHashCode() {
//        return HashCode.Combine(X, Y, Width, Height);
//    }

//    /// <summary>Returns the string representation of the current instance using default formatting.</summary>
//    /// <returns>The string representation of the current instance.</returns>
//    /// <remarks>This method returns a string in which each element of the rectangle is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
//    public override readonly string ToString() {
//        return ToString("G", CultureInfo.CurrentCulture);
//    }

//    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
//    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
//    /// <returns>The string representation of the current instance.</returns>
//    /// <remarks>This method returns a string in which each element of the rectangle is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
//    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
//    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
//    public readonly string ToString(string format) {
//        return ToString(format, CultureInfo.CurrentCulture);
//    }

//    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
//    /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
//    /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
//    /// <returns>The string representation of the current instance.</returns>
//    /// <remarks>This method returns a string in which each element of the rectangle is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
//    /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
//    /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
//    public readonly string ToString(string format, IFormatProvider formatProvider) {
//        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
//        return $"<{X}{separator} {Y}{separator} {Width}{separator} {Height}>";
//    }
//}
