using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text;

/// <summary>
/// ValueStringBuilder
/// </summary>
public ref struct ValueStringBuilder {
    char[] _arrayToReturnToPool;
    Span<char> _chars;
    int _pos;

    // If this ctor is used, you cannot pass in stackalloc ROS for append/replace.
    public ValueStringBuilder(ReadOnlySpan<char> initialString) : this(initialString.Length) => Append(initialString);
    public ValueStringBuilder(ReadOnlySpan<char> initialString, Span<char> initialBuffer) : this(initialBuffer) => Append(initialString);
    public ValueStringBuilder(Span<char> initialBuffer) {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }
    public ValueStringBuilder(int initialCapacity) {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    public int Length {
        get => _pos;
        set { Debug.Assert(value >= 0); Debug.Assert(value <= _chars.Length); _pos = value; }
    }

    public int Capacity => _chars.Length;

    public void EnsureCapacity(int capacity) {
        if (capacity > _chars.Length) Grow(capacity - _chars.Length);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public ref char GetPinnableReference() => ref MemoryMarshal.GetReference(_chars);

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate) {
        if (terminate) { EnsureCapacity(Length + 1); _chars[Length] = '\0'; }
        return ref MemoryMarshal.GetReference(_chars);
    }

    public ref char this[int index] {
        get { Debug.Assert(index < _pos); return ref _chars[index]; }
    }

    public override string ToString() {
        var s = _chars[.._pos].ToString();
        Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate) {
        if (terminate) { EnsureCapacity(Length + 1); _chars[Length] = '\0'; }
        return _chars[.._pos];
    }

    public ReadOnlySpan<char> AsSpan() => _chars[.._pos];
    public ReadOnlySpan<char> AsSpan(int start) => _chars[start.._pos];
    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    public bool TryCopyTo(Span<char> destination, out int charsWritten) {
        if (_chars[.._pos].TryCopyTo(destination)) {
            charsWritten = _pos;
            Dispose();
            return true;
        }
        else {
            charsWritten = 0;
            Dispose();
            return false;
        }
    }

    public void Insert(int index, char value, int count) {
        if (_pos > _chars.Length - count) Grow(count);
        var remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        _chars.Slice(index, count).Fill(value);
        _pos += count;
    }

    public void Insert(int index, ReadOnlySpan<char> s) {
        var count = s.Length;
        if (_pos > (_chars.Length - count)) Grow(count);
        int remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        s.CopyTo(_chars[index..]);
        _pos += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c) {
        var pos = _pos;
        if ((uint)pos < (uint)_chars.Length) { _chars[pos] = c; _pos = pos + 1; }
        else GrowAndAppend(c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string s) {
        var pos = _pos;
        // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        if (s.Length == 1 && (uint)pos < (uint)_chars.Length) { _chars[pos] = s[0]; _pos = pos + 1; }
        else AppendSlow(s);
    }

    void AppendSlow(string s) {
        var pos = _pos;
        if (pos > _chars.Length - s.Length) Grow(s.Length);
        s.AsSpan().CopyTo(_chars[pos..]);
        _pos += s.Length;
    }

    public void Append(char c, int count) {
        if (_pos > _chars.Length - count) Grow(count);
        Span<char> dst = _chars.Slice(_pos, count);
        for (var i = 0; i < dst.Length; i++) dst[i] = c;
        _pos += count;
    }

    public void Append(ReadOnlySpan<char> value) {
        var pos = _pos;
        if (pos > _chars.Length - value.Length) Grow(value.Length);
        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length) {
        var origPos = _pos;
        if (origPos > _chars.Length - length) Grow(length);
        _pos = origPos + length;
        return _chars.Slice(origPos, length);
    }

    public void Replace(ReadOnlySpan<char> oldChars, ReadOnlySpan<char> newChars) => Replace(oldChars, newChars, 0, _pos);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Replace(ReadOnlySpan<char> oldChars, ReadOnlySpan<char> newChars, int startIndex, int count) {
        var slice = _chars.Slice(startIndex, count);
        var indexOf = slice.IndexOf(oldChars);
        if (indexOf == -1) return;
        if (newChars.Length > oldChars.Length) {
            var i = 0;
            for (; i < oldChars.Length; ++i) slice[indexOf + i] = newChars[i];
            Insert(indexOf + i, newChars[i..]);
        }
        else if (newChars.Length < oldChars.Length) {
            var i = 0;
            for (; i < newChars.Length; ++i) slice[indexOf + i] = newChars[i];
            Remove(indexOf + i, oldChars.Length - i);
        }
        else newChars.CopyTo(slice[..oldChars.Length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Replace(char oldChar, char newChar) {
        var slice = _chars;
        var indexOf = slice.IndexOf(oldChar);
        if (indexOf == -1) return;
        slice[indexOf] = newChar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int startIndex, int length) {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
        if (length > _pos - startIndex) throw new ArgumentOutOfRangeException(nameof(length));
        if (startIndex == 0) _chars = _chars[length..];
        else if (startIndex + length == _pos) _chars = _chars[..startIndex];
        // Somewhere in the middle, this will be slow
        else _chars[(startIndex + length)..].CopyTo(_chars[startIndex..]);
        _pos -= length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void GrowAndAppend(char c) {
        Grow(1);
        Append(c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void Grow(int requiredAdditionalCapacity) {
        Debug.Assert(requiredAdditionalCapacity > 0);
        var poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_pos + requiredAdditionalCapacity, _chars.Length * 2));
        _chars.CopyTo(poolArray);
        var toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null) ArrayPool<char>.Shared.Return(toReturn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        var toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null) ArrayPool<char>.Shared.Return(toReturn);
    }
}